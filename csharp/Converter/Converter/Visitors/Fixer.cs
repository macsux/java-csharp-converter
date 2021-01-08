using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Converter.Visitors
{
    public class Fixer : CSharpSyntaxRewriter// CSharpSyntaxVisitor<bool>
    {
        protected readonly Dictionary<string, DocumentEditor> Editors;
        // private Lazy<SemanticModel> _model;
        protected SemanticModel Model => Editor.SemanticModel;// => _model.Value;

        public Fixer(Dictionary<string, DocumentEditor> editors)
        {
            Editors = editors;
            // Model = model;
            // Editor = editor;
            // _model  = new Lazy<SemanticModel>(() => this.Editor.OriginalDocument.GetSemanticModelAsync().Result);
        }
        
        public DocumentEditor Editor { get; protected set; }

        public virtual void Apply()
        {
            foreach (var editor in Editors.Values)
            {
                Editor = editor;
                // Model = editor.SemanticModel;
                this.Visit(editor.OriginalRoot);
            }
        }

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