# GitHub Actions MSBuild Logger

This is a custom `msbuild` and `dotnet` logger that, when run inside GitHub Actions, outputs warnings and errors to GitHub. This includes roslyn analyzers and code analysis.

## Integration

### dotnet

```
name: Build
on: [push]

jobs:
  dotnet:
    runs-on: windows-latest
    env:
      GitHubActionsLogger_Debug: True
    steps:
    - uses: actions/checkout@v1
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
    - uses: StanleyGoldman/setup-GitHubActionsMSBuildLogger@v1
    - name: Run dotnet
      run: |
        dotnet build -c Release .\TestConsoleApp1.sln /logger:GitHubActionsLogger,..\GitHubActionsMSBuildLogger.dll
```

### msbuild

```
name: Build
on: [push]

jobs:
  msbuild:
    runs-on: windows-latest
    env:
      GitHubActionsLogger_Debug: True
    steps:
    - uses: actions/checkout@v1
    - name: Setup MSBuild.exe
      uses: warrenbuckley/Setup-MSBuild@v1
    - uses: StanleyGoldman/setup-GitHubActionsMSBuildLogger@v1
    - name: Run msbuild
      run: |
        msbuild.exe -verbosity:minimal TestConsoleApp1.sln /logger:GitHubActionsLogger,..\GitHubActionsMSBuildLogger.dll
```
