using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HabitTracker.Domain;

namespace HabitTracker.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles.All)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUser(users, "admin@habit.local", "Admin", "Admin123!", Roles.Admin);
        var demo = await EnsureUser(users, "demo@habit.local", "Demo User", "Demo123!", Roles.User);

        if (!await db.Habits.AnyAsync(h => h.UserId == demo.Id))
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var created = DateTime.Now.AddDays(-60);

            var habits = new[]
            {
                new Habit { UserId = demo.Id, Name = "Drink water", Emoji = "💧", Color = "#0ea5e9", Description = "8 glasses a day", CreatedAt = created },
                new Habit { UserId = demo.Id, Name = "Read 20 pages", Emoji = "📚", Color = "#8b5cf6", Description = "Any book", CreatedAt = created },
                new Habit { UserId = demo.Id, Name = "Workout", Emoji = "🏋️", Color = "#ef4444", ScheduledDays = Weekdays.Mon | Weekdays.Wed | Weekdays.Fri, CreatedAt = created },
                new Habit { UserId = demo.Id, Name = "Practice coding", Emoji = "💻", Color = "#22c55e", ScheduledDays = Weekdays.Workdays, CreatedAt = created },
            };
            db.Habits.AddRange(habits);
            await db.SaveChangesAsync();

            // deterministic demo history (so charts and streaks look alive)
            var rnd = new Random(42);
            foreach (var h in habits)
            {
                for (var i = 59; i >= 1; i--)
                {
                    var d = today.AddDays(-i);
                    var chance = h.Name == "Drink water" ? 0.85 : 0.65;
                    if (h.IsScheduledOn(d) && rnd.NextDouble() < chance)
                        db.CheckIns.Add(new CheckIn { HabitId = h.Id, Date = d });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUser(UserManager<ApplicationUser> users, string email,
        string name, string password, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, DisplayName = name, EmailConfirmed = true };
            var res = await users.CreateAsync(user, password);
            if (!res.Succeeded) throw new InvalidOperationException(string.Join("; ", res.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, role);
        }
        return user;
    }
}
