using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static class OrganizerService
{
    public static string OrganizeFile(string fileCode)
    {
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(fileCode);

        CompilationUnitSyntax targetTree = OrganizeTree(sourceTree);

        targetTree = NormalizeBlankLines(targetTree);

        return targetTree.ToFullString();
    }

    private static int AccessModifierPriority(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return 0;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return 1;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return 2;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
            return 3;

        return 3; //  treat as private
    }

    private static CompilationUnitSyntax NormalizeBlankLines(CompilationUnitSyntax sourceTree)
    {
        List<MemberDeclarationSyntax> targetMembers = sourceTree.Members.ToList();
        if (targetMembers.Count == 0)
        {
            return sourceTree;
        }

        for (int i = 0; i < targetMembers.Count - 1; i++)
        {
            if (targetMembers[i] is ClassDeclarationSyntax classDeclaration)
            {
                targetMembers[i] = NormalizeBlankLines(classDeclaration);
            }

            targetMembers[i] = SetTrailingBlankLine(targetMembers[i]);
        }

        return sourceTree.WithMembers(SyntaxFactory.List(targetMembers));
    }

    private static ClassDeclarationSyntax NormalizeBlankLines(ClassDeclarationSyntax sourceClass)
    {
        List<MemberDeclarationSyntax> targetMembers = sourceClass.Members.ToList();
        if (targetMembers.Count == 0)
        {
            return sourceClass;
        }

        MemberDeclarationSyntax? lastField = targetMembers.LastOrDefault(m =>
            m is FieldDeclarationSyntax
        );

        int lastFieldIndex = lastField == null ? -1 : targetMembers.IndexOf(lastField);

        // if (lastField != null)
        // {
        //     if (lastFieldIndex != targetMembers.Count - 1)
        //     {
        //         targetMembers[lastFieldIndex] = SetTrailingBlankLine(targetMembers[lastFieldIndex]);
        //     }
        // }

        for (int i = 0; i < targetMembers.Count; i++)
        {
            // if (targetMembers[i] is FieldDeclarationSyntax)
            // {
            //     targetMembers[i] = SetTrailingBlankLine(
            //         targetMembers[i],
            //         i == lastFieldIndex && lastFieldIndex != targetMembers.Count - 1 ? 1 : 0
            //     );
            // }

            if (
                targetMembers[i]
                is PropertyDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or MethodDeclarationSyntax
            )
            {
                targetMembers[i] = SetTrailingBlankLine(targetMembers[i]);
            }

            if (targetMembers[i] is ClassDeclarationSyntax classDeclaration)
            {
                targetMembers[i] = SetTrailingBlankLine(NormalizeBlankLines(classDeclaration));
            }
        }

        return sourceClass.WithMembers(SyntaxFactory.List(targetMembers));
    }

    private static ClassDeclarationSyntax OrganizeClass(ClassDeclarationSyntax sourceClass)
    {
        // Separate members by type
        List<FieldDeclarationSyntax> fields = sourceClass
            .Members.OfType<FieldDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(f => f.Declaration.Variables.First().Identifier.Text)
            .ToList();

        List<PropertyDeclarationSyntax> properties = sourceClass
            .Members.OfType<PropertyDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(p => p.Identifier.Text)
            .ToList();

        List<ConstructorDeclarationSyntax> constructors = sourceClass
            .Members.OfType<ConstructorDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(c => c.ParameterList.Parameters.Count)
            .ToList();

        List<MethodDeclarationSyntax> methods = sourceClass
            .Members.OfType<MethodDeclarationSyntax>()
            .OrderBy(m => AccessModifierPriority(m.Modifiers))
            .ThenBy(m => m.Identifier.Text)
            .ThenBy(m => m.ParameterList.Parameters.Count)
            .ToList();

        List<TypeDeclarationSyntax> nestedTypes = sourceClass
            .Members.OfType<TypeDeclarationSyntax>()
            .OrderBy(t => AccessModifierPriority(t.Modifiers))
            .ThenBy(t => t.Identifier.Text)
            .Select(t => t is ClassDeclarationSyntax nestedClass ? OrganizeClass(nestedClass) : t)
            .ToList();

        // Create new class with reorganized members
        List<MemberDeclarationSyntax> newMembers =
        [
            .. fields,
            .. properties,
            .. constructors,
            .. methods,
            .. nestedTypes,
        ];

        ClassDeclarationSyntax targetClass = sourceClass.WithMembers(
            SyntaxFactory.List(newMembers)
        );

        return targetClass;
    }

    private static CompilationUnitSyntax OrganizeTree(SyntaxTree tree)
    {
        CompilationUnitSyntax sourceRoot = tree.GetCompilationUnitRoot();

        List<MemberDeclarationSyntax> targetMembers = sourceRoot.Members.ToList();

        List<ClassDeclarationSyntax> sourceClasses = targetMembers
            .OfType<ClassDeclarationSyntax>()
            .ToList();

        List<InterfaceDeclarationSyntax> interfaces = targetMembers
            .OfType<InterfaceDeclarationSyntax>()
            .ToList();

        List<ClassDeclarationSyntax> classes = sourceClasses.Select(OrganizeClass).ToList();

        List<EnumDeclarationSyntax> enums = targetMembers
            .OfType<EnumDeclarationSyntax>()
            .OrderBy(e => e.Identifier.Text)
            .ToList();

        List<MemberDeclarationSyntax> otherMembers = targetMembers
            .Except(interfaces)
            .Except(sourceClasses)
            .Except(enums)
            .ToList();

        List<MemberDeclarationSyntax> reorganizedMembers =
        [
            .. interfaces,
            .. classes,
            .. otherMembers,
            .. enums,
        ];

        return sourceRoot.WithMembers(SyntaxFactory.List(reorganizedMembers));
    }

    private static MemberDeclarationSyntax SetTrailingBlankLine(
        MemberDeclarationSyntax source,
        int count = 1
    )
    {
        if (count == 0)
        {
            return source.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }

        return source.WithTrailingTrivia(
            SyntaxFactory.CarriageReturnLineFeed,
            SyntaxFactory.CarriageReturnLineFeed
        );
    }
}
