namespace JanaApi.Model;

public class Folder : FolderRequest
{
    public long timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public DateTime created { get; set; } = DateTime.UtcNow;
    public int user_id { get; set; }
    public Folder() { }
    public Folder(FolderRequest request, long _timestamp, DateTime _created, int _user_id)
    {
        id = request.id;
        text = request.text;
        parent = request.parent;
        active = request.active;
        timestamp = _timestamp;
        created = _created;
        user_id = _user_id;
    }
}

public class FolderRequest
{
    public string id { get; set; }
    public string text { get; set; } = string.Empty;
    public string? parent { get; set; } = null;
    public int active { get; set; } = 1;
}

public class FolderNewParent
{
    public string? parent { get; set; }
    public long? timestamp { get; set; } = null;
}
