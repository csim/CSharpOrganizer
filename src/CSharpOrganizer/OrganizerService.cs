using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static class OrganizerService
{
    private static readonly SyntaxTrivia _lineEnding = SyntaxFactory.CarriageReturnLineFeed;

    public static string OrganizeFile(string code)
    {
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax sourceRoot = (CompilationUnitSyntax)sourceTree.GetRoot();
        CompilationUnitSyntax targetTree = Organize(sourceRoot);
        targetTree = NormalizeBlankLines(targetTree);

        return targetTree.ToFullString().Trim();
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

    private static SyntaxList<UsingDirectiveSyntax> NormalizeBlankLines(
        SyntaxList<UsingDirectiveSyntax> sourceUsings
    )
    {
        List<UsingDirectiveSyntax> usings = sourceUsings
            .OrderBy(u =>
                u.Name?.ToString().StartsWith("System", StringComparison.OrdinalIgnoreCase) == true
                    ? 0
                    : 1
            )
            .ThenBy(u => u.Name?.ToString() ?? string.Empty)
            .ToList();

        for (int i = 0; i < sourceUsings.Count; i++)
        {
            usings[i] = usings[i].WithoutBlankLineTrivia();
        }

        return new SyntaxList<UsingDirectiveSyntax>(usings);
    }

    private static CompilationUnitSyntax NormalizeBlankLines(CompilationUnitSyntax source)
    {
        if (source.Usings.Count > 0)
        {
            source = source.WithUsings(NormalizeBlankLines(source.Usings));
        }

        return source.WithMembers(NormalizeBlankLines(source.Members));
    }

    private static BaseNamespaceDeclarationSyntax NormalizeBlankLines(
        BaseNamespaceDeclarationSyntax source
    )
    {
        List<MemberDeclarationSyntax> newMembers = NormalizeBlankLines(source.Members).ToList();

        if (source.Usings.Count > 0)
        {
            List<UsingDirectiveSyntax> newUsings = NormalizeBlankLines(source.Usings).ToList();
            newUsings[0] = newUsings[0].WithOneLeadingBlankLine();
            source = source.WithUsings(new SyntaxList<UsingDirectiveSyntax>(newUsings));

            if (newMembers.Count > 0)
            {
                newMembers[0] = newMembers[0].WithOneLeadingBlankLine();
            }
        }

        return source.WithMembers(new SyntaxList<MemberDeclarationSyntax>(newMembers));
    }

    private static ClassDeclarationSyntax NormalizeBlankLines(ClassDeclarationSyntax source)
    {
        return source.WithMembers(NormalizeBlankLines(source.Members));
    }

    private static InterfaceDeclarationSyntax NormalizeBlankLines(InterfaceDeclarationSyntax source)
    {
        return source.WithMembers(NormalizeBlankLines(source.Members));
    }

    private static SyntaxList<MemberDeclarationSyntax> NormalizeBlankLines(
        SyntaxList<MemberDeclarationSyntax> sourceMembers
    )
    {
        if (sourceMembers.Count == 0)
            return sourceMembers;

        List<MemberDeclarationSyntax> targetMembers = sourceMembers.ToList();

        // Process children
        for (int i = 0; i < targetMembers.Count; i++)
        {
            targetMembers[i] = targetMembers[i] switch
            {
                BaseNamespaceDeclarationSyntax item => NormalizeBlankLines(item),
                ClassDeclarationSyntax item => NormalizeBlankLines(item),
                InterfaceDeclarationSyntax item => NormalizeBlankLines(item),
                _ => targetMembers[i],
            };
        }

        for (int i = 0; i < targetMembers.Count; i++)
        {
            if (targetMembers[i] is FieldDeclarationSyntax)
            {
                targetMembers[i] = targetMembers[i].WithoutBlankLineTrivia();
            }

            if (
                targetMembers[i]
                is BaseNamespaceDeclarationSyntax
                    or ClassDeclarationSyntax
                    or PropertyDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or MethodDeclarationSyntax
                    or EnumDeclarationSyntax
            )
            {
                if (i == 0)
                {
                    targetMembers[i] = targetMembers[i]
                        .WithoutLeadingBlankLines()
                        .WithoutTrailingBlankLines();
                }
                else
                {
                    targetMembers[i] = targetMembers[i]
                        .WithOneLeadingBlankLine()
                        .WithoutTrailingBlankLines();
                }
            }
        }

        return new SyntaxList<MemberDeclarationSyntax>(targetMembers);
    }

    private static CompilationUnitSyntax Organize(CompilationUnitSyntax node)
    {
        return node.WithMembers(OrganizeMembers(node.Members));
    }

    private static BaseNamespaceDeclarationSyntax Organize(BaseNamespaceDeclarationSyntax node)
    {
        return node.WithMembers(OrganizeMembers(node.Members));
    }

    private static ClassDeclarationSyntax Organize(ClassDeclarationSyntax node)
    {
        return node.WithMembers(OrganizeMembers(node.Members));
    }

    private static InterfaceDeclarationSyntax Organize(InterfaceDeclarationSyntax node)
    {
        return node.WithMembers(OrganizeMembers(node.Members));
    }

    private static SyntaxList<MemberDeclarationSyntax> OrganizeMembers(
        SyntaxList<MemberDeclarationSyntax> sourceMembers
    )
    {
        List<BaseNamespaceDeclarationSyntax> namespaces = sourceMembers
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<InterfaceDeclarationSyntax> interfaces = sourceMembers
            .OfType<InterfaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        // Separate members by type
        List<FieldDeclarationSyntax> fields = sourceMembers
            .OfType<FieldDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(f => f.Declaration.Variables.First().Identifier.Text)
            .ToList();

        List<PropertyDeclarationSyntax> properties = sourceMembers
            .OfType<PropertyDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(p => p.Identifier.Text)
            .ToList();

        List<ConstructorDeclarationSyntax> constructors = sourceMembers
            .OfType<ConstructorDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(c => c.ParameterList.Parameters.Count)
            .ToList();

        List<MethodDeclarationSyntax> methods = sourceMembers
            .OfType<MethodDeclarationSyntax>()
            .OrderBy(m => AccessModifierPriority(m.Modifiers))
            .ThenBy(m => m.Identifier.Text)
            .ThenBy(m => m.TypeParameterList?.Parameters.Count ?? 0)
            .ThenBy(m => m.ParameterList.Parameters.Count)
            .ToList();

        List<ClassDeclarationSyntax> classes = sourceMembers
            .OfType<ClassDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<MemberDeclarationSyntax> others = sourceMembers
            .Where(i =>
                i
                    is not (
                        BaseNamespaceDeclarationSyntax
                        or InterfaceDeclarationSyntax
                        or FieldDeclarationSyntax
                        or PropertyDeclarationSyntax
                        or ConstructorDeclarationSyntax
                        or MethodDeclarationSyntax
                        or EnumDeclarationSyntax
                        or ClassDeclarationSyntax
                    )
            )
            .ToList();

        List<EnumDeclarationSyntax> enums = sourceMembers
            .OfType<EnumDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ToList();

        // Create new class with reorganized members
        return new SyntaxList<MemberDeclarationSyntax>(
            [
                .. namespaces,
                .. interfaces,
                .. fields,
                .. properties,
                .. constructors,
                .. methods,
                .. classes,
                .. others,
                .. enums,
            ]
        );
    }

    private static TSyntax WithOneLeadingBlankLine<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        List<SyntaxTrivia> leadingTrivia = source
            .WithoutLeadingBlankLines()
            .GetLeadingTrivia()
            .ToList();
        leadingTrivia.Insert(0, _lineEnding);

        return source.WithLeadingTrivia(new SyntaxTriviaList(leadingTrivia));
    }

    private static TSyntax WithOneTrailingBlankLine<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        List<SyntaxTrivia> trailingTrivia = source
            .WithoutTrailingBlankLines()
            .GetTrailingTrivia()
            .ToList();
        trailingTrivia.Add(_lineEnding);

        return source.WithTrailingTrivia(new SyntaxTriviaList(trailingTrivia));
    }

    private static TSyntax WithoutBlankLineTrivia<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        return source.WithoutLeadingBlankLines().WithoutTrailingBlankLines();
    }

    private static TSyntax WithoutLeadingBlankLines<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        SyntaxTriviaList trailingTrivia = source.GetLeadingTrivia();
        List<SyntaxTrivia> cleanedTrivia = [];

        bool strip = true;
        for (int i = 0; i < trailingTrivia.Count; i++)
        {
            if (strip && trailingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                continue;
            }

            strip = false;
            cleanedTrivia.Add(trailingTrivia[i]);
        }

        return source.WithLeadingTrivia(new SyntaxTriviaList(cleanedTrivia));
    }

    private static TSyntax WithoutTrailingBlankLines<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        SyntaxTriviaList trailingTrivia = source.GetTrailingTrivia();
        List<SyntaxTrivia> cleanedTrivia = [];

        bool strip = true;
        for (int i = trailingTrivia.Count - 1; i >= 0; i--)
        {
            if (strip && trailingTrivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                continue;
            }

            strip = false;
            cleanedTrivia.Add(trailingTrivia[i]);
        }

        cleanedTrivia.Reverse();
        cleanedTrivia = cleanedTrivia.ToList();
        cleanedTrivia.Add(_lineEnding);

        return source.WithTrailingTrivia(new SyntaxTriviaList(cleanedTrivia));
    }
}
