using System.Text.Json;
using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public class JsonFileService() : IJsonFileService
{
    private List<Subject>? _subjects;
    public async Task<List<Subject>?> LoadSubjects()
    {
        if (_subjects == null)
        {
            if (!File.Exists("data/subjects.json"))
            {
                Console.WriteLine("File not found.");
                return [];
            }

            var json = await File.ReadAllTextAsync("data/subjects.json");
            _subjects = JsonSerializer.Deserialize<List<Subject>>(json);
            return _subjects;
        }

        return _subjects;
    }
}