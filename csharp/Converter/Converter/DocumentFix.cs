using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Converter
{
    public record DocumentFix(SyntaxNode From, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> To)
    {
        public static DocumentFix Replace<T>(T from, Func<T, SyntaxGenerator, T> to) where T : SyntaxNode
        {
            return new DocumentFix(from, (existing, gen) => to((T)existing, gen));
        }
    }
}