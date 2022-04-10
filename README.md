[![](https://github.com/wraikny/RouteTiles/workflows/CI/badge.svg)](https://github.com/wraikny/RouteTiles/actions?workflow=CI)

# RouteTiles

## Requirements
.NET6  
https://dotnet.microsoft.com/download  

```shell
$ dotnet --version
6.0.101
```

### Restoring after Clone
```shell
$ git submodule update --init
$ dotnet tool restore
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
