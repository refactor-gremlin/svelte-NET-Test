# MySvelteApp.Server

ASP.NET Core API using **vertical slice architecture** - organized by feature/use case rather than technical layers.

## Project Layout

```
Features/{Group}/{FeatureName}/     # Self-contained features
├── {FeatureName}Request.cs         # Request DTO (if needed)
├── {FeatureName}Response.cs       # Response DTO
├── {FeatureName}Handler.cs         # Business logic
├── {FeatureName}Validator.cs      # Validation (optional)
└── {FeatureName}Endpoint.cs       # API endpoint

Shared/                             # Code used by 3+ features
├── Common/                         # ApiResult, ApiResponse, interfaces
├── Domain/                         # Entities
├── Infrastructure/                 # Persistence, auth, config
└── Presentation/                    # Base controllers, middleware
```

## Architecture

**Key Principle**: Features are self-contained. Everything for a feature lives in one folder.

| Rule | Action |
|------|--------|
| 3+ features need it | → `Shared/` |
| 1-2 features need it | → Keep in feature folder |
| Related features | → Group in `Features/{Group}/` |

## Quick Reference

### Service Registration

```csharp
builder.Services.AddFeatureHandlers();           // Feature handlers
builder.Services.AddInfrastructureServices();    // Repositories, UnitOfWork
builder.Services.AddAuthenticationServices(config);
builder.Services.AddDatabaseServices();
builder.Services.AddPresentationServices();      // Controllers, validation
builder.Services.AddHealthCheckServices();
```

### Configuration

```csharp
// Access settings
var jwtSettings = configuration.GetSettings<JwtSettings>();

// Or inject in handlers
public MyHandler(IOptions<JwtSettings> jwtSettings) { }
```

**Settings Classes**: `JwtSettings`, `CorsSettings`, `LoggingSettings`

### ApiResult Pattern

```csharp
// Handler returns ApiResult<T>
return ApiResult<Response>.Success(data);
return ApiResult<Response>.Conflict("Error message");
return ApiResult<Response>.Unauthorized("Error message");
return ApiResult<Response>.NotFound("Error message");

// Endpoint maps to HTTP status
return ToActionResult(result); // Auto-maps ApiResult → IActionResult
```

**Error Types**: `Success`, `Failure`, `Unauthorized`, `Conflict`, `NotFound`, `ValidationError`

### Unit of Work

```csharp
// Handlers use IUnitOfWork for persistence
await _repository.AddAsync(entity);
await _unitOfWork.SaveChangesAsync(); // Centralized save
```

### Middleware Order

1. Exception Handling
2. CORS
3. Swagger (dev only)
4. HTTPS Redirection
5. Serilog Request Logging
6. Authentication
7. Authorization
8. Health Checks
9. Controllers

## Adding a New Feature

**See `FEATURE_TEMPLATE.md` for copy-paste templates.**

1. Create folder: `Features/{Group}/{FeatureName}/`
2. Add files: `Request.cs`, `Response.cs`, `Handler.cs`, `Validator.cs` (optional), `Endpoint.cs`
3. Register handler in `ServiceCollectionExtensions.AddFeatureHandlers()`:
   ```csharp
   services.AddScoped<YourFeatureHandler>();
   // For HttpClient: services.AddHttpClient<YourFeatureHandler>();
   ```

**Example Routes**:
- `POST /auth/register` - RegisterUser
- `POST /auth/login` - LoginUser
- `GET /auth/me` - GetCurrentUser
- `GET /auth/test` - TestAuth
- `GET /pokemon/random` - GetRandomPokemon

## Adding Configuration

1. Create class in `Shared/Infrastructure/Configuration/`:
   ```csharp
   public class YourSettings
   {
       public const string SectionName = "YourSection";
       public string Property { get; set; } = string.Empty;
   }
   ```
2. Register in `ConfigurationExtensions.cs`:
   ```csharp
   services.Configure<YourSettings>(configuration.GetSection(YourSettings.SectionName));
   ```
3. Add to `appsettings.json`

## Adding Validators

Create `{FeatureName}Validator.cs` in feature folder. Automatically discovered and registered.

```csharp
public class YourValidator : AbstractValidator<YourRequest>
{
    public YourValidator()
    {
        RuleFor(x => x.Property).NotEmpty();
    }
}
```

## Adding Health Checks

1. Create class implementing `IHealthCheck` in `Shared/Infrastructure/HealthChecks/`
2. Register in `AddHealthCheckServices()`:
   ```csharp
   services.AddHealthChecks().AddCheck<YourHealthCheck>("your_service");
   ```
3. Access via `GET /health`

## Error Handling

| Error Type | Handler | Response |
|------------|---------|----------|
| Validation | `ValidationFilter` | 400 BadRequest + `ApiErrorResponse` |
| Business Logic | `ApiResult<T>` | Auto-mapped HTTP status + `ApiErrorResponse` |
| Unhandled | `ExceptionHandlingMiddleware` | 500 InternalServerError + `ApiErrorResponse` |

**Response Format**:
```json
// Success
{ "Data": {...}, "Success": true }

// Error
{ "Message": "Error description", "ErrorCode": "ErrorType" }
```

## Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Health check
curl http://localhost:5000/health
```

**Note**: Uses in-memory database by default. Update `AppDbContext` for persistent storage.
