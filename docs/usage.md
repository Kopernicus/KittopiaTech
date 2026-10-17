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
