using System;
using System.Linq;
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
            var created = TypeSchema.Get(type).Construct(context.CelestialBody);
            NewObjectDefaults.Apply(created);
            return created;
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

    /// <summary>The object's name, from IPatchable or a "name" member, so list items can be told apart.</summary>
    public static string SummaryName(object value)
    {
        if (value is IPatchable { name: { Length: > 0 } patchName })
            return patchName;

        var member = TypeSchema
            .Get(value.GetType())
            .Members.FirstOrDefault(m => m.CanRead && !m.IsCollection && m.FieldName == "name");
        if (member == null)
            return null;
        try
        {
            var name = member.GetValue(value);
            return name is IWritable writable ? writable.ValueToString() : name?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
