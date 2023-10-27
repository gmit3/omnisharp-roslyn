using System;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {


    internal enum RuntimeKeyword {

        None,
        DollarRuntime,
        DollarParent,
        DollarThis,
        DollarEvt,
        DollarNewValue,
        DollarOldValue,
        DollarApplication,
        DollarView,
        DollarCurrentStyleValue,
        DollarRoot,
        DollarSlots,
        DollarParameters,
        DollarExtrusions,

    }

    internal enum SimpleTypeName {

        None,
        Bool,
        Byte,
        Sbyte,
        Int,
        UInt,
        Long,
        Ulong,
        Float,
        Double,
        Short,
        Ushort,
        Char,
        String,
        Object
        
    }

    internal enum TemplateKeyword {

        Invalid = 0,

        Template,
        Element,
        Run,
        State,
        Slot,
        Render,
        Attr,
        Style,
        Teleport,
        Portal,
        Marker,
        Defer,
        Enable,
        Create,
        Repeat,
        Import,
        Typography,
        Container,
        Invoke,

        // used C# keywords
        As,
        Case,
        Foreach,
        Else,
        If,
        Implicit,
        Is,
        Null,
        Out,
        Override,
        New,
        Ref,
        Return,
        Switch,
        Using,
        Typeof,
        Var,
        Default,
        When,

        // maybe will use keywords
        Const,
        Continue,
        Break,
        Catch,
        Class,
        Do,
        Finally,
        For,
        Interface,
        Namespace,
        Params,
        Readonly,
        Static,
        Struct,
        Try,
        This,
        Throw,
        While,
        Enum,
        In,
        NameOf,
        Async,
        Float,
        Int,
        Lock,
        Long,
        Object,
        String,
        Uint,
        Ulong,
        Ushort,
        Short,
        Sizeof,
        Double,
        Bool,
        Byte,
        Char,

        // not currently used 
        Abstract,
        Base,
        Checked,
        Decimal,
        Delegate,
        Event,
        Explicit,
        Extern,
        Fixed,

        Goto,
        Internal,
        Operator,
        Private,
        Protected,
        Public,
        Sbyte,
        Sealed,

        Stackalloc,

        Unchecked,
        Unsafe,
        Virtual,
        Void,
        Volatile,

        Await,
        Yield,

        Mouse,

        Down,

        Scroll,

        Context,

        Click,

        Move,

        Exit,

        Enter,

        Up,

        HeldDown,

        Focus,

        Late,

        Shift,

        Ctrl,

        Cmd,

        Alt,

        Drag,

        BeforeUpdate,

        Drop,

        Cancel, 

        Key,

        On,

        BeforeCreate,

        BeforeEnable,

        PostUpdate,

        Disable,

        Destroy,

        Gained,

        Lost,

        Sync,

        OnChange,

        Destructive,

        NonDestructive,

        KeyFn,

        Count,

        End,

        Start,

        StepSize,

        Compile,

        Dynamic,

        Not,

        Or,

        And,

        Extrude,

        AfterEnable,

        AfterCreate,

        AfterUpdate,

        BeforeFinish,

        AfterFinish,

        BeforeDisable,

        AfterDisable,

        BeforeDestroy,

        AfterDestroy,

        BeforeEarlyInput,

        AfterEarlyInput,

        BeforeInput,

        AfterInput,

        BeforeLateInput,

        AfterLateInput,

        Update,

        Finish,

        EarlyInput,

        Input,

        LateInput,

        Before,

        After,

        Early,

        Match,

        Decorator,

        Conditional,

        Computed,

        Method,

        Function,

        From,

        Hover,

        Required,

        Optional,

        Text,


        OnEnable,

        Variant,

        Spawns

    }


    internal struct KeywordMatch {

        public int length;
        public TemplateKeyword keyword;
        public bool isBuiltIn;

    }

    internal unsafe partial struct StringTokens {

        public static bool Match(FixedCharacterSpan input, int location, out KeywordMatch keywordMatch) {
            keywordMatch = default;
            MatchKeywordGenerated(input, location, ref keywordMatch);
            return keywordMatch.length > 0;
        }

        static partial void MatchKeywordGenerated(FixedCharacterSpan input, int location, ref KeywordMatch keywordMatch);

        private static bool Match(FixedCharacterSpan input, int location, int keywordLength, char* buffer, TemplateKeyword keyword, ref KeywordMatch keywordMatch) {
            for (int i = 0; i < keywordLength; i++) {
                if (input[location + i] != buffer[i]) {
                    return false;
                }
            }

            // not sure about the dash value here, probably reject it? almost feels contextual. should just tokenize attr:style- and @style-name differently 
            if (location + keywordLength < input.size && Tokenizer.IsIdentifierCharacter(input[location + keywordLength], false)) {
                return false;
            }

            keywordMatch.keyword = keyword;
            keywordMatch.length = keywordLength;
            return true;

        }

    }

}