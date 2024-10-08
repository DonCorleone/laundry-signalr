using LaundrySignalR.Models;
using StackExchange.Redis;

namespace LaundrySignalR.Services;

public interface IRedisService
{
    IDatabase GetDatabase();

    Task<List<ReservationEntry>> GetAllEntries(List<Subject> subjects);

    Task<bool> Add(ReservationEntry reservationEntry);
    
    Task<bool> Remove(ReservationEntry reservationEntry);
}