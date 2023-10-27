using System;
using Microsoft.CodeAnalysis;
using OmniSharp.Models.Diagnostics;
using OmniSharp.Roslyn.CSharp.Services.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Roslyn;

namespace OmniSharp.Helpers
{
    internal static class DiagnosticExtensions
    {
        private static readonly ImmutableHashSet<string> _tagFilter =
            ImmutableHashSet.Create("Unnecessary", "Deprecated");

        private static DocumentId cached_documentid;
        private static WeakReference<EvolveUIMapper> cached_mapper;

        internal static DiagnosticLocation ToDiagnosticLocation(this Diagnostic diagnostic,
            DocumentId documentId = null)
        {
            var span = diagnostic.Location.GetMappedLineSpan();
            var StartLinePosition = span.StartLinePosition;
            var EndLinePosition = span.EndLinePosition;

            EvolveUIMapper mapper = null;
            if (documentId != null)
            {
                if (cached_documentid == documentId)
                    cached_mapper.TryGetTarget(out mapper);
                if (mapper == null)
                {
                    mapper = EvolveUIManager.GetMapper(documentId);
                    cached_documentid = mapper?.document_id;
                    cached_mapper = new WeakReference<EvolveUIMapper>(mapper);
                }
            }
            if (mapper != null)
            {
                var StartLinePosition2 = mapper.ConvertModifiedLinePositionToOriginal(StartLinePosition);
                var EndLinePosition2 = mapper.ConvertModifiedLinePositionToOriginal(EndLinePosition);
                if (!StartLinePosition2.HasValue || !EndLinePosition2.HasValue)
                    return null;
                StartLinePosition = StartLinePosition2.Value;
                EndLinePosition = EndLinePosition2.Value;
            }

            return new DiagnosticLocation
            {
                FileName = span.Path,
                Line = StartLinePosition.Line,
                Column = StartLinePosition.Character,
                EndLine = EndLinePosition.Line,
                EndColumn = EndLinePosition.Character,
                Text = diagnostic.GetMessage(),
                LogLevel = diagnostic.Severity.ToString(),
                Tags = diagnostic
                    .Descriptor.CustomTags
                    .Where(x => _tagFilter.Contains(x))
                    .ToArray(),
                Id = diagnostic.Id
            };
        }

        internal static IEnumerable<DiagnosticLocation> DistinctDiagnosticLocationsByProject(this IEnumerable<DocumentDiagnostics> documentDiagnostic)
        {
            return documentDiagnostic
                .SelectMany(x => x.Diagnostics, (parent, child) => (projectName: parent.ProjectName, documentId: parent.DocumentId, diagnostic: child))
                .Select(x => new
                {
                    location = x.diagnostic.ToDiagnosticLocation(x.documentId),
                    project = x.projectName
                })
                .Where(x => x.location != null)
                .GroupBy(x => x.location)
                .Select(x =>
                {
                    var location = x.First().location;
                    location.Projects = x.Select(a => a.project).ToList();
                    return location;
                });
        }
    }
}
