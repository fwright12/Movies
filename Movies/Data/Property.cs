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
        public Property Parent { get; set; }
        public Type Type { get; }
        public bool AllowsMultiple { get; }

        public virtual Type FullType => Type;

        public Property(string name, Type type, IEnumerable values, bool allowsMultiple = false) : this(name, type, allowsMultiple)
        {
            Values = values;
        }

        public Property(string name, Type type, bool allowsMultiple = false)
        {
            Name = name;
            Type = type;
            AllowsMultiple = allowsMultiple;
        }

        //public static Property<T> FromEnum<T>(string name) where T : struct, Enum => new Property<T>(name, new DiscreteValueRange<T>();

        public override int GetHashCode() => Name?.GetHashCode() ?? base.GetHashCode();

        public override bool Equals(object obj) => base.Equals(obj);

        public override string ToString() => Name ?? base.ToString();
    }

    public class Property<T> : Property
    {
        public Property(string name, bool allowsMultiple = false) : base(name, typeof(T), allowsMultiple) { }
        public Property(string name, IEnumerable values, bool allowsMultiple = false) : base(name, typeof(T), values, allowsMultiple) { }
    }

    public class MultiProperty<T> : Property<T>
    {
        public override Type FullType => typeof(IEnumerable<T>);

        public MultiProperty(string name) : base(name, true) { }
        public MultiProperty(string name, IEnumerable values) : base(name, values, true) { }
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