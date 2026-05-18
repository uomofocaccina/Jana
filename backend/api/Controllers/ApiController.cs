using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JanaApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly INoteService _note;
        private readonly IFolderService _folder;

        public ApiController(ILogger<ApiController> logger, INoteService note, IFolderService folder)
        {
            _logger = logger;
            _note = note;
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

        //for debug purposes, creates 3 folders with 3 subfolders each and 10 notes in each subfolder for the given user id
        [HttpGet("createrandomnoteandfolder")]
        [Authorize]
        public async Task<IActionResult> CreateRandomNoteAndFolder(int idUser)
        {
            if (!JwtClaimsHelper.IsAdmin(User))
            {
                return Unauthorized();
            }

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
                    user_id = idUser,
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
                        user_id = idUser,
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
                            user_id = idUser,
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
    }
}
