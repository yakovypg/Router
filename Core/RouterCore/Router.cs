using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RouterCore;

public class Router : IRouter
{
    private IRoute[] _routes = [];
    private readonly Lock _registrationLock = new();

    public void Map(string template, Delegate handler, params string[] argumentNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template, nameof(template));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        ArgumentNullException.ThrowIfNull(argumentNames, nameof(argumentNames));

        Route route = Route.Create(template, handler, argumentNames);

        lock (_registrationLock)
        {
            if (_routes.Any(existing => existing.Template == template))
                throw new InvalidOperationException($"Route '{template}' is already registered.");

            _routes = [.. _routes
                .Append(route)
                .OrderByDescending(r => r.StaticSegmentCount)];
        }
    }

    public bool Dispatch(string path)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        IRoute[] routes = Volatile.Read(ref _routes);

        foreach (IRoute route in routes)
        {
            Match match = route.Match(path);

            if (!match.Success)
                continue;

            object?[] arguments = route.BindArguments(match);
            route.InvokeHandler(arguments);

            return true;
        }

        return false;
    }
}
