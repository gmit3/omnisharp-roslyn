using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Roslyn.Utilities;
// ReSharper disable InconsistentNaming

namespace OmniSharp.Roslyn
{
    internal class EvolveUIMapper
    {
        private SourceText original_source = null;
        private SourceText modified_source = null;
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


        public EvolveUIMapper(SourceText original_source)
        {
            this.modified_source = this.original_source = original_source;
            chunks.Add(new OriginalTextChunk());
            chunks[0].modified_span = chunks[0].original_span = new TextSpan(0, original_string.Length);
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
            Debug.Assert(!what.IsEmpty);
            Debug.Assert(original_span.Contains(what));
            Debug.Assert(!string.IsNullOrWhiteSpace(with)); // support if needed
            if(what.IsEmpty || !original_span.Contains(what))
                return null;

            // find chunks
            var (begin_chunk_index, begin_index_within_chunk) = FindOriginalChunkIndex(what.Start).Value;
            var (end_chunk_index, end_index_within_chunk) = FindOriginalChunkIndex(what.End - 1).Value;
            Debug.Assert(begin_chunk_index == end_chunk_index); // support more if needed
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
                modified_span = new(0, what.Length)
            });
            if(end_index_within_chunk < end_chunk.original_end - 1)
            {
                replacement_chunks.Add(new OriginalTextChunk() {
                    original_span = TextSpan.FromBounds(end_chunk.original_start + end_index_within_chunk, end_chunk.original_end),
                    modified_span = new(0, end_chunk.modified_length - end_index_within_chunk)
                });
            }

            // modify chunk list and text buffer
            chunks.Replace(begin_chunk_index, end_chunk_index - begin_chunk_index + 1, replacement_chunks);
            modified_source = modified_source.WithChanges(new TextChange(
                TextSpan.FromBounds(begin_chunk.modified_start + begin_index_within_chunk,
                    end_chunk.modified_start + end_index_within_chunk), with)); // check this!!

            // update offsets in modified chunks
            int next = begin_chunk.modified_start;  // this one is still ok
            for (int i = begin_chunk_index; i < chunks.Count; i++)
            {
                int nextnext = next + chunks[i].modified_length;
                chunks[i].modified_span = TextSpan.FromBounds(next, nextnext);
                next = nextnext;
            }
            return ret;
        }
    }
}
