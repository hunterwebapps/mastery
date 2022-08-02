using Mastery.API.Extensions;
using Mastery.Business.Managers;
using Mastery.Models.Auth;
using Mastery.Models.User;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.API.Controllers;
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthManager authManager;
    private readonly UserManager userManager;

    public AuthController(AuthManager authManager, UserManager userManager)
    {
        this.authManager = authManager;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<UserViewModel>> GetMe()
    {
        var userId = this.User.GetUserId();;
        var user = await this.userManager.GetUserAsync(userId);

        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterBindingModel register)
    {
        var token = await this.authManager.RegisterAsync(register);

        SetAuthCookie(Response.Cookies, token);

        // TODO: Change to 201?
        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserViewModel>> Login(LoginBindingModel login)
    {
        var token = await this.authManager.LoginAsync(login);

        SetAuthCookie(Response.Cookies, token);

        return Ok();
    }

    private void SetAuthCookie(IResponseCookies cookies, string token)
    {
        cookies.Append(
            "mastery_jwt",
            token,
            new CookieOptions()
            {
                Path = "/",
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                Expires = DateTime.Now.AddYears(10),
            });
    }
}
