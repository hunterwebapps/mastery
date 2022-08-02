namespace Mastery.DataAccess.Entities;
public class User
{
    public string Id { get; set; } = default!;

    public string Username { get; set; } = default!;
    public byte[] PasswordHash { get; set; } = default!;
    public byte[] PasswordSalt { get; set; } = default!;
}
