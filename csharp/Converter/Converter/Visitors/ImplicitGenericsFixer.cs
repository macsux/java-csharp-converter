using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Converter.Visitors
{
    public class ImplicitGenericsFixer : Fixer
    {

        public ImplicitGenericsFixer(DocumentEditor editor) : base(editor)
        {
        }
        
        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            // add generic arguments when implicitly omitted in java
            

            GenericNameSyntax varType = null;
            if (node.Parent is EqualsValueClauseSyntax) // ex. HashSet<string> v = new HashSet<>();
            {
                varType = node.Ancestors()
                    .OfType<VariableDeclarationSyntax>()
                    .Select(x => x.Type)
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault();
            }
            else if (node.Parent is ReturnStatementSyntax) // return = new HashSet<>();
            {
                varType = node.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(x => x.ReturnType)
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault();
            }
            else if (node.Parent is AssignmentExpressionSyntax assignment) // this.item = new HashSet<>();
            {
                var symbol = Model.GetSymbolInfo(assignment.Left).Symbol;
                if(symbol is IFieldSymbol fieldSymbol && fieldSymbol.Type is INamedTypeSymbol namedType)
                {
                    //newExpression.Type.GetType().Dump();
                    varType = node.Type as GenericNameSyntax;
                    if(!varType?.TypeArgumentList?.Arguments.Any() ?? false)
                        return base.VisitObjectCreationExpression(node);
                    varType ??= GenericName(((IdentifierNameSyntax) node.Type).Identifier);
			
                    var typeArgs = namedType.TypeArguments
                        .Select(x => ParseTypeName(x.ToString()))
                        .ToList();
                    varType = varType.WithTypeArgumentList(TypeArgumentList(SeparatedList(typeArgs)));
                }
            }
            else
            {
                return base.VisitObjectCreationExpression(node);
            }

            if(varType == null) 
                return base.VisitObjectCreationExpression(node);
            var newType = node.Type as GenericNameSyntax;
            var newNode = node;
            if (node.Type is SimpleNameSyntax simpleName && simpleName.Identifier.Text == varType.Identifier.Text)
            {
                //node = node.WithType( GenericName(simpleName.Identifier, varType.TypeArgumentList)).NormalizeWhitespace();
                Editor.ReplaceNode(node, (existing, generator) => existing.WithType(varType).NormalizeWhitespace());
                // node =  node.WithType(varType).NormalizeWhitespace();
                return base.VisitObjectCreationExpression(node);
            }

            if(varType == null || newType == null || (!newType.TypeArgumentList?.Arguments.OfType<OmittedTypeArgumentSyntax>().Any() ?? false))
                return base.VisitObjectCreationExpression(node);
            // node = node.WithType(newType.WithTypeArgumentList(varType.TypeArgumentList)).NormalizeWhitespace();
            Editor.ReplaceNode(node, (existing, generator) => existing.WithType(newType.WithTypeArgumentList(varType.TypeArgumentList)).NormalizeWhitespace());
            return base.VisitObjectCreationExpression(node);
        }
    }

}