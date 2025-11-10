using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using RecipeBackend.DTOs;
using RecipeBackend.Models;
using RecipeBackend.Services;
using RecipeBackend.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(settings =>
{
    settings.Title = "Recipe Backend API";
    settings.Description = "REST API for managing recipes with authentication and search.";
    settings.Version = "1.0.0";
    // JWT in Swagger
    settings.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = OpenApiSecurityApiKeyLocation.Header,
        Description = "Type 'Bearer {your JWT token}'."
    });
    settings.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In-memory repos and services
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IRecipeRepository, InMemoryRecipeRepository>();
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();

// JWT Auth
var tempProvider = new InMemoryTokenService(); // to obtain validation params
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tempProvider.GetValidationParameters();
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Configure OpenAPI/Swagger
app.UseOpenApi();
app.UseSwaggerUi(config =>
{
    config.Path = "/docs";
});

// Root health check
// PUBLIC_INTERFACE
app.MapGet("/", () => new { message = "Healthy" })
   .WithName("Health");

// Auth endpoints
// PUBLIC_INTERFACE
app.MapPost("/api/auth/register", (IUserRepository users, RegisterRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrEmpty(req.Password))
        return Results.BadRequest(new { error = "Username and password are required." });

    if (users.Exists(req.Username))
        return Results.Conflict(new { error = "Username already exists." });

    var ok = users.TryCreateUser(req.Username, req.Password);
    if (!ok) return Results.BadRequest(new { error = "Unable to create user." });

    return Results.Created($"/api/users/{req.Username}", new { username = req.Username });
})
.WithName("Register");

// PUBLIC_INTERFACE
app.MapPost("/api/auth/login", (IUserRepository users, ITokenService tokenService, LoginRequest req) =>
{
    if (!users.ValidateCredentials(req.Username, req.Password))
        return Results.Unauthorized();

    var token = tokenService.IssueToken(req.Username);
    return Results.Ok(new AuthResponse { Token = token, Username = req.Username });
})
.WithName("Login");

// Recipe endpoints
// PUBLIC_INTERFACE
app.MapGet("/api/recipes", (IRecipeRepository repo, [AsParameters] RecipeQuery query) =>
{
    IEnumerable<Recipe> results = repo.Search(query.Query, query.Ingredient, query.Tag);

    if (query.Skip is > 0)
        results = results.Skip(query.Skip.Value);
    if (query.Take is > 0)
        results = results.Take(query.Take.Value);

    return Results.Ok(results.Select(r => r.ToDto()));
})
.WithName("ListRecipes");

// PUBLIC_INTERFACE
app.MapGet("/api/recipes/{id:guid}", (IRecipeRepository repo, Guid id) =>
{
    var item = repo.Get(id);
    return item is null ? Results.NotFound(new { error = "Recipe not found." }) : Results.Ok(item.ToDto());
})
.WithName("GetRecipe");

// PUBLIC_INTERFACE
app.MapPost("/api/recipes", (ClaimsPrincipal user, IRecipeRepository repo, RecipeCreateDto dto) =>
{
    // Require auth
    if (user?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    // Validate
    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Title is required." });

    var recipe = new Recipe
    {
        Title = dto.Title.Trim(),
        Description = dto.Description?.Trim(),
        Ingredients = dto.Ingredients?.Where(i => !string.IsNullOrWhiteSpace(i))
                                      .Select(i => i.Trim())
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList() ?? new List<string>(),
        Tags = dto.Tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList() ?? new List<string>(),
        Author = user.Identity?.Name
    };

    var saved = repo.Add(recipe);
    return Results.Created($"/api/recipes/{saved.Id}", saved.ToDto());
})
.RequireAuthorization()
.WithName("CreateRecipe");

// PUBLIC_INTERFACE
app.MapPut("/api/recipes/{id:guid}", (ClaimsPrincipal user, IRecipeRepository repo, Guid id, RecipeUpdateDto dto) =>
{
    if (user?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var existing = repo.Get(id);
    if (existing is null)
        return Results.NotFound(new { error = "Recipe not found." });

    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest(new { error = "Title is required." });

    existing.Title = dto.Title.Trim();
    existing.Description = dto.Description?.Trim();
    existing.Ingredients = dto.Ingredients?.Where(i => !string.IsNullOrWhiteSpace(i))
                                          .Select(i => i.Trim())
                                          .Distinct(StringComparer.OrdinalIgnoreCase)
                                          .ToList() ?? new List<string>();
    existing.Tags = dto.Tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                             .Select(t => t.Trim())
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToList() ?? new List<string>();
    existing.UpdatedAtUtc = DateTime.UtcNow;

    var ok = repo.Update(existing);
    if (!ok) return Results.Problem("Failed to update recipe.");

    return Results.Ok(existing.ToDto());
})
.RequireAuthorization()
.WithName("UpdateRecipe");

// PUBLIC_INTERFACE
app.MapDelete("/api/recipes/{id:guid}", (ClaimsPrincipal user, IRecipeRepository repo, Guid id) =>
{
    if (user?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var existing = repo.Get(id);
    if (existing is null)
        return Results.NotFound(new { error = "Recipe not found." });

    var ok = repo.Delete(id);
    return ok ? Results.NoContent() : Results.Problem("Failed to delete recipe.");
})
.RequireAuthorization()
.WithName("DeleteRecipe");

// Notes about usage and websocket (not used here but keeping docs alignment)
// PUBLIC_INTERFACE
app.MapGet("/api/docs/usage", () => Results.Ok(new
{
    message = "Use /api/auth/register and /api/auth/login for authentication. Use /api/recipes for CRUD and search. JWT Bearer token required for POST/PUT/DELETE.",
    swagger = "/docs"
}))
.WithName("ApiUsage");

app.Run();