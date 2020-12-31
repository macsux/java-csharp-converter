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
using Microsoft.CodeAnalysis.FindSymbols;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.MSBuild;
using static System.Console;


namespace Converter
{
    record ClassMap(string FromClass, string ToClass, string FromMethod, string ToMethod)
    {
        public ClassMap(string Class, string FromMethod, string ToMethod) : this(Class, null, FromMethod, ToMethod)
        {
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            var visitors = typeof(Program).Assembly.GetTypes()
                .Where(x => x.IsPublic && typeof(CSharpSyntaxRewriter).IsAssignableFrom(x))
                .OrderByDescending(x => x.GetCustomAttribute<PriorityAttribute>()?.Priority)
                .Select(x => Activator.CreateInstance(x))
                .Cast<CSharpSyntaxRewriter>()
                .ToList();
            MSBuildLocator.RegisterDefaults();
            var mylock = new object();
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (sender, workspaceFailedArgs) => WriteLine(workspaceFailedArgs.Diagnostic.Message);
                var solution = await workspace.OpenSolutionAsync(@"C:\projects\NAxonAuto\\first\NAxonFramework.sln");
                WriteLine($"Loaded solution {solution.FilePath}");
                // var nameFixer = new PascalCaseFixer();
                // solution = await nameFixer.Visit(workspace);
                //workspace.CurrentSolution
                 
                // var project = solution.Projects.First();
                foreach (var project in solution.Projects)
                {

                    // var compilation = await project.GetCompilationAsync();
                    // foreach (var doc in project.Documents.Where(x => x.FilePath.EndsWith("Assert.cs")))
                    // Parallel.ForEach(project.Documents, async doc =>
                    foreach (var doc in project.Documents)
                    {
                        var cu = (CompilationUnitSyntax) await doc.GetSyntaxRootAsync();
                        cu = cu.WithoutLeadingTrivia();
                        foreach (var visitor in visitors)
                        {
                            cu = (CompilationUnitSyntax) visitor.Visit(cu);
                        }

                        cu = (CompilationUnitSyntax) Microsoft.CodeAnalysis.Formatting.Formatter.Format(cu, workspace);

                        // lock (mylock)
                        // {
                            solution = solution.WithDocumentSyntaxRoot(doc.Id, cu);
                        // }
                    };
                }

                workspace.TryApplyChanges(solution);

            }
        }
    }
}