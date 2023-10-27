using EvolveUI.Compiler;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    public static class MathUtil {

        public static int Min(int a, int b) {
            return a < b ? a : b;
        }

        public static int Max(int a, int b) {
            return a > b ? a : b;
        }

    }

    internal struct TemplateParseResult {

#if UNITY_64
        public HardErrorInfo[] errors;
#else
        public ParseError[] errors;
        public Token[] nonTrivialTokens;
#endif

        public NodeIndex[] templateRanges;
        public ExpressionIndex[] expressionRanges;
        public UntypedTemplateNode[] templateNodes;
        public UntypedExpressionNode[] expressionNodes;
        public TopLevelDeclaration[] topLevelDeclarations;

    }

    internal unsafe ref partial struct TemplateParser {

        private ScopedTempAllocator scopedAllocator;

        private int lastHandledError;
        private MemoryPool memoryPool;

        private PodStack<RecoveryPoint> recoveryStack;
        private PodStack<HardErrorInfo> hardErrorStack;
        private PodList<TopLevelDeclaration> declarationList;

        private ExpressionBuffer expressionBuffer;
        private TemplateNodeBuffer templateBuffer;

        internal bool* panic;
        private int tokenCount;

        public static bool TryParseTemplate(string filePath, string source, out TemplateParseResult result) {
            result = default;

            using PodList<Token> tokens = new PodList<Token>(1024);

            fixed (char* pSource = source) {
                FixedCharacterSpan sourceSpan = new FixedCharacterSpan(pSource, source.Length);

                if (!Tokenizer.TryTokenize(sourceSpan, &tokens, out TokenizerResult tokenizerResult)) {
                    return false;
                }

                return TryParseTemplate(filePath, sourceSpan, tokens.ToCheckedArray(), out result);
            }
        }

        public static bool TryParseTemplate(string filePath, FixedCharacterSpan source, CheckedArray<Token> tokens, out TemplateParseResult result) {
            using TemplateParser parser = Create();
            using PodList<Token> nonTrivialTokens = Token.GatherNonTrivialTokens(tokens);
            bool valid = parser.TryParseTemplate(nonTrivialTokens.ToCheckedArray(), source);
            result = parser.GetParseResult(filePath, source, tokens, nonTrivialTokens);
            return valid;
        }

        private TemplateParseResult GetParseResult(string filePath, FixedCharacterSpan source, CheckedArray<Token> tokens, PodList<Token> nonTrivialTokens) {
            TemplateParseResult result = new TemplateParseResult();

            if (hardErrorStack.Count == 0) {
                result.topLevelDeclarations = declarationList.ToArray();
                result.templateRanges = templateBuffer.rangeBuffer.ToArray();
                result.templateNodes = templateBuffer.nodes.ToArray();
                result.expressionRanges = expressionBuffer.rangeBuffer.ToArray();
                result.expressionNodes = expressionBuffer.expressions.ToArray();
                result.nonTrivialTokens = nonTrivialTokens.ToArray();
                result.errors = Array.Empty<ParseError>();
                return result;
            }

            result.topLevelDeclarations = Array.Empty<TopLevelDeclaration>();
            result.templateRanges = Array.Empty<NodeIndex>();
            result.templateNodes = Array.Empty<UntypedTemplateNode>();
            result.expressionRanges = Array.Empty<ExpressionIndex>();
            result.expressionNodes = Array.Empty<UntypedExpressionNode>();
            result.nonTrivialTokens = nonTrivialTokens.ToArray();
            result.errors = new ParseError[hardErrorStack.Count];

            fixed (char* pFilePath = filePath) {
                FixedCharacterSpan fileSpan = new FixedCharacterSpan(pFilePath, filePath.Length);

                for (int i = 0; i < hardErrorStack.Count; i++) {
                    HardErrorInfo hardError = hardErrorStack.Get(i);
                    Diagnostics.ErrorInfo errorInfo = new Diagnostics.ErrorInfo() {
                        details = hardError.details,
                        tokenRange = new NonTrivialTokenRange(hardError.location, hardError.location + 1),
                        filePath = fileSpan,
                        tokens = tokens,
                        nonTrivialTokens = nonTrivialTokens.ToCheckedArray(),
                        errorType = hardError.error,
                        fileSource = source
                    };

                    int location = errorInfo.nonTrivialTokens[errorInfo.tokenRange.start.index].tokenIndex;

                    result.errors[i] = new ParseError() {
                        message = Diagnostics.Implementation.BuildErrorString(errorInfo),
                        line = tokens[location].lineIndex,
                        column = tokens[location].columnIndex
                    };
                }
            }

            return result;
        }

        public static TemplateParser Create() {
            MemoryPool pool = new MemoryPool(TypedUnsafe.Kilobytes(16));
            // todo -- dumb, need to pass a pointer to tokenstreams, find another way 
            bool* panic = Native.Memory.AlignedMalloc<bool>(1);
            *panic = false;
            return new TemplateParser() {
                panic = panic,
                memoryPool = pool,
                lastHandledError = 0,
                scopedAllocator = new ScopedTempAllocator(pool),
                recoveryStack = new PodStack<RecoveryPoint>(8),
                hardErrorStack = new PodStack<HardErrorInfo>(8),
                expressionBuffer = new ExpressionBuffer(32, 1024),
                templateBuffer = new TemplateNodeBuffer(32, 1024),
                declarationList = new PodList<TopLevelDeclaration>(16),
            };
        }

        private bool HasCriticalError => lastHandledError != hardErrorStack.Count;

        public void Dispose() {
            Native.Memory.AlignedFree(panic, Native.Memory.AlignOf<bool>());
            memoryPool.Dispose();
            recoveryStack.Dispose();
            hardErrorStack.Dispose();
            expressionBuffer.Dispose();
            templateBuffer.Dispose();
            declarationList.Dispose();
            scopedAllocator.Dispose();
            this = default;
        }

        private void Clear(ref TokenStream tokenStream) {
            tokenCount = tokenStream.GetTokens().size;
            *panic = false;
            lastHandledError = 0;
            recoveryStack.Clear();
            hardErrorStack.Clear();
            expressionBuffer.Clear();
            templateBuffer.Clear();
            declarationList.Clear();
            scopedAllocator.Clear();
        }

        public bool ParseCSStatements(ref TokenStream tokenStream, out ExpressionRange expressionRange) {
            Clear(ref tokenStream);
            return ParseStatementListWithHardErrors(ref tokenStream, default, out expressionRange);
        }

        public bool ParseCSExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            Clear(ref tokenStream);
            bool retn = ParseExpression(ref tokenStream, out expressionIndex);
            // Debug.Log("TOTAL EXPRESSIONS: " + expressionBuffer.expressions.size);
            // Debug.Log("TOTAL RANGE SIZE: " + expressionBuffer.rangeBuffer.size);
            return retn;
        }

        public void GetTemplateBufferCounts(out int nodeCount, out int rangeCount) {
            nodeCount = templateBuffer.nodes.size;
            rangeCount = templateBuffer.rangeBuffer.size;
        }

        public void CopyTemplateNodes(PodList<UntypedTemplateNode>* nodes, PodList<NodeIndex>* ranges) {
            nodes->AddRange(templateBuffer.nodes.GetArrayPointer(), templateBuffer.nodes.size);
            ranges->AddRange(templateBuffer.rangeBuffer.GetArrayPointer(), templateBuffer.rangeBuffer.size);
        }

        public void GetExpressionBufferCounts(out int expressionCount, out int rangeCount) {
            expressionCount = expressionBuffer.expressions.size;
            rangeCount = expressionBuffer.rangeBuffer.size;
        }

        public void CopyExpressionNodes(PodList<UntypedExpressionNode>* nodes, PodList<ExpressionIndex>* ranges) {
            nodes->AddRange(expressionBuffer.expressions.GetArrayPointer(), expressionBuffer.expressions.size);
            ranges->AddRange(expressionBuffer.rangeBuffer.GetArrayPointer(), expressionBuffer.rangeBuffer.size);
        }

        public bool TryParseTemplate(CheckedArray<Token> nonTrivialTokens, FixedCharacterSpan source) {
#if EVOLVE_UI_DEV
            Token.SetDebugSources(nonTrivialTokens, source);
#endif
            scopedAllocator.AssertEmpty();

            TokenStream tokenStream = new TokenStream(nonTrivialTokens, source, panic);
            Clear(ref tokenStream);
            bool retn = TryParseTemplateFile(ref tokenStream);

#if EVOLVE_UI_DEV
            Token.UnsetDebugSources(nonTrivialTokens);
#endif
            return retn;
        }

        private bool TryParseTemplateFile(ref TokenStream tokenStream) {
            while (tokenStream.HasMoreTokens && !HasCriticalError) {
                int nodeStart = templateBuffer.nodes.size;
                int expressionStart = expressionBuffer.expressions.size;

                if (ParseDecorators(ref tokenStream, out NodeRange<DecoratorNode> decorators)) {
                    if (ParseTemplateDeclaration(ref tokenStream, nodeStart, expressionStart, decorators, out TemplateDeclaration templateDeclaration)) {
                        declarationList.Add(new TopLevelDeclaration() {
                            type = DeclarationType.Template,
                            templateDeclaration = templateDeclaration
                        });
                        continue;
                    }

                    if (ParseTypographyDeclaration(ref tokenStream, nodeStart, expressionStart, decorators, out TypographyDeclaration typographyDeclaration)) {
                        declarationList.Add(new TopLevelDeclaration() {
                            type = DeclarationType.Typography,
                            typographyDeclaration = typographyDeclaration,
                        });
                        continue;
                    }

                    HardError(tokenStream, DiagnosticError.UnexpectedDecorator, HelpType.TopLevelDeclarationSyntax);
                    return false;
                }

                Token token = tokenStream.Current;

                switch (token.Keyword) {
                    case TemplateKeyword.Import: {
                        if (ParseImportDeclaration(ref tokenStream, out ImportDeclaration importDecl)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.Import,
                                importDeclaration = importDecl
                            });
                            continue;
                        }

                        break;
                    }

                    case TemplateKeyword.Using: {
                        if (ParseUsingDeclaration(ref tokenStream, out UsingDeclaration usingDeclaration)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.Using,
                                usingDeclaration = usingDeclaration
                            });
                            continue;
                        }

                        break;
                    }

                    case TemplateKeyword.Decorator: {
                        if (ParseDecoratorDeclaration(ref tokenStream, out DecoratorDeclaration decoratorDeclaration)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.Decorator,
                                decoratorDeclaration = decoratorDeclaration
                            });
                            continue;
                        }

                        HardError(tokenStream, DiagnosticError.UnableToParseDecorator, HelpType.DecoratorDeclarationSyntax);

                        break;
                    }

                    case TemplateKeyword.Template: {
                        if (ParseTemplateDeclaration(ref tokenStream, 0, 0, default, out TemplateDeclaration templateDeclaration)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.Template,
                                templateDeclaration = templateDeclaration
                            });
                            continue;
                        }

                        break;
                    }

                    case TemplateKeyword.Typography: {
                        if (ParseTypographyDeclaration(ref tokenStream, nodeStart, expressionStart, decorators, out TypographyDeclaration typographyDeclaration)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.Typography,
                                typographyDeclaration = typographyDeclaration,
                            });
                            continue;
                        }

                        break;
                    }

                    case TemplateKeyword.Function: {
                        if (ParseFunctionDeclaration(ref tokenStream, out TemplateFunctionDeclaration rootFunctionDeclaration)) {
                            declarationList.Add(new TopLevelDeclaration() {
                                type = DeclarationType.RootFunction,
                                templateFunctionDeclaration = rootFunctionDeclaration,
                            });
                            continue;
                        }

                        break;
                    }

                    default: {
                        // unexpected, cannot recover
                        HardError(tokenStream, DiagnosticError.ExpectedTopLevelDeclaration, HelpType.TopLevelDeclarationSyntax);
                        break;
                    }
                }

                break;
            }

            return hardErrorStack.Count == 0;
        }


        public ParseError[] GetParseErrors(string filePath, string source) {
            if (hardErrorStack.Count == 0) {
                return Array.Empty<ParseError>();
            }

            ParseError[] retn = new ParseError[hardErrorStack.Count];
            for (int i = 0; i < hardErrorStack.Count; i++) {
                ParseError error = new ParseError();
                HardErrorInfo hardError = hardErrorStack.Get(i);
                Diagnostics.ErrorInfo errorInfo = new Diagnostics.ErrorInfo() {
                    details = hardError.details,
                    // filePath = filePath, 
                };
                // error.message = Diagnostics.BuildErrorString
                retn[i] = error;
            }

            return retn;
        }

#if UNITY_64
        public TemplateParseResult GetParseResult() {
            TemplateParseResult templateParseResult = new TemplateParseResult();

            if (hardErrorStack.Count == 0) {
                templateParseResult.topLevelDeclarations = declarationList.ToArray();
                templateParseResult.templateRanges = templateBuffer.rangeBuffer.ToArray();
                templateParseResult.templateNodes = templateBuffer.nodes.ToArray();
                templateParseResult.expressionRanges = expressionBuffer.rangeBuffer.ToArray();
                templateParseResult.expressionNodes = expressionBuffer.expressions.ToArray();

                return templateParseResult;
            }

            templateParseResult.errors = hardErrorStack.ToCheckedArray().ToArray();
            return templateParseResult;
        }

#endif
        private bool ParseUsingDeclaration(ref TokenStream tokenStream, out UsingDeclaration usingDeclaration) {
            // : using type_path ';'

            usingDeclaration = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Using)) {
                return false;
            }

            if (!ParseNamespaceOrTypeName(ref tokenStream, out usingDeclaration.typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedNamespaceName, HelpType.UsingDeclarationSyntax);
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.UsingDeclarationSyntax);
            }

            usingDeclaration.tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
            return true;
        }

        private bool ParseImportDeclaration(ref TokenStream tokenStream, out ImportDeclaration importDeclaration) {
            // : 'import' dashed_identifier (as identifier)? ';'

            importDeclaration = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Import)) {
                return false;
            }

            if (!ParseDashedIdentifier(ref tokenStream, out NonTrivialTokenRange importName)) {
                return HardError(tokenStream, DiagnosticError.ExpectedDashedIdentifier, HelpType.ImportSyntax);
            }

            importDeclaration.importString = importName;

            if (!tokenStream.Consume(TemplateKeyword.As)) {
                if (!tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedSemiColonOrAliasExpression, HelpType.ImportSyntax);
                }

                importDeclaration.tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
                return true;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out importDeclaration.aliasName)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColonOrAliasExpression, HelpType.ImportSyntax);
            }

            importDeclaration.tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.ImportSyntax);
            }

            return true;
        }

        private bool ParseDottedIdentifier(ref TokenStream tokenStream, out NonTrivialTokenRange tokenRange) {
            // : identifier ('.' identifier)*
            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeKeywordOrIdentifier(out Token last)) {
                tokenRange = default;
                return false;
            }

            while (tokenStream.Consume(SymbolType.Dot, out Token token)) {
                // require the tokens not to be separated by whitespace, comments, or new lines
                if (last.tokenIndex + 1 != token.tokenIndex) {
                    tokenRange = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedDottedIdentifierNotToContainWhitespace);
                }

                if (!tokenStream.ConsumeKeywordOrIdentifier(out last)) {
                    tokenRange = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedDottedIdentifierNotToEndWithDot);
                }
            }

            tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
            return true;
        }

        private bool ParseDashedIdentifier(ref TokenStream tokenStream, out NonTrivialTokenRange tokenRange) {
            // : identifier (- identifier)*

            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeKeywordOrIdentifier(out Token last)) {
                tokenRange = default;
                return false;
            }

            while (tokenStream.Consume(SymbolType.Minus, out Token token)) {
                // require the tokens not to be separated by whitespace, comments, or new lines
                if (last.tokenIndex + 1 != token.tokenIndex) {
                    tokenRange = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedDashedIdentifierNotToContainWhitespace);
                }

                if (!tokenStream.ConsumeKeywordOrIdentifier(out last)) {
                    TokenType tokenType = tokenStream.Current.tokenType;

                    if (tokenType == TokenType.NumericLiteral) {
                        LiteralType literalType = tokenStream.Current.extra.literalType;
                        if (literalType == LiteralType.Integer) {
                            tokenStream.location++;
                        }
                    }
                    else {
                        tokenRange = default;
                        return HardError(tokenStream, DiagnosticError.ExpectedDashedIdentifierNotToEndWithMinus);
                    }
                }
            }

            tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
            return true;
        }

        internal enum ErrorLocationAdjustment {

            None,
            TokenEnd,

        }

        private bool HardError(int location, DiagnosticError error, HelpType help = default) {
            return HardError(location, ErrorLocationAdjustment.None, error, help);
        }

        private bool SoftError(int location, ErrorLocationAdjustment adjustment, DiagnosticError error, HelpType help = default) {
            if (*panic) {
                return false;
            }

            if (location >= tokenCount) {
                location--;
                adjustment = ErrorLocationAdjustment.TokenEnd;
            }

            hardErrorStack.Push(new HardErrorInfo() {
                error = error,
                location = location,
                details = new DiagnosticDetails() {
                    helpType = help
                },
                adjustment = adjustment
            });

            return false;
        }

        private bool HardError(int location, ErrorLocationAdjustment adjustment, DiagnosticError error, HelpType help = default) {
            if (*panic) {
                return false;
            }

            if (location >= tokenCount) {
                location--;
                adjustment = ErrorLocationAdjustment.TokenEnd;
            }

            hardErrorStack.Push(new HardErrorInfo() {
                error = error,
                location = location,
                details = new DiagnosticDetails() {
                    helpType = help
                },
                adjustment = adjustment
            });

            *panic = true;
            return false;
        }

        private bool HardError(TokenStream tokenStream, DiagnosticError error, HelpType help = default) {
            return HardError(tokenStream.location, error, help);
        }

        private struct RecoveryPoint {

            public int lastErrorId;
            public int recoveryStart;
            public int recoveryEnd;

            public const bool k_HasErrors = true;
            public const bool k_NoErrors = false;

        }

        private void PushRecoveryPoint(int startLocation, int endLocation) {
            recoveryStack.Push(new RecoveryPoint() {
                recoveryStart = startLocation,
                recoveryEnd = endLocation,
                lastErrorId = hardErrorStack.Count
            });
        }

        // returns whether or not we have new errors introduced in this scope
        private bool PopRecoveryPoint(ref TokenStream tokenStream) {
            RecoveryPoint recovery = recoveryStack.Pop();

            if (hardErrorStack.Count == recovery.lastErrorId) {
                if (tokenStream.location + 1 != recovery.recoveryEnd) {
                    // didn't process everything
                    hardErrorStack.Push(new HardErrorInfo() {
                        error = DiagnosticError.StuckOnUnexpectedToken,
                        location = tokenStream.location,
                        lastValidLocation = recovery.recoveryStart
                    });
                    return true;
                }

                return false; // no new errors, all good
            }

            // new errors, remove all except the last one since they are probably bogus 
            // not sure we actually want that, will need to see whats up with that 
            // hardErrorStack.SetSize(recovery.lastErrorId + 1);
            hardErrorStack.Peek().lastValidLocation = recovery.recoveryStart;
            tokenStream.location = recovery.recoveryEnd;
            *panic = false; // can we do this here? might need to inspect the prev recovery point's panic setting
            return true;
        }

        // not used for expressions! Because in that case a < might be legit 
        private bool ParseTemplateTypeParameterListWithRecovery(ref TokenStream tokenStream, out ExpressionRange<Identifier> parameters) {
            // : '<' type_parameter (',' type_parameter) * '>'

            parameters = default;

            if (tokenStream.Peek(SymbolType.LessThan)) {
                if (!tokenStream.TryGetSubStream(SubStreamType.AngleBrackets, out TokenStream argStream)) {
                    return HardError(tokenStream, DiagnosticError.InvalidGenericArgumentList);
                }

                PushRecoveryPoint(argStream.location, tokenStream.location);

                ParseTypeParameterList(ref argStream, out parameters);

                return !PopRecoveryPoint(ref argStream);
            }

            return true;
        }

        private bool ParseTypeParameter(ref TokenStream tokenStream, out ExpressionIndex<Identifier> typeParameter) {
            // : attributes? identifier
            // not supporting attributes yet
            return ParseIdentifier(ref tokenStream, out typeParameter);
        }

        private bool ParseTypeParameterList(ref TokenStream tokenStream, out ExpressionRange<Identifier> expressionRange) {
            expressionRange = default;

            if (!ParseTypeParameter(ref tokenStream, out ExpressionIndex<Identifier> typeParameter)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier);
            }

            using ScopedList<ExpressionIndex<Identifier>> list = scopedAllocator.CreateListScope<ExpressionIndex<Identifier>>(8);
            list.Add(typeParameter);
            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseTypeParameter(ref tokenStream, out ExpressionIndex<Identifier> next)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier);
                }

                list.Add(next);
            }

            expressionRange = expressionBuffer.AddExpressionList(list);
            return true;
        }

        public bool GetHardErrors(List<HardErrorInfo> errorInfos) {
            if (hardErrorStack.Count == 0) return false;
            for (int i = 0; i < hardErrorStack.Count; i++) {
                errorInfos.Add(hardErrorStack.Get(i));
            }

            return true;
        }

    }

    public struct ParseError {

        public string message;
        public int line;
        public int column;

    }

    internal struct HardErrorInfo {

        public int location;
        public DiagnosticError error;
        public DiagnosticDetails details;
        public int lastValidLocation;
        public TemplateParser.ErrorLocationAdjustment adjustment;

    }

}