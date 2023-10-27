using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniSharp.Roslyn
{
    internal partial class EvolveUIMapper
    {
        private readonly string processed_marker = "<<EvolveUI processed marker>>";
        private void ProcessSource()
        {
            Debug.Assert(!original_string.Contains(processed_marker));

            // #TODO: inserting the same single point won't work yet
            InsertLine(0, $"// {processed_marker} blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            //             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            //             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
//             Replace("template AppRoot : AppRoot", "class __evolveUI__AppRoot");
//             ReplaceAll("state", "");
//             ReplaceAll("[@", "\"xx-style-xx\"", "]");


        }
    }
}
