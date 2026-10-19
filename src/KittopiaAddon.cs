using KittopiaTech.UI.Debug;
using UnityEngine;

namespace KittopiaTech;

/// <summary>
/// Sets up the UI at the first main menu; the inspector opens from the Alt+F12 menu.
/// </summary>
[KSPAddon(KSPAddon.Startup.MainMenu, once: true)]
public class KittopiaAddon : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(this);

        if (!DebugSkin.Initialize())
        {
            UnityEngine.Debug.LogError("[KittopiaTech] Failed to set up the debug skin, UI disabled");
            Destroy(this);
            return;
        }
        DebugMenu.Register();
    }
}
