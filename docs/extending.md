# Extending KittopiaTech
Other mods can add editors, config writers and rebuild hooks without changing KittopiaTech;
they're found by scanning loaded assemblies for these attributes. The "Kittopia Editors"
page, under Kittopia in the Alt+F12 menu, lists every registered editor.

## Field editors
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

## Config writers
Implement `IConfigWriter` and mark it `[ConfigWriter(typeof(T))]` for values the generic
schema walk can't write.

## Apply hooks
```cs
[ApplyHook(typeof(MyLoader), Replaces = nameof(MyLoader.Rebuild))]
static void Rebuild(BodyContext body, MyLoader loader) => loader.Rebuild();
```
Runs after Apply whenever an edit landed on a `MyLoader` or anything beneath it. `Replaces`
hides the loader's own `[KittopiaAction]` button for the same rebuild.

## Field docs
Field tooltips come from the `<summary>` doc comments on your loader's members. Build with
`<GenerateDocumentationFile>true</GenerateDocumentationFile>` and ship the generated XML
beside the DLL (`Plugins/MyMod.xml` next to `Plugins/MyMod.dll`). Members without a summary
fall back to their `[KittopiaDescription]`.
