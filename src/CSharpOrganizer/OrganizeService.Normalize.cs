using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static partial class OrganizeService
{
    private static CompilationUnitSyntax NormalizeBlankLines(CompilationUnitSyntax source)
    {
        List<MemberDeclarationSyntax> newMembers = NormalizeBlankLines(source.Members).ToList();

        if (source.Usings.Count > 0)
        {
            source = source.WithUsings(NormalizeBlankLines(source.Usings));
            source = source.ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        return source.WithMembers(newMembers);
    }

    private static BaseNamespaceDeclarationSyntax NormalizeBlankLines(
        BaseNamespaceDeclarationSyntax source
    )
    {
        List<MemberDeclarationSyntax> newMembers = NormalizeBlankLines(source.Members).ToList();

        if (source.Usings.Count > 0)
        {
            source = source.WithUsings(NormalizeBlankLines(source.Usings));
            source = source.ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        source = source.WithMembers(newMembers);

        if (source is NamespaceDeclarationSyntax && newMembers.Count > 0)
        {
            newMembers[0] = newMembers[0].WithoutLeadingBlankLines();
        }

        return source.WithOneLeadingBlankLine();
    }

    private static ClassDeclarationSyntax NormalizeBlankLines(ClassDeclarationSyntax source)
    {
        source = source.WithMembers(NormalizeBlankLines(source.Members));

        if (source.Members.Count > 0)
        {
            source = source.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return source.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static InterfaceDeclarationSyntax NormalizeBlankLines(InterfaceDeclarationSyntax source)
    {
        source = source.WithMembers(NormalizeBlankLines(source.Members));

        if (source.Members.Count > 0)
        {
            source = source.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return source.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static EnumDeclarationSyntax NormalizeBlankLines(EnumDeclarationSyntax source)
    {
        return source.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static SyntaxList<MemberDeclarationSyntax> NormalizeBlankLines(
        SyntaxList<MemberDeclarationSyntax> sourceMembers
    )
    {
        if (sourceMembers.Count == 0)
            return sourceMembers;

        List<MemberDeclarationSyntax> targetMembers = sourceMembers.ToList();

        for (int i = 0; i < targetMembers.Count; i++)
        {
            targetMembers[i] = targetMembers[i] switch
            {
                BaseNamespaceDeclarationSyntax item => NormalizeBlankLines(item),
                ClassDeclarationSyntax item => NormalizeBlankLines(item),
                InterfaceDeclarationSyntax item => NormalizeBlankLines(item),
                EnumDeclarationSyntax item => NormalizeBlankLines(item),
                _ => targetMembers[i],
            };
        }

        for (int i = 0; i < targetMembers.Count; i++)
        {
            if (targetMembers[i] is FieldDeclarationSyntax f)
            {
                targetMembers[i] = targetMembers[i].WithoutBlankLineTrivia();
            }

            if (
                targetMembers[i]
                is PropertyDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or MethodDeclarationSyntax
                    or EnumDeclarationSyntax
            )
            {
                targetMembers[i] = targetMembers[i]
                    .WithOneLeadingBlankLine()
                    .WithoutTrailingBlankLines();
            }
        }

        return new SyntaxList<MemberDeclarationSyntax>(targetMembers);
    }

    private static SyntaxList<UsingDirectiveSyntax> NormalizeBlankLines(
        SyntaxList<UsingDirectiveSyntax> sourceUsings
    )
    {
        if (sourceUsings.Count == 0)
        {
            return sourceUsings;
        }

        List<UsingDirectiveSyntax> newUsings = sourceUsings
            .OrderBy(u =>
                u.Name?.ToString().StartsWith("System", StringComparison.OrdinalIgnoreCase) == true
                    ? 0
                    : 1
            )
            .ThenBy(u => u.Name?.ToString() ?? string.Empty)
            .ToList();

        for (int i = 0; i < sourceUsings.Count; i++)
        {
            newUsings[i] = newUsings[i].WithoutBlankLineTrivia();
        }

        newUsings[0] = newUsings[0].WithOneLeadingBlankLine();

        return new SyntaxList<UsingDirectiveSyntax>(newUsings);
    }
}
