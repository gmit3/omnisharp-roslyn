using System;
using System.Diagnostics;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal unsafe struct TokenStream {

        public int location;
        public readonly int start;
        public readonly int end;
        public CheckedArray<Token> tokens;
        private readonly FixedCharacterSpan source;

        private bool* panicSource;

        public TokenStream(CheckedArray<Token> tokens, FixedCharacterSpan source, bool* panicSource) {
            this.tokens = tokens;
            this.location = 0;
            this.start = 0;
            this.end = tokens.size;
            this.source = source;
            this.panicSource = panicSource;
        }

        private TokenStream(int start, int end, CheckedArray<Token> tokens, FixedCharacterSpan source, bool* panicSource) {
            this.start = Math.Max(0, start);
            this.end = Math.Min(end, tokens.size);
            this.location = start;
            this.tokens = tokens;
            this.source = source;
            this.panicSource = panicSource;
        }

        public bool IsEmpty => location >= end;

        public bool HasMoreTokens => !IsPanicking && location < end;

        public bool IsPanicking => *panicSource;

        public Token Current {
            get {
                if (IsPanicking) return default;
                return location < end ? tokens[location] : default;
            }
        }

        public TokenStream WidenKeepLocation(int count = 1) {
            TokenStream retn = new TokenStream(start - count, end + count, tokens, source, panicSource);
            retn.location += count;
            return retn;
        }

        public TokenStream Widen(int count = 1) {
            return new TokenStream(start - count, end + count, tokens, source, panicSource);
        }

        public TokenStream Narrow(int count = 1) {
            return new TokenStream(start + count, end - count, tokens, source, panicSource);
        }

        public bool TryGetSubStream(TokenType openType, TokenType closedType, out TokenStream subStream) {
            subStream = default;
            if (IsPanicking) return default;
            
            if (tokens[location].tokenType != openType) {
                return false;
            }

            int i = location + 1;

            int counter = 1;
            while (i < end) {
                TokenType type = tokens[i].tokenType;

                if (type == openType) {
                    counter++;
                }
                else if (type == closedType) {
                    counter--;
                    if (counter == 0) {
                        subStream = new TokenStream(location + 1, i, tokens, source, panicSource);
                        location = i + 1;
                        return true;
                    }
                }

                i++;
            }

            return false;
        }

        public bool TryGetSubStream(SubStreamType subStreamType, out TokenStream subStream) {
            subStream = default;
            SymbolType openType;
            SymbolType closedType;

            if (IsPanicking) return default;

            switch (subStreamType) {

                case SubStreamType.CurlyBraces:
                    openType = SymbolType.CurlyBraceOpen;
                    closedType = SymbolType.CurlyBraceClose;
                    break;

                case SubStreamType.SquareBrackets:
                    openType = SymbolType.SquareBraceOpen;
                    closedType = SymbolType.SquareBraceClose;
                    break;

                case SubStreamType.Parens:
                    openType = SymbolType.OpenParen;
                    closedType = SymbolType.CloseParen;
                    break;

                case SubStreamType.AngleBrackets:
                    openType = SymbolType.LessThan;
                    closedType = SymbolType.GreaterThan;
                    break;

                default:
                    return false;
            }

            if (tokens[location].Symbol != openType) {
                return false;
            }

            int i = location + 1;

            int counter = 1;
            while (i < end) {
                SymbolType type = tokens[i].Symbol;

                if (type == openType) {
                    counter++;
                }
                else if (type == closedType) {
                    counter--;
                    if (counter == 0) {
                        subStream = new TokenStream(location + 1, i, tokens, source, panicSource);
                        location = i + 1;
                        return true;
                    }
                }

                i++;
            }

            return false;
        }

        public bool Consume(SymbolType symbolType, out Token token) {
            if (!IsPanicking && location < end && tokens[location].Symbol == symbolType) {
                token = tokens[location];
                location++;
                return true;
            }

            token = default;
            return false;
        }

        public bool Consume(TokenType tokenType, out Token token) {
            if (!IsPanicking && location < end && tokens[location].tokenType == tokenType) {
                token = tokens[location];
                location++;
                return true;
            }

            token = default;
            return false;
        }

        public bool Consume(TemplateKeyword keyword) {
            if (!IsPanicking && location < end && tokens[location].Keyword == keyword) {
                location++;
                return true;
            }

            return false;

        }

        public bool Consume(TemplateKeyword keyword, out Token token) {
            if (!IsPanicking && location < end && tokens[location].Keyword == keyword) {
                token = tokens[location];
                location++;
                return true;
            }

            token = default;
            return false;

        }

        [DebuggerStepThrough]
        public bool Consume(SymbolType symbol) {
            if (!IsPanicking && location < end && tokens[location].Symbol == symbol) {
                location++;
                return true;
            }

            return false;
        }

        [DebuggerStepThrough]
        public bool Peek(SymbolType symbol, out Token token) {
            if (!IsPanicking && location < end && tokens[location].Symbol == symbol) {
                token = tokens[location];
                return true;
            }

            token = default;
            return false;
        }

        [DebuggerStepThrough]
        public bool Peek(SymbolType symbol) {
            return !IsPanicking && location < end && tokens[location].Symbol == symbol;
        }

        [DebuggerStepThrough]
        public bool Peek(TemplateKeyword keyword) {
            return !IsPanicking && location < end && tokens[location].Keyword == keyword;
        }

        [DebuggerStepThrough]
        public bool PeekIdentifierFollowedByKeyword(TemplateKeyword keyword) {
            if (!IsPanicking && location + 1 < end) {
                return tokens[location].tokenType == TokenType.KeywordOrIdentifier && tokens[location + 1].Keyword == keyword;
            }

            return false;
        }

        [DebuggerStepThrough]
        public bool PeekIdentifierFollowedBySymbol(SymbolType symbolType) {

            if (!IsPanicking && location + 1 < end) {
                return tokens[location].tokenType == TokenType.KeywordOrIdentifier && tokens[location + 1].Symbol == symbolType;
            }

            return false;

        }
        [DebuggerStepThrough]
        public bool PeekIdentifierFollowedByCSharpSymbol() {

            if (!IsPanicking && location + 1 < end) {
                if (tokens[location].tokenType == TokenType.KeywordOrIdentifier) {
                    SymbolType symbol = tokens[location + 1].Symbol;
                    if (symbol == SymbolType.Assign
                        || symbol == SymbolType.Dot
                        || symbol == SymbolType.AddAssign
                        || symbol == SymbolType.MultiplyAssign
                        || symbol == SymbolType.Multiply
                        || symbol == SymbolType.SubtractAssign
                        || symbol == SymbolType.Minus
                        || symbol == SymbolType.Plus
                        || symbol == SymbolType.Modulus
                        || symbol == SymbolType.ModAssign
                        || symbol == SymbolType.Divide
                        || symbol == SymbolType.DivideAssign
                        || symbol == SymbolType.Coalesce
                        || symbol == SymbolType.Equals
                        || symbol == SymbolType.NotEquals
                        // || symbol == SymbolType.LessThan can't do this one because of generic templates
                        || symbol == SymbolType.LessThanEqualTo
                        || symbol == SymbolType.GreaterThan
                        || symbol == SymbolType.GreaterThanEqualTo
                        || symbol == SymbolType.Increment
                        || symbol == SymbolType.Decrement
                        || symbol == SymbolType.BinaryAnd
                        || symbol == SymbolType.BinaryNot
                        || symbol == SymbolType.BinaryOr
                        || symbol == SymbolType.BinaryXor
                        || symbol == SymbolType.ConditionalAccess
                        || symbol == SymbolType.CoalesceAssign
                        || symbol == SymbolType.ConditionalOr
                        || symbol == SymbolType.AndAssign
                        || symbol == SymbolType.OrAssign
                        || symbol == SymbolType.XorAssign
                        || symbol == SymbolType.LeftShiftAssign
                        || symbol == SymbolType.RightShiftAssign
                        || symbol == SymbolType.QuestionMark
                       ) {
                        return true;
                    }
                }

            }

            return false;

        }
        [DebuggerStepThrough]
        public bool ConsumeKeywordOrIdentifier() {
            return Consume(TokenType.KeywordOrIdentifier, out _);
        }

        [DebuggerStepThrough]
        public bool ConsumeNonKeywordIdentifier() {
            return ConsumeNonKeywordIdentifier(out _);
        }

        [DebuggerStepThrough]
        public bool ConsumeNonKeywordIdentifier(out Token token) {
            if (!IsPanicking && location < end) {
                token = tokens[location];
                if (token.tokenType == TokenType.KeywordOrIdentifier && token.Keyword == TemplateKeyword.Invalid) {
                    location++;
                    return true;
                }
            }

            token = default;
            return false;

        }

        [DebuggerStepThrough]
        public bool ConsumeKeywordOrIdentifier(out Token token) {
            return Consume(TokenType.KeywordOrIdentifier, out token);
        }

        [DebuggerStepThrough]
        public bool ConsumeDollarIdentifier(out Token identifier) {
            return Consume(TokenType.DollarIdentifier, out identifier);
        }

        [DebuggerStepThrough]
        public bool ConsumeAnyIdentifier(out Token identifier) {
            return Consume(TokenType.KeywordOrIdentifier, out identifier) || Consume(TokenType.DollarIdentifier, out identifier);
        }

        [DebuggerStepThrough]
        public bool ConsumeAnyIdentifier(out NonTrivialTokenLocation tokenLocation) {
            if (Consume(TokenType.KeywordOrIdentifier, out _)) {
                tokenLocation = new NonTrivialTokenLocation(location - 1);
                return true;
            }

            if (Consume(TokenType.DollarIdentifier, out _)) {
                tokenLocation = new NonTrivialTokenLocation(location - 1);
                return true;
            }

            tokenLocation = new NonTrivialTokenLocation(-1);
            return false;
        }

        public NonTrivialTokenRange GetFullTokenRange() {
            return new NonTrivialTokenRange(start, end);
        }

        public CheckedArray<Token> GetTokens() {
            return tokens;
        }

        public bool TryGetNextTraversedStream(SymbolType target, out TokenStream stream) {
            if (IsPanicking) {
                stream = default;
                return false;
            }

            stream = GetNextTraversedStream(target);
            return stream.HasMoreTokens;
        }

        public TokenStream GetNextTraversedStream(SymbolType target) {
            int i = location;
            int startLocation = location;

            while (i < end) {

                ref Token c = ref tokens.Get(i);

                SymbolType op = c.Symbol;
                if (op == target) {
                    location = i;
                    return new TokenStream(startLocation, i, tokens, source, panicSource);
                }

                if (op == SymbolType.OpenParen) {
                    location = i;
                    if (!TryGetSubStream(SubStreamType.Parens, out _)) {
                        i++;
                        continue;
                    }

                    i = location - 1;
                }
                else if (op == SymbolType.CurlyBraceOpen) {
                    location = i;
                    if (!TryGetSubStream(SubStreamType.CurlyBraces, out _)) {
                        i++;
                        continue;
                    }

                    i = location - 1;
                }
                else if (op == SymbolType.SquareBraceOpen) {
                    location = i;
                    if (!TryGetSubStream(SubStreamType.SquareBrackets, out _)) {
                        i++;
                        continue;
                    }

                    i = location - 1;
                }
                else if (op == SymbolType.LessThan) {
                    location = i;
                    if (!TryGetSubStream(SubStreamType.AngleBrackets, out TokenStream stream)) {
                        i++;
                        continue;
                    }

                    // if not identifier or dot or < or > then bail out 

                    if (!ValidateAngleBracketStream(stream)) {
                        i++;
                        continue;
                    }
                   
                    i = location - 1;
                }

                i++;

            }

            location = end;
            return new TokenStream(startLocation, end, tokens, source, panicSource);

        }

        private static bool ValidateAngleBracketStream(TokenStream stream) {
            for (int k = stream.start; k < stream.end; k++) {
                Token token = stream.tokens[k];
                if (token.tokenType == TokenType.OperatorOrPunctuator) {
                    if (token.Symbol != SymbolType.Dot && token.Symbol != SymbolType.LessThan && token.Symbol != SymbolType.GreaterThan) {
                        return false;
                    }
                }
                else if (token.tokenType != TokenType.KeywordOrIdentifier) {
                    return false;
                }
            }

            return true;
        }

        [DebuggerStepThrough]
        public bool Consume(SymbolType symbolType, int count) {

            if (IsPanicking) return false;

            for (int i = 0; i < count; i++) {
                if (location + i >= end) {
                    return false;
                }

                if (tokens[location + i].Symbol != symbolType) {
                    return false;
                }
            }

            location += count;
            return true;
        }

        [DebuggerStepThrough]
        public bool ConsumeStandardIdentifier(out NonTrivialTokenLocation nonTrivialTokenLocation) {
            if (IsPanicking) {
                nonTrivialTokenLocation = default;
                return false;
            }

            if (ConsumeKeywordOrIdentifier()) {
                nonTrivialTokenLocation = new NonTrivialTokenLocation(location - 1);
                return true;
            }

            nonTrivialTokenLocation = default;
            return false;
        }

    }

    internal enum SubStreamType {

        Parens,
        CurlyBraces,
        SquareBrackets,
        AngleBrackets,

    }

}