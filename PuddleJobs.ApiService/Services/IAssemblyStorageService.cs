using PuddleJobs.ApiService.Models;
using System.Runtime.Loader;

namespace PuddleJobs.ApiService.Services;
public interface IAssemblyStorageService
{
    Task<string> SaveAssemblyVersionAsync(string assemblyName, string version, byte[] zipData);
    Task<System.Reflection.Assembly> LoadAssemblyVersionAsync(AssemblyVersion assemblyVersion, AssemblyLoadContext loadContext);
}
