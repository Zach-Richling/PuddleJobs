using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Helpers;
using System.Reflection;
using System;

namespace PuddleJobs.ApiService.Services;

public interface IAssemblyService
{
    Task<IEnumerable<AssemblyDto>> GetAllAssembliesAsync();
    Task<AssemblyDto?> GetAssemblyByIdAsync(int id);
    Task<AssemblyDto> CreateAssemblyAsync(CreateAssemblyDto dto, byte[] zipData);
    Task<AssemblyVersionDto> CreateAssemblyVersionAsync(int assemblyId, CreateAssemblyVersionDto dto, byte[] zipData);
    Task<IEnumerable<AssemblyVersionDto>> GetAssemblyVersionsAsync(int assemblyId);
    Task<AssemblyVersionDto?> GetAssemblyVersionAsync(int assemblyId, int versionId);
    Task<bool> DeleteAssemblyAsync(int id);
    Task<AssemblyVersionDto> SetActiveVersionAsync(int assemblyId, int versionId);
}

public class AssemblyService : IAssemblyService
{
    private readonly JobSchedulerDbContext _context;
    private readonly IAssemblyStorageService _fileStorage;

    public AssemblyService(JobSchedulerDbContext context, IAssemblyStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<AssemblyDto>> GetAllAssembliesAsync()
    {
        return await _context.Assemblies.Select(a => new AssemblyDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            CreatedAt = a.CreatedAt
        }).ToListAsync();
    }

    public async Task<AssemblyDto?> GetAssemblyByIdAsync(int id)
    {
        var assembly = await _context.Assemblies
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assembly == null)
            return null;

        return new AssemblyDto
        {
            Id = assembly.Id,
            Name = assembly.Name,
            Description = assembly.Description,
            CreatedAt = assembly.CreatedAt
        };
    }

    public async Task<AssemblyDto> CreateAssemblyAsync(CreateAssemblyDto dto, byte[] zipFile)
    {
        // Check if assembly with same name already exists
        if (_context.Assemblies.Any(a => a.Name == dto.Name))
        {
            throw new InvalidOperationException($"Assembly with name '{dto.Name}' already exists.");
        }

        var assembly = new Models.Assembly
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var assemblyVersionDto = new CreateAssemblyVersionDto()
        {
            Version = "1.0.0",
            MainAssemblyName = dto.MainAssemblyName
        };

        _context.Assemblies.Add(assembly);

        await CreateAssemblyVersionAsync(assembly, assemblyVersionDto, zipFile);

        return new AssemblyDto
        {
            Id = assembly.Id,
            Name = assembly.Name,
            Description = assembly.Description,
            CreatedAt = assembly.CreatedAt
        };
    }

    public async Task<AssemblyVersionDto> CreateAssemblyVersionAsync(int assemblyId, CreateAssemblyVersionDto dto, byte[] zipData)
    {
        var assembly = await _context.Assemblies
            .Include(a => a.Versions)
            .FirstOrDefaultAsync(a => a.Id == assemblyId);

        if (assembly == null)
            throw new InvalidOperationException($"Assembly with ID {assemblyId} not found.");

        // Check if version already exists
        if (assembly.Versions.Any(v => v.Version == dto.Version))
        {
            throw new InvalidOperationException($"Version '{dto.Version}' already exists for assembly '{assembly.Name}'.");
        }

        return await CreateAssemblyVersionAsync(assembly, dto, zipData);
    }

    private async Task<AssemblyVersionDto> CreateAssemblyVersionAsync(Models.Assembly assembly, CreateAssemblyVersionDto dto, byte[] zipData)
    {
        // Validate the ZIP contains a valid assembly
        if (!IsValidJobAssemblyFromZip(zipData, dto.MainAssemblyName, out var validAssembly))
        {
            throw new InvalidOperationException($"The uploaded ZIP does not contain a valid assembly that implements Quartz.IJob.");
        }

        // Save and extract ZIP to file system
        var directoryPath = await _fileStorage.SaveAssemblyVersionAsync(assembly.Name, dto.Version, zipData);

        var assemblyVersion = new AssemblyVersion 
        {
            Version = dto.Version,
            DirectoryPath = directoryPath,
            MainAssemblyName = dto.MainAssemblyName,
            ChangeNotes = dto.ChangeNotes,
            UploadedAt = DateTime.UtcNow,
            IsActive = assembly.Versions.Count == 0
        };

        assembly.Versions.Add(assemblyVersion);

        ExtractAndStoreParameterDefinitions(assemblyVersion, validAssembly);

        await _context.SaveChangesAsync();

        return new AssemblyVersionDto
        {
            Id = assemblyVersion.Id,
            Version = assemblyVersion.Version,
            FileName = assemblyVersion.MainAssemblyName,
            UploadedAt = assemblyVersion.UploadedAt,
            ChangeNotes = assemblyVersion.ChangeNotes,
            AssemblyId = assemblyVersion.AssemblyId,
            IsActive = assemblyVersion.IsActive
        };
    }

    public async Task<IEnumerable<AssemblyVersionDto>> GetAssemblyVersionsAsync(int assemblyId)
    {
        return await _context.AssemblyVersions
            .Where(av => av.AssemblyId == assemblyId)
            .OrderByDescending(av => av.UploadedAt)
            .Select(av => new AssemblyVersionDto
            {
                Id = av.Id,
                Version = av.Version,
                FileName = av.MainAssemblyName,
                UploadedAt = av.UploadedAt,
                ChangeNotes = av.ChangeNotes,
                AssemblyId = av.AssemblyId,
                IsActive = av.IsActive
            })
            .ToListAsync();
    }

    public async Task<AssemblyVersionDto?> GetAssemblyVersionAsync(int assemblyId, int versionId)
    {
        var assemblyVersion = await _context.AssemblyVersions
            .FirstOrDefaultAsync(av => av.AssemblyId == assemblyId && av.Id == versionId);

        if (assemblyVersion == null)
            return null;

        return new AssemblyVersionDto
        {
            Id = assemblyVersion.Id,
            Version = assemblyVersion.Version,
            FileName = assemblyVersion.MainAssemblyName,
            UploadedAt = assemblyVersion.UploadedAt,
            ChangeNotes = assemblyVersion.ChangeNotes,
            AssemblyId = assemblyVersion.AssemblyId,
            IsActive = assemblyVersion.IsActive
        };
    }

    public async Task<AssemblyVersionDto> SetActiveVersionAsync(int assemblyId, int versionId)
    {
        var assembly = await _context.Assemblies
            .Include(a => a.Versions)
            .FirstOrDefaultAsync(a => a.Id == assemblyId);

        if (assembly == null)
            throw new InvalidOperationException($"Assembly with ID {assemblyId} not found.");

        var targetVersion = assembly.Versions.FirstOrDefault(v => v.Id == versionId);
        if (targetVersion == null)
            throw new InvalidOperationException($"Version with ID {versionId} not found for assembly '{assembly.Name}'.");

        // Deactivate all other versions for this assembly
        foreach (var version in assembly.Versions)
        {
            version.IsActive = false;
        }

        // Activate the target version
        targetVersion.IsActive = true;

        await _context.SaveChangesAsync();

        return new AssemblyVersionDto
        {
            Id = targetVersion.Id,
            Version = targetVersion.Version,
            FileName = targetVersion.MainAssemblyName,
            UploadedAt = targetVersion.UploadedAt,
            ChangeNotes = targetVersion.ChangeNotes,
            AssemblyId = targetVersion.AssemblyId,
            IsActive = targetVersion.IsActive
        };
    }

    public async Task<bool> DeleteAssemblyAsync(int id)
    {
        var assembly = await _context.Assemblies
            .Include(a => a.Jobs)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assembly == null)
            return false;

        // Check if assembly has active jobs
        if (assembly.Jobs.Any(j => j.IsActive))
        {
            throw new InvalidOperationException($"Cannot delete assembly '{assembly.Name}' because it has active jobs.");
        }

        assembly.IsDeleted = true;
        assembly.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private static bool IsValidJobAssemblyFromZip(byte[] zipData, string mainAssemblyName, out System.Reflection.Assembly validAssembly)
    {
        try
        {
            using var zipStream = new MemoryStream(zipData);
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
            
            var mainAssemblyEntry = archive.GetEntry(mainAssemblyName);
            if (mainAssemblyEntry == null)
            {
                validAssembly = null!;
                return false;
            }

            using var assemblyStream = mainAssemblyEntry.Open();
            using var memoryStream = new MemoryStream();
            assemblyStream.CopyTo(memoryStream);

            var assembly = System.Reflection.Assembly.Load(memoryStream.ToArray());
            var jobTypes = assembly.GetTypes()
                .Where(t => t.IsClass 
                    && !t.IsAbstract 
                    && typeof(Quartz.IJob).IsAssignableFrom(t));

            if(jobTypes.Any())
            {
                validAssembly = assembly;
                return true;
            }

            validAssembly = null!;
            return false;
        }
        catch
        {
            validAssembly = null!;
            return false;
        }
    }

    private static void ExtractAndStoreParameterDefinitions(AssemblyVersion assemblyVersion, System.Reflection.Assembly assembly)
    {
        var jobType = assembly.GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(Quartz.IJob).IsAssignableFrom(t))
            .First();

        var parameterDefinitions = JobParameterHelper.GetJobParameterInfo(jobType);

        foreach (var paramDef in parameterDefinitions)
        {
            var dbParamDef = new AssemblyParameterDefinition
            {
                Name = paramDef.Name,
                Type = paramDef.Type,
                Description = paramDef.Description,
                DefaultValue = paramDef.DefaultValue,
                Required = paramDef.Required,
                AssemblyVersionId = assemblyVersion.Id,
                CreatedAt = DateTime.UtcNow
            };

            assemblyVersion.ParameterDefinitions.Add(dbParamDef);
        }
    }
} 