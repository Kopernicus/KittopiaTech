using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.Components.ModularScatter;
using Upgradeables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KittopiaTech.Model;

/// <summary>Changes to which bodies exist and what they orbit, made while the game runs.</summary>
public static class Bodies
{
    /// <summary>Raised after a body is added, removed or moves to another parent.</summary>
    public static event Action Changed;

    internal static void RaiseChanged() => Changed?.Invoke();

    /// <summary>Bodies made by <see cref="Create"/>, with the Template node their config would use.</summary>
    static readonly Dictionary<CelestialBody, ConfigNode> Created = [];

    /// <summary>The Template node for a body made in game, or null for one loaded from config.</summary>
    public static ConfigNode CreatedTemplate(CelestialBody body) => Created.TryGetValue(body, out var t) ? t : null;

    /// <summary>Whether <paramref name="body"/> is <paramref name="ancestor"/> or orbits it, however distantly.</summary>
    public static bool Orbits(CelestialBody body, CelestialBody ancestor)
    {
        for (var b = body; b != null; b = b.referenceBody == b ? null : b.referenceBody)
        {
            if (b == ancestor)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Moves <paramref name="body"/> into its orbit's reference body's list of orbiting bodies,
    /// which the loader's setter leaves alone. The tree, SOI checks and map view walk that list.
    /// </summary>
    internal static void SyncParent(CelestialBody body)
    {
        var parent = body.orbit?.referenceBody;
        if (parent == null || parent.orbitingBodies.Contains(body))
            return;

        foreach (var other in FlightGlobals.Bodies)
            other.orbitingBodies.Remove(body);
        parent.orbitingBodies.Add(body);
        RaiseChanged();
    }

    /// <summary>Why a body named <paramref name="name"/> can't be copied from <paramref name="source"/>, or null.</summary>
    public static string CantCreate(CelestialBody source, string name, CelestialBody parent)
    {
        if (source == null)
            return "Pick a body to copy";
        if (string.IsNullOrEmpty(name?.Trim()))
            return "Enter a name";
        if (FlightGlobals.Bodies.Any(b => b.transform.name == name || b.bodyName == name))
            return $"There's already a body named {name}";
        if (source.orbitDriver == null)
            return $"{source.bodyName} orbits nothing, so it can't be copied";
        if (parent == null)
            return "Pick a body for it to orbit";
        return null;
    }

    /// <summary>
    /// Spawns a copy of <paramref name="source"/> as it is now, orbiting <paramref name="parent"/>,
    /// the way PSystemManager.SpawnBody spawns bodies at startup. It lasts until the game closes.
    /// </summary>
    public static CelestialBody Create(CelestialBody source, string name, CelestialBody parent)
    {
        string problem = CantCreate(source, name, parent);
        if (problem != null)
            throw new InvalidOperationException(problem);

        // Copied while inactive, so nothing runs Awake until it's wired up.
        var holder = new GameObject("KittopiaTech_Spawning");
        holder.SetActive(false);
        try
        {
            var go = Object.Instantiate(source.gameObject, holder.transform);
            var scaled = Object.Instantiate(source.scaledBody, holder.transform);
            go.name = scaled.name = name;
            var body = go.GetComponent<CelestialBody>();
            var driver = go.GetComponent<OrbitDriver>();
            var pqs = go.GetComponentInChildren<PQS>(true);

            body.bodyName = name;
            body.bodyDisplayName = body.bodyAdjectiveDisplayName = name;
            body.isHomeWorld = false;
            body.orbitingBodies = [];
            body.flightGlobalsIndex = FlightGlobals.Bodies.Count;
            body.scaledBody = scaled;
            body.pqsController = pqs;
            if (pqs != null)
            {
                foreach (var sphere in pqs.GetComponentsInChildren<PQS>(true))
                {
                    sphere.name = sphere.name.Replace(source.bodyName, name);
                    sphere.isStarted = false;
                }
                pqs.isAlive = false;
            }
            RemoveSourceIdentity(go, body);
            RemoveStrays(go);
            RemoveStrays(scaled);
            // A scatter moves itself from its land control to the sphere when it starts, and
            // finds the land control as its parent, so the copy needs it back there.
            foreach (var scatter in go.GetComponentsInChildren<ModularScatter>(true).Where(s => s.landControl != null))
                scatter.transform.SetParent(scatter.landControl.transform, false);

            var orbit = NewOrbit(source, parent);
            driver.orbit = orbit;
            driver.celestialBody = body;
            driver.referenceBody = parent;

            var map = new Dictionary<Object, Object>
            {
                [source] = body,
                [source.gameObject] = go,
                [source.transform] = go.transform,
                [source.scaledBody] = scaled,
                [source.scaledBody.transform] = scaled.transform,
                [source.orbitDriver] = driver,
            };
            if (source.pqsController != null)
                map[source.pqsController] = pqs;
            Remap(go, scaled, map);
            var mapObject = scaled.GetComponent<ScaledMovement>();
            mapObject.tgtRef = go.transform;
            mapObject.celestialBody = body;
            mapObject.orbit = orbit;
            body.MapObject = mapObject;

            if (PSystemManager.OrbitRendererDataCache.TryGetValue(source, out var rendererData))
                PSystemManager.OrbitRendererDataCache[body] = new KopernicusOrbitRendererData(body, rendererData);

            // Activates both; CelestialBody.Awake adds the body to its parent's orbitingBodies.
            var system = PSystemManager.Instance;
            var spaceCenter = SpaceCenter.Instance;
            go.transform.SetParent(source.transform.parent, false);
            scaled.transform.SetParent(source.scaledBody.transform.parent, false);
            SpaceCenter.Instance = spaceCenter;

            system.localBodies.Add(body);
            system.scaledBodies.Add(scaled);
            FlightGlobals.Bodies.Add(body);
            PlanetariumCamera.fetch.targets.Add(mapObject);
            if (!parent.orbitingBodies.Contains(body))
                parent.orbitingBodies.Add(body);

            Created[body] = TemplateFor(source);
            Debug.Log($"[KittopiaTech] Created {name} from {source.bodyName}, orbiting {parent.bodyName}");
            RaiseChanged();
            return body;
        }
        finally
        {
            Object.Destroy(holder);
        }
    }

    /// <summary>The source's orbit, moved to <paramref name="parent"/> and half a turn ahead.</summary>
    static Orbit NewOrbit(CelestialBody source, CelestialBody parent)
    {
        var o = source.orbit;
        double sma = o.semiMajorAxis;
        // Another parent's SOI may not reach the source's distance; settle halfway out.
        if (parent != o.referenceBody && !double.IsInfinity(parent.sphereOfInfluence))
            sma = Math.Max(parent.Radius * 3, parent.sphereOfInfluence / 2);
        return new Orbit(o.inclination, o.eccentricity, sma, o.LAN, o.argumentOfPeriapsis,
            o.meanAnomalyAtEpoch + Math.PI, o.epoch, parent);
    }

    /// <summary>
    /// Drops what makes the copy answer for its source: its UBIs, which lookups by UBI would
    /// otherwise find first, its scaled mesh cache file, and the space centre, whose
    /// facilities register themselves.
    /// </summary>
    static void RemoveSourceIdentity(GameObject go, CelestialBody body)
    {
        var ubis = go.transform.Find("UBI");
        if (ubis != null)
            Object.DestroyImmediate(ubis.gameObject);
        body.Set<string>("identifier", null);
        body.Set("implements", new List<string>());
        // Rebuilding the scaled mesh would otherwise overwrite the source's cached mesh.
        body.Set<string>("cacheFile", null);

        var facilities = go.GetComponentsInChildren<SpaceCenter>(true).Cast<Component>()
            .Concat(go.GetComponentsInChildren<UpgradeableObject>(true))
            .Select(c => c.GetComponentInParent<PQSCity>()?.gameObject ?? c.gameObject)
            .Distinct()
            .ToList();
        foreach (var facility in facilities)
            Object.DestroyImmediate(facility);
    }

    static readonly string[] OwnAssemblies = ["Assembly-CSharp", "Kopernicus", "UnityEngine"];

    /// <summary>
    /// Drops what was copied without belonging to the body: cameras that follow it, which
    /// sit under the body they focus on, and other mods' components. Mods set those up from
    /// their own per-body configs, which don't know the copy, and Parallax's throw every frame.
    /// </summary>
    static void RemoveStrays(GameObject root)
    {
        foreach (var camera in root.GetComponentsInChildren<Camera>(true).Where(c => c != null && c.gameObject != root))
            Object.DestroyImmediate(camera.gameObject);

        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            string assembly = component != null ? component.GetType().Assembly.GetName().Name : null;
            if (assembly != null && !OwnAssemblies.Any(a => assembly.StartsWith(a)))
                Object.DestroyImmediate(component);
        }
    }

    /// <summary>
    /// Points references to the source's objects at the copy's. Instantiate only remaps
    /// references within the copied hierarchy, and the body and its scaled version are two.
    /// </summary>
    static void Remap(GameObject go, GameObject scaled, Dictionary<Object, Object> map)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var components = go.GetComponentsInChildren<Component>(true).Concat(scaled.GetComponentsInChildren<Component>(true));
        foreach (var component in components.Where(c => c is MonoBehaviour))
        {
            for (var type = component.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(flags | BindingFlags.DeclaredOnly))
                {
                    if (!typeof(Object).IsAssignableFrom(field.FieldType))
                        continue;
                    if (field.GetValue(component) is Object value && value != null && map.TryGetValue(value, out var mapped)
                        && (mapped == null || field.FieldType.IsInstanceOfType(mapped)))
                        field.SetValue(component, mapped);
                }
            }
        }
    }

    /// <summary>The source's own Template node, or a plain one naming it if it's a stock body.</summary>
    static ConfigNode TemplateFor(CelestialBody source)
    {
        var template = CreatedTemplate(source) ?? Export.TemplatePatch.FindBodyConfig(source.transform.name)?.GetNode("Template");
        if (template != null)
            return template.CreateCopy();
        if (Utility.FindBody(Injector.StockSystemPrefab.rootBody, source.transform.name) == null)
            return null;
        var node = new ConfigNode("Template");
        node.AddValue("name", source.transform.name);
        return node;
    }

    /// <summary>Why <paramref name="body"/> can't be deleted, or null.</summary>
    public static string CantDelete(CelestialBody body)
    {
        if (body.orbitDriver == null)
            return $"{body.bodyName} orbits nothing";
        if (body.isHomeWorld)
            return $"{body.bodyName} is the home world";
        if (body.orbitingBodies.Count > 0)
            return $"{body.bodyName} has bodies orbiting it; move or delete them first";
        if (body.GetComponentInChildren<KopernicusStar>(true) != null || body.scaledBody.GetComponentInChildren<KopernicusStar>(true) != null)
            return $"{body.bodyName} is a star";
        if (FlightGlobals.Vessels.Any(v => v != null && v.mainBody == body)
            || (HighLogic.CurrentGame?.flightState?.protoVessels.Any(p => p.orbitSnapShot?.ReferenceBodyIndex == body.flightGlobalsIndex) ?? false))
            return $"Vessels are orbiting or landed on {body.bodyName}";
        return null;
    }

    /// <summary>
    /// Removes <paramref name="body"/> from the game until it restarts. It stays in
    /// FlightGlobals.Bodies, hidden, since saves refer to bodies by their place in that list.
    /// </summary>
    public static void Delete(CelestialBody body)
    {
        string problem = CantDelete(body);
        if (problem != null)
            throw new InvalidOperationException(problem);

        foreach (var other in FlightGlobals.Bodies)
            other.orbitingBodies.Remove(body);
        PSystemManager.Instance.localBodies.Remove(body);
        PSystemManager.Instance.scaledBodies.Remove(body.scaledBody);
        PlanetariumCamera.fetch.targets.Remove(body.MapObject);
        if (PlanetariumCamera.fetch.target == body.MapObject)
            PlanetariumCamera.fetch.SetTarget(body.referenceBody.MapObject);
        Planetarium.Orbits.Remove(body.orbitDriver);
        // Nothing finds it as a main body any more.
        body.sphereOfInfluence = body.hillSphere = 0;
        body.scaledBody.SetActive(false);
        body.gameObject.SetActive(false);

        Created.Remove(body);
        Deleted.Add(body);
        Debug.Log($"[KittopiaTech] Deleted {body.bodyName}");
        RaiseChanged();
    }

    static readonly HashSet<CelestialBody> Deleted = [];

    /// <summary>Whether <paramref name="body"/> was deleted, and so is only kept for its index.</summary>
    public static bool IsDeleted(CelestialBody body) => Deleted.Contains(body);
}
