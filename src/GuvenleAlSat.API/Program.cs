using Amazon.Runtime;
using Amazon.S3;
using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.Concrete;
using GuvenleAlSat.Core.Utilities.Security.Jwt;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using JwtTokenOptions = GuvenleAlSat.Core.Utilities.Security.Jwt.TokenOptions;


var builder = WebApplication.CreateBuilder(args);

// 1. PostgreSQL DbContext
// 1. PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// 2. Identity Yapılandırması
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;

    // --- GÜVENLİK VE BLOKE AYARLARI ---
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 3; // 3 hatalı denemede kilitle
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(24); // Şifre sıfırlanana kadar kilitli kalsın
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Kimlik Doğrulama
var tokenOptions = new JwtTokenOptions
{
    Audience = builder.Configuration["TokenOptions:Audience"] ?? string.Empty,
    Issuer = builder.Configuration["TokenOptions:Issuer"] ?? string.Empty,
    AccessTokenExpirationMinutes = int.TryParse(builder.Configuration["TokenOptions:AccessTokenExpirationMinutes"], out var aExp) ? aExp : 1440,
    RefreshTokenExpirationDays = int.TryParse(builder.Configuration["TokenOptions:RefreshTokenExpirationDays"], out var rExp) ? rExp : 7,
    SecurityKey = builder.Configuration["TokenOptions:SecurityKey"] ?? throw new InvalidOperationException("SecurityKey bulunamadı.")
};

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = tokenOptions.Issuer,
        ValidAudience = tokenOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecurityKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// SignalR için WebSocket üzerinden Query String ile JWT okuma desteği
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'ların hem string ("TRY") hem sayı (0, 1) olarak JSON'dan okunabilmesini sağlar:
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 4. Cloudflare R2 / S3 İstemcisi
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = builder.Configuration.GetSection("CloudflareR2");
    var rawUrl = config["ServiceUrl"];

    if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl.Contains("<") || rawUrl.Contains(">") || !Uri.TryCreate(rawUrl, UriKind.Absolute, out _))
    {
        rawUrl = "https://dummy-account-id.r2.cloudflarestorage.com";
    }

    var accessKey = config["AccessKey"] ?? "dummy_access_key";
    var secretKey = config["SecretKey"] ?? "dummy_secret_key";

    var credentials = new BasicAWSCredentials(accessKey, secretKey);
    var s3Config = new AmazonS3Config
    {
        ServiceURL = rawUrl,
        ForcePathStyle = true
    };
    return new AmazonS3Client(credentials, s3Config);
});

// 5. IoC Servis Kayıtları
builder.Services.AddHttpClient<INviVerificationService, NviVerificationManager>();
builder.Services.AddScoped<ITokenHelper, JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthManager>();
builder.Services.AddScoped<IStorageService, CloudflareR2StorageManager>();
builder.Services.AddScoped<IListingService, ListingManager>();
builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<ILocationService, LocationManager>();
builder.Services.AddScoped<IEmailService, SmtpEmailManager>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// 6. Swagger JWT Desteği (.NET 10 OpenAPI)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GuvenleAlSat.API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token formatı (Örnek: 'Bearer {token}')",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// KRİTİK: wwwroot altındaki resimlerin http://localhost:5121/uploads/... ile açılmasını sağlar
app.UseStaticFiles();

app.UseRouting();

// CORS mutlaka UseRouting sonrasında ve UseAuthentication öncesinde olmalıdır
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GuvenleAlSat.API.Hubs.ChatHub>("/hubs/chat");

// Veritabanı Migration ve Seed Kontrolü
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();
    await GuvenleAlSat.DataAccess.Seeds.DatabaseSeeder.SeedAsync(context);
}

app.Run();