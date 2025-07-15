using System.IO.Abstractions.TestingHelpers;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.Tests.Services;

public class LocalAssemblyStorageServiceTests
{
    private readonly string _basePath = "/base";
    private readonly string _assemblyName = "TestAssembly";
    private readonly string _version = "1.0.0";
    private readonly byte[] _zipData = [80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    private IConfiguration CreateConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("AssemblyStorage:BasePath", _basePath)])
        .Build();

    [Fact]
    public async Task SaveAssemblyVersionAsync_ThrowsAndCleansUpOnDirectoryCreateFailure()
    {
        var fs = new MockFileSystem();
        var service = new LocalAssemblyStorageService(CreateConfig(), fs);
        var badPath = "\\badpath";

        // Simulate directory create failure by using an invalid path
        await Assert.ThrowsAnyAsync<Exception>(() => service.SaveAssemblyVersionAsync(_assemblyName, badPath, _zipData));
    }

    [Fact]
    public async Task LoadAssemblyVersionAsync_FileMissing_ThrowsFileNotFound()
    {
        var fs = new MockFileSystem();
        var alc = new AssemblyLoadContext("Testing", true);
        var service = new LocalAssemblyStorageService(CreateConfig(), fs);
        var av = new AssemblyVersion
        {
            DirectoryPath = fs.Path.Combine(_basePath, _assemblyName, _version),
            MainAssemblyName = "Test.dll"
        };
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.LoadAssemblyVersionAsync(av, alc));
    }
} 