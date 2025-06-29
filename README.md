# FS.EntityFramework.Library

[![NuGet Version](https://img.shields.io/nuget/v/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.EntityFramework.Library)](https://github.com/furkansarikaya/FS.EntityFramework.Library/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.EntityFramework.Library.svg)](https://github.com/furkansarikaya/FS.EntityFramework.Library/stargazers)

A comprehensive Entity Framework Core library providing Repository pattern, Unit of Work, Specification pattern, dynamic filtering, pagination support, and **Domain Events** for .NET applications.

## 📋 Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
  - [Basic CRUD Operations](#basic-crud-operations)
  - [Dynamic Filtering](#dynamic-filtering)
  - [Specification Pattern](#specification-pattern)
  - [Pagination](#pagination)
  - [Unit of Work & Transactions](#unit-of-work--transactions)
  - [Soft Delete](#soft-delete-with-global-query-filters)
  - [Domain Events](#domain-events)
- [API Reference](#api-reference)
- [Requirements](#requirements)
- [Contributing](#contributing)
- [License](#license)

## Features

- 🏗️ **Repository Pattern**: Generic repository implementation with advanced querying capabilities
- 🔄 **Unit of Work Pattern**: Coordinate multiple repositories and manage transactions
- 📋 **Specification Pattern**: Flexible and reusable query specifications
- 🔍 **Dynamic Filtering**: Build dynamic queries from filter models
- 📄 **Pagination**: Built-in pagination support with metadata
- 🏷️ **Base Entities**: Pre-built base classes for entities with audit properties
- 🔒 **Soft Delete**: Built-in soft delete functionality
- ⏰ **Automatic Audit**: Automatic CreatedAt, UpdatedAt, DeletedAt tracking
- 👤 **User Tracking**: Automatic CreatedBy, UpdatedBy, DeletedBy tracking
- 🎯 **Domain Events**: Framework-agnostic domain event support (optional)
- 💉 **Dependency Injection**: Easy integration with DI containers
- 🔧 **Flexible User Context**: Works with any user service implementation

## Installation

```bash
dotnet add package FS.EntityFramework.Library
```

## Quick Start

### 1. Configure Services

#### Basic Setup (without audit)
```csharp
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddGenericUnitOfWork<YourDbContext>();
```

#### With Automatic Audit Support

**Option A: Using your existing user service**
```csharp
// If you have your own ICurrentUserService
services.AddScoped<ICurrentUserService, CurrentUserService>();

services.AddGenericUnitOfWorkWithAudit<YourDbContext>(
    provider => provider.GetRequiredService<ICurrentUserService>().UserId);
```

**Option B: Using HttpContext directly**
```csharp
services.AddHttpContextAccessor();

services.AddGenericUnitOfWorkWithAudit<YourDbContext>(
    provider =>
    {
        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    });
```

**Option C: Using IUserContext interface**
```csharp
public class MyUserContext : IUserContext
{
    public MyUserContext(ICurrentUserService currentUserService)
    {
        CurrentUser = currentUserService.UserId;
    }
    
    public string? CurrentUser { get; }
}

services.AddScoped<IUserContext, MyUserContext>();
services.AddGenericUnitOfWorkWithAudit<YourDbContext, MyUserContext>();
```

#### With Domain Events Support (Optional)

**Simple Setup - Automatic Handler Registration:**
```csharp
// All-in-one: Domain events + automatic handler registration from calling assembly
services.AddDomainEventsWithHandlers();

// Or from specific assembly
services.AddDomainEventsWithHandlers(typeof(ProductCreatedEvent).Assembly);

// With MediatR integration
services.AddDomainEventsWithHandlers<MediatRDomainEventDispatcher>(typeof(ProductCreatedEvent).Assembly);
```

**Advanced Setup - Manual Control:**
```csharp
// Basic domain events support
services.AddDomainEvents();

// Automatic handler registration from multiple assemblies
services.AddDomainEventHandlersFromAssemblies(
    typeof(ProductEvents).Assembly,
    typeof(OrderEvents).Assembly
);

// Or manual registration (still supported)
services.AddDomainEventHandler<ProductCreatedEvent, ProductCreatedEventHandler>();
services.AddDomainEventHandler<ProductUpdatedEvent, ProductUpdatedEventHandler>();

// Custom filter-based registration
services.AddDomainEventHandlers(
    typeof(ProductEvents).Assembly,
    type => type.Name.EndsWith("Handler") && !type.Name.Contains("Test"),
    ServiceLifetime.Scoped
);

// Attribute-based registration
services.AddAttributedDomainEventHandlers(typeof(ProductEvents).Assembly);
```

### 2. Create Your Entities

#### Basic Entity
```csharp
public class Product : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}

// When you save a product, these properties are automatically set:
// - CreatedAt: DateTime.UtcNow (when entity is first created)
// - CreatedBy: Current user ID (from your user context)
// - UpdatedAt: DateTime.UtcNow (when entity is modified)
// - UpdatedBy: Current user ID (when entity is modified)
// - IsDeleted: false/true (for soft deletes)
// - DeletedAt: DateTime.UtcNow (when entity is soft deleted)
// - DeletedBy: Current user ID (when entity is soft deleted)
```

#### Entity with Domain Events
```csharp
public class Product : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;

    // Factory method that raises domain events
    public static Product Create(string name, decimal price, string description)
    {
        var product = new Product
        {
            Name = name,
            Price = price,
            Description = description
        };

        // Raise domain event (completely optional)
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Price));
        
        return product;
    }

    public void UpdatePrice(decimal newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        
        // Raise domain event for price change
        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }
}
```

### 3. Use in Your Services

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(string name, decimal price, string description)
    {
        // Using factory method that raises domain events
        var product = Product.Create(name, price, description);
        
        var repository = _unitOfWork.GetRepository<Product, int>();
        await repository.AddAsync(product);
        
        // Domain events are automatically dispatched during SaveChanges
        await _unitOfWork.SaveChangesAsync();
        
        return product;
    }

    public async Task<IPaginate<Product>> GetProductsAsync(int page, int size)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        return await repository.GetPagedAsync(page, size);
    }
}
```

### 4. Dynamic Filtering

```csharp
var filter = new FilterModel
{
    SearchTerm = "laptop",
    Filters = new List<FilterItem>
    {
        new() { Field = "Price", Operator = "greaterthan", Value = "100" },
        new() { Field = "Name", Operator = "contains", Value = "gaming" }
    }
};

var products = await repository.GetPagedWithFilterAsync(filter, 1, 10);
```

### 5. Specification Pattern

```csharp
public class ExpensiveProductsSpecification : BaseSpecification<Product>
{
    public ExpensiveProductsSpecification(decimal minPrice)
    {
        AddCriteria(p => p.Price >= minPrice);
        AddInclude(p => p.Category);
        ApplyOrderByDescending(p => p.Price);
    }
}

// Usage
var spec = new ExpensiveProductsSpecification(1000);
var expensiveProducts = await repository.GetAsync(spec);
```

### 6. Soft Delete with Global Query Filters

```csharp
// In your DbContext's OnModelCreating method:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply global query filters to exclude soft-deleted entities
    modelBuilder.ApplySoftDeleteQueryFilters();
}

// Usage examples:
// Normal queries automatically exclude soft-deleted entities
var activeProducts = await repository.GetAllAsync(); // Only non-deleted products

// Include soft-deleted entities when needed
var allProducts = await repository.GetQueryable()
    .IncludeDeleted()
    .ToListAsync();

// Get only soft-deleted entities
var deletedProducts = await repository.GetQueryable()
    .OnlyDeleted()
    .ToListAsync();

// Soft delete (sets IsDeleted = true, keeps data in database)
await repository.DeleteAsync(productId, saveChanges: true, isSoftDelete: true);

// Hard delete (actually removes from database)
await repository.DeleteAsync(productId, saveChanges: true, isSoftDelete: false);
```

### 7. Domain Events

#### Create Domain Events
```csharp
// Domain event for product creation
public class ProductCreatedEvent : DomainEvent
{
    public ProductCreatedEvent(int productId, string productName, decimal price)
    {
        ProductId = productId;
        ProductName = productName;
        Price = price;
    }

    public int ProductId { get; }
    public string ProductName { get; }
    public decimal Price { get; }
}

// Domain event for price changes
public class ProductPriceChangedEvent : DomainEvent
{
    public ProductPriceChangedEvent(int productId, decimal oldPrice, decimal newPrice)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
    }

    public int ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
}
```

#### Create Event Handlers

**Basic Handler:**
```csharp
public class ProductCreatedEventHandler : IDomainEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;

    public ProductCreatedEventHandler(
        ILogger<ProductCreatedEventHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Handle(ProductCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Product created: {ProductName} with price: {Price}", 
            domainEvent.ProductName, domainEvent.Price);

        // Send notification email
        await _emailService.SendProductCreatedNotificationAsync(
            domainEvent.ProductName, 
            domainEvent.Price, 
            cancellationToken);
    }
}

public class ProductPriceChangedEventHandler : IDomainEventHandler<ProductPriceChangedEvent>
{
    private readonly INotificationService _notificationService;

    public ProductPriceChangedEventHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Handle(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Notify interested customers about price change
        await _notificationService.NotifyPriceChangeAsync(
            domainEvent.ProductId,
            domainEvent.OldPrice,
            domainEvent.NewPrice,
            cancellationToken);
    }
}
```

**Advanced Handler with Attributes:**
```csharp
[DomainEventHandler(ServiceLifetime = ServiceLifetime.Scoped, Order = 1)]
public class ProductCreatedAuditHandler : IDomainEventHandler<ProductCreatedEvent>
{
    private readonly IAuditService _auditService;

    public ProductCreatedAuditHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task Handle(ProductCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _auditService.LogEventAsync("ProductCreated", domainEvent, cancellationToken);
    }
}

// Handler for multiple events
public class GeneralAuditHandler : 
    IDomainEventHandler<ProductCreatedEvent>,
    IDomainEventHandler<ProductPriceChangedEvent>
{
    private readonly IAuditService _auditService;

    public GeneralAuditHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task Handle(ProductCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _auditService.LogEventAsync("ProductCreated", domainEvent, cancellationToken);
    }

    public async Task Handle(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _auditService.LogEventAsync("ProductPriceChanged", domainEvent, cancellationToken);
    }
}
```

**Registration Examples:**
```csharp
// Automatic registration - handlers are discovered and registered automatically
services.AddDomainEventsWithHandlers(); // Scans calling assembly

// Manual registration (still supported for fine control)
services.AddDomainEvents();
services.AddDomainEventHandler<ProductCreatedEvent, ProductCreatedEventHandler>();

// Attribute-based registration (for advanced control)
services.AddDomainEvents();
services.AddAttributedDomainEventHandlers(typeof(ProductCreatedEvent).Assembly);
```

#### Custom Domain Event Dispatcher (MediatR Integration)
```csharp
public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}

// Register in DI - Multiple options available:

// Option 1: All-in-one automatic setup
services.AddDomainEventsWithHandlers<MediatRDomainEventDispatcher>(typeof(ProductCreatedEvent).Assembly);

// Option 2: Manual setup with MediatR
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
services.AddDomainEvents<MediatRDomainEventDispatcher>();
services.AddDomainEventHandlersFromAssembly(typeof(ProductCreatedEvent).Assembly);

// Option 3: Basic setup with automatic handler discovery
services.AddDomainEventsWithHandlers();

```

#### Manual Domain Event Dispatch
```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ProductService(IUnitOfWork unitOfWork, IDomainEventDispatcher eventDispatcher)
    {
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        var product = await repository.GetByIdAsync(productId);
        
        if (product != null)
        {
            product.UpdatePrice(newPrice);
            await repository.UpdateAsync(product);
            
            // Manual dispatch if needed (automatic dispatch happens on SaveChanges)
            await _eventDispatcher.DispatchAsync(product.DomainEvents);
            product.ClearDomainEvents();
            
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
```

## API Reference

### IRepository<TEntity, TKey>

Core repository interface providing:

- Basic CRUD operations
- Advanced querying with includes and ordering
- Pagination support
- Bulk operations
- Dynamic filtering

### IUnitOfWork

Coordinates multiple repositories and provides:

- Repository access
- Transaction management
- Change tracking
- Bulk operations across repositories

### BaseSpecification<T>

Flexible specification implementation supporting:

- Complex criteria
- Include expressions
- Ordering
- Pagination
- Grouping

### Domain Events

Framework-agnostic domain event system providing:

- **IDomainEvent**: Marker interface for domain events
- **DomainEvent**: Base class for domain events with EventId and OccurredOn
- **IHasDomainEvents**: Interface for entities that can raise domain events
- **IDomainEventHandler<T>**: Generic interface for event handlers
- **IDomainEventDispatcher**: Interface for dispatching events to handlers
- **DomainEventInterceptor**: Automatic event dispatching during SaveChanges
- **Automatic Handler Discovery**: Multiple registration strategies for handlers
- **Attribute Support**: Advanced control with `[DomainEventHandler]` attribute
- **Multi-Event Handlers**: Single handler can process multiple event types

## Base Classes

- `BaseEntity<TKey>`: Simple entity with Id property and optional domain events support
- `BaseAuditableEntity<TKey>`: Entity with audit properties (CreatedAt, UpdatedAt, etc.) and domain events
- `ValueObject`: Base class for value objects with equality comparison

## Key Features

### **Domain Events Benefits:**
- ✅ **Framework Agnostic**: Works with any event handling library (MediatR, Mass Transit, etc.)
- ✅ **Completely Optional**: Use domain events only when needed
- ✅ **Automatic Discovery**: Handlers are automatically discovered and registered
- ✅ **Multiple Registration Options**: From simple one-liner to advanced attribute-based control
- ✅ **Automatic Dispatch**: Events are automatically dispatched during SaveChanges
- ✅ **Clean Architecture**: Promotes separation of concerns and loose coupling
- ✅ **Testable**: Easy to unit test domain logic and event handlers
- ✅ **Extensible**: Custom dispatchers and handlers for specific needs
- ✅ **Multi-Event Handlers**: Single handler can handle multiple event types
- ✅ **Handler Ordering**: Control execution order with attributes

### **Handler Registration Options:**
```csharp
// 1. Simplest - One line setup (recommended for most projects)
services.AddDomainEventsWithHandlers();

// 2. Specific assembly
services.AddDomainEventsWithHandlers(typeof(ProductEvents).Assembly);

// 3. Multiple assemblies
services.AddDomainEventHandlersFromAssemblies(
    typeof(ProductEvents).Assembly,
    typeof(OrderEvents).Assembly
);

// 4. Custom filtering
services.AddDomainEventHandlers(
    assembly,
    type => type.Name.EndsWith("Handler"),
    ServiceLifetime.Scoped
);

// 5. Attribute-based (advanced control)
services.AddAttributedDomainEventHandlers(assembly);

// 6. Manual (fine-grained control)
services.AddDomainEventHandler<ProductCreatedEvent, ProductCreatedEventHandler>();
```

### **Audit Features:**
- ✅ **Automatic Tracking**: CreatedAt, UpdatedAt, DeletedAt timestamps
- ✅ **User Tracking**: CreatedBy, UpdatedBy, DeletedBy user identification
- ✅ **Flexible User Context**: Works with any authentication system
- ✅ **Soft Delete Support**: Logical deletion with IsDeleted flag

### **Repository Features:**
- ✅ **Generic Implementation**: Works with any entity type
- ✅ **Dynamic Filtering**: Build complex queries from filter models
- ✅ **Specification Pattern**: Reusable and composable query specifications
- ✅ **Pagination**: Built-in pagination with metadata
- ✅ **Bulk Operations**: Efficient bulk insert, update, and delete operations

## Requirements

- .NET 9.0 or later
- Entity Framework Core 9.0.6 or later

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or have questions, please open an issue on our GitHub repository.