namespace CSharpOrganizer;

public static class Organizer
{
    public static string OrganizeFile(string fileCode)
    {
        // Parse the code
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(fileCode);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        // Find all classes and enums
        List<ClassDeclarationSyntax> classDeclarations =
        [
            .. root.DescendantNodes().OfType<ClassDeclarationSyntax>(),
        ];
        List<EnumDeclarationSyntax> enumDeclarations =
        [
            .. root.DescendantNodes().OfType<EnumDeclarationSyntax>(),
        ];

        CompilationUnitSyntax newRoot = root;
        //var newClassDeclarations = classDeclarations.Select(OrganizeClass);

        // Reorganize top-level declarations: classes first (in original order), then enums at the bottom
        List<MemberDeclarationSyntax> allMembers = [.. newRoot.Members];
        List<ClassDeclarationSyntax> classes =
        [
            .. allMembers.OfType<ClassDeclarationSyntax>().Select(OrganizeClass),
        ]; // Keep original order
        List<EnumDeclarationSyntax> enums =
        [
            .. allMembers.OfType<EnumDeclarationSyntax>().OrderBy(e => e.Identifier.Text),
        ];
        List<MemberDeclarationSyntax> otherMembers =
        [
            .. allMembers
                .Except(classes.Cast<MemberDeclarationSyntax>())
                .Except(enums.Cast<MemberDeclarationSyntax>()),
        ];

        List<MemberDeclarationSyntax> reorganizedMembers =
        [
            .. classes.Cast<MemberDeclarationSyntax>(), // Classes first (original order)
            .. otherMembers, // Other members in middle
            .. enums.Cast<MemberDeclarationSyntax>(), // Enums at the bottom
        ];

        newRoot = newRoot.WithMembers(SyntaxFactory.List(reorganizedMembers));

        // Get the reorganized code
        string reorganizedCode = newRoot.ToFullString();

        return reorganizedCode;
    }

    private static ClassDeclarationSyntax OrganizeClass(ClassDeclarationSyntax declaration)
    {
        // Separate members by type
        List<FieldDeclarationSyntax> fields =
        [
            .. declaration.Members.OfType<FieldDeclarationSyntax>(),
        ];
        List<PropertyDeclarationSyntax> properties =
        [
            .. declaration.Members.OfType<PropertyDeclarationSyntax>(),
        ];
        List<ConstructorDeclarationSyntax> constructors =
        [
            .. declaration.Members.OfType<ConstructorDeclarationSyntax>(),
        ];
        List<MethodDeclarationSyntax> methods =
        [
            .. declaration
                .Members.OfType<MethodDeclarationSyntax>()
                .OrderBy(m => m.Identifier.Text),
        ];
        List<TypeDeclarationSyntax> nestedTypes =
        [
            .. declaration.Members.OfType<TypeDeclarationSyntax>(),
        ];

        // Create new class with reorganized members
        List<MemberDeclarationSyntax> newMembers =
        [
            .. fields, // Fields first
            .. properties, // Then properties
            .. constructors, // Then constructors
            .. methods, // Then methods (alphabetically sorted)
            .. nestedTypes, // Then nested types
        ];

        return declaration.WithMembers(SyntaxFactory.List(newMembers));
    }
}
