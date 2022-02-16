using System;

public static class EnumExt
{
    public static string ToFullString(this Enum @enum)
    {
        return @enum.GetType().ToString() + "." + @enum.ToString();
    }
}
