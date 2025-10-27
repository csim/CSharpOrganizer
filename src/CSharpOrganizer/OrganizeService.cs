using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpOrganizer;

public static partial class OrganizeService
{
    public static string OrganizeFile(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();
        root = Organize(root);

        return root.ToFullString().Trim() + "\r\n";
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

    private static CompilationUnitSyntax Organize(CompilationUnitSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
    }

    private static BaseNamespaceDeclarationSyntax Organize(BaseNamespaceDeclarationSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
    }

    private static ClassDeclarationSyntax Organize(ClassDeclarationSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
    }

    private static InterfaceDeclarationSyntax Organize(InterfaceDeclarationSyntax source)
    {
        source = source.WithMembers(OrganizeMembers(source.Members));

        return NormalizeBlankLines(source);
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
