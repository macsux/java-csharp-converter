using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converter.Visitors
{
    public class NamespaceFixer : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var coreImports = new[]
            {
                "System",
                "System.Linq",
                "System.Reflection",
                "System.Collections.Generic",
                "System.Collections.Concurrent",
                "Microsoft.Extensions.Logging",
            };
            return node
                    .WithUsings(List(node.Usings
                        .Select(x => new {x.StaticKeyword, Name = x.Name.ToString()})
                        .Where(x => x.Name.StartsWith("org.axon"))
                        .Distinct()
                        .Union(coreImports.Select(x => new {StaticKeyword = Token(SyntaxKind.None), Name = x}))
                        .Select(x => UsingDirective(x.StaticKeyword, null, IdentifierName(x.Name)))
                    ));
                // .AddUsings(UsingDirective(IdentifierName("System")))
                // .AddUsings(UsingDirective(IdentifierName("System.Linq")))
                // .AddUsings(UsingDirective(IdentifierName("System.Reflection")))
                // .AddUsings(UsingDirective(IdentifierName("System.Collections.Generic")))
                // .AddUsings(UsingDirective(IdentifierName("System.Collections.Concurrent")))
                // .AddUsings(UsingDirective(IdentifierName("Microsoft.Extensions.Logging")));
        }
    }
}