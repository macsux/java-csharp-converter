using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converter.Visitors
{
    [Priority(Priority = 100)]
    public class ClassRemapper : CSharpSyntaxRewriter
    {
        private static readonly Dictionary<string, string> _typeMap = new()
        {
            {"Logger", "ILogger"},
            {"Class", "Type"},
            {"Map", "IDictionary"},
            {"HashMap", "Dictionary"},
            {"List", "IList"},
            {"Collection", "ICollection"},
            {"ArrayList", "List"},
            {"Set", "ISet"},
            {"Callable", "Func"},
            {"Supplier", "Func"},
            {"RuntimeException", "Exception"},
            {"Throwable", "Exception"},
            {"boolean", "bool"},
            {"Long", "long"},
            {"String", "string"},
            {"Integer", "int"},
            {"Double", "double"},
            {"IllegalStateException", "InvalidOperationException"},
            {"IllegalArgumentException", "ArgumentException"},
            {"ConcurrentMap","ConcurrentDictionary"},
            {"AutoCloseable","IDisposable"},
        };

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            return node.WithIdentifier(MapName(node.Identifier));
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            return base.VisitGenericName(node.WithIdentifier(MapName(node.Identifier)));
        }

        public static string Map(string val)
        {
            if (_typeMap.TryGetValue(val, out var remap))
                return remap;
            return val;
        }

        public static SyntaxToken MapName(SyntaxToken val)
        {
            return Identifier(Map(val.Text));
        } 
    }

}