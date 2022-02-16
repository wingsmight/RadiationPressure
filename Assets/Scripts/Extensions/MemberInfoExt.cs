using System;
using System.Linq;
using System.Reflection;


public static class MemberInfoExt
{
    public static bool HasAttribute(this MemberInfo member, Type attributeType)
    {
        return member.GetCustomAttributes(attributeType, true).FirstOrDefault() != null;
    }
}
