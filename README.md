# FS.EntityFramework.Library

[![NuGet Version](https://img.shields.io/nuget/v/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.EntityFramework.Library.svg)](https://www.nuget.org/packages/FS.EntityFramework.Library/)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.EntityFramework.Library)](https://github.com/furkansarikaya/FS.EntityFramework.Library/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.EntityFramework.Library.svg)](https://github.com/furkansarikaya/FS.EntityFramework.Library/stargazers)

A comprehensive Entity Framework Core library providing Repository pattern, Unit of Work, Specification pattern, dynamic filtering, and pagination support for .NET applications.

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

### 2. Create Your Entities

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

### 3. Use in Your Services

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var repository = _unitOfWork.GetRepository<Product, int>();
        await repository.AddAsync(product);
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

## Base Classes

- `BaseEntity<TKey>`: Simple entity with Id property
- `BaseAuditableEntity<TKey>`: Entity with audit properties (CreatedAt, UpdatedAt, etc.)
- `ValueObject`: Base class for value objects with equality comparison

## Requirements

- .NET 9.0 or later
- Entity Framework Core 9.0.6 or later

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or have questions, please open an issue on our GitHub repository.