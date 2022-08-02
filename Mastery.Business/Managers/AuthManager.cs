using Mastery.DataAccess;
using Mastery.DataAccess.Entities;
using Mastery.Models.Auth;
using Mastery.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Mastery.Business.Managers;

public class AuthManager
{
    private readonly SqlDbContext sqlDbContext;
    private readonly string jwtSecret;

    public AuthManager(SqlDbContext sqlDbContext, IConfiguration configuration)
    {
        this.sqlDbContext = sqlDbContext;
        this.jwtSecret = configuration["ApiAuthSecret"];
    }

    public async Task<string?> RegisterAsync(RegisterBindingModel register)
    {
        var usernameExists = await this.sqlDbContext.Users.AnyAsync(x => x.Username == register.Username);

        if (usernameExists)
        {
            return null;
        }

        var password = HashPassword(register.Password);

        var user = new User()
        {
            Id = Guid.NewGuid().ToString(),
            Username = register.Username,
            PasswordHash = password.hash,
            PasswordSalt = password.salt,
        };

        await this.sqlDbContext.Users.AddAsync(user);

        await this.sqlDbContext.SaveChangesAsync();

        var token = MakeToken(user, false);

        return token;
    }

    public async Task<string?> LoginAsync(LoginBindingModel login)
    {
        var user = await this.sqlDbContext.Users.SingleOrDefaultAsync(x => x.Username == login.Username);

        if (user == null)
        {
            return null;
        }

        var (hash, _) = HashPassword(login.Password, user.PasswordSalt);

        if (!hash.SequenceEqual(user.PasswordHash))
        {
            return null;
        }

        var token = MakeToken(user, login.RememberMe);

        return token;
    }

    private (byte[] hash, byte[] salt) HashPassword(string plainPassword, byte[]? salt = null)
    {
        var passwordBytes = Encoding.ASCII.GetBytes(plainPassword);
        salt ??= RandomNumberGenerator.GetBytes(36);

        using var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations: 1000);

        var hash = deriveBytes.GetBytes(36);

        return (hash, salt);
    }

    private string MakeToken(User model, bool remember)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, model.Id),
        };

        return EncodeToken(claims, remember);
    }

    private string EncodeToken(Claim[] claims, bool rememberMe)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(this.jwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = rememberMe
                ? DateTime.UtcNow.AddYears(10)
                : DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
