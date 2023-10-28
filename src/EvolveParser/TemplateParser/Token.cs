using System;
using System.Runtime.InteropServices;
using System.Text;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal class TokenDebugView {

        public Token token;

        public TokenDebugView(Token token) {
            this.token = token;
        }

    }

#if EVOLVE_UI_DEV
    [System.Diagnostics.DebuggerDisplay("{DebugDisplay()}")]
    publi unsafe struct Token {

#else
    public struct Token {
#endif

        public TokenType tokenType;
        public int lineIndex;
        public int columnIndex;
        public int charIndex;
        public int length;
        public int tokenIndex;
        public TokenExtraData extra;

#if EVOLVE_UI_DEV
        public FixedCharacterSpan debugSource;

        public string __DEBUG__ => DebugDisplay();

        public string DebugDisplay() {
            if (debugSource.size != 0) {
                return Print(debugSource, false);
            }

            return "";
        }

#endif
        public TemplateKeyword Keyword {
            get => tokenType == TokenType.KeywordOrIdentifier ? extra.keyword : (TemplateKeyword)0;
        }

        public SymbolType Symbol {
            get => tokenType == TokenType.OperatorOrPunctuator ? extra.Symbol : (SymbolType)0;
        }

        public RuntimeKeyword RuntimeKeyword {
            get => tokenType == TokenType.DollarIdentifier ? (RuntimeKeyword)0 : (RuntimeKeyword)0; // todo fill this in  
        }

        public string Print(FixedCharacterSpan input, bool newLine) {
            StringBuilder builder = new StringBuilder();

            void AppendToSpaceCount(int count) {
                builder.Append(' ', count - builder.Length);
            }

            const int kSpaceCount = 50;

            switch (tokenType) {

                case TokenType.Whitespace: {
                    builder.Append("Whitespace ");
                    builder.Append("(");
                    builder.Append(length);
                    builder.Append(")");
                    if (newLine) {
                        builder.AppendLine();
                    }

                    break;
                }

                case TokenType.NewLine: {
                    builder.Append("NewLine ");
                    builder.Append("(");
                    builder.Append(length);
                    builder.Append(")");
                    if (newLine) {
                        builder.AppendLine();
                    }

                    break;
                }

                default: {
                    builder.Append(tokenType.ToString());
                    AppendToSpaceCount(kSpaceCount);
                    for (int i = 0; i < length; i++) {
                        builder.Append(input[charIndex + i]);
                    }

                    if (newLine) {
                        builder.AppendLine();
                    }

                    break;
                }
            }

            return builder.ToString();
        }

        public static void UnsetDebugSources(Span<Token> tokens) {
#if EVOLVE_UI_DEV
            for (int i = 0; i < tokens.Length; i++) {
                tokens[i].debugSource = default;
            }
#endif
        }
        public static void UnsetDebugSources(CheckedArray<Token> tokens) {
#if EVOLVE_UI_DEV
            for (int i = 0; i < tokens.size; i++) {
                tokens.Get(i).debugSource = default;
            }
#endif
        }
        public static void SetDebugSources(Span<Token> tokens, FixedCharacterSpan source) {
#if EVOLVE_UI_DEV
            for (int i = 0; i < tokens.Length; i++) {
                tokens[i].debugSource = source;
            }
#endif
        }
        public static void SetDebugSources(CheckedArray<Token> tokens, FixedCharacterSpan source) {
#if EVOLVE_UI_DEV
            for (int i = 0; i < tokens.size; i++) {
                tokens.Get(i).debugSource = source;
            }
#endif
        }
        public static int CountNonTrivialTokens(CheckedArray<Token> tokens) {
            int count = 0;
            for (int i = 0; i < tokens.size; i++) {
                TokenType tokenType = tokens[i].tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    count++;
                }
            }

            return count;
        }

        public static int CountNonTrivialTokens(Span<Token> tokens) {
            int count = 0;
            for (int i = 0; i < tokens.Length; i++) {
                TokenType tokenType = tokens[i].tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    count++;
                }
            }

            return count;
        }

        public static PodList<Token> GatherNonTrivialTokens(CheckedArray<Token> tokens) {
            PodList<Token> retn = new PodList<Token>(tokens.size);

            for (int i = 0; i < tokens.size; i++) {
                TokenType tokenType = tokens[i].tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    retn.Add(tokens[i]);
                }
            }
            
            return retn;
        }
        
        public static void GatherNonTrivialTokens(CheckedArray<Token> tokens, CheckedArray<Token> output) {

            int writeIndex = 0;

            for (int i = 0; i < tokens.size; i++) {
                TokenType tokenType = tokens[i].tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    output[writeIndex++] = tokens[i];
                }
            }

        }
        public static void GatherNonTrivialTokens(PodList<Token> tokens, CheckedArray<Token> output) {

            int writeIndex = 0;

            for (int i = 0; i < tokens.size; i++) {
                ref Token token = ref tokens.Get(i);
                TokenType tokenType = token.tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    output[writeIndex++] = token;
                }
            }

        }
        public static void GatherNonTrivialTokens(Span<Token> tokens, Span<Token> output) {

            int writeIndex = 0;

            for (int i = 0; i < tokens.Length; i++) {
                TokenType tokenType = tokens[i].tokenType;
                if (tokenType != TokenType.Comment && tokenType != TokenType.Whitespace && tokenType != TokenType.NewLine) {
                    output[writeIndex++] = tokens[i];
                }
            }

        }

    }

    public enum LiteralType {

        None = 0,
        UnsignedLong = 1 << 2,
        UnsignedInteger = 1 << 3,
        Long = 1 << 4,
        Float = 1 << 5,
        Double = 1 << 6,
        Integer = 1 << 7,
        BoolTrue = 1 << 8,
        BoolFalse = 1 << 9,
        StyleLiteral = 1 << 10,
        StringLiteral = 1 << 12,
        Character = 1 << 14,
        UnicodeEscapeSequence = 1 << 15,
        Null = 1 << 16,
        VerbatimStringLiteral = 1 << 17

    }
    public enum PrefixOperator {

        Invalid,
        Plus,
        Minus,
        Not,
        BitwiseNot,
        Increment,
        Decrement,
        AddressOf,
        Dereference,
        Range

    }

    public enum TokenType {

        EndOfInput,
        Whitespace,
        NewLine,
        Comment,
        KeywordOrIdentifier,

        OperatorOrPunctuator,
        DollarIdentifier,

        NumericLiteral,
        BoolLiteral,
        CharacterLiteral,
        StringLiteral,

        StyleLiteral,

        StringInterpolationStart,
        StringInterpolationEnd,

        StringInterpolationPartStart,
        StringInterpolationPartEnd,

    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TokenExtraData {

        [FieldOffset(0)] public TemplateKeyword keyword;
        [FieldOffset(0)] public SymbolType Symbol;
        [FieldOffset(0)] public LiteralType literalType;

        public TokenExtraData(SymbolType symbolType) {
            this.keyword = 0;
            this.literalType = 0;
            this.Symbol = symbolType;
        }

        public TokenExtraData(TemplateKeyword keyword) {
            this.Symbol = 0;
            this.literalType = 0;
            this.keyword = keyword;
        }

        public TokenExtraData(LiteralType literalType) {
            this.Symbol = 0;
            this.keyword = 0;
            this.literalType = literalType;
        }

    }

}
