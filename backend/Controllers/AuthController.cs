using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;
using RealEstateAI.Api.Services;

namespace RealEstateAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;

    public AuthController(UserManager<ApplicationUser> users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto input)
    {
        if (await _users.FindByEmailAsync(input.Email) is not null)
            return BadRequest(new { message = "Email is already registered." });

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FullName = input.FullName,
            AgencyName = input.AgencyName
        };

        var result = await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description).ToList() });

        await _users.AddToRoleAsync(user, "Agent");
        return Ok(await BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto input)
    {
        var user = await _users.FindByEmailAsync(input.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, input.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(await BuildAuthResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(await BuildAuthResponse(user));
    }

    private async Task<AuthResponseDto> BuildAuthResponse(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return new AuthResponseDto(_tokens.CreateToken(user, roles), user.Email!, user.FullName, roles.ToArray());
    }
}
