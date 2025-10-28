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

    [Fact]
    public void OrganizeCode_Simple_020_PreProcessor()
    {
        Execute(
            """
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using System.Linq;
            using System.Reflection;
            using Basic.Reference.Assemblies;

            #if !CORECLR
            using System.Text.RegularExpressions;
            #endif

            using Microsoft.ProgramSynthesis.Utils.JetBrains.Annotations;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.ProgramSynthesis.Utils;
            using Microsoft.ProgramSynthesis.Utils.Caching;
            using static System.FormattableString;
            using System.Diagnostics;
            using System.IO;



            namespace TestNamespace;
            public class TestClass
            {
                public int Id { get; set; }
                public string Name { get; set; }

                private int _id2;
                private int _id1;

                public void DoSomething() { }

                #if DEBUG
                private int _idDebug;
                private string _nameDebug;
                #endif

                public bool Method2() {}

                public bool Method1() {}

                private int _id;
                private string _name;
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using System.Linq;
            using System.Reflection;
            using Basic.Reference.Assemblies;

            #if !CORECLR
            using System.Text.RegularExpressions;
            #endif

            using Microsoft.ProgramSynthesis.Utils.JetBrains.Annotations;
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using Microsoft.ProgramSynthesis.Utils;
            using Microsoft.ProgramSynthesis.Utils.Caching;
            using static System.FormattableString;
            using System.Diagnostics;
            using System.IO;

            namespace TestNamespace;

            public class TestClass
            {
                public int Id { get; set; }
                public string Name { get; set; }

                private int _id2;
                private int _id1;

                public void DoSomething() { }

                #if DEBUG
                private int _idDebug;
                private string _nameDebug;
                #endif

                public bool Method2() {}

                public bool Method1() {}

                private int _id;
                private string _name;
            }

            """
        );
    }

    private void Execute(string inputCode, string expectedCode, Options? options = null)
    {
        bool success = false;
        IReadOnlyList<string> expectedLines = [];
        string actualContent = string.Empty;
        string actualCode = string.Empty;
        IReadOnlyList<string> actualLines = [];

        try
        {
            options ??= new();
            actualCode = OrganizeService.OrganizeCode(inputCode);
            actualContent = actualCode;

            expectedLines = expectedCode.Lines().ToReadOnlyList();
            actualLines = actualCode.Lines().ToReadOnlyList();

            // Normalize line endings for comparison - convert both to Unix-style line endings
            actualCode = actualCode.Replace("\r\n", "\n");
            expectedCode = expectedCode.Replace("\r\n", "\n");

            actualCode.Should().Be(expectedCode);
            success = true;
        }
        finally
        {
            if (debug || !success)
            {
                ITextRowBuilder table = TextTableBuilder
                    .Create()
                    .AddIdentityColumn()
                    .AddColumn(nameof(expectedCode))
                    .AddBorderColumn()
                    .AddColumn()
                    .AddColumn(nameof(actualCode))
                    .AddHeadingRow()
                    .AddBorderRow();

                int maxIndex = Math.Max(expectedLines.Count, actualLines.Count);

                for (int i = 0; i < maxIndex; i++)
                {
                    string expectedLine = i < expectedLines.Count ? expectedLines[i] : string.Empty;
                    string actualLine = i < actualLines.Count ? actualLines[i] : string.Empty;

                    string indicator = expectedLine.Equals(actualLine, StringComparison.Ordinal)
                        ? string.Empty
                        : "x";

                    table.AddDataRow(expectedLine, indicator, actualLine);
                }

                WriteLine(table.Render());

                WriteLine(
                    $"""
                    {nameof(inputCode)}:
                    {inputCode.Lines().RenderNumbered(separator: string.Empty)}
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
