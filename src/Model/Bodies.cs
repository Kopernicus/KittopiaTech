using System;

namespace KittopiaTech.Model;

/// <summary>Changes to which bodies exist and what they orbit, made while the game runs.</summary>
public static class Bodies
{
    /// <summary>Raised after a body is added, removed or moves to another parent.</summary>
    public static event Action Changed;

    internal static void RaiseChanged() => Changed?.Invoke();

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
}
