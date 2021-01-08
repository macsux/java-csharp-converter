using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using NuGet.Packaging;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Converter.Visitors
{
    
    public class MethodRemapper : Fixer 
    {

        Dictionary<Tuple<string,string>,ClassMap> fixes = new ClassMap[] 
            {
                new("Object", "GetClass", "GetType()"),
                new("Func", "Get", "Invoke()"),
                new("Predicate", "Test", "Invoke()"),
                new("Exception", "GetCause", "InnerException"),
                new("MethodInfo", "GetParameterCount", "GetParameters().Length"),
                new("MethodInfo", "GetReturnType", "ReturnType"),
                new("MethodInfo", "GetDeclaringClass", "DeclaringType"),
                new("String", "ToLowerCase", "ToLower()"),
                new("String", "Length", "Length"),
                new("IList`1", "Size", "Count"),
                new("ILogger", "Debug", "LogDebug()"),
                new("IDictionary`2", "KeySet", "Keys"),
                new("MemberInfo", "GetName", "Name"),
                new("FieldInfo", "Set", "SetValue()"),
                new("Type", "GetDeclaredMethods", "GetMethods()"),
                new("IDictionary", "Get", "[]"),
                new("FieldInfo", "GetGenericType", "FieldType.GetGenericTypeDefinition()"),
                new("MethodInfo", "GetGenericReturnType", "ReturnType.GetGenericTypeDefinition()"),
                new("Field", "ToGenericString", "ToString()"),
                new("MethodBase", "ToGenericString", "ToString()"),
            }
            .ToDictionary(x => Tuple.Create(x.FromClass, x.FromMethod));

        // private Dictionary<SyntaxNode, SyntaxNode> _replacements;
        // private SemanticModel _model;

        public MethodRemapper(Dictionary<string, DocumentEditor> editors) : base(editors)
        {
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Parent is not InvocationExpressionSyntax)
                return base.VisitMemberAccessExpression(node);
            var fromMethod = node.Name.Identifier.Text;
            var symbol = Model.GetSymbolInfo(node.Expression).Symbol;
            ITypeSymbol fromType =
                (symbol as IParameterSymbol)?.Type ??
                (symbol as IFieldSymbol)?.Type ??
                (symbol as ILocalSymbol)?.Type ??
                (symbol as IMethodSymbol)?.ReturnType;

            if (fromType == null)
                return base.VisitMemberAccessExpression(node);

            var typesToCheck = new List<ITypeSymbol>() { fromType };
            var baseClass = fromType.BaseType;
            while (baseClass != null)
            {
                typesToCheck.Add(baseClass);
                baseClass = baseClass.BaseType;
            }

            typesToCheck.AddRange(fromType.AllInterfaces);

            foreach (var typeToCheck in typesToCheck)
            {
                if (!fixes.TryGetValue(Tuple.Create(typeToCheck?.MetadataName, fromMethod), out var map)) continue;
                var to = ParseExpression(map.To);
                SyntaxNode nodeToReplace = node;
                var leftSide = node.Expression;
                ExpressionSyntax newNode;
                if (to is ElementAccessExpressionSyntax)
                {
                    nodeToReplace = node.Parent;
                    var arguments = ((InvocationExpressionSyntax) node.Parent).ArgumentList.Arguments;
                    newNode = ElementAccessExpression(node.Expression, BracketedArgumentList(arguments));

                }
                else
                {
                    if (to is InvocationExpressionSyntax invocation)
                    {
                        to = invocation.Expression;
                    }
                    else
                    {
                        nodeToReplace = node.Parent;
                    }

                    ExpressionSyntax innerExpression = to;
                    while (innerExpression is MemberAccessExpressionSyntax m)
                    {
                        innerExpression = m.Expression;
                        if (innerExpression is InvocationExpressionSyntax i)
                            innerExpression = i.Expression;
                    }

                    // convert root to member access and replace root with left side
                    var newInnerExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, leftSide, IdentifierName(((IdentifierNameSyntax) innerExpression).Identifier.Text));
                    newNode = to.ReplaceNode(innerExpression, newInnerExpression);
                }

                Editor.ReplaceNode(nodeToReplace, (existing, _) => newNode);
                break;
            }

            return base.VisitMemberAccessExpression(node);
        }

        record ClassMap(string FromClass, string FromMethod, string To);
      
    }
}