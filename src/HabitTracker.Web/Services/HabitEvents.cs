using HabitTracker.Application;

namespace HabitTracker.Web.Services;

/// <summary>Singleton publish/subscribe. Every open circuit of a user subscribes, so a check-in on the phone shows up on the laptop instantly.</summary>
public class HabitEvents : IHabitEvents
{
    public event Action<string>? HabitsChanged;

    public void Publish(string userId)
    {
        var handlers = HabitsChanged;
        if (handlers is null) return;

        foreach (var h in handlers.GetInvocationList().Cast<Action<string>>())
        {
            try { h(userId); }
            catch (Exception) { /* a broken/disposed subscriber must never break the publisher */ }
        }
    }
}
