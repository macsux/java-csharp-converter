using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Converter.Visitors
{
    
    public class MethodRemapper : CSharpSyntaxRewriter 
    {

        static Dictionary<Tuple<string,string>,ClassMap> fixes = new ClassMap[] 
            {
                new("Func", "Get", "Invoke()"),
                new("Predicate", "Test", "Invoke()"),
                new("Exception", "GetCause", "InnerException"),
                // new("HashSet", "add", "Add()"),
                // new("ICollection", "add", "Add()"),
                // new("List", "add", "Add"),
                // new("IList", "add", "Add"),
            }
            .ToDictionary(x => Tuple.Create(x.FromClass, x.FromMethod));

        private Dictionary<SyntaxNode, SyntaxNode> _replacements;
        private SemanticModel _model;

        public MethodRemapper(SemanticModel model) : base()
        {
            _model = model;
        }

        public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax cu)
        {
            
            _replacements = cu.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Select(memberAccessExpression =>
                {
                    var fromMethod = memberAccessExpression.Name.Identifier.Text;
                    var symbol = _model.GetSymbolInfo(memberAccessExpression.Expression).Symbol;
                    ITypeSymbol fromType =
                        (symbol as IParameterSymbol)?.Type ??
                        (symbol as IFieldSymbol)?.Type ??
                        (symbol as ILocalSymbol)?.Type;
                    // return new {syntax = x, type};
                    if (fixes.TryGetValue(Tuple.Create(fromType?.Name, fromMethod), out var map))
                    {
                        return KeyValuePair.Create<SyntaxNode, SyntaxNode>(
                            memberAccessExpression, memberAccessExpression.WithName(IdentifierName(map.ToMethod)))
                            as KeyValuePair<SyntaxNode, SyntaxNode>?;
                    }

                    return (KeyValuePair<SyntaxNode, SyntaxNode>?) null;
                })
                .Where(x => x != null)
                .Select(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            return base.Visit(cu);
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            return _replacements.TryGetValue(node, out var replacement) ? replacement : base.VisitMemberAccessExpression(node);
        }

    }
}