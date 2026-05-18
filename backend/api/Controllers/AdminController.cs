using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JanaApi.Controllers;
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly IUsersService _users;

    public AdminController(ILogger<ApiController> logger, IUsersService users)
    {
        _logger = logger;
        _users = users;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() //pagination not needed
    {
        ResultUser result = new ResultUser();

        if (!JwtClaimsHelper.IsAdmin(User) || JwtClaimsHelper.GetUserId(User) == -1)
        {
            result.success = false;
            result.message = "Unauthorized access. Admin privileges required.";
            return Unauthorized(result);
        }

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
            result.message = $"Error retrieving Users: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok();
    }

    [HttpPost("users")]
    public async Task<IActionResult> AddUsers(User userRequest) 
    {
        ResultAdminAddUser result = new ResultAdminAddUser();

        if (!JwtClaimsHelper.IsAdmin(User) || JwtClaimsHelper.GetUserId(User) == -1)
        {
            result.success = false;
            result.message = "Unauthorized access. Admin privileges required.";
            return Unauthorized(result);
        }

        try
        {
            var data = await _users.AddUserAsync(userRequest);
            result= data;
            result.success = true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding User {UserName}", userRequest.name);
            result.success = false;
            result.message = $"Error adding User: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok();
    }

    [HttpPut("users")]
    public async Task<IActionResult> UpdateUsers(UserMinimal userRequest)
    {
        Result result = new Result();

        if (!JwtClaimsHelper.IsAdmin(User) || JwtClaimsHelper.GetUserId(User) == -1)
        {
            result.success = false;
            result.message = "Unauthorized access. Admin privileges required.";
            return Unauthorized(result);
        }

        try
        {
            var data = await _users.EditUserAsync(userRequest);
            result = data;
            result.success = true;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing User {UserName}", userRequest.name);
            result.success = false;
            result.message = $"Error editing User: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok();
    }

    [HttpPut("users/password")]
    public async Task<IActionResult> ChangeUserPassword(ChangePasswordAdminRequest request)
    {
        Result result = new Result();

        if (!JwtClaimsHelper.IsAdmin(User) || JwtClaimsHelper.GetUserId(User) == -1)
        {
            result.success = false;
            result.message = "Unauthorized access. Admin privileges required.";
            return Unauthorized(result);
        }

        try
        {
            result.success = await _users.ChangePasswordAsync(request.id, request.password);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for User {UserId}", request.id);
            result.success = false;
            result.message = $"Error changing password: {ex.StackTrace}";
            return StatusCode(500, result);
        }

        return Ok();
    }
}