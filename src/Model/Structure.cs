using System;
using Kopernicus.ConfigParser.Interfaces;

namespace KittopiaTech.Model;

/// <summary>
/// Structural edits: creating and destroying loader objects. These run straight away rather
/// than being staged, since Kopernicus constructors act on the live body.
/// </summary>
public static class Structure
{
    /// <summary>Whether new values of <paramref name="type"/> need a concrete type picked first.</summary>
    public static bool NeedsTypePicker(Type type) =>
        MemberSchema.GetConfigType(type) == Kopernicus.ConfigParser.Enumerations.ConfigType.Node
        && TypeSchema.Get(type).IsAbstract;

    public static bool CanCreate(Type type)
    {
        if (type == typeof(string))
            return true;
        if (NeedsTypePicker(type))
            return TypeSchema.GetConcreteTypes(type).Length > 0;
        if (MemberSchema.GetConfigType(type) == Kopernicus.ConfigParser.Enumerations.ConfigType.Value)
            return !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
        return TypeSchema.Get(type).CanConstruct;
    }

    /// <summary>Creates a new value of exactly <paramref name="type"/> for <paramref name="context"/>'s body.</summary>
    public static object Create(BodyContext context, Type type)
    {
        if (type == typeof(string))
            return "";

        using (ParseStateScope.For(context))
        {
            if (MemberSchema.GetConfigType(type) == Kopernicus.ConfigParser.Enumerations.ConfigType.Value)
                return Activator.CreateInstance(type);
            return TypeSchema.Get(type).Construct(context.CelestialBody);
        }
    }

    /// <summary>Runs the object's KittopiaDestructor, if it has one.</summary>
    public static void Destroy(BodyContext context, object value)
    {
        if (value == null)
            return;

        var destructor = TypeSchema.Get(value.GetType()).Destructor;
        if (destructor == null)
            return;

        using (ParseStateScope.For(context))
        {
            try
            {
                destructor.Invoke(value, null);
            }
            catch (System.Reflection.TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }
        }
    }

    /// <summary>A short description of a list item or node for headers.</summary>
    public static string Describe(object value)
    {
        if (value == null)
            return "none";
        var type = value.GetType().Name;
        return value is IPatchable { name: { Length: > 0 } name } ? $"{type} \"{name}\"" : type;
    }
}
