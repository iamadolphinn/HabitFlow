using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HabitTracker.Domain;

namespace HabitTracker.Infrastructure;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Habit>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Color).HasMaxLength(7);
            e.HasIndex(x => x.UserId);
            e.HasMany(x => x.CheckIns).WithOne().HasForeignKey(x => x.HabitId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CheckIn>(e =>
        {
            e.HasIndex(x => new { x.HabitId, x.Date }).IsUnique();
        });
    }
}
