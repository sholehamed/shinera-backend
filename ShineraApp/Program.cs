using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Application.SharedKernel;
using ShineraApp.Application.Features.Plans;
using ShineraApp.Infrastructure;
#if APP_MONITORING
using AppMonitoring;
#endif
using Microsoft.AspNetCore.Authentication.Cookies;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCustomCqrs(typeof(GetPublicPlansQuery).Assembly);
builder.Services.AddPlanCatalog(builder.Configuration);
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("registration", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Monitoring", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("monitoring", "read")));

#if APP_MONITORING
builder.Services.AddAppMonitoring(options =>
{
    options.ApplicationName = "My.Api";
    options.SlowRequestThreshold = TimeSpan.FromSeconds(1);
});
#endif
var app = builder.Build();
app.MapEndpoints("api");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
#if APP_MONITORING
app.UseAppMonitoring();

app.MapAppMonitoring("/monitoring");
#endif

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapScalarApiReference("apis");
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }
