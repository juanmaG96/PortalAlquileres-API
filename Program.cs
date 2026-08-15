using System.IO.Compression;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marketplace.API.Configurations;
using Marketplace.API.Data;
using Marketplace.API.Services;
using Marketplace.API.Validators;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Response Compression (Gzip & Brotli) for Network Optimization
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.SmallestSize;
});

// 2. Memory Cache
builder.Services.AddMemoryCache();

// 3. Rate Limiting for Security & Bot Traffic Protection
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global Fixed Window Rate Limiter (100 requests per 1 minute per IP)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// 4. Database Context (PostgreSQL EF Core)
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=marketplace_paysandu;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 5. HttpClient for Nominatim Geocoding
builder.Services.AddHttpClient<INominatimGeocodingService, NominatimGeocodingService>();

// 6. Application Services & Cloudinary Options Pattern
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IImageService, CloudinaryImageService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 6b. FluentValidation (Intercepta peticiones e interrumpe con HTTP 400 Bad Request si fallan las reglas Fail-Fast)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PropertyCreateDtoValidator>();

// 7. Health Checks for Docker & Orchestration monitoring
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// 8. CORS Configuration for White-Label Frontend Integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://portal-alquileres-web.vercel.app",
                            "http://localhost:4200"
                            )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MarketplaceAPI",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "MarketplaceFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "SUPER_SECRET_WHITE_LABEL_KEY_ALQUILERES_2026!"))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Execute Data Seeder for base Admin user
await DatabaseSeeder.SeedAsync(app.Services, builder.Configuration);

// Enable Response Compression
app.UseResponseCompression();

// Enable Rate Limiting
app.UseRateLimiter();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check Endpoint
app.MapHealthChecks("/health");

app.Run();
