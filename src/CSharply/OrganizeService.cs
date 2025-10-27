using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharply;

public static partial class OrganizeService
{
    public static string OrganizeFile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();
        root = Organize(root);

        return root.WithOneTrailingBlankLine().ToFullString().Trim() + "\r\n";
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

    private static bool IsInsideRegion(SyntaxNode node)
    {
        SyntaxNode root = node.SyntaxTree.GetRoot();
        int nodePosition = node.SpanStart;

        IEnumerable<SyntaxTrivia> allTrivia = root.DescendantTrivia(descendIntoTrivia: true);

        Stack<SyntaxTrivia> regionStack = new();

        foreach (SyntaxTrivia trivia in allTrivia)
        {
            // Stop when we reach our node's position
            if (trivia.SpanStart >= nodePosition)
                break;

            if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
            {
                regionStack.Push(trivia);
            }
            else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            {
                if (regionStack.Count > 0)
                    regionStack.Pop();
            }
        }

        return regionStack.Count > 0;
    }

    private static CompilationUnitSyntax Organize(CompilationUnitSyntax source)
    {
        source = source.RemoveRegions().WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
    }

    private static BaseNamespaceDeclarationSyntax Organize(BaseNamespaceDeclarationSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members)).RemoveRegions();

        return NormalizeBlankLines(source);
    }

    private static ClassDeclarationSyntax Organize(ClassDeclarationSyntax source)
    {
        source = source.RemoveRegions().WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
    }

    private static InterfaceDeclarationSyntax Organize(InterfaceDeclarationSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members)).RemoveRegions();

        return NormalizeBlankLines(source);
    }

    private static SyntaxList<MemberDeclarationSyntax> OrganizeMembers(
        SyntaxList<MemberDeclarationSyntax> subjectMembers
    )
    {
        List<MemberDeclarationSyntax> members = subjectMembers.ToList();

        List<BaseNamespaceDeclarationSyntax> namespaces = members
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        List<InterfaceDeclarationSyntax> interfaces = members
            .OfType<InterfaceDeclarationSyntax>()
            .Select(Organize)
            .ToList();

        // Separate members by type
        List<FieldDeclarationSyntax> fields = members
            .OfType<FieldDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(f => f.Declaration.Variables.First().Identifier.Text)
            .ToList();

        List<PropertyDeclarationSyntax> properties = members
            .OfType<PropertyDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(p => p.Identifier.Text)
            .ToList();

        List<ConstructorDeclarationSyntax> constructors = members
            .OfType<ConstructorDeclarationSyntax>()
            .OrderBy(f => AccessModifierPriority(f.Modifiers))
            .ThenBy(c => c.ParameterList.Parameters.Count)
            .ToList();

        List<MethodDeclarationSyntax> methods = members
            .OfType<MethodDeclarationSyntax>()
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
                        or EnumDeclarationSyntax
                        or ClassDeclarationSyntax
                    )
            )
            .ToList();

        List<EnumDeclarationSyntax> enums = members
            .OfType<EnumDeclarationSyntax>()
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

    private static T RemoveRegions<T>(this T node)
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
}
