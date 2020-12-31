using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Converter.Visitors
{
    public class ImplicitGenericsFixer: CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            // add generic arguments when implicitly omitted in java
            // ex. HashSet<string> v = new HashSet<>();
            if(node.Parent is not EqualsValueClauseSyntax)
                return base.VisitObjectCreationExpression(node);
		
            var varType = node.Ancestors()
                .OfType<VariableDeclarationSyntax>()
                .Select(x => x.Type)
                .OfType<GenericNameSyntax>()
                .FirstOrDefault();

            if(varType == null) 
                return base.VisitObjectCreationExpression(node);
            var newType = node.Type as GenericNameSyntax;
		
            if (node.Type is SimpleNameSyntax simpleName && simpleName.Identifier.Text == varType.Identifier.Text)
            {
                node = node.WithType( SyntaxFactory.GenericName(simpleName.Identifier, varType.TypeArgumentList)).NormalizeWhitespace();
                node =  node.WithType(varType).NormalizeWhitespace();
                return base.VisitObjectCreationExpression(node);
            }

            if(varType == null || newType == null || (!newType.TypeArgumentList?.Arguments.OfType<OmittedTypeArgumentSyntax>().Any() ?? false))
                return base.VisitObjectCreationExpression(node);
            node = node.WithType(newType.WithTypeArgumentList(varType.TypeArgumentList)).NormalizeWhitespace();
		
            return base.VisitObjectCreationExpression(node);
        }
    }

    class A : B
    {
        protected void X()
        {
        }
    }

    class B
    {
        protected virtual void X()
        {
        }
    }
}