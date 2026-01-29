namespace Mastery.Infrastructure.Identity;

public static class AppRoles
{
    public const string Super = "Super";
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = [Super, Admin, User];
}
