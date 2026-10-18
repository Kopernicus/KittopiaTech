# Using KittopiaTech
Press Ctrl+P in any scene from the main menu onwards to open an inspector window. Pick a
body on the left and edit its Kopernicus configuration on the right. Edits are staged:
modified rows turn amber, and nothing reaches the planet until you press Apply. Apply also
runs the matching rebuild (sphere, scaled mesh, orbit, rings).

Export body writes the whole body as a config; Export patch writes a ModuleManager patch
holding only what differs from the body's Template. Both land in
`GameData/KittopiaTech/PluginData/<body>/<timestamp>/`.

Hovering a field's label shows its documentation, if the mod that defines it provides
any (see [Extending KittopiaTech](extending.md#field-docs)).

Clicking a texture or map path opens a searchable list of textures in GameData, in
KSPTextureLoader asset bundles, and built into the game (`BUILTIN/`), starting from the
current value. Click an entry to preview it; double-click or press Select to use it, or
press Enter to use what you typed. Clicking a texture or map thumbnail opens a larger
preview. Textures and on-demand maps that aren't loaded yet are sampled from their file for
thumbnails; only the preview window loads the full texture, and on-demand maps stay
unloaded. Previews show a texture's colour without its alpha, since alpha often holds
something else, like gloss. This needs KittopiaTech's shader bundle, which is only built for
Windows so far; elsewhere, previews show alpha as transparency.

Curves show a small graph; click its button for a larger one. Drag keys on the graph to
move them, and double-click empty space to add a key. Under the graph, the selected key's
time and value can be typed, and its tangent mode picked: Auto writes no tangents and lets
KSP choose them, Smooth and Linear follow the neighbouring keys as they move, Flat is level,
and Free keeps tangents as typed. The table below lists every key for exact numbers; typing
a tangent makes that key Free.

Each material (the scaled and PQS surface materials, ocean and scatter materials) has a
Shader properties section listing every property of its shader by its raw name, e.g.
`_MainTex`, including those with no named field. Hover a property for the shader's
description and range. Materials on spawned bodies need a Kopernicus build with
`MaterialLoader.CreateExisting`; without it, those materials don't appear.

Properties whose shader marks them `[Toggle]`, `[ToggleOff]` or `[KeywordEnum]` show as a
checkbox or a dropdown, and set their keywords as well as the property, as Unity's material
inspector does in the editor. They show the keyword that's set, since that picks what
renders.

Shaders Kopernicus has no loader for, such as a planet pack's own, get only the Shader
properties section, and export as raw `_X` keys plus their `keyword`s. Their Keywords list
holds the keywords enabled on the material that no property sets, each with a checkbox.
Unity can't list every keyword a shader has, so to turn on one that isn't listed, type its
name under the list and press Add. Keyword edits are staged like any other.

With [BurstPQS](https://github.com/Phantomical/BurstPQS) installed, Export scaled maps
opens its Texture Exporter in the Alt+F12 menu with the current body selected, for
exporting height, colour and normal maps.
