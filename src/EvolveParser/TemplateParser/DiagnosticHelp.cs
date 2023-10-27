using System.Runtime.InteropServices;
using EvolveUI.Parsing;
using EvolveUI.Util;

namespace EvolveUI.Compiler {

    [StructLayout(LayoutKind.Explicit)]
    internal struct DiagnosticDetails {

        [FieldOffset(0)] public HelpType helpType;
        [FieldOffset(4)] public VariableNameAlreadyInScopeInfo variableNameAlreadyInScope;
        [FieldOffset(4)] public DecoratorArgumentCountMismatch decoratorArgumentCountMismatch;
        // [FieldOffset(4)] public ParameterError parameterError;
        // [FieldOffset(4)] public TypeResolutionError typeResolutionError;
        // [FieldOffset(4)] public TagResolutionError tagResolutionError;
        // [FieldOffset(4)] public StyleResolutionError styleResolutionError;
        [FieldOffset(4)] public FieldOrPropertyError fieldOrPropertyError;
        [FieldOffset(4)] public IdentifierError identifierError;
        [FieldOffset(4)] public MemberAccessError memberAccessError;
        [FieldOffset(4)] public SlotError slotError;
        [FieldOffset(4)] public ExtrusionError extrusionError;
        [FieldOffset(4)] public AssetLoadError assetLoadError;
        [FieldOffset(4)] public DuplicateTopLevelNameDesc topLevelNameError;
        [FieldOffset(4)] public ErrorStringDesc errorString;
        [FieldOffset(4)] public StyleLiteralParseError styleLiteralParseError;

    }

    internal unsafe struct StyleLiteralParseError {

        public FixedCharacterSpan primaryError;

    }

    internal unsafe struct ErrorStringDesc {

        public FixedCharacterSpan primaryError;
        public FixedCharacterSpan help;
        public GCHandle<string> primaryErrorHandle;

    }

    internal unsafe struct DuplicateTopLevelNameDesc {

        public TemplateFile* templateFile;
        public NonTrivialTokenLocation tokenLocation;
        public FixedCharacterSpan identifier;
        public int line;
        public int col;

    }

    // internal struct TypedParameterDesc {
    //
    //     public FixedCharacterSpan fieldOrPropertyName;
    //     public ResolvedTypePointer resolvedTypePointer;
    //     public bool isRequired;
    //
    // }

    internal struct ExtrusionError {

        public int extrusionsProvided;
        public int extrusionsAvailable;

    }

    internal struct SlotError {

        public FixedCharacterSpan slotName;
        public SlotType slotType;

    }

    internal struct AssetLoadError {
        
        public FixedCharacterSpan message;

    }

    internal struct IdentifierError {

        public FixedCharacterSpan identifier;

    }

    internal struct MemberAccessError {

        public FixedCharacterSpan typeName;
        public FixedCharacterSpan identifier;

    }

    internal struct FieldOrPropertyError {

        public FixedCharacterSpan fieldOrPropertyName;
        public FixedCharacterSpan typeName;

    }

    internal struct VariableNameAlreadyInScopeInfo {

        public FixedCharacterSpan variableName;
        public NonTrivialTokenRange locationA;
        public NonTrivialTokenRange locationB;

    }

    // internal struct ParameterError {
    //
    //     public FixedCharacterSpan parameterName;
    //     public CheckedArray<TypedParameterDesc> availableParameters;
    //     public int parameterIndex;
    //
    // }

    internal struct DecoratorArgumentCountMismatch {

        public int minArgumentCount;
        public int maxArgumentCount;
        public int givenArgumentCount;
        public FixedCharacterSpan decoratorModule;
        public FixedCharacterSpan decoratorTagName;

    }

    // internal unsafe struct StyleResolutionError {
    //
    //     public FixedCharacterSpan styleName;
    //     public ModuleInfo* ambiguousModule;
    //     public ModuleInfo* module;
    //
    // }
    //
    // internal unsafe struct TagResolutionError {
    //
    //     public FixedCharacterSpan tagName;
    //     public ModuleImport ambiguousModule;
    //     public ModuleInfo* module;
    //
    // }

    // internal struct TypeResolutionError {
    //
    //     public BaseTypePointer resolvedType;
    //     public BaseTypePointer ambiguousType;
    //     public FixedCharacterSpan typeName;
    //     public CheckedArray<FixedCharacterSpan> namespaces;
    //     public NonTrivialTokenRange tokenRange;
    //
    // }

}