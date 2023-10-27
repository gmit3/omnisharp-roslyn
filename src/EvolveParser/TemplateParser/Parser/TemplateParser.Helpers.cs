using System;
using EvolveUI.Compiler;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal partial struct TemplateParser {

        private interface IExpressionSnippetParser {

            HelpType SyntaxHelp { get; }
            bool Optional { get; }

            bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out ExpressionIndex result);

        }

        private interface IExpressionSnippetParser<T> where T : unmanaged, IExpressionNode {

            HelpType SyntaxHelp { get; }
            bool Optional { get; }

            bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out ExpressionIndex<T> result);

        }

        private interface ISnippetParser<T> where T : unmanaged, ITemplateNode {

            HelpType SyntaxHelp { get; }
            bool Optional { get; }

            bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out NodeIndex<T> result);

        }

        private bool ParseSquareBracketListWithRecovery<T, U>(ref TokenStream tokenStream, ref U logic, out NodeRange<T> output) where T : unmanaged, ITemplateNode where U : ISnippetParser<T> {
            return ParseListWithRecovery(SubStreamType.SquareBrackets, SymbolType.SquareBraceOpen, DiagnosticError.ExpectedSquareBracket, DiagnosticError.UnmatchedSquareBracket, ref tokenStream, ref logic, out output);
        }

        private bool ParseAngleBracketListWithRecovery<T, U>(ref TokenStream tokenStream, ref U logic, out NodeRange<T> output) where T : unmanaged, ITemplateNode where U : ISnippetParser<T> {
            return ParseListWithRecovery(SubStreamType.AngleBrackets, SymbolType.LessThan, DiagnosticError.ExpectedAngleBracket, DiagnosticError.UnmatchedAngleBracket, ref tokenStream, ref logic, out output);
        }

        private bool ParseAngleBracketListWithRecovery<T, U>(ref TokenStream tokenStream, ref U logic, out ExpressionRange<T> output) where T : unmanaged, IExpressionNode where U : IExpressionSnippetParser<T> {
            return ParseExpressionListWithRecovery(SubStreamType.AngleBrackets, SymbolType.LessThan, DiagnosticError.ExpectedAngleBracket, DiagnosticError.UnmatchedAngleBracket, ref tokenStream, ref logic, out output);
        }

        private bool ParseCurlyBraceListWithRecovery<T, U>(ref TokenStream tokenStream, ref U logic, out NodeRange<T> output) where T : unmanaged, ITemplateNode where U : ISnippetParser<T> {
            return ParseListWithRecovery(SubStreamType.CurlyBraces, SymbolType.CurlyBraceOpen, DiagnosticError.ExpectedCurlyBrace, DiagnosticError.UnmatchedCurlyBrace, ref tokenStream, ref logic, out output);
        }

        private bool ParseParenListWithRecovery<T, U>(ref TokenStream tokenStream, ref U logic, out NodeRange<T> output) where T : unmanaged, ITemplateNode where U : ISnippetParser<T> {
            return ParseListWithRecovery(SubStreamType.Parens, SymbolType.OpenParen, DiagnosticError.ExpectedParenthesis, DiagnosticError.UnmatchedParentheses, ref tokenStream, ref logic, out output);
        }

        private bool ParseParenExpressionListWithRecovery<T>(ref TokenStream tokenStream, ref T logic, out ExpressionRange output) where T : IExpressionSnippetParser {
            return ParseExpressionListWithRecovery(SubStreamType.Parens, SymbolType.OpenParen, DiagnosticError.ExpectedParenthesis, DiagnosticError.UnmatchedParentheses, ref tokenStream, ref logic, out output);
        }

        private bool ParseExpressionListWithRecovery<U>(SubStreamType subStreamType, SymbolType peekSymbol, DiagnosticError expectation, DiagnosticError unmatched, ref TokenStream tokenStream, ref U logic, out ExpressionRange output)
            where U : IExpressionSnippetParser {
            output = default;

            if (!tokenStream.Peek(peekSymbol)) {
                return logic.Optional || HardError(tokenStream, expectation, logic.SyntaxHelp);
            }

            if (!tokenStream.TryGetSubStream(subStreamType, out TokenStream parameterStream)) {
                return HardError(tokenStream, unmatched, logic.SyntaxHelp);
            }

            if (parameterStream.IsEmpty) {
                return true;
            }

            using ScopedList<ExpressionIndex> list = scopedAllocator.CreateListScope<ExpressionIndex>(8);

            while (parameterStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                if (logic.Parse(ref nextStream, ref this, out ExpressionIndex result)) {
                    list.Add(result);
                }

                PopRecoveryPoint(ref nextStream);

                if (!CheckDanglingComma(ref parameterStream)) {
                    return false;
                }
            }

            if (parameterStream.HasMoreTokens) {
                return HardError(parameterStream, DiagnosticError.UnexpectedToken);
            }

            output = expressionBuffer.AddExpressionList(list);

            return true;
        }

        private bool GetRequiredSubStream(ref TokenStream tokenStream, SubStreamType subStreamType, HelpType helpData, out TokenStream substream) {
            SymbolType symbolType = default;
            DiagnosticError expectation = default;
            DiagnosticError unmatched = default;
            switch (subStreamType) {
                case SubStreamType.Parens:
                    symbolType = SymbolType.OpenParen;
                    expectation = DiagnosticError.ExpectedParenthesis;
                    unmatched = DiagnosticError.UnmatchedParentheses;
                    break;

                case SubStreamType.SquareBrackets:
                    symbolType = SymbolType.SquareBraceOpen;
                    expectation = DiagnosticError.ExpectedSquareBracket;
                    unmatched = DiagnosticError.UnmatchedSquareBracket;
                    break;

                case SubStreamType.CurlyBraces:
                    symbolType = SymbolType.CurlyBraceOpen;
                    expectation = DiagnosticError.ExpectedCurlyBrace;
                    unmatched = DiagnosticError.UnmatchedCurlyBrace;
                    break;

                case SubStreamType.AngleBrackets:
                    symbolType = SymbolType.LessThan;
                    expectation = DiagnosticError.ExpectedAngleBracket;
                    unmatched = DiagnosticError.UnmatchedAngleBracket;
                    break;
            }

            if (!tokenStream.Peek(symbolType)) {
                substream = default;
                return HardError(tokenStream, expectation, helpData);
            }

            if (!tokenStream.TryGetSubStream(subStreamType, out substream)) {
                return HardError(tokenStream, unmatched, helpData);
            }

            return true;
        }

        private bool ParseListWithRecovery<T, U>(SubStreamType subStreamType, SymbolType peekSymbol, DiagnosticError expectation, DiagnosticError unmatched, ref TokenStream tokenStream, ref U logic, out NodeRange<T> output)
            where T : unmanaged, ITemplateNode where U : ISnippetParser<T> {
            output = default;

            if (!tokenStream.Peek(peekSymbol)) {
                return logic.Optional || HardError(tokenStream, expectation, logic.SyntaxHelp);
            }

            if (!tokenStream.TryGetSubStream(subStreamType, out TokenStream parameterStream)) {
                return HardError(tokenStream, unmatched, logic.SyntaxHelp);
            }

            if (parameterStream.IsEmpty) {
                return true;
            }

            using ScopedList<NodeIndex<T>> list = scopedAllocator.CreateListScope<NodeIndex<T>>(8);

            while (parameterStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                if (logic.Parse(ref nextStream, ref this, out NodeIndex<T> result)) {
                    list.Add(result);

                }

                PopRecoveryPoint(ref nextStream);

                if (!CheckDanglingComma(ref parameterStream)) {
                    return false;
                }
            }

            if (parameterStream.HasMoreTokens) {
                return HardError(parameterStream, DiagnosticError.UnexpectedToken);
            }

            output = templateBuffer.AddNodeList(list);

            return true;
        }

        private bool ParseExpressionListWithRecovery<T, U>(SubStreamType subStreamType, SymbolType peekSymbol, DiagnosticError expectation, DiagnosticError unmatched, ref TokenStream tokenStream, ref U logic, out ExpressionRange<T> output)
            where T : unmanaged, IExpressionNode where U : IExpressionSnippetParser<T> {
            output = default;

            if (!tokenStream.Peek(peekSymbol)) {
                return logic.Optional || HardError(tokenStream, expectation, logic.SyntaxHelp);
            }

            if (!tokenStream.TryGetSubStream(subStreamType, out TokenStream parameterStream)) {
                return HardError(tokenStream, unmatched, logic.SyntaxHelp);
            }

            if (parameterStream.IsEmpty) {
                return true;
            }

            using ScopedList<ExpressionIndex<T>> list = scopedAllocator.CreateListScope<ExpressionIndex<T>>(8);

            while (parameterStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                if (logic.Parse(ref nextStream, ref this, out ExpressionIndex<T> result)) {
                    list.Add(result);

                }

                PopRecoveryPoint(ref nextStream);

                if (!CheckDanglingComma(ref parameterStream)) {
                    return false;
                }
            }

            if (parameterStream.HasMoreTokens) {
                return HardError(parameterStream, DiagnosticError.UnexpectedToken);
            }

            output = expressionBuffer.AddExpressionList(list);

            return true;
        }

    }

}