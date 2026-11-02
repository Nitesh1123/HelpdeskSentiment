using System.Text.Json.Serialization;
using HelpdeskSentiment.Authentication;
using HelpdeskSentiment.Data;
using HelpdeskSentiment.Middleware;
using HelpdeskSentiment.Services;
using HelpdeskSentiment.Services.Sentiment;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The database files live in the project's App_Data folder; |DataDirectory| in the connection string points here.
string dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HelpdeskDb")));

// Dependency Injection: the analyser has no state so one instance is shared,
// the call service uses the DbContext so it lives for one request.
builder.Services.AddSingleton<ISentimentAnalyzer, LexiconSentimentAnalyzer>();
builder.Services.AddScoped<ICallService, CallService>();

// Authentication: cookies for the MVC pages, HTTP Basic for the Web API.
builder.Services.AddSingleton<IUserStore, ConfigUserStore>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// Session state (used for the "recently viewed calls" list).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Apply migrations and add sample data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();
    db.Database.Migrate();
    await SeedData.SeedCallsAsync(db, scope.ServiceProvider.GetRequiredService<ICallService>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Middleware pipeline: order matters. Logging wraps everything after it,
// authentication must run before authorization, and both after routing.
app.UseRequestLogging();
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
