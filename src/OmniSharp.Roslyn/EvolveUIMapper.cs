using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions;
using OmniSharp.Models.V2;
using OmniSharp.Roslyn.Utilities;
using Range = OmniSharp.Models.V2.Range;

// ReSharper disable InconsistentNaming

namespace OmniSharp.Roslyn
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal class EvolveUIMapper
    {
        private Func<Document> GetDocument;
        public Document document => GetDocument != null ? GetDocument() : null;
        public DocumentId document_id => document?.Id;
        public readonly string filepath;

        private SourceText _original_source;
        private SourceText _modified_source;
        public SourceText original_source
        {
            get {
                LoadTextIfNeeded();
                return _original_source;
            }
        }
        public SourceText modified_source
        {
            get {
                LoadTextIfNeeded();
                return _modified_source;
            }
        }
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


        internal string GetDebuggerDisplay()
            => @$"{document_id?.Id.ToString() ?? "-"}, lines({original_source?.Lines?.Count ?? -1}, {modified_source?.Lines?.Count ?? -1})";


        public SourceText ApplyText(SourceText source_text, bool force = false)
        {
            Debug.Assert(source_text != null);
            if (source_text == null)
                return null;
            if ((_original_source?.ContentEquals(source_text) ?? false) && !force)
                return _modified_source;

            _modified_source = _original_source = source_text;
            chunks.Clear();
            chunks.Add(new OriginalTextChunk());
            chunks[0].modified_span = chunks[0].original_span = new TextSpan(0, _original_source.Length);
            chunks.Add(new OriginalTextChunk()); // empty chunk to allow addressing place behind the last char
            chunks[1].modified_span =
                chunks[1].original_span = new TextSpan(_original_source.Length, 0);

            ProcessSource();
            return _modified_source;
        }
        private void LoadTextIfNeeded()
        {
            if (_original_source != null)
                return;

            try
            {
                if (document?.TryGetText(out var source_text) ?? false)
                    ApplyText(source_text);
            } catch
            {
            }
        }

        private readonly string processed_marker = "<<EvolveUI processed marker>>";
        private void ProcessSource()
        {
            Debug.Assert(!original_string.Contains(processed_marker));

            // #TODO: inserting the same single point won't work yet
            InsertLine(0, $"// {processed_marker} blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
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
            // #EVOLVEUI maybe hint chunk parameter as an optimization
            int i = chunks.BinarySearch(null, Comparer<Chunk>.Create((l, r) => {
                // comparer looks if index is below, above or within textspan
                // since it takes two textspans as a parameter, 'null' is a marker to use index for comparison
                int less = l == null ? -1 : 1;
                var myspan = GetTextSpan(l ?? r);
                if(index < myspan.Start)
                    return less;
                else if (myspan.IsEmpty && index == myspan.End)
                    return 0;       // #EVOLVEUI revisit this? should chunks with zero length exist?
                else if(index < myspan.End)
                    return 0;
                else
                    return -less;
            }));
            if(i < 0)
                return null;
            Debug.Assert(GetTextSpan(chunks[i]).Contains2(index));

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
                _modified_source = _modified_source.WithChanges(new TextChange(new TextSpan(ret.modified_span.Start, ret.original_span.Length), with));
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
            if(chunk is not OriginalTextChunk)
            {
                // at the moment ModifiedTextChunks are not supported, but it's ok if index is on its bounds
                if(index_within_chunk == 0)
                    return chunk.modified_start;
                else if(chunk.original_length > 0 && index_within_chunk == chunk.original_length - 1)
                    return chunk.modified_length > 0 ? chunk.modified_end - 1 : chunk.modified_end;
                else
                    return null;
            }

            Debug.Assert(chunk.modified_span.Contains2(chunk.modified_span.Start + index_within_chunk));
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
            {
                // at the moment ModifiedTextChunks are not supported, but it's ok if index is on its bounds
                if (index_within_chunk == 0)
                    return chunk.original_start;
                else if (chunk.modified_length > 0 && index_within_chunk == chunk.modified_length - 1)
                    return chunk.original_length > 0 ? chunk.original_end - 1 : chunk.original_end;
                else
                    return null;
            }

            Debug.Assert(chunk.original_span.Contains2(chunk.original_span.Start + index_within_chunk));
            return chunk.original_span.Start + index_within_chunk;
        }

        public int? ConvertOriginalLineColumnToModifiedIndex(int line, int column)
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
        public int? ConvertOriginalPointToModifiedIndex(Point point) =>
            ConvertOriginalLineColumnToModifiedIndex(point.Line, point.Column);

        public TextSpan? ConvertOriginalRangeToModifiedSpan(Range range)
        {
            int? i = ConvertOriginalPointToModifiedIndex(range.Start);
            int? j = ConvertOriginalPointToModifiedIndex(range.End);
            if (i.HasValue && j.HasValue)
                return TextSpan.FromBounds(i.Value, j.Value);
            else
                return null;
        }

        public TextSpan? ConvertOriginalTextSpanToModified(TextSpan span)
        {
            int? start = OriginalToModifiedIndex(span.Start);
            int? end = OriginalToModifiedIndex(span.End);
            return start.HasValue && end.HasValue ? TextSpan.FromBounds(start.Value, end.Value) : null;
        }

        public TextSpan? ConvertModifiedTextSpanToOriginal(TextSpan span)
        {
            int? start = ModifiedToOriginalIndex(span.Start);
            int? end = ModifiedToOriginalIndex(span.End);
            return start.HasValue && end.HasValue ? TextSpan.FromBounds(start.Value, end.Value) : null;
        }

        public LinePosition? ConvertModifiedIndexToOriginalLinePosition(int position)
        {
            try
            {
                int? index = ModifiedToOriginalIndex(position);
                return index.HasValue ? original_source.Lines.GetLinePosition(index.Value) : null;
            } catch (Exception)
            {
                return null;
            }
        }

        public Point ConvertModifiedIndexToOriginalPoint(int position)
        {
            LinePosition? lpos = ConvertModifiedIndexToOriginalLinePosition(position);
            return lpos.HasValue ? new Point{Line = lpos.Value.Line, Column = lpos.Value.Character} : null;
        }
        public LinePosition? ConvertModifiedLinePositionToOriginal(LinePosition linepos) =>
            ConvertModifiedIndexToOriginalLinePosition(modified_source.Lines.GetPosition(linepos));
    }
}
