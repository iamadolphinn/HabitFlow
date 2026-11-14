using Microsoft.Playwright;
using Xunit;

namespace HabitTracker.Tests;

/// <summary>
/// End-to-end tests drive a real browser. They run only when E2E_BASE_URL is set, e.g.
///   dotnet run --project src/HabitTracker.Web          (listens on http://localhost:5000)
///   E2E_BASE_URL=http://localhost:5000 dotnet test --filter Category=E2E
/// (first time: pwsh tests/HabitTracker.Tests/bin/Debug/net8.0/playwright.ps1 install chromium)
/// </summary>
[Trait("Category", "E2E")]
public class EndToEndTests
{
    private static readonly string? BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

    private static async Task<(IPlaywright pw, IBrowser browser, IPage page)> Open(bool mobile = false)
    {
        var pw = await Playwright.CreateAsync();
        var browser = await pw.Chromium.LaunchAsync();
        var ctx = await browser.NewContextAsync(mobile
            ? pw.Devices["Pixel 7"]
            : new BrowserNewContextOptions { ViewportSize = new() { Width = 1280, Height = 800 } });
        return (pw, browser, await ctx.NewPageAsync());
    }

    private static async Task Login(IPage page, string email, string password)
    {
        await page.GotoAsync($"{BaseUrl}/login");
        await page.FillAsync("#email", email);
        await page.FillAsync("#password", password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForURLAsync("**/today");
    }

    [Fact]
    public async Task User_Can_Create_A_Habit_And_Check_It_In()
    {
        if (BaseUrl is null) return; // skipped when no server is running

        var (pw, browser, page) = await Open();
        try
        {
            await Login(page, "demo@habit.local", "Demo123!");
            var name = "E2E habit " + Guid.NewGuid().ToString("N")[..6];

            await page.ClickAsync("text=+ New habit >> nth=0");
            await page.FillAsync("#name", name);
            await page.ClickAsync("text=Save");
            await page.WaitForURLAsync("**/habits");

            await page.GotoAsync($"{BaseUrl}/today");
            var card = page.Locator("[data-testid=habit-card]", new() { HasText = name });
            await card.Locator("[data-testid=toggle]").ClickAsync();

            await Assertions.Expect(card.Locator("[data-testid=toggle]")).ToHaveAttributeAsync("aria-pressed", "true");
        }
        finally { await browser.CloseAsync(); pw.Dispose(); }
    }

    [Fact]
    public async Task Normal_User_Cannot_Open_Admin_Page()
    {
        if (BaseUrl is null) return;

        var (pw, browser, page) = await Open();
        try
        {
            await Login(page, "demo@habit.local", "Demo123!");
            await page.GotoAsync($"{BaseUrl}/admin/users");
            Assert.Contains("permission", await page.TextContentAsync("body"));
        }
        finally { await browser.CloseAsync(); pw.Dispose(); }
    }

    [Fact]
    public async Task Today_Page_Is_Usable_On_A_Phone_Screen()
    {
        if (BaseUrl is null) return;

        var (pw, browser, page) = await Open(mobile: true);
        try
        {
            await Login(page, "demo@habit.local", "Demo123!");
            await page.WaitForSelectorAsync("[data-testid=habit-card]");

            var overflow = await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth > document.documentElement.clientWidth");
            Assert.False(overflow);
        }
        finally { await browser.CloseAsync(); pw.Dispose(); }
    }
}
