# Contributing to KittopiaTech

KittopiaTech is built using [KSPBuildTools](https://github.com/KSPModdingLibs/KSPBuildTools).
In order to build the mod you will need:
- the `dotnet` CLI
- [CKAN](https://github.com/KSP-CKAN/CKAN), to install the mod dependencies

## Installing Dependencies
The mods KittopiaTech depends on, listed in `deps/*.ckan`, are installed into
fake CKAN instances under `deps/installs` so that they don't need to be present
in the KSP install you reference. Run one of the following
in the repository root before building:

On Windows, run
```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\setup-deps.ps1 -ckan path\to\ckan.exe
```

On Linux/MacOS run
```bash
./setup-deps.sh
```

## Referencing KSP
Next, create a `KittopiaTech.props.user` file in the repository root that
points at your KSP install:
```xml
<?xml version="1.0" encoding="UTF-8"?>

<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <ReferencePath>$KSP_ROOT_PATH</ReferencePath>
    </PropertyGroup>
</Project>
```

Alternatively, you can set the `KSP_ROOT` environment variable.

## Building
Run `dotnet build`. The built mod will be placed in `GameData/KittopiaTech`,
which you can then copy or symlink into your KSP install's `GameData` folder.

### Shader bundle
The shaders in `unity/` are built into `src/GameData/Shaders`, which is checked in, so only
rebuild it after changing them. Open `unity/` in Unity 2019.4.18f1 and use KittopiaTech >
Build Shader Bundles, or run:

```
Unity.exe -batchmode -quit -projectPath unity -executeMethod BuildShaderBundles.Build
```

Each platform's bundle needs that platform's build support installed in Unity; missing ones
are skipped.

## Hot reload
KittopiaTech supports [HotReloadKSP](https://github.com/Phantomical/HotReloadKSP): rebuild
and reload the DLL without restarting the game. Open windows are reopened after a reload;
pending edits are not carried over.
