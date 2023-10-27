namespace EvolveUI.Compiler {

    public enum DiagnosticError {

        None,

        // Introspection errors
        AmbiguousElementReference,
        AmbiguousTypeMatch,
        UnresolvedType,
        VariableNameAlreadyInScope,
        ParameterNotFound,
        ArgumentCannotBeImplicit,
        RequiredArgumentNotProvided,
        UnresolvedTag,
        AmbiguousStyleReference,
        UnresolvedStyle,
        UnresolvedFieldOrProperty,
        UnresolvedIdentifier,
        InvalidBuiltInVariableUsage,
        UnresolvedMemberAccess,
        SlotNotFound,
        ExtrusionCountMismatch,
        RootIsNotAvailableInsideFunctionContext,
        ExtrusionNotFound,
        ExtrusionIsNotPublic,
        SlotIsDefinedButNeverRendered,
        ImplicitSlotNotDefined,

        // Compilation errors
        RepeatBasedOnCountCannotAcceptAKeyFunction,
        StyleLiteralMustHaveATargetType,
        UnknownStyleProperty,
        InvalidStyleConversion,
        UnableToParseStyleLiteral,
        FailedToParseTextureReference,
        FailedToLoadAsset,

        // Tokenize Errors
        FailedToTokenize,

        // Parsing Errors
        ExpectedDeferKeyword,
        ExpectedTerminatingSemiColon,
        ExpectedIdentifier,
        InvalidGenericArgumentList,
        UnexpectedToken,
        NotImplemented,
        ExpectedTypePath,
        StuckOnUnexpectedToken,
        ExpectedDashedIdentifier,
        ExpectedExpression,
        ExpectedColon,
        ExpectedStatement,
        ExpectedParenthesis,
        UnmatchedParentheses,
        ExpectedElementArgumentOrNoTrailingComma,
        ExpectedAttributeDefinition,
        ExpectedColonOrEqualSignAfterStyleArgument,
        StyleListMustBeWrappedInSquareBrackets,
        UnmatchedSquareBracket,
        ExpectedDashedIdentifierNotToContainWhitespace,
        ExpectedDashedIdentifierNotToEndWithMinus,
        ExpectedIdentifierFollowingAtSymbol,
        ExpectedStyleDefinition,
        ExpectedStylePropertyName,
        ExpectedEqualSign,
        ExpectedInstanceStyle,
        ExpectedMouseEventHandler,
        InvalidMouseInputEvent,
        ExpectedInputModifier,
        DanglingDot,
        InvalidDragInputEvent,
        ExpectedDragEventHandler,
        ExpectedKeyEventHandler,
        InvalidKeyInputEvent,
        InvalidLifeCycleEvent,
        ExpectedFocusEventHandler,
        InvalidFocusEventName,
        ExpectedSyncBinding,
        ExpectedOnChangeBinding,
        ExpectedPropertyBinding,
        DanglingComma,
        UnknownPropertyPrefix,
        ExpectedScopeModifier,
        ExpectedBlockOrEmbeddedStatement,
        UnmatchedCurlyBrace,
        ExpectedRepeatExpression,
        ExpectedRepeatParameterName,
        DefaultValueExpressionIsRequired,
        CannotProvideBothAParameterModiferAndADefaultValue,
        ExpectedCurlyBrace,
        ExpectedAngleBracket,
        UnmatchedAngleBracket,
        ExpectedSquareBracket,
        ExpectedElementTag,
        ExpectedVariableInitializer,
        ConstantsMustBeFullyInitialized,
        ExpectedDot,
        ExpectedSemiColonOrBlock,
        UnknownRenderEntity,
        ExpectedQualifiedIdentifier,
        ExpectedSwitchLabel,
        DefaultSwitchCasesCannotHaveExpressions,
        DuplicateDefaultLabel,
        ExpectedFormalParameterList,
        ExpectedLocalFunctionBody,
        ExpectedInKeyword,
        IfStatementsNotInDirectlyInTemplateBlockCannotHaveScopeModifiers,
        ExpectedSemiColon,
        ExpectedWhileKeywordAfterDoStatement,
        ExpectedCatchAndOrFinallyBlock,
        ExpectedExpressionOrVariableDeclaration,
        ExpectedReturnOrBreak,
        ExpectedSemiColonOrAliasExpression,
        ExpectedNamespaceName,
        ExpectedTopLevelDeclaration,
        ExpectedConstantDeclarator,
        LambdaParameterTypesCannotBeBothFormalAndAnonymous,
        ExpectedIsTypeOrIsNullPattern,
        UnexpectedIdentifier,
        ExpectedCaseLabelOrDefault,
        ExpectedThrowableExpression,
        ExpectedFatArrow,
        ExpectedPattern,
        ExpectedParenSquareBracketOrCurlyBrace,
        ExpectedStringInterpolation,
        ExpectedNonEmptyStringInterpolation,
        ExpectedStringInterpolationAlignment,
        ExpectedStringInterpolationFormat,
        ExpectedStringInterpolationPart,
        ExpectedTemplateParameter,
        UnexpectedDecorator,
        OnlyTemplatesAndFunctionsSupportSlots,
        TypographyDoesNotSupportParameters,
        ExpectedSlotName,
        InvalidSlotPlacement,
        ExpectedThinArrow,
        ImplicitSlotCannotBeExplicitlyUsed,
        TemplateFunctionsDoNotSupportOnChange,
        TemplateFunctionsDoNotSupportLifeCycleEvents,
        TemplateFunctionsDoNotSupportAttributes,
        TemplateFunctionsDoNotSupportStyles,
        TemplateFunctionsDoNotSupportInputHandlers,
        TemplateFunctionsDoNotSupportDecorators,
        TemplateFunctionsDoNotSupportElementReferenceExtrusions,

        RepeatOnlyExtrudesIndexAndIteration,
        ExpectedPublicOrPrivateAccessModifier,

        ParameterIsNotPublicAndCannotBeUsedInSyncExpression,

        OnChangeTargetIsNotPublic,

        OnChangeTargetIsNotDefined,

        OnChangeDoesNotSupportExpressionExtrusions,

        ExpectedMemberDeclaration,

        ExpectedComputedPropertyExpression,

        IncorrectArgumentCount,

        TagIsNotADecorator,

        TagIsNotAFunction,

        TagIsNotAnElement,

        UnableToParseDecorator,

        ImportIsNotADependencyOfThisModule,

        ComplexCharacterLiteralIsNotYetImplemented,

        UnicodeEscapeSequenceIsNotYetImplemented,

        UnknownAlias,

        LocalMethodsAreNotImplementedYet,

        ExpectedMethodDefinition,

        MemberTypeNotAllowedInDecorator,

        MemberTypeNotAllowedInTypography,

        MemberTypeNotAllowedInFunction,

        DuplicateTopLevelName,

        ElementExtrusionNotAllowedInDecorators,

        UnresolvedInstanceMethodOrOverload,

        NoSuitableIndexerFound,

        UnableToParseUnsignedInt,

        UnableToParseUnsignedLong,

        InvalidLiteral,

        UnableToParseLong,

        UnableToParseFloat,

        UnableToParseDouble,

        UnableToParseInt,

        ExpressionTypeDoesNotMatchAssignment,

        LambdaParameterTypeDoesntMatchTargetType,

        UnableToResolveType,

        InvalidImplicitConversion,

        ErrorString,

        EntityDoesNotHaveACompanionObjectDefined,

        FromSourceMustBeTopLevelStateOrCompanionField,

        PropertyMustBeReadableAndWritableToBeUsedInAParameterMapping,

        UnresolvedTypeOrIdentifier,

        UnresolvedExtrusion,

        CannotDeclareVariableHere,

        FromSourceCannotBeRecursive,

        FromMappingIsOnlyValidInTopLevelState,

        FromMappingIsOnlyAllowedInTopLevelDeclarations,

        ExpectedFatArrowOrFromMapping,

        UnresolvedConstructor,

        NoCompanionTypeIsDefined,

        ExpectedParamKeyword,


        RequiredParametersMustBeDefinedBeforeOptionalOnes,

        RequiredParametersCannotDefineDefaultValues,

        LifeCycleHandlerWasAlreadyDefined,

        InputHandlerWasAlreadyDefined,

        DisableAndDestroyHandlersAreOnlyAllowedInTopLevelDeclarations,

        BuiltInExpressionIsNotValidInFromMapping,

        InvalidTextEventName,

        ExpectedTextEventHandler,

        TextBuiltInIsOnlyValidInTypographyAndTextDecorators,

        TypographyDecoratorsCanOnlyBeUsedOnTypographyElements,

        ExpectedAPropertyBinding_InstanceStyle_OrAttribute,

        StateDefaultValueCannotBeInitializedUsingParameters,

        ExpectedDottedIdentifierNotToContainWhitespace,

        ExpectedDottedIdentifierNotToEndWithDot,

        SpawnTargetMustBeATemplate,

        DecoratorsCannotSupportSyncParameters,

        WhenUsingVarAsATypePathYouMustProvideADefaultValueOrMappingExpression,

        ElementBuiltInIsOnlyAvailableInTheContextOfAnElement,

        ExpectedAStringTypeExpressionWhenUsingSyncWithATypographyTextField,

        TooManyParametersPassed,

        DecoratorGenericArgumentCountMismatch,

        OperationIsNotAllowedHere

    }

}