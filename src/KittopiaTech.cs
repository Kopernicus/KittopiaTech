using KittopiaTech.UI;
using KittopiaTech.UI.Framework;
using UnityEngine;

namespace KittopiaTech
{
    /// <summary>
    /// The main MonoBehaviour. Here we create the UI
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KittopiaTech : MonoBehaviour
    {
        void Start()
        {
            // Stop the Garbage Collector
            DontDestroyOnLoad(this);
            
            // Init the UI
            PlanetSelector.Instance.Init();
        }

        void Update()
        {
            // Check if the user wants to open the UI
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                // Open or close the main window
                if (!PlanetSelector.Instance.IsOpen)
                {
                    PlanetSelector.Instance.Open();
                }
                else
                {
                    PlanetSelector.Instance.ToggleVisibility();
                }
            }
        }

        void OnGUI()
        {
            if (Tools.KittopiaSkin == null)
            {
                Tools.KittopiaSkin = StyleConverter.Convert(GUI.skin);
            }
        }
    }
}