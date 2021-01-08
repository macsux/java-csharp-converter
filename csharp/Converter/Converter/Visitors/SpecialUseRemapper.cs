using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Converter.Visitors
{
    public class SpecialUseRemapper : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            

            // // Void.TYPE -> typeof(Void)
            if (node.Expression is IdentifierNameSyntax identifierName)
            {
                var fromMethod = node.Name.Identifier.Text;
                var type = identifierName.Identifier.Text;
                var access = $"{type}.{fromMethod}";
                switch (access)
                {
                    case "Void.TYPE":
                        return TypeOfExpression(PredefinedType(Token(SyntaxKind.VoidKeyword)));
                    case "T.GetClass":
                        return TypeOfExpression(ParseTypeName("T"));
                    case "K.GetClass":
                        return TypeOfExpression(ParseTypeName("K"));
                       
                }
            }
            
            return node;
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierName)
                {
                    var fromMethod = memberAccessExpressionSyntax.Name.Identifier.Text;
                    var type = identifierName.Identifier.Text;
                    var access = $"{type}.{fromMethod}";
                    switch (access)
                    {
                        case "Arrays.AsList":
                        case "Collections.UnmodifiableList":
                            var paramExpression = node.ArgumentList.Arguments.First().Expression;
                            var newExpression = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, paramExpression, IdentifierName("ToList")));
                            return newExpression;
                    }
                }
            }
            return base.VisitInvocationExpression(node);
        }
    }
}