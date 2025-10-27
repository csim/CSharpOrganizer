using System;
using System.Data;
using System.Reflection.Metadata;
using Newtonsoft;

namespace TestNamespace;

public interface IInterface2 { }

public interface IInterface1 { }

public class Class2
{
    public bool _test1 = true;
    private bool _test2 = true;
    private bool _test3 = true;

    public bool Text1 => _test1; // test comment

    public string Text2 { get; set; }

    protected bool Text3 => _test;

    public Class1()
    {
        _test1 = false;
    }

    public Class1(string text1)
    {
        _test1 = false;
    }

    /// <summary>
    /// method1
    /// </summary>
    public void Method1() { }

    // this is a test of a comment
    // this is a test of a comment
    public void Method2() { }

    public class SubClass1 
    {
    }
}

public class Class1
{
    private bool _test1 = true;

    public Class1()
    {
        _test1 = false;
    }

    public void Method1() { }

    public void Method2()
    {
        // test comment 1
        var x = 1; // test comment 2

        return x;
    }

    private void Method4() { }

    private void Private1() { }
}

public enum Enum1
{
    Value1,
    Value2,
}
