using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;

namespace JanaApi.Controllers;

[Route("api/folder")]
[ApiController]
[Authorize]
public class FolderController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly INoteService _note;
    private readonly IFolderService _folder;
    private readonly IUsersService _users;

    public FolderController(ILogger<ApiController> logger, INoteService note, IFolderService folder, IUsersService users)
    {
        _logger = logger;
        _note = note;
        _folder = folder;
        _users = users;
    }

    [HttpGet("{timestamp}")]
    public async Task<IActionResult> GetFolder(long timestamp, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        ResultDirectoryNote result = new ResultDirectoryNote();
        int idUtente = JwtClaimsHelper.GetUserId(User);

        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        try
        {
            result.count = await _folder.GetCountFolderAsync(idUtente, timestamp);
            if (result.count != 0)
            {
                var resultFolder = await _folder.GetFolderAsync(idUtente, timestamp, limit, offset);
                result.data = resultFolder;
            }

            result.success = true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Folder for user {UserId} at timestamp {Timestamp}", idUtente, timestamp);
            result.success = false;
            result.message = $"Error retrieving Folder: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPut()]
    public async Task<IActionResult> AddFolder(FolderRequest folderRequest)
    {
        Result result = new Result();
        if (folderRequest == null)
        {
            result.success = false;
            result.message = "Folder cannot be null.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        Folder folder = new Folder(folderRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtente);

        try
        {
            result = await _folder.AddFolderAsync(folder);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error add Folder for user {UserId}", folder.user_id);
            result.success = false;
            result.message = $"Error add Folder: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost()]
    public async Task<IActionResult> UpdateFolder(FolderRequest folderRequest)
    {
        Result result = new Result();
        if (folderRequest == null)
        {
            result.success = false;
            result.message = "Folder cannot be null.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        Folder folder = new Folder(folderRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtente);
        try
        {
            result = await _folder.UpdateFolderAsync(folder);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error edit Folder for user {UserId}", folder.user_id);
            result.success = false;
            result.message = $"Error edit Folder: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(string id)
    {
        Result result = new Result();
        if (!Guid.TryParse(id, out Guid result1))
        {
            result.success = false;
            result.message = "Folder id can be valid guid.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        try
        {
            await _folder.DeleteFolderAsync(id,null, idUtente);

        }
        catch (Exception ex)
        {
            result.success = false;
            result.message = $"Error delete Folder: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost("parent/{id}")]
    public async Task<IActionResult> UpdateFolderParent(string id, FolderNewParent parentData)
    {
        Result result = new Result();
        if (!Guid.TryParse(id, out Guid result1))
        {
            result.success = false;
            result.message = "Folder id can be valid guid.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        try
        {
            await _folder.UpdateFolderParentAsync(id, parentData.parent, parentData.timestamp, idUtente);

        }
        catch (Exception ex)
        {
            result.success = false;
            result.message = $"Error edit Folder: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }
}
