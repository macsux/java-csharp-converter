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
            // var visitors = typeof(Program).Assembly.GetTypes()
            //     .Where(x => x.IsPublic && typeof(CSharpSyntaxRewriter).IsAssignableFrom(x))
            //     .OrderByDescending(x => x.GetCustomAttribute<PriorityAttribute>()?.Priority)
            //     .Select(x => Activator.CreateInstance(x))
            //     .Cast<CSharpSyntaxRewriter>()
            //     .ToList();
            MSBuildLocator.RegisterDefaults();
            var mylock = new object();
            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (sender, workspaceFailedArgs) => WriteLine(workspaceFailedArgs.Diagnostic.Message);
            var solution = await workspace.OpenSolutionAsync(@"C:\projects\NAxonAuto\\first\NAxonFramework.sln");
            WriteLine($"Loaded solution {solution.FilePath}");

            
            // var nameFixer = new PascalCaseFixer();
            // solution = await nameFixer.Visit(workspace);
            // var fixers = new Type[]
            // {
            //     typeof(NamespaceFixer),
            //     typeof(AttributeRemover),
            //     typeof(ImplicitGenericsFixer),
            //     typeof(ClassRemapper),
            //     typeof(PascalCaseFixer2),
            // };

            foreach (var project in solution.Projects.Skip(2))
            {
                // foreach (var doc in project.Documents.Where(x => x.FilePath.Contains("RetryingCallback")))
                foreach (var doc in project.Documents)
                {
                    
                    //var fixer = (Fixer)Activator.CreateInstance(fixerType, editor);
                    var visitors = new List<CSharpSyntaxRewriter>
                    {
                        new NamespaceFixer(),
                        new AttributeRemover(),
                        new ClassRemapper(),
                        new PascalCaseFixer2()
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
                    var editor = await DocumentEditor.CreateAsync(solution.GetDocument(doc.Id));
                    var genericsFixer = new ImplicitGenericsFixer(editor);
                    genericsFixer.Visit(editor.OriginalRoot);
                    // genericsFixer.VisitDocument();
                    solution = solution.WithDocumentSyntaxRoot(doc.Id, editor.GetChangedRoot());
                }
            }
            

            workspace.TryApplyChanges(solution);
        }
    }
}