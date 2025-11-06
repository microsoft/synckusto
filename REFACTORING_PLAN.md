# SyncKusto Refactoring Plan

## Executive Summary

This document outlines a comprehensive plan to refactor the SyncKusto application from a monolithic Windows Forms application into a modular, testable architecture that separates business logic from UI concerns. The refactoring will improve maintainability and testability.

## Current State Analysis

### Project Structure
- **SyncKusto** (Main Project): Windows Forms application (.NET 8, Windows-only)
  - Contains ~70 files mixing UI and business logic
  - Tightly coupled to Windows Forms controls and UI patterns
- **SyncKusto.Tests** (Test Project): Minimal test coverage
  - Only contains `DictionaryExtensionTests.cs`

### Key Architectural Characteristics

#### Existing Good Practices

1. **Interface-based abstractions** (partially implemented)
   - `IDatabaseSchemaBuilder` with multiple implementations
   - `IDatabaseSchema` for schema representation
   - `IKustoSchema` for Kusto objects
   - `IOperationError` for error representation

2. **Builder Pattern**
   - `KustoDatabaseSchemaBuilder`, `FileDatabaseSchemaBuilder`, `EmptyDatabaseSchemaBuilder`
   - Each handles a different source type

3. **Strategy Pattern** (implicit)
   - Different authentication modes
   - Different source selection strategies

### Current Problems

1. **UI and Business Logic Entanglement**
   - `MainForm.cs` contains core comparison and sync logic
   - Business operations directly call `MessageBox.Show()`
   - `FileDatabaseSchemaBuilder` references `Windows.Forms.MessageBox`
   - Progress reporting tightly coupled to UI controls

2. **Hard Dependencies**
   - `QueryEngine` constructor creates Kusto clients directly
   - `SettingsWrapper` is a static class reading from disk
   - No dependency injection
   - Direct instantiation of dependencies throughout

3. **Limited Testability**
   - Business logic embedded in event handlers
   - No interfaces for key orchestration components
   - Static dependencies difficult to mock
   - Side effects (file I/O, network calls) not abstracted

4. **Monolithic Responsibilities**
   - `MainForm` handles: UI, validation, schema loading, comparison, persistence, error handling
   - `QueryEngine` handles: connection management, schema retrieval, CRUD operations
   - `SchemaPickerControl` handles: UI, validation, schema loading, settings management

5. **Overcomplicated Error Handling and Validation**
   - Current use of `Either<L, R>` and `Option<T>` adds unnecessary complexity
   - Specification pattern is overkill for simple validation needs
   - `NonEmptyString`/`INonEmptyStringState` state machine for string validation
   - For a small, controlled codebase, standard .NET exceptions are more appropriate
   - .NET's nullable reference types provide sufficient null safety

## Target Architecture

### Project Structure

```
SyncKusto.sln
??? SyncKusto.Core/                    # Core business logic (cross-platform)
?   ??? Abstractions/                  # Interfaces and contracts
?   ??? Models/                        # Domain models
?   ??? Services/                      # Business services
?   ??? Configuration/                 # Configuration models
?   ??? Exceptions/                    # Custom exception types
?
??? SyncKusto.Kusto/                   # Kusto-specific implementations
?   ??? Services/                      # Kusto schema operations
?   ??? Models/                        # Kusto-specific models
?   ??? Authentication/                # Kusto authentication
?   ??? Exceptions/                    # Kusto-specific exceptions
?
??? SyncKusto.FileSystem/              # File system implementations
?   ??? Services/                      # File-based schema operations
?   ??? Exceptions/                    # File system exceptions
?
??? SyncKusto.UI.WinForms/             # Windows Forms UI
?   ??? Forms/                         # Form implementations
?   ??? Controls/                      # User controls
?   ??? ViewModels/                    # Presentation logic
?   ??? Adapters/                      # UI to Core adapters
?
??? SyncKusto.Core.Tests/              # Core logic tests
??? SyncKusto.Kusto.Tests/             # Kusto implementation tests
??? SyncKusto.Integration.Tests/       # End-to-end tests
```

## Refactoring Principles

### 1. Dependency Inversion Principle
- **Program to interfaces, not implementations**
- All dependencies should be injected via constructors
- Use interfaces for all cross-boundary communications
- Avoid static classes for stateful or I/O operations

### 2. Single Responsibility Principle
- Each class should have one reason to change
- Separate concerns: orchestration, persistence, transformation, presentation
- Extract UI logic from business logic

### 3. Separation of Concerns
- **Core Logic**: Platform-agnostic, no UI dependencies
- **Infrastructure**: Kusto/File system implementations
- **Presentation**: UI-specific code only

### 4. Testability First
- All business logic should be testable without UI
- Mock-friendly interfaces
- Avoid sealed classes and static methods in core logic
- Prefer pure functions where possible

### 5. Modern .NET Patterns
- Use standard .NET exception handling
- Use nullable reference types for optional values
- Use simple, direct validation (no Specification pattern)
- Side effects pushed to boundaries (I/O, UI)
- Immutable data structures where appropriate

## Core Abstractions to Define

### 1. Custom Exception Types

```csharp
// SyncKusto.Core/Exceptions/SyncKustoException.cs
/// <summary>
/// Base exception for all SyncKusto operations
/// </summary>
public abstract class SyncKustoException : Exception
{
    protected SyncKustoException(string message) : base(message) { }
    protected SyncKustoException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// SyncKusto.Core/Exceptions/SchemaLoadException.cs
public class SchemaLoadException : SyncKustoException
{
    public SchemaLoadException(string message) : base(message) { }
    public SchemaLoadException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// SyncKusto.Core/Exceptions/SchemaValidationException.cs
public class SchemaValidationException : SyncKustoException
{
    public IReadOnlyList<string> ValidationErrors { get; }
    
    public SchemaValidationException(string message, IReadOnlyList<string> validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors;
    }
}

// SyncKusto.Core/Exceptions/SchemaSyncException.cs
public class SchemaSyncException : SyncKustoException
{
    public SchemaSyncException(string message) : base(message) { }
    public SchemaSyncException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

### 2. Schema Repository Pattern

```csharp
// SyncKusto.Core/Abstractions/ISchemaRepository.cs
public interface ISchemaRepository
{
    /// <summary>
    /// Gets the database schema.
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schema cannot be loaded</exception>
    Task<DatabaseSchema> GetSchemaAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves schemas to the repository.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be saved</exception>
    Task SaveSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes schemas from the repository.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be deleted</exception>
    Task DeleteSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default);
}
```

**Implementations:**
- `KustoSchemaRepository` (in SyncKusto.Kusto)
- `FileSystemSchemaRepository` (in SyncKusto.FileSystem)
- `InMemorySchemaRepository` (for testing)

### 3. Schema Comparison Service

```csharp
// SyncKusto.Core/Abstractions/ISchemaComparisonService.cs
public interface ISchemaComparisonService
{
    /// <summary>
    /// Compares two database schemas and returns their differences.
    /// </summary>
    SchemaDifferenceResult CompareSchemas(
        DatabaseSchema source, 
        DatabaseSchema target);
}

public record SchemaDifferenceResult(
    IReadOnlyList<SchemaDifference> TableDifferences,
    IReadOnlyList<SchemaDifference> FunctionDifferences)
{
    public IEnumerable<SchemaDifference> AllDifferences => 
        TableDifferences.Concat(FunctionDifferences);
}
```

**Implementation:**
- `SchemaComparisonService` (in SyncKusto.Core)

### 4. Schema Synchronization Service (Orchestrator)

```csharp
// SyncKusto.Core/Abstractions/ISchemaSyncService.cs
public interface ISchemaSyncService
{
    /// <summary>
    /// Compares source and target schemas.
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schemas cannot be loaded</exception>
    /// <exception cref="SchemaValidationException">Thrown when schema validation fails</exception>
    Task<SchemaDifferenceResult> CompareAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Synchronizes selected differences from source to target.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when synchronization fails</exception>
    Task<SyncResult> SynchronizeAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

**Implementation:**
- `SchemaSyncService` (in SyncKusto.Core)

### 5. Configuration Abstraction

```csharp
// SyncKusto.Core/Abstractions/ISettingsProvider.cs
public interface ISettingsProvider
{
    string? GetSetting(string key);
    void SetSetting(string key, string value);
    IEnumerable<string> GetRecentValues(string key);
    void AddRecentValue(string key, string value);
}

// SyncKusto.Core/Configuration/SyncKustoSettings.cs
public record SyncKustoSettings
{
    public required string TempCluster { get; init; }
    public required string TempDatabase { get; init; }
    public string? AADAuthority { get; init; }
    public bool KustoObjectDropWarning { get; init; }
    public bool TableFieldsOnNewLine { get; init; }
    public bool CreateMergeEnabled { get; init; }
    public bool UseLegacyCslExtension { get; init; }
    public LineEndingMode LineEndingMode { get; init; }
    public StoreLocation CertificateLocation { get; init; }
    public string FileExtension => UseLegacyCslExtension ? "csl" : "kql";
}
```

**Implementations:**
- `JsonFileSettingsProvider` (current behavior)
- `InMemorySettingsProvider` (for testing)

### 6. Progress Reporting Abstraction

```csharp
// SyncKusto.Core/Models/SyncProgress.cs
public record SyncProgress(
    string Message,
    int? PercentComplete = null,
    SyncProgressStage Stage = SyncProgressStage.Unknown);

public enum SyncProgressStage
{
    Unknown,
    ValidatingSource,
    ValidatingTarget,
    LoadingSourceSchema,
    LoadingTargetSchema,
    ComparingSchemas,
    SynchronizingSchemas,
    Complete
}
```

Use `IProgress<SyncProgress>` for reporting (built-in .NET interface)

### 7. Error Message Resolution

```csharp
// SyncKusto.Core/Abstractions/IErrorMessageResolver.cs
/// <summary>
/// Resolves user-friendly error messages from exceptions
/// </summary>
public interface IErrorMessageResolver
{
    string? ResolveErrorMessage(Exception exception);
}

// SyncKusto.Core/Services/CompositeErrorMessageResolver.cs
public class CompositeErrorMessageResolver : IErrorMessageResolver
{
    private readonly IReadOnlyList<IErrorMessageResolver> _resolvers;
    
    public CompositeErrorMessageResolver(IEnumerable<IErrorMessageResolver> resolvers)
    {
        _resolvers = resolvers.ToList();
    }
    
    public string? ResolveErrorMessage(Exception exception)
    {
        foreach (var resolver in _resolvers)
        {
            var message = resolver.ResolveErrorMessage(exception);
            if (message != null)
                return message;
        }
        
        return exception.Message;
    }
}

// SyncKusto.Core/Services/AggregateExceptionResolver.cs
/// <summary>
/// Resolves error messages from AggregateException by unwrapping inner exceptions
/// </summary>
public class AggregateExceptionResolver : IErrorMessageResolver
{
    private readonly IErrorMessageResolver _innerResolver;
    
    public AggregateExceptionResolver(IErrorMessageResolver innerResolver)
    {
        _innerResolver = innerResolver;
    }
    
    public string? ResolveErrorMessage(Exception exception)
    {
        if (exception is not AggregateException ae)
            return null;
            
        foreach (var inner in ae.InnerExceptions)
        {
            var message = _innerResolver.ResolveErrorMessage(inner);
            if (message != null)
                return message;
        }
        
        return null;
    }
}
```

**Implementation:**
- `CompositeErrorMessageResolver` that aggregates multiple resolvers
- Exception type-based resolvers that handle specific exception types
- No need for Specification pattern or NonEmptyString state machine

### 8. Authentication Factory

```csharp
// SyncKusto.Kusto/Abstractions/IKustoConnectionFactory.cs
public interface IKustoConnectionFactory
{
    KustoConnectionStringBuilder CreateConnectionString(
        KustoConnectionOptions options);
}

public record KustoConnectionOptions(
    string Cluster,
    string Database,
    AuthenticationMode AuthMode,
    string Authority,
    string? AppId = null,
    string? AppKey = null,
    string? CertificateThumbprint = null);
```

**Implementation:**
- `KustoConnectionFactory` (in SyncKusto.Kusto)

## Migration Strategy

### Phase 0: Simplify Error Handling and Validation (Foundation)
**Goal:** Remove Either/Option/Specification patterns in favor of standard exceptions and nullables

#### Step 1: Enable Nullable Reference Types
1. Enable nullable reference types in all projects
2. Fix nullable warnings incrementally

#### Step 2: Create Custom Exception Hierarchy
1. Create `SyncKustoException` and derived types
2. Create exception-based error message resolver

#### Step 3: Remove Specification Pattern
1. **Audit usage:**
   - Search for `Specification<` usages
   - Search for `.IsSatisfiedBy()` calls
   - Search for `Spec<T>` static calls
   - Document each validation use case

2. **Remove Specification infrastructure:**
   - `Specification.cs`
   - `Composite.cs`
   - `Transform.cs`
   - `Property.cs`
   - `Predicate.cs`
   - `NotNull.cs`
   - `NotNullOrEmptyString.cs`
   - `Spec.cs`

3. **Replace with standard validation:**
   - `NonEmptyString` ? Use `string` with validation in constructor/method
   - `INonEmptyStringState` ? Remove, use `string?` instead
   - `UninitializedString` ? Remove, use nullable types
   - `OperationErrorMessageSpecification` ? Replace with exception-type-based resolvers

4. **Migration patterns:**
   | Before | After |
   |--------|-------|
   | `Spec<string>.NonEmptyString(s => s).IsSatisfiedBy(value)` | `ArgumentException.ThrowIfNullOrWhiteSpace(value)` |
   | `new NonEmptyString(value)` | Direct `string` usage with validation |
   | `INonEmptyStringState` state pattern | `string?` with null checks |
   | Specification-based error matching | Exception type checking in resolver |

#### Step 4: Remove Either/Option Patterns
1. Replace `Either<IOperationError, T>` return types with:
   - Direct return of `T`
   - Throw exceptions on error
2. Replace `Option<T>` with nullable types (`T?`)
3. Update all consuming code to use try/catch instead of Either matching
4. Remove `Either`, `Option`, `Some`, `None`, `EitherAdapters`, `OptionAdapters` types

#### Step 5: Keep Unit Type
- Keep `Unit` type for operations with no return value
- Remove other functional utilities

**Deliverables:**
- Standard .NET exception handling throughout
- Nullable reference types enabled
- Simple, direct validation code
- No Specification pattern
- No Either/Option patterns
- Simpler, more maintainable error handling code

### Phase 1: Extract Core Abstractions (No Breaking Changes)
**Goal:** Create interfaces and move models to new Core project

1. Create `SyncKusto.Core` project (target: .NET 8, no Windows dependencies)
2. Move models (`SchemaDifference`, `LineEndingMode`, etc.) to Core
3. Define core interfaces (listed above)
4. Existing code references new Core project, no behavior changes

**Deliverables:**
- New Core project compiles
- Existing WinForms app still works
- All existing tests pass

### Phase 2: Extract Kusto Infrastructure
**Goal:** Separate Kusto-specific code into its own project

1. Create `SyncKusto.Kusto` project (target: .NET 8)
2. Move `QueryEngine` to Kusto project
3. Create `KustoSchemaRepository` implementing `ISchemaRepository`
4. Create `KustoConnectionFactory` implementing `IKustoConnectionFactory`
5. Move Kusto-specific models and extensions
6. Update `KustoDatabaseSchemaBuilder` to use new abstractions
7. Create Kusto-specific exceptions (e.g., `KustoConnectionException`, `KustoQueryException`)

**Deliverables:**
- Kusto operations isolated in separate assembly
- Can be tested independently
- WinForms app references Kusto project

### Phase 3: Extract File System Infrastructure
**Goal:** Separate file system operations

1. Create `SyncKusto.FileSystem` project (target: .NET 8)
2. Create `FileSystemSchemaRepository` implementing `ISchemaRepository`
3. Move file-related extension methods
4. Update `FileDatabaseSchemaBuilder` to use new abstractions
5. **Critical:** Remove `MessageBox` calls from file system code
   - Throw appropriate exceptions instead
   - Let UI layer catch and display errors
6. Create file system exceptions (e.g., `FileSchemaException`)

**Deliverables:**
- File operations isolated and testable
- No UI dependencies in file system code

### Phase 4: Create Core Services
**Goal:** Extract business logic from MainForm

1. Implement `SchemaComparisonService`
2. Implement `SchemaSyncService` as orchestrator
3. Implement `CompositeErrorMessageResolver`
4. Extract schema loading logic from `SchemaPickerControl`
5. Create `SchemaLoaderService` that uses repositories

**Deliverables:**
- Business logic in testable services
- MainForm reduced to UI coordination

### Phase 5: Create Settings Abstraction
**Goal:** Make configuration testable and injectable

1. Create `ISettingsProvider` interface
2. Create `JsonFileSettingsProvider` (wraps existing `SettingsWrapper`)
3. Create `SyncKustoSettings` immutable record
4. Inject `ISettingsProvider` into services that need it
5. Eventually replace static `SettingsWrapper` with instance

**Deliverables:**
- Settings can be mocked in tests
- Configuration loaded once, passed around as immutable

### Phase 6: Refactor UI Layer
**Goal:** Clean UI code, introduce presentation layer patterns

#### Context from Completed Phases

**Phase 1-3 Accomplishments:**
- ? Core abstractions defined in `SyncKusto.Core`
- ? Kusto operations isolated in `SyncKusto.Kusto`
- ? File system operations isolated in `SyncKusto.FileSystem`
- ? Exception-based error handling established
- ? `ISchemaRepository` pattern implemented
- ? `IErrorMessageResolver` pattern implemented

**Available Infrastructure:**
1. **Repositories** (Ready to Use)
   - `KustoSchemaRepository` - Kusto database access
   - `FileSystemSchemaRepository` - File system access
   - Both implement `ISchemaRepository` with async methods

2. **Error Handling** (Ready to Use)
   - `FileSystemErrorMessageResolver` - File system errors
   - `KustoErrorMessageResolver` - Kusto errors
   - `CompositeErrorMessageResolver` - Aggregates resolvers
   - `AggregateExceptionResolver` - Unwraps aggregate exceptions

3. **Exception Hierarchy** (Ready to Use)
   - `SyncKustoException` - Base
   - `SchemaLoadException` - Schema loading failures
   - `SchemaSyncException` - Schema synchronization failures
   - `SchemaValidationException` - Validation errors with list
   - `FileSchemaException` - File system errors
   - `SchemaParseException` - Parse errors with failed objects list

4. **Models** (Ready to Use)
   - `SchemaDifference` - Represents schema differences
   - `SchemaDifferenceResult` - Collection of differences
   - `SyncProgress` - Progress reporting data
   - `SyncResult` - Synchronization results
   - `LineEndingMode`, `StoreLocation`, `AuthenticationMode` - Enums

#### Current UI State Analysis

**MainForm.cs** - 400+ lines, multiple responsibilities:
- ? Has error message resolver (good!)
- ? Direct schema loading via builders (should use repositories)
- ? Business logic in `btnCompare_Click` (schema comparison)
- ? Business logic in `btnUpdate_Click` (synchronization)
- ? Direct persistence logic in `PersistChanges`
- ? Tree view population mixed with comparison logic
- ? Direct calls to `MessageBox.Show()` throughout
- ? No progress reporting abstraction (hardcoded UI updates)
- ? Validation logic embedded in event handlers

**SchemaPickerControl.cs** - 300+ lines, multiple responsibilities:
- ? UI, validation, and schema loading all mixed
- ? Creates schema builders directly (tight coupling)
- ? Settings access via static `SettingsWrapper`
- ? Progress reporting directly to TextBox
- ? Validation logic scattered across multiple methods
- ? Recent values management mixed with UI
- ? Authentication mode selection tightly coupled

**Current Data Flow:**
```
User Click ? MainForm Event Handler ? Direct Builder Creation ? Schema Load ? 
Direct Comparison Logic ? Tree Population ? Direct Persistence ? MessageBox
```

**Target Data Flow:**
```
User Click ? Presenter/ViewModel ? Service Call ? Repository ? 
Result ? Presenter/ViewModel ? View Update ? User Feedback
```

#### Implementation Steps

##### 1. Create Presentation Layer Abstractions

**IMainFormPresenter** - Orchestrates main form operations
```csharp
public interface IMainFormPresenter
{
    /// <summary>
    /// Compare source and target schemas
    /// </summary>
    Task<ComparisonResult> CompareAsync(
        SchemaSourceInfo source,
        SchemaSourceInfo target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Synchronize selected differences
    /// </summary>
    Task<SyncResult> SynchronizeAsync(
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that settings are configured
    /// </summary>
    ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target);
}

public record SchemaSourceInfo(
    SourceSelection SourceType,
    string? FilePath = null,
    KustoConnectionInfo? KustoInfo = null);

public record KustoConnectionInfo(
    string Cluster,
    string Database,
    AuthenticationMode AuthMode,
    string Authority,
    string? AppId = null,
    string? AppKey = null,
    string? CertificateThumbprint = null);

public record ComparisonResult(
    SchemaDifferenceResult Differences,
    DatabaseSchema SourceSchema,
    DatabaseSchema TargetSchema);

public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null);
```

**ISchemaSourceSelector** - Abstracts schema source selection
```csharp
public interface ISchemaSourceSelector
{
    /// <summary>
    /// Get schema source information from the control
    /// </summary>
    SchemaSourceInfo GetSourceInfo();
    
    /// <summary>
    /// Validate the current source configuration
    /// </summary>
    ValidationResult Validate();
    
    /// <summary>
    /// Report progress to the user
    /// </summary>
    void ReportProgress(string message);
    
    /// <summary>
    /// Save recent values for next session
    /// </summary>
    void SaveRecentValues();
    
    /// <summary>
    /// Reload recent values
    /// </summary>
    void ReloadRecentValues();
}
```

##### 2. Create MainFormPresenter Implementation

```csharp
public class MainFormPresenter : IMainFormPresenter
{
    private readonly ISchemaSyncService _syncService;
    private readonly Func<SchemaSourceInfo, ISchemaRepository> _repositoryFactory;
    private readonly IErrorMessageResolver _errorResolver;
    
    // Cached for synchronization
    private ISchemaRepository? _lastSourceRepository;
    private ISchemaRepository? _lastTargetRepository;
    
    public async Task<ComparisonResult> CompareAsync(
        SchemaSourceInfo source,
        SchemaSourceInfo target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _lastSourceRepository = _repositoryFactory(source);
        _lastTargetRepository = _repositoryFactory(target);
        
        var differences = await _syncService.CompareAsync(
            _lastSourceRepository,
            _lastTargetRepository,
            progress,
            cancellationToken);
        
        // Also get the actual schemas for display
        var sourceSchema = await _lastSourceRepository.GetSchemaAsync(cancellationToken);
        var targetSchema = await _lastTargetRepository.GetSchemaAsync(cancellationToken);
        
        return new ComparisonResult(differences, sourceSchema, targetSchema);
    }
    
    public async Task<SyncResult> SynchronizeAsync(
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_lastSourceRepository == null || _lastTargetRepository == null)
            throw new InvalidOperationException("Must compare before synchronizing");
        
        return await _syncService.SynchronizeAsync(
            _lastSourceRepository,
            _lastTargetRepository,
            selectedDifferences,
            progress,
            cancellationToken);
    }
    
    public ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target)
    {
        // Validation logic extracted from MainForm
        if ((source.SourceType == SourceSelection.FilePath() || 
             target.SourceType == SourceSelection.FilePath()) &&
            string.IsNullOrWhiteSpace(/* settings */.TempCluster))
        {
            return new ValidationResult(
                false, 
                "File system sources require temp cluster configuration");
        }
        
        return new ValidationResult(true);
    }
}
```

##### 3. Update MainForm to Use Presenter

**Before (Current):**
```csharp
private void btnCompare_Click(object sender, EventArgs e)
{
    // 60+ lines of validation, schema loading, comparison logic
    _sourceSchema = spcSource.LoadSchema();
    _targetSchema = spcTarget.LoadSchema();
    // ... comparison logic ...
    // ... tree population ...
}
```

**After (Target):**
```csharp
private IMainFormPresenter _presenter;
private ComparisonResult? _lastComparison;

private async void btnCompare_Click(object sender, EventArgs e)
{
    try
    {
        // Get source info from controls
        var source = spcSource.GetSourceInfo();
        var target = spcTarget.GetSourceInfo();
        
        // Validate
        var validation = _presenter.ValidateSettings(source, target);
        if (!validation.IsValid)
        {
            MessageBox.Show(validation.ErrorMessage, "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        
        // Compare with progress
        var progress = new Progress<SyncProgress>(p => 
        {
            spcSource.ReportProgress(p.Message);
            spcTarget.ReportProgress(p.Message);
        });
        
        _lastComparison = await _presenter.CompareAsync(source, target, progress);
        
        // Update UI
        PopulateTree(_lastComparison.Differences);
        btnUpdate.Enabled = true;
    }
    catch (Exception ex)
    {
        var message = _errorResolver.ResolveErrorMessage(ex);
        MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

##### 4. Extract Schema Source Selection Logic

Create `SchemaSourceSelectorAdapter` to wrap `SchemaPickerControl`:

```csharp
public class SchemaSourceSelectorAdapter : ISchemaSourceSelector
{
    private readonly SchemaPickerControl _control;
    
    public SchemaSourceInfo GetSourceInfo()
    {
        if (_control.SourceSelection == SourceSelection.FilePath())
        {
            return new SchemaSourceInfo(
                SourceSelection.FilePath(),
                FilePath: _control.SourceFilePath);
        }
        else
        {
            return new SchemaSourceInfo(
                SourceSelection.Kusto(),
                KustoInfo: new KustoConnectionInfo(
                    // ... extract from _control.KustoConnection
                ));
        }
    }
    
    public ValidationResult Validate() => 
        _control.IsValid() 
            ? new ValidationResult(true) 
            : new ValidationResult(false, "Invalid source configuration");
    
    public void ReportProgress(string message) => 
        _control.ReportProgress(message);
}
```

##### 5. Create Repository Factory

```csharp
public class SchemaRepositoryFactory
{
    private readonly ISettingsProvider _settings;
    
    public ISchemaRepository CreateRepository(SchemaSourceInfo sourceInfo)
    {
        return sourceInfo.SourceType switch
        {
            _ when sourceInfo.SourceType == SourceSelection.FilePath() => 
                new FileSystemSchemaRepository(
                    sourceInfo.FilePath!,
                    _settings.GetSetting("FileExtension") ?? "kql",
                    _settings.GetSetting("TempCluster")!,
                    _settings.GetSetting("TempDatabase")!,
                    _settings.GetSetting("Authority") ?? ""),
            
            _ when sourceInfo.SourceType == SourceSelection.Kusto() =>
                new KustoSchemaRepository(
                    sourceInfo.KustoInfo!.Cluster,
                    sourceInfo.KustoInfo.Database,
                    sourceInfo.KustoInfo.AuthMode,
                    sourceInfo.KustoInfo.Authority,
                    sourceInfo.KustoInfo.AppId,
                    sourceInfo.KustoInfo.AppKey,
                    sourceInfo.KustoInfo.CertificateThumbprint),
            
            _ => throw new InvalidOperationException("Unknown source type")
        };
    }
}
```

##### 6. Simplify SchemaPickerControl

**Current Responsibilities** (300+ lines):
- UI rendering ? Keep
- Validation logic ? Move to adapter/presenter
- Schema loading ? Remove (use repository via presenter)
- Settings management ? Move to settings provider
- Recent values ? Keep (UI state)
- Progress reporting ? Keep (UI update)

**Target** (~150 lines):
- Only UI and state management
- Expose properties for data binding
- Simple validation of UI inputs only
- No business logic

##### 7. Update Program.cs for Dependency Injection

```csharp
static void Main(string[] args)
{
    // Configure DI container
    var services = new ServiceCollection();
    
    // Core services
    services.AddSingleton<ISchemaComparisonService, SchemaComparisonService>();
    services.AddSingleton<ISchemaSyncService, SchemaSyncService>();
    
    // Error resolvers
    services.AddSingleton<IErrorMessageResolver, FileSystemErrorMessageResolver>();
    services.AddSingleton<IErrorMessageResolver, KustoErrorMessageResolver>();
    services.AddSingleton<IErrorMessageResolver, AggregateExceptionResolver>();
    services.AddSingleton<IErrorMessageResolver, CompositeErrorMessageResolver>();
    
    // Settings (Phase 5 dependency)
    services.AddSingleton<ISettingsProvider, JsonFileSettingsProvider>();
    
    // Repository factory
    services.AddSingleton<SchemaRepositoryFactory>();
    
    // Presenters
    services.AddSingleton<IMainFormPresenter, MainFormPresenter>();
    
    // Build and run
    var serviceProvider = services.BuildServiceProvider();
    
    Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
    Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    
    var mainForm = new MainForm(
        serviceProvider.GetRequiredService<IMainFormPresenter>(),
        serviceProvider.GetRequiredService<IErrorMessageResolver>());
    
    Application.Run(mainForm);
}
```

#### Migration Checklist

**MainForm.cs:**
- [ ] Add constructor parameter for `IMainFormPresenter`
- [ ] Add constructor parameter for `IErrorMessageResolver`
- [ ] Replace schema loading with `_presenter.CompareAsync()`
- [ ] Replace synchronization with `_presenter.SynchronizeAsync()`
- [ ] Use `IProgress<SyncProgress>` for progress reporting
- [ ] Catch exceptions and use error resolver
- [ ] Remove direct `SettingsWrapper` access
- [ ] Remove direct builder instantiation
- [ ] Keep only UI-specific logic (tree population, node clicking, etc.)

**SchemaPickerControl.cs:**
- [ ] Remove `LoadSchema()` method
- [ ] Add `GetSourceInfo()` method
- [ ] Simplify `IsValid()` to only check UI inputs
- [ ] Remove direct `SettingsWrapper` access (except for recent values UI)
- [ ] Keep UI state management
- [ ] Keep progress reporting UI updates

**Settings Management:**
- [ ] Wait for Phase 5 to complete `ISettingsProvider`
- [ ] Then inject `ISettingsProvider` into components
- [ ] Replace `SettingsWrapper` static calls with injected provider

**Progress Reporting:**
- [ ] Use `IProgress<SyncProgress>` throughout
- [ ] Create progress adapter for UI updates
- [ ] Support cancellation via `CancellationToken`

#### Dependencies on Other Phases

**Requires Phase 4** (In Progress):
- ? `ISchemaComparisonService` - Schema comparison logic
- ? `ISchemaSyncService` - Synchronization orchestration
- ? Need implementations created

**Requires Phase 5** (Not Started):
- ? `ISettingsProvider` - Configuration abstraction
- ? `SyncKustoSettings` - Immutable settings model
- Workaround: Can still use static `SettingsWrapper` temporarily

**Enables Phase 7** (Testing):
- Presenters can be unit tested with mock services
- UI can be tested by mocking presenters
- Integration tests can verify full stack

#### Key Design Decisions

1. **Presenter Pattern vs MVVM**
   - Choose Presenter pattern (simpler for WinForms)
   - No data binding framework needed
   - Direct method calls from view to presenter
   - Easier to test than tightly coupled event handlers

2. **Keep Existing Controls**
   - Don't create new `SyncKusto.UI.WinForms` project yet
   - Refactor in-place first
   - Move to new project only if needed
   - Reduces risk and scope

3. **Progress Reporting**
   - Use built-in `IProgress<T>` interface
   - Create simple adapter for UI updates
   - Support cancellation from start
   - No custom progress events

4. **Error Display**
   - Always use `IErrorMessageResolver`
   - Catch exceptions at UI boundary
   - Display user-friendly messages
   - Log full exception details

5. **Backward Compatibility**
   - Keep existing forms and controls
   - Gradual migration
   - No UI redesign (same user experience)
   - Focus on architecture improvement

#### Testing Strategy

**Presenter Tests:**
```csharp
[Test]
public async Task CompareAsync_WithValidSources_ReturnsComparison()
{
    var mockService = new Mock<ISchemaSyncService>();
    var mockRepoFactory = new Mock<Func<SchemaSourceInfo, ISchemaRepository>>();
    var presenter = new MainFormPresenter(mockService.Object, mockRepoFactory.Object, ...);
    
    var result = await presenter.CompareAsync(source, target);
    
    Assert.IsNotNull(result);
    mockService.Verify(s => s.CompareAsync(It.IsAny<ISchemaRepository>(), 
        It.IsAny<ISchemaRepository>(), null, default), Times.Once);
}
```

**Integration Tests:**
```csharp
[Test]
public async Task EndToEnd_FileToKusto_Success()
{
    // Setup real repositories
    var fileRepo = new FileSystemSchemaRepository(...);
    var kustoRepo = new KustoSchemaRepository(...);
    
    // Setup real services
    var comparisonService = new SchemaComparisonService();
    var syncService = new SchemaSyncService(comparisonService);
    
    // Setup presenter
    var presenter = new MainFormPresenter(syncService, ...);
    
    // Execute
    var result = await presenter.CompareAsync(fileSource, kustoTarget);
    
    // Verify
    Assert.IsNotEmpty(result.Differences.AllDifferences);
}
```

#### Success Criteria

1. **Thin UI Layer**
   - MainForm < 200 lines (currently 400+)
   - SchemaPickerControl < 150 lines (currently 300+)
   - No business logic in event handlers
   - Only UI updates and presenter calls

2. **Testable Presenters**
   - Can unit test with mock services
   - No UI dependencies in presenters
   - All paths covered by tests

3. **Clean Exception Handling**
   - All exceptions caught at UI boundary
   - Error resolver used consistently
   - User-friendly messages displayed
   - Full exceptions logged

4. **Progress Reporting**
   - Uses `IProgress<SyncProgress>`
   - UI remains responsive
   - Can be cancelled
   - Clear progress messages

5. **No Regression**
   - All existing features work
   - Same user experience
   - No performance degradation
   - All UI interactions preserved

#### Deliverables

- ? `IMainFormPresenter` interface and implementation
- ? `ISchemaSourceSelector` interface and adapter
- ? `SchemaRepositoryFactory` for creating repositories
- ? Updated MainForm using presenter
- ? Updated SchemaPickerControl (simplified)
- ? DI configuration in Program.cs
- ? Progress reporting abstraction
- ? Comprehensive presenter tests
- ? Updated UI migration guide

#### Notes for Implementer

**Start Here:**
1. Implement `IMainFormPresenter` interface
2. Create `MainFormPresenter` with dependencies on Phase 4 services
3. Update `MainForm` constructor to accept presenter
4. Replace `btnCompare_Click` logic with presenter call
5. Test that comparison still works

**Then:**
6. Replace `btnUpdate_Click` logic with presenter call
7. Test that synchronization still works
8. Add progress reporting
9. Add cancellation support
10. Write unit tests

**Finally:**
11. Simplify `SchemaPickerControl`
12. Extract repository factory
13. Configure DI in Program.cs
14. Remove remaining static dependencies
15. Full integration testing

**Dependencies to Watch:**
- Phase 4 must provide `ISchemaSyncService` implementation
- Phase 5 will replace `SettingsWrapper` usage
- Keep interfaces compatible for future changes

### Phase 7: Add Comprehensive Tests
**Goal:** Achieve high test coverage of core logic

1. Create `SyncKusto.Core.Tests` project
2. Test all services with mock repositories
3. Test exception scenarios with `Assert.Throws`
4. Create `SyncKusto.Kusto.Tests` project
5. Test Kusto operations with test containers or mocked clients
6. Create `SyncKusto.Integration.Tests` for end-to-end scenarios
7. Aim for 80%+ coverage of Core and Infrastructure projects

**Deliverables:**
- Confidence in refactoring
- Regression prevention
- Documentation via tests

## Detailed Design Patterns

### Repository Pattern
- Each data source (Kusto, FileSystem) has a repository
- Repositories throw exceptions on failure
- Repositories are async by default
- Repositories don't know about each other

### Service Layer Pattern
- Services orchestrate multiple repositories
- Services contain business rules and workflows
- Services are testable with mock repositories
- Services don't reference UI
- Services throw domain-specific exceptions

### Factory Pattern
- `IKustoConnectionFactory` creates connection strings
- Schema repository factories for different sources
- Error message resolver factories

### Strategy Pattern
- Different authentication strategies
- Different schema comparison strategies (if needed)
- Different error resolution strategies

### Adapter Pattern
- UI adapters convert service results to UI models
- Progress adapters convert `IProgress<T>` to UI updates

## Key Technical Decisions

### 1. Async/Await Throughout
- All I/O operations are async
- Use `CancellationToken` for long-running operations
- Allow UI to remain responsive

### 2. Immutability Where Possible
- Use `record` types for DTOs
- Settings are immutable
- Schema models should be immutable (refactor if needed)

### 3. Error Handling
- Use standard .NET exceptions for all error cases
- Create custom exception hierarchy for domain-specific errors
- Include meaningful error messages and context
- UI layer catches exceptions and displays user-friendly messages
- Nullable reference types enabled for null safety

### 4. Nullable Reference Types
- Enable nullable reference types in all projects
- Use `?` suffix for nullable reference types
- Use nullable value types where appropriate

### 5. Dependency Injection
- Use constructor injection
- Use `Microsoft.Extensions.DependencyInjection`
- WinForms app configures DI container in `Program.cs`

### 6. Logging
- Add `ILogger<T>` from Microsoft.Extensions.Logging
- Log important operations (schema loads, syncs)
- Log exceptions with full context
- Don't log sensitive data (connection strings with keys)

### 7. Validation
- Use simple, direct validation in methods and constructors
- Use `ArgumentNullException.ThrowIfNull()` for null checks (.NET 6+)
- Use `ArgumentException.ThrowIfNullOrWhiteSpace()` for string validation (.NET 8+)
- Throw `SchemaValidationException` with error list for complex validation
- No Specification pattern - too complex for this codebase

## Testing Strategy

### Unit Tests
- Test all services in isolation
- Mock all dependencies
- Fast, deterministic
- Focus on business logic
- Use `Assert.Throws<TException>` for exception testing
- Test both success and failure paths

### Integration Tests
- Test repository implementations
- May require test infrastructure (Kusto emulator, temp files)
- Slower, but verify real integrations
- Test actual exception scenarios

### UI Tests
- Minimal - mostly manual
- Focus on view model/presenter logic
- Avoid testing WinForms framework itself

## Migration Risks and Mitigations

### Risk 1: Breaking Existing Functionality
**Mitigation:**
- Incremental refactoring
- Keep existing code working during migration
- Comprehensive testing before removing old code
- Feature flags if needed

### Risk 2: Performance Regression
**Mitigation:**
- Profile before and after
- Async operations should improve performance
- Monitor schema comparison times
- Exceptions for control flow are acceptable in this small codebase

### Risk 3: Over-Engineering
**Mitigation:**
- Start with simplest working design
- Refactor as needed
- YAGNI (You Aren't Gonna Need It) principle
- Each phase should deliver value

### Risk 4: Incomplete Abstraction
**Mitigation:**
- Design interfaces carefully
- Review with team
- Be willing to revise interfaces

### Risk 5: Pattern Migration Complexity
**Mitigation:**
- Phase 0 focuses solely on pattern migration
- Use compiler to find all usages of Either/Option/Specification
- Comprehensive testing after each type is migrated
- Keep git history clean with focused commits

### Risk 6: Exception Handling Overhead
**Mitigation:**
- Acceptable for small, controlled codebase
- Not on hot paths
- Clear, domain-specific exception types
- Comprehensive logging

### Risk 7: Validation Code Scattered
**Mitigation:**
- Centralize validation in constructors and factory methods
- Use guard clauses consistently
- Document validation requirements in XML comments
- Consider Data Annotations for simple property validation if needed

## Files to Remove in Phase 0

### Validation Infrastructure (Specification Pattern)
- `SyncKusto/Validation/Infrastructure/Specification.cs`
- `SyncKusto/Validation/Infrastructure/Composite.cs`
- `SyncKusto/Validation/Infrastructure/Transform.cs`
- `SyncKusto/Validation/Infrastructure/Property.cs`
- `SyncKusto/Validation/Infrastructure/Predicate.cs`
- `SyncKusto/Validation/Infrastructure/NotNull.cs`
- `SyncKusto/Validation/Infrastructure/NotNullOrEmptyString.cs`
- `SyncKusto/Validation/Infrastructure/Spec.cs`

### String Validation State Machine
- `SyncKusto/Models/NonEmptyString.cs`
- `SyncKusto/Models/INonEmptyStringState.cs`
- `SyncKusto/Models/UninitializedString.cs`

### Error Message Specifications (to be replaced)
- `SyncKusto/Validation/ErrorMessages/OperationErrorMessageSpecification.cs`
- `SyncKusto/Validation/ErrorMessages/IOperationErrorMessageSpecification.cs`
- `SyncKusto/Validation/ErrorMessages/DefaultOperationErrorMessageResolver.cs`
- `SyncKusto/Validation/ErrorMessages/Specifications/DefaultOperationErrorSpecification.cs`
- Any other specification-based error resolvers in that directory

### Functional Programming Infrastructure (after Either/Option migration)
- `SyncKusto/Functional/Either.cs` (or `SyncKusto.Core/Functional/Either.cs`)
- `SyncKusto/Functional/EitherAdapters.cs`
- `SyncKusto/Functional/Option.cs`
- `SyncKusto/Functional/Some.cs`
- `SyncKusto/Functional/None.cs`
- `SyncKusto/Functional/OptionAdapters.cs`
- `SyncKusto/Functional/EnumerableExtensions.cs` (unless has non-FP utilities)
- `SyncKusto/Functional/Reiterable.cs`
- `SyncKusto/Functional/ReiterableExtensions.cs`

### Keep
- `SyncKusto/Functional/Unit.cs` - Still useful for operations with no return value
- `SyncKusto/Validation/ErrorMessages/IOperationError.cs` - Temporarily keep, migrate away in Phase 1

## Success Criteria

1. **Core Logic Isolation**
   - SyncKusto.Core has zero references to Windows Forms
   - SyncKusto.Core has zero references to Kusto SDK
   - All file I/O abstracted behind interfaces

2. **Modern .NET Patterns**
   - No usage of Either/Option patterns
   - No usage of Specification pattern
   - No complex validation state machines
   - Nullable reference types enabled and warnings clean
   - Standard exception handling used consistently
   - Simple, direct validation throughout

3. **Testability**
   - 80%+ code coverage in Core project
   - All services can be tested with mocks
   - Integration tests verify real implementations
   - Exception scenarios well-tested

4. **Maintainability**
   - Clear separation of concerns
   - Each project has single responsibility
   - Dependencies flow inward (UI ? Services ? Core)
   - Clear exception hierarchy
   - Code is accessible to developers unfamiliar with functional programming

5. **No Regression**
   - All existing features still work
   - Performance is same or better
   - User experience unchanged (for WinForms app)

## Open Questions to Resolve

1. **Schema Model Ownership**
   - Current code uses `DatabaseSchema` from Kusto SDK
   - Should we create our own domain model?
   - Trade-off: independence vs. conversion overhead

2. **Temporary Database Strategy**
   - Current implementation uses temp Kusto database for file comparison
   - Is this still the best approach?
   - Could we parse CSL files without deploying them?

3. **Transaction Semantics**
   - Should sync operations be transactional?
   - How to handle partial failures?
   - Should we implement rollback?

4. **Concurrency**
   - How to handle concurrent access to same target?
   - Should we implement locking?
   - Current warnings sufficient?

5. **Configuration Migration**
   - Should we support importing old settings?
   - Breaking change acceptable?
   - Schema versioning needed?

## Next Steps

1. **Review and Approve Plan** - Team discusses and approves approach
2. **Spike Phase 0** - Create exception hierarchy, test migration of a few Specification/Either/Option usages
3. **Complete Phase 0** - Fully migrate from Specification/Either/Option to standard .NET patterns
4. **Implement Remaining Phases Sequentially** - One phase at a time, validate each
5. **Regular Check-ins** - Review progress, adjust plan as needed
6. **Documentation** - Update architecture docs as we go

## Conclusion

This refactoring will transform SyncKusto from a monolithic Windows Forms application into a modern, modular, testable architecture. By following these principles and executing in phases, we can:

- Maintain existing functionality while improving architecture
- Adopt standard .NET exception handling patterns
- Simplify validation with direct, straightforward code
- Dramatically improve testability and maintainability
- Reduce coupling and increase cohesion
- Follow industry best practices
- Make the codebase more accessible to .NET developers
- Eliminate unnecessary abstraction layers

The current Specification pattern, NonEmptyString state machine, and functional programming patterns (Either/Option) add complexity that isn't justified for a codebase of this size. By using standard .NET exception handling, nullable reference types, and simple validation, we make the code more maintainable and familiar to contributors while achieving the same goals with less overhead.
