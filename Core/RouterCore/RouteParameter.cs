using System;

namespace RouterCore;

internal sealed record RouteParameter(string Name, Type Type, int CaptureIndex)
{
    internal static RouteParameter Parse(string segment, int captureIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment, nameof(segment));
        ArgumentOutOfRangeException.ThrowIfNegative(captureIndex, nameof(captureIndex));

        if (!segment.StartsWith('{') || !segment.EndsWith('}'))
            throw new FormatException("Segment has invalid format.");

        string content = segment[1..^1];
        string[] parts = content.Split(':', 2);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            throw new ArgumentException($"Invalid template parameter: '{{{content}}}'.", nameof(segment));

        if (!RouteValueParser.TryResolveType(parts[1], out Type type))
            throw new ArgumentException($"Type '{parts[1]}' is not supported.", nameof(segment));

        string name = parts[0];

        return new RouteParameter(name, type, captureIndex);
    }

    internal static bool TryParse(string segment, int captureIndex, out RouteParameter? routeParameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment, nameof(segment));
        ArgumentOutOfRangeException.ThrowIfNegative(captureIndex, nameof(captureIndex));

        try
        {
            routeParameter = Parse(segment, captureIndex);
            return true;
        }
        catch
        {
            routeParameter = null;
            return false;
        }
    }
}