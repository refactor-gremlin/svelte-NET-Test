# Feature Template

Copy-paste templates for creating new features. Replace `{Group}` and `{FeatureName}` placeholders.

## Folder Structure

```
Features/{Group}/{FeatureName}/
├── {FeatureName}Request.cs      # If needed
├── {FeatureName}Response.cs
├── {FeatureName}Handler.cs
├── {FeatureName}Validator.cs     # Optional
└── {FeatureName}Endpoint.cs
```

## Request DTO

```csharp
namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Request
{
    public string Property { get; set; } = string.Empty;
}
```

## Response DTO

```csharp
namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Response
{
    public int Id { get; set; }
    public string Property { get; set; } = string.Empty;
}
```

**Using Shared DTOs:**

```csharp
using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Response
{
    public string Token { get; set; } = string.Empty;  // Feature-specific
    public UserDto User { get; set; } = null!;         // Shared DTO
}
```

## Handler

```csharp
using MySvelteApp.Server.Features.{Group}.{FeatureName};
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Events;
using MySvelteApp.Server.Shared.Domain.Services;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Handler
{
    private readonly I{Feature}Repository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly I{Feature}DomainService _domainService;  // Optional: if using domain service
    private readonly IDomainEventPublisher _eventPublisher;    // Optional: if publishing events

    public {FeatureName}Handler(
        I{Feature}Repository repository, 
        IUnitOfWork unitOfWork,
        I{Feature}DomainService? domainService = null,
        IDomainEventPublisher? eventPublisher = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _domainService = domainService;
        _eventPublisher = eventPublisher;
    }

    public async Task<ApiResult<{FeatureName}Response>> HandleAsync(
        {FeatureName}Request request,
        CancellationToken cancellationToken = default)
    {
        // Example: Using value objects
        // var email = Email.Create(request.Email);
        // var username = Username.Create(request.Username);

        // Example: Using domain service
        // var (canProceed, errorMessage) = await _domainService
        //     .CanPerformActionAsync(...);
        // if (!canProceed)
        // {
        //     return ApiResult<{FeatureName}Response>.Conflict(errorMessage!);
        // }

        // Business logic
        var entity = new {Feature} { Property = request.Property };
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Example: Publishing domain event
        // if (_eventPublisher != null)
        // {
        //     await _eventPublisher.PublishAsync(
        //         new {FeatureName}CreatedEvent(entity.Id),
        //         cancellationToken);
        // }

        return ApiResult<{FeatureName}Response>.Success(new {FeatureName}Response
        {
            Id = entity.Id,
            Property = entity.Property
        });
    }
}
```

## Validator (Optional)

```csharp
using FluentValidation;
using MySvelteApp.Server.Features.{Group}.{FeatureName};

namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Validator : AbstractValidator<{FeatureName}Request>
{
    public {FeatureName}Validator()
    {
        RuleFor(x => x.Property)
            .NotEmpty().WithMessage("Property is required.")
            .MinimumLength(3).WithMessage("Property must be at least 3 characters.");
    }
}
```

## Endpoint

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.{Group}.{FeatureName};
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

[ApiController]
[Route("{group}/{feature-name}")]  // e.g., [Route("products")]
public class {FeatureName}Endpoint : ApiControllerBase
{
    private readonly {FeatureName}Handler _handler;

    public {FeatureName}Endpoint({FeatureName}Handler handler)
    {
        _handler = handler;
    }

    [HttpPost]  // or [HttpGet], [HttpPut], [HttpDelete]
    [AllowAnonymous]  // Remove if auth required
    [ProducesResponseType(typeof(ApiResponse<{FeatureName}Response>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Handle(
        [FromBody] {FeatureName}Request request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }
}
```

## Registration

Add to `Shared/Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
{
    // ... existing handlers ...
    services.AddScoped<{FeatureName}Handler>();
    // For HttpClient: services.AddHttpClient<{FeatureName}Handler>();
    return services;
}
```

## Complete Example: CreateProduct

### CreateProductRequest.cs
```csharp
namespace MySvelteApp.Server.Features.Products.CreateProduct;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### CreateProductResponse.cs
```csharp
namespace MySvelteApp.Server.Features.Products.CreateProduct;

public class CreateProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

**Example with Shared DTO:**

```csharp
using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
```

### CreateProductHandler.cs
```csharp
using MySvelteApp.Server.Features.Products.CreateProduct;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;

namespace MySvelteApp.Server.Features.Products.CreateProduct;

public class CreateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResult<CreateProductResponse>> HandleAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResult<CreateProductResponse>.Success(new CreateProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        });
    }
}
```

### CreateProductValidator.cs
```csharp
using FluentValidation;
using MySvelteApp.Server.Features.Products.CreateProduct;

namespace MySvelteApp.Server.Features.Products.CreateProduct;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

### CreateProductEndpoint.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.Products.CreateProduct;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Products.CreateProduct;

[ApiController]
[Route("products")]
public class CreateProductEndpoint : ApiControllerBase
{
    private readonly CreateProductHandler _handler;

    public CreateProductEndpoint(CreateProductHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Handle(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }
}
```

### Registration
```csharp
services.AddScoped<CreateProductHandler>();
```

## Using Value Objects

When your feature uses domain concepts that need validation (like Email, Username):

```csharp
// In Handler
public async Task<ApiResult<Response>> HandleAsync(Request request)
{
    // Create value objects from request strings
    Email email;
    Username username;
    
    try
    {
        email = Email.Create(request.Email);
        username = Username.Create(request.Username);
    }
    catch (ArgumentException ex)
    {
        return ApiResult<Response>.ValidationError(ex.Message);
    }

    // Use value objects in entities
    var entity = new Entity
    {
        Email = email,
        Username = username
    };

    // Access value when needed
    string emailString = entity.Email.Value;
}
```

## Using Domain Services

When business logic spans multiple entities or requires complex operations:

```csharp
// 1. Create domain service interface in Shared/Domain/Services/
public interface I{Feature}DomainService
{
    Task<(bool CanProceed, string? ErrorMessage)> CanPerformActionAsync(
        // parameters
        CancellationToken cancellationToken = default);
}

// 2. Implement in Shared/Domain/Services/
public class {Feature}DomainService : I{Feature}DomainService
{
    private readonly I{Feature}Repository _repository;

    public {Feature}DomainService(I{Feature}Repository repository)
    {
        _repository = repository;
    }

    public async Task<(bool CanProceed, string? ErrorMessage)> CanPerformActionAsync(
        // parameters
        CancellationToken cancellationToken = default)
    {
        // Complex business logic here
        return (true, null);
    }
}

// 3. Register in ServiceCollectionExtensions.AddDomainServices()
services.AddScoped<I{Feature}DomainService, {Feature}DomainService>();

// 4. Use in handler
private readonly I{Feature}DomainService _domainService;

var (canProceed, errorMessage) = await _domainService
    .CanPerformActionAsync(..., cancellationToken);
if (!canProceed)
{
    return ApiResult<Response>.Conflict(errorMessage!);
}
```

## Using Domain Events

When something important happens that other features might care about:

```csharp
// 1. Create event in feature folder
namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

using MySvelteApp.Server.Shared.Domain.Events;

public record {FeatureName}CreatedEvent(int Id, string Property) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// 2. Publish in handler after successful operation
await _eventPublisher.PublishAsync(
    new {FeatureName}CreatedEvent(entity.Id, entity.Property),
    cancellationToken);

// 3. Create event handler (optional, in any feature or Shared/)
public class {FeatureName}CreatedEventHandler : IDomainEventHandler<{FeatureName}CreatedEvent>
{
    public async Task HandleAsync(
        {FeatureName}CreatedEvent domainEvent, 
        CancellationToken cancellationToken)
    {
        // Handle the event (send email, update cache, etc.)
    }
}

// 4. Register handler in ServiceCollectionExtensions
services.AddScoped<IDomainEventHandler<{FeatureName}CreatedEvent>, {FeatureName}CreatedEventHandler>();
```

## Using Shared DTOs

When multiple features need the same data structure, use shared DTOs:

```csharp
// 1. Shared DTOs are in Shared/Common/DTOs/
// Example: Shared/Common/DTOs/UserDto.cs
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 2. Use in feature response DTOs
using MySvelteApp.Server.Shared.Common.DTOs;

public class {FeatureName}Response
{
    public string Token { get; set; } = string.Empty;  // Feature-specific
    public UserDto User { get; set; } = null!;          // Shared DTO
}

// 3. Populate in handler
return ApiResult<{FeatureName}Response>.Success(new {FeatureName}Response
{
    Token = token,
    User = new UserDto
    {
        Id = user.Id,
        Username = user.Username.Value,
        Email = user.Email.Value
    }
});
```

**Guidelines:**
- Always prefer shared DTOs for common entities (User, Product, Order, etc.)
- Keeps API responses consistent across endpoints
- Makes frontend integration easier
- Use feature-specific DTOs only when the data shape is truly unique to that feature
