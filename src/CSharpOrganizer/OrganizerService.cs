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

        //CompilationUnitSyntax targetTree = NormalizeBlankLines(sourceRoot);

        CompilationUnitSyntax targetTree = Organize(sourceRoot);
        targetTree = NormalizeBlankLines(targetTree);

        // CompilationUnitSyntax targetTree = NormalizeBlankLines(sourceRoot);
        // targetTree = Organize(targetTree);

        //CompilationUnitSyntax targetTree = NormalizeBlankLines(sourceRoot);

        // sourceTree = CSharpSyntaxTree.ParseText(targetTree.ToFullString().Trim());
        // sourceRoot = (CompilationUnitSyntax)sourceTree.GetRoot();
        // targetTree = NormalizeBlankLines(sourceRoot);

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
        bool alterBrace = source.OpenBraceToken.TrailingTrivia.All(i =>
            i.IsKind(SyntaxKind.WhitespaceTrivia) || i.IsKind(SyntaxKind.EndOfLineTrivia)
        );
        if (alterBrace)
        {
            source = source.WithOpenBraceToken(
                source.OpenBraceToken.WithTrailingTrivia(_lineEnding)
            );
        }

        alterBrace = source.CloseBraceToken.LeadingTrivia.All(i =>
            i.IsKind(SyntaxKind.WhitespaceTrivia) || i.IsKind(SyntaxKind.EndOfLineTrivia)
        );
        if (alterBrace)
        {
            List<SyntaxTrivia> newLeadingTrivia = source.CloseBraceToken.LeadingTrivia.ToList();
            newLeadingTrivia.Reverse();
            newLeadingTrivia = newLeadingTrivia
                .TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia))
                .ToList();
            newLeadingTrivia.Reverse();
            source = source.WithCloseBraceToken(
                source.CloseBraceToken.WithLeadingTrivia(newLeadingTrivia)
            );
        }

        source = source.WithMembers(NormalizeBlankLines(source.Members));

        if (source.Members.Count > 0)
        {
            source = source.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return source.WithOneLeadingBlankLine();
    }

    private static InterfaceDeclarationSyntax NormalizeBlankLines(InterfaceDeclarationSyntax source)
    {
        source = source.WithMembers(NormalizeBlankLines(source.Members));

        if (source.Members.Count > 0)
        {
            source = source.ReplaceMember(0, m => m.WithoutLeadingBlankLines());
        }

        return source.WithOneLeadingBlankLine();
    }

    private static EnumDeclarationSyntax NormalizeBlankLines(EnumDeclarationSyntax source)
    {
        return source.WithOneLeadingBlankLine();
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
}
