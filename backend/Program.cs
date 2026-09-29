/*
 * ---------------------------------------------------------------------------
 * File        : Program.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-16
 * Description : Application entry point and composition root. This is the only
 *               place in the solution where concrete implementations are bound
 *               to their interfaces, and where the HTTP pipeline is assembled.
 *
 *               Keeping all wiring here means every other class can depend on
 *               abstractions and stay unaware of how they are constructed,
 *               which is what makes the Dependency Inversion Principle work in
 *               practice rather than only on paper.
 *
 * Security    : Configuration is validated at startup so the service refuses to
 *               start with a missing connection string rather than failing on
 *               the first request. CORS is restricted to a configured list of
 *               origins. The Kestrel "Server" header is suppressed so the
 *               response does not advertise the web server in use.
 * ---------------------------------------------------------------------------
 */

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SolarMicrogrid.Api.Common;
using SolarMicrogrid.Api.Configuration;
using SolarMicrogrid.Api.Data;
using SolarMicrogrid.Api.Middleware;
using SolarMicrogrid.Api.Repositories;
using SolarMicrogrid.Api.Security;
using SolarMicrogrid.Api.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------
// Configuration binding
// -------------------------------------------------------------------------

// Bind and validate the MongoDB settings. ValidateOnStart turns a missing or
// malformed connection string into a startup failure with a readable message,
// instead of a NullReference on the first database call.
builder.Services
    .AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection(MongoDbSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection(CorsSettings.SectionName))
    .ValidateOnStart();

// The signing key is validated here too, so the service refuses to start with a
// weak or missing key rather than issuing tokens that cannot be trusted.
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

JwtSettings jwtSettings =
    builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

CorsSettings corsSettings =
    builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

// -------------------------------------------------------------------------
// Dependency injection
// -------------------------------------------------------------------------

// MongoContext is a singleton because it owns the MongoClient, which keeps its
// own connection pool and is thread safe. One instance per application is the
// pattern MongoDB documents; one per request would exhaust the pool.
builder.Services.AddSingleton<IMongoContext, MongoContext>();

// Creates the required MongoDB indexes once, as the host starts.
builder.Services.AddHostedService<DatabaseInitializer>();

// Creates the first Backoffice officer when the database has none, so a fresh
// deployment is not deadlocked with nobody able to administer it.
builder.Services
    .AddOptions<SeedSettings>()
    .Bind(builder.Configuration.GetSection(SeedSettings.SectionName));
builder.Services.AddHostedService<BackofficeSeeder>();

// Repositories are scoped: one instance per request. They are registered
// against their interfaces so that services never name a concrete type.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<ISlotRepository, SlotRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

// Exposes the current request so ICurrentUser can read the caller's claims.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Services that carry business rules are scoped: one instance per request.
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProsumerService, ProsumerService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

// Security collaborators. Both are stateless and thread safe, so a single
// instance serves every request.
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IQrTokenGenerator, QrTokenGenerator>();

// -------------------------------------------------------------------------
// Authentication and authorisation
// -------------------------------------------------------------------------

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Every one of these checks is enabled deliberately. Turning any of
        // them off would accept tokens this service should reject: an unsigned
        // token, one minted by a different system, or an expired one.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

            ValidateLifetime = true,

            // The default allows five minutes of clock drift, which keeps an
            // expired token working for five minutes longer than it should.
            // Thirty seconds is ample for machines syncing to internet time.
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Tokens travel over plain HTTP on the local network during the mobile
        // demonstration, so this is relaxed outside Development. On a public
        // deployment it must stay true.
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------------------------
// Rate limiting
// -------------------------------------------------------------------------

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Partitioned by client address so one attacker cannot lock out everyone
    // else. Ten attempts a minute is generous for a person signing in and
    // hostile to a script working through a password list.
    options.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// -------------------------------------------------------------------------
// Web / HTTP services
// -------------------------------------------------------------------------

builder.Services.AddControllers(options =>
{
    // By default ASP.NET Core strips the "Async" suffix when it registers an
    // action, so a method called GetByIdAsync is known to routing as "GetById".
    // That makes nameof(GetByIdAsync) fail to match when CreatedAtAction builds
    // a Location header — and it fails at response time, AFTER the record has
    // been written, producing a 500 for an operation that actually succeeded.
    //
    // Keeping the suffix means nameof() and the registered action name agree,
    // so the compiler catches a renamed action instead of it breaking at run
    // time. Attribute routes are unaffected: the URLs come from the templates.
    options.SuppressAsyncSuffixInActionNames = false;
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Smart Solar Microgrid Trading System API",
        Version = "v1",
        Description =
            "Central web service for the Smart Solar Microgrid Trading System. " +
            "All business logic resides in this API; the web and Android clients are UI layers only.",
    });

    // Surface the XML documentation comments in Swagger so the generated page
    // doubles as the API contract for the web and Android teams.
    string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Adds the "Authorize" button to the Swagger page so a token obtained from
    // /api/auth/login can be attached to subsequent calls while testing.
    OpenApiSecurityScheme bearerScheme = new()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the token returned by /api/auth/login. Swagger adds the \"Bearer \" prefix.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme,
        },
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
});

// Explicit origin whitelist. AllowAnyOrigin is deliberately not used: this API
// will issue bearer tokens, and an open policy would let any site call it.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        if (corsSettings.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsSettings.AllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Do not advertise the web server in the response headers.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// When hosted behind IIS, honour the forwarded headers so the application sees
// the original scheme and client address rather than the reverse proxy's.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

WebApplication app = builder.Build();

// -------------------------------------------------------------------------
// HTTP pipeline — order matters
// -------------------------------------------------------------------------

// Registered first so it can catch failures thrown anywhere further down.
// The nodes all sit in one country, so one time zone serves them. It is set
// once here, from configuration, and used wherever an opening time becomes a
// real instant.
GridTime.Configure(builder.Configuration["Grid:TimeZoneId"]);

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseForwardedHeaders();

// HTTPS redirection is applied in development only. On the IIS demo the
// Android device reaches the service over plain HTTP on the local network,
// where no trusted certificate exists; redirecting there would break the
// mobile client. Production hosting terminates TLS at IIS instead.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Swagger is enabled through configuration rather than environment alone, so it
// can be switched on for the IIS deployment demonstration and off afterwards.
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Solar Microgrid API v1");
        options.DocumentTitle = "Smart Solar Microgrid API";
    });
}

app.UseCors(CorsSettings.PolicyName);

app.UseRateLimiter();

// Order is significant and not interchangeable: authentication establishes WHO
// the caller is by validating the bearer token, and authorisation then decides
// WHAT that caller may reach. Reversing them would leave every [Authorize]
// check looking at an anonymous user and rejecting valid requests.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
