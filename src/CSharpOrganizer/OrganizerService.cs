using System.ComponentModel;
using System.Net.NetworkInformation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static class OrganizerService
{
    public static string OrganizeFile(string fileCode)
    {
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(fileCode);

        var sourceRoot = sourceTree.GetRoot();

        SyntaxNode targetTree = Organize(sourceRoot);

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

    private static SyntaxNode NormalizeBlankLines(SyntaxNode source)
    {
        return source switch
        {
            CompilationUnitSyntax item => NormalizeBlankLines(item),
            NamespaceDeclarationSyntax item => NormalizeBlankLines(item),
            ClassDeclarationSyntax item => NormalizeBlankLines(item),
            InterfaceDeclarationSyntax item => NormalizeBlankLines(item),
            _ => throw new ArgumentException(source.GetType().Name),
            //_ => source,
        };
    }

    private static CompilationUnitSyntax NormalizeBlankLines(CompilationUnitSyntax source)
    {
        if (source.Usings.Count > 0)
        {
            List<UsingDirectiveSyntax> usings = source
                .Usings.OrderBy(u =>
                    u.Name?.ToString().StartsWith("System", StringComparison.OrdinalIgnoreCase)
                    == true
                        ? 0
                        : 1
                )
                .ThenBy(u => u.Name?.ToString() ?? string.Empty)
                .ToList();

            for (int i = 0; i < source.Usings.Count; i++)
            {
                usings[i] = usings[i].WithoutLeadingBlankLines().WithoutTrailingBlankLines();
            }

            // usings[^1] = usings[^1].WithOneTrailingBlankLine();

            source = source.WithUsings(new SyntaxList<UsingDirectiveSyntax>(usings));
        }

        return source.WithMembers(NormalizeBlankLines(source.Members));
    }

    private static BaseNamespaceDeclarationSyntax NormalizeBlankLines(
        BaseNamespaceDeclarationSyntax source
    )
    {
        List<MemberDeclarationSyntax> members = NormalizeBlankLines(source.Members).ToList();

        if (source is FileScopedNamespaceDeclarationSyntax && members.Any())
        {
            members[0] = members[0].WithOneLeadingBlankLine();
        }

        return source.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
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

        // Reset blank lines
        for (int i = 0; i < targetMembers.Count; i++)
        {
            targetMembers[i] = targetMembers[i]
                .WithoutLeadingBlankLines()
                .WithoutTrailingBlankLines();
        }

        if (targetMembers.Any(m => m is FileScopedNamespaceDeclarationSyntax))
        {
            targetMembers[0] = targetMembers[0].WithOneLeadingBlankLine();
        }

        // Process children
        for (int i = 0; i < targetMembers.Count; i++)
        {
            if (targetMembers[i] is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                targetMembers[i] = NormalizeBlankLines(namespaceDeclaration);
            }

            if (targetMembers[i] is ClassDeclarationSyntax classDeclaration)
            {
                targetMembers[i] = NormalizeBlankLines(classDeclaration);
            }
        }

        MemberDeclarationSyntax? lastField = targetMembers.LastOrDefault(m =>
            m is FieldDeclarationSyntax
        );

        // Set last field trailing blank line
        int lastFieldIndex = lastField == null ? -1 : targetMembers.IndexOf(lastField);
        if (lastFieldIndex >= 0 && lastFieldIndex != targetMembers.Count - 1)
        {
            targetMembers[lastFieldIndex] = targetMembers[lastFieldIndex]
                .WithOneTrailingBlankLine();
        }

        for (int i = 0; i < targetMembers.Count - 1; i++)
        {
            if (
                targetMembers[i]
                is InterfaceDeclarationSyntax
                    or ClassDeclarationSyntax
                    or PropertyDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or MethodDeclarationSyntax
            )
            {
                targetMembers[i] = targetMembers[i].WithOneTrailingBlankLine();
            }
        }

        return new SyntaxList<MemberDeclarationSyntax>(targetMembers);
    }

    private static SyntaxNode Organize(SyntaxNode source)
    {
        return source switch
        {
            CompilationUnitSyntax root => Organize(root),
            NamespaceDeclarationSyntax i => Organize(i),
            ClassDeclarationSyntax i => Organize(i),
            InterfaceDeclarationSyntax i => Organize(i),
            _ => throw new ArgumentException(source.GetType().Name),
            //_ => source,
        };
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
            .ThenBy(m => m.ParameterList.Parameters.Count)
            .ToList();

        List<ClassDeclarationSyntax> classes = sourceMembers
            .OfType<ClassDeclarationSyntax>()
            .OrderBy(t => AccessModifierPriority(t.Modifiers))
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
        return source.WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }

    private static TSyntax WithOneTrailingBlankLine<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        return source.WithTrailingTrivia(
            SyntaxFactory.CarriageReturnLineFeed,
            SyntaxFactory.CarriageReturnLineFeed
        );
    }

    /// <summary>
    /// Creates a new node from this node without leading blank lines but preserves indentation.
    /// </summary>
    private static TSyntax WithoutLeadingBlankLines<TSyntax>(this TSyntax syntax)
        where TSyntax : SyntaxNode
    {
        SyntaxTriviaList leadingTrivia = syntax.GetLeadingTrivia();
        List<SyntaxTrivia> cleanedTrivia = [];

        // Find the last whitespace trivia that represents indentation
        // (not followed by EndOfLine)
        SyntaxTrivia? indentation = null;

        for (int i = leadingTrivia.Count - 1; i >= 0; i--)
        {
            SyntaxTrivia trivia = leadingTrivia[i];

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // If this is the last trivia or not followed by EndOfLine,
                // it's likely indentation
                if (i == leadingTrivia.Count - 1)
                {
                    indentation = trivia;
                    break;
                }
            }
            else if (!trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                // Found non-whitespace, non-EOL trivia (comments, etc.)
                // Keep everything from this point forward
                for (int j = i; j < leadingTrivia.Count; j++)
                {
                    cleanedTrivia.Add(leadingTrivia[j]);
                }
                return syntax.WithLeadingTrivia(cleanedTrivia);
            }
        }

        // If we only found whitespace and EOL, preserve the indentation
        if (indentation.HasValue)
        {
            cleanedTrivia.Add(indentation.Value);
        }

        return syntax.WithLeadingTrivia(cleanedTrivia);
    }

    /// <summary>
    /// Creates a new node from this node without trailing blank lines but preserves comments.
    /// </summary>
    private static TSyntax WithoutTrailingBlankLines<TSyntax>(this TSyntax syntax)
        where TSyntax : SyntaxNode
    {
        SyntaxTriviaList trailingTrivia = syntax.GetTrailingTrivia();
        List<SyntaxTrivia> cleanedTrivia = [];

        // Process trivia from the end backwards to remove trailing blank lines
        bool hasNonBlankContent = false;

        for (int i = trailingTrivia.Count - 1; i >= 0; i--)
        {
            SyntaxTrivia trivia = trailingTrivia[i];

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                // Only keep EndOfLine if we've found non-blank content
                if (hasNonBlankContent)
                {
                    cleanedTrivia.Insert(0, trivia);
                }
            }
            else if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // Only keep whitespace if we've found non-blank content
                if (hasNonBlankContent)
                {
                    cleanedTrivia.Insert(0, trivia);
                }
            }
            else
            {
                // Found meaningful trivia (comments, etc.)
                hasNonBlankContent = true;
                cleanedTrivia.Insert(0, trivia);
            }
        }

        return syntax
            .WithTrailingTrivia(cleanedTrivia)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }
}
