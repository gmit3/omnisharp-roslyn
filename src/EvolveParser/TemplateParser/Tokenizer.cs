using System.Globalization;
using System.Runtime.InteropServices;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal struct TokenizerResult {

        public int nonTrivialTokenCount;
        public TokenizerError error;
        public int lineIndex;
        public int linePosition;
        public int errorStartLocation;
        public int endParseLocation;

    }

    internal enum TokenizerError {

        None = 0,

        Stuck,
        NewlineInsideStringLiteral,
        EndOfInputInStringLiteral,
        EndOfInputInStyleLiteral,
        NewlineInStyleLiteral,

        InvalidNumericLiteral,
        InvalidCharacterLiteral,

        InvalidStringInterpolation

    }

    internal unsafe ref struct Tokenizer {

        private int location;
        private int lineIndex;
        private int linePosition;
        private int errorLocation;

        private FixedCharacterSpan input;
        private PodList<Token>* outputStream;
        private TokenizerError error;

        public bool HasMoreInput => location < input.size;

        public static bool TryTokenize(string source, IList<Token> output) {
            return TryTokenize(source, output, out _);
        }

        public static bool TryTokenize(string source, IList<Token> output, out TokenizerResult result) {
            Tokenizer tokenizer = new Tokenizer();
            PodList<Token> buffer = new PodList<Token>(1024);
            fixed (char* cbuffer = source) {
                tokenizer.Tokenize(new FixedCharacterSpan(cbuffer, source.Length), &buffer, out result);
            }

            for (int i = 0; i < buffer.size; i++) {
                output.Add(buffer.Get(i));
            }

            buffer.Dispose();
            return result.error == TokenizerError.None;
        }

        public static bool TryTokenize(FixedCharacterSpan source, PodList<Token>* output, out TokenizerResult result) {
            Tokenizer tokenizer = new Tokenizer();

            tokenizer.Tokenize(source, output, out result);

            if (result.error == TokenizerError.None) {
                result.nonTrivialTokenCount = Token.CountNonTrivialTokens(output->ToCheckedArray());
            }

            return result.error == TokenizerError.None;
        }

        private bool Tokenize(FixedCharacterSpan source, PodList<Token>* output, out TokenizerResult result) {
            location = 0;
            lineIndex = 1;
            linePosition = 0;
            input = source;
            errorLocation = 0;
            outputStream = output;
            result = default;

            while (error == TokenizerError.None && HasMoreInput) {
                int loopStart = location;

                if (Tokenize()) {
                    continue;
                }

                if (location == loopStart) {
                    error = TokenizerError.Stuck;
                    errorLocation = location;
                }
            }

            if (error != TokenizerError.None) {
                result.error = error;
                result.errorStartLocation = errorLocation;
                result.lineIndex = lineIndex;
                result.linePosition = linePosition;
                result.endParseLocation = location;
            }

            for (int i = 0; i < outputStream->size; i++) {
                outputStream->Get(i).tokenIndex = i;
            }

            return result.error == TokenizerError.None;
        }

        private bool Tokenize() {
            if (TryReadWhitespace()) {
                return true;
            }

            if (TryReadComment()) {
                return true;
            }

            if (TryReadNewLine()) {
                return true;
            }

            if (TryReadKeyword()) {
                return true;
            }

            if (TryReadDollarIdentifier()) {
                return true;
            }

            if (TryReadBoolLiteral()) {
                return true;
            }

            if (TryReadAtIdentifier()) {
                return true;
            }

            if (TryReadIdentifier()) {
                return true;
            }

            if (TryReadCharacterLiteral()) {
                return true;
            }

            if (TryReadNumericLiteral()) {
                return true;
            }

            if (TryReadStringInterpolation()) {
                return true;
            }

            if (MatchSequence('$', '@', '"') || MatchSequence('@', '"')) {
                throw new NotImplementedException("Interpolated strings verbatim haven't been implemented yet");
            }

            if (TryReadStringLiteral()) {
                return true;
            }

            if (TryReadStyleLiteral()) {
                return true;
            }

            if (TryReadOperatorOrPunctuator()) {
                return true;
            }

            return false;
        }

        private bool TryReadStringInterpolation() {
            if (!MatchSequence('$', '"')) return false;
            int start = location;

            outputStream->Add(new Token() {
                length = 2,
                tokenType = TokenType.StringInterpolationStart,
                lineIndex = lineIndex,
                charIndex = start - 2,
                columnIndex = start - 2 - linePosition,
                extra = default
            });

            // read until " or { 
            char last = '\0';

            bool gracefulExit = false;
            // read until we get to " or newline
            // do we handle new lines? I think not until we support verbatim strings later on 

            int lastStringStart = location;

            int level = 0;
            while (HasMoreInput) {
                char c = input[location];

                if (c == '"' && last != '\\') {
                    location++;
                    gracefulExit = true;
                    break;
                }

                // scan ahead to next unsecaped }  
                // recurse in that range 

                if (c == '\n') {
                    Abort(TokenizerError.NewlineInsideStringLiteral, start);
                    return false;
                }

                if (c == '{' && last != '{' && last != '\'') {
                    location++;
                    int scanStart = location;

                    level++;

                    // add the leading string from start of string to { 
                    if (location - lastStringStart - 1 != 0) {
                        outputStream->Add(new Token() {
                            tokenType = TokenType.StringLiteral,
                            length = location - lastStringStart - 1,
                            lineIndex = lineIndex,
                            charIndex = lastStringStart,
                            columnIndex = lastStringStart - linePosition,
                            extra = new TokenExtraData(LiteralType.StringLiteral)
                        });
                    }

                    int scanEnd = -1;

                    while (HasMoreInput) {
                        if (MatchSequence('/', '/')) {
                            // read until new line
                            while (HasMoreInput) {
                                char commentChar = input[location];

                                if (commentChar == '\n' || MatchCharacters('\u000D', '\u000A', '\u0085', '\u2028', '\u2029') || MatchSequence('\r', '\n', k_DoNotAdvance)) {
                                    break;
                                }

                                // we know there is a new line here but the next loop iteration will handle it 
                                location++;
                            }

                            continue;
                        }

                        // how do I pick up ':' formats and ',' alignment directives? maybe just let them go and handle it via parsing?

                        if (input[location] == '{' && input[location - 1] != '\\') {
                            level++;
                            location++;
                            continue;
                        }

                        if (input[location] == '}' && input[location - 1] != '\\') {
                            // found our end point
                            // make sure the next char isn't also a closing brace 
                            location++;
                            if (HasMoreInput && input[location] == '}') {
                                continue;
                            }

                            level--;
                            if (level == 0) {
                                // otherwise trigger out end condition
                                scanEnd = location;
                                break;
                            }
                        }

                        location++;
                    }

                    if (scanEnd == -1) {
                        Abort(TokenizerError.InvalidStringInterpolation, scanStart);
                        return false;
                    }

                    FixedCharacterSpan prevInput = input;
                    scanEnd--;
                    input = new FixedCharacterSpan(input.ptr, scanEnd);
                    location = scanStart;

                    outputStream->Add(new Token() {
                        length = 1,
                        tokenType = TokenType.StringInterpolationPartStart,
                        lineIndex = lineIndex,
                        charIndex = scanStart - 1,
                        columnIndex = scanStart - 1 - linePosition,
                        extra = default
                    });

                    while (HasMoreInput) {
                        if (!Tokenize()) {
                            input = prevInput;
                            return false;
                        }
                    }

                    input = prevInput;

                    outputStream->Add(new Token() {
                        length = 1,
                        tokenType = TokenType.StringInterpolationPartEnd,
                        lineIndex = lineIndex,
                        charIndex = location,
                        columnIndex = location - linePosition,
                        extra = default
                    });

                    lastStringStart = location + 1;
                }

                last = input[location];
                location++;
            }

            if (!gracefulExit) {
                // error, hard abort 
                Abort(TokenizerError.EndOfInputInStringLiteral, start);
                return false;
            }

            // add the trailing string from } to end of string 
            if (location - lastStringStart - 1 != 0) {
                outputStream->Add(new Token() {
                    tokenType = TokenType.StringLiteral,
                    length = location - lastStringStart - 1,
                    lineIndex = lineIndex,
                    charIndex = lastStringStart,
                    columnIndex = lastStringStart - linePosition,
                    extra = new TokenExtraData(LiteralType.StringLiteral)
                });
            }

            outputStream->Add(new Token() {
                tokenType = TokenType.StringInterpolationEnd,
                length = 1,
                lineIndex = lineIndex,
                charIndex = location - 1,
                columnIndex = location - 1 - linePosition,
                extra = default
            });

            return true;
        }

        private const bool k_DoNotAdvance = false;

        private bool TryReadComment() {
            int start = location;

            if (MatchSequence('/', '/')) {
                // read until new line
                while (HasMoreInput) {
                    char c = input[location];

                    if (c == '\n' || MatchCharacters('\u000D', '\u000A', '\u0085', '\u2028', '\u2029') || MatchSequence('\r', '\n', k_DoNotAdvance)) {
                        break;
                    }

                    location++;
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.Comment,
                    length = location - start,
                    columnIndex = start - linePosition,
                    charIndex = start,
                    lineIndex = lineIndex
                });
                // we know there is a new line here but the next loop iteration handle it 
                return true;
            }

            return false;
        }

        private bool TryReadOperatorOrPunctuator() {
            char c0 = input[location];

            if (char.IsLetterOrDigit(c0)) {
                return false;
            }

            Token token = new Token();
            token.tokenType = TokenType.OperatorOrPunctuator;
            token.charIndex = location;
            token.lineIndex = lineIndex;
            token.columnIndex = location - linePosition;

            if (MatchOperator('@', SymbolType.At, ref token)) return true;
            if (MatchOperator('&', '&', SymbolType.BooleanAnd, ref token)) return true;
            if (MatchOperator('?', '?', '=', SymbolType.CoalesceAssign, ref token)) return true;
            if (MatchOperator('?', '?', SymbolType.Coalesce, ref token)) return true;
            if (MatchOperator('?', '.', SymbolType.ConditionalAccess, ref token)) return true;
            if (MatchOperator('|', '|', SymbolType.ConditionalOr, ref token)) return true;
            if (MatchOperator('=', '>', SymbolType.FatArrow, ref token)) return true;
            if (MatchOperator('=', '=', SymbolType.Equals, ref token)) return true;
            if (MatchOperator('!', '=', SymbolType.NotEquals, ref token)) return true;
            if (MatchOperator('+', '+', SymbolType.Increment, ref token)) return true;
            if (MatchOperator('-', '-', SymbolType.Decrement, ref token)) return true;
            if (MatchOperator('-', '>', SymbolType.ThinArrow, ref token)) return true;
            if (MatchOperator('=', SymbolType.Assign, ref token)) return true;
            if (MatchOperator('+', '=', SymbolType.AddAssign, ref token)) return true;
            if (MatchOperator('-', '=', SymbolType.SubtractAssign, ref token)) return true;
            if (MatchOperator('*', '=', SymbolType.MultiplyAssign, ref token)) return true;
            if (MatchOperator('/', '=', SymbolType.DivideAssign, ref token)) return true;
            if (MatchOperator('%', '=', SymbolType.ModAssign, ref token)) return true;
            if (MatchOperator('&', '=', SymbolType.AndAssign, ref token)) return true;
            if (MatchOperator('|', '=', SymbolType.OrAssign, ref token)) return true;
            if (MatchOperator('^', '=', SymbolType.XorAssign, ref token)) return true;
            if (MatchOperator('<', '<', '=', SymbolType.LeftShiftAssign, ref token)) return true;
            if (MatchOperator('>', '>', '=', SymbolType.RightShiftAssign, ref token)) return true;
            if (MatchOperator('>', '=', SymbolType.GreaterThanEqualTo, ref token)) return true;
            if (MatchOperator('<', '=', SymbolType.LessThanEqualTo, ref token)) return true;
            if (MatchOperator(':', ':', SymbolType.DoubleColon, ref token)) return true;
            if (MatchOperator('.', '.', SymbolType.Range, ref token)) return true;
            if (MatchOperator('>', SymbolType.GreaterThan, ref token)) return true;
            if (MatchOperator('<', SymbolType.LessThan, ref token)) return true;
            if (MatchOperator('!', SymbolType.Not, ref token)) return true;
            if (MatchOperator('+', SymbolType.Plus, ref token)) return true;
            if (MatchOperator('-', SymbolType.Minus, ref token)) return true;
            if (MatchOperator('/', SymbolType.Divide, ref token)) return true;
            if (MatchOperator('*', SymbolType.Multiply, ref token)) return true;
            if (MatchOperator('%', SymbolType.Modulus, ref token)) return true;
            if (MatchOperator('~', SymbolType.BinaryNot, ref token)) return true;
            if (MatchOperator('|', SymbolType.BinaryOr, ref token)) return true;
            if (MatchOperator('&', SymbolType.BinaryAnd, ref token)) return true;
            if (MatchOperator('^', SymbolType.BinaryXor, ref token)) return true;
            if (MatchOperator('?', SymbolType.QuestionMark, ref token)) return true;
            if (MatchOperator(':', SymbolType.Colon, ref token)) return true;
            if (MatchOperator(';', SymbolType.SemiColon, ref token)) return true;
            if (MatchOperator('.', SymbolType.Dot, ref token)) return true;
            if (MatchOperator(',', SymbolType.Comma, ref token)) return true;
            if (MatchOperator('(', SymbolType.OpenParen, ref token)) return true;
            if (MatchOperator(')', SymbolType.CloseParen, ref token)) return true;
            if (MatchOperator('[', SymbolType.SquareBraceOpen, ref token)) return true;
            if (MatchOperator(']', SymbolType.SquareBraceClose, ref token)) return true;
            if (MatchOperator('{', SymbolType.CurlyBraceOpen, ref token)) return true;
            if (MatchOperator('}', SymbolType.CurlyBraceClose, ref token)) return true;
            if (MatchOperator('#', SymbolType.HashTag, ref token)) return true;
            if (MatchOperator('_', SymbolType.Underscore, ref token)) return true;

            return false;
        }

        private bool MatchOperator(char c0, char c1, char c2, SymbolType symbol, ref Token token) {
            if (location + 2 >= input.size || input[location] != c0 || input[location + 1] != c1 || input[location + 2] != c2) {
                return false;
            }

            location += 3;
            token.length = 3;
            token.extra = new TokenExtraData(symbol);
            outputStream->Add(token);
            return true;
        }

        private bool MatchOperator(char c0, char c1, SymbolType symbol, ref Token token) {
            if (location + 1 >= input.size || input[location] != c0 || input[location + 1] != c1) {
                return false;
            }

            location += 2;
            token.length = 2;
            token.extra = new TokenExtraData(symbol);
            outputStream->Add(token);
            return true;
        }

        private bool MatchOperator(char c, SymbolType symbol, ref Token token) {
            if (location >= input.size || input[location] != c) {
                return false;
            }

            location++;

            token.length = 1;
            token.extra = new TokenExtraData(symbol);
            outputStream->Add(token);
            return true;
        }

        private bool TryReadStyleLiteral() {
            int start = location;

            if (MatchSequence('`')) {
                bool gracefulExit = false;
                // read until we get to " or newline
                // do we handle new lines? I think not until we support verbatim strings later on 

                while (HasMoreInput) {
                    char c = input[location];

                    if (c == '`') {
                        gracefulExit = true;
                        break;
                    }

                    if (c == '\n') {
                        Abort(TokenizerError.NewlineInStyleLiteral, start);
                        return true; // fail here, but return true to escape
                    }

                    location++;
                }

                if (!gracefulExit) {
                    // error, hard abort 
                    Abort(TokenizerError.EndOfInputInStyleLiteral, start);
                    return true; // fail here, but return true to escape
                }

                location++;

                outputStream->Add(new Token() {
                    tokenType = TokenType.StyleLiteral,
                    length = location - start,
                    lineIndex = lineIndex,
                    charIndex = start,
                    columnIndex = location - linePosition,
                    extra = new TokenExtraData(LiteralType.StyleLiteral)
                });

                return true;
            }

            if (MatchSequence('$', '`')) {
                throw new NotImplementedException("Interpolated style literals haven't been implemented yet");
            }

            return false;
        }

        private bool TryReadStringLiteral() {
            int start = location;

            if (MatchSequence('@', '"')) {
                char last = '\0';

                bool gracefulExit = false;
                // read until we get to " or newline
                // do we handle new lines? I think not until we support verbatim strings later on 

                while (HasMoreInput) {
                    char c = input[location];

                    if (c == '"' && last != '\\') {
                        location++;
                        gracefulExit = true;
                        break;
                    }

                    if (c == '\n') {
                        Abort(TokenizerError.NewlineInsideStringLiteral, start);
                        return true; // fail here, but return true to escape
                    }

                    last = c;
                    location++;
                }

                if (!gracefulExit) {
                    // error, hard abort 
                    return Abort(TokenizerError.EndOfInputInStringLiteral, start);
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.StringLiteral,
                    length = location - start,
                    lineIndex = lineIndex,
                    charIndex = start,
                    columnIndex = start - linePosition,
                    extra = new TokenExtraData(LiteralType.VerbatimStringLiteral)
                });

                return true;
            }

            if (MatchSequence('"')) {
                char last = '\0';

                bool gracefulExit = false;
                // read until we get to " or newline
                // do we handle new lines? I think not until we support verbatim strings later on 

                while (HasMoreInput) {
                    char c = input[location];

                    if (c == '"' && last != '\\') {
                        location++;
                        gracefulExit = true;
                        break;
                    }

                    if (c == '\n') {
                        Abort(TokenizerError.NewlineInsideStringLiteral, start);
                        return true; // fail here, but return true to escape
                    }

                    last = c;
                    location++;
                }

                if (!gracefulExit) {
                    // error, hard abort 
                    return Abort(TokenizerError.EndOfInputInStringLiteral, start);
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.StringLiteral,
                    length = location - start,
                    lineIndex = lineIndex,
                    charIndex = start,
                    columnIndex = start - linePosition,
                    extra = new TokenExtraData(LiteralType.StringLiteral)
                });

                return true;
            }

            return false;
        }

        private bool Abort(TokenizerError error, int errorLocation) {
            this.error = error;
            this.errorLocation = errorLocation;
            return true;
        }

        private bool MatchSequence(char c) {
            if (location >= input.size || input[location] != c) {
                return false;
            }

            location++;
            return true;
        }

        private bool MatchSequence(char c, char c1, bool advance = true) {
            if (location + 1 >= input.size) {
                return false;
            }

            if (input[location] == c && input[location + 1] == c1) {
                if (advance) {
                    location += 2;
                }

                return true;
            }

            return false;
        }

        private bool MatchSequence(char c, char c1, char c2) {
            if (location + 2 >= input.size) {
                return false;
            }

            if (input[location] == c && input[location + 1] == c1 && input[location + 2] == c2) {
                location += 3;
                return true;
            }

            return false;
        }

        private bool TryReadCharacterLiteral() {
            if (input[location] != '\'') {
                return false;
            }

            if (location + 1 >= input.size) {
                return false;
            }

            int start = location;
            location++;

            // anything but ', \, and New_Line_Character
            // todo -- im not sure about this actually, should probably allow these if inside single quotes, right?
            // if (MatchCharacters('\u000D', '\u000A', '\u0085', '\u2028', '\u2029')) {
            //     location = start;
            //     return false;
            // }

            TokenExtraData extraData;

            // simple escape sequences, not sure how to handle this
            // i think we need to check for '\\" then 'n' instead here 
            if (MatchSequence('\\', 'u') && MatchHexDigit(input, location + 0) &&
                MatchHexDigit(input, location + 1) &&
                MatchHexDigit(input, location + 2) &&
                MatchHexDigit(input, location + 3)) {
                location += 4;
                extraData = new TokenExtraData(LiteralType.UnicodeEscapeSequence);
            }
            else if (MatchCharacters('\\', '\"', '\\', '\0', '\a') || MatchCharacters('\b', '\f', '\n', '\r', '\t')) {
                location += 2; // step over character
                extraData = new TokenExtraData(LiteralType.Character);
            }
            else {
                // todo handle hex and unicode escapes 

                if (input[location] != '\\') {
                    // read 1 character
                    extraData = new TokenExtraData(LiteralType.Character);
                    location++; // step over character
                }
                // else if (MatchSequence('\\', 'u') &&
                //          MatchHexDigit(input, location + 0) &&
                //          MatchHexDigit(input, location + 1) &&
                //          MatchHexDigit(input, location + 2) &&
                //          MatchHexDigit(input, location + 3)) {
                //     location += 4;
                //     extraData = new TokenExtraData(LiteralType.UnicodeEscapeSequence);
                // }
                // hex sequence maybe    
                else {
                    return Abort(TokenizerError.InvalidCharacterLiteral, start);
                }
            }

            if (location >= input.size || input[location] != '\'') {
                return Abort(TokenizerError.InvalidCharacterLiteral, start);
            }

            location++; // account for ' on the end

            // todo -- flag escape type in token flags

            outputStream->Add(new Token() {
                tokenType = TokenType.CharacterLiteral,
                length = location - start,
                extra = extraData,
                charIndex = start,
                columnIndex = start - linePosition,
                lineIndex = lineIndex,
            });

            return true;
        }

        private bool TryReadBoolLiteral() {
            if (location + 4 <= input.size &&
                input[location + 0] == 't' &&
                input[location + 1] == 'r' &&
                input[location + 2] == 'u' &&
                input[location + 3] == 'e') {
                if (location + 4 < input.size && IsIdentifierCharacter(input[location + 4], false)) {
                    return false;
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.BoolLiteral,
                    length = 4,
                    charIndex = location,
                    columnIndex = location - linePosition,
                    lineIndex = lineIndex,
                    extra = new TokenExtraData(LiteralType.BoolTrue)
                });
                location += 4;
                return true;
            }

            if (location + 5 <= input.size &&
                input[location + 0] == 'f' &&
                input[location + 1] == 'a' &&
                input[location + 2] == 'l' &&
                input[location + 3] == 's' &&
                input[location + 4] == 'e') {
                if (location + 5 < input.size && IsIdentifierCharacter(input[location + 5], false)) {
                    return false;
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.BoolLiteral,
                    extra = new TokenExtraData(LiteralType.BoolFalse),
                    length = 5,
                    charIndex = location,
                    columnIndex = location - linePosition,
                    lineIndex = lineIndex,
                });
                location += 5;
                return true;
            }

            return false;
        }

        private static bool TryReadNumericSequence(FixedCharacterSpan input, int ptr, out int length) {
            int start = ptr;

            while (ptr < input.size) {
                char c = input[ptr];
                if (!char.IsDigit(c)) {
                    break;
                }

                ptr++;
            }

            length = ptr - start;
            return length != 0;
        }

        private bool TryReadNumericLiteral() {
            int start = location;

            if (!TryReadNumericSequence(input, location, out int readCount)) {
                return false;
            }

            location += readCount;

            // todo -- can consider supporting _ magnitude separators

            bool hasDot = false;
            // not sure how to handle dot vs comma here w/ locale, I probably require dots 
            if (HasMoreInput && input[location] == '.') {
                hasDot = true;
                // cannot trail with a dot w/o a digit following, 10.f and 10. are invalid  
                if (!TryReadNumericSequence(input, location + 1, out readCount)) {
                    // something like a literal access is happening here
                    // 10.ToString() for example
                    // we truncate the numeric token and continue
                    outputStream->Add(new Token() {
                        tokenType = TokenType.NumericLiteral,
                        extra = new TokenExtraData(LiteralType.Integer),
                        length = location - start,
                        lineIndex = lineIndex,
                        columnIndex = start - linePosition,
                        charIndex = start
                    });

                    return true;
                }

                location += 1 + readCount; // 1 for the dot 
            }

            if (!TryParseIntegerTypeSuffix(out LiteralType suffixType) && !TryParseRealTypeSuffix(out suffixType)) {
                suffixType = hasDot ? LiteralType.Double : LiteralType.Integer;
            }

            if (HasMoreInput) {
                // if next character is number or identifier character, its an abort condition
                // maybe arguable that the tokenizer doesn't care about this case 
                if (char.IsNumber(input[location]) || IsIdentifierCharacter(input[location], false)) {
                    return Abort(TokenizerError.InvalidNumericLiteral, start);
                }
            }

            outputStream->Add(new Token() {
                tokenType = TokenType.NumericLiteral,
                extra = new TokenExtraData(suffixType),
                length = location - start,
                lineIndex = lineIndex,
                columnIndex = start - linePosition,
                charIndex = start
            });

            return true;
        }

        private bool TryParseRealTypeSuffix(out LiteralType suffixType) {
            suffixType = default;
            if (location >= input.size) return false;
            char c = input[location];

            if (c == 'f' || c == 'F') {
                suffixType = LiteralType.Float;
                location++;
                return true;
            }

            if (c == 'D' || c == 'd') {
                suffixType = LiteralType.Double;
                location++;
                return true;
            }

            return false;
        }

        private bool TryReadAtIdentifier() {
            Token operatorToken = new Token();
            operatorToken.tokenType = TokenType.OperatorOrPunctuator;
            operatorToken.charIndex = location;
            operatorToken.lineIndex = lineIndex;
            operatorToken.columnIndex = location - linePosition;

            // we need to cheat a little. if we have an @ symbol we need to 
            // keep reading characters including numbers until we hit a non
            // character / digit / underscore / dash
            // if we didn't cheat the tokenizer would fail on invalid postfixes for cases like @abc-1s which 
            // should be a valid dashed identifier and not @abc - 1s which is meaningless
            if (MatchOperator('@', SymbolType.At, ref operatorToken)) {
                int start = location;
                char c = input[location];

                // this is probably just a straight up failure 
                if (!char.IsLetter(c) && c != '_') {
                    outputStream->size--;
                    location--;
                    return false;
                }

                while (location < input.size) {
                    c = input[location];

                    if (char.IsLetterOrDigit(c) || c == '_' || c == '-') {
                        location++;
                        continue;
                    }

                    UnicodeCategory category = char.GetUnicodeCategory(c);

                    if (category == UnicodeCategory.ConnectorPunctuation ||
                        category == UnicodeCategory.Format ||
                        category == UnicodeCategory.NonSpacingMark ||
                        category == UnicodeCategory.SpacingCombiningMark) {
                        location++;
                        continue;
                    }

                    break;
                }

                outputStream->Add(new Token() {
                    tokenType = TokenType.KeywordOrIdentifier,
                    length = location - start,
                    charIndex = start,
                    lineIndex = lineIndex,
                    columnIndex = start - linePosition,
                    extra = new TokenExtraData()
                });

                return true;
            }

            return false;
        }

        private bool TryReadIdentifier() {
            if (MatchIdentifier(input, location, out int length)) {
                outputStream->Add(new Token() {
                    tokenType = TokenType.KeywordOrIdentifier,
                    length = length,
                    charIndex = location,
                    lineIndex = lineIndex,
                    columnIndex = location - linePosition,
                    extra = new TokenExtraData()
                });

                location += length;

                return true;
            }

            return false;
        }

        private bool TryReadDollarIdentifier() {
            if (input[location] != '$') {
                return false;
            }

            if (location + 1 >= input.size) {
                return false;
            }

            // todo -- maybe map to a runtime keyword

            if (MatchIdentifier(input, location + 1, out int length)) {
                length++; // account for prefix

                outputStream->Add(new Token() {
                    tokenType = TokenType.DollarIdentifier,
                    length = length,
                    charIndex = location,
                    lineIndex = lineIndex,
                    columnIndex = location - linePosition,
                    extra = default
                });

                location += length;
                return true;
            }

            return false;
        }

        public static bool IsIdentifierCharacter(char c, bool allowDash) {
            if (char.IsLetterOrDigit(c) || c == '_' || (allowDash && c == '-')) {
                return true;
            }

            UnicodeCategory category = char.GetUnicodeCategory(c);

            if (category == UnicodeCategory.ConnectorPunctuation ||
                category == UnicodeCategory.Format ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark) {
                return true;
            }

            return false;
        }

        private static bool MatchIdentifier(FixedCharacterSpan span, int i, out int length) {
            length = 0;
            int start = i;

            if (!char.IsLetter(span[i]) && span[i] != '_') {
                return false;
            }

            i++;

            // fragment Identifier_Part_Character
            //     : Letter_Character
            //     | Decimal_Digit_Character
            //     | Connecting_Character -> Pc category
            //     | Combining_Character -> Mn or Mc categories
            //     | Formatting_Character
            //     ;

            while (i < span.size) {
                char c = span[i];

                if (char.IsLetterOrDigit(c) || c == '_') {
                    i++;
                    continue;
                }

                UnicodeCategory category = char.GetUnicodeCategory(c);

                if (category == UnicodeCategory.ConnectorPunctuation ||
                    category == UnicodeCategory.Format ||
                    category == UnicodeCategory.NonSpacingMark ||
                    category == UnicodeCategory.SpacingCombiningMark) {
                    i++;
                    continue;
                }

                break;
            }

            if ((i - start) == 1 && span[start] == '_') {
                return false;
            }

            length = i - start;
            return true;
        }

        private bool TryReadWhitespace() {
            // : [\p{Zs}]  // any character with Unicode class Zs
            // | '\u0009'  // horizontal tab
            // | '\u000B'  // vertical tab
            // | '\u000C'  // form feed

            int whitespaceCount = 0;
            int startLocation = location;

            while (HasMoreInput) {
                char c = input[location];
                if (char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator ||
                    c == '\u0009' || // horizontal tab
                    c == '\u000B' || // vertical tab
                    c == '\u000C' // form feed
                   ) {
                    whitespaceCount++;
                    location++;
                    continue;
                }

                break;
            }

            if (whitespaceCount != 0) {
                outputStream->Add(new Token() {
                    tokenType = TokenType.Whitespace,
                    length = whitespaceCount,
                    charIndex = startLocation,
                    columnIndex = startLocation - linePosition,
                    lineIndex = lineIndex
                });
                return true;
            }

            return false;
        }

        private bool TryReadKeyword() {
            if (StringTokens.Match(input, location, out KeywordMatch match)) {
                outputStream->Add(new Token() {
                    tokenType = TokenType.KeywordOrIdentifier,
                    length = match.length,
                    charIndex = location,
                    columnIndex = location - linePosition,
                    lineIndex = lineIndex,
                    extra = new TokenExtraData(match.keyword)
                });
                location += match.length;
                return true;
            }

            return false;
        }

        private bool Fragment_HexEscapeSequence() {
            if (!InputRemainingAtLeast(3)) return false;

            char slash = input[location + 0];
            char x = input[location + 1];

            if (slash != '\\' && x != 'x') {
                return false;
            }

            // todo -- not sure I really want a stack for state saving, probably just store a reference on the stack
            // TokenizerState state = SaveState();
            //
            // location += 2;
            // if (!Fragment_HexDigit()) {
            //     RestoreState(state);
            //     return false;
            // }
            //
            // AtMost(ParseRule.HexDigit, 3);

            return true;
        }

        private static bool MatchHexDigit(FixedCharacterSpan input, int ptr) {
            if (ptr >= input.size) {
                return false;
            }

            switch (input[ptr]) {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':

                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':

                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                    return true;
            }

            return false;
        }

        private bool TryParseIntegerTypeSuffix(out LiteralType suffixType) {
            suffixType = default;

            if (HasMoreInput) {
                if (InputRemainingAtLeast(2)) {
                    char c0 = input[location + 0];
                    char c1 = input[location + 1];

                    bool isU = c0 == 'u' || c0 == 'U';
                    bool isL = c1 == 'l' || c1 == 'L';

                    if (isU && isL) {
                        suffixType = LiteralType.UnsignedLong;
                        location += 2;
                        return true;
                    }

                    isL = c0 == 'l' || c0 == 'L';
                    isU = c1 == 'u' || c1 == 'U';

                    if (isU && isL) {
                        suffixType = LiteralType.UnsignedLong;
                        location += 2;
                        return true;
                    }
                }

                char c = input[location];
                if (c == 'u' || c == 'U') {
                    suffixType = LiteralType.UnsignedInteger;
                    location++;
                    return true;
                }

                if (c == 'l' || c == 'L') {
                    suffixType = LiteralType.Long;
                    location++;
                    return true;
                }
            }

            return false;
        }

        private bool TryReadNewLine() {
            int start = location;
            int newLineCount = 0;
            while (location < input.size) {
                char c = input[location];

                if (c == '\r' && location + 1 < input.size && input[location + 1] == '\n') {
                    location += 2;
                    newLineCount++;
                }
                else if (c == '\n' || MatchCharacters('\u000D', '\u000A', '\u0085', '\u2028', '\u2029')) {
                    location++;
                    newLineCount++;
                }
                else {
                    break;
                }
            }

            if (start != location) {
                outputStream->Add(new Token() {
                    tokenType = TokenType.NewLine,
                    columnIndex = start - linePosition,
                    length = location - start,
                    lineIndex = lineIndex,
                    charIndex = start
                });
                lineIndex += newLineCount;
                linePosition = location;
                return true;
            }

            return false;
        }

        private bool MatchCharacters(char c0, char c1, char c2, char c3, char c4) {
            return input[location] == c0 || input[location] == c1 || input[location] == c2 || input[location] == c3 || input[location] == c4;
        }

        private bool InputRemainingAtLeast(int i) {
            return location + i <= input.size;
        }

    }

}