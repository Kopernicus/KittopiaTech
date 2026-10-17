# KittopiaTech
A visual editor for Kopernicus

## Using it
Press Ctrl+P in any scene from the main menu onwards to open an inspector window. Pick a
body on the left and edit its Kopernicus configuration on the right. Edits are staged:
modified rows turn amber, and nothing reaches the planet until you press Apply. Apply also
runs the matching rebuild (sphere, scaled mesh, orbit, rings).

Export body writes the whole body as a config; Export patch writes a ModuleManager patch
holding only what differs from the body's Template. Both land in
`GameData/KittopiaTech/PluginData/<body>/<timestamp>/`.

Hovering a field's label shows its documentation: the `<summary>` from the XML doc file a
mod ships beside its DLL (`Plugins/Mod.xml` next to `Plugins/Mod.dll`), or else its
`[KittopiaDescription]`.

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
[ApplyHook(typeof(MyLoader), Replaces = nameof(MyLoader.Rebuild))]
static void Rebuild(BodyContext body, MyLoader loader) => loader.Rebuild();
```
Runs after Apply whenever an edit landed on a `MyLoader` or anything beneath it. `Replaces`
hides the loader's own `[KittopiaAction]` button for the same rebuild.

## Building
See [CONTRIBUTING.md](CONTRIBUTING.md).
