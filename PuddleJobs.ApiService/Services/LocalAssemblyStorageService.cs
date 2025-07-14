using System.IO;
using System.IO.Compression;
using PuddleJobs.ApiService.Models;
using System.IO.Abstractions;
using System.Runtime.Loader;

namespace PuddleJobs.ApiService.Services;

public class LocalAssemblyStorageService : IAssemblyStorageService
{
    private readonly string _baseDirectory;
    private readonly IFileSystem _fileSystem;

    public LocalAssemblyStorageService(IConfiguration configuration, IFileSystem fileSystem)
    {
        _baseDirectory = configuration["AssemblyStorage:BasePath"] 
            ?? throw new Exception("AssemblyStorage:BasePath is not configured. Assemblies cannot be saved");
        
        _fileSystem = fileSystem;
    }

    public async Task<string> SaveAssemblyVersionAsync(string assemblyName, string version, byte[] zipData)
    {
        var assemblyVersionPath = _fileSystem.Path.Combine(_baseDirectory, assemblyName, string.Join("", version.Split(_fileSystem.Path.GetInvalidPathChars())));
        _fileSystem.Directory.CreateDirectory(assemblyVersionPath);

        try
        {
            var tempZipPath = _fileSystem.Path.Combine(assemblyVersionPath, "temp.zip");
            await _fileSystem.File.WriteAllBytesAsync(tempZipPath, zipData);
        
            ZipFile.ExtractToDirectory(tempZipPath, assemblyVersionPath);
            _fileSystem.File.Delete(tempZipPath);
            
            return assemblyVersionPath;
        }
        catch (Exception)
        {
            try
            {
                _fileSystem.Directory.Delete(assemblyVersionPath, true);
            }
            catch { }
            throw;
        }
    }

    public async Task<System.Reflection.Assembly> LoadAssemblyVersionAsync(AssemblyVersion assemblyVersion, AssemblyLoadContext loadContext)
    {
        var fullPath = _fileSystem.Path.Combine(assemblyVersion.DirectoryPath, assemblyVersion.MainAssemblyName);
        if (!_fileSystem.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Assembly file not found: {fullPath}");
        }

        return await Task.FromResult(loadContext.LoadFromAssemblyPath(fullPath));
    }
} 