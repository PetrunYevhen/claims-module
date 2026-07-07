using System.Security.Claims;
using ClaimsModule.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(JwtTokenGenerator tokenGenerator) : ControllerBase
{
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, Guid UserId, string Name, string Role);

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = MockUsers.FindByCredentials(request.Username, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = tokenGenerator.GenerateToken(user);
        return Ok(new LoginResponse(token, user.UserId, user.Name, user.Role));
    }

    /// <summary>Lets a frontend role switcher list the mock accounts without exposing passwords.</summary>
    [AllowAnonymous]
    [HttpGet("users")]
    public ActionResult<IEnumerable<object>> ListMockUsers()
    {
        return Ok(MockUsers.All.Select(u => new { u.Username, u.Name, u.Role }));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            name = User.FindFirstValue(ClaimTypes.Name),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
