using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FitTrack.Application;
using FitTrack.Application.Services;
using FitTrack.Infrastructure;
using FitTrack.Infrastructure.Persistence;
using FitTrack.Infrastructure.Security;
using FitTrack.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ApplicationExceptionFilter>();
        options.ModelMetadataDetailsProviders.Add(
            new BodyValidationNames(JsonNamingPolicy.SnakeCaseLower));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    })
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context => ApiErrors.Validation(context.ModelState));

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("The Jwt section is missing from appsettings.json.");

if (jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 characters. Set it in appsettings.Development.json or the Jwt__Key environment variable.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        };
        options.Events = new JwtBearerEvents { OnTokenValidated = ValidateSessionAsync };
    });

// Every endpoint needs a signed-in user unless it opts out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
        new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many attempts. Please wait a minute and try again." }, cancellationToken));
    options.AddPolicy(RateLimits.Auth, TenPerMinutePerIp);
    options.AddPolicy(RateLimits.Uploads, TenPerMinutePerIp);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

// Uploaded pictures are public. These headers stop a browser from ever treating one
// as a page or script.
var uploadsRoot = (app.Configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions())
    .UploadsRoot(app.Environment);
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    },
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static RateLimitPartition<string> TenPerMinutePerIp(HttpContext context) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) });

static async Task ValidateSessionAsync(TokenValidatedContext context)
{
    var identity = (ClaimsIdentity)context.Principal!.Identity!;
    var user = int.TryParse(identity.FindFirst("sub")?.Value, out var userId)
        ? await context.HttpContext.RequestServices.GetRequiredService<SessionService>()
            .FindValidUserAsync(userId, identity.FindFirst(TokenClaims.Stamp)?.Value)
        : null;

    if (user is null)
    {
        context.Fail("The session is no longer valid.");
        return;
    }

    // The role may have changed since the token was issued.
    foreach (var claim in identity.FindAll("role").ToList())
    {
        identity.RemoveClaim(claim);
    }

    identity.AddClaim(new Claim("role", user.Role.ToValue()));
}