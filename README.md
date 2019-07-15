# Sync Kusto

The Sync Kusto tool was built to help create a maintainable development process around [Kusto](https://docs.microsoft.com/en-us/azure/data-explorer/). The tool makes 
it easy to sync database schemas using either the local file system or a Kusto cluster as either the source of the target. It will copy full function definitions along 
with table schemas. Table data is not synced.

## Getting Started
Assuming that the user already has a Kusto database with functions and tables, set the Kusto database as the source and the local file system as the target. Press the 
compare button and the tool will find that none of the database objects are in the target. Press the update button to download everything from Kusto to the local file 
system.

## Recommended Team Process
The general usage pattern on our team is to make changes in a Kusto dev database which starts with a schema that mirrors the production database. Once the developer is 
satisfied with the changes, they use Sync Kusto with the development database as the source and their local file system as the target. The changes are committed to source 
control and a pull request is sent out for review. After the request is approved and the code merges to master, the developer syncs the master branch and uses Sync Kusto 
to sync from their local file system to the production Kusto database. This is a more flexible/manual version of the [Azure Data Explorer task for Azure Dev Ops]
(https://docs.microsoft.com/en-us/azure/data-explorer/devops). We've had great success with this process and use the tool daily to help with our development and for deploying 
to production assets.

## Settings
###Temporary Databases
When the local file system is selected as either the source or the target, Sync Kusto creates a temporary database containing all of the CSL files on the local file system. 
Specify a Kusto cluster and database where you have permissions to not only create new functions and tables but also to delete all the functions and tables that exist there 
already. If two users run a comparison pointing to the same temporary database at the same time, they'll get incorrect results. Ideally every user has their own temporary database. 
(This is one of the top areas where the project could use some improvement. Please see [CONTRIBUTING.md](CONTRIBUTING.md) if you're interested in pitching in!)

### AAD Authority
If your username is in the form of UPN (user@contoso.com), you can hit the common AAD endpoint and your home tenant will be resolved automatically. If your tenant's automatic 
resolution does not work, you need to specify the AAD authority in the settings. Note that if you're planning to connect via an AAD application id and key then this setting is 
required regardless of tenant configuration.

## Contributing
Issues, additional features, and tests are all welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for more information.