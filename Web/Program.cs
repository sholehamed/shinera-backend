using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Modules.System.Identity;
using Modules.System.Subscription;
using Modules.System.Services;
using Modules.System.Workforce;
using Scalar.ClientGeneration;
using Web.SharedKernel;
using Web.SharedKernel.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBaseApiServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddIdentityModule(
    builder.Configuration,
    builder.Environment);
builder.Services.AddSubscriptionModule(builder.Configuration);
builder.Services.AddServicesModule(builder.Configuration);
builder.Services.AddWorkforceModule(builder.Configuration);

builder.Services.AddScalarClientGeneration();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapScalarWithClientGeneration();
app.UseHttpsRedirection();

app.UseIdentityModule(builder.Configuration);
app.UseSubscriptionModule(builder.Configuration);
app.UseServicesModule(builder.Configuration);
app.UseWorkforceModule(builder.Configuration);

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
