using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Converter;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converter.Visitors
{
    public class PascalCaseFixer
    {
        public async Task<Solution> Visit(Workspace workspace)
        {
        
            var solution = workspace.CurrentSolution;
            
            var documents = solution.Projects.SelectMany(x => x.Documents);

            var fixesInFiles = (await documents
                .ToAsyncEnumerable()
                .SelectAwait(async doc =>
                {
                    var cu = (CompilationUnitSyntax)await doc.GetSyntaxRootAsync();
                    
                    var camelCaseMethods = cu.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Where(x => !x.Identifier.IsPascalCase())
                        .ToList();

                    var model = await doc.GetSemanticModelAsync();

                    var usages = await camelCaseMethods
                        .ToAsyncEnumerable()
                        .SelectManyAwait(async methodDeclaration =>
                        {
                            var callers = await SymbolFinder.FindCallersAsync(model.GetDeclaredSymbol(methodDeclaration), workspace.CurrentSolution);
                            var result = callers
                                .SelectMany(x => x.Locations)
                                .Concat(new[] { methodDeclaration.Identifier.GetLocation()});
                            return result.ToAsyncEnumerable();
                        })
                        .ToListAsync();

                    return usages;

                })
                .SelectMany(x => x.ToAsyncEnumerable())
                .ToListAsync())
                .GroupBy(x => workspace.CurrentSolution.GetDocumentIdsWithFilePath(x.SourceTree.FilePath).First(), x => x);
	
            foreach(var docFixes in fixesInFiles)
            {
                var docId = docFixes.Key;
		
                // var fixer = new NameFixer(await docFixes.ToListAsync());
                // var root = await workspace.CurrentSolution.GetDocument(docId).GetSyntaxRootAsync();
                // root = fixer.Visit(root);
                
                var documentEditor = await DocumentEditor.CreateAsync(workspace.CurrentSolution.GetDocument(docId));
                foreach(var fix in docFixes)
                {
                    var originalNode = documentEditor.OriginalRoot.FindNode(fix.SourceSpan);
                    documentEditor.ReplaceNode(originalNode, (original, syntax) =>
                    {
                        if (original is IdentifierNameSyntax i)
                            return i.WithIdentifier(Identifier(i.Identifier.Text.ToPascalCase()));
                        if (original is GenericNameSyntax g)
                            return g.WithIdentifier(Identifier(g.Identifier.Text.ToPascalCase()));
                        if (original is MethodDeclarationSyntax m)
                            return m.WithIdentifier(Identifier(m.Identifier.Text.ToPascalCase()));
                        return original;
                    });
                }
		

                var root = documentEditor.GetChangedRoot();
                root = Formatter.Format(root, workspace);
                solution = solution.WithDocumentSyntaxRoot(docId, root);
            }

            return solution;
        }
        // private class NameFixer : CSharpSyntaxRewriter
        // {
        //     HashSet<TextSpan> _fixes;
        //     public NameFixer(IEnumerable<Location> fixes)
        //     {
        //         _fixes = fixes.Select(x => x.SourceSpan).ToHashSet();
        //     }
        //     public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        //     {
        //         if (_fixes.Contains(node.Identifier.Span))
        //             node = node.WithIdentifier(Identifier(node.Identifier.Text.ToPascalCase()));
        //         return base.VisitMethodDeclaration(node);
        //     }
        //     public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        //     {
        //         if(_fixes.Contains(node.Identifier.Span))
        //             node = node.WithIdentifier(Identifier(node.Identifier.Text.ToPascalCase()));
        //         return base.VisitGenericName(node);
        //     }
        //     public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        //     {
        //         if (_fixes.Contains(node.Identifier.Span))
        //             node = node.WithIdentifier(Identifier(node.Identifier.Text.ToPascalCase()));
        //         return base.VisitIdentifierName(node);
        //     }
	       //
        // }
    }

  
    
}