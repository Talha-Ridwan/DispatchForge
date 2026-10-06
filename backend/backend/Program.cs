using System.Text;
using backend.Mappers;
using backend.Data;
using backend.Repositories;
using backend.Utils;
using backend.Entities;
using backend.Handlers;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using backend.Settings;
using Microsoft.AspNetCore.Diagnostics;
using backend.BackgroundJob;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddAuthorization();
//User
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<JwtUtil>();

//Tenants
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<TenantMapper>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();

//EventTypes
builder.Services.AddScoped<IEventTypeService, EventTypeService>();
builder.Services.AddScoped<EventTypeMapper>();
builder.Services.AddScoped<IEventTypeRepository, EventTypeRepository>();

//Destinations
builder.Services.AddScoped<IDestinationService, DestinationService>();
builder.Services.AddScoped<DestinationMapper>();
builder.Services.AddScoped<IDestinationRepository, DestinationRepository>();

//Janitor crew
builder.Services.AddHostedService<CleanupService>();

var jwtSection = builder.Configuration.GetSection("Jwt"); // the "Jwt:*" keys from all config sources
builder.Services.Configure<JwtSettings>(jwtSection); // DI binds this to JwtSettings on demand, for IOptions<JwtSettings>

var jwtSettings = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' section in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers().RequireAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();



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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}