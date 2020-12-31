<Query Kind="Program">
  <NuGetReference>Microsoft.CodeAnalysis.CSharp.Workspaces</NuGetReference>
  <NuGetReference>Microsoft.CodeAnalysis.Workspaces.MSBuild</NuGetReference>
  <NuGetReference>System.Linq.Async</NuGetReference>
  <Namespace>Microsoft.CodeAnalysis</Namespace>
  <Namespace>Microsoft.CodeAnalysis.CSharp</Namespace>
  <Namespace>Microsoft.CodeAnalysis.CSharp.Syntax</Namespace>
  <Namespace>Microsoft.CodeAnalysis.FindSymbols</Namespace>
  <Namespace>static Microsoft.CodeAnalysis.CSharp.SyntaxFactory</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.CodeAnalysis.Text</Namespace>
  <Namespace>Microsoft.CodeAnalysis.Formatting</Namespace>
  <Namespace>Microsoft.CodeAnalysis.Editing</Namespace>
</Query>


async Task Main()
{
	var helper = typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation).Assembly.DefinedTypes
		.Where(x => x.Name == "OverriddenOrHiddenMembersHelpers")
		.SelectMany(x => x.AsType()
			.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
			.Where(x => x.Name == "MakeOverriddenOrHiddenMembers" && x.GetParameters().FirstOrDefault()?.ParameterType.Name == "MethodSymbol"))
		.First()
		.GetParameters()
		.Select(x => x.ParameterType)
		.Dump();
		//return;
	//Type.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.OverriddenOrHiddenMembersHelpers").Dump();
	var code = @"
    public class A : B
    {
		[Override]
		public string IMethod(bool i) => true;
    }
	public class B : IHi
	{
		public string IMethod(bool i) => true;
	}
	public interface IHi
	{
		bool IMethod();
	}
	
	";
	var cu = SyntaxFactory.ParseSyntaxTree(code).GetCompilationUnitRoot();
	var mscorlib = PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location);
	var workspace = new AdhocWorkspace();

	//Create new project
	
	var project = workspace.AddProject("Sample", "C#");
	var solution = workspace.CurrentSolution.AddMetadataReference(project.Id, mscorlib);
	workspace.TryApplyChanges(solution);
	
	//var solution = workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
	//solution.AddProject(ProjectInfo.Create(ProjectId.CreateNewId, VersionStamp.Create(), "Sample", "Sample", LanguageNames.CSharp, 

	var doc = workspace.AddDocument(project.Id, @"hello\test.cs", cu.GetText());
	doc = doc.WithFilePath(doc.Name);

	workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentFilePath(doc.Id, doc.FilePath));
	solution = workspace.CurrentSolution;
	var documents = solution.Projects.SelectMany(x => x.Documents);
	
	var model = await doc.GetSemanticModelAsync();
	var root = (CompilationUnitSyntax)await doc.GetSyntaxRootAsync();
	var classSyntax = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
	var symbol = model.GetDeclaredSymbol(classSyntax);
	//model.GetDiagnostics().Where(x => x.Id == "CS0108").Dump();
	//return;
	var overridenMethods = model.GetDiagnostics()
		.Where(x => x.Id == "CS0108")
		.Select(x => root.FindNode(x.Location.SourceSpan))
		.OfType<MethodDeclarationSyntax>()
		.Where(x => x.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "Override"))
		.ToList();
Type.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.MethodSymbol, Microsoft.CodeAnalysis.CSharp").Dump();
return;
	var underlyingMethodSymbolProperty = overridenMethods
	.Select(x =>
	{
		var symbol = model.GetDeclaredSymbol(x);
		var prop = symbol.GetType().GetProperty("UnderlyingMethodSymbol", BindingFlags.NonPublic | BindingFlags.Instance);
		var internalSymbol = prop.GetValue(symbol);
		var prop2 = internalSymbol.GetType().GetProperty("OverriddenOrHiddenMembers", BindingFlags.NonPublic | BindingFlags.Instance);
		var hiddenResult = prop2.GetValue(internalSymbol);
		var prop3 = hiddenResult.GetType().GetProperty("HiddenMembers", BindingFlags.Public | BindingFlags.Instance);
		//var finalSymbol = new (
		var hiddenSymbols = (System.Collections.IEnumerable)prop3.GetValue(hiddenResult);
		hiddenSymbols.Cast<object>()
		.Dump();
		return hiddenSymbols;
	})
	.First();
	
	underlyingMethodSymbolProperty.Dump();
	
	//var directMembers = symbol.GetMembers().OfType<IMethodSymbol>().Select(x => x).Dump();
	//var baseMembers = symbol.BaseType.GetMembers().OfType<IMethodSymbol>().Select(x => x.First().Dump();
}
public static class Extensions
{
	public static bool IsPascalCase(this SyntaxToken token)
	{
		var val = token.Text;
        if (string.IsNullOrEmpty(val))
            return false;
        return Regex.IsMatch(val, "^[A-Z]");
    }

    public static string ToPascalCase(this string val)
    {
        if (string.IsNullOrEmpty(val))
            return val;
        return val[0].ToString().ToUpper() + val.Remove(0, 1);
	}
	public static ValueTask<T> ToValueTask<T>(this Task<T> task)
	{
		return new ValueTask<T>(task);
	}
	
	
}