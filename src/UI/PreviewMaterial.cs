using System.IO;
using UnityEngine;

namespace KittopiaTech.UI;

/// <summary>
/// A UI material that draws a texture's colour and ignores its alpha, for previews of textures
/// whose alpha holds something else, like gloss. It comes from the shader bundle built by the
/// Unity project in unity/; without one for this platform, previews use the default material.
/// </summary>
internal static class PreviewMaterial
{
    const string ShaderName = "KittopiaTech/UI/IgnoreAlpha";

    static Material material;
    static bool loaded;

    /// <summary>The material, or null to use the default UI material.</summary>
    public static Material IgnoreAlpha
    {
        get
        {
            if (!loaded)
            {
                loaded = true;
                var shader = LoadShader();
                if (shader != null)
                    material = new Material(shader);
            }
            return material;
        }
    }

    static Shader LoadShader()
    {
        string platform = Application.platform switch
        {
            RuntimePlatform.WindowsPlayer => "windows",
            RuntimePlatform.LinuxPlayer => "linux",
            RuntimePlatform.OSXPlayer => "macos",
            _ => null,
        };
        string path = Path.Combine(
            KSPUtil.ApplicationRootPath,
            "GameData",
            "KittopiaTech",
            "Shaders",
            $"kittopiatech-{platform}.unity3d"
        );
        if (platform == null || !File.Exists(path))
        {
            UnityEngine.Debug.LogWarning($"[KittopiaTech] No shader bundle at {path}; previews will show alpha");
            return null;
        }

        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
            return null;
        var shader = bundle.LoadAsset<Shader>("Assets/Shaders/IgnoreAlpha.shader");
        bundle.Unload(false);
        if (shader == null || !shader.isSupported)
        {
            UnityEngine.Debug.LogWarning($"[KittopiaTech] {ShaderName} isn't usable here; previews will show alpha");
            return null;
        }
        return shader;
    }
}
