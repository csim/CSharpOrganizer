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

    private static CompilationUnitSyntax Organize(CompilationUnitSyntax subject)
    {
        subject = subject.RemoveRegions().WithMembers(OrganizeMembers(subject.Members));

        return NormalizeBlankLines(subject);
    }

    private static BaseNamespaceDeclarationSyntax Organize(BaseNamespaceDeclarationSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        return NormalizeBlankLines(subject);
    }

    private static InterfaceDeclarationSyntax Organize(InterfaceDeclarationSyntax subject)
    {
        subject = subject.WithMembers(OrganizeMembers(subject.Members)).RemoveRegions();

        return NormalizeBlankLines(subject);
    }

    private static FieldDeclarationSyntax Organize(FieldDeclarationSyntax subject)
    {
        return NormalizeBlankLines(subject);
    }

    private static PropertyDeclarationSyntax Organize(PropertyDeclarationSyntax subject)
    {
        return NormalizeBlankLines(subject);
    }

    private static ConstructorDeclarationSyntax Organize(ConstructorDeclarationSyntax subject)
    {
        return NormalizeBlankLines(subject);
    }

    private static MethodDeclarationSyntax Organize(MethodDeclarationSyntax subject)
    {
        return NormalizeBlankLines(subject);
    }

    private static ClassDeclarationSyntax Organize(ClassDeclarationSyntax subject)
    {
        subject = subject.RemoveRegions().WithMembers(OrganizeMembers(subject.Members));

        return NormalizeBlankLines(subject);
    }

    private static EnumDeclarationSyntax Organize(EnumDeclarationSyntax subject)
    {
        return NormalizeBlankLines(subject);
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
