using System;
using System.Collections.Generic;
using Kopernicus.ConfigParser;
using Kopernicus.Configuration;
using Kopernicus.OnDemand;

namespace KittopiaTech.Model;

/// <summary>
/// Sets the parser state that Kopernicus loaders read while parsing
/// (<c>Kopernicus:currentBody</c>, <c>Kopernicus:pqsVersion</c>), so that setters and
/// SetFromString work outside of a config load. Keys that are already set are left alone,
/// which makes nesting safe.
/// </summary>
public sealed class ParseStateScope : IDisposable
{
    readonly List<string> owned = [];

    public static ParseStateScope For(BodyContext context) => new(context);

    ParseStateScope(BodyContext context)
    {
        Set("Kopernicus:currentBody", () => context.Body);

        var pqs = context.CelestialBody.pqsController;
        if (pqs != null)
        {
            Set("Kopernicus:pqsVersion", () => pqs);
            var handler = pqs.GetComponentInChildren<PQSMod_OnDemandHandler>(true);
            Set("Kopernicus:pqsOnDemandHandler", () => handler);
        }
    }

    void Set<T>(string key, Func<T> accessor)
    {
        if (IsSet(key))
            return;
        Parser.SetState(key, accessor);
        owned.Add(key);
    }

    static bool IsSet(string key)
    {
        try
        {
            Parser.GetState<object>(key);
            return true;
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        foreach (var key in owned)
            Parser.ClearState(key);
        owned.Clear();
    }
}

/// <summary>The live Kopernicus wrapper for one celestial body, shared by every window.</summary>
public sealed class BodyContext
{
    static readonly Dictionary<CelestialBody, BodyContext> Contexts = [];

    public CelestialBody CelestialBody { get; }
    public Body Body { get; }

    BodyContext(CelestialBody body)
    {
        CelestialBody = body;
        Body = new Body(body);
    }

    public static BodyContext Get(CelestialBody body)
    {
        if (!Contexts.TryGetValue(body, out var context))
        {
            context = new BodyContext(body);
            Contexts.Add(body, context);
        }
        return context;
    }
}
