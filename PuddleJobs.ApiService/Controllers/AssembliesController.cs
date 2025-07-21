using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.ApiService.Controllers;

/// <summary>
/// Manages job assemblies and their versions
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssembliesController : ControllerBase
{
    private readonly IAssemblyService _assemblyService;

    public AssembliesController(IAssemblyService assemblyService)
    {
        _assemblyService = assemblyService;
    }

    /// <summary>
    /// Gets all assemblies
    /// </summary>
    /// <returns>List of all assemblies</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssemblyDto>>> GetAssemblies()
    {
        var assemblies = await _assemblyService.GetAllAssembliesAsync();
        return Ok(assemblies);
    }

    /// <summary>
    /// Gets a specific assembly by ID
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <returns>Assembly details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<AssemblyDto>> GetAssembly(int id)
    {
        var assembly = await _assemblyService.GetAssemblyByIdAsync(id);
        if (assembly == null)
            return NotFound();

        return Ok(assembly);
    }

    /// <summary>
    /// Creates a new assembly
    /// </summary>
    /// <param name="dto">Assembly creation data</param>
    /// <param name="zipFile">The zipped publish of the assembly</param>
    /// <returns>Created assembly</returns>
    [HttpPost]
    public async Task<ActionResult<AssemblyDto>> CreateAssembly([FromForm] CreateAssemblyDto dto, IFormFile zipFile)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await zipFile.CopyToAsync(memoryStream);
            var zipData = memoryStream.ToArray();

            var assembly = await _assemblyService.CreateAssemblyAsync(dto, zipData);
            return CreatedAtAction(nameof(GetAssembly), new { id = assembly.Id }, assembly);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Uploads a new version of an assembly
    /// </summary>5
    /// <param name="id">Assembly ID</param>
    /// <param name="dto">Version information</param>
    /// <param name="zipFile">ZIP file containing the assembly</param>
    /// <returns>Created assembly version</returns>
    [HttpPost("{id}/versions")]
    public async Task<ActionResult<AssemblyVersionDto>> UploadVersion(int id, [FromForm] CreateAssemblyVersionDto dto, IFormFile zipFile)
    {
        if (zipFile == null || zipFile.Length == 0)
            return BadRequest("No ZIP file uploaded.");

        if (string.IsNullOrWhiteSpace(dto.MainAssemblyName))
            return BadRequest("Main assembly name is required.");

        try
        {
            using var memoryStream = new MemoryStream();
            await zipFile.CopyToAsync(memoryStream);
            var zipData = memoryStream.ToArray();

            var version = await _assemblyService.CreateAssemblyVersionAsync(id, dto, zipData);

            return CreatedAtAction(nameof(GetVersion), new { id, versionId = version.Id }, version);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets all versions of an assembly
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <returns>List of assembly versions</returns>
    [HttpGet("{id}/versions")]
    public async Task<ActionResult<IEnumerable<AssemblyVersionDto>>> GetVersions(int id)
    {
        var versions = await _assemblyService.GetAssemblyVersionsAsync(id);
        return Ok(versions);
    }

    /// <summary>
    /// Gets a specific version of an assembly
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <param name="versionId">Version ID</param>
    /// <returns>Assembly version details</returns>
    [HttpGet("{id}/versions/{versionId}")]
    public async Task<ActionResult<AssemblyVersionDto>> GetVersion(int id, int versionId)
    {
        var version = await _assemblyService.GetAssemblyVersionAsync(id, versionId);
        if (version == null)
            return NotFound();

        return Ok(version);
    }

    /// <summary>
    /// Gets parameter definitions for an assembly (from active version)
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <returns>Parameter definitions</returns>
    [HttpGet("{id}/parameters")]
    public async Task<ActionResult<AssemblyParameterDefintionDto[]>> GetAssemblyParameters(int id)
    {
        try
        {
            var parameters = await _assemblyService.GetAssemblyParametersAsync(id);
            return Ok(parameters);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error loading assembly parameters: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets a version as the active version for an assembly
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <param name="versionId">Version ID to activate</param>
    /// <returns>Activated assembly version</returns>
    [HttpPost("{id}/versions/{versionId}/activate")]
    public async Task<ActionResult<AssemblyVersionDto>> SetActiveVersion(int id, int versionId)
    {
        try
        {
            var version = await _assemblyService.SetActiveVersionAsync(id, versionId);
            return Ok(version);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes an assembly (soft delete)
    /// </summary>
    /// <param name="id">Assembly ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAssembly(int id)
    {
        try
        {
            var deleted = await _assemblyService.DeleteAssemblyAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
} 