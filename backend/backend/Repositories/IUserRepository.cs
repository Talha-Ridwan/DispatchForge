using backend.Entities;

namespace backend.Repositories;

public interface IUserRepository
{
    Task<User> SaveUser(User user);
    Task DeleteUser(int id);
    Task<User?> GetUser(int id);
    Task<User?> GetUserByUsername(string username);
}