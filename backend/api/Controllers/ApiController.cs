using Microsoft.AspNetCore.Mvc;
using JanaApi.Model;
using JanaApi.Service;

namespace JanaApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly INoteService _note;
        private readonly IFolderService _folder;
        private readonly IUsersService _users;
        private readonly int idUtenteFisso = 1;

        public ApiController(ILogger<ApiController> logger, INoteService note, IUsersService users, IFolderService folder)
        {
            _logger = logger;
            _note = note;
            _users = users;
            _folder = folder;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> GetPing()
        {
            Result result = new Result
            {
                message = "Pong!",
                success = true
            };
            return Ok(result);
        }

        [HttpGet("createrandomnoteandfolder")]
        public async Task<IActionResult> CreateRandomNoteAndFolder()
        {
            using var httpClient = new HttpClient();
            string loremApi = "https://baconipsum.com/api/?type=meat-and-filler&paras=2";

            for (int i = 0; i < 3; i++)
            {
                // get text for folder
                var folderTextResponse = await httpClient.GetStringAsync(loremApi);
                var folderText = System.Text.Json.JsonSerializer.Deserialize<List<string>>(folderTextResponse)?[0].Substring(0, 15) ?? $"Folder {i}";

                var folder = new Folder
                {
                    id = Guid.NewGuid().ToString(),
                    text = folderText,
                    user_id = idUtenteFisso,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                await _folder.AddFolderAsync(folder);

                for (int f = 0; f < 3; f++)
                {
                    // get text for subfolder
                    var subFolderTextResponse = await httpClient.GetStringAsync(loremApi);
                    var subFolderText = System.Text.Json.JsonSerializer.Deserialize<List<string>>(subFolderTextResponse)?[0].Substring(0, 15) ?? $"SubFolder {i}-{f}";

                    var subFolder = new Folder
                    {
                        id = Guid.NewGuid().ToString(),
                        text = subFolderText,
                        parent = folder.id,
                        user_id = idUtenteFisso,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    await _folder.AddFolderAsync(subFolder);

                    for (int j = 0; j < 10; j++)
                    {
                        // get text for note
                        var noteTextResponse = await httpClient.GetStringAsync(loremApi);
                        var noteText = System.Text.Json.JsonSerializer.Deserialize<List<string>>(noteTextResponse)?[0] ?? $"Content of note {i}-{j}";

                        var note = new Note
                        {
                            id = Guid.NewGuid().ToString(),
                            title = $"{noteText.Substring(1, 15)} {f}-{j}",
                            text = noteText,
                            folder = subFolder.id,
                            user_id = idUtenteFisso,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                        await _note.AddNoteAsync(note);
                    }
                }
            }

            Result result = new Result
            {
                message = "done",
                success = true
            };
            return Ok(result);
        }

        //[HttpGet("notes/{timestamp}")]
        //public async Task<IActionResult> GetNotes(long timestamp, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
        //{
        //    ResultNote result = new ResultNote();
        //    int idUtente = idUtenteFisso; // For testing purposes, using a fixed user ID

        //    try
        //    {
        //        result.count = await _note.GetCountNotesAsync(idUtente, timestamp);
        //        if (result.count != 0)
        //        {
        //            var resultNote = await _note.GetNotesAsync(idUtente, timestamp, limit, offset);
        //            result.data = resultNote;
        //        }

        //        result.success = true;

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving notes for user {UserId} at timestamp {Timestamp}", idUtente, timestamp);
        //        result.success = false;
        //        result.message = $"Error retrieving notes: {ex.StackTrace}";
        //        return StatusCode(500, result);
        //    }

        //    return Ok(result);
        //}

        //[HttpPut("note")]
        //public async Task<IActionResult> AddNote(NoteRequest noteRequest)
        //{
        //    Result result = new Result();
        //    if (noteRequest == null)
        //    {
        //        result.success = false;
        //        result.message = "Note cannot be null.";
        //        return BadRequest(result);
        //    }

        //    Note note = new Note(noteRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtenteFisso);

        //    try
        //    {
        //        result = await _note.AddNoteAsync(note);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error add note for user {UserId}", note.user_id);
        //        result.success = false;
        //        result.message = $"Error add note: {ex.StackTrace}";
        //        return StatusCode(500, result);
        //    }

        //    return Ok(result);
        //}

        //[HttpPost("note")]
        //public async Task<IActionResult> UpdateNote(NoteRequest noteRequest)
        //{
        //    Result result = new Result();
        //    if (noteRequest == null)
        //    {
        //        result.success = false;
        //        result.message = "Note cannot be null.";
        //        return BadRequest(result);
        //    }

        //    Note note = new Note(noteRequest, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTime.UtcNow, idUtenteFisso);
        //    try
        //    {
        //        result = await _note.UpdateNoteAsync(note);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error edit note for user {UserId}", note.user_id);
        //        result.success = false;
        //        result.message = $"Error edit note: {ex.StackTrace}";
        //        return StatusCode(500, result);
        //    }

        //    return Ok(result);
        //}
    }
}
