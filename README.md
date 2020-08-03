[![](https://github.com/wraikny/RouteTiles/workflows/CI/badge.svg)](https://github.com/wraikny/RouteTiles/actions?workflow=CI)

# RouteTiles
distributed under [MIT License](/LICENSE)

## Requirements
.NET Core 3.1  
https://dotnet.microsoft.com/download  

```shell
$ dotnet --version
3.1.201
```

### Restoring after Clone
```shell
$ dotnet tool restore
```

### Download Altseed
Download artifact `Altseed2-{commit id}` from [Altseed2-csharp](https://github.com/altseed/Altseed2-csharp/tree/c05605fffaaed70b81c8a09c2ac108b8a57c9452), and place it as follows

```
lib
|--.gitkeep
|--Altseed2
|  |--Altseed2.dll
|  |--Altseed2.xml
|  |--Altseed2_Core.dll
|  |--libAltseed2_Core
|  |--libAltseed2_Core.dylib
|  |--LICENSE
```


### Build
```shell
$ dotnet fake build # Build all projects as Release
$ # or
$ dotnet build --project src/RouteTiles [-c {Debug|Release}]
```

### Run
```shell
$ dotnet run --project src/RouteTiles [-c {Debug|Release}]
```
<!-- 
### Tests
```shell
$ dotnet fake build -t Test
``` -->

## References
### [Paket](https://fsprojects.github.io/Paket/index.html)  
Each project requires `paket.references` file.

After updating [paket.dependencies](/paket.dependencies):
```shell
$ dotnet paket install
```

To Update Versions of Libraries,
```shell
$ dotnet paket update
```

### [FAKE](https://fake.build/)  
Scripting at [build.fsx](/build.fsx).  

```shell
$ dotnet fake build -t Clean # Run "Clean" Target
$ dotnet fake build # Run Default Taret
```

### Create Project
```shell
$ # Application
$ dotnet new console -lang=f# -o src/SampleApp
$ echo 'FSharp.Core' > src/SampleApp/paket.references
$ paket install

$ # Library
$ dotnet new classlib -lang=f# -o src/SampleLib
$ echo 'FSharp.Core' > src/SampleLib/paket.references
$ paket install
```

### Create Test Project
```shell
$ dotnet new console -lang=f# -o tests/SampleTest
$ echo -e 'FSharp.Core\nExpecto\nExpecto.FsCheck' > tests/SampleTest/paket.references

$ paket install # Add reference of Paket to .fsproj file
```
and then, Add **Project Name** to [build.fsx](/build.fsx).

### Update Tool
```shell
$ dotnet fake build -t Tool
```
and then, commit [.config/dotnet-tools.json](/.config/dotnet-tools.json).

## Link
- [Paket（.NETのパッケージマネージャー）とFAKE（F#のMake）について - anti scroll](https://tategakibunko.hatenablog.com/entry/2019/07/09/123655)
- [.NET Core 3.0 の新機能 #ローカルツール - Microsoft Docs](https://docs.microsoft.com/ja-jp/dotnet/core/whats-new/dotnet-core-3-0#local-tools)
