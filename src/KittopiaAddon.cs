using KittopiaTech.UI;
using KittopiaTech.UI.Debug;
using UnityEngine;

namespace KittopiaTech;

/// <summary>
/// Lives from the first main menu onwards and opens the inspector on Ctrl+P in every scene.
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
        }
    }

    void Update()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.P))
            InspectorWindow.Toggle();
    }
}
