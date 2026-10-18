using System;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.Configuration;
using UnityEngine;

namespace KittopiaTech.Model;

/// <summary>
/// Visible starting values for objects created in the editor. Constructors leave most fields
/// zeroed, which for colours means transparent black and for rings means no size at all.
/// </summary>
internal static class NewObjectDefaults
{
    public static void Apply(object created)
    {
        foreach (var member in TypeSchema.Get(created.GetType()).Members)
        {
            if (member.Shape != MemberShape.Value || !member.CanRead || !member.CanWrite || member.Untouchable)
                continue;
            try
            {
                switch (member.GetValue(created))
                {
                    case ColorParser parser when parser.Value == default:
                        member.SetValue(created, new ColorParser(Color.white));
                        break;
                    case Color color when color == default:
                        member.SetValue(created, Color.white);
                        break;
                }
            }
            catch (Exception)
            {
                // Some getters throw until the object is fully set up; leave those alone.
            }
        }

        // Ring radii are in milliradii: 1.5 to 2.5 planet radii.
        if (created is RingLoader ring)
        {
            ring.InnerRadius = 1500f;
            ring.OuterRadius = 2500f;
        }
    }
}
