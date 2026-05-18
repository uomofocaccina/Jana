namespace JanaApi.Model;

public class Result
{
    public bool success { get; set; } = true;
    public string message { get; set; } = string.Empty;
    public long timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

public class ResultDirectoryNote : Result
{
    public List<Folder> data { get; set; } = new List<Folder>();
    public int count { get; set; } = 0;

}

public class ResultNote : Result
{
    public List<Note> data { get; set; } = new List<Note>();
    public int count { get; set; } = 0;
}

public class ResultUser : Result
{
    public List<UserMinimal> data { get; set; } = new List<UserMinimal>();
}

public class ResultAdminAddUser : Result
{
    public int id { get; set; }
}