# FS.EntityFramework.Library

A comprehensive Entity Framework Core library providing Repository pattern, Unit of Work, Specification pattern, dynamic filtering, and pagination support for .NET applications.

## Features

- 🏗️ **Repository Pattern**: Generic repository implementation with advanced querying capabilities
- 🔄 **Unit of Work Pattern**: Coordinate multiple repositories and manage transactions
- 📋 **Specification Pattern**: Flexible and reusable query specifications
- 🔍 **Dynamic Filtering**: Build dynamic queries from filter models
- 📄 **Pagination**: Built-in pagination support with metadata
- 🏷️ **Base Entities**: Pre-built base classes for entities with audit properties
- 🔒 **Soft Delete**: Built-in soft delete functionality
- 💉 **Dependency Injection**: Easy integration with DI containers

## Installation

```bash
dotnet add package FS.EntityFramework.Library
```

## Quick Start

### 1. Configure Services

```csharp
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddGenericUnitOfWork<YourDbContext>();
```

### 2. Create Your Entities

```csharp
public class Product : BaseAuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
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