using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RouterCore;

internal sealed record RouteTemplate(string Pattern, int StaticSegmentCount, List<RouteParameter> Parameters)
{
    internal static RouteTemplate Parse(string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template, nameof(template));

        string[] segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var regexParts = new List<string>(segments.Length);
        var parameters = new List<RouteParameter>();

        int staticSegmentCount = 0;

        foreach (string segment in segments)
        {
            if (RouteParameter.TryParse(segment, parameters.Count + 1, out RouteParameter? routeParameter) &&
                routeParameter is not null)
            {
                if (parameters.Any(p => p.Name == routeParameter.Name))
                    throw new ArgumentException($"Parameter name '{routeParameter.Name}' is duplicated in the template.");

                parameters.Add(routeParameter);
                regexParts.Add("([^/]+)");
            }
            else
            {
                staticSegmentCount++;
                regexParts.Add(Regex.Escape(segment));
            }
        }

        string pattern = "^/" + string.Join("/", regexParts) + "/?$";

        return new RouteTemplate(pattern, staticSegmentCount, parameters);
    }
}