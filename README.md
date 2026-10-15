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

## Settings
Changes take effect as soon as you successfully click **OK**; restarting the app is not required. If any settings changed, the previous comparison is cleared and you must click **Compare** again before **Update**. Canceling, failing validation, or declining the temporary-database warning does not save changes. Settings cannot be changed while a comparison or update is running.

### Temporary Databases
When the local file system is selected as either the source or the target, Sync Kusto loads the schema files into the temporary Kusto database you configure. Specify both a cluster and an existing database where you have permissions to create and delete functions and tables. **All existing functions and tables in this database will be dropped every time a comparison is run!** If the database is not empty when you configure it, Settings asks you to confirm that it can be wiped before saving. Use a dedicated, disposable database, never a database containing data you need to keep. If two users run a comparison pointing to the same temporary database at the same time, they'll get incorrect results. Ideally every user has their own temporary database.

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
