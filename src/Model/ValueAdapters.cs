using System;
using System.Linq;
using System.Reflection;
using Kopernicus.ConfigParser.Interfaces;

namespace KittopiaTech.Model;

/// <summary>Converts between what a slot stores and what an editor works with.</summary>
public abstract class ValueAdapter
{
    public abstract Type ValueType { get; }

    public abstract object Unwrap(object raw);

    /// <summary>Builds a new raw value. Never mutates the current one: getters return fresh copies.</summary>
    public abstract object Wrap(object value);

    public static ValueAdapter Identity(Type type) => new IdentityAdapter(type);

    /// <summary>
    /// An adapter exposing <c>ITypeParser&lt;T&gt;.Value</c>, or null if
    /// <paramref name="declared"/> doesn't implement it or can't be created.
    /// </summary>
    public static ValueAdapter TypeParser(Type declared)
    {
        if (declared.IsAbstract || declared.GetConstructor(Type.EmptyTypes) == null)
            return null;

        var iface = declared
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeParser<>)
            );
        if (iface == null)
            return null;
        return new TypeParserAdapter(declared, iface);
    }

    /// <summary>An adapter that edits any IParsable as its config string.</summary>
    public static ValueAdapter ParsableText(Type declared)
    {
        if (!typeof(IParsable).IsAssignableFrom(declared) || declared.IsAbstract)
            return null;
        if (declared.GetConstructor(Type.EmptyTypes) == null)
            return null;
        return new ParsableTextAdapter(declared);
    }

    sealed class IdentityAdapter(Type type) : ValueAdapter
    {
        public override Type ValueType => type;

        public override object Unwrap(object raw) => raw;

        public override object Wrap(object value) => value;
    }

    sealed class TypeParserAdapter : ValueAdapter
    {
        readonly Type declared;
        readonly PropertyInfo valueProperty;

        public TypeParserAdapter(Type declared, Type iface)
        {
            this.declared = declared;
            ValueType = iface.GetGenericArguments()[0];
            valueProperty = iface.GetProperty(nameof(ITypeParser<int>.Value));
        }

        public override Type ValueType { get; }

        public override object Unwrap(object raw)
        {
            if (raw != null)
                return valueProperty.GetValue(raw, null);
            return ValueType.IsValueType ? Activator.CreateInstance(ValueType) : null;
        }

        public override object Wrap(object value)
        {
            var raw = Activator.CreateInstance(declared);
            valueProperty.SetValue(raw, value, null);
            return raw;
        }
    }

    sealed class ParsableTextAdapter(Type declared) : ValueAdapter
    {
        public override Type ValueType => typeof(string);

        public override object Unwrap(object raw) => ((IWritable)raw)?.ValueToString();

        public override object Wrap(object value)
        {
            var raw = (IParsable)Activator.CreateInstance(declared);
            raw.SetFromString((string)value);
            return raw;
        }
    }
}
