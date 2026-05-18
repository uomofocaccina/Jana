using JanaApi.Model;

namespace JanaApi.Service;

public interface IUsersService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> ChangePasswordAsync(int userId, string password);
    Task<bool> UpdateAdminPasswordAsync(string newPassword);
    Task<List<UserMinimal>> GetAllUsersAsync();
    Task<ResultAdminAddUser> AddUserAsync(User userRequest);
    Task<Result> EditUserAsync(UserMinimal userRequest);
}
