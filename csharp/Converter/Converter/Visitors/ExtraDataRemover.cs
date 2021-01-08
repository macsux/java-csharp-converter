using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Converter.Visitors
{
    public class AttributeRemover : CSharpSyntaxRewriter
    {
        private HashSet<string> attributesToRemove = new()
        {
            "SuppressWarnings",
            "FunctionalInterface"
        };
        public override SyntaxNode? VisitAttribute(AttributeSyntax node)
        {
            if (attributesToRemove.Contains(node.Name.ToString()))
                return null;
            return base.VisitAttribute(node);
        }

        public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
        {
            List<AttributeSyntax> newAttributes = new();
            foreach (var attribute in node.Attributes)
            {
                if (!attributesToRemove.Contains(attribute.Name.ToString()))
                    newAttributes.Add(attribute);
            }

            if (!newAttributes.Any())
                return null;
            node = node.WithAttributes(SeparatedList(newAttributes));
            return base.VisitAttributeList(node);
        }

        private static HashSet<string> _typesToStripGenerics = new ()
        {
            "Type"
        };
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if(_typesToStripGenerics.Contains(node.Identifier.Text))
                return IdentifierName(node.Identifier.Text);
            return base.VisitGenericName(node);
        }
    }
}