using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using HabitTracker.Application;
using HabitTracker.Domain;
using HabitTracker.Infrastructure;
using HabitTracker.Web.Services;
using Xunit;

namespace HabitTracker.Tests;

/// <summary>Integration tests: real EF Core + in-memory SQLite + real service, fake clock.</summary>
public class HabitServiceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly FakeClock _clock = new(new DateOnly(2026, 11, 16)); // Monday
    private readonly HabitEvents _events = new();
    private readonly HabitService _sut;

    private class FakeClock : IClock
    {
        public FakeClock(DateOnly today) => Today = today;
        public DateOnly Today { get; set; }
    }

    private class TestFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestFactory(SqliteConnection conn) =>
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        public AppDbContext CreateDbContext() => new(_options);
    }

    public HabitServiceIntegrationTests()
    {
        _conn.Open();
        var factory = new TestFactory(_conn);
        using (var db = factory.CreateDbContext()) db.Database.EnsureCreated();
        _sut = new HabitService(new HabitRepository(factory), _clock, _events);
    }

    private static Habit NewHabit(string name = "Read") => new()
    {
        Name = name, Emoji = "📚", Color = "#112233", ScheduledDays = Weekdays.Everyday
    };

    [Fact]
    public async Task Save_Creates_Habit_For_User()
    {
        var saved = await _sut.SaveAsync(NewHabit(), "u1");
        var all = await _sut.GetAllAsync("u1");

        Assert.True(saved.Id > 0);
        Assert.Single(all);
        Assert.Equal("Read", all[0].Name);
        Assert.Empty(await _sut.GetAllAsync("u2"));
    }

    [Fact]
    public async Task Toggle_Adds_Then_Removes_Check_In()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");

        Assert.True(await _sut.ToggleAsync(h.Id, "u1", _clock.Today));
        Assert.True((await _sut.GetTodayAsync("u1")).Single().DoneToday);

        Assert.False(await _sut.ToggleAsync(h.Id, "u1", _clock.Today));
        Assert.False((await _sut.GetTodayAsync("u1")).Single().DoneToday);
    }

    [Fact]
    public async Task Toggle_Rejects_Future_And_Too_Old_Dates()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");

        await Assert.ThrowsAsync<DomainException>(() => _sut.ToggleAsync(h.Id, "u1", _clock.Today.AddDays(1)));
        await Assert.ThrowsAsync<DomainException>(() =>
            _sut.ToggleAsync(h.Id, "u1", _clock.Today.AddDays(-(HabitService.MaxBackfillDays + 1))));
    }

    [Fact]
    public async Task Other_Users_Cannot_Touch_My_Habit()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");

        await Assert.ThrowsAsync<DomainException>(() => _sut.ToggleAsync(h.Id, "intruder", _clock.Today));
        await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteAsync(h.Id, "intruder"));
        await Assert.ThrowsAsync<DomainException>(() => _sut.SetArchivedAsync(h.Id, "intruder", true));
        Assert.Null(await _sut.GetDetailAsync(h.Id, "intruder"));
        Assert.Null(await _sut.GetForEditAsync(h.Id, "intruder"));
    }

    [Fact]
    public async Task Streak_Is_Computed_From_Stored_Check_Ins()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");
        foreach (var back in new[] { 0, 1, 2 })
            await _sut.ToggleAsync(h.Id, "u1", _clock.Today.AddDays(-back));

        var detail = await _sut.GetDetailAsync(h.Id, "u1");

        Assert.Equal(3, detail!.Streak.Current);
        Assert.Equal(3, detail.Streak.TotalCheckIns);
        Assert.Contains(detail.Badges, b => b.Name == "3-day streak" && b.Unlocked);
    }

    [Fact]
    public async Task Archived_Habits_Disappear_From_Today_But_Stay_In_List()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");
        await _sut.SetArchivedAsync(h.Id, "u1", true);

        Assert.Empty(await _sut.GetTodayAsync("u1"));
        Assert.Single(await _sut.GetAllAsync("u1"));
    }

    [Fact]
    public async Task Delete_Removes_Habit_And_Its_Check_Ins()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");
        await _sut.ToggleAsync(h.Id, "u1", _clock.Today);

        await _sut.DeleteAsync(h.Id, "u1");

        Assert.Empty(await _sut.GetAllAsync("u1"));
        Assert.Equal("Habit,Date\n", await _sut.ExportCsvAsync("u1"));
    }

    [Fact]
    public async Task Editing_Updates_Fields_Without_Losing_History()
    {
        var h = await _sut.SaveAsync(NewHabit(), "u1");
        await _sut.ToggleAsync(h.Id, "u1", _clock.Today);

        var edit = (await _sut.GetForEditAsync(h.Id, "u1"))!;
        edit.Name = "Read books";
        edit.ScheduledDays = Weekdays.Workdays;
        await _sut.SaveAsync(edit, "u1");

        var detail = (await _sut.GetDetailAsync(h.Id, "u1"))!;
        Assert.Equal("Read books", detail.Habit.Name);
        Assert.Equal(Weekdays.Workdays, detail.Habit.ScheduledDays);
        Assert.Equal(1, detail.Streak.TotalCheckIns);
    }

    [Fact]
    public async Task Overview_Summarises_Today_And_History()
    {
        var a = await _sut.SaveAsync(NewHabit("A"), "u1");
        await _sut.SaveAsync(NewHabit("B"), "u1");
        await _sut.ToggleAsync(a.Id, "u1", _clock.Today);

        var o = await _sut.GetOverviewAsync("u1");

        Assert.Equal(2, o.HabitCount);
        Assert.Equal(2, o.ScheduledToday);
        Assert.Equal(1, o.DoneToday);
        Assert.Equal(HabitService.HeatmapDays, o.Days.Count);
        Assert.Equal(_clock.Today, o.Days.Last().Date);
    }

    [Fact]
    public async Task Csv_Export_Contains_Check_Ins_And_Escapes_Formulas()
    {
        var h = await _sut.SaveAsync(NewHabit("=SUM(A1)"), "u1");
        await _sut.ToggleAsync(h.Id, "u1", _clock.Today);

        var csv = await _sut.ExportCsvAsync("u1");

        Assert.Contains("\"'=SUM(A1)\",2026-11-16", csv);
    }

    [Fact]
    public async Task Changes_Publish_Events_For_Live_Sync()
    {
        var received = new List<string>();
        _events.HabitsChanged += received.Add;

        var h = await _sut.SaveAsync(NewHabit(), "u1");
        await _sut.ToggleAsync(h.Id, "u1", _clock.Today);

        Assert.Equal(new[] { "u1", "u1" }, received);
    }

    public void Dispose() => _conn.Dispose();
}

public class HabitValidationTests
{
    private static Habit Valid() => new() { Name = "X", Emoji = "✅", Color = "#abcdef", ScheduledDays = Weekdays.Mon };

    [Fact] public void Valid_Habit_Passes() => HabitService.Validate(Valid());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Name_Fails(string name)
    {
        var h = Valid(); h.Name = name;
        Assert.Throws<DomainException>(() => HabitService.Validate(h));
    }

    [Fact]
    public void Too_Long_Name_Fails()
    {
        var h = Valid(); h.Name = new string('a', 81);
        Assert.Throws<DomainException>(() => HabitService.Validate(h));
    }

    [Fact]
    public void No_Days_Fails()
    {
        var h = Valid(); h.ScheduledDays = Weekdays.None;
        Assert.Throws<DomainException>(() => HabitService.Validate(h));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("javascript:alert(1)")]
    public void Bad_Color_Fails(string color)
    {
        var h = Valid(); h.Color = color;
        Assert.Throws<DomainException>(() => HabitService.Validate(h));
    }
}

public class HabitEventsTests
{
    [Fact]
    public void A_Throwing_Subscriber_Does_Not_Break_Others()
    {
        var events = new HabitEvents();
        var received = 0;
        events.HabitsChanged += _ => throw new InvalidOperationException("boom");
        events.HabitsChanged += _ => received++;

        events.Publish("u1");

        Assert.Equal(1, received);
    }
}
