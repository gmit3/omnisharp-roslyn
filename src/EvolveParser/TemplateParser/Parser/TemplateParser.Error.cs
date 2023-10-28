using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EvolveUI.Compiler;
using EvolveUI.Util;

namespace EvolveUI.Parsing {
    public unsafe ref partial struct TemplateParser {

        public static string TitleCaseToDisplayString(string input, bool lowerFirstLetter = true) {
            string str = Regex.Replace(Regex.Replace(input, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
            string[] strs = str.Split(' ');
            int start = 1;
            if (lowerFirstLetter) start = 0;
            for (int i = start; i < strs.Length; i++) {
                strs[i] = strs[i].ToLower()[0] + strs[i].Substring(1);
            }

            return string.Join(' ', strs);
        }

        public static string TranslateError(HardErrorInfo error, CheckedArray<Token> tokens, CheckedArray<Token> nonTrivialTokens, CheckedArray<char> source) {
            string context;

            error.location = nonTrivialTokens[error.location].tokenIndex;
            error.lastValidLocation = nonTrivialTokens[error.lastValidLocation].tokenIndex;
            string primaryErrorSpan = "";

            switch (error.error) {
                case DiagnosticError.None:
                    return "";

                case DiagnosticError.UnexpectedToken: {
                    Token unexpected = tokens[error.location];
                    context = TitleCaseToDisplayString(error.error.ToString(), false);
                    primaryErrorSpan = context;
                    context += " `" + new string(source.GetArrayPointer(), unexpected.charIndex, unexpected.length) + "`";
                    break;
                }

                case DiagnosticError.StuckOnUnexpectedToken: {
                    Token unexpected = tokens[error.location];
                    context = TitleCaseToDisplayString(error.error.ToString(), false);
                    primaryErrorSpan = context;
                    context += " `" + new string(source.GetArrayPointer(), unexpected.charIndex, unexpected.length) + "`";
                    break;
                }

                default:
                    context = TitleCaseToDisplayString(error.error.ToString(), false);
                    primaryErrorSpan = context;
                    break;
            }

            context += " at ";

            context += tokens[error.location].lineIndex + ":" + tokens[error.location].columnIndex;

            context += "\nFull Context:\n";
            context += "<color=green>file: Evolve/Tests/TestTemplate.ui</color>\n";

            Token endToken = tokens[error.location];

            List<string> lines = new string(source.GetArrayPointer(), 0, source.size).Split('\n').ToList();
            lines.Insert(0, "");

            int lineStart = MathUtil.Max(1, endToken.lineIndex - 5);

            for (int i = lineStart; i <= endToken.lineIndex; i++) {
                context += i.ToString("0000");
                context += "|";
                context += lines[i];
                context += "\n";
            }

            context += "<color=red>error";

            if (error.adjustment != ErrorLocationAdjustment.None) {
                if (error.adjustment == ErrorLocationAdjustment.TokenEnd) {
                    context += new string(' ', endToken.columnIndex + endToken.length - 1);
                }
            }
            else {
                context += new string(' ', endToken.columnIndex);
            }

            context += "^ " + primaryErrorSpan + "</color>";
            context += "\n";
            int maxLine = MathUtil.Min(endToken.lineIndex + 5, lines.Count);
            for (int i = endToken.lineIndex + 1; i < maxLine; i++) {
                context += i.ToString("0000");
                context += "|";
                context += lines[i];
                context += "\n";
            }

            //if (error.help != default) {
            context += "<color=blue>help: ";
            context += " do something different";
            context += "</color>";
            //}

            context += "\n\n\n"; // space before the debug log 

            return context;
        }

    }

}
