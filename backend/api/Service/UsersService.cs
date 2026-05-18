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

            return await ChangePasswordAsync(userId, newPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string password)
    {
        try
        {
            string hashedNewPassword = PasswordHelper.ComputeSha256Hash(password);
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

    public async Task<List<UserMinimal>> GetAllUsersAsync()
    {
        try
        {
            string sql = "SELECT id, username, name, active, admin, created FROM Users";
            var result = await _dapperContext.GetDataAsync<UserMinimal>(sql);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return new List<UserMinimal>();
        }
    }

    public async Task<ResultAdminAddUser> AddUserAsync(User userRequest)
    {
        ResultAdminAddUser result = new ResultAdminAddUser();
        try
        {
            if (string.IsNullOrEmpty(userRequest.password) || userRequest.password.Length < 6)
            {
                result.success = false;
                result.message = "Password is required and must be at least 6 characters long.";
                return result;
            }

            if (string.IsNullOrEmpty(userRequest.name))
            {
                result.success = false;
                result.message = "Name is required.";
                return result;
            }

            if (string.IsNullOrEmpty(userRequest.username))
            {
                result.success = false;
                result.message = "Username is required.";
                return result;
            }

            string sql = "select count(*) from Users where username = @username";
            int count = await _dapperContext.GetSingleValueAsync<int>(sql, new { username = userRequest.username });

            if (count > 0)
            {
                result.success = false;
                result.message = "Username already exists.";
                return result;
            }

            string hashedPassword = PasswordHelper.ComputeSha256Hash(userRequest.password);
            sql = "INSERT INTO Users (username, password, admin, active, name) VALUES (@username, @password, @admin, @active, @name); SELECT last_insert_rowid() as int;";
            int userId = await _dapperContext.GetSingleValueAsync<int>(sql, new { username = userRequest.username, password = hashedPassword, admin = userRequest.admin, active = userRequest.active, name = userRequest.name });
            result.success = true;
            result.message = "User added successfully.";
            result.id = userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {Username}", userRequest.username);
            result.success = false;
            result.message = $"Error adding user: {ex.Message}";
        }

        return result;
    }

    public async Task<Result> EditUserAsync(UserMinimal userRequest)
    {
        Result result = new Result();
        try
        {
            if (string.IsNullOrEmpty(userRequest.name))
            {
                result.success = false;
                result.message = "Name is required.";
                return result;
            }

            if (string.IsNullOrEmpty(userRequest.username))
            {
                result.success = false;
                result.message = "Username is required.";
                return result;
            }

            string sql = "select count(*) from Users where username = @username and id != @id";
            int count = await _dapperContext.GetSingleValueAsync<int>(sql, new { username = userRequest.username, id = userRequest.id });

            if (count > 0)
            {
                result.success = false;
                result.message = "Username already exists.";
                return result;
            }

            sql = "update Users set username = @username, admin = @admin, active = @active, name = @name where id = @id;";
            await _dapperContext.ExecuteAsync(sql, new { username = userRequest.username, admin = userRequest.admin, active = userRequest.active, name = userRequest.name, id = userRequest.id });
            result.success = true;
            result.message = "User updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing user {Username}", userRequest.username);
            result.success = false;
            result.message = $"Error editing user: {ex.Message}";
        }

        return result;
    }
}
