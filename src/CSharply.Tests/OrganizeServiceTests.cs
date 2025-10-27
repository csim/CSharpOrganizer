using FluentAssertions;
using Koalas.Extensions;
using Koalas.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CSharply.Tests;

public class OrganizeServiceTests(ITestOutputHelper output, bool debug = false)
{
    [Fact]
    public void OrganizeCode_Simple_001()
    {
        Execute(
            """
            // leading namespace comment
            namespace Text.Namespace1; // namespace comment

            public class Class1
            {
                public bool Region1 => true;

                public bool _region1 = true;

                private bool _test2 = true;

                private bool _test3 = true; // test3 comment

                ///<summary>
                /// test1 summary
                ///</summary>
                public bool _test1 = true;
            }
            """,
            """
            // leading namespace comment
            namespace Text.Namespace1; // namespace comment

            public class Class1
            {
                public bool _region1 = true;
                ///<summary>
                /// test1 summary
                ///</summary>
                public bool _test1 = true;
                private bool _test2 = true;
                private bool _test3 = true; // test3 comment

                public bool Region1 => true;
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_002_UsingsOrder()
    {
        Execute(
            """
            using Microsoft.Extensions.Logging;

            using System.Linq;


            using System.Collections.Generic;


            using System;



            namespace TestNamespace
            {


                public class TestClass
                {
                    public void TestMethod() { }
                }


            }



            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Microsoft.Extensions.Logging;

            namespace TestNamespace
            {
                public class TestClass
                {
                    public void TestMethod() { }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_003_FieldsAndProperties()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public string Name { get; set; }
                    private readonly int _count;
                    public int Id { get; set; }
                    private string _description;
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    private readonly int _count;
                    private string _description;

                    public int Id { get; set; }

                    public string Name { get; set; }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_004_MethodsAndConstructors()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void PublicMethod() { }
                    private void PrivateMethod() { }
                    public TestClass() { }
                    protected void ProtectedMethod() { }
                    internal void InternalMethod() { }
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public TestClass() { }

                    public void PublicMethod() { }

                    internal void InternalMethod() { }

                    protected void ProtectedMethod() { }

                    private void PrivateMethod() { }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_005_WithRegions()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    #region Private Fields
                    private int _id;
                    private string _name;
                    #endregion

                    #region Properties
                    public int Id { get; set; }
                    public string Name { get; set; }
                    #endregion

                    #region Methods
                    public void DoSomething() { }
                    #endregion
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    private int _id;
                    private string _name;

                    public int Id { get; set; }

                    public string Name { get; set; }

                    public void DoSomething() { }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_006_Interface()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public interface ITestService
                {
                    void DoSomething();
                    string Name { get; set; }
                    int Calculate(int value);
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public interface ITestService
                {
                    string Name { get; set; }

                    int Calculate(int value);

                    void DoSomething();
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_007_Enum()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public enum Status
                {
                    Active,
                    Inactive,
                    Pending
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public enum Status
                {
                    Active,
                    Inactive,
                    Pending
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_008_MixedMembers()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    public void Method1() { }
                    private int _field1;
                    public string Property1 { get; set; }
                    public TestClass() { }
                    private void Method2() { }
                    protected string _field2;
                    internal int Property2 { get; set; }
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public class TestClass
                {
                    protected string _field2;
                    private int _field1;

                    public string Property1 { get; set; }

                    internal int Property2 { get; set; }

                    public TestClass() { }

                    public void Method1() { }

                    private void Method2() { }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_009_FileScopedNamespace()
    {
        Execute(
            """
            using System;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace;

            public class TestClass
            {
                private readonly string _name;
                public string Name { get; set; }
                public void DoSomething() { }
            }
            """,
            """
            using System;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace;

            public class TestClass
            {
                private readonly string _name;

                public string Name { get; set; }

                public void DoSomething() { }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_010_MultipleClasses()
    {
        Execute(
            """
            using Microsoft.Extensions.DependencyInjection;
            using System;
            namespace TestNamespace;
            public class Class2
            {
                public void Method2() { }
            }

            public class Class1
            {
                public void Method1() { }
            }

            internal class Class3
            {
                public void Method3() { }
            }
            """,
            """
            using System;
            using Microsoft.Extensions.DependencyInjection;

            namespace TestNamespace;

            public class Class2
            {
                public void Method2() { }
            }

            public class Class1
            {
                public void Method1() { }
            }

            internal class Class3
            {
                public void Method3() { }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_011_StaticMembers()
    {
        Execute(
            """
            namespace TestNamespace
            {
                public static class StaticClass
                {
                    public static void StaticMethod() { }
                    private static int _staticField;
                    public static string StaticProperty { get; set; }
                    static StaticClass() { }
                }
            }
            """,
            """
            namespace TestNamespace
            {
                public static class StaticClass
                {
                    private static int _staticField;

                    public static string StaticProperty { get; set; }

                    static StaticClass() { }

                    public static void StaticMethod() { }
                }
            }

            """
        );
    }

    [Fact]
    public void OrganizeCode_Simple_012_AbstractClass()
    {
        Execute(
            """
            namespace TestNamespace;


            public abstract class AbstractClass
            {
                protected abstract void AbstractMethod();
                private int _field;
                public virtual void VirtualMethod() { }
                protected AbstractClass() { }
                public abstract string AbstractProperty { get; set; }
            }
            """,
            """
            namespace TestNamespace;

            public abstract class AbstractClass
            {
                private int _field;

                public abstract string AbstractProperty { get; set; }

                protected AbstractClass() { }

                public virtual void VirtualMethod() { }

                protected abstract void AbstractMethod();
            }

            """
        );
    }

    private void Execute(string inputCode, string expectedCode)
    {
        bool success = false;
        IReadOnlyList<string> expectedLines = [];
        string actualContent = string.Empty;
        string actualCode = string.Empty;
        IReadOnlyList<string> actualLines = [];

        try
        {
            actualCode = OrganizeService.OrganizeCode(inputCode);
            actualContent = actualCode;

            expectedLines = expectedCode.Lines().ToReadOnlyList();
            actualLines = actualCode.Lines().ToReadOnlyList();

            actualCode.Should().Be(expectedCode);
            success = true;
        }
        finally
        {
            if (debug || !success)
            {
                TextListBuilder actualLineList = TextListBuilder.Create();

                for (int i = 0; i < actualLines.Count; i++)
                {
                    string indicator =
                        i >= expectedLines.Count ? "x "
                        : expectedLines[i] == actualLines[i] ? "  "
                        : "x ";

                    actualLineList.AddItem(
                        id: string.Empty,
                        separator: "|",
                        indicator: indicator,
                        body: actualLines[i]
                    );
                }

                actualContent = actualLineList.Render();

                WriteLine(
                    TextTableBuilder
                        .Create()
                        .AddColumn("expected")
                        .AddBorderColumn()
                        .AddColumn("actual")
                        .AddHeadingRow()
                        .AddBorderRow()
                        .AddDataRow(expectedCode.Lines().RenderNumbered(), actualContent)
                        .Render()
                );

                WriteLine(
                    $"""
                    inputCode:
                    {inputCode.Lines().RenderNumbered()}
                    """
                );
            }
        }
    }

    private void WriteLine(string content)
    {
        output.WriteLine(content);
        output.WriteLine("---");
    }
}
