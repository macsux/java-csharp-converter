using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Converter.Visitors
{
    public class OverrideFixer
    {
        static ConstructorInfo MethodSymbolConstructor = Type.GetType("Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.MethodSymbol, Microsoft.CodeAnalysis.CSharp").GetConstructors().First();

        public async Task<Solution> Visit(Workspace workspace)
        {
            var solution = workspace.CurrentSolution;

            var editors = await solution.Projects
	            .SelectMany(x => x.Documents)
	            .ToAsyncEnumerable()
				.SelectAwait(async x => await DocumentEditor.CreateAsync(x))
				.ToDictionaryAsync(x => x.OriginalDocument.FilePath);

			foreach (var editor in editors.Values)
			{
				var doc = editor.OriginalDocument;
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
					editor.ReplaceNode(overrideMethod, (existing, _) => existing
						.WithAttributeLists(SyntaxFactory.List(existing.AttributeLists.Where(x => x.Attributes.All(a => a.Name.ToString() != "Override")))));
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
						.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword)));
					baseClassEditor.ReplaceNode(overridenMethod,
						(existing, _) => existing.Body != null || existing.ExpressionBody != null
							? existing.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
							: existing.AddModifiers(SyntaxFactory.Token(SyntaxKind.AbstractKeyword)));
				}
			}

			solution = editors.Values.Aggregate(solution, (sol, editor) => sol.WithDocumentSyntaxRoot(editor.OriginalDocument.Id, editor.GetChangedRoot()));
			return solution;
        }
        
    }
}