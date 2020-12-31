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
            
            return node
                    .WithUsings(List(node.Usings.Where(x => x.Name.ToString().StartsWith("org.axon"))))
                .AddUsings(UsingDirective(IdentifierName("System")))
                .AddUsings(UsingDirective(IdentifierName("System.Linq")))
                .AddUsings(UsingDirective(IdentifierName("System.Reflection")))
                .AddUsings(UsingDirective(IdentifierName("System.Collections.Generic")))
                .AddUsings(UsingDirective(IdentifierName("System.Collections.Concurrent")));
        }
    }
}