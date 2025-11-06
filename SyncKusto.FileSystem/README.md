# SyncKusto.FileSystem

This project provides file system-based implementations for the SyncKusto schema repository pattern. It allows reading, writing, and deleting Kusto schemas (tables and functions) from the local file system.

## Overview

The FileSystem project is part of Phase 3 of the SyncKusto refactoring plan, which aims to separate file system operations from business logic and UI code.

## Key Components

### Repositories

#### `FileSystemSchemaRepository`
Implements `ISchemaRepository` to provide file system-based schema storage.

**Features:**
- Loads Kusto schemas from `.kql` or `.csl` files
- Saves schemas to the file system with proper folder structure
- Deletes schemas from the file system
- Uses a temporary Kusto database to parse and validate schema files
- Throws domain-specific exceptions for error handling

**Usage:**
```csharp
var repository = new FileSystemSchemaRepository(
    rootFolder: @"C:\MySchemas",
    fileExtension: "kql",
    tempCluster: "https://mycluster.kusto.windows.net",
    tempDatabase: "TempDB",
    authority: "https://login.microsoftonline.com/tenant-id"
);

// Load schemas
var schema = await repository.GetSchemaAsync();

// Save schemas
await repository.SaveSchemaAsync(new[] { tableSchema, functionSchema });

// Delete schemas
await repository.DeleteSchemaAsync(new[] { oldSchema });
```

### Extensions

#### `FileSystemSchemaExtensions`
Extension methods for performing file operations on Kusto schema objects.

**Methods:**
- `WriteToFile()` - Writes table or function schemas to files
- `DeleteFromFolder()` - Deletes schema files from the file system
- `HandleLongFileNames()` - Handles Windows long path limitations

### Exceptions

#### `FileSchemaException`
Base exception for file system schema operations.

#### `SchemaParseException`
Thrown when schema files cannot be parsed. Includes a list of failed objects.

### Services

#### `FileSystemErrorMessageResolver`
Resolves user-friendly error messages for file system exceptions.

**Handles:**
- `SchemaParseException` - Lists failed objects
- `UnauthorizedAccessException` - Access denied messages
- `DirectoryNotFoundException` - Directory not found messages
- `FileNotFoundException` - File not found messages
- `IOException` - Generic I/O error messages

## Folder Structure

The repository uses the following folder structure:

```
RootFolder/
??? Tables/
?   ??? TableName1.kql
?   ??? TableName2.kql
?   ??? FolderName/
?       ??? TableName3.kql
??? Functions/
    ??? FunctionName1.kql
    ??? FunctionName2.kql
    ??? FolderName/
        ??? FunctionName3.kql
```

## Dependencies

- **SyncKusto.Core** - Core abstractions and models
- **SyncKusto.Kusto** - Kusto SDK integration and command generation
- **Microsoft.Azure.Kusto.Data** - Kusto .NET SDK

## Design Decisions

### No UI Dependencies
This project has zero dependencies on Windows Forms or any UI framework. All user interaction (e.g., error messages) is handled through exceptions that the UI layer can catch and display.

### Exception-Based Error Handling
Instead of using MessageBox or other UI components, this project throws domain-specific exceptions:
- `FileSchemaException` for general file system errors
- `SchemaParseException` for parsing failures
- These exceptions include detailed context for debugging and user-friendly messages

### Temporary Database for Validation
The repository uses a temporary Kusto database to parse and validate schema files. This approach:
- Handles slightly malformed CSL files more gracefully
- Validates schema syntax before accepting files
- Provides consistent schema representation

### Async by Default
All I/O operations are asynchronous to support responsive UI and better scalability.

## Testing

Unit tests should mock the file system operations and test:
- Schema loading from various file structures
- Error handling for invalid files
- Parse error collection and reporting
- Long path handling

Integration tests should verify:
- Actual file system operations
- Temporary database schema validation
- End-to-end read/write/delete operations

## Future Improvements

1. **Settings Injection** - Currently uses hard-coded settings for create-merge and formatting options. These should be injected via configuration (Phase 5).
2. **Async File Operations** - Consider using async file I/O throughout
3. **File Watching** - Add file system watching capabilities for real-time sync
4. **Caching** - Cache parsed schemas to improve performance
5. **Parallel Processing** - Load multiple schema files in parallel

## Migration Notes

### From FileDatabaseSchemaBuilder
The old `FileDatabaseSchemaBuilder` class has been marked as obsolete and updated to:
- Remove `MessageBox.Show()` calls
- Throw `SchemaParseException` instead
- Use `FileSystemSchemaExtensions.HandleLongFileNames()`

### Backward Compatibility
Extension methods in `SyncKusto.ExtensionMethods` have been updated to delegate to `FileSystemSchemaExtensions` for file operations, maintaining backward compatibility.

## See Also

- [Refactoring Plan](../REFACTORING_PLAN.md) - Overall refactoring strategy
- [SyncKusto.Core](../SyncKusto.Core/README.md) - Core abstractions
- [SyncKusto.Kusto](../SyncKusto.Kusto/README.md) - Kusto-specific implementations
