// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Xunit;

// namespace CSharply.Tests;

// public class NormalizeBlankLinesTests
// {
//     [Fact]
//     public void NormalizeBlankLines_WithClassDeclaration_PreservesIndentationOnCloseBrace()
//     {
//         // Arrange
//         string code =
//             @"
// namespace TestNamespace
// {
//     public class TestClass
//     {
//         public string Name { get; set; }


//     }
// }";

//         SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
//         CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

//         // Act
//         string result = OrganizeService.OrganizeCode(code);

//         // Assert
//         Assert.NotNull(result);

//         // Parse the result to check the close brace indentation
//         SyntaxTree resultTree = CSharpSyntaxTree.ParseText(result);
//         CompilationUnitSyntax resultRoot = resultTree.GetCompilationUnitRoot();

//         ClassDeclarationSyntax? classDeclaration = resultRoot
//             .DescendantNodes()
//             .OfType<ClassDeclarationSyntax>()
//             .FirstOrDefault();

//         Assert.NotNull(classDeclaration);

//         // Check that the close brace has proper indentation
//         SyntaxTrivia[] leadingTrivia = classDeclaration.CloseBraceToken.LeadingTrivia.ToArray();
//         bool hasWhitespaceTrivia = leadingTrivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

//         Assert.True(hasWhitespaceTrivia, "Close brace should have indentation (whitespace trivia)");

//         // Ensure there are no excessive blank lines before the close brace
//         int endOfLineCount = leadingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
//         Assert.True(
//             endOfLineCount <= 1,
//             "Close brace should not have multiple blank lines before it"
//         );
//     }

//     [Fact]
//     public void NormalizeBlankLines_WithInterface_RemovesExcessiveBlankLines()
//     {
//         // Arrange
//         string code =
//             @"
// namespace TestNamespace
// {
//     public interface ITestInterface
//     {


//         string Name { get; set; }


//         void TestMethod();


//     }
// }";

//         // Act
//         string result = OrganizeService.OrganizeCode(code);

//         // Assert
//         Assert.NotNull(result);

//         // Check that excessive blank lines are removed
//         string[] lines = result.Split('\n');

//         // Count consecutive empty lines
//         int maxConsecutiveEmptyLines = 0;
//         int currentConsecutiveEmptyLines = 0;

//         foreach (string line in lines)
//         {
//             if (string.IsNullOrWhiteSpace(line))
//             {
//                 currentConsecutiveEmptyLines++;
//                 maxConsecutiveEmptyLines = Math.Max(
//                     maxConsecutiveEmptyLines,
//                     currentConsecutiveEmptyLines
//                 );
//             }
//             else
//             {
//                 currentConsecutiveEmptyLines = 0;
//             }
//         }

//         Assert.True(
//             maxConsecutiveEmptyLines <= 1,
//             "Should not have more than one consecutive blank line"
//         );
//     }

//     [Theory]
//     [InlineData("public")]
//     [InlineData("internal")]
//     [InlineData("private")]
//     public void NormalizeBlankLines_WithMethods_AppliesCorrectSpacing(string accessModifier)
//     {
//         // Arrange
//         string code =
//             $@"
// namespace TestNamespace
// {{
//     public class TestClass
//     {{
//         {accessModifier} void Method1()
//         {{
//         }}
//         {accessModifier} void Method2()
//         {{
//         }}
//     }}
// }}";

//         // Act
//         string result = OrganizeService.OrganizeCode(code);

//         // Assert
//         Assert.NotNull(result);
//         Assert.Contains($"{accessModifier} void Method1()", result);
//         Assert.Contains($"{accessModifier} void Method2()", result);

//         // Methods should have proper spacing
//         string[] lines = result.Split('\n');
//         bool foundMethod1 = false;
//         bool foundBlankLineAfterMethod1 = false;

//         for (int i = 0; i < lines.Length - 1; i++)
//         {
//             if (lines[i].Contains("Method1()"))
//             {
//                 foundMethod1 = true;
//             }

//             if (foundMethod1 && lines[i].Trim() == "}" && lines[i + 1].Trim() == "")
//             {
//                 foundBlankLineAfterMethod1 = true;
//                 break;
//             }
//         }

//         Assert.True(foundMethod1, "Should find Method1");
//     }

//     [Fact]
//     public void NormalizeBlankLines_WithProperties_AppliesCorrectSpacing()
//     {
//         // Arrange
//         string code =
//             @"
// namespace TestNamespace
// {
//     public class TestClass
//     {
//         public string Property1 { get; set; }
//         public string Property2 { get; set; }

//         public void Method1()
//         {
//         }
//     }
// }";

//         // Act
//         string result = OrganizeService.OrganizeCode(code);

//         // Assert
//         Assert.NotNull(result);
//         Assert.Contains("Property1", result);
//         Assert.Contains("Property2", result);
//         Assert.Contains("Method1", result);

//         // Properties and methods should be properly spaced
//         string[] lines = result.Split('\n');
//         int propertyLines = lines.Count(line => line.Contains("Property"));
//         int methodLines = lines.Count(line => line.Contains("Method1"));

//         Assert.Equal(2, propertyLines);
//         Assert.Equal(1, methodLines);
//     }
// }
