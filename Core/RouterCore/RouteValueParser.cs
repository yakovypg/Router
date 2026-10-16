using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;

namespace RouterCore;

internal static class RouteValueParser
{
    private static readonly FrozenDictionary<string, Type> SupportedTypes =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["string"] = typeof(string),
            ["int"] = typeof(int),
            ["float"] = typeof(float),
            ["guid"] = typeof(Guid),
            ["datetime"] = typeof(DateTime),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static bool TryResolveType(string typeName, out Type type)
    {
        return SupportedTypes.TryGetValue(typeName, out type!);
    }

    public static object Parse(string value, Type type)
    {
        if (type == typeof(string))
            return value;

        if (type == typeof(int))
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (type == typeof(float))
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

        if (type == typeof(Guid))
            return Guid.Parse(value);

        if (type == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        throw new InvalidOperationException($"Type {type} is not supported.");
    }
}