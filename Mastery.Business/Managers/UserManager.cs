using Mastery.DataAccess;
using Mastery.Models.User;

namespace Mastery.Business.Managers;

public class UserManager
{
    private readonly SqlDbContext sqlDbContext;

    public UserManager(SqlDbContext sqlDbContext)
    {
        this.sqlDbContext = sqlDbContext;
    }

    public async Task<UserViewModel?> GetUserAsync(string id)
    {
        var user = await this.sqlDbContext.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        return new UserViewModel()
        {
            Id = user.Id,
            Username = user.Username,
        };
    }
}
