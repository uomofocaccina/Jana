using JanaApi.Dapper;
using JanaApi.Model;

namespace JanaApi.Service;

public class NoteService : INoteService
{
    private readonly IDapperContext _dapperContext;
    private readonly ILogger<NoteService> _logger;
    public NoteService(IDapperContext dapperContext, ILogger<NoteService> logger)
    {
        _dapperContext = dapperContext;
        _logger = logger;
    }
    public async Task<List<Note>> GetNotesAsync(int user_id, long timestamp, int limit, int offset)
    {
        limit = limit <= 0 ? 20 : limit;
        offset = offset < 0 ? 0 : offset;

        string sql = @"SELECT * FROM note WHERE user_id = @user_id AND timestamp >= @timestamp ORDER BY created limit @limit offset @offset";
        var result = await _dapperContext.GetDataAsync<Note>(sql, new { user_id, timestamp, limit, offset });
        return result.ToList();
    }

    public async Task<int> GetCountNotesAsync(int user_id, long timestamp)
    {
        string sql = @"SELECT count(user_id) FROM note WHERE user_id = @user_id AND timestamp >= @timestamp";
        var result = await _dapperContext.GetSingleValueAsync<int>(sql, new { user_id, timestamp });
        return result;
    }

    public async Task<Result> AddNoteAsync(Note note)
    {

        if (note == null)
        {
            return new Result
            {
                message = "Note cannot be null.",
                success = false
            };
        }

        if (string.IsNullOrWhiteSpace(note.text))
        {
            return new Result
            {
                message = "Note text cannot be empty.",
                success = false
            };
        }

        Result result = new Result
        {
            success = true,
            message = "Note added successfully."
        };

        if (note.timestamp == 0)
        {
            note.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"INSERT INTO note (id,text,title,folder, timestamp, created, active, user_id) VALUES (@id,@text,@title,@folder, @timestamp, @created, @active, @user_id)";
            await _dapperContext.InsertAsync(sql, new
            {
                id = note.id,
                text = note.text,
                title = note.title,
                folder = note.folder,
                timestamp = note.timestamp,
                created = note.created,
                active = note.active,
                user_id = note.user_id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note for user {UserId}", note.user_id);
            result.success = false;
            result.message = $"An error occurred while adding the note.  {ex.StackTrace}";
        }

        return result;

    }

    public async Task<Result> UpdateNoteAsync(Note note)
    {

        if (note == null)
        {
            return new Result
            {
                message = "Note cannot be null.",
                success = false
            };
        }

        if (note.id == string.Empty)
        {
            return new Result
            {
                message = "Id cannot be empty.",
                success = false
            };
        }

        Result result = new Result
        {
            success = true,
            message = "Folder note edited successfully."
        };

        if (note.timestamp == 0)
        {
            note.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"UPDATE note SET text = @text, title = @title, folder = @folder, timestamp = @timestamp, active = @active WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = note.id,
                text = note.text,
                title = note.title,
                folder = note.folder,
                timestamp = note.timestamp,
                active = note.active,
                user_id = note.user_id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error edit note for user {UserId} ", note.user_id);
            result.success = false;
            result.message = $"An error occurred while edit the note. {ex.StackTrace}";
        }

        return result;

    }

    public async Task UpdateNoteFolderAsync(string id, string folder, long? timestamp, int user_id)
    {
        if (timestamp == null || timestamp == 0)
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"UPDATE note SET folder = @folder,timestamp = @timestamp WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = id,
                folder = folder,
                user_id = user_id,
                timestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error update folder {folder} in Note {id} for user {user_id}");
        }
    }

    public async Task DeleteNoteAsync(string id, long? timestamp, int user_id)
    {
        if (timestamp == null || timestamp == 0)
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"UPDATE note SET active = 0,timestamp = @timestamp WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = id,
                user_id = user_id,
                timestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error delete Note {id} for user {user_id}");
        }
    }
}
