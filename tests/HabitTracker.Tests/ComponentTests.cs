using Bunit;
using HabitTracker.Application;
using HabitTracker.Domain;
using HabitTracker.Web.Components;
using Xunit;

namespace HabitTracker.Tests;

/// <summary>Blazor component tests with bUnit (no browser needed).</summary>
public class ComponentTests : TestContext
{
    private static Habit Water() => new() { Id = 1, Name = "Drink water", Emoji = "💧", Color = "#0ea5e9", Description = "8 glasses" };

    [Fact]
    public void HabitCard_Shows_Name_And_Streak()
    {
        var cut = RenderComponent<HabitCard>(p => p.Add(c => c.Habit, Water()).Add(c => c.Streak, 5));

        Assert.Contains("Drink water", cut.Markup);
        Assert.Contains("5 days", cut.Markup);
    }

    [Fact]
    public void HabitCard_Singular_Streak_Text()
    {
        var cut = RenderComponent<HabitCard>(p => p.Add(c => c.Habit, Water()).Add(c => c.Streak, 1));
        Assert.Contains("1 day", cut.Markup);
        Assert.DoesNotContain("1 days", cut.Markup);
    }

    [Fact]
    public void HabitCard_Click_Raises_OnToggle()
    {
        var clicks = 0;
        var cut = RenderComponent<HabitCard>(p => p
            .Add(c => c.Habit, Water())
            .Add(c => c.OnToggle, () => { clicks++; }));

        cut.Find("[data-testid=toggle]").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void HabitCard_Done_State_Is_Marked_For_Accessibility()
    {
        var cut = RenderComponent<HabitCard>(p => p.Add(c => c.Habit, Water()).Add(c => c.Done, true));

        Assert.Equal("true", cut.Find("[data-testid=toggle]").GetAttribute("aria-pressed"));
        Assert.Contains("checked", cut.Find("[data-testid=toggle]").ClassList);
    }

    [Fact]
    public void WeekdayPicker_Renders_Seven_Days_With_Selection()
    {
        var cut = RenderComponent<WeekdayPicker>(p => p.Add(c => c.Value, Weekdays.Mon | Weekdays.Fri));

        var days = cut.FindAll("[data-testid=day]");
        Assert.Equal(7, days.Count);
        Assert.Equal("true", days[0].GetAttribute("aria-pressed")); // Mon
        Assert.Equal("false", days[1].GetAttribute("aria-pressed")); // Tue
        Assert.Equal("true", days[4].GetAttribute("aria-pressed")); // Fri
    }

    [Fact]
    public void WeekdayPicker_Click_Toggles_The_Day()
    {
        Weekdays? value = null;
        var cut = RenderComponent<WeekdayPicker>(p => p
            .Add(c => c.Value, Weekdays.Mon)
            .Add(c => c.ValueChanged, (Weekdays v) => value = v));

        cut.FindAll("[data-testid=day]")[2].Click(); // Wed

        Assert.Equal(Weekdays.Mon | Weekdays.Wed, value);
    }

    [Fact]
    public void WeekdayPicker_Presets_Set_Days()
    {
        Weekdays? value = null;
        var cut = RenderComponent<WeekdayPicker>(p => p
            .Add(c => c.Value, Weekdays.Mon)
            .Add(c => c.ValueChanged, (Weekdays v) => value = v));

        cut.FindAll("button.btn-link")[1].Click(); // Weekdays

        Assert.Equal(Weekdays.Workdays, value);
    }

    [Fact]
    public void Heatmap_Renders_A_Cell_Per_Day_And_Pads_To_Monday()
    {
        // Wednesday 18 Nov 2026 → 2 empty cells before it (Mon, Tue)
        var cells = new[]
        {
            new Heatmap.HeatCell(new DateOnly(2026, 11, 18), 4, "Wed"),
            new Heatmap.HeatCell(new DateOnly(2026, 11, 19), 0, "Thu")
        };

        var cut = RenderComponent<Heatmap>(p => p.Add(c => c.Cells, cells));

        Assert.Equal(2, cut.FindAll("[data-testid=heat-cell]").Count);
        Assert.Equal(2, cut.FindAll(".heat-cell.empty").Count);
        Assert.Contains("level-4", cut.FindAll("[data-testid=heat-cell]")[0].ClassList);
    }

    [Fact]
    public void Heatmap_Click_Reports_The_Date_When_Clickable()
    {
        DateOnly? clicked = null;
        var cells = new[] { new Heatmap.HeatCell(new DateOnly(2026, 11, 16), 0, "Mon") };
        var cut = RenderComponent<Heatmap>(p => p
            .Add(c => c.Cells, cells)
            .Add(c => c.Clickable, true)
            .Add(c => c.OnCellClick, (DateOnly d) => clicked = d));

        cut.Find("[data-testid=heat-cell]").Click();

        Assert.Equal(new DateOnly(2026, 11, 16), clicked);
    }

    [Fact]
    public void Heatmap_Is_Read_Only_By_Default()
    {
        var cells = new[] { new Heatmap.HeatCell(new DateOnly(2026, 11, 16), 0, "Mon") };
        var cut = RenderComponent<Heatmap>(p => p.Add(c => c.Cells, cells));

        Assert.True(cut.Find("[data-testid=heat-cell]").HasAttribute("disabled"));
    }

    [Fact]
    public void BadgeList_Marks_Locked_And_Unlocked()
    {
        var cut = RenderComponent<BadgeList>(p => p.Add(c => c.Badges, BadgeService.Evaluate(3, 3)));

        var tiles = cut.FindAll("[data-testid=badge]");
        Assert.Equal(5, tiles.Count);
        Assert.Equal(2, tiles.Count(t => t.ClassList.Contains("unlocked")));
    }

    [Fact]
    public void BarChart_Renders_A_Row_Per_Item_And_Clamps_Width()
    {
        var cut = RenderComponent<BarChart>(p => p.Add(c => c.Items, new[]
        {
            new BarChart.ChartItem("Mon", 50),
            new BarChart.ChartItem("Tue", 150)
        }));

        Assert.Equal(2, cut.FindAll(".bar-row").Count);
        Assert.Contains("width:100%", cut.FindAll(".bar-fill")[1].GetAttribute("style"));
    }
}
