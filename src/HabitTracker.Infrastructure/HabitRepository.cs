using Microsoft.EntityFrameworkCore;
using HabitTracker.Application;
using HabitTracker.Domain;

namespace HabitTracker.Infrastructure;

/// <summary>
/// One short-lived DbContext per call (IDbContextFactory): the recommended pattern for Blazor Server,
/// where a scoped context would live as long as the whole circuit.
/// </summary>
public class HabitRepository : IHabitRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public HabitRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Habit>> GetByUserAsync(string userId, bool includeArchived = false)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Habits.AsNoTracking()
            .Include(h => h.CheckIns)
            .Where(h => h.UserId == userId && (includeArchived || !h.IsArchived))
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<Habit?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Habits.AsNoTracking()
            .Include(h => h.CheckIns)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<Habit> AddAsync(Habit habit)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Habits.Add(habit);
        await db.SaveChangesAsync();
        return habit;
    }

    public async Task UpdateAsync(Habit habit)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var stored = await db.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id)
                     ?? throw new DomainException("Habit not found.");
        stored.Name = habit.Name;
        stored.Description = habit.Description ?? "";
        stored.Emoji = habit.Emoji;
        stored.Color = habit.Color;
        stored.ScheduledDays = habit.ScheduledDays;
        stored.IsArchived = habit.IsArchived;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.CheckIns.Where(c => c.HabitId == id).ExecuteDeleteAsync();
        await db.Habits.Where(h => h.Id == id).ExecuteDeleteAsync();
    }

    public async Task<bool> AddCheckInAsync(int habitId, DateOnly date)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if (await db.CheckIns.AnyAsync(c => c.HabitId == habitId && c.Date == date)) return false;
        db.CheckIns.Add(new CheckIn { HabitId = habitId, Date = date });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveCheckInAsync(int habitId, DateOnly date)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var n = await db.CheckIns.Where(c => c.HabitId == habitId && c.Date == date).ExecuteDeleteAsync();
        return n > 0;
    }

    public async Task<Dictionary<string, int>> CountByUserAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Habits.AsNoTracking()
            .GroupBy(h => h.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }
}
