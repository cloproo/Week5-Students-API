using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StudentApi.Data;
using StudentApi.Models;
using StudentApi.Options;
using StudentApi.Services;
using StudentApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

// SQLite for local development; PostgreSQL is REQUIRED for online/Production deployment.
var postgres = builder.Configuration.GetConnectionString("Postgres");
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(postgres))
{
    throw new InvalidOperationException(
        "Production deployment requires ConnectionStrings__Postgres (persistent database).");
}
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgres))
        options.UseNpgsql(postgres);
    else
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
            ?? "Data Source=students.db");
});

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt__Key must contain at least 32 UTF-8 bytes. Set it via an environment variable or user secrets.");
if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
if (jwt.ExpiryMinutes is < 1 or > 1440)
    throw new InvalidOperationException("Jwt:ExpiryMinutes must be 1..1440.");

builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
        config.AutoReplenishment = true;
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Students Management API - Week 5",
        Version = "v1",
        Description = "JWT authentication, student CRUD, search, filters and pagination"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the JWT token (Swagger sends the Bearer prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.OperationFilter<JwtAuthorizeOperationFilter>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler();

// Required by assignment: Swagger remains available on the deployed server.
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Students API v1"));

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem("Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .ExcludeFromDescription();

// This self-contained academic application creates its initial schema automatically.
// For versioned production schema changes, replace EnsureCreated with EF migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();

public partial class Program { }
