using HabitTracker.Application;
using HabitTracker.Domain;
using Xunit;

namespace HabitTracker.Tests;

public class StreakCalculatorTests
{
    // Monday 16 Nov 2026
    private static readonly DateOnly Today = new(2026, 11, 16);

    private static Habit MakeHabit(Weekdays days, int createdDaysAgo, params int[] doneDaysAgo) => new()
    {
        ScheduledDays = days,
        CreatedAt = Today.AddDays(-createdDaysAgo).ToDateTime(TimeOnly.MinValue),
        CheckIns = doneDaysAgo.Select(n => new CheckIn { Date = Today.AddDays(-n) }).ToList()
    };

    [Fact]
    public void No_CheckIns_Gives_Zero()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10), Today);
        Assert.Equal(0, info.Current);
        Assert.Equal(0, info.Longest);
        Assert.Equal(0, info.TotalCheckIns);
    }

    [Fact]
    public void Consecutive_Days_Including_Today()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10, 0, 1, 2), Today);
        Assert.Equal(3, info.Current);
        Assert.Equal(3, info.Longest);
    }

    [Fact]
    public void Open_Today_Does_Not_Break_The_Streak()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10, 1, 2), Today);
        Assert.Equal(2, info.Current);
    }

    [Fact]
    public void Missed_Yesterday_Resets_Current_Streak()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10, 2, 3, 4), Today);
        Assert.Equal(0, info.Current);
        Assert.Equal(3, info.Longest);
    }

    [Fact]
    public void Gap_Splits_Streaks_And_Longest_Is_Kept()
    {
        // done 5,4,3 ago, missed 2 ago, done yesterday and today
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10, 5, 4, 3, 1, 0), Today);
        Assert.Equal(2, info.Current);
        Assert.Equal(3, info.Longest);
        Assert.Equal(5, info.TotalCheckIns);
    }

    [Fact]
    public void Rest_Days_Do_Not_Break_A_Weekday_Habit()
    {
        // Today is Monday; Thu 12 and Fri 13 were done, the weekend is a rest day, Monday done
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Workdays, 14, 0, 3, 4), Today);
        Assert.Equal(3, info.Current);
    }

    [Fact]
    public void Missing_A_Scheduled_Day_Breaks_A_Weekday_Habit()
    {
        // Monday done, Friday missed, Thursday done
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Workdays, 14, 0, 4), Today);
        Assert.Equal(1, info.Current);
    }

    [Fact]
    public void Habit_Created_Today_Starts_Clean()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 0), Today);
        Assert.Equal(0, info.Current);
        Assert.Equal(0, info.Rate30Days);
    }

    [Fact]
    public void Completion_Rate_Over_Last_30_Days()
    {
        // 10-day-old everyday habit, done on 5 of the last 9 closed days; today is open
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 10, 1, 2, 3, 4, 5), Today);
        // window = creation (10 days ago) .. yesterday = 10 scheduled days, 5 done
        Assert.Equal(50, info.Rate30Days);
    }

    [Fact]
    public void Doing_It_Today_Counts_In_The_Rate()
    {
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Everyday, 1, 0, 1), Today);
        Assert.Equal(100, info.Rate30Days);
    }

    [Fact]
    public void Bonus_Check_In_On_A_Rest_Day_Extends_The_Streak()
    {
        // weekday habit, also done on Sunday (rest day)
        var info = StreakCalculator.Calculate(MakeHabit(Weekdays.Workdays, 14, 0, 1, 3, 4), Today);
        Assert.Equal(4, info.Current);
    }
}

public class BadgeTests
{
    [Fact]
    public void Nothing_Unlocked_For_A_New_User() =>
        Assert.DoesNotContain(BadgeService.Evaluate(0, 0), b => b.Unlocked);

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(3, 3, 2)]
    [InlineData(7, 7, 3)]
    [InlineData(30, 30, 4)]
    [InlineData(30, 100, 5)]
    public void Badges_Unlock_At_Thresholds(int longest, int total, int expectedUnlocked) =>
        Assert.Equal(expectedUnlocked, BadgeService.Evaluate(longest, total).Count(b => b.Unlocked));
}
