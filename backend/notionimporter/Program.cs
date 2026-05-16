using JanaApi.Model;
using System.Net.Http.Json;

string url = "https://jana.server/api";

if (!Directory.Exists("notion"))
{
    Directory.CreateDirectory("notion");
}

string cartella = File.ReadAllText("notion/cartella.txt").Trim();

Folder folder = new Folder
{
    id = Guid.NewGuid().ToString(),
    text = cartella
};

HttpClient httpClient = new HttpClient
{
    BaseAddress = new Uri(url)
};

try
{
    var risposta = httpClient.PutAsync(url+"/folder", new StringContent(System.Text.Json.JsonSerializer.Serialize(folder), System.Text.Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

    var contentJson = await risposta.Content.ReadFromJsonAsync<Result>();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return;
}

var fileInDirectory = Directory.GetFiles("notion", "*.md", SearchOption.AllDirectories);

foreach (var file in fileInDirectory)
{
    string fileName = Path.GetFileName(file);
    string fileContent = File.ReadAllText(file);
    var rows = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    string title = rows.Length > 0 ? rows[0] : fileName;
    title = title.Trim().Replace("#", "").Trim();
    string text = string.Join(Environment.NewLine, rows.Skip(5)).Trim();
    Note note = new Note
    {
        id = Guid.NewGuid().ToString(),
        title = title,
        text = text,
        folder = folder.id
    };
    try
    {
        var risposta = httpClient.PutAsync(url+"/note", new StringContent(System.Text.Json.JsonSerializer.Serialize(note), System.Text.Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
        var contentJson = await risposta.Content.ReadFromJsonAsync<Result>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return;
    }
}