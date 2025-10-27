// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Xunit;

// namespace CSharply.Tests;

// public class ExtensionsTests
// {
//     [Fact]
//     public void RemoveRegions_WithRegionDirectives_RemovesRegions()
//     {
//         // Arrange
//         var code =
//             @"
// using System;

// namespace TestNamespace
// {
//     public class TestClass
//     {
//         #region Properties
//         public string Name { get; set; }
//         #endregion

//         #region Methods
//         public void TestMethod()
//         {
//         }
//         #endregion
//     }
// }";

//         var tree = CSharpSyntaxTree.ParseText(code);
//         var root = tree.GetCompilationUnitRoot();

//         // Act
//         var result = root.RemoveRegions();

//         // Assert
//         var resultText = result.ToFullString();
//         Assert.DoesNotContain("#region", resultText);
//         Assert.DoesNotContain("#endregion", resultText);
//         Assert.Contains("public string Name { get; set; }", resultText);
//         Assert.Contains("public void TestMethod()", resultText);
//     }

//     [Fact]
//     public void RemoveRegions_WithoutRegionDirectives_ReturnsUnchanged()
//     {
//         // Arrange
//         var code =
//             @"
// using System;

// namespace TestNamespace
// {
//     public class TestClass
//     {
//         public string Name { get; set; }
//         public void TestMethod()
//         {
//         }
//     }
// }";

//         var tree = CSharpSyntaxTree.ParseText(code);
//         var root = tree.GetCompilationUnitRoot();

//         // Act
//         var result = root.RemoveRegions();

//         // Assert
//         Assert.Equal(root.ToFullString(), result.ToFullString());
//     }

//     [Fact]
//     public void WithOneLeadingBlankLine_AddsBlankLine()
//     {
//         // Arrange
//         var code = "public class TestClass { }";
//         var tree = CSharpSyntaxTree.ParseText(code);
//         var root = tree.GetCompilationUnitRoot();
//         var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

//         // Act
//         var result = classDeclaration.WithOneLeadingBlankLine();

//         // Assert
//         var leadingTrivia = result.GetLeadingTrivia();
//         Assert.True(leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)));
//     }

//     [Fact]
//     public void WithoutTrailingBlankLines_RemovesTrailingBlankLines()
//     {
//         // Arrange
//         var code =
//             @"public class TestClass { }


// ";
//         var tree = CSharpSyntaxTree.ParseText(code);
//         var root = tree.GetCompilationUnitRoot();
//         var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

//         // Act
//         var result = classDeclaration.WithoutTrailingBlankLines();

//         // Assert
//         var trailingTrivia = result.GetTrailingTrivia();
//         var endOfLineCount = trailingTrivia.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
//         Assert.True(endOfLineCount <= 1); // Should have at most one end of line
//     }

//     [Fact]
//     public void WithoutLeadingBlankLines_RemovesLeadingBlankLines()
//     {
//         // Arrange
//         var code =
//             @"

// public class TestClass { }";
//         var tree = CSharpSyntaxTree.ParseText(code);
//         var root = tree.GetCompilationUnitRoot();
//         var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

//         // Act
//         var result = classDeclaration.WithoutLeadingBlankLines();

//         // Assert
//         var leadingTrivia = result.GetLeadingTrivia();
//         var consecutiveEndOfLines = 0;
//         var maxConsecutiveEndOfLines = 0;

//         foreach (var trivia in leadingTrivia)
//         {
//             if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
//             {
//                 consecutiveEndOfLines++;
//                 maxConsecutiveEndOfLines = Math.Max(
//                     maxConsecutiveEndOfLines,
//                     consecutiveEndOfLines
//                 );
//             }
//             else
//             {
//                 consecutiveEndOfLines = 0;
//             }
//         }

//         Assert.True(maxConsecutiveEndOfLines <= 1); // Should not have multiple consecutive blank lines
//     }
// }
