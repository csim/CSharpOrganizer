using Newtonsoft;
using System.Reflection.Metadata;
using System;
using System.Data;
namespace TestNamespace;




public enum Enum1
{
    Value1,
    Value2,
}



public class Class2
{
    // this is a test of a comment
    // this is a test of a comment
    public void Method2() { }

    /// <summary>
    /// method1
    /// </summary>
    public void Method1() { }

    public bool Text1 => _test1; // test comment
    protected bool Text3 => _test;
    public string Text2 { get; set; }

    public Class1(string text1)
    {
        _test1 = false;
    }

    public Class1()
    {
        _test1 = false;
    }

    public class SubClass1
    {

    }

    private bool _test2 = true;
    private bool _test3 = true;
    public bool _test1 = true;



}

public class Class1
{
    private void Private1() { }

    private void Method4() { }

    public void Method2()
    {
        // test comment 1
        var x = 1; // test comment 2

        return x;
    }

    public void Method1() { }

    private bool _test1 = true;

    public Class1()
    {
        _test1 = false;
    }
}

public interface IInterface2 { }

public interface IInterface1 { }
