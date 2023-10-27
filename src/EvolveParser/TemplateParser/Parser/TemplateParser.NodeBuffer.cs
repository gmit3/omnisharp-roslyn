using System;
using System.Diagnostics;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal partial struct TemplateParser {

        internal unsafe struct TemplateNodeBuffer : IDisposable {

            internal PodList<UntypedTemplateNode> nodes;
            internal PodList<NodeIndex> rangeBuffer;
            private PodStack<BufferScope> scopes;

            public TemplateNodeBuffer(int scopeCount, int expressionCapacity) {
                this.scopes = new PodStack<BufferScope>(scopeCount);
                this.nodes = new PodList<UntypedTemplateNode>(expressionCapacity);
                this.rangeBuffer = new PodList<NodeIndex>(expressionCapacity);
                this.nodes.size = 1;
                this.rangeBuffer.size = 1;
            }

            public void Dispose() {
                scopes.Dispose();
                nodes.Dispose();
                rangeBuffer.Dispose();
            }

            public void Clear() {
                nodes.size = 1;
                rangeBuffer.size = 1;
                scopes.Clear();
            }

            [DebuggerStepThrough]
            public NodeIndex<T> Add<T>(int start, int end, in T node) where T : unmanaged, ITemplateNode {
                return Add(node, new NonTrivialTokenRange(start, end));
            }

            [DebuggerStepThrough]
            private NodeIndex<T> Add<T>(T node, NonTrivialTokenRange tokenRange) where T : unmanaged, ITemplateNode {

                UntypedTemplateNode untypedNode = *(UntypedTemplateNode*)&node;
                untypedNode.meta = new TemplateNodeMeta() {
                    nodeType = node.NodeType,
                    nodeIndexShort = (ushort)(nodes.size),
                    tokenRange = tokenRange,
                };
                nodes.Add(untypedNode);
                return new NodeIndex<T>(nodes.size - 1);
            }

            public NodeRange AddNodeList(ScopedList<NodeIndex> buffer) {
                NodeRange range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = (ushort)buffer.size;
                rangeBuffer.AddRange(buffer.array, buffer.size);
                return range;
            }

            public NodeRange<T> AddNodeList<T>(ScopedList<NodeIndex<T>> buffer) where T : unmanaged, ITemplateNode {
                NodeRange<T> range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = (ushort)buffer.size;
                rangeBuffer.AddRange((NodeIndex*)buffer.array, buffer.size);
                return range;
            }

        }

    }

}