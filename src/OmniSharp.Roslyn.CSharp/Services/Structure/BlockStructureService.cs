using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Structure;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.V2;

namespace OmniSharp.Roslyn.CSharp.Services.Structure
{
    [OmniSharpHandler(OmniSharpEndpoints.V2.BlockStructure, LanguageNames.CSharp)]
    public class BlockStructureService : IRequestHandler<BlockStructureRequest, BlockStructureResponse>
    {
        private readonly OmniSharpWorkspace _workspace;

        [ImportingConstructor]
        public BlockStructureService(OmniSharpWorkspace workspace)
        {
            _workspace = workspace;
        }

        public async Task<BlockStructureResponse> Handle(BlockStructureRequest request)
        {
            // To provide complete code structure for the document wait until all projects are loaded.
            var document = await _workspace.GetDocumentFromFullProjectModelAsync(request.FileName);
            if (document == null)
            {
                return new BlockStructureResponse { Spans = Array.Empty<CodeFoldingBlock>() };
            }

            var mapper = EvolveUI.ShouldProcess(document) ? EvolveUI.GetMapper(document) : null;
            var text = mapper != null ? mapper.original_source : await document.GetTextAsync();

            var options = new OmniSharpBlockStructureOptions(
                ShowBlockStructureGuidesForCommentsAndPreprocessorRegions: true,
                ShowOutliningForCommentsAndPreprocessorRegions: true);

            var structure = await OmniSharpBlockStructureService.GetBlockStructureAsync(document, options, CancellationToken.None);

            var outliningSpans = new List<CodeFoldingBlock>();
            foreach(var span in structure.Spans)
            {
                if (span.IsCollapsible)
                {
                    TextSpan? textspan;
                    if (mapper != null)
                    {
                        textspan = mapper.ConvertModifiedTextSpanToOriginal(span.TextSpan);
                        if (!textspan.HasValue)
                            continue;
                    }
                    else
                        textspan = span.TextSpan;

                    outliningSpans.Add(new CodeFoldingBlock(
                        text.GetRangeFromSpan(textspan.Value),
                        type: ConvertToWellKnownBlockType(span.Type)));
                }
            }

            return new BlockStructureResponse() { Spans = outliningSpans };
        }

        private string ConvertToWellKnownBlockType(string kind)
        {
            return kind == CodeFoldingBlockKinds.Comment || kind == CodeFoldingBlockKinds.Imports
                ? kind
                : kind == OmniSharpBlockTypes.PreprocessorRegion
                    ? CodeFoldingBlockKinds.Region
                    : null;
        }
    }
}
