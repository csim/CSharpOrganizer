using Microsoft.CodeAnalysis;

namespace CSharply;

public static class SyntaxExtensions
{
    private static readonly SyntaxTrivia _lineEnding = SyntaxFactory.CarriageReturnLineFeed;
    private static readonly HashSet<SyntaxKind> _preProcessorDirectives = new(
        [
            SyntaxKind.IfDirectiveTrivia,
            SyntaxKind.ElifDirectiveTrivia,
            SyntaxKind.ElseDirectiveTrivia,
            SyntaxKind.EndIfDirectiveTrivia,
            SyntaxKind.DefineDirectiveTrivia,
            SyntaxKind.UndefDirectiveTrivia,
            SyntaxKind.WarningDirectiveTrivia,
            SyntaxKind.ErrorDirectiveTrivia,
            SyntaxKind.RegionDirectiveTrivia,
            SyntaxKind.EndRegionDirectiveTrivia,
            SyntaxKind.LineDirectiveTrivia,
            SyntaxKind.LineSpanDirectiveTrivia,
            SyntaxKind.PragmaWarningDirectiveTrivia,
            SyntaxKind.PragmaChecksumDirectiveTrivia,
            SyntaxKind.ReferenceDirectiveTrivia,
            SyntaxKind.LoadDirectiveTrivia,
            SyntaxKind.NullableDirectiveTrivia,
            SyntaxKind.BadDirectiveTrivia,
        ]
    );

    public static bool HasPreProcessorDirective(this IEnumerable<UsingDirectiveSyntax> usings)
    {
        foreach (UsingDirectiveSyntax usingDirective in usings)
        {
            // Check leading trivia
            if (
                usingDirective
                    .GetLeadingTrivia()
                    .Any(trivia =>
                        trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.NullableDirectiveTrivia)
                    )
            )
            {
                return true;
            }

            // Check trailing trivia
            if (
                usingDirective
                    .GetTrailingTrivia()
                    .Any(trivia =>
                        trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.NullableDirectiveTrivia)
                    )
            )
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasPreProcessorDirective(this CompilationUnitSyntax root)
    {
        IEnumerable<SyntaxNode> descendents = root.DescendantNodes();

        foreach (SyntaxNode descendent in descendents)
        {
            // Check leading trivia
            if (
                descendent
                    .GetLeadingTrivia()
                    .Any(trivia =>
                        trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.NullableDirectiveTrivia)
                    )
            )
            {
                return true;
            }

            // Check trailing trivia
            if (
                descendent
                    .GetTrailingTrivia()
                    .Any(trivia =>
                        trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.NullableDirectiveTrivia)
                    )
            )
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasPreProcessorDirective(this IEnumerable<MemberDeclarationSyntax> members)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            // Check leading trivia
            if (
                member
                    .GetLeadingTrivia()
                    .Any(trivia => _preProcessorDirectives.Contains(trivia.Kind()))
            )
            {
                return true;
            }

            // Check trailing trivia
            if (
                member
                    .GetTrailingTrivia()
                    .Any(trivia => _preProcessorDirectives.Contains(trivia.Kind()))
            )
            {
                return true;
            }
        }

        return false;
    }

    public static T RemoveRegions<T>(this T node)
        where T : SyntaxNode
    {
        List<SyntaxTrivia> allTrivia = node.DescendantTrivia(descendIntoTrivia: false).ToList();
        List<SyntaxTrivia> triviaToRemove = [];

        for (int i = 0; i < allTrivia.Count; i++)
        {
            SyntaxTrivia trivia = allTrivia[i];

            if (
                !trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)
                && !trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
            )
            {
                continue;
            }

            // Find the start of the line (including indentation)
            int lineStart = i;
            while (lineStart > 0 && allTrivia[lineStart - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                lineStart--;
            }

            // Add all trivia from line start to the region directive
            for (int j = lineStart; j <= i; j++)
            {
                triviaToRemove.Add(allTrivia[j]);
            }

            // Also remove the following end-of-line trivia if present
            if (i + 1 < allTrivia.Count && allTrivia[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                triviaToRemove.Add(allTrivia[i + 1]);
            }
        }

        if (!triviaToRemove.Any())
            return node;

        return node.ReplaceTrivia(triviaToRemove, (originalTrivia, rewrittenTrivia) => default);
    }

    public static BaseNamespaceDeclarationSyntax ReplaceMember(
        this BaseNamespaceDeclarationSyntax subject,
        int index,
        Func<MemberDeclarationSyntax, MemberDeclarationSyntax> transform
    )
    {
        List<MemberDeclarationSyntax> members = subject.Members.ToList();
        members[index] = transform(members[index]);

        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static ClassDeclarationSyntax ReplaceMember(
        this ClassDeclarationSyntax subject,
        int index,
        Func<MemberDeclarationSyntax, MemberDeclarationSyntax> transform
    )
    {
        List<MemberDeclarationSyntax> members = subject.Members.ToList();
        members[index] = transform(members[index]);

        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static InterfaceDeclarationSyntax ReplaceMember(
        this InterfaceDeclarationSyntax subject,
        int index,
        Func<MemberDeclarationSyntax, MemberDeclarationSyntax> transform
    )
    {
        List<MemberDeclarationSyntax> members = subject.Members.ToList();
        members[index] = transform(members[index]);

        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static BaseNamespaceDeclarationSyntax ReplaceUsing(
        this BaseNamespaceDeclarationSyntax subject,
        int index,
        Func<UsingDirectiveSyntax, UsingDirectiveSyntax> transform
    )
    {
        List<UsingDirectiveSyntax> usings = subject.Usings.ToList();
        usings[index] = transform(usings[index]);

        return subject.WithUsings(new SyntaxList<UsingDirectiveSyntax>(usings));
    }

    public static CompilationUnitSyntax ReplaceUsing(
        this CompilationUnitSyntax subject,
        int index,
        Func<UsingDirectiveSyntax, UsingDirectiveSyntax> transform
    )
    {
        List<UsingDirectiveSyntax> usings = subject.Usings.ToList();
        usings[index] = transform(usings[index]);

        return subject.WithUsings(new SyntaxList<UsingDirectiveSyntax>(usings));
    }

    public static BaseNamespaceDeclarationSyntax WithMembers(
        this BaseNamespaceDeclarationSyntax subject,
        IEnumerable<MemberDeclarationSyntax> members
    )
    {
        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static ClassDeclarationSyntax WithMembers(
        this ClassDeclarationSyntax subject,
        IEnumerable<MemberDeclarationSyntax> members
    )
    {
        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static InterfaceDeclarationSyntax WithMembers(
        this InterfaceDeclarationSyntax subject,
        IEnumerable<MemberDeclarationSyntax> members
    )
    {
        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static CompilationUnitSyntax WithMembers(
        this CompilationUnitSyntax subject,
        IEnumerable<MemberDeclarationSyntax> members
    )
    {
        return subject.WithMembers(new SyntaxList<MemberDeclarationSyntax>(members));
    }

    public static TSyntax WithOneLeadingBlankLine<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        List<SyntaxTrivia> leadingTrivia = source
            .WithoutLeadingBlankLines()
            .GetLeadingTrivia()
            .ToList();
        leadingTrivia.Insert(0, _lineEnding);

        return source.WithLeadingTrivia(new SyntaxTriviaList(leadingTrivia));
    }

    public static TSyntax WithOneTrailingBlankLine<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        List<SyntaxTrivia> trailingTrivia = source
            .WithoutTrailingBlankLines()
            .GetTrailingTrivia()
            .ToList();
        trailingTrivia.Add(_lineEnding);

        return source.WithTrailingTrivia(new SyntaxTriviaList(trailingTrivia));
    }

    public static SyntaxToken WithoutBlankLineTrivia(this SyntaxToken source)
    {
        return source.WithoutLeadingBlankLines().WithoutTrailingBlankLines();
    }

    public static TSyntax WithoutBlankLineTrivia<TSyntax>(this TSyntax source)
        where TSyntax : SyntaxNode
    {
        return source.WithoutLeadingBlankLines().WithoutTrailingBlankLines();
    }

    public static SyntaxToken WithoutLeadingBlankLines(this SyntaxToken source)
    {
        SyntaxTriviaList trailingTrivia = source.LeadingTrivia;
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

    public static TSyntax WithoutLeadingBlankLines<TSyntax>(this TSyntax source)
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

    public static SyntaxToken WithoutLeadingBlankLineTrivia(this SyntaxToken node)
    {
        bool allLeadingWhitespace = node.LeadingTrivia.All(i =>
            i.IsKind(SyntaxKind.WhitespaceTrivia) || i.IsKind(SyntaxKind.EndOfLineTrivia)
        );
        if (!allLeadingWhitespace)
            return node;

        IEnumerable<SyntaxTrivia> indentationTrivia = node.LeadingTrivia.Where(i =>
            i.IsKind(SyntaxKind.WhitespaceTrivia)
        );

        return node.WithLeadingTrivia(indentationTrivia);
    }

    public static SyntaxToken WithoutTrailingBlankLines(this SyntaxToken source)
    {
        SyntaxTriviaList trailingTrivia = source.TrailingTrivia;
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

    public static TSyntax WithoutTrailingBlankLines<TSyntax>(this TSyntax source)
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

    public static SyntaxToken WithoutTrailingBlankLineTrivia(this SyntaxToken node)
    {
        bool allTrailingWhitespace = node.TrailingTrivia.All(i =>
            i.IsKind(SyntaxKind.WhitespaceTrivia) || i.IsKind(SyntaxKind.EndOfLineTrivia)
        );
        if (!allTrailingWhitespace)
            return node;

        return node.WithTrailingTrivia(_lineEnding);
    }
}
