using System;
using EvolveUI.Util;

namespace EvolveUI.Parsing {

    internal unsafe partial struct StringTokens {
        
         static partial void MatchKeywordGenerated(FixedCharacterSpan input, int location, ref  KeywordMatch keywordMatch) {
            keywordMatch = default;
            char * buffer = stackalloc char[16];
            switch(input[location]) {
                case 't': {
                if(location + 10 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'y';
                    buffer[2] = 'p';
                    buffer[3] = 'o';
                    buffer[4] = 'g';
                    buffer[5] = 'r';
                    buffer[6] = 'a';
                    buffer[7] = 'p';
                    buffer[8] = 'h';
                    buffer[9] = 'y';
                    if(Match(input, location, 10, buffer, TemplateKeyword.Typography, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'e';
                    buffer[2] = 'm';
                    buffer[3] = 'p';
                    buffer[4] = 'l';
                    buffer[5] = 'a';
                    buffer[6] = 't';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Template, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'e';
                    buffer[2] = 'l';
                    buffer[3] = 'e';
                    buffer[4] = 'p';
                    buffer[5] = 'o';
                    buffer[6] = 'r';
                    buffer[7] = 't';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Teleport, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'y';
                    buffer[2] = 'p';
                    buffer[3] = 'e';
                    buffer[4] = 'o';
                    buffer[5] = 'f';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Typeof, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'h';
                    buffer[2] = 'r';
                    buffer[3] = 'o';
                    buffer[4] = 'w';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Throw, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'h';
                    buffer[2] = 'i';
                    buffer[3] = 's';
                    if(Match(input, location, 4, buffer, TemplateKeyword.This, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'e';
                    buffer[2] = 'x';
                    buffer[3] = 't';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Text, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 't';
                    buffer[1] = 'r';
                    buffer[2] = 'y';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Try, ref keywordMatch)) return;
                }
                break;
                }
                case 'e': {
                if(location + 10 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    buffer[3] = 'l';
                    buffer[4] = 'y';
                    buffer[5] = 'I';
                    buffer[6] = 'n';
                    buffer[7] = 'p';
                    buffer[8] = 'u';
                    buffer[9] = 't';
                    if(Match(input, location, 10, buffer, TemplateKeyword.EarlyInput, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'x';
                    buffer[2] = 'p';
                    buffer[3] = 'l';
                    buffer[4] = 'i';
                    buffer[5] = 'c';
                    buffer[6] = 'i';
                    buffer[7] = 't';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Explicit, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'l';
                    buffer[2] = 'e';
                    buffer[3] = 'm';
                    buffer[4] = 'e';
                    buffer[5] = 'n';
                    buffer[6] = 't';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Element, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'x';
                    buffer[2] = 't';
                    buffer[3] = 'r';
                    buffer[4] = 'u';
                    buffer[5] = 'd';
                    buffer[6] = 'e';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Extrude, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'n';
                    buffer[2] = 'a';
                    buffer[3] = 'b';
                    buffer[4] = 'l';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Enable, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'x';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'n';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Extern, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'v';
                    buffer[2] = 'e';
                    buffer[3] = 'n';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Event, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'n';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Enter, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    buffer[3] = 'l';
                    buffer[4] = 'y';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Early, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'l';
                    buffer[2] = 's';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Else, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'n';
                    buffer[2] = 'u';
                    buffer[3] = 'm';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Enum, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'x';
                    buffer[2] = 'i';
                    buffer[3] = 't';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Exit, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'e';
                    buffer[1] = 'n';
                    buffer[2] = 'd';
                    if(Match(input, location, 3, buffer, TemplateKeyword.End, ref keywordMatch)) return;
                }
                break;
                }
                case 'r': {
                if(location + 8 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 'a';
                    buffer[3] = 'd';
                    buffer[4] = 'o';
                    buffer[5] = 'n';
                    buffer[6] = 'l';
                    buffer[7] = 'y';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Readonly, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 'q';
                    buffer[3] = 'u';
                    buffer[4] = 'i';
                    buffer[5] = 'r';
                    buffer[6] = 'e';
                    buffer[7] = 'd';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Required, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 'n';
                    buffer[3] = 'd';
                    buffer[4] = 'e';
                    buffer[5] = 'r';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Render, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 'p';
                    buffer[3] = 'e';
                    buffer[4] = 'a';
                    buffer[5] = 't';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Repeat, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 't';
                    buffer[3] = 'u';
                    buffer[4] = 'r';
                    buffer[5] = 'n';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Return, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'u';
                    buffer[2] = 'n';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Run, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'r';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Ref, ref keywordMatch)) return;
                }
                break;
                }
                case 's': {
                if(location + 10 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'a';
                    buffer[3] = 'c';
                    buffer[4] = 'k';
                    buffer[5] = 'a';
                    buffer[6] = 'l';
                    buffer[7] = 'l';
                    buffer[8] = 'o';
                    buffer[9] = 'c';
                    if(Match(input, location, 10, buffer, TemplateKeyword.Stackalloc, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'e';
                    buffer[3] = 'p';
                    buffer[4] = 'S';
                    buffer[5] = 'i';
                    buffer[6] = 'z';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.StepSize, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'w';
                    buffer[2] = 'i';
                    buffer[3] = 't';
                    buffer[4] = 'c';
                    buffer[5] = 'h';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Switch, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'a';
                    buffer[3] = 't';
                    buffer[4] = 'i';
                    buffer[5] = 'c';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Static, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'r';
                    buffer[3] = 'u';
                    buffer[4] = 'c';
                    buffer[5] = 't';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Struct, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'r';
                    buffer[3] = 'i';
                    buffer[4] = 'n';
                    buffer[5] = 'g';
                    if(Match(input, location, 6, buffer, TemplateKeyword.String, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'i';
                    buffer[2] = 'z';
                    buffer[3] = 'e';
                    buffer[4] = 'o';
                    buffer[5] = 'f';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Sizeof, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'e';
                    buffer[2] = 'a';
                    buffer[3] = 'l';
                    buffer[4] = 'e';
                    buffer[5] = 'd';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Sealed, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'c';
                    buffer[2] = 'r';
                    buffer[3] = 'o';
                    buffer[4] = 'l';
                    buffer[5] = 'l';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Scroll, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'p';
                    buffer[2] = 'a';
                    buffer[3] = 'w';
                    buffer[4] = 'n';
                    buffer[5] = 's';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Spawns, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'a';
                    buffer[3] = 't';
                    buffer[4] = 'e';
                    if(Match(input, location, 5, buffer, TemplateKeyword.State, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'y';
                    buffer[3] = 'l';
                    buffer[4] = 'e';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Style, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'h';
                    buffer[2] = 'o';
                    buffer[3] = 'r';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Short, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'b';
                    buffer[2] = 'y';
                    buffer[3] = 't';
                    buffer[4] = 'e';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Sbyte, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'h';
                    buffer[2] = 'i';
                    buffer[3] = 'f';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Shift, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 't';
                    buffer[2] = 'a';
                    buffer[3] = 'r';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Start, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'l';
                    buffer[2] = 'o';
                    buffer[3] = 't';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Slot, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 's';
                    buffer[1] = 'y';
                    buffer[2] = 'n';
                    buffer[3] = 'c';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Sync, ref keywordMatch)) return;
                }
                break;
                }
                case 'a': {
                if(location + 15 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'E';
                    buffer[6] = 'a';
                    buffer[7] = 'r';
                    buffer[8] = 'l';
                    buffer[9] = 'y';
                    buffer[10] = 'I';
                    buffer[11] = 'n';
                    buffer[12] = 'p';
                    buffer[13] = 'u';
                    buffer[14] = 't';
                    if(Match(input, location, 15, buffer, TemplateKeyword.AfterEarlyInput, ref keywordMatch)) return;
                }
                if(location + 14 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'L';
                    buffer[6] = 'a';
                    buffer[7] = 't';
                    buffer[8] = 'e';
                    buffer[9] = 'I';
                    buffer[10] = 'n';
                    buffer[11] = 'p';
                    buffer[12] = 'u';
                    buffer[13] = 't';
                    if(Match(input, location, 14, buffer, TemplateKeyword.AfterLateInput, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'D';
                    buffer[6] = 'i';
                    buffer[7] = 's';
                    buffer[8] = 'a';
                    buffer[9] = 'b';
                    buffer[10] = 'l';
                    buffer[11] = 'e';
                    if(Match(input, location, 12, buffer, TemplateKeyword.AfterDisable, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'D';
                    buffer[6] = 'e';
                    buffer[7] = 's';
                    buffer[8] = 't';
                    buffer[9] = 'r';
                    buffer[10] = 'o';
                    buffer[11] = 'y';
                    if(Match(input, location, 12, buffer, TemplateKeyword.AfterDestroy, ref keywordMatch)) return;
                }
                if(location + 11 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'E';
                    buffer[6] = 'n';
                    buffer[7] = 'a';
                    buffer[8] = 'b';
                    buffer[9] = 'l';
                    buffer[10] = 'e';
                    if(Match(input, location, 11, buffer, TemplateKeyword.AfterEnable, ref keywordMatch)) return;
                }
                if(location + 11 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'C';
                    buffer[6] = 'r';
                    buffer[7] = 'e';
                    buffer[8] = 'a';
                    buffer[9] = 't';
                    buffer[10] = 'e';
                    if(Match(input, location, 11, buffer, TemplateKeyword.AfterCreate, ref keywordMatch)) return;
                }
                if(location + 11 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'U';
                    buffer[6] = 'p';
                    buffer[7] = 'd';
                    buffer[8] = 'a';
                    buffer[9] = 't';
                    buffer[10] = 'e';
                    if(Match(input, location, 11, buffer, TemplateKeyword.AfterUpdate, ref keywordMatch)) return;
                }
                if(location + 11 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'F';
                    buffer[6] = 'i';
                    buffer[7] = 'n';
                    buffer[8] = 'i';
                    buffer[9] = 's';
                    buffer[10] = 'h';
                    if(Match(input, location, 11, buffer, TemplateKeyword.AfterFinish, ref keywordMatch)) return;
                }
                if(location + 10 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'I';
                    buffer[6] = 'n';
                    buffer[7] = 'p';
                    buffer[8] = 'u';
                    buffer[9] = 't';
                    if(Match(input, location, 10, buffer, TemplateKeyword.AfterInput, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'b';
                    buffer[2] = 's';
                    buffer[3] = 't';
                    buffer[4] = 'r';
                    buffer[5] = 'a';
                    buffer[6] = 'c';
                    buffer[7] = 't';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Abstract, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 's';
                    buffer[2] = 'y';
                    buffer[3] = 'n';
                    buffer[4] = 'c';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Async, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'w';
                    buffer[2] = 'a';
                    buffer[3] = 'i';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Await, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'f';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    if(Match(input, location, 5, buffer, TemplateKeyword.After, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 't';
                    buffer[2] = 't';
                    buffer[3] = 'r';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Attr, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'l';
                    buffer[2] = 't';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Alt, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 'n';
                    buffer[2] = 'd';
                    if(Match(input, location, 3, buffer, TemplateKeyword.And, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'a';
                    buffer[1] = 's';
                    if(Match(input, location, 2, buffer, TemplateKeyword.As, ref keywordMatch)) return;
                }
                break;
                }
                case 'p': {
                if(location + 10 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'o';
                    buffer[2] = 's';
                    buffer[3] = 't';
                    buffer[4] = 'U';
                    buffer[5] = 'p';
                    buffer[6] = 'd';
                    buffer[7] = 'a';
                    buffer[8] = 't';
                    buffer[9] = 'e';
                    if(Match(input, location, 10, buffer, TemplateKeyword.PostUpdate, ref keywordMatch)) return;
                }
                if(location + 9 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'r';
                    buffer[2] = 'o';
                    buffer[3] = 't';
                    buffer[4] = 'e';
                    buffer[5] = 'c';
                    buffer[6] = 't';
                    buffer[7] = 'e';
                    buffer[8] = 'd';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Protected, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'r';
                    buffer[2] = 'i';
                    buffer[3] = 'v';
                    buffer[4] = 'a';
                    buffer[5] = 't';
                    buffer[6] = 'e';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Private, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'o';
                    buffer[2] = 'r';
                    buffer[3] = 't';
                    buffer[4] = 'a';
                    buffer[5] = 'l';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Portal, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    buffer[3] = 'a';
                    buffer[4] = 'm';
                    buffer[5] = 's';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Params, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'p';
                    buffer[1] = 'u';
                    buffer[2] = 'b';
                    buffer[3] = 'l';
                    buffer[4] = 'i';
                    buffer[5] = 'c';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Public, ref keywordMatch)) return;
                }
                break;
                }
                case 'm': {
                if(location + 6 <= input.size) { 
                    buffer[0] = 'm';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    buffer[3] = 'k';
                    buffer[4] = 'e';
                    buffer[5] = 'r';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Marker, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'm';
                    buffer[1] = 'e';
                    buffer[2] = 't';
                    buffer[3] = 'h';
                    buffer[4] = 'o';
                    buffer[5] = 'd';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Method, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'm';
                    buffer[1] = 'o';
                    buffer[2] = 'u';
                    buffer[3] = 's';
                    buffer[4] = 'e';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Mouse, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'm';
                    buffer[1] = 'a';
                    buffer[2] = 't';
                    buffer[3] = 'c';
                    buffer[4] = 'h';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Match, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'm';
                    buffer[1] = 'o';
                    buffer[2] = 'v';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Move, ref keywordMatch)) return;
                }
                break;
                }
                case 'd': {
                if(location + 11 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 's';
                    buffer[3] = 't';
                    buffer[4] = 'r';
                    buffer[5] = 'u';
                    buffer[6] = 'c';
                    buffer[7] = 't';
                    buffer[8] = 'i';
                    buffer[9] = 'v';
                    buffer[10] = 'e';
                    if(Match(input, location, 11, buffer, TemplateKeyword.Destructive, ref keywordMatch)) return;
                }
                if(location + 9 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 'c';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'a';
                    buffer[6] = 't';
                    buffer[7] = 'o';
                    buffer[8] = 'r';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Decorator, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 'l';
                    buffer[3] = 'e';
                    buffer[4] = 'g';
                    buffer[5] = 'a';
                    buffer[6] = 't';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Delegate, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'a';
                    buffer[4] = 'u';
                    buffer[5] = 'l';
                    buffer[6] = 't';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Default, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 'c';
                    buffer[3] = 'i';
                    buffer[4] = 'm';
                    buffer[5] = 'a';
                    buffer[6] = 'l';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Decimal, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'i';
                    buffer[2] = 's';
                    buffer[3] = 'a';
                    buffer[4] = 'b';
                    buffer[5] = 'l';
                    buffer[6] = 'e';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Disable, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 's';
                    buffer[3] = 't';
                    buffer[4] = 'r';
                    buffer[5] = 'o';
                    buffer[6] = 'y';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Destroy, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'y';
                    buffer[2] = 'n';
                    buffer[3] = 'a';
                    buffer[4] = 'm';
                    buffer[5] = 'i';
                    buffer[6] = 'c';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Dynamic, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'o';
                    buffer[2] = 'u';
                    buffer[3] = 'b';
                    buffer[4] = 'l';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Double, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Defer, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'o';
                    buffer[2] = 'w';
                    buffer[3] = 'n';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Down, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'r';
                    buffer[2] = 'a';
                    buffer[3] = 'g';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Drag, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'r';
                    buffer[2] = 'o';
                    buffer[3] = 'p';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Drop, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'd';
                    buffer[1] = 'o';
                    if(Match(input, location, 2, buffer, TemplateKeyword.Do, ref keywordMatch)) return;
                }
                break;
                }
                case 'c': {
                if(location + 11 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 'd';
                    buffer[4] = 'i';
                    buffer[5] = 't';
                    buffer[6] = 'i';
                    buffer[7] = 'o';
                    buffer[8] = 'n';
                    buffer[9] = 'a';
                    buffer[10] = 'l';
                    if(Match(input, location, 11, buffer, TemplateKeyword.Conditional, ref keywordMatch)) return;
                }
                if(location + 9 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 't';
                    buffer[4] = 'a';
                    buffer[5] = 'i';
                    buffer[6] = 'n';
                    buffer[7] = 'e';
                    buffer[8] = 'r';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Container, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 't';
                    buffer[4] = 'i';
                    buffer[5] = 'n';
                    buffer[6] = 'u';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Continue, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'm';
                    buffer[3] = 'p';
                    buffer[4] = 'u';
                    buffer[5] = 't';
                    buffer[6] = 'e';
                    buffer[7] = 'd';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Computed, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'h';
                    buffer[2] = 'e';
                    buffer[3] = 'c';
                    buffer[4] = 'k';
                    buffer[5] = 'e';
                    buffer[6] = 'd';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Checked, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 't';
                    buffer[4] = 'e';
                    buffer[5] = 'x';
                    buffer[6] = 't';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Context, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'm';
                    buffer[3] = 'p';
                    buffer[4] = 'i';
                    buffer[5] = 'l';
                    buffer[6] = 'e';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Compile, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'r';
                    buffer[2] = 'e';
                    buffer[3] = 'a';
                    buffer[4] = 't';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Create, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'a';
                    buffer[2] = 'n';
                    buffer[3] = 'c';
                    buffer[4] = 'e';
                    buffer[5] = 'l';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Cancel, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 's';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Const, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'a';
                    buffer[2] = 't';
                    buffer[3] = 'c';
                    buffer[4] = 'h';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Catch, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'l';
                    buffer[2] = 'a';
                    buffer[3] = 's';
                    buffer[4] = 's';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Class, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'l';
                    buffer[2] = 'i';
                    buffer[3] = 'c';
                    buffer[4] = 'k';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Click, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'o';
                    buffer[2] = 'u';
                    buffer[3] = 'n';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Count, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'a';
                    buffer[2] = 's';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Case, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'h';
                    buffer[2] = 'a';
                    buffer[3] = 'r';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Char, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 't';
                    buffer[2] = 'r';
                    buffer[3] = 'l';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Ctrl, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'c';
                    buffer[1] = 'm';
                    buffer[2] = 'd';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Cmd, ref keywordMatch)) return;
                }
                break;
                }
                case 'i': {
                if(location + 9 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'f';
                    buffer[6] = 'a';
                    buffer[7] = 'c';
                    buffer[8] = 'e';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Interface, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'm';
                    buffer[2] = 'p';
                    buffer[3] = 'l';
                    buffer[4] = 'i';
                    buffer[5] = 'c';
                    buffer[6] = 'i';
                    buffer[7] = 't';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Implicit, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    buffer[5] = 'n';
                    buffer[6] = 'a';
                    buffer[7] = 'l';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Internal, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'm';
                    buffer[2] = 'p';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 't';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Import, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    buffer[2] = 'v';
                    buffer[3] = 'o';
                    buffer[4] = 'k';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Invoke, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    buffer[2] = 'p';
                    buffer[3] = 'u';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Input, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    buffer[2] = 't';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Int, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'f';
                    if(Match(input, location, 2, buffer, TemplateKeyword.If, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 's';
                    if(Match(input, location, 2, buffer, TemplateKeyword.Is, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'i';
                    buffer[1] = 'n';
                    if(Match(input, location, 2, buffer, TemplateKeyword.In, ref keywordMatch)) return;
                }
                break;
                }
                case 'f': {
                if(location + 8 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'u';
                    buffer[2] = 'n';
                    buffer[3] = 'c';
                    buffer[4] = 't';
                    buffer[5] = 'i';
                    buffer[6] = 'o';
                    buffer[7] = 'n';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Function, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'o';
                    buffer[2] = 'r';
                    buffer[3] = 'e';
                    buffer[4] = 'a';
                    buffer[5] = 'c';
                    buffer[6] = 'h';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Foreach, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'i';
                    buffer[2] = 'n';
                    buffer[3] = 'a';
                    buffer[4] = 'l';
                    buffer[5] = 'l';
                    buffer[6] = 'y';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Finally, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'i';
                    buffer[2] = 'n';
                    buffer[3] = 'i';
                    buffer[4] = 's';
                    buffer[5] = 'h';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Finish, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'l';
                    buffer[2] = 'o';
                    buffer[3] = 'a';
                    buffer[4] = 't';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Float, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'i';
                    buffer[2] = 'x';
                    buffer[3] = 'e';
                    buffer[4] = 'd';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Fixed, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'o';
                    buffer[2] = 'c';
                    buffer[3] = 'u';
                    buffer[4] = 's';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Focus, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'r';
                    buffer[2] = 'o';
                    buffer[3] = 'm';
                    if(Match(input, location, 4, buffer, TemplateKeyword.From, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'f';
                    buffer[1] = 'o';
                    buffer[2] = 'r';
                    if(Match(input, location, 3, buffer, TemplateKeyword.For, ref keywordMatch)) return;
                }
                break;
                }
                case 'n': {
                if(location + 14 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 'D';
                    buffer[4] = 'e';
                    buffer[5] = 's';
                    buffer[6] = 't';
                    buffer[7] = 'r';
                    buffer[8] = 'u';
                    buffer[9] = 'c';
                    buffer[10] = 't';
                    buffer[11] = 'i';
                    buffer[12] = 'v';
                    buffer[13] = 'e';
                    if(Match(input, location, 14, buffer, TemplateKeyword.NonDestructive, ref keywordMatch)) return;
                }
                if(location + 9 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'a';
                    buffer[2] = 'm';
                    buffer[3] = 'e';
                    buffer[4] = 's';
                    buffer[5] = 'p';
                    buffer[6] = 'a';
                    buffer[7] = 'c';
                    buffer[8] = 'e';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Namespace, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'a';
                    buffer[2] = 'm';
                    buffer[3] = 'e';
                    buffer[4] = 'O';
                    buffer[5] = 'f';
                    if(Match(input, location, 6, buffer, TemplateKeyword.NameOf, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'u';
                    buffer[2] = 'l';
                    buffer[3] = 'l';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Null, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'e';
                    buffer[2] = 'w';
                    if(Match(input, location, 3, buffer, TemplateKeyword.New, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'n';
                    buffer[1] = 'o';
                    buffer[2] = 't';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Not, ref keywordMatch)) return;
                }
                break;
                }
                case 'o': {
                if(location + 8 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'v';
                    buffer[2] = 'e';
                    buffer[3] = 'r';
                    buffer[4] = 'r';
                    buffer[5] = 'i';
                    buffer[6] = 'd';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Override, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'p';
                    buffer[2] = 'e';
                    buffer[3] = 'r';
                    buffer[4] = 'a';
                    buffer[5] = 't';
                    buffer[6] = 'o';
                    buffer[7] = 'r';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Operator, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'n';
                    buffer[2] = 'C';
                    buffer[3] = 'h';
                    buffer[4] = 'a';
                    buffer[5] = 'n';
                    buffer[6] = 'g';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.OnChange, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'p';
                    buffer[2] = 't';
                    buffer[3] = 'i';
                    buffer[4] = 'o';
                    buffer[5] = 'n';
                    buffer[6] = 'a';
                    buffer[7] = 'l';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Optional, ref keywordMatch)) return;
                }
                if(location + 8 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'n';
                    buffer[2] = 'E';
                    buffer[3] = 'n';
                    buffer[4] = 'a';
                    buffer[5] = 'b';
                    buffer[6] = 'l';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.OnEnable, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'b';
                    buffer[2] = 'j';
                    buffer[3] = 'e';
                    buffer[4] = 'c';
                    buffer[5] = 't';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Object, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'u';
                    buffer[2] = 't';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Out, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'n';
                    if(Match(input, location, 2, buffer, TemplateKeyword.On, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'o';
                    buffer[1] = 'r';
                    if(Match(input, location, 2, buffer, TemplateKeyword.Or, ref keywordMatch)) return;
                }
                break;
                }
                case 'u': {
                if(location + 9 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'n';
                    buffer[2] = 'c';
                    buffer[3] = 'h';
                    buffer[4] = 'e';
                    buffer[5] = 'c';
                    buffer[6] = 'k';
                    buffer[7] = 'e';
                    buffer[8] = 'd';
                    if(Match(input, location, 9, buffer, TemplateKeyword.Unchecked, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 's';
                    buffer[2] = 'h';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 't';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Ushort, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'n';
                    buffer[2] = 's';
                    buffer[3] = 'a';
                    buffer[4] = 'f';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Unsafe, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'p';
                    buffer[2] = 'd';
                    buffer[3] = 'a';
                    buffer[4] = 't';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Update, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 's';
                    buffer[2] = 'i';
                    buffer[3] = 'n';
                    buffer[4] = 'g';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Using, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'l';
                    buffer[2] = 'o';
                    buffer[3] = 'n';
                    buffer[4] = 'g';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Ulong, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'i';
                    buffer[2] = 'n';
                    buffer[3] = 't';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Uint, ref keywordMatch)) return;
                }
                if(location + 2 <= input.size) { 
                    buffer[0] = 'u';
                    buffer[1] = 'p';
                    if(Match(input, location, 2, buffer, TemplateKeyword.Up, ref keywordMatch)) return;
                }
                break;
                }
                case 'v': {
                if(location + 8 <= input.size) { 
                    buffer[0] = 'v';
                    buffer[1] = 'o';
                    buffer[2] = 'l';
                    buffer[3] = 'a';
                    buffer[4] = 't';
                    buffer[5] = 'i';
                    buffer[6] = 'l';
                    buffer[7] = 'e';
                    if(Match(input, location, 8, buffer, TemplateKeyword.Volatile, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'v';
                    buffer[1] = 'i';
                    buffer[2] = 'r';
                    buffer[3] = 't';
                    buffer[4] = 'u';
                    buffer[5] = 'a';
                    buffer[6] = 'l';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Virtual, ref keywordMatch)) return;
                }
                if(location + 7 <= input.size) { 
                    buffer[0] = 'v';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    buffer[3] = 'i';
                    buffer[4] = 'a';
                    buffer[5] = 'n';
                    buffer[6] = 't';
                    if(Match(input, location, 7, buffer, TemplateKeyword.Variant, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'v';
                    buffer[1] = 'o';
                    buffer[2] = 'i';
                    buffer[3] = 'd';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Void, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'v';
                    buffer[1] = 'a';
                    buffer[2] = 'r';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Var, ref keywordMatch)) return;
                }
                break;
                }
                case 'w': {
                if(location + 5 <= input.size) { 
                    buffer[0] = 'w';
                    buffer[1] = 'h';
                    buffer[2] = 'i';
                    buffer[3] = 'l';
                    buffer[4] = 'e';
                    if(Match(input, location, 5, buffer, TemplateKeyword.While, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'w';
                    buffer[1] = 'h';
                    buffer[2] = 'e';
                    buffer[3] = 'n';
                    if(Match(input, location, 4, buffer, TemplateKeyword.When, ref keywordMatch)) return;
                }
                break;
                }
                case 'b': {
                if(location + 16 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'E';
                    buffer[7] = 'a';
                    buffer[8] = 'r';
                    buffer[9] = 'l';
                    buffer[10] = 'y';
                    buffer[11] = 'I';
                    buffer[12] = 'n';
                    buffer[13] = 'p';
                    buffer[14] = 'u';
                    buffer[15] = 't';
                    if(Match(input, location, 16, buffer, TemplateKeyword.BeforeEarlyInput, ref keywordMatch)) return;
                }
                if(location + 15 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'L';
                    buffer[7] = 'a';
                    buffer[8] = 't';
                    buffer[9] = 'e';
                    buffer[10] = 'I';
                    buffer[11] = 'n';
                    buffer[12] = 'p';
                    buffer[13] = 'u';
                    buffer[14] = 't';
                    if(Match(input, location, 15, buffer, TemplateKeyword.BeforeLateInput, ref keywordMatch)) return;
                }
                if(location + 13 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'D';
                    buffer[7] = 'i';
                    buffer[8] = 's';
                    buffer[9] = 'a';
                    buffer[10] = 'b';
                    buffer[11] = 'l';
                    buffer[12] = 'e';
                    if(Match(input, location, 13, buffer, TemplateKeyword.BeforeDisable, ref keywordMatch)) return;
                }
                if(location + 13 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'D';
                    buffer[7] = 'e';
                    buffer[8] = 's';
                    buffer[9] = 't';
                    buffer[10] = 'r';
                    buffer[11] = 'o';
                    buffer[12] = 'y';
                    if(Match(input, location, 13, buffer, TemplateKeyword.BeforeDestroy, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'U';
                    buffer[7] = 'p';
                    buffer[8] = 'd';
                    buffer[9] = 'a';
                    buffer[10] = 't';
                    buffer[11] = 'e';
                    if(Match(input, location, 12, buffer, TemplateKeyword.BeforeUpdate, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'C';
                    buffer[7] = 'r';
                    buffer[8] = 'e';
                    buffer[9] = 'a';
                    buffer[10] = 't';
                    buffer[11] = 'e';
                    if(Match(input, location, 12, buffer, TemplateKeyword.BeforeCreate, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'E';
                    buffer[7] = 'n';
                    buffer[8] = 'a';
                    buffer[9] = 'b';
                    buffer[10] = 'l';
                    buffer[11] = 'e';
                    if(Match(input, location, 12, buffer, TemplateKeyword.BeforeEnable, ref keywordMatch)) return;
                }
                if(location + 12 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'F';
                    buffer[7] = 'i';
                    buffer[8] = 'n';
                    buffer[9] = 'i';
                    buffer[10] = 's';
                    buffer[11] = 'h';
                    if(Match(input, location, 12, buffer, TemplateKeyword.BeforeFinish, ref keywordMatch)) return;
                }
                if(location + 11 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    buffer[6] = 'I';
                    buffer[7] = 'n';
                    buffer[8] = 'p';
                    buffer[9] = 'u';
                    buffer[10] = 't';
                    if(Match(input, location, 11, buffer, TemplateKeyword.BeforeInput, ref keywordMatch)) return;
                }
                if(location + 6 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'e';
                    buffer[2] = 'f';
                    buffer[3] = 'o';
                    buffer[4] = 'r';
                    buffer[5] = 'e';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Before, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'r';
                    buffer[2] = 'e';
                    buffer[3] = 'a';
                    buffer[4] = 'k';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Break, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'o';
                    buffer[2] = 'o';
                    buffer[3] = 'l';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Bool, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'y';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Byte, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'b';
                    buffer[1] = 'a';
                    buffer[2] = 's';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Base, ref keywordMatch)) return;
                }
                break;
                }
                case 'l': {
                if(location + 9 <= input.size) { 
                    buffer[0] = 'l';
                    buffer[1] = 'a';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    buffer[4] = 'I';
                    buffer[5] = 'n';
                    buffer[6] = 'p';
                    buffer[7] = 'u';
                    buffer[8] = 't';
                    if(Match(input, location, 9, buffer, TemplateKeyword.LateInput, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'l';
                    buffer[1] = 'o';
                    buffer[2] = 'c';
                    buffer[3] = 'k';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Lock, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'l';
                    buffer[1] = 'o';
                    buffer[2] = 'n';
                    buffer[3] = 'g';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Long, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'l';
                    buffer[1] = 'a';
                    buffer[2] = 't';
                    buffer[3] = 'e';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Late, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'l';
                    buffer[1] = 'o';
                    buffer[2] = 's';
                    buffer[3] = 't';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Lost, ref keywordMatch)) return;
                }
                break;
                }
                case 'g': {
                if(location + 6 <= input.size) { 
                    buffer[0] = 'g';
                    buffer[1] = 'a';
                    buffer[2] = 'i';
                    buffer[3] = 'n';
                    buffer[4] = 'e';
                    buffer[5] = 'd';
                    if(Match(input, location, 6, buffer, TemplateKeyword.Gained, ref keywordMatch)) return;
                }
                if(location + 4 <= input.size) { 
                    buffer[0] = 'g';
                    buffer[1] = 'o';
                    buffer[2] = 't';
                    buffer[3] = 'o';
                    if(Match(input, location, 4, buffer, TemplateKeyword.Goto, ref keywordMatch)) return;
                }
                break;
                }
                case 'y': {
                if(location + 5 <= input.size) { 
                    buffer[0] = 'y';
                    buffer[1] = 'i';
                    buffer[2] = 'e';
                    buffer[3] = 'l';
                    buffer[4] = 'd';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Yield, ref keywordMatch)) return;
                }
                break;
                }
                case 'h': {
                if(location + 8 <= input.size) { 
                    buffer[0] = 'h';
                    buffer[1] = 'e';
                    buffer[2] = 'l';
                    buffer[3] = 'd';
                    buffer[4] = 'D';
                    buffer[5] = 'o';
                    buffer[6] = 'w';
                    buffer[7] = 'n';
                    if(Match(input, location, 8, buffer, TemplateKeyword.HeldDown, ref keywordMatch)) return;
                }
                if(location + 5 <= input.size) { 
                    buffer[0] = 'h';
                    buffer[1] = 'o';
                    buffer[2] = 'v';
                    buffer[3] = 'e';
                    buffer[4] = 'r';
                    if(Match(input, location, 5, buffer, TemplateKeyword.Hover, ref keywordMatch)) return;
                }
                break;
                }
                case 'k': {
                if(location + 5 <= input.size) { 
                    buffer[0] = 'k';
                    buffer[1] = 'e';
                    buffer[2] = 'y';
                    buffer[3] = 'F';
                    buffer[4] = 'n';
                    if(Match(input, location, 5, buffer, TemplateKeyword.KeyFn, ref keywordMatch)) return;
                }
                if(location + 3 <= input.size) { 
                    buffer[0] = 'k';
                    buffer[1] = 'e';
                    buffer[2] = 'y';
                    if(Match(input, location, 3, buffer, TemplateKeyword.Key, ref keywordMatch)) return;
                }
                break;
                }
default:break;
            }
        
        }
        
    }
    
}