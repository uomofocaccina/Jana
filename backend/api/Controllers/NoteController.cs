using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;

namespace JanaApi.Controllers;

[ApiController]
[Route("api/note")]
[Authorize]
public class NoteController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly INoteService _note;

    public NoteController(ILogger<ApiController> logger, INoteService note, IUsersService users)
    {
        _logger = logger;
        _note = note;
    }

    [HttpGet("{timestamp}")]
    public async Task<IActionResult> GetNotes(long timestamp, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        ResultNote result = new ResultNote();
        int idUtente = JwtClaimsHelper.GetUserId(User);

        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        try
        {
            result.count = await _note.GetCountNotesAsync(idUtente, timestamp);
            if (result.count != 0)
            {
                var resultNote = await _note.GetNotesAsync(idUtente, timestamp, limit, offset);
                result.data = resultNote;
            }

            result.success = true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notes for user {UserId} at timestamp {Timestamp}", idUtente, timestamp);
            result.success = false;
            result.message = $"Error retrieving notes: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPut()]
    public async Task<IActionResult> AddNote(NoteRequest noteRequest)
    {
        Result result = new Result();
        if (noteRequest == null)
        {
            result.success = false;
            result.message = "Note cannot be null.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        Note note = new Note(noteRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtente);

        try
        {
            result = await _note.AddNoteAsync(note);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error add note for user {UserId}", note.user_id);
            result.success = false;
            result.message = $"Error add note: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost()]
    public async Task<IActionResult> UpdateNote(NoteRequest noteRequest)
    {
        Result result = new Result();
        if (noteRequest == null)
        {
            result.success = false;
            result.message = "Note cannot be null.";
            return BadRequest(result);
        }

        int idUtente = JwtClaimsHelper.GetUserId(User);
        if (idUtente == -1)
        {
            result.success = false;
            result.message = "Invalid user token.";
            return Unauthorized(result);
        }

        Note note = new Note(noteRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtente);
        try
        {
            result = await _note.UpdateNoteAsync(note);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error edit note for user {UserId}", note.user_id);
            result.success = false;
            result.message = $"Error edit note: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(string id)
    {
        Result result = new Result();
        if (!Guid.TryParse(id, out Guid result1))
        {
            result.success = false;
            result.message = "Note id can be valid guid.";
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
            await _note.DeleteNoteAsync(id, result.timestamp, idUtente);

        }
        catch (Exception ex)
        {
            result.success = false;
            result.message = $"Error delete note: {id} : {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost("directory/{id}")]
    public async Task<IActionResult> UpdateNoteFolder(string id, NoteNewFolder parentData)
    {
        Result result = new Result();
        if (!Guid.TryParse(id, out Guid result1))
        {
            result.success = false;
            result.message = "Note id can be valid guid.";
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
            await _note.UpdateNoteFolderAsync(id, parentData.folder, parentData.timestamp, idUtente);

        }
        catch (Exception ex)
        {
            result.success = false;
            result.message = $"Error edit note: {id} : {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }
}
