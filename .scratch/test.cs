using System.Reflection.Metadata;

public enum Enum1
{
    Value1,
    Value2,
}

public class Class2
{
    public void Method2() { }

    public void Method1() { }

    public bool Text1 => _test1;
    protected bool Text2 => _test;
    public string Test3 { get; set; }

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
        public SubClass1() { }

        private bool _bool1;
    }

    private bool _test2 = true;
    private bool _test1 = true;
}

public class Class1
{
    private void Private1() { }

    public void Method2() { }

    public void Method1() { }

    private bool _test1 = true;

    public Class1()
    {
        _test1 = false;
    }
}

public interface IInterface2 { }

public interface IInterface1 { }
