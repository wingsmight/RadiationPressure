using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class TypeExt
{
    public static bool IsList(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }
    public static bool IsPrimitive(this Type type)
    {
        return type.IsValueType || type == typeof(string);
    }
    public static Type[] GetChildTypes(this Type type)
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && t != type && type.IsAssignableFrom(t)).ToArray();
        return types;
    }
    public static FieldInfo GetPrivateFieldRecursive(this Type type, string fieldName)
    {
        FieldInfo field = null;

        do
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.GetField);
            type = type.BaseType;

        } while (field == null && type != null);

        return field;
    }
    public static IList GetIList(this Type type)
    {
        Array values = Array.CreateInstance(type, 0);

        return GetIList(type, values);
    }
    public static IList GetIList(this Type type, object values)
    {
        Type genericListType = typeof(List<>);
        Type concreteListType = genericListType.MakeGenericType(type);

        return Activator.CreateInstance(concreteListType, new object[] { values }) as IList;
    }
}
