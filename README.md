# FS.EntityFramework.Library

[![NuGet Version](https://img.shields.io/nuget/v/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.EntityFramework.Library)](https://github.com/furkansarikaya/FS.EntityFramework.Library/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.EntityFramework.Library.svg)](https://github.com/furkansarikaya/FS.EntityFramework.Library/stargazers)

A comprehensive, production-ready Entity Framework Core library providing **Repository pattern**, **Unit of Work**, **Specification pattern**, **dynamic filtering**, **pagination support**, **Domain Events**, **Domain-Driven Design (DDD)**, **Fluent Configuration API**, and **modular ID generation** strategies for .NET applications.

## 🌟 Why Choose FS.EntityFramework.Library?

This library transforms Entity Framework Core into a powerful, enterprise-ready data access layer that follows best practices and design patterns. Whether you're building a simple application or a complex domain-rich system, this library provides the tools you need to create maintainable, testable, and scalable data access code.

## 📋 Table of Contents

- [🚀 Quick Start](#-quick-start)
- [💾 Installation](#-installation)
- [🏗️ Step-by-Step Implementation Guide](#️-step-by-step-implementation-guide)
- [🏛️ Domain-Driven Design Features](#️-domain-driven-design-features)
- [📊 Advanced Features](#-advanced-features)
- [🎯 Best Practices](#-best-practices)
- [🔧 Troubleshooting](#-troubleshooting)
- [🤝 Contributing](#-contributing)

## 🚀 Quick Start

Get started with FS.EntityFramework.Library in just 3 steps:

### Step 1: Install the Package

```bash
dotnet add package FS.EntityFramework.Library
```

### Step 2: Configure Your DbContext

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
}
```

### Step 3: Configure Services

```csharp
// In Program.cs or Startup.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add FS.EntityFramework services
services.AddFSEntityFramework<ApplicationDbContext>()
    .Build();
```

### Step 4: Create Your First Entity

```csharp
public class Product : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

### Step 5: Use in Your Services

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Product> CreateProductAsync(string name, decimal price)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        var product = new Product { Name = name, Price = price };
        
        await repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
        
        return product;
    }
}
```

## 💾 Installation

### Core Package

```bash
# Core library with all essential features including DDD
dotnet add package FS.EntityFramework.Library
```

### Extension Packages (Optional)

```bash
# GUID Version 7 ID generation (.NET 9+)
dotnet add package FS.EntityFramework.Library.GuidV7

# ULID ID generation
dotnet add package FS.EntityFramework.Library.UlidGenerator
```

### Requirements

- **.NET 9.0** or later
- **Entity Framework Core 9.0.7** or later
- **Microsoft.AspNetCore.Http.Abstractions 2.3.0** or later (for HttpContext support)

## 🏗️ Step-by-Step Implementation Guide

Let's build a complete example from scratch, implementing all the major features of the library.

### Step 1: Set Up Your Project Structure

First, create a new project and organize it following clean architecture principles:

```
YourProject/
├── Models/           # Entity models
├── Services/         # Business logic
├── Repositories/     # Custom repositories (if needed)
└── Configuration/    # Database configuration
```

### Step 2: Install Required Packages

```bash
dotnet new webapi -n YourProject
cd YourProject
dotnet add package FS.EntityFramework.Library
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Step 3: Create Base Entities

Understanding the entity hierarchy is crucial. The library provides several base entity classes:

```csharp
// Models/Category.cs
using FS.EntityFramework.Library.Common;

/// <summary>
/// Simple entity with just ID and domain events support
/// </summary>
public class Category : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation property
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

// Models/Product.cs
using FS.EntityFramework.Library.Common;

/// <summary>
/// Auditable entity with creation and modification tracking
/// </summary>
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    
    // Navigation property
    public virtual Category Category { get; set; } = null!;
    
    // ISoftDelete properties (automatically implemented)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Business method with domain events
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be positive", nameof(newPrice));
            
        var oldPrice = Price;
        Price = newPrice;
        
        // Raise domain event
        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }
}
```

### Step 4: Create Domain Events

Domain events enable loose coupling between different parts of your application:

```csharp
// Models/Events/ProductPriceChangedEvent.cs
using FS.EntityFramework.Library.Common;

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

// Services/EventHandlers/ProductPriceChangedEventHandler.cs
using FS.EntityFramework.Library.Events;

public class ProductPriceChangedEventHandler : IDomainEventHandler<ProductPriceChangedEvent>
{
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;
    
    public ProductPriceChangedEventHandler(ILogger<ProductPriceChangedEventHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task Handle(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Product {ProductId} price changed from {OldPrice} to {NewPrice}", 
            domainEvent.ProductId, domainEvent.OldPrice, domainEvent.NewPrice);
        
        // Add your business logic here:
        // - Send price change notification emails
        // - Update related data
        // - Trigger other business processes
        
        await Task.CompletedTask;
    }
}
```

### Step 5: Configure Your DbContext

You have two options for DbContext configuration:

#### Option A: Use FSDbContext (Recommended)

```csharp
// Data/ApplicationDbContext.cs
using FS.EntityFramework.Library.Common;

public class ApplicationDbContext : FSDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider) 
        : base(options, serviceProvider)
    {
        // FSDbContext automatically applies all FS.EntityFramework configurations
    }
    
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // This applies FS configurations
        
        // Add your custom configurations here
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId);
        });
        
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });
    }
}
```

#### Option B: Use Regular DbContext with Manual Configuration

```csharp
// Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider) 
        : base(options)
    {
        _serviceProvider = serviceProvider;
    }
    
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply FS.EntityFramework configurations manually
        if (_serviceProvider != null)
        {
            modelBuilder.ApplyFSEntityFrameworkConfigurations(_serviceProvider);
        }
        
        // Your entity configurations...
    }
}
```

### Step 6: Configure Services with Fluent API

The Fluent Configuration API provides a clean way to configure all features:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure FS.EntityFramework with all features
builder.Services.AddFSEntityFramework<ApplicationDbContext>()
    // Enable audit tracking
    .WithAudit()
        .UsingHttpContext() // For web applications
    
    // Enable domain events
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscovery() // Automatically find event handlers
    .Complete()
    
    // Enable soft delete
    .WithSoftDelete()
    
    // Build the configuration
    .Build();

var app = builder.Build();
```

### Step 7: Create Business Services

Now create services that use the repository pattern:

```csharp
// Services/ProductService.cs
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IUnitOfWork unitOfWork, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Description = request.Description,
            CategoryId = request.CategoryId
        };
        
        await repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Created product: {ProductName}", product.Name);
        return product;
    }
    
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        return await repository.GetByIdAsync(id);
    }
    
    public async Task<IPaginate<Product>> GetProductsPagedAsync(int page, int size)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        
        return await repository.GetPagedAsync(
            pageIndex: page,
            pageSize: size,
            includes: new List<Expression<Func<Product, object>>> { p => p.Category },
            orderBy: query => query.OrderBy(p => p.Name)
        );
    }
    
    public async Task UpdateProductPriceAsync(int id, decimal newPrice)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        var product = await repository.GetByIdAsync(id);
        
        if (product == null)
            throw new InvalidOperationException($"Product with ID {id} not found");
        
        product.UpdatePrice(newPrice); // This will raise a domain event
        
        await repository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync(); // Domain events will be dispatched here
    }
    
    public async Task SoftDeleteProductAsync(int id)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        var product = await repository.GetByIdAsync(id);
        
        if (product != null)
        {
            await repository.DeleteAsync(product); // Soft delete
            await _unitOfWork.SaveChangesAsync();
        }
    }
    
    public async Task RestoreProductAsync(int id)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        await repository.RestoreAsync(id); // Restore soft deleted product
        await _unitOfWork.SaveChangesAsync();
    }
}

// DTOs for service methods
public record CreateProductRequest(string Name, decimal Price, string Description, int CategoryId);
```

### Step 8: Implement Dynamic Filtering

The library provides powerful dynamic filtering capabilities:

```csharp
// Services/ProductSearchService.cs
using FS.EntityFramework.Library.Models;

public class ProductSearchService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductSearchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IPaginate<Product>> SearchProductsAsync(ProductFilterRequest request)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        
        var filter = new FilterModel
        {
            SearchTerm = request.SearchTerm, // Searches across all string properties
            Filters = new List<FilterItem>()
        };
        
        // Add price range filtering
        if (request.MinPrice.HasValue)
        {
            filter.Filters.Add(new FilterItem
            {
                Field = nameof(Product.Price),
                Operator = "greaterthanorequal",
                Value = request.MinPrice.Value.ToString()
            });
        }
        
        if (request.MaxPrice.HasValue)
        {
            filter.Filters.Add(new FilterItem
            {
                Field = nameof(Product.Price),
                Operator = "lessthanorequal",
                Value = request.MaxPrice.Value.ToString()
            });
        }
        
        // Add category filtering
        if (request.CategoryId.HasValue)
        {
            filter.Filters.Add(new FilterItem
            {
                Field = nameof(Product.CategoryId),
                Operator = "equals",
                Value = request.CategoryId.Value.ToString()
            });
        }
        
        return await repository.GetPagedWithFilterAsync(
            filter,
            request.Page,
            request.PageSize,
            orderBy: query => query.OrderBy(p => p.Name),
            includes: new List<Expression<Func<Product, object>>> { p => p.Category }
        );
    }
}

public record ProductFilterRequest(
    string? SearchTerm = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? CategoryId = null,
    int Page = 1,
    int PageSize = 10);
```

### Step 9: Create API Controllers

Finally, create controllers that expose your services:

```csharp
// Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ProductSearchService _searchService;
    
    public ProductsController(ProductService productService, ProductSearchService searchService)
    {
        _productService = productService;
        _searchService = searchService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }
    
    [HttpGet]
    public async Task<ActionResult<IPaginate<Product>>> GetProducts(int page = 1, int size = 10)
    {
        var products = await _productService.GetProductsPagedAsync(page, size);
        return Ok(products);
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IPaginate<Product>>> SearchProducts([FromQuery] ProductFilterRequest request)
    {
        var products = await _searchService.SearchProductsAsync(request);
        return Ok(products);
    }
    
    [HttpPut("{id}/price")]
    public async Task<IActionResult> UpdateProductPrice(int id, [FromBody] decimal newPrice)
    {
        await _productService.UpdateProductPriceAsync(id, newPrice);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.SoftDeleteProductAsync(id);
        return NoContent();
    }
    
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        await _productService.RestoreProductAsync(id);
        return NoContent();
    }
}
```

### Step 10: Register Services

Don't forget to register your custom services:

```csharp
// Program.cs (continued)
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductSearchService>();
```

## 🏛️ Domain-Driven Design Features

The library provides comprehensive support for Domain-Driven Design patterns.

### Aggregate Roots

Aggregate Roots are the entry points to your aggregates and ensure consistency boundaries:

```csharp
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Domain;

public class OrderAggregate : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public DateTime OrderDate { get; private set; }
    
    // Read-only access to items
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    // Factory method enforcing business rules
    public static OrderAggregate Create(string orderNumber)
    {
        DomainGuard.AgainstNullOrWhiteSpace(orderNumber, nameof(orderNumber));
        
        var order = new OrderAggregate(Guid.CreateVersion7())
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 0
        };
        
        // Raise domain event
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, orderNumber));
        
        return order;
    }
    
    // Business method with domain logic
    public void AddItem(string productName, decimal unitPrice, int quantity)
    {
        DomainGuard.AgainstNullOrWhiteSpace(productName, nameof(productName));
        DomainGuard.AgainstNegativeOrZero(unitPrice, nameof(unitPrice));
        DomainGuard.AgainstNegativeOrZero(quantity, nameof(quantity));
        
        var item = new OrderItem(productName, unitPrice, quantity);
        _items.Add(item);
        
        RecalculateTotal();
        RaiseDomainEvent(new OrderItemAddedEvent(Id, productName, quantity));
    }
    
    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.TotalPrice);
    }
}

public class OrderItem
{
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    public decimal TotalPrice => UnitPrice * Quantity;
    
    public OrderItem(string productName, decimal unitPrice, int quantity)
    {
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
```

### Value Objects

Value Objects encapsulate business concepts and ensure type safety:

```csharp
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Domain;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency = "USD")
    {
        DomainGuard.AgainstNegative(amount, nameof(amount));
        DomainGuard.AgainstNullOrWhiteSpace(currency, nameof(currency));
        
        Amount = amount;
        Currency = currency;
    }
    
    public static Money Zero => new(0);
    public static Money FromDecimal(decimal amount) => new(amount);
    
    // Value object operations
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        
        return new Money(Amount + other.Amount, Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    // Operators
    public static Money operator +(Money left, Money right) => left.Add(right);
}
```

### Business Rules

Implement business rules for domain validation:

```csharp
using FS.EntityFramework.Library.Domain;

public class OrderMustHaveItemsRule : BusinessRule
{
    private readonly IReadOnlyCollection<OrderItem> _items;
    
    public OrderMustHaveItemsRule(IReadOnlyCollection<OrderItem> items)
    {
        _items = items;
    }
    
    public override bool IsBroken() => _items.Count == 0;
    
    public override string Message => "Order must have at least one item";
    
    public override string ErrorCode => "ORDER_NO_ITEMS";
}

// Usage in aggregate
public void ProcessOrder()
{
    CheckRule(new OrderMustHaveItemsRule(_items));
    
    // Process the order...
}
```

### Domain Specifications

Build reusable domain logic with specifications:

```csharp
using FS.EntityFramework.Library.Domain;

public class ExpensiveProductsSpecification : DomainSpecification<Product>
{
    private readonly decimal _minimumPrice;
    
    public ExpensiveProductsSpecification(decimal minimumPrice)
    {
        _minimumPrice = minimumPrice;
    }
    
    public override bool IsSatisfiedBy(Product candidate)
    {
        return candidate.Price >= _minimumPrice;
    }
    
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Price >= _minimumPrice;
    }
}

// Usage
var expensiveSpec = new ExpensiveProductsSpecification(100);
var expensiveProducts = await repository.FindAsync(expensiveSpec.ToExpression());
```

## 📊 Advanced Features

### ID Generation Extensions

The library supports modular ID generation strategies:

#### GUID Version 7 (Requires extension package)

```csharp
// Install: dotnet add package FS.EntityFramework.Library.GuidV7

services.AddFSEntityFramework<ApplicationDbContext>()
    .WithGuidV7() // Automatic GUID V7 generation
    .Build();

// Entity with GUID V7
public class User : BaseAuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // ID will be automatically generated as GUID V7
}
```

#### ULID (Requires extension package)

```csharp
// Install: dotnet add package FS.EntityFramework.Library.UlidGenerator

services.AddFSEntityFramework<ApplicationDbContext>()
    .WithUlid() // Automatic ULID generation
    .Build();

// Entity with ULID
public class Order : BaseAuditableEntity<Ulid>
{
    public string OrderNumber { get; set; } = string.Empty;
    
    // ID will be automatically generated as ULID
}
```

### Advanced Audit Configuration

Configure audit tracking with different user context providers:

```csharp
// Web applications with HttpContext
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithAudit()
        .UsingHttpContext() // Uses NameIdentifier claim
    .Build();

// Custom user provider
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithAudit()
        .UsingUserProvider(provider =>
        {
            var userService = provider.GetService<ICurrentUserService>();
            return userService?.GetCurrentUserId();
        })
    .Build();

// Interface-based user context
public class ApplicationUserContext : IUserContext
{
    private readonly ICurrentUserService _userService;
    
    public ApplicationUserContext(ICurrentUserService userService)
    {
        _userService = userService;
    }
    
    public string? CurrentUser => _userService.GetCurrentUserId();
}

services.AddScoped<IUserContext, ApplicationUserContext>();
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithAudit()
        .UsingUserContext<IUserContext>()
    .Build();
```

### Comprehensive Configuration Example

Here's a full-featured configuration example:

```csharp
services.AddFSEntityFramework<ApplicationDbContext>()
    // Audit Configuration
    .WithAudit()
        .UsingHttpContext() // User tracking via HTTP context
    
    // Domain Events Configuration
    .WithDomainEvents()
        .UsingDefaultDispatcher() // Default event dispatcher
        .WithAutoHandlerDiscovery() // Auto-discover event handlers
    .Complete()
    
    // Soft Delete Configuration
    .WithSoftDelete()
    
    // ID Generation Configuration
    .WithIdGeneration()
        .WithGenerator<string, CustomStringIdGenerator>()
    .Complete()
    
    // Validation & Build
    .ValidateConfiguration()
    .Build();
```

## 🎯 Best Practices

### Entity Design Guidelines

Follow these guidelines when designing your entities:

```csharp
// ✅ Good: Well-designed entity
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    // Private setters for business logic enforcement
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    
    // Public properties for simple data
    public string Description { get; set; } = string.Empty;
    
    // Soft delete properties (automatic)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Factory method for creation
    public static Product Create(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (price <= 0)
            throw new ArgumentException("Price must be positive", nameof(price));
        
        var product = new Product();
        product.SetName(name);
        product.SetPrice(price);
        
        // Raise domain event
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, price));
        
        return product;
    }
    
    // Business methods with validation
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        Name = name;
    }
    
    public void SetPrice(decimal price)
    {
        if (price <= 0)
            throw new ArgumentException("Price must be positive", nameof(price));
        
        var oldPrice = Price;
        Price = price;
        
        if (oldPrice != price)
        {
            AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, price));
        }
    }
}
```

### Service Layer Patterns

Implement clean service layer patterns:

```csharp
// ✅ Good: Service with proper separation of concerns
public class ProductApplicationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductApplicationService> _logger;
    
    public ProductApplicationService(
        IUnitOfWork unitOfWork, 
        ILogger<ProductApplicationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<ProductDto> CreateProductAsync(CreateProductCommand command)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Product name is required");
        
        var repository = _unitOfWork.GetRepository<Product, int>();
        
        // Business logic
        var product = Product.Create(command.Name, command.Price);
        
        // Persistence
        await repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(); // Domain events dispatched here
        
        _logger.LogInformation("Created product {ProductId}: {ProductName}", 
            product.Id, product.Name);
        
        // Return DTO
        return new ProductDto(product.Id, product.Name, product.Price);
    }
}
```

### Error Handling Best Practices

Implement comprehensive error handling:

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    
    public async Task<Product> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        try
        {
            var repository = _unitOfWork.GetRepository<Product, int>();
            var product = await repository.GetByIdAsync(id);
            
            if (product == null)
            {
                throw new ProductNotFoundException($"Product with ID {id} was not found");
            }
            
            // Update with business logic
            product.SetName(request.Name);
            product.SetPrice(request.Price);
            
            await repository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            
            return product;
        }
        catch (ProductNotFoundException)
        {
            _logger.LogWarning("Attempted to update non-existent product {ProductId}", id);
            throw; // Re-throw domain exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            throw new ProductUpdateException($"Failed to update product {id}", ex);
        }
    }
}

// Custom exception classes
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(string message) : base(message) { }
}

public class ProductUpdateException : Exception
{
    public ProductUpdateException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

## 🔧 Troubleshooting

### Common Issues and Solutions

#### Issue: Domain Events Not Being Dispatched

**Problem:** Domain events are not being handled even though handlers are registered.

**Solution:** Ensure you're using the domain unit of work or have properly configured event dispatching:

```csharp
// ❌ Wrong: Using regular SaveChanges
await _unitOfWork.SaveChangesAsync(); // Events might not be dispatched

// ✅ Correct: Ensure domain events are configured
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithDomainEvents()
        .UsingDefaultDispatcher()
        .WithAutoHandlerDiscovery()
    .Complete()
    .Build();
```

#### Issue: Soft Delete Not Working

**Problem:** Entities are being hard deleted instead of soft deleted.

**Solution:** Ensure entity implements `ISoftDelete` and soft delete is configured:

```csharp
// ✅ Entity must implement ISoftDelete
public class Product : BaseAuditableEntity<int>, ISoftDelete
{
    // ISoftDelete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// ✅ Configure soft delete
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithSoftDelete()
    .Build();
```

#### Issue: Audit Properties Not Being Set

**Problem:** `CreatedAt`, `CreatedBy`, etc., are not being populated automatically.

**Solution:** Ensure audit configuration is properly set up:

```csharp
// ✅ Configure audit with user provider
services.AddFSEntityFramework<ApplicationDbContext>()
    .WithAudit()
        .UsingHttpContext() // or another user provider
    .Build();
```

#### Issue: Repository Not Found

**Problem:** `InvalidOperationException` when trying to get a repository.

**Solution:** Ensure your DbContext is properly registered before adding FS.EntityFramework:

```csharp
// ✅ Register DbContext first
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ✅ Then add FS.EntityFramework
services.AddFSEntityFramework<ApplicationDbContext>()
    .Build();
```

### Performance Optimization Tips

#### Use Projections for Read-Only Data

```csharp
// ✅ Use projections for better performance
public async Task<IEnumerable<ProductSummaryDto>> GetProductSummariesAsync()
{
    var repository = _unitOfWork.GetRepository<Product, int>();
    
    return await repository.GetQueryable()
        .Select(p => new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        })
        .ToListAsync();
}
```

#### Disable Tracking for Read-Only Operations

```csharp
// ✅ Disable tracking for read-only queries
var products = await repository.GetQueryable(disableTracking: true)
    .Where(p => p.Price > 100)
    .ToListAsync();
```

#### Use Bulk Operations for Large Data Sets

```csharp
// ✅ Use bulk operations for better performance
await repository.BulkInsertAsync(products, saveChanges: true);
```

## 🤝 Contributing

We welcome contributions! This project is open source and benefits from community involvement.

### Areas for Contribution

- 🏛️ **Enhanced DDD patterns** (Saga patterns, Event Sourcing support)
- 🔌 **Additional domain event dispatchers** (Mass Transit, NServiceBus, etc.)
- ⚡ **Performance optimizations** for aggregate loading and persistence
- 📋 **Advanced specification implementations**
- 📚 **Documentation and examples**
- 🧪 **Test coverage improvements**
- 🔑 **New ID generation strategies**
- 🎯 **Domain modeling tools and utilities**

### Code Style

- Use **meaningful domain language** in code
- Follow **DDD naming conventions**
- Add **XML documentation** for public APIs
- Include **unit tests** for domain logic
- Follow **SOLID principles** and **DDD patterns**

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

## 🌟 Acknowledgments

- Thanks to all contributors who have helped make this library better
- Inspired by **Domain-Driven Design** principles by Eric Evans
- Built on top of the excellent **Entity Framework Core**
- Special thanks to the .NET community for continuous feedback and support

---

## 📞 Support

If you encounter any issues or have questions:

1. Check the [troubleshooting section](#-troubleshooting)
2. Search existing [GitHub issues](https://github.com/furkansarikaya/FS.EntityFramework.Library/issues)
3. Create a new issue with detailed information
4. Join our community discussions

**Happy Domain Modeling! 🏛️**

---

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**

[![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white)](https://github.com/furkansarikaya)
[![LinkedIn](https://img.shields.io/badge/linkedin-%230077B5.svg?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/furkansarikaya/)
[![Medium](https://img.shields.io/badge/medium-%23121011.svg?style=for-the-badge&logo=medium&logoColor=white)](https://medium.com/@furkansarikaya)