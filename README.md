# Sync Kusto

The Sync Kusto tool was built to help create a maintainable development process around [Kusto](https://docs.microsoft.com/en-us/azure/data-explorer/). The tool makes it easy to sync database schemas using either the local file system or a Kusto cluster as either the source of the target. It will copy full function definitions along with table schemas. Table data is not synced.

![Screenshot](/screenshot.png)

## Scope
This tool is intended specifically for the developer desktop scenario. It will not help if you need to build an automated syncing process, generate change scripts, sync policies, etc. For all of that, we recommend using [Delta Kusto](https://github.com/microsoft/delta-kusto). 

## Getting Started
Assuming that the user already has a Kusto database with functions and tables, set the Kusto database as the source and the local file system as the target. Before your first comparison, open **Settings** and configure a dedicated temporary cluster and database as described below. Press the compare button and the tool will find that none of the database objects are in the target. Press the update button to download everything from Kusto to the local file system.

## Recommended Team Process
The general usage pattern on our team is to make changes in a Kusto dev database which starts with a schema that mirrors the production database. Once the developer is satisfied with the changes, they use Sync Kusto with the development database as the source and their local file system as the target. The changes are committed to source control and a pull request is sent out for review. After the request is approved and the code merges to master, the developer syncs the master branch and uses Sync Kusto to sync from their local file system to the production Kusto database. This is a more flexible/manual version of the [Azure Data Explorer task for Azure Dev Ops](https://docs.microsoft.com/en-us/azure/data-explorer/devops). We've had great success with this process and use the tool daily to help with our development and for deploying to production assets.

## Sync Kusto Binaries
The release can be downloaded from the [GitHub Releases](https://github.com/microsoft/synckusto/releases) page. Individual builds can also be found in the [GitHub Actions](https://github.com/microsoft/synckusto/actions/workflows/official.yml) page, or the repo can be cloned and built in Visual Studio. The framework-dependent Windows application requires the [.NET 10 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) matching the application's architecture.

## Settings
Changes take effect as soon as you successfully click **OK**; restarting the app is not required. If any settings changed, the previous comparison is cleared and you must click **Compare** again before **Update**. Canceling, failing validation, or declining the temporary-database warning does not save changes. Settings cannot be changed while a comparison or update is running.

### Schema pickers and authentication
Changing an authentication mode, application ID, key, or certificate in either schema picker preserves the comparison, checked changes, and skipped-file report. **Update** uses the target's current credentials with a new Kusto client, without comparing or normalizing files again. Incomplete target credentials must be corrected before updating; the comparison is kept. Source credential edits apply the next time an operation connects to Source, such as **Compare**; Update uses the already compared source data.

Changing a cluster, database, folder, or source type still clears the comparison and requires another **Compare**. Changes saved in the separate **Settings** dialog also still clear it. Pickers cannot be edited while an operation is running.

**Select Folder** opens the native picker with a short "Select schema folder" title and the current folder selected. A file-backed comparison remembers its folder before loading schemas, even if a later Kusto operation fails. This is one shared last-used folder, restored into both pickers on the next launch; when both sides use files, Target is remembered last. Merely browsing, canceling, or editing an inactive folder field does not change the remembered value.

### Kusto errors
Compare, Update, and Kusto settings-validation failures show a short operation heading with always-visible, read-only, scrollable error details. Select and copy the original Kusto diagnostics, including inner/aggregate errors and failed object names, without opening a separate Details section. Long errors stay within a resizable window. Diagnostics do not append picker credentials or settings; rejected-file input messages remain concise and do not include rejected file contents.

File-system diagnostics distinguish missing folders/files, access denials, and wrapped I/O errors. Schema parse failures list the affected objects with guidance to correct their definitions and compare again.

### Window icons
The main window, Kusto error details, skipped-file report, Settings, and Drop Warning use the same SyncKusto icon as the executable. Folder and certificate pickers and ordinary message boxes remain Windows-native and are owned by the calling SyncKusto window. Their APIs do not expose a custom caption-icon option: Windows controls whether they inherit the owner's icon, show a native icon, or show no caption icon. Standard error, warning, information, and question symbols in message content are unchanged.

### Temporary Databases
When the local file system is selected as either the source or the target, Sync Kusto loads its schema definitions into the configured temporary database for Kusto-backed normalization. Specify both a cluster and an existing database where you have permissions to create and delete functions and tables. All selected files on both sides are read and checked before opening a temporary client or changing that database. Files rejected by the schema-command checks are skipped and reported; only accepted definitions are loaded. After checking both sides, **all existing functions and tables in the temporary database are deleted before loading each file-backed side**. If the database is not empty when you configure it, Settings asks you to confirm that it can be wiped before saving. Use a dedicated scratch database containing no valuable objects or data. Do not point it at a source or target Kusto database. If two users share a temporary database concurrently, results can be incorrect; give each user their own.

### Schema file formats and corrections
The selected extension applies to both reading and writing: `.kql`, or legacy `.csl` when that setting is enabled. Files beneath the schema root's `Functions` folder (including its subfolders) must contain one `.create-or-alter function` definition. Other matching files must contain one `.create table` or `.create-merge table` definition with an explicit column list.

Function parameters, bodies, quoted names, comments, multiline strings, dependency references, and cross-database references **inside a function body** are supported. Optional `with` properties are `folder` and `docstring` strings, plus `skipvalidation` and `view` booleans for functions (boolean strings are also accepted). Normalization supplies `skipvalidation=true` so dependent functions can be loaded in any order.

Compare skips each file rejected by those checks, including unsupported object types such as materialized views, syntax errors, and scripts containing multiple commands. Materialized views are not supported for comparison or sync. A compact, resizable warning lists every skipped file in a scrolling, copyable report with **Source** or **Target**, the full path relative to that selected root, the reason and correction guidance, and a parser line/column when available. Reopen it with **Skipped files**. The comparison is incomplete: skipped source definitions can make target objects eligible for deletion, and file-target updates can overwrite or remove skipped files. Review checked changes before Update. Skipped file contents are never executed.

### AAD Authority
For Microsoft Entra ID user authentication, this field is optional. Leave it blank to use the Kusto SDK's default authority resolution. If automatic resolution does not work for your tenant, specify its authority in Settings. Application authentication with a key or certificate requires an explicit authority.

### Warnings
- Ask before dropping objects in the target Kusto database - This optional check is enabled by default and will prompt the user before dropping anything in the target database.

### Formatting
- Place table fields on new lines - When CSL table files are written to a file target, each field is placed on a new line. This can make it easier to diff changes in a pull request.
- Generated ".create-merge table" commands instead of ".create table" - When CSL table files are written to a file target, the command will be ".create-merge table" if this is checked. If it is unchecked, the command will be ".create table".

### Files
- Use legacy `.csl` file extensions - Kusto originally used files with `.csl` file extensions. This extension has since been deprecated and replaced with the `.kql` file extension. This setting tells synckusto which file extension should be used for reading _and_ writing all files. The default state is checked, which means synckusto will consume and emit files with the `.csl` file extension.

## Contributing
Issues, additional features, and tests are all welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions and contribution guidelines.
