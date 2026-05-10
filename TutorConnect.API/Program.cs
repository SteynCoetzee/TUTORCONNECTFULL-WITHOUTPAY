using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TutorConnect.API.Data;
using TutorConnect.API.Models;
using TutorConnect.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS: allow the Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5149", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add services to the container.
builder.Services.AddControllers(options =>
    {
        // Remove text/plain formatter so string returns are always JSON-encoded.
        // Without this, Ok("message") returns text/plain which Angular's JSON
        // HttpClient can't parse, triggering false error callbacks.
        options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── Seed roles + hardcoded admin account ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Seed User_Roles if the table is empty (fresh database)
    if (!db.User_Roles.Any())
    {
        db.User_Roles.AddRange(
            new User_Role { User_Role_Name = "Admin" },
            new User_Role { User_Role_Name = "Tutor" },
            new User_Role { User_Role_Name = "Student" },
            new User_Role { User_Role_Name = "AW-Tutor" }
        );
        db.SaveChanges();
    }

    // Seed hardcoded admin user
    const string adminEmail = "TutorConnect00@gmail.com";
    if (!db.Users.Any(u => u.Email == adminEmail))
    {
        var adminRoleId = db.User_Roles.First(r => r.User_Role_Name == "Admin").User_Role_ID;
        db.Users.Add(new User
        {
            FirstName    = "ADMIN",
            LastName     = "ADMIN",
            Email        = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TutorConnect123!"),
            User_Role_ID = adminRoleId,
            Phone        = "ADMIN",
            Address      = "ADMIN",
            Bio          = "System administrator account."
        });
        db.SaveChanges();
    }
}
// ─────────────────────────────────────────────────────────────────────────────

app.Run();
