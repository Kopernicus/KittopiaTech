# KittopiaTech
A visual editor for Kopernicus

## Building
KittopiaTech is built using [KSPBuildTools](https://github.com/KSPModdingLibs/KSPBuildTools).
In order to build the mod you will need:
- the `dotnet` CLI
- [CKAN](https://github.com/KSP-CKAN/CKAN), to install the mod dependencies

### Installing Dependencies
The mods KittopiaTech depends on (Kopernicus, ModularFlightIntegrator) are
installed into fake CKAN instances under `deps/installs` so that they don't
need to be present in the KSP install you reference. Run one of the following
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

### Referencing KSP
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

### Building
Run `dotnet build`. The built mod will be placed in `GameData/KittopiaTech`,
which you can then copy or symlink into your KSP install's `GameData` folder.

## Using it
Press Ctrl+P in any scene from the main menu onwards to open an inspector window. Pick a
body on the left and edit its Kopernicus configuration on the right. Edits are staged:
modified rows turn amber, and nothing reaches the planet until you press Apply. Apply also
runs the matching rebuild (sphere, scaled mesh, orbit, rings).

Export body writes the whole body as a config; Export patch writes a ModuleManager patch
holding only what differs from the body's Template. Both land in
`GameData/KittopiaTech/PluginData/<body>/<timestamp>/`.

## Extending KittopiaTech
Other mods can add editors, config writers and rebuild hooks without changing KittopiaTech;
they're found by scanning loaded assemblies for these attributes. The Alt+F12 "Kittopia
Editors" page lists every registered editor.

### Field editors
```cs
[FieldEditor(typeof(MyParser))]           // or a value type, base class, open generic or interface
public sealed class MyEditor : FieldEditor<MyValue>
{
    [SerializeField] TMP_InputField input; // widget references must be serialized fields

    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        // Runs once; build widgets with DebugSkin and store them in serialized fields.
        EditorLayout.Row(transform);
        input = DebugSkin.CreateInputField(transform);
    }

    protected override void Awake() => input.onEndEdit.AddListener(text => Commit(Parse(text)));

    public override void Refresh() => input.text = Format(Value); // never call Commit here
}
```
- `Priority` on the attribute breaks ties; core editors use 0.
- A `public static bool CanEdit(MemberSchema member, Type valueType)` method can reject members
  after the type matched.
- Keep fields on the concrete class: Unity can't serialize fields in generic classes.

### Config writers
Implement `IConfigWriter` and mark it `[ConfigWriter(typeof(T))]` for values the generic
schema walk can't write.

### Apply hooks
```cs
[ApplyHook(typeof(MyLoader))]
static void Rebuild(BodyContext body, MyLoader loader) => loader.Rebuild();
```
Runs after Apply whenever an edit landed on a `MyLoader` or anything beneath it.

## Development
KittopiaTech supports [HotReloadKSP](https://github.com/Phantomical/HotReloadKSP): rebuild
and reload the DLL without restarting the game. Open windows are reopened after a reload;
pending edits are not carried over.
