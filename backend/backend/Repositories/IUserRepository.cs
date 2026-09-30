using backend.DTOs;
using backend.Entities;

namespace backend.Repositories;

public interface IUserRepository
{
    Task<User> SaveUser(User user);
    Task<int> DeleteUser(string userName);
    Task<User?> GetUser(int id);
    Task<User?> GetUserByUsername(string username);
}