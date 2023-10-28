using System.Diagnostics;
using System.Runtime.InteropServices;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {
    public unsafe struct SerializedTemplateFile {

        public OffsetRange<long> lastWriteTime;
        public OffsetRange<bool> hasErrors;
        public OffsetRange<char> filePath;
        public OffsetRange<char> fileSource;
        public OffsetRange<Token> tokens;
        public OffsetRange<Token> nonTrivialTokens;
        public OffsetRange<TopLevelDeclaration> topLevelDeclarations;
        public OffsetRange<UntypedTemplateNode> untypedTemplateNodes;
        public OffsetRange<NodeIndex> templateRanges;
        public OffsetRange<UntypedExpressionNode> untypedExpressionNodes;
        public OffsetRange<ExpressionIndex> expressionRanges;

//         public static bool TryDeserialize(ref SerializedTemplateFile serialized, CheckedArray<byte> bytes, PerThreadAllocator* allocator, out TemplateFile templateFile) {
//             CheckedArray<long> writeTime = serialized.lastWriteTime.FromBytes(bytes);
//             CheckedArray<bool> hasErrors = serialized.hasErrors.FromBytes(bytes);
//
//             FixedCharacterSpan filePath = serialized.filePath.FromBytes(bytes);
//             FixedCharacterSpan fileSource = serialized.fileSource.FromBytes(bytes);
//
//             CheckedArray<Token> tokens = serialized.tokens.FromBytes(bytes);
//             CheckedArray<Token> nonTrivialTokens = serialized.nonTrivialTokens.FromBytes(bytes);
//             CheckedArray<TopLevelDeclaration> topLevelDeclarations = serialized.topLevelDeclarations.FromBytes(bytes);
//             CheckedArray<UntypedTemplateNode> untypedTemplateNodes = serialized.untypedTemplateNodes.FromBytes(bytes);
//             CheckedArray<NodeIndex> templateRanges = serialized.templateRanges.FromBytes(bytes);
//             CheckedArray<UntypedExpressionNode> untypedExpressionNodes = serialized.untypedExpressionNodes.FromBytes(bytes);
//             CheckedArray<ExpressionIndex> expressionRanges = serialized.expressionRanges.FromBytes(bytes);
//
//             templateFile = new TemplateFile() {
//                 filePath = filePath,
//                 fileSource = fileSource,
//                 lastWriteTime = writeTime.Get(0),
//                 hasErrors = hasErrors.Get(0),
//                 topLevelDeclarations = topLevelDeclarations,
//                 tokens = tokens,
//                 nonTrivialTokens = nonTrivialTokens,
//                 expressionTree = allocator->Allocate<ExpressionTree>(1),
//                 templateTree = allocator->Allocate<TemplateTree>(1),
//
//                 // set later
//                 moduleInfo = default,
//                 namespaces = default,
//                 moduleImports = default,
//                 parsingErrors = default,
//             };
//
//             templateFile.expressionTree->source = fileSource;
//             templateFile.expressionTree->nonTrivialTokens = nonTrivialTokens;
//             templateFile.expressionTree->ranges = expressionRanges;
//             templateFile.expressionTree->untypedNodes = untypedExpressionNodes;
//
//             templateFile.templateTree->nonTrivialTokens = nonTrivialTokens;
//             templateFile.templateTree->ranges = templateRanges;
//             templateFile.templateTree->untypedNodes = untypedTemplateNodes;
//             templateFile.templateTree->source = fileSource;
//             templateFile.templateTree->expressionTree = templateFile.expressionTree;
//
// #if EVOLVE_UI_DEV
//             Token.SetDebugSources(templateFile.nonTrivialTokens, templateFile.fileSource);
// #endif
//             return !templateFile.hasErrors;
//         }
//

    }

    internal struct TemplateCacheHeader {

        public int version;
        public int templateCount;

    }

    public unsafe partial struct TemplateFile : IDisposable {

        // public ModuleInfo* moduleInfo;
        public FixedCharacterSpan filePath;
        public FixedCharacterSpan fileSource;
        public CheckedArray<Token> tokens;
        public CheckedArray<Token> nonTrivialTokens; // not serialized, can be recomputed

        public CheckedArray<HardErrorInfo> parsingErrors; // not serialized

        public CheckedArray<TopLevelDeclaration> topLevelDeclarations;
        public CheckedArray<FixedCharacterSpan> namespaces;
        // public CheckedArray<ModuleImport> moduleImports; // not serialized 

        public TemplateTree* templateTree;
        public ExpressionTree* expressionTree;
        public long lastWriteTime;
        public bool hasErrors;

        public bool RequiresParsing => tokens.size == 0;

        public static string GetCacheDirectory(string tempPath, string moduleName) {
            return Path.Join(tempPath, moduleName);
        }

        public static string GetCachePath(string tempPath, string moduleName) {
            return Path.Join(tempPath, moduleName, "templates.uicache");
        }

        public bool TryGetDeclaration(DeclarationType declarationType, FixedCharacterSpan tagName, out int declarationIndex) {
            for (int tld = 0; tld < topLevelDeclarations.size; tld++) {
                TopLevelDeclaration topLevel = topLevelDeclarations.Get(tld);
                if (topLevel.type == declarationType && GetTokenSource(topLevel.templateDeclaration.identifierLocation) == tagName) {
                    declarationIndex = tld;
                    return true;
                }
            }

            declarationIndex = -1;
            return false;
        }

        public SerializedTemplateFile Serialize(PodList<byte>* bytes) {
            long writeTime = lastWriteTime;
            bool errors = hasErrors || parsingErrors.size > 0;

            return new SerializedTemplateFile() {
                lastWriteTime = OffsetRange<long>.Create(bytes, new CheckedArray<long>(&writeTime, 1)),
                hasErrors = OffsetRange<bool>.Create(bytes, new CheckedArray<bool>(&errors, 1)),

                filePath = OffsetRange<char>.Create(bytes, filePath.ToCheckedArray()),
                fileSource = OffsetRange<char>.Create(bytes, fileSource.ToCheckedArray()),
                tokens = OffsetRange<Token>.Create(bytes, tokens),
                nonTrivialTokens = OffsetRange<Token>.Create(bytes, nonTrivialTokens),

                templateRanges = OffsetRange<NodeIndex>.Create(bytes, templateTree == null ? default : templateTree->ranges),
                untypedTemplateNodes = OffsetRange<UntypedTemplateNode>.Create(bytes, templateTree == null ? default : templateTree->untypedNodes),
                expressionRanges = OffsetRange<ExpressionIndex>.Create(bytes, expressionTree == null ? default : expressionTree->ranges),
                untypedExpressionNodes = OffsetRange<UntypedExpressionNode>.Create(bytes, expressionTree == null ? default : expressionTree->untypedNodes),

                topLevelDeclarations = OffsetRange<TopLevelDeclaration>.Create(bytes, topLevelDeclarations),
            };
        }

        public void Dispose() {
            this = default;
        }

        [DebuggerStepThrough]
        public UntypedExpressionNode Get(ExpressionRangeIndex index) {
            return expressionTree->Get(index);
        }

        [DebuggerStepThrough]
        public T Get<T>(ExpressionRangeIndex<T> index) where T : unmanaged, IExpressionNode {
            return expressionTree->Get(index);
        }

        [DebuggerStepThrough]
        public UntypedExpressionNode Get(ExpressionIndex index) {
            return expressionTree->Get(index);
        }

        [DebuggerStepThrough]
        public T Get<T>(ExpressionIndex<T> index) where T : unmanaged, IExpressionNode {
            return expressionTree->Get(index);
        }

        [DebuggerStepThrough]
        public UntypedTemplateNode Get(NodeRangeIndex index) {
            return templateTree->Get(index);
        }

        // [DebuggerStepThrough]
        public T Get<T>(NodeRangeIndex<T> index) where T : unmanaged, ITemplateNode {
            return templateTree->Get(index);
        }

        [DebuggerStepThrough]
        public UntypedTemplateNode Get(NodeIndex index) {
            return templateTree->Get(index);
        }

        [DebuggerStepThrough]
        public T Get<T>(NodeIndex<T> index) where T : unmanaged, ITemplateNode {
            return templateTree->Get(index);
        }

        public TemplateFunctionDeclaration GetRootFunctionDeclaration(int declIndex) {
            if (declIndex < 0 || declIndex > topLevelDeclarations.size) {
                return default;
            }

            TopLevelDeclaration decl = topLevelDeclarations[declIndex];
            return decl.type != DeclarationType.RootFunction ? default : decl.templateFunctionDeclaration;
        }

        public DecoratorDeclaration GetDecoratorDeclaration(int declIndex) {
            if (declIndex < 0 || declIndex > topLevelDeclarations.size) {
                return default;
            }

            TopLevelDeclaration decl = topLevelDeclarations[declIndex];
            return decl.type != DeclarationType.Decorator ? default : decl.decoratorDeclaration;
        }

        public TemplateDeclaration GetTemplateDeclaration(int declIndex) {
            if (declIndex < 0 || declIndex > topLevelDeclarations.size) {
                return default;
            }

            TopLevelDeclaration decl = topLevelDeclarations[declIndex];
            return decl.type != DeclarationType.Template ? default : decl.templateDeclaration;
        }

        public TypographyDeclaration GetTypographyDeclaration(int declIndex) {
            if (declIndex < 0 || declIndex > topLevelDeclarations.size) {
                return default;
            }

            TopLevelDeclaration decl = topLevelDeclarations[declIndex];
            return decl.type != DeclarationType.Typography ? default : decl.typographyDeclaration;
        }

        public FixedCharacterSpan GetTokenSource(NonTrivialTokenLocation tokenLocation) {
            if (!tokenLocation.IsValid) return default;
            int start = nonTrivialTokens[tokenLocation.index].charIndex;
            int length = nonTrivialTokens[tokenLocation.index].length;
            TypedUnsafe.CheckRange(start, fileSource.size);
            TypedUnsafe.CheckRange(start + length - 1, fileSource.size);
            return fileSource.Slice(start, length);
        }

        public FixedCharacterSpan GetTokenSource(NonTrivialTokenRange tokenRange) {
            if (!tokenRange.IsValid) {
                return default;
            }

            int start = nonTrivialTokens[tokenRange.start.index].charIndex;
            int end = nonTrivialTokens[tokenRange.end.index - 1].charIndex;
            end += nonTrivialTokens[tokenRange.end.index - 1].length;
            int length = end - start;
            TypedUnsafe.CheckRange(start, fileSource.size);
            TypedUnsafe.CheckRange(start + length - 1, fileSource.size);
            return new FixedCharacterSpan(fileSource.GetPointer(start), length);
        }

    }

    public enum DeclarationType {

        Invalid,
        Using,
        Import,
        Template,
        Typography,
        RootFunction,

        Decorator

    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TopLevelDeclaration {

        [FieldOffset(0)] public DeclarationType type;

        [FieldOffset(4)] public UsingDeclaration usingDeclaration;
        [FieldOffset(4)] public ImportDeclaration importDeclaration;
        [FieldOffset(4)] public TemplateDeclaration templateDeclaration;
        [FieldOffset(4)] public TypographyDeclaration typographyDeclaration;
        [FieldOffset(4)] public TemplateFunctionDeclaration templateFunctionDeclaration;
        [FieldOffset(4)] public DecoratorDeclaration decoratorDeclaration;
        [FieldOffset(4)] public VariantDeclaration variantDeclaration;

    }

    public struct DecoratorDeclaration {

        public NonTrivialTokenRange tokenRange;
        public RangeInt templateRange;
        public RangeInt expressionRange;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionRange<Identifier> typeParameters;
        public NodeIndex<TemplateSignatureNode> signature;
        public bool isTypography;
        public bool isMarkerOnly;
        public NodeIndex<TemplateSpawnList> spawnList;

    }

    public struct TemplateDeclaration {

        public NodeRange<DecoratorNode> decorators;
        public ExpressionRange<Identifier> typeParameters;
        public NodeIndex<TemplateSignatureNode> signature;
        public NodeIndex<TemplateBlockNode> body;
        public NonTrivialTokenLocation identifierLocation;
        public NonTrivialTokenRange tokenRange;
        public RangeInt templateRange;
        public RangeInt expressionRange;
        public NodeIndex<TemplateSpawnList> spawnList;

    }

    public struct VariantDeclaration {

        public NonTrivialTokenLocation identifierLocation;
        public RangeInt templateRange;
        public RangeInt expressionRange;
        public ExpressionRange<Identifier> typeParameters;
        public NodeIndex<TemplateBlockNode> body;
        public NonTrivialTokenRange tokenRange;
        public NodeIndex<TemplateSignatureNode> signature;

    }

    public struct TemplateFunctionDeclaration {

        public NonTrivialTokenLocation identifierLocation;
        public RangeInt templateRange;
        public RangeInt expressionRange;
        public ExpressionRange<Identifier> typeParameters;
        public NodeIndex<TemplateBlockNode> body;
        public NonTrivialTokenRange tokenRange;
        public NodeIndex<TemplateSignatureNode> signature;
        public NodeIndex<TemplateSpawnList> spawnList;

    }

    public struct TypographyDeclaration {

        public NonTrivialTokenLocation identifierLocation;
        public NodeRange<DecoratorNode> decorators;
        public NodeIndex<TemplateSignatureNode> signature;
        public NonTrivialTokenRange tokenRange;
        public RangeInt templateRange;
        public RangeInt expressionRange;
        public ExpressionRange<Identifier> typeParameters;
        public NodeIndex<TemplateSpawnList> spawnList;

    }

    public struct UsingDeclaration {

        public ExpressionIndex<TypeNamePart> typePath;
        public NonTrivialTokenRange tokenRange;

    }

    public struct ImportDeclaration {

        public NonTrivialTokenLocation aliasName;
        public NonTrivialTokenRange importString;
        public NonTrivialTokenRange tokenRange;

    }

}
