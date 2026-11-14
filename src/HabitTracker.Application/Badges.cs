namespace HabitTracker.Application;

public record Badge(string Name, string Emoji, string Description, bool Unlocked);

public static class BadgeService
{
    public static List<Badge> Evaluate(int longestStreak, int totalCheckIns) => new()
    {
        new("First step", "🌱", "Complete a habit for the first time", totalCheckIns >= 1),
        new("3-day streak", "🔥", "Reach a 3-day streak", longestStreak >= 3),
        new("Week warrior", "⚡", "Reach a 7-day streak", longestStreak >= 7),
        new("Monthly master", "🏆", "Reach a 30-day streak", longestStreak >= 30),
        new("Centurion", "💎", "100 check-ins in total", totalCheckIns >= 100),
    };
}
