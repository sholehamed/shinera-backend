using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.ClientGeneration;
using Web.SharedKernel;
using Web.SharedKernel.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBaseApiServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

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

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions { Predicate = _ => false });

app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
