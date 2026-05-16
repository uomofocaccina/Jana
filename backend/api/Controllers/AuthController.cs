using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JanaApi.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IUsersService _usersService;
    private readonly IJwtHelper _jwtHelper;

    public AuthController(ILogger<AuthController> logger, IUsersService usersService, IJwtHelper jwtHelper)
    {
        _logger = logger;
        _usersService = usersService;
        _jwtHelper = jwtHelper;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        LoginResult result = new LoginResult();

        if (request == null || string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.password))
        {
            result.success = false;
            result.message = "Username and password are required.";
            return BadRequest(result);
        }

        try
        {
            var user = await _usersService.AuthenticateAsync(request.username, request.password);
            
            if (user == null)
            {
                result.success = false;
                result.message = "Invalid username or password.";
                return Unauthorized(result);
            }

            // Create JWT claims
            var claims = new[]
            {
                new Claim("username", user.username),
                new Claim("user_id", user.id.ToString()),
                new Claim("admin", (user.admin == 1).ToString().ToLower())
            };

            var token = _jwtHelper.GetJwtToken(claims);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            result.success = true;
            result.token = tokenString;
            result.admin = user.admin == 1;

            _logger.LogInformation("User {Username} logged in successfully", user.username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.username);
            result.success = false;
            result.message = "An error occurred during login.";
            return StatusCode(500, result);
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        Result result = new Result();

        if (request == null || string.IsNullOrEmpty(request.oldPassword) || string.IsNullOrEmpty(request.newPassword))
        {
            result.success = false;
            result.message = "Old password and new password are required.";
            return BadRequest(result);
        }

        try
        {
            int userId = JwtClaimsHelper.GetUserId(User);
            if (userId == -1)
            {
                result.success = false;
                result.message = "Invalid user token.";
                return Unauthorized(result);
            }

            bool success = await _usersService.ChangePasswordAsync(userId, request.oldPassword, request.newPassword);
            
            if (success)
            {
                result.success = true;
                result.message = "Password changed successfully.";
                _logger.LogInformation("Password changed for user {UserId}", userId);
                return Ok(result);
            }
            else
            {
                result.success = false;
                result.message = "Old password is incorrect.";
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            result.success = false;
            result.message = "An error occurred while changing password.";
            return StatusCode(500, result);
        }
    }
}