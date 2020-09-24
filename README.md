[![](https://github.com/wraikny/RouteTiles/workflows/CI/badge.svg)](https://github.com/wraikny/RouteTiles/actions?workflow=CI)

# RouteTiles

## Requirements
.NET Core 3.1  
https://dotnet.microsoft.com/download  

```shell
$ dotnet --version
3.1.201
```

### Restoring after Clone
```shell
$ git submodule update --init
$ dotnet tool restore
```

### Download Altseed
Download artifact `Altseed2-{commit id}` from [Altseed2-csharp](https://github.com/altseed/Altseed2-csharp/tree/95f965be486427a94b8ebc1c5e676c447cd5923d), and place it as follows

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

then,

```shell
$ fake build -t copylib
```


### Build

1. Write content of `ResourcesPassword.fs`.  
    You can generate template: `dotnet fake build -t cisetting`.
2. Pack `Resources`: `dotnet fake build -t resources`.
3. Build
    ```shell
    $ dotnet fake build # Build all projects as Release
    $ # or
    $ dotnet build --project src/RouteTiles [-c {Debug|Release}]
    ```

### Run
#### Game

```shell
$ dotnet run --project src/RouteTiles [-c {Debug|Release}]
```

#### Ranking Server (when debugging)

```shell
$ dotnet fake build -t serve
```

### Publish
```shell
$ dotnet fake build -t publish
```
<!-- 
### Tests
```shell
$ dotnet fake build -t Test
``` -->

### [FAKE](https://fake.build/)  
Scripting at [build.fsx](/build.fsx).  

```shell
$ dotnet fake build -t Clean # Run "Clean" Target
$ dotnet fake build # Run Default Taret
```
