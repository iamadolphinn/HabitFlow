using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HabitTracker.Application;

namespace HabitTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default") ?? "Data Source=habits.db";

        // Factory (for repositories) + scoped context (for ASP.NET Core Identity)
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(cs), ServiceLifetime.Scoped);
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddScoped<IHabitRepository, HabitRepository>();
        return services;
    }
}
