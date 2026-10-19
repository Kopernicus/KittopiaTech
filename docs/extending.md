# Extending KittopiaTech
Other mods can add editors, config writers and rebuild hooks without changing KittopiaTech;
they're found by scanning loaded assemblies for these attributes. The "Kittopia Editors"
page, under Kittopia in the Alt+F12 menu, lists every registered editor.

## Field editors
```cs
[FieldEditor(typeof(MyParser))] // or a value type, base class, open generic or interface
public sealed class MyEditor : FieldEditor<MyValue>
{
    [SerializeField] TMP_InputField input; // widget references must be serialized fields

    // Set up a prefab under the current object.
    // It will be instantiated as needed for fields.
    protected internal override void BuildPrefab(PrefabContext ctx)
    {
        EditorLayout.Row(transform);
        input = DebugSkin.CreateInputField(transform);
    }

    protected override void Awake() =>
        input.onEndEdit.AddListener(text => Commit(Parse(text)));

    public override void Refresh() =>
        input.text = Format(Value); // never call Commit here
}
```
- Use `Priority` in the attribute to break ties. Defaults to 0.
- A `public static bool CanEdit(MemberSchema member, Type valueType)` method can
  reject members after the type matched.

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
A normal C# doc comment will be turned into a tooltip on the field. Build your project
with `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and ship the
generated XML beside your DLL. Kittopia will automatically pick up the member
documentation. You can also add a `[KittopiaDescription]` attribute which will take
precedence over the doc comment.
