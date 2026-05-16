using JanaApi.Model;

namespace JanaApi.Service;

public interface IUsersService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> UpdateAdminPasswordAsync(string newPassword);
}
