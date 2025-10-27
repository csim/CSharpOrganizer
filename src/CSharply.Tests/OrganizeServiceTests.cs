// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Moq;
// using Xunit;

// namespace CSharply.Tests;

// public class OrganizeServiceTests
// {
//     [Fact]
//     public void OrganizeCode_WithSimpleClass_ReturnsOrganizedCode()
//     {
//         // Arrange
//         var input =
//             @"
// using System;
// using Microsoft.Extensions.Logging;
// using System.Collections.Generic;

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

//         // Act
//         var result = OrganizeService.OrganizeCode(input);

//         // Assert
//         Assert.NotNull(result);
//         Assert.NotEmpty(result);

//         // Verify that System usings come first
//         var lines = result.Split('\n');
//         var systemUsingIndex = Array.FindIndex(
//             lines,
//             line => line.Trim().StartsWith("using System;")
//         );
//         var extensionsUsingIndex = Array.FindIndex(
//             lines,
//             line => line.Trim().StartsWith("using Microsoft.Extensions")
//         );

//         Assert.True(
//             systemUsingIndex < extensionsUsingIndex,
//             "System usings should come before other usings"
//         );
//     }

//     [Fact]
//     public void OrganizeCode_WithEmptyString_ReturnsEmptyString()
//     {
//         // Arrange
//         var input = "";

//         // Act
//         var result = OrganizeService.OrganizeCode(input);

//         // Assert
//         Assert.Equal("\r\n", result); // The method adds a trailing newline
//     }

//     [Fact]
//     public void OrganizeCode_WithRegions_RemovesRegions()
//     {
//         // Arrange
//         var input =
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

//         // Act
//         var result = OrganizeService.OrganizeCode(input);

//         // Assert
//         Assert.DoesNotContain("#region", result);
//         Assert.DoesNotContain("#endregion", result);
//         Assert.Contains("public string Name { get; set; }", result);
//         Assert.Contains("public void TestMethod()", result);
//     }

//     [Theory]
//     [InlineData(true, false)]
//     [InlineData(false, true)]
//     [InlineData(true, true)]
//     [InlineData(false, false)]
//     public void Constructor_WithVariousOptions_CreatesInstance(bool verbose, bool debug)
//     {
//         // Arrange & Act
//         var options = new Options(verbose, debug);
//         var service = new OrganizeService(options);

//         // Assert
//         Assert.NotNull(service);
//     }

//     [Fact]
//     public void Process_WithValidFilePath_ReturnsResult()
//     {
//         // Arrange
//         var options = new Options(Verbose: false, Debug: true); // Use debug mode to prevent file writing
//         var service = new OrganizeService(options);

//         // Create a temporary test file
//         var tempFile = Path.GetTempFileName();
//         File.WriteAllText(
//             tempFile,
//             @"
// using System;
// namespace Test { public class Test { } }"
//         );

//         try
//         {
//             // Rename to .cs extension
//             var csFile = Path.ChangeExtension(tempFile, ".cs");
//             File.Move(tempFile, csFile);

//             // Act
//             var result = service.Process(csFile);

//             // Assert
//             Assert.NotNull(result);
//             Assert.True(result.SuccessCount > 0 || result.FailCount > 0);

//             // Clean up
//             File.Delete(csFile);
//         }
//         finally
//         {
//             if (File.Exists(tempFile))
//                 File.Delete(tempFile);
//         }
//     }
// }
