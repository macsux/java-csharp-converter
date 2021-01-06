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

static ConstructorInfo MethodSymbolConstructor = Type.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.MethodSymbol, Microsoft.CodeAnalysis.CSharp").GetConstructors().First();
async Task Main()
{
	
		
	var code = @"
    public class A : B
    {
		[Override]
		public bool IMethod() => true;
    }
	public class Bs
	{
		public bool IMethod() => false;
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
	var docId = doc.Id;

	workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentFilePath(doc.Id, doc.FilePath));
	solution = workspace.CurrentSolution;
	
	var editors = await solution.Projects
	            .SelectMany(x => x.Documents)
	            .ToAsyncEnumerable()
				.SelectAwait(async x => await DocumentEditor.CreateAsync(x))
				.ToDictionaryAsync(x => x.OriginalDocument.FilePath);

	foreach (var editor in editors.Values)
	{
		doc = editor.OriginalDocument;
		var model = await doc.GetSemanticModelAsync();
		var root = (CompilationUnitSyntax) await doc.GetSyntaxRootAsync();
		
		var overridenMethods = model.GetDiagnostics()
			.Where(x => x.Id == "CS0108")
			.Select(x => root.FindNode(x.Location.SourceSpan))
			.OfType<MethodDeclarationSyntax>()
			.Where(x => x.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "Override"))
			.ToList();

		foreach(var overrideMethod in overridenMethods)
		{
			var symbol = model.GetDeclaredSymbol(overrideMethod);
			var underlyingMethodSymbolProp =
				symbol.GetType()
					.GetProperty("UnderlyingMethodSymbol", BindingFlags.NonPublic | BindingFlags.Instance); // Microsoft.CodeAnalysis.CSharp.Symbols.MethodSymbol
			var internalSymbol = underlyingMethodSymbolProp.GetValue(symbol);
			var overriddenOrHiddenMembersProp = internalSymbol.GetType().GetProperty("OverriddenOrHiddenMembers", BindingFlags.NonPublic | BindingFlags.Instance);
			var hiddenResult = overriddenOrHiddenMembersProp.GetValue(internalSymbol);
			var hiddenMembersProp = hiddenResult.GetType().GetProperty("HiddenMembers", BindingFlags.Public | BindingFlags.Instance);
			var hiddenSymbols = (System.Collections.IEnumerable) hiddenMembersProp.GetValue(hiddenResult);
			var overridenMethod = hiddenSymbols
				.Cast<object>()
				.Select(x => MethodSymbolConstructor.Invoke(new[] {x}))
				.Cast<IMethodSymbol>()
				.Where(x => x.ContainingType.TypeKind == TypeKind.Class)
				.SelectMany(x => x.DeclaringSyntaxReferences)
				.Select(x => x.SyntaxTree.GetRoot().FindNode(x.Span))
				.Cast<MethodDeclarationSyntax>()
				.FirstOrDefault();
			// the override attribute actually overrides a method on base class (not implemented interface). requires "override" keyword and abstract/virtual on base class
			if (overridenMethod == null) continue;
			var baseClassEditor = editors[overrideMethod.SyntaxTree.FilePath];
			editor.ReplaceNode(overrideMethod, (existing, _) => existing
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
				.WithAttributeLists(SyntaxFactory.List(existing.AttributeLists.Where(x => x.Attributes.All(a => a.Name.ToString() != "Override")))));
			baseClassEditor.ReplaceNode(overridenMethod,
				(existing, _) => existing.Body != null || existing.ExpressionBody != null
					? existing.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
					: existing.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword)));
		}
	}

	solution = editors.Values.Aggregate(solution, (sol, editor) => sol.WithDocumentSyntaxRoot(editor.OriginalDocument.Id, editor.GetChangedRoot()));
	solution.GetDocument(docId).GetSyntaxRootAsync().Result.ToString().Dump();
}
public record DocumentFix(SyntaxNode From, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> To)
{
	public static DocumentFix Replace<T>(T from, Func<T, SyntaxGenerator, T> to) where T : SyntaxNode
	{
		return new DocumentFix(from, (existing, gen) => to((T)existing, gen));
	}
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
	public static void ReplaceNode<T>(this DocumentEditor editor, T from, Func<T, SyntaxGenerator, T> to) where T : SyntaxNode
	{
		editor.ReplaceNode(from, (existing, gen) => to((T)existing, gen));
	}

}