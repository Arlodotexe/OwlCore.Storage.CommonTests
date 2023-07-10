# OwlCore.Storage.CommonTests [![Version](https://img.shields.io/nuget/v/OwlCore.Storage.CommonTests.svg)](https://www.nuget.org/packages/OwlCore.Storage.CommonTests)

Common tests that should pass for all implementations of OwlCore.Storage.


## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Storage.CommonTests). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Storage.CommonTests
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Storage.CommonTests

## Usage

This library should be used with MSTest.

The classes provided in this package are abstract. To use them:
1. Implement a provided class
2. Override and provide an implementation for the required methods
3. Add a `[TestClass]` attribute to your new class.

Tests defined in the base class will be picked up by MSTest. 

### Example:

```cs
[TestClass]
public class IFolderTests : CommonIFolderTests
{
    public override Task<IFolder> CreateFolderAsync()
    {
        var directoryInfo = Directory.CreateDirectory(Path.GetTempPath());

        return Task.FromResult<IFolder>(new SystemFolder(directoryInfo.FullName));
    }

    public override Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        for (var i = 0; i < fileCount; i++)
        {
            var path = Path.Combine(tempFolder, $"File.{i}.tmp");
            using var _ = File.Create(path);
        }

        for (var i = 0; i < folderCount; i++)
        {
            var path = Path.Combine(tempFolder, $"Folder.{i}");
            Directory.CreateDirectory(path);
        }

        return Task.FromResult<IFolder>(new SystemFolder(tempFolder));
    }
}
```


## Financing

We accept donations [here](https://github.com/sponsors/Arlodotexe) and [here](https://www.patreon.com/arlodotexe), and we do not have any active bug bounties.

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

All OwlCore code is licensed under the MIT License. OwlCore is licensed under the MIT License. See the [LICENSE](./src/LICENSE.txt) file for more details.

