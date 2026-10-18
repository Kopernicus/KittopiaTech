# Using KittopiaTech
Press Ctrl+P in any scene from the main menu onwards to open an inspector window. Pick a
body on the left (type in the box above the list to show only bodies whose names contain
those words, with what they orbit) and edit its Kopernicus configuration on the right. Edits are staged:
modified rows turn amber, and nothing reaches the planet until you press Apply. Apply also
runs the matching rebuild (sphere, scaled mesh, orbit, rings).

Creating a node or adding a list item (a ring, say) makes the object straight away so you
can edit it, but it stays amber until Apply rebuilds it; Discard removes it again.
Removing a node or item is staged too: a node shows "removed on Apply" with an Undo button,
and a list hides the item and counts it in its header until Apply, Reset or Discard. Apply
destroys it, keeping a copy as config so History can rebuild it. Removing something created
since the last Apply happens straight away. In lists that hold several types, such as a
PQS's Mods, Add opens a searchable list of the types to pick from; Enter picks the top match.

History lists the last 40 Applies made to the body since the game started, newest first,
under an Original entry for the body as loaded (once there are more, the oldest kept entry is
as far back as you can go). Each body keeps its own history, and an open History window
switches to it when you pick another body. Click one to put the body back the way it was after
that Apply (with edits pending, click again to confirm discarding them); later entries stay,
so you can step forward again until you apply something new. Restoring past an Apply that
removed objects rebuilds them from the copies it kept, and earlier entries then apply to the
rebuilt objects. Some edits can't be undone: running an action that asks for confirmation,
or removing an object KittopiaTech can't create, so entries before them can no longer be
restored. Restoring past an Apply that created objects
removes them, and that Apply and those after it leave the list. History isn't saved when
the game closes.

Text holding a localization key such as `#autoLOC_900101` shows what the key reads in game
when hovered.

Add Body, under the body list, copies a body as it is now under a new name. Pick the body to
copy as its Template and, optionally, another body for it to orbit; by default it orbits what
the template orbits, half a turn ahead of it. Around another body it starts halfway out to
that body's sphere of influence. The copy leaves out what other mods add to bodies (they
configure bodies by name and don't know the copy), any space centre, and its template's UBIs.
Its Export Patch writes a new Body using the template's Template node, then the patch
against that stock body.

Delete Body, at the bottom of a body's fields, removes it after a second click. It can't
delete stars, the home world, bodies with moons or bodies with vessels around or on them.

New and deleted bodies last until the game closes; nothing is written to the body's config.
A deleted body's name stays taken until then. Don't save the game with vessels around a new
body: the body won't be there when the save loads.

Export Body writes the whole body as a config; Export Patch writes a ModuleManager patch
holding only what differs from the body's Template. Both write
`PluginData/Kopernicus/<body>.cfg` in the KSP folder (beside `GameData`, so the game doesn't
load it), replacing the body's last export. Move the file into `GameData` to use it.

Fields that name a body, such as the orbit's `referenceBody`, open a list of the bodies in
game when clicked. Applying a new `referenceBody` moves the body there, in the body list
and on the map; a body can't be moved to orbit itself or one of its moons.

Lists of plain text, such as the UBIs a body `implements`, show a text box per entry: clear
one or press its x to remove it, and type in the last box to add one.

Hovering a field's label shows its documentation, if the mod that defines it provides
any (see [Extending KittopiaTech](extending.md#field-docs)).

Clicking a texture or map path opens a searchable list of textures in GameData, in
KSPTextureLoader asset bundles, and built into the game (`BUILTIN/`), starting from the
current value. Click an entry to preview it; double-click or press Select to use it, or
press Enter to use what you typed. Clicking a texture or map thumbnail opens a larger
preview. Textures and on-demand maps that aren't loaded yet are sampled from their file for
thumbnails; only the preview window loads the full texture, and on-demand maps stay
unloaded. Previews show a texture's colour without its alpha, since alpha often holds
something else, like gloss.

Curves show a small graph; click its button for a larger one. Drag keys on the graph to
move them; dragging one past its neighbours reorders the keys, and holding it at the edge of
the graph widens the view in that direction; the scales left of and below the graph follow it.
Double-click empty space to add a key. Under the graph, the selected key's time and value can
be typed, and its tangent mode picked: Auto writes no tangents and lets
KSP choose them, Smooth and Linear follow the neighbouring keys as they move, Flat is level,
and Free keeps tangents as typed. The table below lists every key for exact numbers; typing
a tangent makes that key Free.

Colour ramps that Kopernicus can bake from a gradient (rim and fog colour ramps) are edited
as gradient stops. Click the ramp's button: drag the markers under the ramp to move stops,
double-click to add one, and pick the selected stop's colour or type its time. A ramp
loaded from a texture is read back as the few stops that best match it. Once edited, the ramp
exports as its gradient node (`Gradient` or `FogColorRamp`) rather than a texture path. To
use a texture instead, set the raw property (e.g. `_rimColorRamp`) under Shader properties.

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
properties section, and export as raw `_X` keys plus their `keyword`s. Click their shader's
name to pick another from the shaders loaded in game. Their Keywords list
holds the keywords enabled on the material that no property sets, each with a checkbox.
Unity can't list every keyword a shader has, so to turn on one that isn't listed, type its
name under the list and press Add. Keyword edits are staged like any other.

With [BurstPQS](https://github.com/Phantomical/BurstPQS) installed, Export Scaled Maps
opens its Texture Exporter in the Alt+F12 menu with the current body selected, for
exporting height, colour and normal maps. It replaces Kopernicus's slower Rebuild
ScaledSpace Textures action, which KittopiaTech doesn't show.
