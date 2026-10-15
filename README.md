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
The release can be downloaded from the [GitHub Releases](https://github.com/microsoft/synckusto/releases) page. Individual builds can also be found in the [GitHub Actions](https://github.com/microsoft/synckusto/actions/workflows/official.yml) page, or the repo can be cloned and built in Visual Studio.

## Dependency Restore and Build
Use Windows with MSBuild/Visual Studio supporting the .NET 8 and Windows Desktop SDKs. Both build workflows use [NuGet CLI 7.9.0](https://dist.nuget.org/win-x86-commandline/v7.9.0/nuget.exe). Run these commands from the repository root in a Developer PowerShell with that version of `nuget.exe` on `PATH`:

```powershell
nuget restore SyncKusto.sln -LockedMode -ConfigFile NuGet.Config -NonInteractive
if ($LASTEXITCODE -ne 0) { throw "Dependency restore failed" }
msbuild.exe SyncKusto.sln /t:Build /p:Configuration=Release
```

`NuGet.Config` approves only the HTTPS NuGet.org v3 feed and clears inherited package sources and fallback package folders. `Directory.Build.props` also disables the SDK's implicit fallback and workload `library-packs` feeds. Do not add user/runner feeds, fallback folders, or source overrides to work around restore failures. NuGet's existing signature verification remains enabled; this policy does not introduce trusted-signer certificate pinning or require all packages to be signed.

All five solution projects have a reviewed `packages.lock.json` containing resolved direct/transitive versions and package content hashes. CI checks that every lock file exists before restoring, fails on dependency or content-hash drift, and builds only after a successful locked restore. `/t:Build` does not perform another restore. Any subsequent `dotnet build` or `dotnet test` command must use `--no-restore` (and `--no-build` for tests of the existing build).

For an intentional dependency update, change the relevant `PackageReference`, then use the same NuGet version and approved configuration to regenerate locks locally:

```powershell
nuget restore SyncKusto.sln -UseLockFile -ForceEvaluate -ConfigFile NuGet.Config -NonInteractive
```

Review all five lock files together with the project changes: explain every direct/transitive version change and unexpected hash change, and never hand-edit package hashes. To review a genuinely clean resolution, point `NUGET_PACKAGES` and `NUGET_HTTP_CACHE_PATH` at new disposable folders rather than clearing shared caches. Repeat the normal locked restore, build, and appropriate offline tests; the reviewed locks must remain unchanged. Include the updated locks in the dependency-update commit. Never delete locks, use `-ForceEvaluate`, or retry unlocked in CI to bypass a failure.

## Settings
Changes take effect as soon as you successfully click **OK**; restarting the app is not required. If any settings changed, the previous comparison is cleared and you must click **Compare** again before **Update**. Canceling, failing validation, or declining the temporary-database warning does not save changes. Settings cannot be changed while a comparison or update is running.

### Temporary Databases
When the local file system is selected as either the source or the target, Sync Kusto loads its schema definitions into the configured temporary database for Kusto-backed normalization. Specify both a cluster and an existing database where you have permissions to create and delete functions and tables. All selected files on both sides are read and checked before opening a temporary client or changing that database. An invalid file stops the comparison; no partial comparison is returned. After those checks pass, **all existing functions and tables in the temporary database are deleted before loading each file-backed side**. If the database is not empty when you configure it, Settings asks you to confirm that it can be wiped before saving. Use a dedicated scratch database containing no valuable objects or data. Do not point it at a source or target Kusto database. If two users share a temporary database concurrently, results can be incorrect; give each user their own.

### Schema file formats and corrections
The selected extension applies to both reading and writing: `.kql`, or legacy `.csl` when that setting is enabled. Files beneath the schema root's `Functions` folder (including its subfolders) must contain one `.create-or-alter function` definition. Other matching files must contain one `.create table` or `.create-merge table` definition with an explicit column list. Object names must be unqualified: the declaration applies to the configured scratch database, not another database or cluster. File names need not equal declared object names.

Function parameters, bodies, quoted names, comments, multiline strings, dependency references, and cross-database references **inside a function body** are supported. Optional `with` properties are `folder` and `docstring` strings, plus `skipvalidation` and `view` booleans for functions (boolean strings are also accepted). Properties must not repeat. Legacy functions without `skipvalidation` remain supported; normalization supplies `skipvalidation=true` so dependent functions can be loaded in any order. Scripts, extra commands, command pipelines, other properties, unsupported declaration variants, and syntax errors are not accepted.

If Compare stops, its ordinary error dialog identifies **Source** or **Target**, the file path relative to that selected root, and a parser line/column when available. Correct that file to contain just the supported definition, remove other commands or unsupported properties, and compare again. Files are not ignored. Comparison and diff display use the same in-memory snapshot; edits made after it was read are picked up by the next Compare. A failed attempt clears old comparison results and disables Update until a comparison succeeds. Target updates and drop confirmations remain separate from Compare.

Review imported definitions before comparing, especially function bodies. Syntax checks restrict declaration commands; they are not a guarantee that arbitrary function logic is safe. Kusto-backed normalization still submits accepted definitions using the runner's identity and Kusto permissions. Use least-privilege credentials for both the scratch database and any target database, and keep the existing drop confirmation enabled when appropriate.

If a message instead identifies a **parser compatibility issue**, update Sync Kusto or report the issue rather than rewriting an otherwise valid definition. Unrecognized parser layouts still stop the comparison before the temporary database is changed.

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
Issues, additional features, and tests are all welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for more information.
