using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharply;

public partial class OrganizeService
{
    public static string OrganizeCode(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax root = Organize((CompilationUnitSyntax)tree.GetRoot());

        return root.WithOneTrailingBlankLine().ToFullString().Trim() + Environment.NewLine;
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

        return 3;
    }

    private static CompilationUnitSyntax Organize(CompilationUnitSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        if (subject.Usings.Count > 0)
        {
            subject = subject
                .WithUsings(Organize(subject.Usings))
                .ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        return subject.WithoutBlankLineTrivia();
    }

    private static BaseNamespaceDeclarationSyntax Organize(BaseNamespaceDeclarationSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        List<MemberDeclarationSyntax> members = subject.Members.ToList();

        if (subject.Usings.Count > 0)
        {
            subject = subject
                .WithUsings(Organize(subject.Usings))
                .ReplaceUsing(0, m => m.WithOneLeadingBlankLine());
        }

        if (subject is NamespaceDeclarationSyntax namespaceBlock && members.Count > 0)
        {
            subject = namespaceBlock.WithOpenBraceToken(
                namespaceBlock.OpenBraceToken.WithoutTrailingBlankLineTrivia()
            );

            subject = namespaceBlock.WithCloseBraceToken(
                namespaceBlock.CloseBraceToken.WithoutLeadingBlankLineTrivia()
            );

            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static SyntaxList<UsingDirectiveSyntax> Organize(
        SyntaxList<UsingDirectiveSyntax> subjectUsings
    )
    {
        if (subjectUsings.HasPreProcessorDirective())
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

    private static InterfaceDeclarationSyntax Organize(InterfaceDeclarationSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        if (subject.Members.Count > 0)
        {
            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static FieldDeclarationSyntax Organize(FieldDeclarationSyntax subject)
    {
        return subject.WithoutBlankLineTrivia();
    }

    private static PropertyDeclarationSyntax Organize(PropertyDeclarationSyntax subject)
    {
        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static ConstructorDeclarationSyntax Organize(ConstructorDeclarationSyntax subject)
    {
        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static MethodDeclarationSyntax Organize(MethodDeclarationSyntax subject)
    {
        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static ClassDeclarationSyntax Organize(ClassDeclarationSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        subject = subject.WithOpenBraceToken(
            subject.OpenBraceToken.WithoutTrailingBlankLineTrivia()
        );

        subject = subject.WithCloseBraceToken(
            subject.CloseBraceToken.WithoutLeadingBlankLineTrivia()
        );

        if (subject.Members.Count > 0)
        {
            subject = subject.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static EnumDeclarationSyntax Organize(EnumDeclarationSyntax subject)
    {
        return subject.WithOneLeadingBlankLine().WithoutTrailingBlankLines();
    }

    private static SyntaxList<MemberDeclarationSyntax> OrganizeMembers(
        SyntaxList<MemberDeclarationSyntax> subjectMembers
    )
    {
        if (subjectMembers.HasPreProcessorDirective())
        {
            return subjectMembers;
        }

        List<MemberDeclarationSyntax> members = subjectMembers.ToList();

        List<BaseNamespaceDeclarationSyntax> namespaces = members
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<InterfaceDeclarationSyntax> interfaces = members
            .OfType<InterfaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<FieldDeclarationSyntax> fields = members
            .OfType<FieldDeclarationSyntax>()
            .Select(Organize)
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(f => f.Declaration.Variables.First().Identifier.Text)
            .ToList();

        List<PropertyDeclarationSyntax> properties = members
            .OfType<PropertyDeclarationSyntax>()
            .Select(Organize)
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(p => p.Identifier.Text)
            .ToList();

        List<ConstructorDeclarationSyntax> constructors = members
            .OfType<ConstructorDeclarationSyntax>()
            .Select(Organize)
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(c => c.ParameterList.Parameters.Count)
            .ToList();

        List<MethodDeclarationSyntax> methods = members
            .OfType<MethodDeclarationSyntax>()
            .Select(Organize)
            .OrderBy(m => AccessModifierPriority(m.Modifiers))
            .ThenBy(m => m.Identifier.Text)
            .ThenBy(m => m.TypeParameterList?.Parameters.Count ?? 0)
            .ThenBy(m => m.ParameterList.Parameters.Count)
            .ToList();

        List<ClassDeclarationSyntax> classes = members
            .OfType<ClassDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<MemberDeclarationSyntax> others = members
            .Where(i =>
                i
                    is not (
                        BaseNamespaceDeclarationSyntax
                        or InterfaceDeclarationSyntax
                        or FieldDeclarationSyntax
                        or PropertyDeclarationSyntax
                        or ConstructorDeclarationSyntax
                        or MethodDeclarationSyntax
                        or ClassDeclarationSyntax
                        or EnumDeclarationSyntax
                    )
            )
            .ToList();

        List<EnumDeclarationSyntax> enums = members
            .OfType<EnumDeclarationSyntax>()
            .Select(Organize)
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ToList();

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
}
