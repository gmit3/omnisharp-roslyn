using EvolveUI.Compiler;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal unsafe ref partial struct TemplateParser {

        private struct TypeArgumentSnippet : IExpressionSnippetParser<TypePath> {

            public HelpType SyntaxHelp { get; }
            public bool Optional { get; }

            public TypeArgumentSnippet(HelpType helpData, bool isOptional) {
                SyntaxHelp = helpData;
                Optional = isOptional;
            }

            public bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out ExpressionIndex<TypePath> result) {
                return parser.ParseTypePath(ref tokenStream, out result);
            }

        }

        public bool ParseTypeArgumentList(ref TokenStream tokenStream, out ExpressionRange<TypePath> output) {
            // '<' type_ ( ',' type_)* '>'
            output = default;

            if (tokenStream.Current.Symbol != SymbolType.LessThan) {
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.location++;

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> firstPath)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            using AllocatorScope scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex<TypePath>> typeList = scope.CreateList<ExpressionIndex<TypePath>>(8);
            typeList.Add(firstPath);

            // parse , TypePath until end of stream or false

            while (true) {

                if (tokenStream.Consume(SymbolType.GreaterThan)) {
                    output = expressionBuffer.AddExpressionList(typeList);
                    return true;
                }

                if (!tokenStream.Consume(SymbolType.Comma)) {
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                typeList.Add(typePath);

            }
        }

        public bool ParseBaseType(ref TokenStream tokenStream, out ExpressionIndex<TypeNamePart> typeName) {

            expressionBuffer.PushRuleScope();

            expressionBuffer.MakeRule(ParseSimpleType(tokenStream));
            expressionBuffer.MakeRule(ParseClassTypeRule(tokenStream));
            // expressionBuffer.MakeRule(ParseTupleType(tokenStream));

            if (expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex winningIndex)) {
                typeName = new ExpressionIndex<TypeNamePart>(winningIndex);
                return true;
            }

            typeName = default;
            return false;

        }

        // grammar refers to this as type_
        public bool ParseTypePath(ref TokenStream tokenStream, out ExpressionIndex<TypePath> typePath) {

            // base_type ('?' | rank_specifier | '*')*

            ExpressionParseState state = SaveState(tokenStream);

            if (InvalidTypeKeyword(tokenStream.Current.Keyword)) {
                typePath = default;
                return false;
            }

            if (!ParseBaseType(ref tokenStream, out ExpressionIndex<TypeNamePart> typeHeader)) {
                RestoreState(ref tokenStream, state);
                typePath = default;
                return false;
            }

            if (!(tokenStream.Peek(SymbolType.QuestionMark) || tokenStream.Peek(SymbolType.SquareBraceOpen) || tokenStream.Peek(SymbolType.Multiply))) {
                typePath = expressionBuffer.Add(state.location, tokenStream.location, new TypePath() {
                    baseTypePath = typeHeader,
                    modifiers = default
                });
                return true;
            }

            using AllocatorScope scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex<TypeModifier>> modifierList = scope.CreateList<ExpressionIndex<TypeModifier>>(8);

            while (true) {
                // type? nullable syntax support
                if (tokenStream.Consume(SymbolType.QuestionMark)) {
                    modifierList.Add(expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new TypeModifier() {
                        isNullable = true,
                    }));
                }
                // type[] or type[,] or type[,,,,] support 
                else if (ParseRankSpecifier(ref tokenStream, out ExpressionIndex<TypeModifier> arrayModifier)) {
                    modifierList.Add(arrayModifier);
                }
                // type* pointer support
                else if (tokenStream.Consume(SymbolType.Multiply)) {
                    modifierList.Add(expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new TypeModifier() {
                        isPointer = true,
                    }));
                }
                else {
                    break;
                }

            }

            typePath = expressionBuffer.Add(state.location, tokenStream.location, new TypePath() {
                baseTypePath = typeHeader,
                modifiers = expressionBuffer.AddExpressionList(modifierList),
            });

            return true;
        }

        private bool InvalidTypeKeyword(TemplateKeyword currentKeyword) {

            if (currentKeyword == TemplateKeyword.Invalid) {
                return false;
            }

            return !(currentKeyword == TemplateKeyword.String
                     || currentKeyword == TemplateKeyword.Bool
                     || currentKeyword == TemplateKeyword.Object
                     || currentKeyword == TemplateKeyword.Byte
                     || currentKeyword == TemplateKeyword.Sbyte
                     || currentKeyword == TemplateKeyword.Float
                     || currentKeyword == TemplateKeyword.Double
                     || currentKeyword == TemplateKeyword.Decimal
                     || currentKeyword == TemplateKeyword.Short
                     || currentKeyword == TemplateKeyword.Ushort
                     || currentKeyword == TemplateKeyword.Int
                     || currentKeyword == TemplateKeyword.Uint
                     || currentKeyword == TemplateKeyword.Long
                     || currentKeyword == TemplateKeyword.Ulong
                     || currentKeyword == TemplateKeyword.Char
                     || currentKeyword == TemplateKeyword.Void);
        }

        private bool ParseRankSpecifier(ref TokenStream tokenStream, out ExpressionIndex<TypeModifier> modifier) {

            modifier = default;
            if (tokenStream.Current.Symbol != SymbolType.SquareBraceOpen) {
                return false;
            }

            int startLocation = tokenStream.location;
            tokenStream.location++;

            int rank = 1;

            while (tokenStream.Consume(SymbolType.Comma)) {
                // forever
                rank++;
            }

            if (tokenStream.Current.Symbol != SymbolType.SquareBraceClose) {
                tokenStream.location = startLocation;
                return false;
            }

            tokenStream.location++;

            modifier = expressionBuffer.Add(startLocation, tokenStream.location, new TypeModifier() {
                arrayRank = rank,
            });

            return true;

        }

        public bool ParseClassType(ref TokenStream tokenStream, out ExpressionIndex<TypeNamePart> typeNamePart) {
            Token current = tokenStream.Current;

            // these are separate from simple types so we can parse inheritance better 
            if (current.Keyword == TemplateKeyword.String || current.Keyword == TemplateKeyword.Object) {
                typeNamePart = expressionBuffer.Add(tokenStream.location, tokenStream.location + 1, new TypeNamePart() {
                    simpleTypeName = GetSimpleTypeName(current.Keyword)
                });
                tokenStream.location++;
                return true;
            }

            return ParseNamespaceOrTypeName(ref tokenStream, out typeNamePart);
        }

        // System.Collections.Generic.List<IList<string>>.Enumerator<float>.Value
        // System.Collections.Generic.List<IList<string>>
        // .Enumerator

        public RuleResult<TypeNamePart> ParseClassTypeRule(TokenStream tokenStream) {
            return ParseClassType(ref tokenStream, out ExpressionIndex<TypeNamePart> typeNamePart) ? new RuleResult<TypeNamePart>(tokenStream.location, typeNamePart) : default;
        }

        public RuleResult<TypeNamePart> ParseSimpleType(TokenStream tokenStream) {
            Token current = tokenStream.Current;

            if (current.tokenType != TokenType.KeywordOrIdentifier) {
                return default;
            }

            if (current.Keyword == TemplateKeyword.Bool
                || current.Keyword == TemplateKeyword.Int
                || current.Keyword == TemplateKeyword.Long
                || current.Keyword == TemplateKeyword.Ulong
                || current.Keyword == TemplateKeyword.Float
                || current.Keyword == TemplateKeyword.Double
                || current.Keyword == TemplateKeyword.Byte
                || current.Keyword == TemplateKeyword.Sbyte
                || current.Keyword == TemplateKeyword.Short
                || current.Keyword == TemplateKeyword.Ushort
                || current.Keyword == TemplateKeyword.Char
                // moved string & object to class type 
                // || current.Keyword == TemplateKeyword.String 
                // || current.Keyword == TemplateKeyword.Object
               ) {
                tokenStream.location++;
                return new RuleResult<TypeNamePart>(tokenStream.location, expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new TypeNamePart() {
                    simpleTypeName = GetSimpleTypeName(current.Keyword),
                }));
            }

            return default;
        }

        private static SimpleTypeName GetSimpleTypeName(TemplateKeyword keyword) {
            switch (keyword) {
                case TemplateKeyword.Bool:
                    return SimpleTypeName.Bool;

                case TemplateKeyword.Int:
                    return SimpleTypeName.Int;

                case TemplateKeyword.Long:
                    return SimpleTypeName.Long;

                case TemplateKeyword.Ulong:
                    return SimpleTypeName.Ulong;

                case TemplateKeyword.Float:
                    return SimpleTypeName.Float;

                case TemplateKeyword.Double:
                    return SimpleTypeName.Double;

                case TemplateKeyword.Byte:
                    return SimpleTypeName.Byte;

                case TemplateKeyword.Sbyte:
                    return SimpleTypeName.Sbyte;

                case TemplateKeyword.Short:
                    return SimpleTypeName.Short;

                case TemplateKeyword.Ushort:
                    return SimpleTypeName.Ushort;

                case TemplateKeyword.Char:
                    return SimpleTypeName.Char;

                case TemplateKeyword.String:
                    return SimpleTypeName.String;

                case TemplateKeyword.Object:
                    return SimpleTypeName.Object;

                default:
                    return SimpleTypeName.None;

            }
        }

        public bool ParseNamespaceOrTypeName(ref TokenStream tokenStream, out ExpressionIndex<TypeNamePart> typeNamePart) {
            // not using qualified_alias_member!
            // (identifier type_argument_list? | qualified_alias_member) ('.' identifier type_argument_list?)*
            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation firstLocation)) {
                typeNamePart = default;
                return false;
            }

            // optional 
            ParseTypeArgumentList(ref tokenStream, out ExpressionRange<TypePath> argumentList);

            if (!tokenStream.Peek(SymbolType.Dot)) {
                typeNamePart = expressionBuffer.Add(state.location, tokenStream.location, new TypeNamePart() {
                    identifierLocation = firstLocation,
                    argumentList = argumentList,
                });
                return true;

            }

            using ScopedList<ExpressionIndex<TypeNamePart>> list = scopedAllocator.CreateListScope<ExpressionIndex<TypeNamePart>>(8);

            while (tokenStream.Consume(SymbolType.Dot)) {

                int start = tokenStream.location;
                if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation nextLocation)) {

                    // optional 
                    ParseTypeArgumentList(ref tokenStream, out ExpressionRange<TypePath> chainedArgumentList);

                    list.Add(expressionBuffer.Add(start, tokenStream.location, new TypeNamePart() {
                        identifierLocation = nextLocation,
                        argumentList = chainedArgumentList,
                    }));

                }
                else {
                    // probably hard error? 
                    RestoreState(ref tokenStream, state);
                    typeNamePart = default;
                    return false;
                }

            }

            typeNamePart = expressionBuffer.Add(state.location, tokenStream.location, new TypeNamePart() {
                identifierLocation = firstLocation,
                argumentList = argumentList,
                partList = expressionBuffer.AddExpressionList(list)
            });

            return true;
        }

        private bool IsTypePatternArm(ref TokenStream tokenStream, out ExpressionIndex<TypePatternArm> typePatternArm) {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/patterns#example-1
            // x is string { Length: 5 }
            // type_pattern_arm: identifier ':' expression

            ExpressionParseState state = SaveState(tokenStream);
            typePatternArm = default;

            if (!ParseIdentifier(ref tokenStream, out ExpressionIndex<Identifier> identifier)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                // hard error probably 
                RestoreState(ref tokenStream, state);
                return false;
            }

            typePatternArm = expressionBuffer.Add(state.location, tokenStream.location, new TypePatternArm() {
                expression = expressionIndex,
                identifier = identifier
            });

            return true;
        }

        private bool IsTypePatternArms(ref TokenStream tokenStream, out ExpressionRange<TypePatternArm> armList) {
            // '{' isTypePatternArm (',' isTypePatternArm)* '}'

            armList = default;
            if (!tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);

            if (!IsTypePatternArm(ref tokenStream, out ExpressionIndex<TypePatternArm> firstPattern)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            using AllocatorScope scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex<TypePatternArm>> arms = scope.CreateList<ExpressionIndex<TypePatternArm>>(8);
            arms.Add(firstPattern);
            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!IsTypePatternArm(ref tokenStream, out ExpressionIndex<TypePatternArm> next)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                arms.Add(next);

            }

            if (!tokenStream.Consume(SymbolType.CurlyBraceClose)) {
                // hard error
                RestoreState(ref tokenStream, state);
                return false;
            }

            armList = expressionBuffer.AddExpressionList(arms);

            return true;
        }

        private bool IsTypeExpression(ref TokenStream tokenStream, out ExpressionIndex<IsTypeExpression> expressionIndex) {
            // note: original grammar uses below
            // base_type (rank_specifier | '*')* '?'? isTypePatternArms? identifier?
            // I've changed it slightly, because TypePath handles the type modifiers 
            // typepath isTypePatternArms? identifier?

            ExpressionParseState state = SaveState(tokenStream);
            expressionIndex = default;

            bool not = tokenStream.Consume(TemplateKeyword.Not);

            // todo -- the hard errors are not standalone, this is only because we checked before that this wasn't an `is null` pattern

            if (!ParseTypePath(ref tokenStream, out var typePath)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedTypePath, HelpType.IsTypeSyntax);
            }

            // optional, but not supported
            int typePatternArmStart = tokenStream.location;
            if (IsTypePatternArms(ref tokenStream, out var typePatternArms)) {
                return HardError(typePatternArmStart, DiagnosticError.NotImplemented, HelpType.TypePatternArmsNotSupported);
            }

            // optional
            int idStart = tokenStream.location;
            if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation) && not) {
                return HardError(idStart, DiagnosticError.UnexpectedIdentifier, HelpType.IsNotTypeSyntax);
            }

            expressionIndex = expressionBuffer.Add(state.location, tokenStream.location, new IsTypeExpression() {
                isNegated = not,
                typePath = typePath,
                typePatternArms = typePatternArms,
                identifier = identifierLocation
            });

            return true;
        }

    }

}