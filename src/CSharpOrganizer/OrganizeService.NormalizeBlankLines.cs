using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static partial class OrganizeService
{
    private static CompilationUnitSyntax NormalizeBlankLines(CompilationUnitSyntax subject)
    {
        List<MemberDeclarationSyntax> members = NormalizeBlankLines(subject.Members).ToList();

        if (subject.Usings.Count > 0)
        {
            subject = subject
                .WithUsings(NormalizeBlankLines(subject.Usings))
                .ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        return subject.WithMembers(members).WithoutTrailingBlankLines();
    }

    private static BaseNamespaceDeclarationSyntax NormalizeBlankLines(
        BaseNamespaceDeclarationSyntax subject
    )
    {
        List<MemberDeclarationSyntax> members = NormalizeBlankLines(subject.Members).ToList();

        if (subject.Usings.Count > 0)
        {
            subject = subject
                .WithUsings(NormalizeBlankLines(subject.Usings))
                .ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        subject = subject.WithMembers(members);

        if (subject is NamespaceDeclarationSyntax && members.Count > 0)
        {
            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static ClassDeclarationSyntax NormalizeBlankLines(ClassDeclarationSyntax subject)
    {
        subject = subject.WithMembers(NormalizeBlankLines(subject.Members));

        if (subject.Members.Count > 0)
        {
            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static InterfaceDeclarationSyntax NormalizeBlankLines(
        InterfaceDeclarationSyntax subject
    )
    {
        subject = subject.WithMembers(NormalizeBlankLines(subject.Members));

        if (subject.Members.Count > 0)
        {
            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static EnumDeclarationSyntax NormalizeBlankLines(EnumDeclarationSyntax subject)
    {
        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static SyntaxList<MemberDeclarationSyntax> NormalizeBlankLines(
        SyntaxList<MemberDeclarationSyntax> subjectMembers
    )
    {
        if (subjectMembers.Count == 0)
            return subjectMembers;

        List<MemberDeclarationSyntax> members = subjectMembers.ToList();

        for (int i = 0; i < members.Count; i++)
        {
            members[i] = members[i] switch
            {
                BaseNamespaceDeclarationSyntax item => NormalizeBlankLines(item),
                ClassDeclarationSyntax item => NormalizeBlankLines(item),
                InterfaceDeclarationSyntax item => NormalizeBlankLines(item),
                EnumDeclarationSyntax item => NormalizeBlankLines(item),
                _ => members[i],
            };
        }

        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] is FieldDeclarationSyntax f)
            {
                members[i] = members[i].WithoutBlankLineTrivia();
            }

            if (
                members[i]
                is PropertyDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or MethodDeclarationSyntax
                    or EnumDeclarationSyntax
            )
            {
                members[i] = members[i].WithOneLeadingBlankLine().WithoutTrailingBlankLines();
            }
        }

        return new SyntaxList<MemberDeclarationSyntax>(members);
    }

    private static SyntaxList<UsingDirectiveSyntax> NormalizeBlankLines(
        SyntaxList<UsingDirectiveSyntax> subjectUsings
    )
    {
        if (subjectUsings.Count == 0)
        {
            return subjectUsings;
        }

        List<UsingDirectiveSyntax> usings = subjectUsings
            .OrderBy(u =>
                u.Name?.ToString().StartsWith("System", StringComparison.OrdinalIgnoreCase) == true
                    ? 0
                    : 1
            )
            .ThenBy(u => u.Name?.ToString() ?? string.Empty)
            .ToList();

        for (int i = 0; i < subjectUsings.Count; i++)
        {
            usings[i] = usings[i].WithoutBlankLineTrivia();
        }

        usings[0] = usings[0].WithOneLeadingBlankLine();

        return new SyntaxList<UsingDirectiveSyntax>(usings);
    }
}
