namespace MoviesTests
{
    internal static class AccessHelpers
    {
        public static void Set(this Type type, string name, object value) => type.GetProperty(name)?.SetValue(null, value);

        public static T Get<T>(this object obj, string name) => (T)obj.GetType()?.GetProperty(name)?.GetValue(obj)!;

        public static void Set(this object obj, string name, object value) => obj.GetType()?.GetProperty(name)?.SetValue(obj, value);

        public static object Invoke(this object obj, string name, params object[] parameters) => obj.GetType()?.GetMethod(name)!.Invoke(obj, parameters)!;
    }
}
