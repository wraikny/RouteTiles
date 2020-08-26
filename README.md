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
$ git submodule update --init
$ dotnet tool restore
```

### Download Altseed
Download artifact `Altseed2-{commit id}` from [Altseed2-csharp](https://github.com/altseed/Altseed2-csharp/tree/fcfce90aa7f26e816ed970cf35bf309691fc2140), and place it as follows

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

### [FAKE](https://fake.build/)  
Scripting at [build.fsx](/build.fsx).  

```shell
$ dotnet fake build -t Clean # Run "Clean" Target
$ dotnet fake build # Run Default Taret
```
