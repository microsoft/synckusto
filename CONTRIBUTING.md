# Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Building the project
Dependencies:
- Windows and a .NET SDK compatible with `global.json`: `10.0.402` or a newer servicing patch in the `10.0.4xx` feature band.
- For IDE development and the WinForms designer, a supported [Visual Studio 2026](https://visualstudio.microsoft.com/vs/) installation with the .NET desktop development workload.

Both build workflows install the baseline SDK specified in `global.json`. Local and CI SDK resolution use `latestPatch`, selecting the newest installed servicing patch in that same feature band, and use its bundled NuGet client. Older SDKs, other feature bands, minor/major roll-forward, and prerelease versions are not allowed. Locked restores continue to enforce the reviewed dependency graph and package hashes.

Local development:
1. Clone the Sync Kusto repo to your local machine
2. Run the following locked restore, build, and offline test commands from PowerShell at the repository root.
3. Open `SyncKusto.sln` in Visual Studio to run the WinForms application or use its designer.

```powershell
dotnet restore SyncKusto.sln --locked-mode
if ($LASTEXITCODE -ne 0) { throw "Dependency restore failed" }
dotnet build SyncKusto.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
dotnet test SyncKusto.Tests\SyncKusto.Tests.csproj --configuration Release --no-build --no-restore --filter "TestCategory=SchemaImport|TestCategory=Kusto|FullyQualifiedName~SyncKusto.Tests.Forms" -- RunConfiguration.TreatNoTestsAsError=true
if ($LASTEXITCODE -ne 0) { throw "Regression tests failed" }
```

Local restores use the [normal NuGet configuration hierarchy](https://learn.microsoft.com/nuget/consume-packages/configuring-nuget-behavior), including the developer's enabled and disabled sources. Configure trusted sources that can supply the project's public packages. If your environment requires an organization-managed mirror, configure it outside this repository using your administrator's instructions. For authenticated feeds, use the feed's recommended credential provider and add `--interactive` to the restore command when signing in.

The CI regression tests run offline; they do not validate live Kusto authentication or sync operations.

The main window shows a date-based build version in `Build YYYYMMDD` format (currently `Build 20261020`, for October 20, 2026). To update the displayed date, change `label2.Text` in `SyncKusto\MainForm.Designer.cs`.

### Refreshing the README screenshot

On Windows, install PowerShell 7.2 or newer and an SDK compatible with `global.json`, then run:

```powershell
pwsh -NoProfile -File .\scripts\Update-ReadmeScreenshot.ps1
```

The script resolves the repository from its own location, so it also works when invoked by absolute path from another working directory. It performs a locked restore using your normal trusted NuGet configuration, builds the application and test project in Release, and runs only the explicitly selected screenshot fixture. Package restore may require network access; the capture itself is completely offline.

The fixture opens the real main form without requesting activation, with in-memory settings and a fake presenter supplying a fictional modified `CurrentTime` function. It never uses live Kusto connections, credentials, saved settings, or schema files. Windows renders only that window, including its native rich-text diff; no desktop pixels or unrelated windows are captured. A Windows desktop session is required; this is not a browser or headless cross-platform renderer.

Screenshots intentionally omit the entire build-version label, regardless of its value. This is limited to the capture fixture and does not change the production UI. Build-label changes alone do not require refreshing the screenshot.

The command replaces root `screenshot.png` only after capture succeeds and the PNG passes content and size checks. Capture has a timeout, and failures leave the existing image unchanged. Normal test runs do not regenerate the image, and no CI or scheduled regeneration is configured.

Inspect the result before including it with your changes: the build-version label should be absent, and the source and target pickers, expanded comparison, checked function, and red/yellow diff should be visible. The demo content and window dimensions are fixed, but native window chrome, fonts, and theme can vary between Windows environments; byte-identical output across machines is not guaranteed. Agents should follow the refresh guidance in `AGENTS.md`.

### Dependency restore policy

Local development and CI deliberately select sources differently:

- The root `NuGet.Config` clears fallback package folders but does not declare or clear package sources. Local restore commands omit `--configfile` so the developer's applicable source settings, source mappings, authentication, and audit sources remain available. Use only trusted, approved sources; do not add an unapproved public fallback to bypass environment restrictions.
- CI explicitly selects `.github\NuGet.Config`, which clears inherited sources and fallback package folders and permits only the HTTPS NuGet.org v3 feed. CI restores remain noninteractive and do not inherit private workstation configuration.

Keep private source configuration, credentials, and restore diagnostics outside the public repository. Do not commit or upload generated restore metadata that can contain private source addresses.

`Directory.Build.props` continues to disable the SDK's implicit fallback and workload `library-packs` feeds. NuGet's existing signature verification remains enabled; this policy does not introduce trusted-signer certificate pinning or require all packages to be signed.

All five solution projects have a reviewed `packages.lock.json` containing resolved direct/transitive versions and package content hashes. CI checks that every lock file exists before restoring, fails on dependency or content-hash drift, and builds only after a successful locked restore. Subsequent `dotnet build` and `dotnet test` commands must use `--no-restore` (and `--no-build` for tests of the existing build).

An approved mirror must supply packages matching the reviewed content hashes. Changing package sources is not a reason to regenerate lock files.

### Updating dependencies

For an intentional dependency or framework update, change the relevant project references or target framework, then use an SDK compatible with `global.json` and approved configuration to regenerate locks locally:

```powershell
dotnet restore SyncKusto.sln --use-lock-file --force-evaluate
```

This uses the same inherited local source settings as normal development. Dependency updates must also remain restorable using the public-only CI configuration; do not introduce dependencies available only from private feeds.

Review all five lock files together with the project changes: explain every direct/transitive version change and unexpected hash change, and never hand-edit package hashes. To review a genuinely clean resolution, point `NUGET_PACKAGES` and `NUGET_HTTP_CACHE_PATH` at new disposable folders rather than clearing shared caches.

Repeat the normal locked restore, build, and appropriate offline tests; the reviewed locks must remain unchanged. Include the updated locks in the dependency-update commit. Servicing patches within the allowed feature band do not require editing `global.json`; raising the minimum SDK version or changing feature bands does. Verify the locks when changing the SDK, and never regenerate them just to accommodate a roll-forward failure. Never delete locks, use `--force-evaluate`, or retry unlocked in CI to bypass a failure.

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
