using System.IO;
using System.IO.Compression;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Services;

public class LocalAssemblyStorageService : IAssemblyStorageService
{
    private readonly string _baseDirectory;
    private readonly ILogger<LocalAssemblyStorageService> _logger;

    public LocalAssemblyStorageService(IConfiguration configuration, ILogger<LocalAssemblyStorageService> logger)
    {
        _baseDirectory = configuration["AssemblyStorage:BasePath"] 
            ?? throw new Exception("AssemblyStorage:BasePath is not configured. Assemblies cannot be saved");
        
        _logger = logger;
    }

    public async Task<string> SaveAssemblyVersionAsync(string assemblyName, string version, byte[] zipData)
    {
        var assemblyVersionPath = Path.Combine(_baseDirectory, assemblyName, string.Join("", version.Split(Path.GetInvalidPathChars())));
        Directory.CreateDirectory(assemblyVersionPath);

        try
        {
            var tempZipPath = Path.Combine(assemblyVersionPath, "temp.zip");
            await File.WriteAllBytesAsync(tempZipPath, zipData);
        
            ZipFile.ExtractToDirectory(tempZipPath, assemblyVersionPath);
            File.Delete(tempZipPath);
            
            _logger.LogInformation("Extracted assembly {AssemblyName} version {Version} from ZIP to {DirectoryPath}", 
                assemblyName, version, assemblyVersionPath);
            
            return assemblyVersionPath;
        }
        catch (Exception)
        {
            try
            {
                Directory.Delete(assemblyVersionPath, true);
            }
            catch { }
            throw;
        }
    }

    public async Task<System.Reflection.Assembly> LoadAssemblyVersionAsync(AssemblyVersion assemblyVersion)
    {
        var fullPath = Path.Combine(assemblyVersion.DirectoryPath, assemblyVersion.MainAssemblyName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Assembly file not found: {fullPath}");
        }
        
        return await Task.FromResult(System.Reflection.Assembly.LoadFrom(fullPath));
    }
} 