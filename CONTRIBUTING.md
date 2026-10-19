# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Building the project
Dependencies:
- Windows and the .NET SDK version pinned in `global.json` (currently `10.0.402`).
- For IDE development and the WinForms designer, a supported [Visual Studio 2026](https://visualstudio.microsoft.com/vs/) installation with the .NET desktop development workload.

Local development:
1. Clone the Sync Kusto repo to your local machine
2. Follow the locked restore, build, and offline test commands in [README.md](README.md#dependency-restore-and-build).
3. Open `SyncKusto.sln` in Visual Studio to run the WinForms application or use its designer.

## Maintainers
Sync Kusto is lovingly maintained by:
- **@benmartens**
- **@nicksw1**
- **@srivas15**

## Feature Ideas
If you're interested in helping but don't have specific ideas for improvements, here are a couple that would make a significant difference in the user experience.
### Temporary Databases
The Settings dialog asks for a temporary cluster and database to use during the comparison. Pushing all the local CSL files to a database lets us use ".show database x schema as json" to easily pull the entire schema into a normalized data structure, but it it would be much nicer if the user didn't have to specify this. It would also be a significant perf improvement  since it takes a while to clean out the database each time. Some ideas are:
1) Using the management api to automatically create and destroy databases.
2) Build a DatabaseSchema object straight from the files without going through the Kusto cluster
3) Host a Kusto cluster locally in memory
### Swap Source and Target
Users frequently set up a comparison between a source and a target but then they want to reverse the comparison. Instead of making them type everything in, there could be a button to swap the settings.

## Submitting Pull Requests

- **DO** submit issues for features. This facilitates discussion of a feature separately from its implementation, and increases the acceptance rates for pull requests.
- **DO NOT** submit large code formatting changes without discussing with the team first.

These two blogs posts on contributing code to open source projects are good too: [Open Source Contribution Etiquette](http://tirania.org/blog/archive/2010/Dec-31.html) by Miguel de Icaza 
and [Don’t “Push” Your Pull Requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) by Ilya Grigorik.

## Creating Issues

- **DO** use a descriptive title that identifies the issue to be addressed or the requested feature. For example, when describing an issue where the comparison is not behaving as expected, 
write your bug title in terms of what the comparison should do rather than what it is doing – “Comparison should parse syntax XYZ in functions”
- **DO** specify a detailed description of the issue or requested feature.
- **DO** provide the following for bug reports
    - Describe the expected behavior and the actual behavior. If it is not self-evident such as in the case of a crash, provide an explanation for why the expected behavior is expected.
    - Specify any relevant exception messages and stack traces.
- **DO** subscribe to notifications for the created issue in case there are any follow up questions.
