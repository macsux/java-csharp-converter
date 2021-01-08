using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Converter.Visitors;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.MSBuild;
using static System.Console;


namespace Converter
{
    class Program
    {
        static async Task Main(string[] args)
        {

            MSBuildLocator.RegisterDefaults();
            var mylock = new object();
            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (sender, workspaceFailedArgs) => WriteLine(workspaceFailedArgs.Diagnostic.Message);
            var solution = await workspace.OpenSolutionAsync(@"C:\projects\NAxonAuto\\first\NAxonFramework.sln");
            WriteLine($"Loaded solution {solution.FilePath}");

            IEnumerable<Document> GetDocuments(Solution sol)
            {
                return sol.Projects.Skip(2)
                        .SelectMany(x => x.Documents)
                        // .Where(x => x.FilePath.Contains("MessageHandler.cs"))
                    ;
            }

            foreach (var doc in GetDocuments(solution))
            {
           
                var visitors = new List<CSharpSyntaxRewriter>
                {
                    new NamespaceFixer(),
                    new ClassRemapper(),
                    new AttributeRemover(),
                    new PascalCaseFixer2(),
                    new SpecialUseRemapper()
                };
                var cu = (CompilationUnitSyntax) await doc.GetSyntaxRootAsync();
                cu = cu.WithoutLeadingTrivia();
                foreach (var visitor in visitors)
                {
                    cu = (CompilationUnitSyntax) visitor.Visit(cu);
                }
                
                // await fixer.Initialize();
                // fixer.VisitDocument();
                cu = (CompilationUnitSyntax) Microsoft.CodeAnalysis.Formatting.Formatter.Format(cu, workspace);

                solution = solution.WithDocumentSyntaxRoot(doc.Id, cu);
               
            }
            
            
            var editors = await GetDocuments(solution)
                .ToAsyncEnumerable()
                .SelectAwait(async x => await DocumentEditor.CreateAsync(x))
                .ToDictionaryAsync(x => x.OriginalDocument.FilePath);
            
            var fixers = new[]
            {
                typeof(MethodRemapper),
                typeof(ImplicitGenericsFixer)
            };
            
            foreach (var fixerType in fixers)
            {
                var fixer = (Fixer)Activator.CreateInstance(fixerType, editors);
                fixer.Apply();
            }
            
            foreach (var editor in editors.Values)
            {
                solution = solution.WithDocumentSyntaxRoot(editor.OriginalDocument.Id, editor.GetChangedRoot().NormalizeWhitespace());
            }
            

            workspace.TryApplyChanges(solution);
        }
    }
}