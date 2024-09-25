using System.Text.Json;
using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public interface IJsonFileService
{
    Task<List<Subject>?> LoadSubjects();
}