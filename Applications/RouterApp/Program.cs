using System;
using RouterCore;

var router = new Router();

router.Map(
    "/foo/bar/{p:int}",
    (int p) => Console.WriteLine(p));

router.Map(
    "/too/bar/{a:int}/{b:int}",
    (int b, int a) => Console.WriteLine($"a={a} b={b}"));

router.Map(
    "/zoo/bar/{a:int}/{b:int}",
    (int a, int b) => Console.WriteLine($"a={a} b={b}"));

router.Map(
    "/foo/bar/{a:int}/{b:int}/{c:int}",
    (int c, int a, int b) => Console.WriteLine($"a={a} b={b} c={c}"));

_ = router.Dispatch("/foo/bar/1");
_ = router.Dispatch("/too/bar/1/2");
_ = router.Dispatch("/zoo/bar/1/2");
_ = router.Dispatch("/foo/bar/1/2/3");
