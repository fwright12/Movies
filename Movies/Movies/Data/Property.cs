using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Movies
{
    public abstract class Property
    {
        public string Name { get; set; }
        public IEnumerable Values { get; }
        public Property Parent { get; }
        public Type Type { get; }

        public Property(string name, Type type, IEnumerable values) : this(name, type)
        {
            Values = values;
        }

        public Property(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        //public static Property<T> FromEnum<T>(string name) where T : struct, Enum => new Property<T>(name, new DiscreteValueRange<T>();

        public override int GetHashCode() => Name?.GetHashCode() ?? base.GetHashCode();

        public override bool Equals(object obj) => base.Equals(obj);

        public override string ToString() => Name ?? base.ToString();
    }

    public class Property<T> : Property
    {
        public Property(string name) : base(name, typeof(T)) { }
        public Property(string name, IEnumerable values) : base(name, typeof(T), values) { }
    }

    public class MultiProperty<T> : Property<T>
    {
        public MultiProperty(string name) : base(name) { }
        public MultiProperty(string name, IEnumerable values) : base(name, values) { }
    }

    public class ReflectedProperty : Property
    {
        public PropertyInfo Info { get; }

        public ReflectedProperty(PropertyInfo info) : this(info.Name, info) { }
        public ReflectedProperty(string name, PropertyInfo info) : base(name, info.PropertyType)
        {
            Info = info;
        }
    }
}