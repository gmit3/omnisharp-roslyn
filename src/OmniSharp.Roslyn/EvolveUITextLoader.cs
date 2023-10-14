using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace OmniSharp.Roslyn
{
    internal class EvolveUITextLoader : OmniSharpTextLoader
    {
        public EvolveUITextLoader(OmniSharpWorkspace workspace, string filePath) : base(filePath)
        {
            this.workspace = workspace;
            this.filepath = filePath;
        }

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            var doc = workspace?.GetDocument(filepath);
            mapper ??= EvolveUI.GetOrAddMapper(doc);

            var ret = base.LoadTextAndVersionAsync(options, cancellationToken);
            var tv = ret.Result;
            var text = mapper?.ApplyText(tv?.Text);
            if(text != null)
                ret = Task.FromResult(TextAndVersion.Create(text, tv.Version, mapper.filepath));
            return ret;
        }

        private readonly OmniSharpWorkspace workspace;
        private readonly string filepath;
        protected EvolveUIMapper mapper;
    }
}
