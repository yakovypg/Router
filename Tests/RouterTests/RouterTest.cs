using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RouterCore;
using Xunit;

namespace RouterTests;

public class RouterTest
{
    // ---------------------------------------------------------------------
    // Basic dispatch
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_StaticRoute_CallsHandler()
    {
        var router = new Router();
        bool called = false;

        router.Map(
            "/foo/bar/",
            () => { called = true; });

        bool matched = router.Dispatch("/foo/bar/");

        Assert.True(matched);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_UnregisteredRoute_ReturnsFalse()
    {
        var router = new Router();
        bool matched = router.Dispatch("/not/registered/");

        Assert.False(matched);
    }

    [Fact]
    public void Dispatch_TrailingSlashIsOptional()
    {
        var router = new Router();
        int calls = 0;

        router.Map(
            "/foo/bar/",
            () => { calls++; });

        Assert.True(router.Dispatch("/foo/bar/"));
        Assert.True(router.Dispatch("/foo/bar"));
        Assert.Equal(2, calls);
    }

    // ---------------------------------------------------------------------
    // Parameter types
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_IntParameter_PassesParsedValue()
    {
        var router = new Router();

        const int expected = 42;
        int received = 0;

        router.Map(
            "/items/{id:int}/",
            (int id) => received = id);

        bool matched = router.Dispatch($"/items/{expected}/");

        Assert.True(matched);
        Assert.Equal(expected, received);
    }

    [Fact]
    public void Dispatch_StringParameter_PassesSegment()
    {
        var router = new Router();

        const string expected = "alice";
        string? received = null;

        router.Map(
            "/users/{name:string}/",
            (string name) => received = name);

        bool matched = router.Dispatch($"/users/{expected}/");

        Assert.True(matched);
        Assert.Equal(expected, received);
    }

    [Fact]
    public void Dispatch_StringParameter_DoesNotMatchAcrossSlash()
    {
        var router = new Router();
        bool called = false;

        router.Map(
            "/users/{name:string}/",
            (string name) => { called = true; });

        bool matched = router.Dispatch("/users/a/b/");

        Assert.False(matched);
        Assert.False(called);
    }

    [Fact]
    public void Dispatch_FloatParameter_UsesInvariantCulture()
    {
        var router = new Router();
        float received = 0f;

        router.Map(
            "/values/{value:float}/",
            (float value) => received = value);

        Assert.True(router.Dispatch("/values/12.5/"));
        Assert.Equal(12.5f, received);

        // The locale-specific form must not be accepted.
        Assert.Throws<FormatException>(() => router.Dispatch("/values/12,5/"));
    }

    [Fact]
    public void Dispatch_GuidParameter_PassesParsedValue()
    {
        var router = new Router();

        Guid expected = Guid.NewGuid();
        Guid received = Guid.Empty;

        router.Map(
            "/items/{id:guid}/",
            (Guid id) => received = id);

        bool matched = router.Dispatch($"/items/{expected:D}/");

        Assert.True(matched);
        Assert.Equal(expected, received);
    }

    [Fact]
    public void Dispatch_DateTimeParameter_PassesParsedValue()
    {
        var router = new Router();

        DateTime expected = new(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc);
        DateTime received = default;

        router.Map(
            "/events/{date:datetime}/",
            (DateTime date) => received = date);

        bool matched = router.Dispatch($"/events/{expected:O}/");

        Assert.True(matched);
        Assert.Equal(expected, received);
        Assert.Equal(expected.Kind, received.Kind);
    }

    [Fact]
    public void Dispatch_MixedStaticAndTypedParameters_PassesParsedValues()
    {
        var router = new Router();

        const int expectedA = 123;
        Guid expectedB = Guid.NewGuid();
        const float expectedC = 12.75f;

        int receivedA = 0;
        Guid receivedB = Guid.Empty;
        float receivedC = 0f;

        router.Map(
            "/foo/{a:int}/bar/{b:guid}/tar/{c:float}/",
            (int a, Guid b, float c) =>
            {
                receivedA = a;
                receivedB = b;
                receivedC = c;
            });

        bool matched = router.Dispatch($"/foo/{expectedA}/bar/{expectedB:D}/tar/{expectedC}/");

        Assert.True(matched);
        Assert.Equal(expectedA, receivedA);
        Assert.Equal(expectedB, receivedB);
        Assert.Equal(expectedC, receivedC);
    }

    // ---------------------------------------------------------------------
    // Binding by name
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_ParametersAreBoundByName_NotByPosition()
    {
        var router = new Router();

        const int expectedA = 1;
        const int expectedB = 2;

        int receivedA = 0;
        int receivedB = 0;

        router.Map(
            "/foo/bar/{a:int}/{b:int}/",
            (int b, int a) =>
            {
                receivedA = a;
                receivedB = b;
            });

        bool matched = router.Dispatch($"/foo/bar/{expectedA}/{expectedB}/");

        Assert.True(matched);
        Assert.Equal(expectedA, receivedA);
        Assert.Equal(expectedB, receivedB);
    }

    [Fact]
    public void Dispatch_ParametersAreBoundByName_ThreeParametersShuffled()
    {
        var router = new Router();

        const int expectedA = 1;
        const int expectedB = 2;
        const int expectedC = 3;

        int receivedA = 0;
        int receivedB = 0;
        int receivedC = 0;

        router.Map(
            "/foo/bar/{a:int}/{b:int}/{c:int}/",
            (int c, int a, int b) =>
            {
                receivedA = a;
                receivedB = b;
                receivedC = c;
            });

        bool matched = router.Dispatch($"/foo/bar/{expectedA}/{expectedB}/{expectedC}/");

        Assert.True(matched);
        Assert.Equal(expectedA, receivedA);
        Assert.Equal(expectedB, receivedB);
        Assert.Equal(expectedC, receivedC);
    }

    [Fact]
    public void Dispatch_ExplicitArgumentNames_OverrideReflection()
    {
        var router = new Router();

        const int expectedA = 1;
        const int expectedB = 2;

        int receivedA = 0;
        int receivedB = 0;

        // Explicitly declare that the first delegate parameter receives 'b'
        // and the second one receives 'a'.
        router.Map(
            "/foo/bar/{a:int}/{b:int}/",
            (int first, int second) =>
            {
                receivedB = first;
                receivedA = second;
            },
            "b", "a");

        bool matched = router.Dispatch($"/foo/bar/{expectedA}/{expectedB}/");

        Assert.True(matched);
        Assert.Equal(expectedA, receivedA);
        Assert.Equal(expectedB, receivedB);
    }

    [Fact]
    public void Map_ExplicitArgumentNamesCountMismatch_ThrowsArgumentException()
    {
        var router = new Router();

        _ = Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/foo/{a:int}/{b:int}/",
                (int a, int b) => { },
                "a");
        });
    }

    [Fact]
    public void Map_ExplicitArgumentNamesNotInTemplate_ThrowsArgumentException()
    {
        var router = new Router();

        _ = Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/foo/{a:int}/",
                (int a) => { },
                "b");
        });
    }

    // ---------------------------------------------------------------------
    // Static vs dynamic preference
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_StaticRoute_IsPreferredOverDynamicRoute()
    {
        var router = new Router();

        bool calledStatic = false;
        bool calledDynamic = false;

        router.Map(
            "/items/{name:string}/",
            (string name) => { calledDynamic = true; });

        router.Map(
            "/items/special/",
            () => { calledStatic = true; });

        bool matched = router.Dispatch("/items/special/");

        Assert.True(matched);
        Assert.True(calledStatic);
        Assert.False(calledDynamic);
    }

    // ---------------------------------------------------------------------
    // Registration errors
    // ---------------------------------------------------------------------

    [Fact]
    public void Map_DuplicateTemplate_ThrowsInvalidOperationException()
    {
        var router = new Router();

        router.Map("/foo/", () => { });

        Assert.Throws<InvalidOperationException>(() =>
        {
            router.Map("/foo/", () => { });
        });
    }

    [Fact]
    public void Map_UnsupportedParameterType_ThrowsArgumentException()
    {
        var router = new Router();

        Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/items/{id:decimal}/",
                (decimal id) => { });
        });
    }

    [Fact]
    public void Map_ParameterWithoutType_ThrowsArgumentException()
    {
        var router = new Router();

        Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/items/{id}/",
                (string id) => { });
        });
    }

    [Fact]
    public void Map_ParameterWithoutName_ThrowsArgumentException()
    {
        var router = new Router();

        Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/items/{:int}/",
                (int id) => { });
        });
    }

    [Fact]
    public void Map_DuplicateParameterName_ThrowsArgumentException()
    {
        var router = new Router();

        Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/items/{id:int}/{id:int}/",
                (int a, int b) => { });
        });
    }

    [Fact]
    public void Map_UnclosedParameterBrace_IsTreatedAsStaticSegment()
    {
        var router = new Router();
        bool called = false;

        // '{id:int' has no closing '}', so it is a static segment.
        router.Map(
            "/items/{id:int/",
            () => { called = true; });

        Assert.True(router.Dispatch("/items/{id:int/"));
        Assert.True(called);
    }

    [Fact]
    public void Map_DelegateParameterNameNotInTemplate_ThrowsArgumentException()
    {
        var router = new Router();

        _ = Assert.Throws<ArgumentException>(() =>
        {
            router.Map(
                "/items/{id:int}/",
                (int a) => { });
        });
    }

    // ---------------------------------------------------------------------
    // Invalid parameter values
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_InvalidIntValue_ThrowsFormatException()
    {
        var router = new Router();

        router.Map(
            "/items/{id:int}/",
            (int id) => { });

        Assert.Throws<FormatException>(() =>
        {
            _ = router.Dispatch("/items/not-an-int/");
        });
    }

    [Fact]
    public void Dispatch_InvalidGuidValue_ThrowsFormatException()
    {
        var router = new Router();

        router.Map(
            "/items/{id:guid}/",
            (Guid id) => { });

        Assert.Throws<FormatException>(() =>
        {
            _ = router.Dispatch("/items/not-a-guid/");
        });
    }

    [Fact]
    public void Dispatch_InvalidDateTimeValue_ThrowsFormatException()
    {
        var router = new Router();

        router.Map(
            "/events/{date:datetime}/",
            (DateTime date) => { });

        Assert.Throws<FormatException>(() =>
        {
            _ = router.Dispatch("/events/not-a-date/");
        });
    }

    // ---------------------------------------------------------------------
    // Concurrency
    // ---------------------------------------------------------------------

    [Fact]
    public void Dispatch_ParallelCalls_InvokesHandlerForEveryRequest()
    {
        var router = new Router();

        const int expectedReceivedValuesCount = 1_000;
        IEnumerable<int> expected = Enumerable.Range(0, expectedReceivedValuesCount);

        var received = new ConcurrentBag<int>();

        router.Map(
            "/items/{id:int}/",
            (int id) => received.Add(id));

        _ = Parallel.For(0, expectedReceivedValuesCount, id =>
        {
            bool matched = router.Dispatch($"/items/{id}/");
            Assert.True(matched);
        });

        Assert.Equal(expectedReceivedValuesCount, received.Count);
        Assert.Equal(expected, received.OrderBy(t => t));
    }

    [Fact]
    public void Map_ParallelRegistration_DoesNotCorruptRoutes()
    {
        var router = new Router();

        const int routeCount = 100;

        _ = Parallel.For(0, routeCount, i =>
        {
            router.Map($"/route/{i}/", () => { });
        });

        for (int i = 0; i < routeCount; ++i)
        {
            Assert.True(router.Dispatch($"/route/{i}/"));
        }
    }

    [Fact]
    public void Map_RegistrationDuringDispatch_DoesNotThrow()
    {
        var router = new Router();

        router.Map("/items/{id:int}/", (int id) => { });

        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < 500; ++i)
                    _ = router.Dispatch($"/items/{i}/");
            },
            () =>
            {
                for (int i = 0; i < 50; ++i)
                    router.Map($"/extra/{i}/", () => { });
            });
    }
}
