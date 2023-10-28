namespace EvolveUI.Parsing {
    public unsafe ref partial struct TemplateParser {

        private RuleResult ParseExpressionRule(TokenStream tokenStream) {
            return ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex) ? new RuleResult(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult<ObjectInitializer> ParseObjectInitializerRule(TokenStream tokenStream) {
            return ParseObjectInitializer(ref tokenStream, out ExpressionIndex<ObjectInitializer> init) ? new RuleResult<ObjectInitializer>(tokenStream.location, init) : default;
        }

        private RuleResult ParseObjectOrCollectionInitializer(TokenStream tokenStream) {
            return ParseObjectOrCollectionInitializer(ref tokenStream, out ExpressionIndex index) ? new RuleResult(tokenStream.location, index) : default;
        }

        private RuleResult<CollectionInitializer> ParseCollectionInitializerRule(TokenStream tokenStream) {
            return ParseCollectionInitializer(ref tokenStream, out ExpressionIndex<CollectionInitializer> index) ? new RuleResult<CollectionInitializer>(tokenStream.location, index) : default;
        }

        private RuleResult<LambdaExpression> ParseLambdaExpressionRule(TokenStream tokenStream) {
            return ParseLambdaExpression(ref tokenStream, out ExpressionIndex<LambdaExpression> lambda) ? new RuleResult<LambdaExpression>(tokenStream.location, lambda) : default;
        }

        private RuleResult<BlockExpression> ParseBlockRule(TokenStream tokenStream) {
            return ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> block) ? new RuleResult<BlockExpression>(tokenStream.location, block) : default;
        }

        private RuleResult ParseGeneralCatchClauseRule(TokenStream tokenStream) {
            return ParseGeneralCatchClause(ref tokenStream, out ExpressionIndex<Catch> catchClause) ? new RuleResult(tokenStream.location, new ExpressionIndex(catchClause.id)) : default;
        }

        private RuleResult ParseTerminatedExpressionRule(TokenStream tokenStream) {
            return ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex) && tokenStream.Consume(SymbolType.SemiColon) ? new RuleResult(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult ParseSemiColonRule(TokenStream tokenStream) {
            return tokenStream.Consume(SymbolType.SemiColon) ? new RuleResult(tokenStream.location, default) : default;
        }

        private RuleResult ParseSimpleEmbeddedStatementRule(TokenStream tokenStream) {
            return ParseSimpleEmbeddedStatement(ref tokenStream, out ExpressionIndex expressionIndex) ? new RuleResult(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult ParseEmbeddedStatementRule(TokenStream tokenStream) {
            return ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex expressionIndex) ? new RuleResult(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult ParseLocalFunctionDeclarationRule(TokenStream tokenStream) {
            return ParseLocalFunctionDeclaration(ref tokenStream, out ExpressionIndex<LocalFunctionDefinition> local) ? new RuleResult(tokenStream.location, local) : default;
        }

        private RuleResult ParseLocalVariableDeclarationRule(TokenStream tokenStream, bool terminated) {
            return ParseLocalVariableDeclaration(ref tokenStream, terminated, out ExpressionIndex<VariableDeclaration> index) ? new RuleResult(tokenStream.location, index) : default;
        }

        private RuleResult ParseTerminatedLocalConstantRule(TokenStream tokenStream) {
            return ParseLocalConstant(ref tokenStream, out ExpressionIndex<VariableDeclaration> constDeclaration) ? new RuleResult(tokenStream.location, constDeclaration) : default;
        }

        private RuleResult<Literal> ParseLiteral(TokenStream tokenStream) {
            return ParseLiteral(ref tokenStream, out ExpressionIndex<Literal> literal) ? new RuleResult<Literal>(tokenStream.location, literal) : default;
        }

        private RuleResult<BaseAccessExpression> ParseBaseAccessRule(TokenStream tokenStream) {
            return ParseBaseAccess(ref tokenStream, out ExpressionIndex<BaseAccessExpression> baseAccess) ? new RuleResult<BaseAccessExpression>(tokenStream.location, baseAccess) : default;
        }

        private RuleResult<CheckedExpression> ParseCheckedUncheckedExpression(TokenStream tokenStream) {
            return ParseCheckedUncheckedExpression(ref tokenStream, out ExpressionIndex<CheckedExpression> expr) ? new RuleResult<CheckedExpression>(tokenStream.location, expr) : default;
        }

        private RuleResult ParseThrowableExpressionRule(TokenStream tokenStream) {
            return ParseThrowableExpression(ref tokenStream, out ExpressionIndex throwable) ? new RuleResult(tokenStream.location, throwable) : default;
        }

        private RuleResult ParseConditionalExpressionRule(TokenStream tokenStream) {
            return ParseConditionalExpression(ref tokenStream, out ExpressionIndex expressionIndex) ? new RuleResult(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult<Identifier> ParseIdentifierWithTypeArgumentsRule(TokenStream tokenStream) {
            return ParseIdentifierWithTypeArguments(ref tokenStream, out ExpressionIndex<Identifier> expressionIndex) ? new RuleResult<Identifier>(tokenStream.location, expressionIndex) : default;
        }

        private RuleResult ParseStackAllocInitializerRule(TokenStream tokenStream) {
            return ParseStackAllocInitializer(ref tokenStream, out ExpressionIndex expr) ? new RuleResult(tokenStream.location, expr) : default;
        }

        private RuleResult ParseNonAssignmentRule(TokenStream tokenStream) {
            return ParseNonAssignment(ref tokenStream, out ExpressionIndex index) ? new RuleResult(tokenStream.location, index) : default;
        }

        private RuleResult ParseUnboundTypeNameRule(TokenStream tokenStream) {
            return ParseUnboundTypeName(ref tokenStream, out ExpressionIndex<TypeOfExpression> typeOf) ? new RuleResult(tokenStream.location, typeOf) : default;
        }

        private RuleResult ParseTypeOfTypePathRule(TokenStream tokenStream) {
            int startLocation = tokenStream.location;
            if (ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                ExpressionIndex<TypeOfExpression> typeOf = expressionBuffer.Add(startLocation, tokenStream.location, new TypeOfExpression() {
                    typePath = typePath
                });
                return new RuleResult(tokenStream.location, typeOf);
            }

            return default;
        }

        private RuleResult ParseBinaryPatternRule(TokenStream tokenStream) {
            return ParseBinaryPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseConstantPatternRule(TokenStream tokenStream) {
            return ParseConstantPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseDeclarationPatternRule(TokenStream tokenStream) {
            return ParseDeclarationPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseDiscardPatternRule(TokenStream tokenStream) {
            return ParseDiscardPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseParenthesizedPatternRule(TokenStream tokenStream) {
            return ParseParenthesizedPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseRecursivePatternRule(TokenStream tokenStream) {
            return ParseRecursivePattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseRelationalPatternRule(TokenStream tokenStream) {
            return ParseRelationalPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseTypePatternRule(TokenStream tokenStream) {
            return ParseTypePattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseUnaryPatternRule(TokenStream tokenStream) {
            return ParseUnaryPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseVarPatternRule(TokenStream tokenStream) {
            return ParseVarPattern(ref tokenStream, out ExpressionIndex pattern) ? new RuleResult(tokenStream.location, pattern) : default;
        }

        private RuleResult ParseTypeCreationWithArgsRule(int startLocation, ExpressionIndex<TypePath> typePath, TokenStream tokenStream) {
            return ParseTypeCreationWithArgs(ref tokenStream, startLocation, typePath, out ExpressionIndex<NewExpression> newExpression) ? new RuleResult(tokenStream.location, newExpression) : default;
        }

        private RuleResult ParseTypeCreationWithInitializerRule(int startLocation, ExpressionIndex<TypePath> typePath, TokenStream tokenStream) {
            return ParseTypeCreationWithInitializer(ref tokenStream, startLocation, typePath, out ExpressionIndex<NewExpression> newExpression) ? new RuleResult(tokenStream.location, newExpression) : default;
        }

        private RuleResult ParseArrayCreationWithoutSizeExpressionsRule(int startLocation, ExpressionIndex<TypePath> typePath, TokenStream tokenStream) {
            return ParseArrayCreationWithoutSizeExpressions(ref tokenStream, startLocation, typePath, out ExpressionIndex<NewExpression> newExpression) ? new RuleResult(tokenStream.location, newExpression) : default;
        }

        private RuleResult ParseUsingDeclaration(TokenStream tokenStream) {
            return ParseUsingDeclaration(ref tokenStream, out ExpressionIndex<UsingStatement> usingStatement) ? new RuleResult(tokenStream.location, usingStatement) : default;
        }

    }

}
