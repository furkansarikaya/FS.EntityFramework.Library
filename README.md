# FS.EntityFramework.Library

[![NuGet Version](https://img.shields.io/nuget/v/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.EntityFramework.Library)](https://github.com/furkansarikaya/FS.EntityFramework.Library/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.EntityFramework.Library.svg)](https://github.com/furkansarikaya/FS.EntityFramework.Library/stargazers)

A comprehensive Entity Framework Core library providing Repository pattern, Unit of Work, Specification pattern, dynamic filtering, pagination support, **Domain Events**, and **Fluent Configuration API** for .NET applications.

## 📋 Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Configuration Methods](#configuration-methods)
  - [🆕 Fluent Configuration API (Recommended)](#-fluent-configuration-api-recommended)
  - [Classic Configuration](#classic-configuration)
- [Usage Examples](#usage-examples)
  - [Basic CRUD Operations](#basic-crud-operations)
  - [Dynamic Filtering](#dynamic-filtering)
  - [Specification Pattern](#specification-pattern)
  - [Pagination](#pagination)
  - [Unit of Work & Transactions](#unit-of-work--transactions)
  - [Soft Delete & Restore](#soft-delete--restore)
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
- 🔒 **Soft Delete & Restore**: Interface-based soft delete functionality with restore capability
- ⏰ **Automatic Audit**: Automatic CreatedAt, UpdatedAt, DeletedAt tracking
- 👤 **User Tracking**: Automatic CreatedBy, UpdatedBy, DeletedBy tracking
- 🎯 **Domain Events**: Framework-agnostic domain event support (optional)
- 🔗 **Fluent Configuration API**: Intuitive, chainable configuration (v9.0.6.2+)
- 💉 **Dependency Injection**: Easy integration with DI containers
- 🔧 **Flexible User Context**: Works with any user service implementation

## Installation

```bash
dotnet add package FS.EntityFramework.Library
```

## Configuration Methods

You can configure FS.EntityFramework.Library using either the new **Fluent Configuration API** (recommended) or the classic approach.

### 🆕 Fluent Configuration API (Recommended)

The Fluent Configuration API provides an intuitive, chainable way to configure the library with better readability and validation.

#### Basic Setup
```csharp
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddFSEntityFramework<YourDbContext>()
    .Build();
```

#### With Audit Support
```csharp
// Using HttpContext (for web applications)
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingHttpContext()
    .Build();

// Using custom user provider
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingUserProvider(provider => 
        {
            var userService = provider.GetService<ICurrentUserService>();
            return userService?.GetCurrentUserId();
        })
    .Build();

// Using interface-based approach
services.AddScoped<IUserContext, MyUserContext>();
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingUserContext<IUserContext>()
    .Build();

// For testing with static user
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingStaticUser("test-user-123")
    .Build();
```

#### With Domain Events
```csharp
// Basic domain events with auto handler discovery
services.AddFSEntityFramework<YourDbContext>()
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscovery()
    .Complete()
    .Build();

// With custom dispatcher (e.g., for MediatR integration)
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
services.AddFSEntityFramework<YourDbContext>()
    .WithDomainEvents()
        .UsingCustomDispatcher<YourCustomDomainEventDispatcher>()
        .WithAutoHandlerDiscovery(typeof(ProductCreatedEvent).Assembly)
    .Complete()
    .Build();

// Advanced handler registration options
services.AddFSEntityFramework<YourDbContext>()
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscoveryFromTypes(typeof(ProductCreatedEvent), typeof(OrderPlacedEvent))
        .WithAttributeBasedDiscovery(Assembly.GetExecutingAssembly())
        .WithCustomHandlerDiscovery(
            Assembly.GetExecutingAssembly(), 
            type => type.Name.EndsWith("Handler") && !type.Name.Contains("Test"),
            ServiceLifetime.Scoped)
        .WithHandler<ProductCreatedEvent, ProductCreatedEventHandler>()
    .Complete()
    .Build();
```

#### Complete Setup with All Features
```csharp
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingHttpContext()
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscovery()
        .WithAttributeBasedDiscovery(Assembly.GetExecutingAssembly())
    .Complete()
    .WithSoftDelete()
    .WithCustomRepository<Product, int, ProductRepository>()
    .WithRepositoriesFromAssembly(Assembly.GetExecutingAssembly())
    .ValidateConfiguration()
    .Build();
```

#### Conditional Configuration
```csharp
services.AddFSEntityFramework<YourDbContext>()
    .WithAudit()
        .UsingHttpContext()
    .When(isDevelopment, builder =>
        builder.WithDomainEvents()
            .UsingDefaultDispatcher()
            .WithAutoHandlerDiscovery()
        .Complete())
    .When(!isDevelopment, builder =>
        builder.WithServices(s => s.AddSingleton<ILoggingService, ProductionLoggingService>()))
    .ValidateConfiguration()
    .Build();
```

### Classic Configuration

The original configuration methods are still supported for backward compatibility:

#### Basic Setup (without audit)
```csharp
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddGenericUnitOfWork<YourDbContext>();
```

#### With Automatic Audit Support

**Option A: Using your existing user service**
```csharp
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

#### With Domain Events Support (Classic)

**Simple Setup - Automatic Handler Registration:**
```csharp
// All-in-one: Domain events + automatic handler registration from calling assembly
services.AddDomainEventsWithHandlers();

// Or from specific assembly
services.AddDomainEventsWithHandlers(typeof(ProductCreatedEvent).Assembly);
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

## Usage Examples

### Create Your Entities

#### Basic Auditable Entity
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
```

#### Entity with Soft Delete Support
```csharp
// Create a soft deletable entity by implementing ISoftDelete interface
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;

    // ISoftDelete properties - automatically handled by AuditInterceptor
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// When you delete a product:
// - IsDeleted: Set to true (logical deletion)
// - DeletedAt: DateTime.UtcNow (when entity is soft deleted)
// - DeletedBy: Current user ID (who performed the deletion)
```

#### Entity with Domain Events
```csharp
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;

    // ISoftDelete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

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

    public void Delete()
    {
        // Add domain event before deletion
        AddDomainEvent(new ProductDeletedEvent(Id, Name));
    }
}
```

### Basic CRUD Operations

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

    public async Task DeleteProductAsync(int productId)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        var product = await repository.GetByIdAsync(productId);
        
        if (product != null)
        {
            product.Delete(); // Raises domain event
            
            // Soft delete (if entity implements ISoftDelete)
            await repository.DeleteAsync(product, saveChanges: true);
        }
    }

    public async Task RestoreProductAsync(int productId)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        
        // Restore a soft-deleted product
        await repository.RestoreAsync(productId, saveChanges: true);
    }

    public async Task<IPaginate<Product>> GetProductsAsync(int page, int size)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        return await repository.GetPagedAsync(page, size);
    }
}
```

### Dynamic Filtering

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

### Specification Pattern

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

### Pagination

```csharp
// Basic pagination
var pagedProducts = await repository.GetPagedAsync(pageIndex: 1, pageSize: 10);

// Pagination with filtering and ordering
var pagedProducts = await repository.GetPagedAsync(
    pageIndex: 1, 
    pageSize: 10,
    predicate: p => p.Price > 100,
    orderBy: query => query.OrderBy(p => p.Name),
    includes: new List<Expression<Func<Product, object>>> { p => p.Category }
);

// Access pagination metadata
Console.WriteLine($"Total items: {pagedProducts.Count}");
Console.WriteLine($"Total pages: {pagedProducts.Pages}");
Console.WriteLine($"Has next page: {pagedProducts.HasNext}");
Console.WriteLine($"Has previous page: {pagedProducts.HasPrevious}");
```

### Unit of Work & Transactions

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateOrderWithProductsAsync(Order order, List<Product> products)
    {
        // Manual transaction management
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var orderRepository = _unitOfWork.GetRepository<Order, int>();
            var productRepository = _unitOfWork.GetRepository<Product, int>();
            
            await orderRepository.AddAsync(order);
            await productRepository.BulkInsertAsync(products);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Order> CreateOrderWithTransactionScopeAsync(Order order)
    {
        // Automatic transaction management
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var repository = _unitOfWork.GetRepository<Order, int>();
            await repository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return order;
        });
    }
}
```

### Soft Delete & Restore

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

// Soft delete (entity must implement ISoftDelete)
await repository.DeleteAsync(productId, saveChanges: true);
// Sets: IsDeleted = true, DeletedAt = DateTime.UtcNow, DeletedBy = currentUser

// Restore a soft-deleted entity
await repository.RestoreAsync(productId, saveChanges: true);
// Sets: IsDeleted = false, DeletedAt = null, DeletedBy = null

// Check if entity is soft deletable
if (typeof(ISoftDelete).IsAssignableFrom(typeof(Product)))
{
    // Entity supports soft delete operations
    await repository.RestoreAsync(productId);
}
```

### Domain Events

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

// Domain event for product deletion
public class ProductDeletedEvent : DomainEvent
{
    public ProductDeletedEvent(int productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
    }

    public int ProductId { get; }
    public string ProductName { get; }
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
    IDomainEventHandler<ProductDeletedEvent>
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

    public async Task Handle(ProductDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _auditService.LogEventAsync("ProductDeleted", domainEvent, cancellationToken);
    }
}
```

#### Custom Domain Event Dispatcher (Framework Integration)
```csharp
// Example: MediatR integration
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

// Register with Fluent API
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
services.AddFSEntityFramework<YourDbContext>()
    .WithDomainEvents()
        .UsingCustomDispatcher<MediatRDomainEventDispatcher>()
        .WithAutoHandlerDiscovery(typeof(ProductCreatedEvent).Assembly)
    .Complete()
    .Build();
```

## API Reference

### IRepository<TEntity, TKey>

Core repository interface providing:

- Basic CRUD operations with soft delete support
- **RestoreAsync**: Restore soft-deleted entities
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

### Fluent Configuration API

**Main Builder Interface:**
- **IFSEntityFrameworkBuilder**: Core fluent configuration interface

**Configuration Builders:**
- **IAuditConfigurationBuilder**: Configure audit functionality
- **IDomainEventsConfigurationBuilder**: Configure domain events

**Extension Methods:**
- **WithAudit()**: Enable audit tracking
- **WithDomainEvents()**: Enable domain events
- **WithSoftDelete()**: Enable soft delete query filters
- **WithCustomRepository()**: Register custom repositories
- **ValidateConfiguration()**: Validate setup

### Audit Interfaces

Type-safe audit implementation:

- **ICreationAuditableEntity**: Provides CreatedAt and CreatedBy properties
- **IModificationAuditableEntity**: Provides UpdatedAt and UpdatedBy properties
- **ISoftDelete**: Provides IsDeleted, DeletedAt, and DeletedBy properties

## Base Classes

- `BaseEntity<TKey>`: Simple entity with Id property and optional domain events support
- `BaseAuditableEntity<TKey>`: Entity with creation and modification audit properties and domain events
- `ValueObject`: Base class for value objects with equality comparison

## Key Features

### **🆕 Fluent Configuration API Benefits:**
- ✅ **Chainable Configuration**: Intuitive method chaining for clean setup
- ✅ **Framework Agnostic**: Use any event handling library (MediatR, MassTransit, custom)
- ✅ **Flexible User Providers**: HttpContext, delegates, interfaces, or static users
- ✅ **Auto-Discovery**: Automatic handler registration from assemblies
- ✅ **Built-in Validation**: Configuration validation with helpful error messages
- ✅ **Conditional Setup**: Environment-based configuration support
- ✅ **Backward Compatible**: Classic configuration still supported

### **Soft Delete & Restore Benefits:**
- ✅ **Interface-Based**: Clean separation using `ISoftDelete` interface
- ✅ **Type-Safe**: Compile-time checking for soft delete capabilities
- ✅ **Restore Functionality**: Built-in `RestoreAsync` methods to recover deleted entities
- ✅ **Global Query Filters**: Automatic exclusion of soft-deleted entities
- ✅ **Flexible Queries**: Include deleted or only deleted entity queries
- ✅ **Automatic Audit**: Automatic tracking of deletion and restoration

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

### **Configuration Comparison:**

| Feature | Fluent API | Classic API |
|---------|------------|-------------|
| Readability | ✅ Excellent | ⚠️ Good |
| Validation | ✅ Built-in | ❌ Manual |
| Chaining | ✅ Yes | ❌ No |
| Conditional Setup | ✅ Yes | ⚠️ Limited |
| Error Messages | ✅ Helpful | ⚠️ Generic |
| IDE Support | ✅ Excellent | ⚠️ Good |

### **Handler Registration Options:**
```csharp
// 1. Simplest - One line setup (recommended for most projects)
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscovery()
    .Complete()
    .Build();

// 2. Specific assembly
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .WithAutoHandlerDiscovery(typeof(ProductEvents).Assembly)
    .Complete()
    .Build();

// 3. Multiple assemblies
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .WithAutoHandlerDiscovery(
            typeof(ProductEvents).Assembly,
            typeof(OrderEvents).Assembly)
    .Complete()
    .Build();

// 4. Custom filtering
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .WithCustomHandlerDiscovery(
            assembly,
            type => type.Name.EndsWith("Handler"),
            ServiceLifetime.Scoped)
    .Complete()
    .Build();

// 5. Attribute-based (advanced control)
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .WithAttributeBasedDiscovery(assembly)
    .Complete()
    .Build();

// 6. Manual (fine-grained control)
services.AddFSEntityFramework<MyDbContext>()
    .WithDomainEvents()
        .WithHandler<ProductCreatedEvent, ProductCreatedEventHandler>()
    .Complete()
    .Build();
```

### **Audit Features:**
- ✅ **Interface-Based Design**: Type-safe audit implementation with specific interfaces
- ✅ **Automatic Tracking**: CreatedAt, UpdatedAt, DeletedAt timestamps
- ✅ **User Tracking**: CreatedBy, UpdatedBy, DeletedBy user identification
- ✅ **Flexible User Context**: Works with any authentication system
- ✅ **Granular Control**: Choose which audit features to implement per entity

### **Repository Features:**
- ✅ **Generic Implementation**: Works with any entity type
- ✅ **Soft Delete Support**: Built-in soft delete and restore operations
- ✅ **Dynamic Filtering**: Build complex queries from filter models
- ✅ **Specification Pattern**: Reusable and composable query specifications
- ✅ **Pagination**: Built-in pagination with metadata
- ✅ **Bulk Operations**: Efficient bulk insert, update, and delete operations

## Usage Patterns

### **Soft Delete Entity Creation:**
```csharp
// Option 1: Implement ISoftDelete on existing auditable entity
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    // Entity properties
    public string Name { get; set; } = string.Empty;
    
    // ISoftDelete properties (required)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Option 2: Only basic audit (no soft delete)
public class Category : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    // No ISoftDelete - hard deletes only
}

// Option 3: Minimal entity (no audit, no soft delete)
public class Tag : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    // Only basic entity with domain events support
}
```

### **Repository Usage with Soft Delete:**
```csharp
// Entities that implement ISoftDelete automatically support:
await repository.DeleteAsync(entity); // Soft delete
await repository.RestoreAsync(entity); // Restore
await repository.RestoreAsync(id); // Restore by ID

// Entities that don't implement ISoftDelete:
await repository.DeleteAsync(entity); // Hard delete only
// repository.RestoreAsync() throws InvalidOperationException
```

### **Migration from Classic to Fluent API:**

**Before (Classic):**
```csharp
services.AddGenericUnitOfWorkWithAudit<MyDbContext>(
    provider => provider.GetRequiredService<ICurrentUserService>().UserId);

services.AddDomainEvents();
services.AddDomainEventHandler<ProductCreatedEvent, ProductCreatedEventHandler>();
```

**After (Fluent):**
```csharp
services.AddFSEntityFramework<MyDbContext>()
    .WithAudit()
        .UsingUserProvider(provider => 
            provider.GetRequiredService<ICurrentUserService>().UserId)
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithHandler<ProductCreatedEvent, ProductCreatedEventHandler>()
    .Complete()
    .Build();
```

## Best Practices

### **Configuration Best Practices:**

1. **Use Fluent API for new projects:**
   ```csharp
   services.AddFSEntityFramework<MyDbContext>()
       .WithAudit()
           .UsingHttpContext()
       .ValidateConfiguration()
       .Build();
   ```

2. **Validate configuration in development:**
   ```csharp
   services.AddFSEntityFramework<MyDbContext>()
       .WithAudit()
           .UsingHttpContext()
       .When(isDevelopment, builder => builder.ValidateConfiguration())
       .Build();
   ```

3. **Use conditional configuration for different environments:**
   ```csharp
   services.AddFSEntityFramework<MyDbContext>()
       .WithAudit()
           .UsingHttpContext()
       .When(isProduction, builder =>
           builder.WithDomainEvents()
               .UsingCustomDispatcher<ProductionEventDispatcher>()
               .WithAutoHandlerDiscovery()
           .Complete())
       .Build();
   ```

### **Entity Design Best Practices:**

1. **Implement interfaces based on needs:**
   ```csharp
   // For entities that need audit + soft delete
   public class Product : BaseAuditableEntity<int>, ISoftDelete
   
   // For entities that only need audit
   public class Category : BaseAuditableEntity<int>
   
   // For simple entities
   public class Tag : BaseEntity<int>
   ```

2. **Use factory methods for domain events:**
   ```csharp
   public static Product Create(string name, decimal price)
   {
       var product = new Product { Name = name, Price = price };
       product.AddDomainEvent(new ProductCreatedEvent(product.Id, name));
       return product;
   }
   ```

### **Repository Usage Best Practices:**

1. **Use specifications for complex queries:**
   ```csharp
   public class ActiveExpensiveProductsSpec : BaseSpecification<Product>
   {
       public ActiveExpensiveProductsSpec(decimal minPrice)
       {
           AddCriteria(p => p.Price >= minPrice && !p.IsDeleted);
           AddInclude(p => p.Category);
           ApplyOrderByDescending(p => p.CreatedAt);
       }
   }
   ```

2. **Use Unit of Work for transactions:**
   ```csharp
   public async Task<Order> ProcessOrderAsync(CreateOrderRequest request)
   {
       return await _unitOfWork.ExecuteInTransactionAsync(async () =>
       {
           // Multiple repository operations
           var order = await CreateOrderAsync(request);
           await UpdateInventoryAsync(request.Items);
           await SendNotificationAsync(order);
           return order;
       });
   }
   ```

## Requirements

- .NET 9.0 or later
- Entity Framework Core 9.0.6 or later
- Microsoft.AspNetCore.Http.Abstractions 2.3.0 or later (for HttpContext support)

## Performance Considerations

### **Query Performance:**
- Use `disableTracking: true` for read-only operations
- Implement proper indexing for filtered properties
- Use specification pattern for complex reusable queries
- Consider pagination for large datasets

### **Domain Events Performance:**
- Events are dispatched synchronously during SaveChanges
- Consider async handlers for I/O operations
- Use background services for heavy processing
- Be mindful of handler execution order

### **Soft Delete Performance:**
- Global query filters automatically exclude soft-deleted entities
- Use indexes on IsDeleted column for better performance
- Consider archiving old soft-deleted records

## Troubleshooting

### **Common Configuration Issues:**

1. **DbContext not registered error:**
   ```csharp
   // Ensure DbContext is registered before AddFSEntityFramework
   services.AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString));
   services.AddFSEntityFramework<MyDbContext>(); // Then register FS.EntityFramework
   ```

2. **Handler not found errors:**
   ```csharp
   // Ensure handlers are in the scanned assembly
   services.AddFSEntityFramework<MyDbContext>()
       .WithDomainEvents()
           .WithAutoHandlerDiscovery(typeof(MyHandler).Assembly) // Specify correct assembly
       .Complete()
       .Build();
   ```

3. **Audit properties not being set:**
   ```csharp
   // Ensure audit is configured and user context is available
   services.AddFSEntityFramework<MyDbContext>()
       .WithAudit()
           .UsingHttpContext() // or other user provider
       .Build();
   ```

### **Debugging Tips:**

1. **Enable validation in development:**
   ```csharp
   services.AddFSEntityFramework<MyDbContext>()
       .ValidateConfiguration() // Will throw helpful errors
       .Build();
   ```

2. **Check registration with validation:**
   ```csharp
   services.AddFSEntityFramework<MyDbContext>()
       .WithServices(s => 
       {
           // Add debugging services
           s.AddSingleton<IValidationService, ValidationService>();
       })
       .ValidateConfiguration()
       .Build();
   ```

## 🤝 Contributing

We welcome contributions! This project is open source and benefits from community involvement:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

**Development Guidelines:**
- Follow existing code patterns and conventions
- Add comprehensive tests for new features
- Update documentation for any public API changes
- Ensure backward compatibility when possible
- Use the Fluent Configuration API for new features

**Areas for Contribution:**
- Additional domain event dispatchers (Mass Transit, NServiceBus, etc.)
- Performance optimizations
- Additional specification implementations
- Documentation improvements
- Example projects

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Changelog

### v9.0.6.5 - Added Fluent Configuration API
- **NEW**: Fluent Configuration API for intuitive setup with method chaining
- **NEW**: Flexible Domain Events support with custom dispatcher integration
- **NEW**: Enhanced Audit configuration with multiple user provider options
- **NEW**: Advanced repository registration with assembly scanning
- **NEW**: Conditional configuration support for different environments
- **NEW**: Comprehensive validation and error handling
- **IMPROVED**: Better separation of concerns and cleaner API design
- **IMPROVED**: Framework-agnostic approach - integrate with any event handling library
- **IMPROVED**: More flexible and extensible configuration options

**Breaking Changes**: None - Fully backward compatible with existing configurations

### v9.0.6.1 - Initial Release
- Repository pattern implementation
- Unit of Work pattern
- Specification pattern
- Dynamic filtering
- Pagination support
- Domain Events (basic)
- Audit tracking
- Soft delete functionality

## 🌟 Star History

If you find this library useful, please consider giving it a star on GitHub! It helps others discover the project.

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**

[![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white)](https://github.com/furkansarikaya)
[![LinkedIn](https://img.shields.io/badge/linkedin-%230077B5.svg?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/furkansarikaya/)
[![Medium](https://img.shields.io/badge/medium-%23121011.svg?style=for-the-badge&logo=medium&logoColor=white)](https://medium.com/@furkansarikaya)

---

## Support

If you encounter any issues or have questions:

1. Check the [troubleshooting section](#troubleshooting)
2. Search existing [GitHub issues](https://github.com/furkansarikaya/FS.EntityFramework.Library/issues)
3. Create a new issue with detailed information
4. Join our community discussions

**Happy coding! 🚀**