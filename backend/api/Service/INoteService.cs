using JanaApi.Model;

namespace JanaApi.Service;

public interface INoteService
{
    Task<List<Note>> GetNotesAsync(int user_id, long timestamp, int limit, int offset);
    Task<int> GetCountNotesAsync(int user_id, long timestamp);
    Task<Result> AddNoteAsync(Note note);
    Task<Result> UpdateNoteAsync(Note note);
    Task UpdateNoteFolderAsync(string id, string folder, long? timestamp, int user_id);
    Task DeleteNoteAsync(string id, long? timestamp, int user_id);
}
