using System;

namespace RouterCore;

public interface IRouter
{
    /// <summary>
    /// Registers a route.
    /// </summary>
    /// <param name="template">Route template.</param>
    /// <param name="handler">Route handler.</param>
    /// <param name="argumentNames">
    /// Optional explicit argument names. Required when parameter names are not
    /// preserved in metadata, and their order differs from the template order.
    /// </param>
    void Map(string template, Delegate handler, params string[] argumentNames);

    /// <summary>
    /// Invokes the handler corresponding to the route.
    /// </summary>
    /// <param name="path">Route path.</param>
    /// <returns>Whether a handler was invoked.</returns>
    bool Dispatch(string path);
}