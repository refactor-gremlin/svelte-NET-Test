# MySvelteApp.Server

This ASP.NET Core project hosts the API for the Svelte.NET solution. The codebase follows a layered (Clean Architecture inspired) layout to keep domain logic, application workflows, infrastructure integrations, and presentation concerns isolated and testable.

## Project Layout

```
MySvelteApp.Server/
├── Domain/
│   └── Entities/
│       └── User.cs
├── Application/
│   ├── Authentication/
│   │   ├── AuthService.cs
│   │   ├── DTOs/
│   │   │   ├── AuthErrorType.cs
│   │   │   ├── AuthResult.cs
│   │   │   ├── LoginRequest.cs
│   │   │   └── RegisterRequest.cs
│   │   ├── Validators/
│   │   │   ├── LoginRequestValidator.cs
│   │   │   └── RegisterRequestValidator.cs
│   │   └── IAuthService.cs
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IJwtTokenGenerator.cs
│   │   │   ├── IPasswordHasher.cs
│   │   │   ├── IUserRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Results/
│   │       ├── Result.cs
│   │       └── ResultExtensions.cs
│   ├── Pokemon/
│   │   ├── DTOs/RandomPokemonDto.cs
│   │   └── IRandomPokemonService.cs
│   └── Weather/
│       ├── DTOs/WeatherForecastDto.cs
│       └── IWeatherForecastService.cs
├── Infrastructure/
│   ├── Authentication/JwtTokenGenerator.cs
│   ├── Configuration/
│   │   ├── ConfigurationExtensions.cs
│   │   ├── CorsSettings.cs
│   │   ├── JwtSettings.cs
│   │   └── LoggingSettings.cs
│   ├── DependencyInjection/
│   │   └── ServiceCollectionExtensions.cs
│   ├── External/PokeApiRandomPokemonService.cs
│   ├── HealthChecks/
│   │   ├── DatabaseHealthCheck.cs
│   │   └── ExternalApiHealthCheck.cs
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   └── Repositories/UserRepository.cs
│   ├── Security/PasswordHasher.cs
│   └── Weather/WeatherForecastService.cs
├── Presentation/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── RandomPokemonController.cs
│   │   ├── TestAuthController.cs
│   │   └── WeatherForecastController.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── MiddlewareExtensions.cs
│   └── Models/Auth/
│       ├── AuthErrorResponse.cs
│       └── AuthSuccessResponse.cs
├── Program.cs
├── README.md
└── (configuration & build assets)
```

## Layer Responsibilities & Restrictions

The layers depend strictly inward to keep responsibilities separated:

- **Domain**: Pure entities and domain primitives. No references to ASP.NET Core, Entity Framework, or other project layers.
- **Application**: Use cases, DTOs, interfaces, validators, and result patterns. Depends only on `Domain`. It defines the contracts (`IUserRepository`, `IJwtTokenGenerator`, `IPasswordHasher`, `IUnitOfWork`, etc.) that the infrastructure layer must implement.
- **Infrastructure**: External concerns such as Entity Framework, JWT generation, password hashing, HTTP clients, configuration management, and health checks. Implements the interfaces from `Application` and depends on third-party libraries, configuration, and the framework.
- **Presentation**: ASP.NET Core controllers, response models, middleware, and filters. This is the only layer talking directly to HTTP. Controllers depend on `Application` interfaces/services and presentation-specific models.
- **Program.cs (Composition Root)**: Wires up dependency injection and middleware using extension methods. Only this file references all layers to compose the application.

**Key rules**

1. `Domain` must not depend on any other folder.
2. `Application` can reference `Domain` only.
3. `Infrastructure` can reference `Application` and `Domain`, but controllers should not use infrastructure types directly—only through interfaces registered in DI.
4. `Presentation` should remain thin, delegating work to `Application` services and using DTOs for inbound/outbound models.

## Configuration Management

The application uses strongly-typed configuration classes instead of magic strings for better type safety and IntelliSense support.

### Configuration Classes

- **JwtSettings**: JWT token configuration (Key, Issuer, Audience, ExpirationHours)
- **CorsSettings**: CORS policy configuration (AllowedOrigins, AllowAnyHeader, AllowAnyMethod, AllowCredentials)
- **LoggingSettings**: Logging and OpenTelemetry configuration (LokiPushUrl, OtelServiceName, OtelExporterOtlpEndpoint, OtelExporterOtlpProtocol)

### Usage

Configuration is registered in `Program.cs`:

```csharp
builder.Services.AddConfigurationSettings(builder.Configuration);
```

Access configuration using extension methods:

```csharp
var jwtSettings = configuration.GetJwtSettings();
var corsSettings = configuration.GetCorsSettings();
var loggingSettings = configuration.GetLoggingSettings();
```

Or inject `IOptions<T>` in services:

```csharp
public class MyService
{
    private readonly JwtSettings _jwtSettings;
    
    public MyService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
}
```

Configuration is validated at startup using DataAnnotations validation.

## Service Registration Organization

Services are registered using extension methods organized by layer and concern. This keeps `Program.cs` clean and makes it easy to find where services are registered.

### Extension Methods

- `AddApplicationServices()`: Registers application layer services (e.g., `IAuthService`)
- `AddInfrastructureServices()`: Registers infrastructure services (repositories, UnitOfWork)
- `AddAuthenticationServices(IConfiguration)`: Registers authentication and authorization services
- `AddDatabaseServices()`: Registers database context and persistence services
- `AddExternalServices()`: Registers HTTP clients and external API services
- `AddPresentationServices()`: Registers controllers and validation filters
- `AddHealthCheckServices()`: Registers health check endpoints

### Usage in Program.cs

```csharp
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddDatabaseServices();
builder.Services.AddExternalServices();
builder.Services.AddPresentationServices();
builder.Services.AddHealthCheckServices();
```

### Adding New Services

To add a new service:

1. Create the interface in `Application/Common/Interfaces/` or feature-specific folder
2. Create the implementation in `Infrastructure/`
3. Register it in the appropriate extension method in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`

Example:

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IYourNewService, YourNewService>(); // Add here
    return services;
}
```

## Middleware Pipeline

The application uses a middleware pipeline for cross-cutting concerns.

### Exception Handling Middleware

Global exception handling is provided by `ExceptionHandlingMiddleware`, which:
- Catches unhandled exceptions
- Logs exceptions with context
- Returns consistent error responses
- Maps exceptions to appropriate HTTP status codes

Registered in `Program.cs`:

```csharp
app.UseExceptionHandling();
```

### Middleware Order

The middleware pipeline order is:
1. Exception Handling (catches all downstream exceptions)
2. CORS
3. Swagger (Development only)
4. HTTPS Redirection
5. Serilog Request Logging
6. Authentication
7. Authorization
8. Health Checks
9. Controllers

## Unit of Work Pattern

The application uses the Unit of Work pattern to manage database transactions and ensure data consistency.

### IUnitOfWork Interface

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### Usage in Services

Services use `IUnitOfWork` instead of calling `SaveChangesAsync` directly on repositories:

```csharp
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(); // Centralized save
        // ...
    }
}
```

### Benefits

- **Transaction Management**: Multiple repository operations can be wrapped in a transaction
- **Consistency**: All changes are saved together or rolled back together
- **Testability**: Easy to mock for unit tests
- **Separation of Concerns**: Repositories focus on data access, UnitOfWork handles persistence

## Validation Layer

Input validation is handled using FluentValidation, keeping validation logic separate from business logic.

### Validators

Validators are located in feature-specific `Validators/` folders:

- `Application/Authentication/Validators/RegisterRequestValidator.cs`
- `Application/Authentication/Validators/LoginRequestValidator.cs`

### Creating Validators

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.");
    }
}
```

### Validation Filter

The `ValidationFilter` automatically validates requests before they reach controllers:

- Validates model state
- Returns BadRequest with error details if validation fails
- Allows valid requests to proceed

## Health Checks

Health checks provide monitoring endpoints for the application and its dependencies.

### Available Health Checks

- **DatabaseHealthCheck**: Checks database connectivity
- **ExternalApiHealthCheck**: Checks external API availability (e.g., Pokemon API)

### Health Check Endpoint

Access health checks at `/health`:

```bash
GET /health
```

Response includes status of all registered health checks.

### Adding Health Checks

1. Create a class implementing `IHealthCheck`:

```csharp
public class YourHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Check your service
        return HealthCheckResult.Healthy("Service is available.");
    }
}
```

2. Register in `AddHealthCheckServices()`:

```csharp
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<YourHealthCheck>("your_service");
```

## Error Handling

The application uses a consistent error handling approach:

1. **Validation Errors**: Handled by `ValidationFilter` → Returns 400 BadRequest
2. **Business Logic Errors**: Returned as `AuthResult` with error details → Mapped to appropriate HTTP status
3. **Unhandled Exceptions**: Caught by `ExceptionHandlingMiddleware` → Returns 500 InternalServerError

### Error Response Format

```json
{
  "Message": "Error description"
}
```

## Dependency Registration

`Program.cs` configures services and middleware using extension methods:

- Configuration settings are registered via `AddConfigurationSettings()`
- CORS, authentication, authorization are configured
- Swagger is set up for API documentation
- Serilog and OpenTelemetry are configured for logging and tracing
- The in-memory `AppDbContext` is registered via `AddDatabaseServices()`
- Infrastructure implementations are bound to application interfaces via extension methods:
  - `PasswordHasher` → `IPasswordHasher`
  - `UserRepository` → `IUserRepository`
  - `UnitOfWork` → `IUnitOfWork`
  - `JwtTokenGenerator` → `IJwtTokenGenerator`
  - `PokeApiRandomPokemonService` → `IRandomPokemonService` (typed HTTP client)
  - `WeatherForecastService` → `IWeatherForecastService`
- `AuthService` is added as the orchestrator for registration and login workflows

## Working With the Project

### Build

```bash
# Build the API project only
cd MySvelteApp.Server
dotnet build MySvelteApp.Server.csproj -p:BuildProjectReferences=false
```

### Run

```bash
cd MySvelteApp.Server
dotnet run --project MySvelteApp.Server.csproj
```

By default the project uses an in-memory database. Swap in a persistent provider by updating `AppDbContext` options and injecting the required `DbContextOptions` configuration.

### Health Checks

```bash
# Check application health
curl http://localhost:5000/health
```

## Extending the API

### Adding a New Feature

1. **Add domain types** under `Domain/` with no framework dependencies.

2. **Define contracts and DTOs** in `Application/` to describe new use cases:
   - Create DTOs in feature-specific `DTOs/` folder
   - Create service interface in feature folder or `Application/Common/Interfaces/`

3. **Create validators** (if needed) in `Application/{Feature}/Validators/`:
   ```csharp
   public class YourRequestValidator : AbstractValidator<YourRequest>
   {
       public YourRequestValidator()
       {
           RuleFor(x => x.Property).NotEmpty();
       }
   }
   ```

4. **Implement integrations** in `Infrastructure/`:
   - Repositories in `Infrastructure/Persistence/Repositories/`
   - External services in `Infrastructure/External/`
   - Other infrastructure concerns in appropriate folders

5. **Create application service** in `Application/{Feature}/`:
   - Implement the service interface
   - Use `IUnitOfWork` for database operations
   - Inject dependencies through constructor

6. **Expose endpoints** via controllers in `Presentation/Controllers/`:
   - Use application services and DTOs
   - Keep controllers thin

7. **Register services** via extension methods in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:
   ```csharp
   // In AddApplicationServices()
   services.AddScoped<IYourService, YourService>();
   
   // In AddInfrastructureServices()
   services.AddScoped<IYourRepository, YourRepository>();
   ```

8. **Add health checks** (if needed) in `Infrastructure/HealthChecks/` and register in `AddHealthCheckServices()`

9. **Add configuration** (if needed):
   - Create configuration class in `Infrastructure/Configuration/`
   - Register in `ConfigurationExtensions.AddConfigurationSettings()`
   - Add to `appsettings.json`

### Adding Configuration Classes

1. Create settings class:

```csharp
public class YourSettings
{
    public const string SectionName = "YourSection";
    public string Property { get; set; } = string.Empty;
}
```

2. Add to `ConfigurationExtensions.cs`:

```csharp
services.Configure<YourSettings>(configuration.GetSection(YourSettings.SectionName));
```

3. Add to `appsettings.json`:

```json
{
  "YourSection": {
    "Property": "value"
  }
}
```

### Adding Validators

1. Create validator class in `Application/{Feature}/Validators/`
2. Validators are automatically discovered and registered
3. Validation runs automatically via `ValidationFilter`

### Adding Health Checks

1. Create health check class implementing `IHealthCheck`
2. Register in `AddHealthCheckServices()` extension method
3. Access via `/health` endpoint

Following these steps preserves the separation of concerns and keeps cross-layer dependencies predictable.
