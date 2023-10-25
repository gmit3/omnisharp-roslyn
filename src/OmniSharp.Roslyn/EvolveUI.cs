using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;
using Range = OmniSharp.Models.V2.Range;

namespace OmniSharp.Roslyn
{
    internal class EvolveUI
    {
        private static readonly Dictionary<DocumentId, EvolveUIMapper> _mappers = new();

        public static bool ShouldProcess(string filepath) => filepath?.EndsWith(".ui", StringComparison.CurrentCultureIgnoreCase) ?? false;
        public static bool ShouldProcess(Document document) => ShouldProcess(document?.FilePath);

        public static EvolveUIMapper GetOrAddMapper(Document document)
        {
            Debug.Assert(document != null);
            if(!ShouldProcess(document))
                return null;

            var mapper = GetMapper(document);
            if(mapper == null)
                _mappers[document.Id] = mapper = new(document);
            return mapper;
        }


        public static int? ConvertOriginalLineColumnToModifiedIndex(Document document, int line, int column, out EvolveUIMapper mapper)
        {
            mapper = GetMapper(document);
            return mapper?.ConvertOriginalLineColumnToModifiedIndex(line, column);
        }

        public static TextSpan? ConvertOriginalRangeToModifiedSpan(Document document, Range range, out EvolveUIMapper mapper)
        {
            mapper = GetMapper(document);
            return mapper?.ConvertOriginalRangeToModifiedSpan(range);
        }

        public static TextSpan? ConvertOriginalTextSpanToModified(Document document, TextSpan span,
            out EvolveUIMapper mapper)
        {
            mapper = GetMapper(document);
            return mapper?.ConvertOriginalTextSpanToModified(span);
        }



        public static SourceText ApplyText(Document document, SourceText sourceText)
        {
            if(document == null || sourceText == null)
                return sourceText;

            var mapper = GetMapper(document);
            if (mapper == null)
            {
                Debug.Assert(!ShouldProcess(document)); // it should be already tracked? if debug will fire then add it here
                return sourceText;
            }

            return mapper.ApplyText(sourceText) ?? sourceText;
        }


        // events from the outside
        public static void OnDocumentAdded(Document document)
        {
            GetOrAddMapper(document);
//             mapper.TextChanged();
        }
        public static void OnDocumentClosing(DocumentId documentId)
        {
            _mappers.Remove(documentId);
        }
        public static void OnDocumentTextChanged(Document document)
        {
//            GetMapper(document)?.TextChanged();
        }

        public static EvolveUIMapper GetMapper(DocumentId documentId) => documentId != null ? _mappers.GetValueOrDefault(documentId) : null;
        public static EvolveUIMapper GetMapper(Document document) => GetMapper(document?.Id);
        public static bool IsEvolveUI(DocumentId documentId) => GetMapper(documentId) != null;
        public static bool IsEvolveUI(Document document) => GetMapper(document) != null;
    }
}
