using System.Text.RegularExpressions;

namespace RouterCore;

public interface IRoute
{
    /// <summary>
    /// Route template.
    /// </summary>
    string Template { get; }

    /// <summary>
    /// Number of static segments in the route.
    /// </summary>
    int StaticSegmentCount { get; }

    /// <summary>
    /// Matches the route.
    /// </summary>
    /// <param name="path">Route path.</param>
    /// <returns>The match result.</returns>
    Match Match(string path);

    /// <summary>
    /// Invokes the route handler.
    /// </summary>
    /// <param name="arguments">Route handler arguments.</param>
    void InvokeHandler(object?[] arguments);

    /// <summary>
    /// Binds arguments to the matched route.
    /// </summary>
    /// <param name="match">Route match.</param>
    /// <returns>Bound arguments.</returns>
    object?[] BindArguments(Match match);
}