using JanaApi.Dapper;
using JanaApi.Model;
using JanaApi.Utilities;

namespace JanaApi.Service;

public class UsersService : IUsersService
{
    private readonly IDapperContext _dapperContext;
    private readonly ILogger<UsersService> _logger;
    
    public UsersService(IDapperContext dapperContext, ILogger<UsersService> logger)
    {
        _dapperContext = dapperContext;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        try
        {
            string sql = "SELECT * FROM Users WHERE username = @username AND active = 1";
            var user = await _dapperContext.GetObjectAsync<User>(sql, new { username });
            
            if (user != null && PasswordHelper.VerifyPassword(password, user.password))
            {
                return user;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", username);
            return null;
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            string sql = "SELECT * FROM Users WHERE id = @userId AND active = 1";
            return await _dapperContext.GetObjectAsync<User>(sql, new { userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        try
        {
            string sql = "SELECT * FROM Users WHERE username = @username AND active = 1";
            return await _dapperContext.GetObjectAsync<User>(sql, new { username });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username {Username}", username);
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!PasswordHelper.VerifyPassword(oldPassword, user.password))
            {
                return false;
            }

            string hashedNewPassword = PasswordHelper.ComputeSha256Hash(newPassword);
            string sql = "UPDATE Users SET password = @password WHERE id = @userId";
            
            await _dapperContext.UpdateAsync(sql, new { password = hashedNewPassword, userId });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UpdateAdminPasswordAsync(string newPassword)
    {
        try
        {
            string hashedNewPassword = PasswordHelper.ComputeSha256Hash(newPassword);
            string sql = "UPDATE Users SET password = @password WHERE username = 'admin'";
            
            await _dapperContext.UpdateAsync(sql, new { password = hashedNewPassword });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin password");
            return false;
        }
    }

    public async Task<int> SommaId(int id)
    {
        return id + 1;
        //await _dapperContext.InsertAsync("INSERT INTO [dbo].[Prova] ([Nome]) VALUES (@Nome)", new { Nome = "Prova" });
    }
}
