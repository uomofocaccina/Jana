using JanaApi.Model;
using JanaApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JanaApi.Controllers;
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IUsersService _users;

    public AdminController(ILogger<AdminController> logger, IUsersService users)
    {
        _logger = logger;
        _users = users;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() //pagination not needed
    {
        ResultUser result = new ResultUser();
        try
        {
            var data = await _users.GetAllUsersAsync();
            result.data = data;
            result.success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Users");
            result.success = false;
            result.message = $"Error retrieving Users: {ex.Message}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPost("users")]
    public async Task<IActionResult> AddUsers(User userRequest)
    {
        ResultAdminAddUser result = new ResultAdminAddUser();
        try
        {
            var data = await _users.AddUserAsync(userRequest);
            result = data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding User {UserName}", userRequest.name);
            result.success = false;
            result.message = $"Error adding User: {ex.Message}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPut("users")]
    public async Task<IActionResult> UpdateUsers(UserMinimal userRequest)
    {
        Result result = new Result();
        try
        {
            var data = await _users.EditUserAsync(userRequest);
            result = data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing User {UserName}", userRequest.name);
            result.success = false;
            result.message = $"Error editing User: {ex.Message}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }

    [HttpPut("users/password")]
    public async Task<IActionResult> ChangeUserPassword(ChangePasswordAdminRequest request)
    {
        Result result = new Result();
        if (string.IsNullOrEmpty(request.password) || request.password.Length < 6)
        {
            result.success = false;
            result.message = "Password must be at least 6 characters long.";
            return BadRequest(result);
        }

        try
        {
            result.success = await _users.ChangePasswordAsync(request.id, request.password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for User {UserId}", request.id);
            result.success = false;
            result.message = $"Error changing password: {ex.Message}";
            return StatusCode(500, result);
        }

        return Ok(result);
    }
}
