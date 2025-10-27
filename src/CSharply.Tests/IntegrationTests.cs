// using System.IO;
// using Xunit;

// namespace CSharply.Tests;

// public class IntegrationTests
// {
//     [Fact]
//     public void OrganizeService_WithRealFile_ProcessesSuccessfully()
//     {
//         // Arrange
//         string testCode =
//             @"using Microsoft.Extensions.Logging;
// using System.Collections.Generic;
// using System;

// namespace CSharply.Tests.TestData
// {
//     #region Test Class
//     public class TestClass
//     {
//         #region Properties
//         public string Name { get; set; }


//         public int Age { get; set; }
//         #endregion

//         #region Methods
//         public void DoSomething()
//         {
//             Console.WriteLine(""Hello"");
//         }


//         public void DoSomethingElse()
//         {
//             Console.WriteLine(""World"");
//         }
//         #endregion
//     }
//     #endregion
// }";

//         // Act
//         string result = OrganizeService.OrganizeCode(testCode);

//         // Assert
//         Assert.NotNull(result);

//         // Verify regions are removed
//         Assert.DoesNotContain("#region", result);
//         Assert.DoesNotContain("#endregion", result);

//         // Verify using statements are ordered (System should come first)
//         string[] lines = result.Split('\n');
//         int systemUsingIndex = Array.FindIndex(
//             lines,
//             line => line.Trim().StartsWith("using System;")
//         );
//         int extensionsUsingIndex = Array.FindIndex(
//             lines,
//             line => line.Trim().StartsWith("using Microsoft.Extensions")
//         );

//         Assert.True(systemUsingIndex >= 0, "Should find System using");
//         Assert.True(extensionsUsingIndex >= 0, "Should find Microsoft.Extensions using");
//         Assert.True(
//             systemUsingIndex < extensionsUsingIndex,
//             "System usings should come before other usings"
//         );

//         // Verify class structure is maintained
//         Assert.Contains("public class TestClass", result);a
//         Assert.Contains("public string Name { get; set; }", result);
//         Assert.Contains("public int Age { get; set; }", result);
//         Assert.Contains("public void DoSomething()", result);
//         Assert.Contains("public void DoSomethingElse()", result);

//         // Verify methods contain their code
//         Assert.Contains("Console.WriteLine(\"Hello\")", result);
//         Assert.Contains("Console.WriteLine(\"World\")", result);
//     }

//     [Fact]
//     public void OrganizeService_Process_WithDebugMode_DoesNotWriteFile()
//     {
//         // Arrange
//         Options debugOptions = new(Verbose: false, Debug: true);
//         OrganizeService service = new(debugOptions);

//         string tempFile = Path.GetTempFileName();
//         string csFile = Path.ChangeExtension(tempFile, ".cs");

//         try
//         {
//             File.WriteAllText(csFile, @"using System; namespace Test { public class Test { } }");
//             string originalContent = File.ReadAllText(csFile);

//             // Act
//             OrganizeResult result = service.Process(csFile);

//             // Assert
//             Assert.NotNull(result);
//             Assert.True(result.SuccessCount > 0);

//             // File content should remain unchanged in debug mode
//             string currentContent = File.ReadAllText(csFile);
//             Assert.Equal(originalContent, currentContent);
//         }
//         finally
//         {
//             if (File.Exists(csFile))
//                 File.Delete(csFile);
//             if (File.Exists(tempFile))
//                 File.Delete(tempFile);
//         }
//     }

//     [Fact]
//     public void OrganizeService_Process_WithNonCsFile_IgnoresFile()
//     {
//         // Arrange
//         Options options = new(Verbose: false, Debug: false);
//         OrganizeService service = new(options);

//         string tempFile = Path.GetTempFileName();
//         string txtFile = Path.ChangeExtension(tempFile, ".txt");

//         try
//         {
//             File.WriteAllText(txtFile, "This is not a C# file");

//             // Get initial counts
//             OrganizeResult initialResult = service.Process("nonexistent"); // This should not change counts
//             int initialSuccessCount = initialResult.SuccessCount;
//             int initialFailCount = initialResult.FailCount;

//             // Act
//             OrganizeResult result = service.Process(txtFile);

//             // Assert
//             Assert.NotNull(result);
//             // The counts should be the same as before since non-.cs files are ignored
//             Assert.Equal(initialSuccessCount, result.SuccessCount);
//             Assert.Equal(initialFailCount, result.FailCount);
//         }
//         finally
//         {
//             if (File.Exists(txtFile))
//                 File.Delete(txtFile);
//             if (File.Exists(tempFile))
//                 File.Delete(tempFile);
//         }
//     }

//     [Fact]
//     public void OrganizeService_Process_WithDirectory_ProcessesAllCsFiles()
//     {
//         // Arrange
//         Options debugOptions = new(Verbose: false, Debug: true); // Use debug to avoid file changes
//         OrganizeService service = new(debugOptions);

//         string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
//         Directory.CreateDirectory(tempDir);

//         try
//         {
//             // Create test files
//             string csFile1 = Path.Combine(tempDir, "Test1.cs");
//             string csFile2 = Path.Combine(tempDir, "Test2.cs");
//             string txtFile = Path.Combine(tempDir, "Test.txt");

//             File.WriteAllText(csFile1, "using System; namespace Test1 { public class Test1 { } }");
//             File.WriteAllText(csFile2, "using System; namespace Test2 { public class Test2 { } }");
//             File.WriteAllText(txtFile, "Not a C# file");

//             // Get initial counts
//             OrganizeResult initialResult = service.Process("nonexistent"); // This should not change counts
//             int initialSuccessCount = initialResult.SuccessCount;

//             // Act
//             OrganizeResult result = service.Process(tempDir);

//             // Assert
//             Assert.NotNull(result);
//             Assert.True(
//                 result.SuccessCount >= initialSuccessCount + 2,
//                 $"Should process at least 2 .cs files. Expected at least {initialSuccessCount + 2}, got {result.SuccessCount}"
//             );
//             Assert.True(result.Duration > TimeSpan.Zero);
//         }
//         finally
//         {
//             if (Directory.Exists(tempDir))
//                 Directory.Delete(tempDir, true);
//         }
//     }
// }
