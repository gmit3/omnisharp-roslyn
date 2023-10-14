using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Roslyn.Utilities;
// ReSharper disable InconsistentNaming

namespace OmniSharp.Roslyn
{
    internal class EvolveUIMapper
    {
        private Func<Document> GetDocument;
        public Document document => GetDocument != null ? GetDocument() : null;
        public readonly string filepath;

        public SourceText original_source { get; private set; }
        public SourceText modified_source { get; private set; }
        private string original_string => original_source.ToString();
        private string modified_string => modified_source.ToString();
        private TextSpan original_span => new TextSpan(0, original_string.Length);
        private TextSpan modified_span => new TextSpan(0, modified_string.Length);
        private TextLineCollection original_lines => original_source.Lines;
        private TextLineCollection modified_lines => modified_source.Lines;

//        private StringBuilder modified_builder;

        public class Chunk
        {
            public TextSpan original_span = new();
            public TextSpan modified_span = new();

            public int original_start => original_span.Start;
            public int original_end => original_span.End;
            public int original_length => original_span.Length;
            public int modified_start => modified_span.Start;
            public int modified_end => modified_span.End;
            public int modified_length => modified_span.Length;
        }
        public class OriginalTextChunk : Chunk
        { }
        public class ModifiedTextChunk : Chunk
        { }

        private readonly List<Chunk> chunks = new();


        public EvolveUIMapper(OmniSharpWorkspace workspace, string filepath)
        {
            Debug.Assert(workspace != null && filepath != null);
            if(workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            if(string.IsNullOrWhiteSpace(filepath))
                throw new ArgumentNullException(nameof(filepath));

            GetDocument = () => workspace.GetDocument(filepath);
            this.filepath = filepath;
        }

        public EvolveUIMapper(Document document)
        {
            Debug.Assert(document != null);
            if(document == null)
                throw new ArgumentNullException(nameof(document));

            WeakReference<Document> wdocument = new(document);
            GetDocument = () => wdocument.TryGetTarget(out var doc) ? doc: null;
            filepath = document.FilePath;
        }


        public SourceText ApplyText(SourceText source_text, bool force = false)
        {
            Debug.Assert(source_text != null);
            if (source_text == null)
                return null;
            if ((original_source?.ContentEquals(source_text) ?? false) && !force)
                return null;

            this.modified_source = this.original_source = source_text;
            chunks.Clear();
            chunks.Add(new OriginalTextChunk());
            chunks[0].modified_span = chunks[0].original_span = new TextSpan(0, original_string.Length);

            ProcessSource();
            return modified_source;
        }

        private void ProcessSource()
        {
            // #TODO: inserting the same single point won't work yet
            InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
//             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
//             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            Replace("template AppRoot : AppRoot", "class __evolveUI__AppRoot");
            ReplaceAll("state", "");
            ReplaceAll("[@", "\"xx-style-xx\"", "]");
        }

        public void TextChanged()
        {
//             if (wdocument.TryGetTarget(out var document))
//             {
//                 var source_text = document.GetTextAsync().Result;
//                 Debug.Assert(source_text != null);
//                 if(source_text != null)
//                     ApplyText(source_text);
//             }
        }

        public (int, int)? FindChunkIndex(int index, Func<Chunk, TextSpan> GetTextSpan)
        {
            int i = chunks.BinarySearch(null, Comparer<Chunk>.Create((l, r) => {
                int less = l == null ? -1 : 1;
                var myspan = l ?? r;
                if(index < GetTextSpan(myspan).Start)
                    return less;
                else if(index < GetTextSpan(myspan).End)
                    return 0;
                else
                    return -less;
            }));
            if(i < 0)
                return null;
            Debug.Assert(GetTextSpan(chunks[i]).Contains(index));

            return (i, index - GetTextSpan(chunks[i]).Start);
        }

        private (Chunk, int)? ChunkIndexToChunk((int, int)? chunk_index) => chunk_index switch {
            var (chunk_index2, index) => (chunks[chunk_index2], index),
            null => null
        };
        public (int, int)? FindOriginalChunkIndex(int index) => FindChunkIndex(index, (s) => s.original_span);
        public (int, int)? FindModifiedChunkIndex(int index) => FindChunkIndex(index, (s) => s.modified_span);
        public (Chunk, int)? FindOriginalChunk(int index) => ChunkIndexToChunk(FindChunkIndex(index, (s) => s.original_span));
        public (Chunk, int)? FindModifiedChunk(int index) => ChunkIndexToChunk(FindChunkIndex(index, (s) => s.modified_span));

        public Chunk Replace(TextSpan what, string with)
        {
            try
            {
                Debug.Assert(original_span.Contains(what));
                if(!original_span.Contains(what))
                    return null;

                // find chunks
                var (begin_chunk_index, begin_index_within_chunk) = FindModifiedChunkIndex(what.Start).Value;
                var (end_chunk_index, end_index_within_chunk) = FindModifiedChunkIndex(what.Length > 0 ? what.End - 1 : what.End).Value;
                Debug.Assert(begin_chunk_index == end_chunk_index); // support more if needed
                if(what.Length > 0)
                    ++end_index_within_chunk; // had to be inclusive for Find to be in a correct chunk
                var begin_chunk = chunks[begin_chunk_index];
                var end_chunk = chunks[end_chunk_index];

                // split chunk as needed
                List<Chunk> replacement_chunks = new();
                if(begin_index_within_chunk > 0)
                    replacement_chunks.Add(new OriginalTextChunk() {
                        original_span = new(begin_chunk.original_start, begin_index_within_chunk),
                        modified_span = new(begin_chunk.modified_start, begin_index_within_chunk)
                    });
                Chunk ret;
                replacement_chunks.Add(ret = new ModifiedTextChunk() {
                    original_span = TextSpan.FromBounds(begin_chunk.original_start + begin_index_within_chunk, end_chunk.original_start + end_index_within_chunk),
                    modified_span = new(0, with.Length)
                });
                if(end_index_within_chunk < end_chunk.original_end)
                {
                    replacement_chunks.Add(new OriginalTextChunk() {
                        original_span = TextSpan.FromBounds(end_chunk.original_start + end_index_within_chunk, end_chunk.original_end),
                        modified_span = new(0, end_chunk.modified_length - end_index_within_chunk)
                    });
                }

                // update chunks; update offsets in modified chunks
                chunks.Replace(begin_chunk_index, end_chunk_index - begin_chunk_index + 1, replacement_chunks);
                begin_chunk = replacement_chunks.First();
                end_chunk = replacement_chunks.Last();

                // update offsets in modified chunks
                int next = begin_chunk.modified_start;  // this one is still ok
                for(int i = begin_chunk_index; i < chunks.Count; i++)
                {
                    int nextnext = next + chunks[i].modified_length;
                    chunks[i].modified_span = TextSpan.FromBounds(next, nextnext);
                    next = nextnext;
                }

                // update modified text buffer
                modified_source = modified_source.WithChanges(new TextChange(new TextSpan(ret.modified_span.Start, ret.original_span.Length), with));
                Debug.Assert(next == modified_string.Length);

                return ret;
            }
            catch(Exception e)
            {
                Debug.Assert(false, e.ToString());
                return null;
            }
        }

        public int Replace(string string_beginning, string with, string string_ending = null, int repeat_count = 1)
        {
            int replaces = 0, i = 0;
            while (repeat_count-- > 0)
            {
                i = modified_string.IndexOf(string_beginning, i);
                if (i == -1)
                    break;
                int j = i + string_beginning.Length;
                if (!string.IsNullOrWhiteSpace(string_ending))
                {
                    j = modified_string.IndexOf(string_ending, j);
                    if (j == -1)
                        break;
                    j += string_ending.Length;
                }

                var span = TextSpan.FromBounds(i, j);
                if(Replace(span, with) != null)
                    ++replaces;
                i += with.Length;
            }
            return replaces;
        }
        public int ReplaceAll(string string_beginning, string with, string string_ending = null) => Replace(string_beginning, with, string_ending, int.MaxValue);
        public void Insert(int position, string what) => Replace(new TextSpan(position, 0), what);
        public void InsertLine(int position, string what) => Insert(position, what + Environment.NewLine);  // #TODO: find out new line within file

        public int? OriginalToModifiedIndex(int index)
        {
            var oc = FindOriginalChunk(index);
            if (!oc.HasValue)
                return null;
            var (chunk, index_within_chunk) = oc.Value;

//            Debug.Assert(chunk is OriginalTextChunk);
            if (chunk is not OriginalTextChunk)
                return null;    // at the moment ModifiedTextChunks are not supported

            Debug.Assert(chunk.modified_span.Contains(chunk.modified_span.Start + index_within_chunk));
            return chunk.modified_span.Start + index_within_chunk;
        }

        public int? ModifiedToOriginalIndex(int index)
        {
            var oc = FindModifiedChunk(index);
            if(!oc.HasValue)
                return null;
            var (chunk, index_within_chunk) = oc.Value;

//            Debug.Assert(chunk is OriginalTextChunk);
            if(chunk is not OriginalTextChunk)
                return null;    // at the moment ModifiedTextChunks are not supported

            Debug.Assert(chunk.original_span.Contains(chunk.original_span.Start + index_within_chunk));
            return chunk.original_span.Start + index_within_chunk;
        }

        public int? ConvertOriginalLineColumnToMappedIndex(int line, int column)
        {
            try
            {
                int index = original_source.Lines.GetPosition(new LinePosition(line, column));
                return OriginalToModifiedIndex(index);
            } catch (Exception)
            {
                return null;
            }
        }

        public TextSpan? ConvertModifiedTextSpanToOriginal(TextSpan span)
        {
            int? start = ModifiedToOriginalIndex(span.Start);
            int? end = ModifiedToOriginalIndex(span.End);
            return start.HasValue && end.HasValue ? TextSpan.FromBounds(start.Value, end.Value) : null;
        }

}
}
