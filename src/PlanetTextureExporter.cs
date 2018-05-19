using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KittopiaTech
{
    public class PlanetTextureExporter
    {
        public struct TextureOptions
        {
            public Boolean ExportColor;

            public Boolean ExportHeight;

            public Boolean ExportNormal;

            public Boolean TransparentMaps;

            public Boolean SaveToDisk;

            public Boolean ApplyToScaled;

            public Single NormalStrength;
        }

        public static void UpdateTextures(CelestialBody celestialBody, TextureOptions options)
        {
            // If the user wants to export normals, we need height too
            if (options.ExportNormal)
            {
                options.ExportHeight = true;
            }

            // Prepare the PQS
            PQS pqsVersion = celestialBody.pqsController;

            // If the PQS is null, abort
            if (pqsVersion == null)
            {
                throw new InvalidOperationException();
            }

            // Tell the PQS that we are going to build maps
            pqsVersion.isBuildingMaps = true;
            pqsVersion.isFakeBuild = true;

            // Get the mod building methods from the PQS
            Action<PQS.VertexBuildData> modOnVertexBuildHeight =
                (Action<PQS.VertexBuildData>) Delegate.CreateDelegate(
                    typeof(Action<PQS.VertexBuildData>),
                    pqsVersion,
                    typeof(PQS).GetMethod("Mod_OnVertexBuildHeight",
                        BindingFlags.Instance | BindingFlags.NonPublic));
            Action<PQS.VertexBuildData> modOnVertexBuild = (Action<PQS.VertexBuildData>) Delegate.CreateDelegate(
                typeof(Action<PQS.VertexBuildData>),
                pqsVersion,
                typeof(PQS).GetMethod("Mod_OnVertexBuild", BindingFlags.Instance | BindingFlags.NonPublic));

            // Disable the PQS
            pqsVersion.gameObject.SetActive(false);

            // Get all mods the PQS is connected to
            PQSMod[] mods = pqsVersion.GetComponentsInChildren<PQSMod>()
                .Where(m => m.sphere == pqsVersion && m.modEnabled).ToArray();

            // Create the Textures
            Texture2D colorMap = new Texture2D(pqsVersion.mapFilesize, pqsVersion.mapFilesize / 2,
                TextureFormat.ARGB32,
                true);
            Texture2D heightMap = new Texture2D(pqsVersion.mapFilesize, pqsVersion.mapFilesize / 2,
                TextureFormat.RGB24,
                true);

            // Arrays
            Color[] colorMapValues = new Color[pqsVersion.mapFilesize * (pqsVersion.mapFilesize / 2)];
            Color[] heightMapValues = new Color[pqsVersion.mapFilesize * (pqsVersion.mapFilesize / 2)];


            // Create a VertexBuildData
            PQS.VertexBuildData data = new PQS.VertexBuildData();

            // Loop through the pixels
            for (Int32 y = 0; y < (pqsVersion.mapFilesize / 2); y++)
            {
                for (Int32 x = 0; x < pqsVersion.mapFilesize; x++)
                {
                    // Update Message
                    // Double percent = ((double) ((y*pqs.mapFilesize) + x)/((pqs.mapFilesize/2)*pqs.mapFilesize))*100;
                    // while (CanvasUpdateRegistry.IsRebuildingLayout()) Thread.Sleep(10);
                    // message.textInstance.text.text = "Generating Planet-Maps: " + percent.ToString("0.00") + "%";

                    // Update the VertexBuildData
                    data.directionFromCenter =
                        QuaternionD.AngleAxis((360d / pqsVersion.mapFilesize) * x, Vector3d.up) *
                        QuaternionD.AngleAxis(90d - 180d / (pqsVersion.mapFilesize / 2f) * y, Vector3d.right)
                        * Vector3d.forward;
                    data.vertHeight = pqsVersion.radius;

                    // Build from the Mods 
                    Double height = Double.MinValue;
                    if (options.ExportHeight)
                    {
                        modOnVertexBuildHeight(data);

                        // Adjust the height
                        height = (data.vertHeight - pqsVersion.radius) * (1d / pqsVersion.mapMaxHeight);
                        if (height < 0)
                        {
                            height = 0;
                        }
                        else if (height > 1)
                        {
                            height = 1;
                        }

                        // Set the Pixels
                        heightMapValues[y * pqsVersion.mapFilesize + x] =
                            new Color((Single) height, (Single) height, (Single) height);
                    }

                    if (options.ExportColor)
                    {
                        modOnVertexBuild(data);

                        // Adjust the Color
                        Color color = data.vertColor;
                        if (!pqsVersion.mapOcean)
                        {
                            color.a = 1f;
                        }
                        else if (height > pqsVersion.mapOceanHeight)
                        {
                            color.a = options.TransparentMaps ? 0f : 1f;
                        }
                        else
                        {
                            color = pqsVersion.mapOceanColor.A(1f);
                        }

                        // Set the Pixels
                        colorMapValues[y * pqsVersion.mapFilesize + x] = color;
                    }
                }
            }

            // Serialize the maps to disk
            String path = KSPUtil.ApplicationRootPath + "/GameData/KittopiaTech/Textures/" +
                          celestialBody.transform.name + "/";
            Directory.CreateDirectory(path);

            // Colormap
            if (options.ExportColor)
            {
                // Save it
                colorMap.SetPixels(colorMapValues);
                if (options.SaveToDisk)
                {
                    File.WriteAllBytes(path + celestialBody.transform.name + "_Color.png", colorMap.EncodeToPNG());
                }

                // Apply it
                if (options.ApplyToScaled)
                {
                    colorMap.Apply();
                    celestialBody.scaledBody.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", colorMap);
                }
            }

            if (options.ExportHeight)
            {
                heightMap.SetPixels(heightMapValues);
                if (options.SaveToDisk)
                {
                    File.WriteAllBytes(path + celestialBody.transform.name + "_Height.png",
                        heightMap.EncodeToPNG());
                }

                if (options.ExportNormal)
                {
                    // Bump to Normal Map
                    Texture2D normalMap = BumpToNormalMap(heightMap, options.NormalStrength);
                    if (options.SaveToDisk)
                    {
                        File.WriteAllBytes(path + celestialBody.transform.name + "_Normal.png",
                            normalMap.EncodeToPNG());
                    }

                    // Apply it
                    if (options.ApplyToScaled)
                    {
                        normalMap.Apply();
                        celestialBody.scaledBody.GetComponent<MeshRenderer>().material
                            .SetTexture("_BumpMap", normalMap);
                    }
                }
            }

            // Close the Renderer
            pqsVersion.isBuildingMaps = false;
            pqsVersion.isFakeBuild = false;
        }

        // Credit goes to Kragrathea.
        public static Texture2D BumpToNormalMap(Texture2D source, Single strength)
        {
            strength = Mathf.Clamp(strength, 0.0F, 10.0F);
            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true);
            for (Int32 by = 0; by < result.height; by++)
            {
                for (Int32 bx = 0; bx < result.width; bx++)
                {
                    Single xLeft = source.GetPixel(bx - 1, by).grayscale * strength;
                    Single xRight = source.GetPixel(bx + 1, by).grayscale * strength;
                    Single yUp = source.GetPixel(bx, by - 1).grayscale * strength;
                    Single yDown = source.GetPixel(bx, by + 1).grayscale * strength;
                    Single xDelta = (xLeft - xRight + 1) * 0.5f;
                    Single yDelta = (yUp - yDown + 1) * 0.5f;
                    result.SetPixel(bx, by, new Color(yDelta, yDelta, yDelta, xDelta));
                }
            }
            return result;
        }

        /// <summary>
        /// Generates a thumbnail for the planet
        /// </summary>
        public static Texture2D GetPlanetThumbnail(CelestialBody body)
        {
            // Config
            RuntimePreviewGenerator.TransparentBackground = true;
            RuntimePreviewGenerator.BackgroundColor = Color.clear;
            RuntimePreviewGenerator.PreviewDirection = Vector3.forward;
            RuntimePreviewGenerator.Padding = -0.15f;
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponentInChildren<MeshFilter>().sharedMesh =
                body.scaledBody.GetComponent<MeshFilter>().sharedMesh;
            sphere.GetComponentInChildren<MeshRenderer>().sharedMaterial =
                body.scaledBody.GetComponent<MeshRenderer>().sharedMaterial;
            
            Texture2D finalTexture = RuntimePreviewGenerator.GenerateModelPreview(sphere.transform, 256, 256);
            UnityEngine.Object.DestroyImmediate(sphere);
            return finalTexture;
        }
    }
}