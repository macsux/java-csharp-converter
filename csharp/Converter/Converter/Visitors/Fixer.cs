using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Converter.Visitors
{
    public class Fixer : CSharpSyntaxRewriter// CSharpSyntaxVisitor<bool>
    {
        private Lazy<SemanticModel> _model;
        protected SemanticModel Model => _model.Value;

        public Fixer(DocumentEditor editor)
        {
            Editor = editor;
            _model  = new Lazy<SemanticModel>(() => this.Editor.OriginalDocument.GetSemanticModelAsync().Result);
        }
        
        public DocumentEditor Editor { get; }

        // public Document VisitDocument()
        // {
        //     Visit(Editor.OriginalRoot);
        //     return Editor.GetChangedDocument();
        // }

        // public async Task Initialize()
        // {
        //     Model = await Editor.OriginalDocument.GetSemanticModelAsync();
        // } 
    }
}