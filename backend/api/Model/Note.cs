namespace JanaApi.Model;

public class Note : NoteRequest
{
    public long timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public DateTime created { get; set; } = DateTime.UtcNow;
    public int user_id { get; set; }
    public Note() { }
    public Note(NoteRequest request, long _timestamp, DateTime _created, int _user_id)
    {
        id = request.id;
        text = request.text;
        title = request.title;
        active = request.active;
        folder = request.folder;
        timestamp = _timestamp;
        created = _created;
        user_id = _user_id;
    }
}

public class NoteRequest
{
    public string id { get; set; }
    public string text { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string folder { get; set; } = string.Empty;
    public int active { get; set; } = 1;
}

public class NoteNewFolder
{
    public string? folder { get; set; }
    public long? timestamp { get; set; } = null;
}
