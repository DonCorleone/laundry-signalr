using System.Text.Json;
using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public class JsonFileService() : IJsonFileService
{
    public async Task<List<Subject>?> LoadSubjects()
    {
        if (!File.Exists("data/subjects.json"))
        {
            Console.WriteLine("File not found.");
            return [];
        }

        var json = await File.ReadAllTextAsync("data/subjects.json");
        return JsonSerializer.Deserialize<List<Subject>>(json);
    }
}