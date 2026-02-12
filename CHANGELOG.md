# Changelog

All notable changes to this project will be documented in this file.

## [10.0.3] - 2026-02-12

### Security
- **FilterExpressionBuilder**: Field names are now validated with regex (`^[a-zA-Z][a-zA-Z0-9.]*$`) to prevent property traversal attacks
- **FilterExpressionBuilder**: Unknown properties now return `false` (safe default) instead of `true` (filter bypass)
- **FilterExpressionBuilder**: Invalid operators throw `ArgumentException` instead of silently returning `true`
- **DbContextSoftDeleteExtensions**: `EnableBypassSoftDelete()`, `DisableBypassSoftDelete()`, and `IsBypassSoftDeleteEnabled()` changed from `public` to `internal` to prevent unauthorized soft delete bypass
- **UnitOfWork**: Registered service list in error messages is now only shown in DEBUG builds to prevent information leakage

### Performance
- **UnitOfWork**: Fixed memory leak - removed `CreateScope()` call that was never disposed; uses `serviceProvider.GetService<>()` directly since UnitOfWork is already scoped
- **AuditInterceptor**: PropertyInfo lookups cached in `ConcurrentDictionary<(Type, string), PropertyInfo?>` to avoid repeated reflection per save
- **IdGenerationInterceptor**: `GetBaseEntityType` results cached in `ConcurrentDictionary<Type, Type?>`
- **IdGenerationInterceptor**: `GetProperty("Id")` results cached in `ConcurrentDictionary<Type, PropertyInfo?>`
- **IdGenerationInterceptor**: Optimized default value checks for common types (int, long, Guid, string) to avoid boxing via `Activator.CreateInstance`
- **DomainEventDispatcher**: `DispatchAsync(IEnumerable)` now dispatches events sequentially to preserve ordering (handlers within each event remain parallel)
- **UnitOfWork**: Removed unused `PerformCacheMaintenance()` dead code

### Bug Fixes
- **BaseRepository**: `SingleOrDefaultAsync<TResult>` now correctly calls `SingleOrDefaultAsync` instead of `SingleAsync`, which was throwing when no entity was found despite the "OrDefault" contract
- **BaseRepository**: `BulkDeleteAsync` now uses `typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity))` instead of fragile `GetProperty("IsDeleted")` reflection check
- **IRepository**: Updated `SingleOrDefaultAsync<TResult>` return type to `Task<TResult?>` to match OrDefault semantics
- **UnitOfWork**: Fixed `CommitTransactionAsync` NullReferenceException in finally block after rollback (transaction was already disposed and nulled by `RollbackTransactionAsync`)
- **DomainEventDispatcher**: Async handler exceptions are now caught individually per handler via `WrapHandlerTask`, preventing a single faulted handler from crashing the entire dispatch and losing error context
- **FilterExpressionBuilder**: Operator matching now uses `ToLowerInvariant()` instead of culture-sensitive `ToLower()` for consistent behavior across locales
- **AuditInterceptor**: Soft delete bypass check now runs before materializing change tracker entries to avoid unnecessary work
- **GuidV7 csproj**: Fixed release notes referencing v9.0.7 instead of current version
- **UlidGenerator csproj**: Fixed release notes referencing v9.0.7 instead of current version
- **GuidV7 csproj**: Fixed XML comment referencing .NET 9 instead of .NET 10
- **DomainRepository**: Fixed `ApplyIncludeChain` using element type instead of expression return type for `MakeGenericMethod`, causing `ArgumentException` when `IncludeCollection().ThenInclude()` chains were used (e.g., `IncludeCollection(o => o.Items).ThenInclude(i => i.Product)`)
- **DomainRepository**: Fixed `ApplyThenIncludeChain` using element type instead of expression return type for the third generic argument, causing `ArgumentException` when `ThenIncludeCollection()` chains were used
- **README.md**: Removed undocumented filter operators (`isnull`, `isnotnull`, `isempty`, `isnotempty`) that were never implemented

### Added
- **FilterOperator**: Type-safe enum with 15 operators and full IntelliSense support — eliminates string-based operator errors at compile time
- **FilterBuilder**: Fluent API for constructing filters (`FilterBuilder.Create().WhereEquals(...).WhereIsNull(...).Build()`)
- **FilterItem constructor**: New `FilterItem(string field, FilterOperator op, string? value)` constructor for type-safe filter creation
- **New filter operators**: `IsNull`, `IsNotNull`, `IsEmpty`, `IsNotEmpty`, `In`, `NotIn` — all fully implemented in FilterExpressionBuilder
- **Operator aliases**: Short aliases for all operators (`eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `sw`, `ew`)
- **WhereIf**: Conditional filter method in FilterBuilder for building dynamic filters from optional parameters
- **FSEntityFrameworkMetrics**: New opt-in OpenTelemetry-compatible metrics system using `System.Diagnostics.Metrics`
  - Repository operation counters, error counters, and duration histograms
  - UnitOfWork save/transaction/cache metrics
  - AuditInterceptor entity counters by state
  - IdGenerationInterceptor generation counters by key type
  - DomainEventDispatcher dispatch counters and duration histograms
- **WithMetrics()**: New fluent configuration extension to enable metrics (default: OFF)
- **DomainEventDispatcher**: Added null checks and try-catch for individual handler invocations

### Changed
- Version bumped to 10.0.3 across all packages
- Extension packages (GuidV7, UlidGenerator) reference FS.EntityFramework.Library 10.0.3

## [10.0.2] - Previous Release

Comprehensive Projection Support & New Features (see package release notes for details).
