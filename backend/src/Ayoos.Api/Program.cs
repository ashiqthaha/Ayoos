using Ayoos.Api.Endpoints;
using Ayoos.Api.ErrorHandling;
using Ayoos.Application;
using Ayoos.Infrastructure;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Ayoos API",
        Version = "v1",
        Description = "Backend API for tenant-isolated clinic practices."
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyAyoosMigrationsAsync();
}

app.UseExceptionHandler();
app.UseMultiTenant();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");

app.MapGet("/", () => Results.Ok(new
{
    name = "Ayoos API",
    status = "ready"
}));
app.MapHealthChecks("/health");
app.MapPracticeEndpoints();
app.MapProviderEndpoints();

app.Run();
