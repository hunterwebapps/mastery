using Mastery.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mastery.DataAccess;

public class SqlDbContext : DbContext
{
    public SqlDbContext(DbContextOptions<SqlDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Quest> Quests { get; set; } = default!;
    public DbSet<Event> Events { get; set; } = default!;
    public DbSet<EventType> EventTypes { get; set; } = default!;
    public DbSet<Decision> Decisions { get; set; } = default!;
    public DbSet<ActivityType> Activities { get; set; } = default!;
    public DbSet<Activity> DecisionActivities { get; set; } = default!;
    public DbSet<Skill> Skills { get; set; } = default!;
}
