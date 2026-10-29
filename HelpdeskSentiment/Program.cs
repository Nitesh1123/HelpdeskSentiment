using HelpdeskSentiment.Data;
using HelpdeskSentiment.Services;
using HelpdeskSentiment.Services.Sentiment;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HelpdeskDb")));

// Dependency Injection: the analyser has no state so one instance is shared,
// the call service uses the DbContext so it lives for one request.
builder.Services.AddSingleton<ISentimentAnalyzer, LexiconSentimentAnalyzer>();
builder.Services.AddScoped<ICallService, CallService>();

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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
