using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HabitTracker.Application;
using HabitTracker.Domain;
using HabitTracker.Infrastructure;
using HabitTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();   // uses a SignalR circuit per browser tab
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddSingleton<IHabitEvents, HabitEvents>();   // cross-tab / cross-device live sync
builder.Services.AddScoped<HabitState>();                      // state container (one per circuit)
builder.Services.AddScoped<CurrentUser>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.User.RequireUniqueEmail = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/login";
    o.AccessDeniedPath = "/login";
    o.Cookie.HttpOnly = true;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ---- auth endpoints (plain form posts: cookies need a real HTTP response, which a Blazor circuit can't give) ----
static string SafeLocal(string? url) =>
    !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\") ? url : "/";

app.MapPost("/auth/login", async ([FromForm] string email, [FromForm] string password,
    [FromForm] string? returnUrl, SignInManager<ApplicationUser> signIn) =>
{
    var result = await signIn.PasswordSignInAsync(email?.Trim() ?? "", password ?? "", isPersistent: true, lockoutOnFailure: true);
    return result.Succeeded
        ? Results.LocalRedirect(SafeLocal(returnUrl))
        : Results.Redirect("/login?error=1" + (returnUrl is null ? "" : "&returnUrl=" + Uri.EscapeDataString(returnUrl)));
}).DisableAntiforgery();

app.MapPost("/auth/register", async ([FromForm] string name, [FromForm] string email, [FromForm] string password,
    UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) =>
{
    // everyone who signs up is a normal user; admins can only be created by an admin
    var user = new ApplicationUser { UserName = email?.Trim(), Email = email?.Trim(), DisplayName = name?.Trim() ?? "" };
    var res = await users.CreateAsync(user, password ?? "");
    if (!res.Succeeded)
        return Results.Redirect("/register?error=" + Uri.EscapeDataString(res.Errors.First().Description));

    await users.AddToRoleAsync(user, Roles.User);
    await signIn.SignInAsync(user, isPersistent: true);
    return Results.LocalRedirect("/today");
}).DisableAntiforgery();

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.LocalRedirect("/");
}).DisableAntiforgery();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await DbSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program { }
