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

## Handler

```csharp
using MySvelteApp.Server.Features.{Group}.{FeatureName};
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;

namespace MySvelteApp.Server.Features.{Group}.{FeatureName};

public class {FeatureName}Handler
{
    private readonly I{Feature}Repository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public {FeatureName}Handler(I{Feature}Repository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResult<{FeatureName}Response>> HandleAsync(
        {FeatureName}Request request,
        CancellationToken cancellationToken = default)
    {
        // Business logic
        var entity = new {Feature} { Property = request.Property };
        
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
