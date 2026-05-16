namespace JanaApi.Model;

public class Result
{
    public bool success { get; set; } = true;
    public string message { get; set; } = string.Empty;
    public long timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class ResultDirecotoryNote : Result
{
    public List<Folder> data { get; set; } = new List<Folder>();
    public int count { get; set; } = 0;

}

public class ResultNote : Result
{
    public List<Note> data { get; set; } = new List<Note>();
    public int count { get; set; } = 0;
}