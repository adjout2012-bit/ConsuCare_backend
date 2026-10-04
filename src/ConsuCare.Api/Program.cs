using System.Text;
using System.Text.Json.Serialization;
using ConsuCare.Api.Data;
using ConsuCare.Api.Hubs;
using ConsuCare.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddSignalR();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSingleton<JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:Key in User Secrets or the environment.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ConsuCare";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            // Accept tokens issued before the product rename; new tokens use the ConsuCare issuer.
            ValidIssuers = [jwtIssuer, "MentorLink"],
            ValidateAudience = true,
            ValidAudiences = [jwtIssuer, "MentorLink"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Postgres (Supabase) when a connection string is configured; in-memory otherwise
// so the app runs out of the box. Set ConnectionStrings:DefaultConnection in
// appsettings.json (see README) to switch to Supabase.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
        options.UseInMemoryDatabase("ConsuCare");
    else
        options.UseNpgsql(connectionString);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    SeedData.Ensure(db);
}

app.UseRouting();
app.UseCors("VercelFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

// Exposes the entry point to WebApplicationFactory in tests/ConsuCare.Api.Tests.
public partial class Program { }
