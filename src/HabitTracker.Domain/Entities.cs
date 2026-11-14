namespace HabitTracker.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public static readonly string[] All = { Admin, User };
}

[Flags]
public enum Weekdays
{
    None = 0,
    Mon = 1, Tue = 2, Wed = 4, Thu = 8, Fri = 16, Sat = 32, Sun = 64,
    Workdays = Mon | Tue | Wed | Thu | Fri,
    Everyday = Workdays | Sat | Sun
}

public static class WeekdaysExtensions
{
    public static Weekdays From(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekdays.Mon,
        DayOfWeek.Tuesday => Weekdays.Tue,
        DayOfWeek.Wednesday => Weekdays.Wed,
        DayOfWeek.Thursday => Weekdays.Thu,
        DayOfWeek.Friday => Weekdays.Fri,
        DayOfWeek.Saturday => Weekdays.Sat,
        _ => Weekdays.Sun
    };
}

public class Habit
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Emoji { get; set; } = "✅";
    public string Color { get; set; } = "#4f46e5";
    public Weekdays ScheduledDays { get; set; } = Weekdays.Everyday;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsArchived { get; set; }
    public List<CheckIn> CheckIns { get; set; } = new();

    public bool IsScheduledOn(DateOnly date) =>
        (ScheduledDays & WeekdaysExtensions.From(date.DayOfWeek)) != 0;
}

public class CheckIn
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public DateOnly Date { get; set; }
}
