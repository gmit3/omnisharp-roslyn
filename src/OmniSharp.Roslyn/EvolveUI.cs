using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Document = Microsoft.CodeAnalysis.Document;

namespace OmniSharp.Roslyn
{
    internal class EvolveUI
    {
        private static readonly Dictionary<DocumentId, EvolveUIMapper> _mappers = new();

        // events from the outside
        public static void OnDocumentAdded(Document document)
        {
            Debug.Assert(document != null);
            if (document == null)
                return;
            if(_mappers.ContainsKey(document.Id))
                return;

            EvolveUIMapper mapper = new (document);
            _mappers[document.Id] = mapper;
            mapper.TextChanged();
        }
        public static void OnDocumentClosing(DocumentId documentId)
        {
            _mappers.Remove(documentId);
        }
        public static void OnDocumentTextChanged(Document document)
        {
            if(document != null && _mappers.TryGetValue(document.Id, out var mapper))
               mapper.TextChanged();
        }

    }
}
