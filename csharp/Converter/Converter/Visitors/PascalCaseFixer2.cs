using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converter.Visitors
{
    public class PascalCaseFixer2  : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.Identifier.IsPascalCase())
            {
                node = node.WithIdentifier(Identifier(node.Identifier.Text.ToPascalCase()));
            }

            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var expression = node.Expression;
            switch (expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    // if (memberAccess.Expression is GenericNameSyntax genericName)
                    // {
                    //     genericName.WithIdentifier()
                    // }
                    expression = memberAccess.WithName(memberAccess.Name.WithIdentifier(Identifier(memberAccess.Name.Identifier.Text.ToPascalCase())));
                    // expression = memberAccess.WithName(IdentifierName(memberAccess.Name.Identifier.Text.ToPascalCase()));
                    break;
                case IdentifierNameSyntax identifierName:
                    expression = IdentifierName(identifierName.Identifier.Text.ToPascalCase());
                    break;
            }

            node = node.WithExpression(expression);
            
            return base.VisitInvocationExpression(node);
        }
    }
}