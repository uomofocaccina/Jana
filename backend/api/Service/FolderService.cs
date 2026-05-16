using JanaApi.Dapper;
using JanaApi.Model;

namespace JanaApi.Service;

public class FolderService : IFolderService
{
    private readonly IDapperContext _dapperContext;
    private readonly ILogger<Folder> _logger;
    public FolderService(IDapperContext dapperContext, ILogger<Folder> logger)
    {
        _dapperContext = dapperContext;
        _logger = logger;
    }
    public async Task<List<Folder>> GetFolderAsync(int user_id, long timestamp, int limit, int offset)
    {
        limit = limit <= 0 ? 20 : limit;
        offset = offset < 0 ? 0 : offset;

        string sql = @"SELECT * FROM Folder WHERE user_id = @user_id AND timestamp > @timestamp ORDER BY created limit @limit offset @offset";
        var result = await _dapperContext.GetDataAsync<Folder>(sql, new { user_id, timestamp, limit, offset });
        return result.ToList();
    }

    public async Task<int> GetCountFolderAsync(int user_id, long timestamp)
    {
        string sql = @"SELECT count(user_id) FROM Folder WHERE user_id = @user_id AND timestamp > @timestamp";
        var result = await _dapperContext.GetSingleValueAsync<int>(sql, new { user_id, timestamp });
        return result;
    }

    public async Task<Result> AddFolderAsync(Folder folder)
    {

        if (folder == null)
        {
            return new Result
            {
                message = "Folder cannot be null.",
                success = false
            };
        }

        if (string.IsNullOrWhiteSpace(folder.text))
        {
            return new Result
            {
                message = "Folder text cannot be empty.",
                success = false
            };
        }

        if (folder.timestamp == 0)
        {
            folder.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        Result result = new Result
        {
            success = true,
            message = "Folder added successfully.",
            timestamp = folder.timestamp
        };

        try
        {
            string sql = @"INSERT INTO Folder (id,text,parent, timestamp, created, active, user_id) VALUES (@id,@text,@parent, @timestamp, @created, @active, @user_id)";
            await _dapperContext.InsertAsync(sql, new
            {
                id = folder.id,
                text = folder.text,
                parent = folder.parent,
                timestamp = folder.timestamp,
                created = folder.created,
                active = folder.active,
                user_id = folder.user_id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding Folder for user {UserId}", folder.user_id);
            result.success = false;
            result.message = "An error occurred while adding the Folder.";
        }

        return result;

    }

    public async Task<Result> UpdateFolderAsync(Folder folder)
    {

        if (folder == null)
        {
            return new Result
            {
                message = "Folder cannot be null.",
                success = false
            };
        }

        if (folder.id == string.Empty)
        {
            return new Result
            {
                message = "Id cannot be empty.",
                success = false
            };
        }

        if (folder.timestamp == 0)
        {
            folder.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        Result result = new Result
        {
            success = true,
            message = "Folder edited successfully.",
            timestamp = folder.timestamp
        };

        try
        {
            string sql = @"UPDATE Folder SET text = @text, parent = @parent, timestamp = @timestamp, active = @active WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = folder.id,
                text = folder.text,
                parent = folder.parent,
                timestamp = folder.timestamp,
                active = folder.active,
                user_id = folder.user_id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error edit Folder for user {UserId}", folder.user_id);
            result.success = false;
            result.message = "An error occurred while edit the Folder.";
        }

        return result;

    }

    public async Task DeleteFolderAsync(string id, long? timestamp,int user_id)
    {
        if (timestamp == null || timestamp == 0)
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"UPDATE Folder SET active = @active,timestamp = @timestamp WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = id,
                active = 0,
                user_id = user_id,
                timestamp = timestamp
            });

            sql = @"UPDATE Note SET active = @active,timestamp = @timestamp WHERE folder = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = id,
                active = 0,
                user_id = user_id,
                timestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error delete Folder {id} for user {user_id}");
        }
    }

    public async Task UpdateFolderParentAsync(string id, string parent, long? timestamp, int user_id)
    {
        if (timestamp == null || timestamp == 0)
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        try
        {
            string sql = @"UPDATE Folder SET parent = @parent,timestamp = @timestamp WHERE id = @id AND user_id = @user_id";
            await _dapperContext.UpdateAsync(sql, new
            {
                id = id,
                parent = parent,
                user_id = user_id,
                timestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error update parent {parent} in Folder {id} for user {user_id}");
        }
    }
}
