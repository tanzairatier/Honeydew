using System.Text;
using Honeydew.Controllers.Models;
using Honeydew.Data;
using Honeydew.Middleware;
using Honeydew.Options;
using Honeydew.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = actionContext =>
        {
            var firstError = actionContext.ModelState
                .SelectMany(s => s.Value?.Errors ?? [])
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m));
            var message = !string.IsNullOrEmpty(firstError) ? firstError : "Validation failed.";
            return new BadRequestObjectResult(ApiErrorResponse.Validation(message));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=honeydew.db";

builder.Services.AddDbContext<HoneydewDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtService, JwtService>();

// Data access
builder.Services.AddScoped<AuthDataAccess>();
builder.Services.AddScoped<TenantDataAccess>();
builder.Services.AddScoped<UsersDataAccess>();
builder.Services.AddScoped<TodoDataAccess>();
builder.Services.AddScoped<SupportTicketsDataAccess>();
builder.Services.AddScoped<UserPreferencesDataAccess>();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<SupportTicketsService>();
builder.Services.AddScoped<UserPreferencesService>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
var keyBytes = Encoding.UTF8.GetBytes(signingKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

// Run pending EF Core migrations on startup: opens the DB, reads __EFMigrationsHistory,
// compares with migrations in the assembly, and applies any that haven't run yet.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<HoneydewDbContext>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        MigrationConnectionProvider.SetConnection(db.Database.GetDbConnection());
        try
        {
            await db.Database.MigrateAsync();
        }
        finally
        {
            MigrationConnectionProvider.SetConnection(null);
        }
        logger.LogInformation("Migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed. Fix the error and restart the API.");
        throw;
    }
    if (app.Environment.IsDevelopment())
    {
        await SeedData.SeedDevIfEmpty(db);
    }
    await SeedData.SeedBillingPlansIfEmptyAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
// Skip HTTPS redirect in Development so the Vite proxy (HTTP → backend) doesn't get 307;
// following that redirect would drop the Authorization header and cause 401.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
