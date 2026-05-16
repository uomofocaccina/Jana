using JanaApi.Model;

namespace JanaApi.Service;

public interface IFolderService
{
    Task<List<Folder>> GetFolderAsync(int user_id, long timestamp, int limit, int offset);
    Task<int> GetCountFolderAsync(int user_id, long timestamp);
    Task<Result> AddFolderAsync(Folder folder);
    Task<Result> UpdateFolderAsync(Folder folder);
    Task DeleteFolderAsync(string id, long? timestamp, int user_id);
    Task UpdateFolderParentAsync(string id, string parent, long? timestamp, int user_id);
}
