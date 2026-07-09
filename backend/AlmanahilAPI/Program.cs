// ============================================
// Program.cs — the app's STARTUP / setup file. This runs first when the API starts.
// Job: switch on and configure all the parts the app needs (who can call us,
// the database, login checking, our services, the controllers, and the test UI),
// then start listening for web requests.
// Think of it as flipping on all the switches before opening the shop.
// ============================================
using System.Security.Claims;
using System.Text;
using AlmanahilAPI.Data;
using AlmanahilAPI.Helpers;
using AlmanahilAPI.Services;
using AlmanahilAPI.Settings;
using AlmanahilAPI.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// 'builder' is where we register all the settings and services before the app starts.
var builder = WebApplication.CreateBuilder(args);

// CORS = allow our Vue website (running on localhost:5173) to call this API.
// Browsers block cross-site calls by default, so we must permit it here.
// 1. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// DATABASE: connect to our PostgreSQL database using the connection details
// stored in appsettings.json. EF Core lets us use C# instead of raw SQL.
// 2. PostgreSQL + EF Core
builder.Services.AddDbContext<AlmanahilDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// LOGIN CHECKING (JWT): teach the app how to read and trust the login token
// that the browser sends with each request, so it knows who is calling.
// 3. JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // These are the checks the app runs on each token: is it from us, for us,
        // still in date, and correctly signed? All must pass or the token is rejected.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),

            // Explicitly tell the framework which claim types hold the user's
            // name and role. TokenService writes the role as ClaimTypes.Role,
            // so mapping RoleClaimType to the same value makes
            // [Authorize(Roles = "Admin")] work reliably (the modern
            // JsonWebTokenHandler does not remap inbound claims by default).
            NameClaimType = ClaimTypes.Email,
            RoleClaimType = ClaimTypes.Role
        };
    });

// AUTHORIZATION: turns on the rules that decide what a logged-in user is allowed
// to do (e.g. [Authorize(Roles = "Admin")] on admin-only endpoints).
// 4. Authorization
builder.Services.AddAuthorization();

// DEPENDENCY INJECTION: register our own services here so the app can
// automatically hand them to any controller that asks for them.
// This one adds the token maker and the login/password service.
// 4b. Application services (auth + JWT generation)
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register the email sender + load its settings from appsettings.json.
// AddHttpClient gives it a ready-to-use tool for making internet calls to Brevo.
// 4c. Brevo email: bind settings + register a typed HttpClient for the service.
builder.Services.Configure<BrevoSettings>(
    builder.Configuration.GetSection(BrevoSettings.SectionName));
builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();

// Register the Supabase file-upload service + load its settings, and give it
// its own internet-calling tool too (used for uploading PDFs).
// 4d. Supabase Storage: bind settings + register a typed HttpClient for the file uploader
//     (Module 5 Stage 2 — PDF uploads). The service_role key stays in appsettings.json.
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection(SupabaseSettings.SectionName));
builder.Services.AddHttpClient<SupabaseStorageService>();

// CONTROLLERS: switch on the controller classes that handle the actual API
// endpoints (like /api/auth/login). This is what turns requests into responses.
// 5. Controllers + OpenAPI
builder.Services.AddControllers();

// When a request has bad/missing fields, send the error back in OUR standard
// { success, message, data } shape instead of the framework's default format,
// so the website always gets replies in one consistent shape.
// Return model-validation failures in the same ApiResponse envelope as every other
// endpoint (instead of the default ProblemDetails), so the frontend always sees
// { success, message, data }. This only reshapes validation errors — it changes no
// auth logic.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Invalid request.";

        return new BadRequestObjectResult(ApiResponse<object>.Fail(message));
    };
});

builder.Services.AddEndpointsApiExplorer();

// SWAGGER: a built-in test UI (a web page) where we can try each API endpoint
// by hand during development. The setup below also lets us paste a login token
// so we can test the protected endpoints too.
// 6. Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AlmanahilAPI", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    // Attach the Bearer requirement per-operation (to every [Authorize] endpoint)
    // instead of a single global requirement. This reliably makes Swagger UI send
    // the token to protected endpoints like /api/auth/change-password.
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

// Everything above was SETUP. Now we build the finished app and start
// arranging what happens on each incoming request (the 'pipeline').
var app = builder.Build();

// TEMPORARY DEBUG (Module 5 Stage 2): confirm the "Supabase" section was actually read from
// appsettings.json. Logs the URL + bucket + whether the key is present (its length) — NEVER the
// key itself. If ServiceRoleKey shows "MISSING", the config section/name is wrong. Remove later.
var supa = app.Configuration.GetSection(SupabaseSettings.SectionName).Get<SupabaseSettings>() ?? new SupabaseSettings();
app.Logger.LogInformation(
    "[Supabase config] Url='{Url}', Bucket='{Bucket}', ServiceRoleKey={KeyState} (length {Length}).",
    supa.Url, supa.Bucket,
    string.IsNullOrWhiteSpace(supa.ServiceRoleKey) ? "MISSING" : "present",
    supa.ServiceRoleKey?.Length ?? 0);

// PIPELINE: every incoming request passes through these steps IN THIS ORDER,
// like a checklist, before it reaches a controller. The order matters.
// Pipeline
// Only while developing: show the Swagger test page.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Push visitors to the secure https address.
app.UseHttpsRedirection();

// Allow the Vue website to call us (the CORS rule from the top).
app.UseCors("VueFrontend");
// Work out WHO the caller is (read their login token). Must come before the next line.
app.UseAuthentication();
// Check whether that caller is ALLOWED to do this action.
app.UseAuthorization();

// Hand the request to the matching controller endpoint.
app.MapControllers();

// Start the app and begin listening for requests. This line keeps it running.
app.Run();
