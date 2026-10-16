using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RouterCore;

public class Route : IRoute
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private readonly Delegate _handler;
    private readonly Regex _regex;
    private readonly RouteParameter[] _parameters;

    private Route(
        string template,
        int staticSegmentCount,
        Delegate handler,
        Regex regex,
        RouteParameter[] parameters)
    {
        _handler = handler;
        _regex = regex;
        _parameters = parameters;

        Template = template;
        StaticSegmentCount = staticSegmentCount;
    }

    public string Template { get; }
    public int StaticSegmentCount { get; }

    public static Route Create(string template, Delegate handler, string[] argumentNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template, nameof(template));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        ArgumentNullException.ThrowIfNull(argumentNames, nameof(argumentNames));

        RouteTemplate parsedTemplate = RouteTemplate.Parse(template);
        ParameterInfo[] delegateParameters = handler.Method.GetParameters();

        string[] resolvedNames = ResolveArgumentNames(
            parsedTemplate.Parameters,
            delegateParameters,
            argumentNames);

        ValidateDelegateArgumentCount(delegateParameters, resolvedNames);

        RouteParameter[] orderedParameters = OrderParameters(
            parsedTemplate.Parameters,
            delegateParameters,
            resolvedNames,
            argumentNames);

        var regex = new Regex(
            parsedTemplate.Pattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            RegexTimeout);

        return new Route(
            template,
            parsedTemplate.StaticSegmentCount,
            handler,
            regex,
            orderedParameters);
    }

    public Match Match(string path)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        return _regex.Match(path);
    }

    public void InvokeHandler(object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments, nameof(arguments));
        _handler.DynamicInvoke(arguments);
    }

    public object?[] BindArguments(Match match)
    {
        ArgumentNullException.ThrowIfNull(match, nameof(match));

        object?[] result = new object?[_parameters.Length];

        for (int i = 0; i < _parameters.Length; ++i)
        {
            RouteParameter parameter = _parameters[i];
            string capture = match.Groups[parameter.CaptureIndex].Value;
            result[i] = RouteValueParser.Parse(capture, parameter.Type);
        }

        return result;
    }

    private static void ValidateDelegateArgumentCount(
        ParameterInfo[] delegateParameters,
        string[] resolvedNames)
    {
        if (delegateParameters.Length != resolvedNames.Length)
        {
            throw new ArgumentException(
                "The number of delegate arguments does not match the number of template parameters.",
                nameof(delegateParameters));
        }
    }

    private static RouteParameter[] OrderParameters(
        List<RouteParameter> parameters,
        ParameterInfo[] delegateParameters,
        string[] resolvedNames,
        string[] argumentNames)
    {
        Dictionary<string, RouteParameter> parametersDict = parameters.ToDictionary(p => p.Name);
        RouteParameter[] ordered = new RouteParameter[resolvedNames.Length];

        for (int i = 0; i < resolvedNames.Length; ++i)
        {
            if (!parametersDict.TryGetValue(resolvedNames[i], out RouteParameter? parameter))
            {
                throw new ArgumentException(
                    $"Parameter '{resolvedNames[i]}' was not found in the template.",
                    nameof(argumentNames));
            }

            if (delegateParameters[i].ParameterType != parameter.Type)
            {
                throw new ArgumentException(
                    $"The type of parameter '{resolvedNames[i]}' " +
                    "in the template does not match the type of the delegate argument.",
                    nameof(delegateParameters));
            }

            ordered[i] = parameter;
        }

        return ordered;
    }

    private static string[] ResolveArgumentNames(
        List<RouteParameter> routeParameters,
        ParameterInfo[] delegateParameters,
        string[] argumentNames)
    {
        // Use explicitly provided argument names.
        if (argumentNames.Length > 0)
        {
            if (argumentNames.Length != delegateParameters.Length)
            {
                throw new ArgumentException(
                    "The number of explicitly provided argument names " +
                    "does not match the number of delegate parameters.",
                    nameof(argumentNames));
            }

            return argumentNames;
        }

        string?[] reflectedNames = [.. delegateParameters.Select(p => p.Name)];

        bool namesMatchTemplate =
            reflectedNames.Length == routeParameters.Count &&
            reflectedNames.All(name => name is not null && routeParameters.Any(p => p.Name == name));

        if (!namesMatchTemplate)
        {
            throw new ArgumentException(
                $"Delegate parameter names [{string.Join(", ", reflectedNames)}] " +
                $"do not match template parameters [{string.Join(", ", routeParameters.Select(p => p.Name))}]. " +
                "Pass explicit argument names to Map when using lambdas or local functions.",
                nameof(delegateParameters));
        }

        return reflectedNames!;
    }
}
