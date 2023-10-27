using System;
using System.Diagnostics;
using EvolveUI.Compiler;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal ref partial struct TemplateParser {

        private struct ExpressionParseState {

            public int location;
            public int expressionListSize;
            public int rangeBufferSize;

        }

        [DebuggerStepThrough]
        private ExpressionParseState SaveState(in TokenStream tokenStream) {
            return new ExpressionParseState() {
                location = tokenStream.location,
                expressionListSize = expressionBuffer.expressions.size,
                rangeBufferSize = expressionBuffer.rangeBuffer.size
            };
        }

        [DebuggerStepThrough]
        private void RestoreState(ref TokenStream tokenStream, ExpressionParseState state) {
            tokenStream.location = state.location;
            expressionBuffer.expressions.size = state.expressionListSize;
            expressionBuffer.rangeBuffer.size = state.rangeBufferSize;
        }

        private bool ParseInterpolatedStringPart(ref TokenStream tokenStream, out ExpressionIndex interpolationPart) {
            // : string_literal | interpolated_string_expression

            int startLocation = tokenStream.location;
            interpolationPart = default;

            if (tokenStream.Current.tokenType == TokenType.StringInterpolationPartStart) {

                if (!tokenStream.TryGetSubStream(TokenType.StringInterpolationPartStart, TokenType.StringInterpolationPartEnd, out TokenStream subStream)) {
                    return HardError(subStream, DiagnosticError.ExpectedStringInterpolationPart, HelpType.StringInterpolationSyntax);
                }

                PushRecoveryPoint(subStream.start, subStream.end + 1);

                ParseExpression(ref subStream, out ExpressionIndex expression);

                ExpressionIndex alignmentExpression = default;
                NonTrivialTokenLocation formatLocation = default;

                if (subStream.Consume(SymbolType.Comma)) {

                    // alignment

                    if (!ParseExpression(ref subStream, out alignmentExpression)) {
                        return HardError(subStream, DiagnosticError.ExpectedStringInterpolationAlignment, HelpType.StringInterpolationSyntax);
                    }

                }

                if (subStream.Consume(SymbolType.Colon)) {
                    // format directive, not really sure what this is, might be anything? for now just read an identifier i guess? f2, d3,
                    // i guess that won't support 0 and # or other format specifiers  
                    // will address those if needed 
                    if (!subStream.ConsumeStandardIdentifier(out formatLocation)) {
                        return HardError(subStream, DiagnosticError.ExpectedStringInterpolationFormat, HelpType.StringInterpolationSyntax);
                    }
                }

                if (PopRecoveryPoint(ref subStream)) {
                    return false;
                }

                interpolationPart = expressionBuffer.Add(startLocation, tokenStream.location, new StringInterpolationPart() {
                    alignmentExpression = alignmentExpression,
                    formatDirective = formatLocation,
                    expression = expression
                });

                return true;
            }
 
            if (tokenStream.Current.tokenType == TokenType.StringLiteral) {
                tokenStream.location++;
                interpolationPart = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Literal() {
                    literalType = LiteralType.StringLiteral, // tokenStream.Current.extra.literalType
                });
                return true;
            }

            interpolationPart = default;
            return false;

        }

        private bool ParseInterpolatedStringExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // INTERPOLATED_REGULAR_STRING_START interpolated_regular_string_part* INTERPOLATED_REGULAR_STRING_END
            expressionIndex = default;

            int startLocation = tokenStream.location;
            
            if (tokenStream.Current.tokenType != TokenType.StringInterpolationStart) {
                return false;
            }

            if (!tokenStream.TryGetSubStream(TokenType.StringInterpolationStart, TokenType.StringInterpolationEnd, out TokenStream subStream)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedStringInterpolation, HelpType.StringInterpolationSyntax);
            }

            if (subStream.IsEmpty) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedNonEmptyStringInterpolation, HelpType.StringInterpolationSyntax);
            }

            using ScopedList<ExpressionIndex> list = scopedAllocator.CreateListScope<ExpressionIndex>(16);

            while (ParseInterpolatedStringPart(ref subStream, out ExpressionIndex interpolationPart)) {
                list.Add(interpolationPart);
            }

            if (list.size == 0) {
                return HardError(subStream, DiagnosticError.UnexpectedToken, HelpType.StringInterpolationSyntax);
            }

            if (subStream.HasMoreTokens) {
                return HardError(subStream, DiagnosticError.UnexpectedToken, HelpType.StringInterpolationSyntax);
            }

            expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new StringInterpolation() {
                parts = expressionBuffer.AddExpressionList(list)
            });
            
            return true;
        }

        private bool ParseLiteral(ref TokenStream tokenStream, out ExpressionIndex<Literal> literal) {
            TokenType tokenType = tokenStream.Current.tokenType;
            if (tokenType == TokenType.CharacterLiteral ||
                tokenType == TokenType.BoolLiteral ||
                tokenType == TokenType.NumericLiteral) {

                LiteralType literalType = tokenStream.Current.extra.literalType;
                tokenStream.location++;

                literal = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Literal() {
                    literalType = literalType
                });
                return true;
            }

            if (tokenStream.Current.Keyword == TemplateKeyword.Null) {
                tokenStream.location++;
                literal = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Literal() {
                    literalType = LiteralType.Null
                });
                return true;
            }

            if (tokenType == TokenType.StringLiteral) {

                LiteralType literalType = tokenStream.Current.extra.literalType;
                
                tokenStream.location++;

                literal = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Literal() {
                    literalType = literalType
                });
                return true;
            }

            if (tokenType == TokenType.StyleLiteral) {

                tokenStream.location++;
                literal = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Literal() {
                    literalType = LiteralType.StyleLiteral
                });
                
                return true;

            }

            literal = default;
            return default;
        }

        private bool ParseUnaryExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseDirectCast(tokenStream));
            expressionBuffer.MakeRule(ParsePrimaryExpression(tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Plus, PrefixOperator.Plus, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Minus, PrefixOperator.Minus, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Not, PrefixOperator.Not, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.BinaryNot, PrefixOperator.BitwiseNot, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Increment, PrefixOperator.Increment, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Decrement, PrefixOperator.Decrement, tokenStream));
            // expressionBuffer.MakeRule(ParseAwait(tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.BinaryAnd, PrefixOperator.AddressOf, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.Multiply, PrefixOperator.Dereference, tokenStream));
            expressionBuffer.MakeRule(ParsePrefixUnary(SymbolType.BinaryXor, PrefixOperator.Range, tokenStream)); // for ranges 

            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private RuleResult ParseUnaryExpressionRule(TokenStream tokenStream) {
            return ParseUnaryExpression(ref tokenStream, out ExpressionIndex expressionIndex)
                ? new RuleResult(tokenStream.location, expressionIndex)
                : default;
        }

        private RuleResult<DirectCast> ParseDirectCast(TokenStream tokenStream) {

            if (tokenStream.Current.Symbol != SymbolType.OpenParen) {
                return default;
            }

            int startLocation = tokenStream.location;

            tokenStream.location++;
            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                return default;
            }

            if (tokenStream.Current.Symbol != SymbolType.CloseParen) {
                return default;
            }

            tokenStream.location++;

            if (!ParseUnaryExpression(ref tokenStream, out ExpressionIndex unary)) {
                return default;
            }

            ExpressionIndex<DirectCast> directCast = expressionBuffer.Add(startLocation, tokenStream.location, new DirectCast() {
                typePath = typePath,
                expression = unary
            });

            return new RuleResult<DirectCast>(tokenStream.location, directCast);
        }

        private RuleResult<PrefixUnaryExpression> ParsePrefixUnary(SymbolType symbol, PrefixOperator prefixOperator, TokenStream tokenStream) {

            if (tokenStream.Current.Symbol != symbol) {
                return default;
            }

            int startLocation = tokenStream.location;

            tokenStream.location++;

            if (!ParseUnaryExpression(ref tokenStream, out ExpressionIndex unary)) {
                return default;
            }

            PrefixUnaryExpression prefixUnaryExpression = new PrefixUnaryExpression {
                expression = unary,
                prefixOperator = prefixOperator
            };

            ExpressionIndex<PrefixUnaryExpression> expressionIndex = expressionBuffer.Add(prefixUnaryExpression, new NonTrivialTokenRange(startLocation, tokenStream.location));

            return new RuleResult<PrefixUnaryExpression>(tokenStream.location, expressionIndex);

        }

        private RuleResult<AssignmentExpression> ParseTypicalAssignment(int startLocation, ExpressionIndex lhs, TokenStream tokenStream) {
            // : unary_expression assignment_operator expression
            // note: lhs is passed in for perf reasons

            AssignmentOperatorType operatorType;

            switch (tokenStream.Current.Symbol) {
                case SymbolType.Assign:
                case SymbolType.AddAssign:
                case SymbolType.SubtractAssign:
                case SymbolType.MultiplyAssign:
                case SymbolType.DivideAssign:
                case SymbolType.ModAssign:
                case SymbolType.AndAssign:
                case SymbolType.OrAssign:
                case SymbolType.XorAssign:
                case SymbolType.LeftShiftAssign:
                case SymbolType.RightShiftAssign:
                    operatorType = GetAssignmentOperatorType(tokenStream.Current.Symbol);
                    tokenStream.location++;
                    break;

                default:
                    return default;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex rhs)) {
                return default;
            }

            // I actually need to report the token start from rhs
            return new RuleResult<AssignmentExpression>(tokenStream.location, expressionBuffer.Add(startLocation, tokenStream.location, new AssignmentExpression() {
                lhs = lhs,
                rhs = rhs,
                assignmentOperatorType = operatorType,
            }));
        }

        private static AssignmentOperatorType GetAssignmentOperatorType(SymbolType symbolType) {
            switch (symbolType) {
                case SymbolType.Assign: return AssignmentOperatorType.Assign;
                case SymbolType.AddAssign: return AssignmentOperatorType.AddAssign;
                case SymbolType.SubtractAssign: return AssignmentOperatorType.SubtractAssign;
                case SymbolType.MultiplyAssign: return AssignmentOperatorType.MultiplyAssign;
                case SymbolType.DivideAssign: return AssignmentOperatorType.DivideAssign;
                case SymbolType.ModAssign: return AssignmentOperatorType.ModAssign;
                case SymbolType.AndAssign: return AssignmentOperatorType.AndAssign;
                case SymbolType.OrAssign: return AssignmentOperatorType.OrAssign;
                case SymbolType.XorAssign: return AssignmentOperatorType.XorAssign;
                case SymbolType.LeftShiftAssign: return AssignmentOperatorType.LeftShiftAssign;
                case SymbolType.RightShiftAssign: return AssignmentOperatorType.RightShiftAssign;
                default: return AssignmentOperatorType.Invalid;
            }
        }

        private RuleResult ParseAssignment(TokenStream tokenStream) {

            ExpressionParseState state = SaveState(tokenStream);

            if (!ParseUnaryExpression(ref tokenStream, out ExpressionIndex lhs)) {
                return default;
            }

            expressionBuffer.PushRuleScope();

            expressionBuffer.MakeRule(ParseTypicalAssignment(state.location, lhs, tokenStream));
            expressionBuffer.MakeRule(ParseCoalesceAssignment(state.location, lhs, tokenStream));

            if (expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex winningIndex)) {
                return new RuleResult(tokenStream.location, winningIndex);
            }

            RestoreState(ref tokenStream, state);
            return default;

        }

        private RuleResult<AssignmentExpression> ParseCoalesceAssignment(int startLocation, ExpressionIndex lhs, TokenStream tokenStream) {
            // unary_expression '??=' throwable_expression
            // note: lhs is passed in for perf reasons

            if (!tokenStream.Consume(SymbolType.CoalesceAssign)) {
                return default;
            }

            if (!ParseThrowableExpression(ref tokenStream, out ExpressionIndex rhs)) {
                // hard error here
                return default;
            }

            return new RuleResult<AssignmentExpression>(tokenStream.location, expressionBuffer.Add(startLocation, tokenStream.location, new AssignmentExpression() {
                rhs = rhs,
                lhs = lhs,
                assignmentOperatorType = AssignmentOperatorType.CoalesceAssignment
            }));
        }

        private bool ParseThrowableExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {

            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(TemplateKeyword.Throw)) {

                // hard error 
                if (!ParseExpression(ref tokenStream, out ExpressionIndex thrownExpr)) {
                    RestoreState(ref tokenStream, state);
                    expressionIndex = default;
                    return false;
                }

                expressionIndex = expressionBuffer.Add(state.location, tokenStream.location, new ThrowStatement() {
                    expression = thrownExpr
                });

                return true;

            }

            return ParseExpression(ref tokenStream, out expressionIndex);
        }

        private bool ParseIdentifierWithTypeArguments(ref TokenStream tokenStream, out ExpressionIndex<Identifier> expressionIndex) {
            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeKeywordOrIdentifier(out Token keywordOrIdentifier)) {
                expressionIndex = default;
                return false;
            }

            ParseTypeArgumentList(ref tokenStream, out ExpressionRange<TypePath> typeArgumentList);

            expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new Identifier() {
                identifierTokenIndex = new NonTrivialTokenLocation(startLocation),
                typeArgumentList = typeArgumentList
            });

            return true;

        }

        private bool ParsePrimaryExpressionStart(ref TokenStream tokenStream, out ExpressionIndex expression) {
            // : literal                                   #literalExpression
            // | identifier type_argument_list?            #simpleNameExpression
            // | OPEN_PARENS expression CLOSE_PARENS       #parenthesisExpressions
            // | predefined_type                           #memberAccessExpression
            // | qualified_alias_member                    #memberAccessExpression
            // | LITERAL_ACCESS                            #literalAccessExpression
            // | THIS                                      #thisReferenceExpression
            // | BASE ('.' identifier type_argument_list? | '[' expression_list ']') #baseAccessExpression
            // | NEW (type_ (object_creation_expression
            //               | object_or_collection_initializer
            //               | '[' expression_list ']' rank_specifier* array_initializer?
            //               | rank_specifier+ array_initializer)
            //            | anonymous_object_initializer
            //            | rank_specifier array_initializer)                       #objectCreationExpression
            // | OPEN_PARENS argument ( ',' argument )+ CLOSE_PARENS           #tupleExpression
            // | TYPEOF OPEN_PARENS (unbound_type_name | type_ | VOID) CLOSE_PARENS   #typeofExpression
            // | CHECKED OPEN_PARENS expression CLOSE_PARENS                   #checkedExpression
            // | UNCHECKED OPEN_PARENS expression CLOSE_PARENS                 #uncheckedExpression
            // | DEFAULT (OPEN_PARENS type_ CLOSE_PARENS)?                     #defaultValueExpression
            // | ASYNC? DELEGATE (OPEN_PARENS explicit_anonymous_function_parameter_list? CLOSE_PARENS)? block #anonymousMethodExpression
            // | SIZEOF OPEN_PARENS type_ CLOSE_PARENS                          #sizeofExpression
            // // C# 6: https://msdn.microsoft.com/en-us/library/dn986596.aspx
            // | NAMEOF OPEN_PARENS (identifier '.')* identifier CLOSE_PARENS  #nameofExpression

            // I re-arranged this a little bit so we can keep the node size smaller and avoid expression buffer scopes

            if (ParseLiteralAccess(ref tokenStream, out ExpressionIndex<LiteralAccess> literalAccess)) {
                expression = literalAccess;
                return true;
            }

            if (ParseDefaultExpression(ref tokenStream, out ExpressionIndex<DefaultExpression> defaultExpr)) {
                expression = defaultExpr;
                return true;
            }

            if (ParseLiteral(ref tokenStream, out ExpressionIndex<Literal> literal)) {
                expression = literal;
                return true;
            }

            if (ParseResolveId(ref tokenStream, out ExpressionIndex<ResolveIdExpression> styleId)) {
                expression = styleId;
                return true;
            }
            
            if (ParseInterpolatedStringExpression(ref tokenStream, out ExpressionIndex stringInterpolation)) {
                expression = stringInterpolation;
                return true;
            }

            if (ParseThis(ref tokenStream, out ExpressionIndex<Identifier> thisExpr)) {
                expression = thisExpr;
                return true;
            }

            if (ParseTypeOfExpression(ref tokenStream, out ExpressionIndex<TypeOfExpression> typeOf)) {
                expression = typeOf;
                return true;
            }

            if (ParseSizeOfExpression(ref tokenStream, out ExpressionIndex<SizeOfExpression> sizeOf)) {
                expression = sizeOf;
                return true;
            }

            if (ParseNameOfExpression(ref tokenStream, out ExpressionIndex<NameOfExpression> nameOf)) {
                expression = nameOf;
                return true;
            }

            if (ParseNewExpression(ref tokenStream, out ExpressionIndex<NewExpression> newExpr)) {
                expression = newExpr;
                return true;
            }

            if (ParseBaseAccess(ref tokenStream, out ExpressionIndex<BaseAccessExpression> baseAccess)) {
                expression = baseAccess;
                return true;
            }

            // maybe need to use a scope to handle tuple vs paren, we'll have to see via testing 
            if (ParseParenExpression(ref tokenStream, out ExpressionIndex<ParenExpression> parens)) {
                expression = parens;
                return true;
            }

            if (ParseTupleExpression(ref tokenStream, out ExpressionIndex<TupleExpression> tuple)) {
                expression = tuple;
                return true;
            }

            if (ParsePredefinedType(ref tokenStream, out ExpressionIndex<PrimaryIdentifier> predefinedType)) {
                expression = predefinedType;
                return true;
            }

            // competes with predefined type, must come second 
            if (ParsePrimaryIdentifier(ref tokenStream, out ExpressionIndex<PrimaryIdentifier> primaryIdentifier)) {
                expression = primaryIdentifier;
                return true;
            }

            if (ParseAnonymousMethodExpression(ref tokenStream, out ExpressionIndex<AnonymousMethodExpression> anonymous)) {
                expression = anonymous;
                return true;
            }

            if (ParseCheckedUncheckedExpression(ref tokenStream, out ExpressionIndex<CheckedExpression> checkedExpression)) {
                expression = checkedExpression;
                return true;
            }

            expression = default;
            return false;
        }

        private bool ParsePrimaryIdentifier(ref TokenStream tokenStream, out ExpressionIndex<PrimaryIdentifier> expressionIndex) {
            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeAnyIdentifier(out NonTrivialTokenLocation tokenLocation)) {
                expressionIndex = default;
                return false;
            }

            ParseTypeArgumentList(ref tokenStream, out ExpressionRange<TypePath> typeArgumentList);

            expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new PrimaryIdentifier() {
                identifierLocation = tokenLocation,
                typeArgumentList = typeArgumentList
            });

            return true;

        }

        private bool ParseLiteralAccess(ref TokenStream tokenStream, out ExpressionIndex<LiteralAccess> literalAccess) {
            // [0-9] ('_'* [0-9])* IntegerTypeSuffix? '.' '@'? IdentifierOrKeyword
            literalAccess = default;
            return false;
        }

        private bool ParseAnonymousMethodExpression(ref TokenStream tokenStream, out ExpressionIndex<AnonymousMethodExpression> anonymous) {
            // ASYNC? DELEGATE (OPEN_PARENS explicit_anonymous_function_parameter_list? CLOSE_PARENS)? block
            anonymous = default;
            return false;
        }

        private bool ParseCheckedUncheckedExpression(ref TokenStream tokenStream, out ExpressionIndex<CheckedExpression> checkedExpr) {
            // ('checked | 'unchecked') OPEN_PARENS expression CLOSE_PARENS
            checkedExpr = default;
            return false;
        }

        private bool ParseBaseAccess(ref TokenStream tokenStream, out ExpressionIndex<BaseAccessExpression> baseAccess) {
            // BASE ('.' identifier type_argument_list? | '[' expression_list ']')

            if (!tokenStream.Peek(TemplateKeyword.Base)) {
                baseAccess = default;
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);
            tokenStream.location++;

            if (tokenStream.Consume(SymbolType.Dot)) {
                if (!ParseIdentifierWithTypeArguments(ref tokenStream, out ExpressionIndex<Identifier> identifier)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    baseAccess = default;
                    return false;
                }

                baseAccess = expressionBuffer.Add(state.location, tokenStream.location, new BaseAccessExpression() {
                    identifier = identifier,
                });
                return true;
            }

            if (tokenStream.Consume(SymbolType.SquareBraceOpen)) {

                if (!ParseExpressionList(ref tokenStream, out ExpressionRange expressionList)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    baseAccess = default;
                    return false;
                }

                if (!tokenStream.Consume(SymbolType.SquareBraceClose)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    baseAccess = default;
                    return false;
                }

                baseAccess = expressionBuffer.Add(state.location, tokenStream.location, new BaseAccessExpression() {
                    indexExpressions = expressionList
                });
            }

            // hard error
            RestoreState(ref tokenStream, state);
            baseAccess = default;
            return false;
        }

        private bool ParseExpressionListWithHardErrors(ref TokenStream tokenStream, bool canBeEmptyList, HelpType helpData, out ExpressionRange expressionList) {
            expressionList = default;

            if (tokenStream.IsEmpty && canBeEmptyList) {
                return true;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex firstExpression)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, helpData);
            }

            using ScopedList<ExpressionIndex> list = scopedAllocator.CreateListScope<ExpressionIndex>(16);

            list.Add(firstExpression);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseExpression(ref tokenStream, out ExpressionIndex next)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, helpData);
                }

                list.Add(next);
            }

            expressionList = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private bool ParseExpressionList(ref TokenStream tokenStream, out ExpressionRange expressionList) {
            expressionList = default;
            ExpressionParseState state = SaveState(tokenStream);
            if (!ParseExpression(ref tokenStream, out ExpressionIndex firstExpression)) {
                return default;
            }

            using ScopedList<ExpressionIndex> list = scopedAllocator.CreateListScope<ExpressionIndex>(16);

            list.Add(firstExpression);
            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseExpression(ref tokenStream, out ExpressionIndex next)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                list.Add(next);
            }

            expressionList = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private bool ParseNameOfExpression(ref TokenStream tokenStream, out ExpressionIndex<NameOfExpression> nameOf) {

            // todo I think this grammar is wrong, not account for things like nameof(List<string>)

            // NAMEOF OPEN_PARENS (identifier '.')* identifier CLOSE_PARENS

            nameOf = default;
            if (tokenStream.Current.Keyword != TemplateKeyword.NameOf) {
                return default;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.location++;

            if (!tokenStream.Consume(SymbolType.OpenParen)) {
                // probably a hard error
                return default;
            }

            // todo -- need a chain here, not just 1 identifier, also probably needs to support type paths & unbound paths 

            if (!ParseIdentifier(ref tokenStream, out ExpressionIndex<Identifier> identifier)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            nameOf = expressionBuffer.Add(state.location, tokenStream.location, new NameOfExpression() {
                identifier = identifier
            });

            return true;
        }

        private bool ParseSizeOfExpression(ref TokenStream tokenStream, out ExpressionIndex<SizeOfExpression> sizeOf) {

            if (tokenStream.Current.Keyword != TemplateKeyword.Sizeof) {
                sizeOf = default;
                return default;
            }

            sizeOf = default;

            ExpressionParseState state = SaveState(tokenStream);
            int start = tokenStream.location;

            tokenStream.location++;

            if (!tokenStream.Consume(SymbolType.OpenParen)) {
                // probably a hard error
                return default;
            }

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            // expressionBuffer.Get(index).tokenRange.end = tokenStream.location;
            sizeOf = expressionBuffer.Add(start, tokenStream.location, new SizeOfExpression() {
                typePath = typePath
            });
            return true;

        }

        private bool ParseDefaultExpression(ref TokenStream tokenStream, out ExpressionIndex<DefaultExpression> defaultExpr) {
            // DEFAULT (OPEN_PARENS type_ CLOSE_PARENS)?  

            defaultExpr = default;

            if (tokenStream.Current.Keyword != TemplateKeyword.Default) {
                return default;
            }

            ExpressionParseState state = SaveState(tokenStream);
            int start = tokenStream.location;

            tokenStream.location++;

            if (!tokenStream.Consume(SymbolType.OpenParen)) {
                defaultExpr = expressionBuffer.Add(start, tokenStream.location, new DefaultExpression());
                return true;
            }

            if (!ParseTypePath(ref tokenStream, out var typePath)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                // probably a hard error 
                RestoreState(ref tokenStream, state);
                return default;
            }

            defaultExpr = expressionBuffer.Add(start, tokenStream.location, new DefaultExpression() {
                typePath = typePath
            });

            return true;
        }

        private bool ParseTupleExpression(ref TokenStream tokenStream, out ExpressionIndex<TupleExpression> tupleExpr) {
            // OPEN_PARENS argument ( ',' argument )+ CLOSE_PARENS  
            tupleExpr = default;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.Consume(SymbolType.OpenParen);

            if (!ParseArgumentList(ref tokenStream, out ExpressionRange<Argument> argList)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            tupleExpr = expressionBuffer.Add(state.location, tokenStream.location, new TupleExpression() {
                arguments = argList
            });

            return true;
        }

        private bool ParseTypeOfExpression(ref TokenStream tokenStream, out ExpressionIndex<TypeOfExpression> typeOf) {

            if (tokenStream.Current.Keyword != TemplateKeyword.Typeof) {
                typeOf = default;
                return default;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.location++;

            typeOf = default;

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.TypeOfSyntax, out TokenStream parenStream)) {
                return false;
            }

            if (parenStream.Consume(TemplateKeyword.Void)) {
                if (parenStream.HasMoreTokens) {
                    return HardError(parenStream.location, DiagnosticError.UnexpectedToken);
                }

                typeOf = expressionBuffer.Add(state.location, parenStream.location, new TypeOfExpression() {
                    isVoidType = true
                });
                return true;
            }

            expressionBuffer.PushRuleScope();

            expressionBuffer.MakeRule(ParseTypeOfTypePathRule(parenStream));
            expressionBuffer.MakeRule(ParseUnboundTypeNameRule(parenStream));

            if (!expressionBuffer.PopRuleScope(ref parenStream, out ExpressionIndex index)) {
                return HardError(state.location, DiagnosticError.ExpectedTypePath, HelpType.TypeOfSyntax);
            }

            typeOf = new ExpressionIndex<TypeOfExpression>(index);

            return true;

        }

        private bool ParseUnboundTypeName(ref TokenStream tokenStream, out ExpressionIndex<TypeOfExpression> unboundName) {
            // unbound_type_name
            //     : identifier generic_dimension_specifier? ('.' identifier generic_dimension_specifier?)*

            unboundName = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return false;
            }

            ParseGenericDimensionSpecifier(ref tokenStream, out int degree);

            using ScopedList<ExpressionIndex<UnboundTypeNameExpression>> list = scopedAllocator.CreateListScope<ExpressionIndex<UnboundTypeNameExpression>>(8);

            list.Add(expressionBuffer.Add(startLocation, tokenStream.location, new UnboundTypeNameExpression() {
                genericDegree = degree,
                nameLocation = identifier
            }));

            while (tokenStream.Consume(SymbolType.Dot)) {

                if (!tokenStream.ConsumeStandardIdentifier(out identifier)) {
                    return false;
                }

                ParseGenericDimensionSpecifier(ref tokenStream, out degree);

                list.Add(expressionBuffer.Add(startLocation, tokenStream.location, new UnboundTypeNameExpression() {
                    genericDegree = degree,
                    nameLocation = identifier
                }));
            }

            unboundName = expressionBuffer.Add(startLocation, tokenStream.location, new TypeOfExpression() {
                unboundTypeName = expressionBuffer.AddExpressionList(list)
            });
            return true;

        }

        private bool ParseGenericDimensionSpecifier(ref TokenStream tokenStream, out int degree) {
            degree = 0;

            if (tokenStream.Current.Symbol != SymbolType.LessThan) {
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);
            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.AngleBrackets, default, out TokenStream stream)) {
                return false;
            }

            while (stream.Consume(SymbolType.Comma)) {
                degree++;
            }

            if (stream.HasMoreTokens) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            return true;

        }

        private bool ParseObjectCreationArguments(ref TokenStream tokenStream, out ExpressionRange<Argument> argList) {
            // : OPEN_PARENS argument_list? CLOSE_PARENS 

            argList = default;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.ObjectCreationSyntax, out TokenStream stream)) {
                return false;
            }

            PushRecoveryPoint(stream.start, stream.end + 1);

            ParseArgumentList(ref stream, out argList);

            return !PopRecoveryPoint(ref stream);

        }

        private bool ParseNewExpression(ref TokenStream tokenStream, out ExpressionIndex<NewExpression> newExpression) {
            // NEW (type_    (object_creation_expression
            //               | object_or_collection_initializer
            //               | '[' expression_list ']' rank_specifier* array_initializer?
            //               | rank_specifier+ array_initializer
            //               )
            //            | anonymous_object_initializer
            //            | rank_specifier array_initializer)   

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.New)) {
                newExpression = default;
                return false;
            }

            if (tokenStream.Peek(SymbolType.OpenParen)) {
                return ParseTypeCreationWithArgs(ref tokenStream, startLocation, default, out newExpression);
            }
            
            if (ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {

                if (tokenStream.Peek(SymbolType.SquareBraceOpen)) {

                    if (ParseArrayCreationWithExpressions(ref tokenStream, startLocation, typePath, out newExpression)) {
                        return true;
                    }

                    return HardError(tokenStream.location, DiagnosticError.ExpectedParenSquareBracketOrCurlyBrace, HelpType.NewExpressionSyntax);

                }

                if (tokenStream.Peek(SymbolType.OpenParen)) {
                    return ParseTypeCreationWithArgs(ref tokenStream, startLocation, typePath, out newExpression);
                }

                if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {

                    expressionBuffer.PushRuleScope();
                    expressionBuffer.MakeRule(ParseTypeCreationWithInitializerRule(startLocation, typePath, tokenStream));
                    expressionBuffer.MakeRule(ParseArrayCreationWithoutSizeExpressionsRule(startLocation, typePath, tokenStream));
                    if (expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex winningIndex)) {
                        newExpression = new ExpressionIndex<NewExpression>(winningIndex);
                        return true;
                    }
                    // if (ParseArrayCreationWithoutSizeExpressions(ref tokenStream, startLocation, typePath, out newExpression)) {
                    //     return true;
                    // }
                    //
                    // if (ParseTypeCreationWithInitializer(ref tokenStream, startLocation, typePath, out newExpression)) {
                    //     return true;
                    // }

                }

                newExpression = default;
                return HardError(tokenStream.location, DiagnosticError.ExpectedParenSquareBracketOrCurlyBrace, HelpType.NewExpressionSyntax);

            }

            if (ParseAnonymousArrayCreation(ref tokenStream, startLocation, out newExpression)) {
                return true;
            }
            // | anonymous_object_initializer
            // | rank_specifier array_initializer

            newExpression = default;
            return false;
        }

        private bool ParseAnonymousArrayCreation(ref TokenStream tokenStream, int startLocation, out ExpressionIndex<NewExpression> newExpression) {

            using ScopedList<ExpressionIndex<ArrayCreationRank>> list = scopedAllocator.CreateListScope<ExpressionIndex<ArrayCreationRank>>(4);
            int rankStartLocation = tokenStream.location;

            ExpressionParseState state = SaveState(tokenStream);

            while (ParseBlankArrayRankSpecifier(ref tokenStream, out int rank)) {
                list.Add(expressionBuffer.Add(rankStartLocation, tokenStream.location, new ArrayCreationRank() {
                    rank = rank,
                    expressionList = default
                }));
                rankStartLocation = tokenStream.location;
            }

            if (ParseCollectionInitializer(ref tokenStream, out ExpressionIndex<CollectionInitializer> initializer)) {
                newExpression = expressionBuffer.Add(startLocation, tokenStream.location, new NewExpression() {
                    initializer = initializer,
                    arraySpecs = expressionBuffer.AddExpressionList(list),
                    typePath = default,
                });
                return true;
            }

            RestoreState(ref tokenStream, state);
            newExpression = default;
            return false;
        }

        private bool ParseBlankArrayRankSpecifier(ref TokenStream tokenStream, out int rank) {

            rank = default;
            if (tokenStream.Current.Symbol != SymbolType.SquareBraceOpen) {
                return false;
            }

            int startLocation = tokenStream.location;
            tokenStream.location++;

            rank++;

            while (tokenStream.Consume(SymbolType.Comma)) {
                // forever
                rank++;
            }

            if (tokenStream.Current.Symbol != SymbolType.SquareBraceClose) {
                tokenStream.location = startLocation;
                return false;
            }

            tokenStream.location++;

            return true;

        }

        private bool ParseArrayCreationWithoutSizeExpressions(ref TokenStream tokenStream, int startLocation, ExpressionIndex<TypePath> typePath, out ExpressionIndex<NewExpression> newExpression) {
            // : array_initializer
            newExpression = default;

            if (!ParseCollectionInitializer(ref tokenStream, out ExpressionIndex<CollectionInitializer> arrayInitializer)) {
                return false;
            }

            newExpression = expressionBuffer.Add(startLocation, tokenStream.location, new NewExpression() {
                initializer = arrayInitializer,
                arraySpecs = default,
                typePath = typePath
            });
            return true;
        }

        private bool ParseArrayCreationWithExpressions(ref TokenStream tokenStream, int startLocation, ExpressionIndex<TypePath> typePath, out ExpressionIndex<NewExpression> newExpression) {
            // : '[' expression_list ']' rank_specifier* array_initializer?

            newExpression = default;
            int openStart = tokenStream.location;
            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.SquareBrackets, HelpType.ArrayInitSyntax, out TokenStream stream)) {
                return false;
            }

            PushRecoveryPoint(stream.start, stream.end + 1);

            ParseExpressionList(ref stream, out ExpressionRange expressionList);

            if (PopRecoveryPoint(ref stream)) {
                return false;
            }

            ExpressionIndex<ArrayCreationRank> sizeSpec = expressionBuffer.Add(openStart, tokenStream.location, new ArrayCreationRank() {
                expressionList = expressionList,
                rank = expressionList.length
            });

            using ScopedList<ExpressionIndex<ArrayCreationRank>> arrayRankList = scopedAllocator.CreateListScope<ExpressionIndex<ArrayCreationRank>>(4);
            arrayRankList.Add(sizeSpec);

            // todo -- rank specifiers 
            // ParseRankSpecifierList(ref tokenStream, out  rank);

            ParseCollectionInitializer(ref tokenStream, out ExpressionIndex<CollectionInitializer> collectionInit);

            newExpression = expressionBuffer.Add(startLocation, tokenStream.location, new NewExpression() {
                initializer = collectionInit,
                argList = default,
                arraySpecs = expressionBuffer.AddExpressionList(arrayRankList),
                typePath = typePath
            });

            return true;
        }

        private bool ParseTypeCreationWithInitializer(ref TokenStream tokenStream, int startLocation, ExpressionIndex<TypePath> typePath, out ExpressionIndex<NewExpression> newExpression) {
            // object_or_collection_initializer

            ExpressionParseState state = SaveState(tokenStream);
            if (ParseObjectInitializer(ref tokenStream, out ExpressionIndex<ObjectInitializer> initializer)) {
                newExpression = expressionBuffer.Add(startLocation, tokenStream.location, new NewExpression() {
                    initializer = initializer,
                    typePath = typePath,
                    argList = default,
                });

                return true;
            }

            RestoreState(ref tokenStream, state);

            if (!ParseCollectionInitializer(ref tokenStream, out ExpressionIndex<CollectionInitializer> collectionInit)) {
                RestoreState(ref tokenStream, state);
                newExpression = default;
                return false;
            }

            newExpression = expressionBuffer.Add(startLocation, tokenStream.location, new NewExpression() {
                initializer = collectionInit,
                typePath = typePath,
                argList = default,
            });

            return true;

        }

        private bool ParseTypeCreationWithArgs(ref TokenStream tokenStream, int expressionStart, ExpressionIndex<TypePath> typePath, out ExpressionIndex<NewExpression> newExpression) {
            // object_creation_argument object_or_collection_initializer?

            if (!ParseObjectCreationArguments(ref tokenStream, out ExpressionRange<Argument> argList)) {
                newExpression = default;
                return false;
            }

            ParseObjectOrCollectionInitializer(ref tokenStream, out ExpressionIndex initializer);

            newExpression = expressionBuffer.Add(expressionStart, tokenStream.location, new NewExpression() {
                initializer = initializer,
                typePath = typePath,
                argList = argList
            });

            return true;
        }

        private bool ParseObjectInitializer(ref TokenStream tokenStream, out ExpressionIndex<ObjectInitializer> objectInitializer) {
            // OPEN_BRACE (member_initializer_list ','?)? CLOSE_BRACE

            int startLocation = tokenStream.location;

            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Current.Symbol != SymbolType.CurlyBraceOpen) {
                objectInitializer = default;
                return default;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.ObjectInitializerSyntax, out TokenStream stream)) {
                objectInitializer = default;
                return false;
            }

            // can't handle recovery because we use this in a rule scope 
            // PushRecoveryPoint(stream.start, stream.end + 1);
            ParseMemberInitializerList(ref stream, out ExpressionRange<MemberInitializer> memberInit);

            // allow trailing comma 
            stream.Consume(SymbolType.Comma);

            // if (PopRecoveryPoint(ref stream)) {
            if (stream.HasMoreTokens) {
                RestoreState(ref tokenStream, state);
                objectInitializer = default;
                return false;
            }

            objectInitializer = expressionBuffer.Add(startLocation, tokenStream.location, new ObjectInitializer() {
                memberInit = memberInit
            });

            return true;

        }

        private bool ParseMemberInitializer(ref TokenStream tokenStream, out ExpressionIndex<MemberInitializer> init) {
            // : (identifier | '[' expression_list ']') '=' initializer_value

            ExpressionParseState state = SaveState(tokenStream);
            ExpressionRange expressionRange = default;

            if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                if (!tokenStream.Consume(SymbolType.Assign)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return false;
                }

                if (!ParseInitializerValue(ref tokenStream, out ExpressionIndex initializerValue)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return false;
                }

                init = expressionBuffer.Add(state.location, tokenStream.location, new MemberInitializer() {
                    lhsIdentifier = identifierLocation,
                    lhsExpressionList = expressionRange,
                    rhs = initializerValue,
                    initializerType = ElementInitializerType.SingleElement
                });
                return true;
            }

            if (tokenStream.Consume(SymbolType.SquareBraceOpen)) {

                // probably a hard error
                if (!ParseExpressionList(ref tokenStream, out expressionRange)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return default;
                }

                // probably a hard error 
                if (!tokenStream.Consume(SymbolType.SquareBraceClose)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return default;
                }

                if (!tokenStream.Consume(SymbolType.Assign)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return false;
                }

                if (!ParseInitializerValue(ref tokenStream, out ExpressionIndex initializerValue)) {
                    RestoreState(ref tokenStream, state);
                    init = default;
                    return false;
                }

                init = expressionBuffer.Add(state.location, tokenStream.location, new MemberInitializer() {
                    lhsIdentifier = identifierLocation,
                    lhsExpressionList = expressionRange,
                    rhs = initializerValue,
                    initializerType = ElementInitializerType.Indexer
                });
                return true;
            }

            init = default;
            return false;

        }

        private bool ParseInitializerValue(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // : expression
            // | object_or_collection_initializer
            expressionBuffer.PushRuleScope();

            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseObjectOrCollectionInitializer(tokenStream));

            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private bool ParseExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            expressionBuffer.PushRuleScope();

            // todo both branches want a unary expression, see if we can't extract it and pass it through 

            expressionBuffer.MakeRule(ParseAssignment(tokenStream));
            expressionBuffer.MakeRule(ParseNonAssignmentRule(tokenStream));

            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private bool ParseLambdaExpression(ref TokenStream tokenStream, out ExpressionIndex<LambdaExpression> lambda) {
            // also handles relaxed_lambda
            // relaxed_lambda: '(' argument_list? ')' block

            ExpressionParseState state = SaveState(tokenStream);

            bool isAsync = tokenStream.Consume(TemplateKeyword.Async);

            if (!ParseLambdaSignature(ref tokenStream, out bool hasInformalParameters, out bool hasFormalParameters, out ExpressionRange<Parameter> parameterList)) {
                lambda = default;
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.FatArrow)) {

                // validate signature here 
                if (hasInformalParameters && hasFormalParameters) {
                    lambda = default;
                    return HardError(state.location, DiagnosticError.LambdaParameterTypesCannotBeBothFormalAndAnonymous, HelpType.LambdaSyntax);
                }

                if (!ParseAnonymousFunctionBody(ref tokenStream, out ExpressionIndex body)) {
                    lambda = default;
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                lambda = expressionBuffer.Add(state.location, tokenStream.location, new LambdaExpression() {
                    isAsync = isAsync,
                    hasFormalParameters = hasFormalParameters,
                    parameters = parameterList,
                    body = body
                });
                return true;
            }

            if (ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> block)) {
                lambda = expressionBuffer.Add(state.location, tokenStream.location, new LambdaExpression() {
                    isAsync = isAsync,
                    hasFormalParameters = hasFormalParameters,
                    parameters = parameterList,
                    body = block
                });
                return true;

            }

            lambda = default;
            RestoreState(ref tokenStream, state);
            return false;
        }

        private bool ParseAnonymousFunctionBody(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseThrowableExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseBlockRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private bool RequireBlockWithRecovery(ref TokenStream tokenStream, out ExpressionIndex<BlockExpression> blockExpression) {
            // OPEN_BRACE statement_list? CLOSE_BRACE

            blockExpression = default;

            int startLocation = tokenStream.location;

            if (tokenStream.Current.Symbol != SymbolType.CurlyBraceOpen) {
                return HardError(tokenStream, DiagnosticError.ExpectedCurlyBrace);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream subStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedCurlyBrace);
            }

            PushRecoveryPoint(subStream.start, subStream.end + 1);
            using ScopedList<ExpressionIndex> buffer = scopedAllocator.CreateListScope<ExpressionIndex>(32);

            while (ParseStatement(ref subStream, out ExpressionIndex expressionIndex)) {
                buffer.Add(expressionIndex);
            }

            if (PopRecoveryPoint(ref subStream)) {
                return false;
            }

            blockExpression = expressionBuffer.Add(startLocation, tokenStream.location, new BlockExpression() {
                isUnsafe = false,
                statementList = expressionBuffer.AddExpressionList(buffer)
            });

            return true;
        }

        private bool ParseRequiredBlock(ref TokenStream tokenStream, HelpType helpData, out ExpressionIndex<BlockExpression> blockExpression) {

            blockExpression = default;
            int startLocation = tokenStream.location;

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, helpData, out TokenStream blockStream)) {
                return false;
            }

            PushRecoveryPoint(blockStream.start, blockStream.end + 1);

            using ScopedList<ExpressionIndex> buffer = scopedAllocator.CreateListScope<ExpressionIndex>(32);

            while (ParseStatement(ref blockStream, out ExpressionIndex expressionIndex)) {

                buffer.Add(expressionIndex);

            }

            if (PopRecoveryPoint(ref blockStream) == RecoveryPoint.k_HasErrors) {
                return false;
            }

            blockExpression = expressionBuffer.Add(startLocation, tokenStream.location, new BlockExpression() {
                statementList = expressionBuffer.AddExpressionList(buffer)
            });

            return true;
        }

        private bool ParseBlock(ref TokenStream tokenStream, out ExpressionIndex<BlockExpression> blockExpression) {
            // OPEN_BRACE statement_list? CLOSE_BRACE

            blockExpression = default;

            int startLocation = tokenStream.location;
            if (tokenStream.Current.Symbol != SymbolType.CurlyBraceOpen) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, default, out TokenStream blockStream)) {
                return false;
            }

            using ScopedList<ExpressionIndex> buffer = scopedAllocator.CreateListScope<ExpressionIndex>(32);

            PushRecoveryPoint(blockStream.start, blockStream.end + 1);

            while (ParseStatement(ref blockStream, out ExpressionIndex expressionIndex)) {
                buffer.Add(expressionIndex);
            }

            if (PopRecoveryPoint(ref blockStream) == RecoveryPoint.k_HasErrors) {
                return false;
            }

            blockExpression = expressionBuffer.Add(startLocation, tokenStream.location, new BlockExpression() {
                isUnsafe = false,
                statementList = expressionBuffer.AddExpressionList(buffer)
            });

            return true;

        }

        private bool ParseStatement(ref TokenStream tokenStream, out ExpressionIndex expression) {
            if (tokenStream.IsEmpty) {
                expression = default;
                return false;
            }

            expressionBuffer.PushRuleScope();
            // expressionBuffer.MakeRule(ParseLabeledStatementRule(tokenStream)); we don't support labels
            expressionBuffer.MakeRule(ParseDeclarationStatementRule(tokenStream));
            expressionBuffer.MakeRule(ParseEmbeddedStatementRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expression);
        }

        private bool ParseSimpleEmbeddedStatement(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            //: ';'                                                         #theEmptyStatement
            //| expression ';'                                              #expressionStatement

            //// selection statements
            //| IF OPEN_PARENS expression CLOSE_PARENS if_body (ELSE if_body)?               #ifStatement
            //| SWITCH OPEN_PARENS expression CLOSE_PARENS OPEN_BRACE switch_section* CLOSE_BRACE           #switchStatement

            //// iteration statements
            //| WHILE OPEN_PARENS expression CLOSE_PARENS embedded_statement                                        #whileStatement
            //| DO embedded_statement WHILE OPEN_PARENS expression CLOSE_PARENS ';'                                 #doStatement
            //| FOR OPEN_PARENS for_initializer? ';' expression? ';' for_iterator? CLOSE_PARENS embedded_statement  #forStatement
            //| AWAIT? FOREACH OPEN_PARENS local_variable_type identifier IN expression CLOSE_PARENS embedded_statement    #foreachStatement

            //// jump statements
            //| BREAK ';'                                                   #breakStatement
            //| CONTINUE ';'                                                #continueStatement
            //| GOTO (identifier | CASE expression | DEFAULT) ';'           #gotoStatement
            //| RETURN expression? ';'                                      #returnStatement
            //| THROW expression? ';'                                       #throwStatement

            //| TRY block (catch_clauses finally_clause? | finally_clause)  #tryStatement
            //| CHECKED block                                               #checkedStatement
            //| UNCHECKED block                                             #uncheckedStatement
            //| LOCK OPEN_PARENS expression CLOSE_PARENS embedded_statement                  #lockStatement
            //| USING OPEN_PARENS resource_acquisition CLOSE_PARENS embedded_statement       #usingStatement
            //| YIELD (RETURN expression | BREAK) ';'                       #yieldStatement

            //// unsafe statements
            //| UNSAFE block                                                                       #unsafeStatement
            //| FIXED OPEN_PARENS pointer_type fixed_pointer_declarators CLOSE_PARENS embedded_statement            #fixedStatement

            // handle the empty statement 
            if (tokenStream.Consume(SymbolType.SemiColon)) {
                expressionIndex = default;
                return true;
            }

            TemplateKeyword keyword = tokenStream.Current.Keyword;

            switch (keyword) {
                case TemplateKeyword.For: {
                    bool result = ParseForLoop(ref tokenStream, out ExpressionIndex<ForLoop> forLoopExpression);
                    expressionIndex = forLoopExpression;
                    return result;
                }

                case TemplateKeyword.Foreach: {
                    bool result = ParseForeachLoopStatement(ref tokenStream, out ExpressionIndex<ForeachLoop> foreachLoop);
                    expressionIndex = foreachLoop;
                    return result;
                }

                case TemplateKeyword.If: {
                    ParseIfStatement(ref tokenStream, out ExpressionIndex<IfStatement> ifExpression);
                    expressionIndex = ifExpression;
                    return true;
                }

                case TemplateKeyword.While: {
                    bool result = ParseWhileStatement(ref tokenStream, out ExpressionIndex<WhileLoop> whileLoop);
                    expressionIndex = whileLoop;
                    return result;
                }

                case TemplateKeyword.Do: {
                    bool result = ParseDoWhileStatement(ref tokenStream, out ExpressionIndex<WhileLoop> doWhileLoop);
                    expressionIndex = doWhileLoop;
                    return result;
                }

                case TemplateKeyword.Switch: {
                    bool result = ParseSwitchStatement(ref tokenStream, out ExpressionIndex<SwitchStatement> switchStatement);
                    expressionIndex = switchStatement;
                    return result;
                }

                case TemplateKeyword.Break: {
                    bool result = ParseBreakStatement(ref tokenStream, out ExpressionIndex<BreakStatement> breakStatement);
                    expressionIndex = breakStatement;
                    return result;
                }

                case TemplateKeyword.Continue: {
                    bool result = ParseContinueStatement(ref tokenStream, out ExpressionIndex<ContinueStatement> continueStatement);
                    expressionIndex = continueStatement;
                    return result;
                }

                case TemplateKeyword.Return: {
                    bool result = ParseReturnStatement(ref tokenStream, out ExpressionIndex<ReturnStatement> returnStatement);
                    expressionIndex = returnStatement;
                    return result;
                }

                case TemplateKeyword.Throw: {
                    bool result = ParseThrowStatement(ref tokenStream, out ExpressionIndex<ThrowStatement> throwStatement);
                    expressionIndex = throwStatement;
                    return result;
                }

                case TemplateKeyword.Try: {
                    bool result = ParseTryCatchStatement(ref tokenStream, out ExpressionIndex<TryCatchFinally> tryCatchFinally);
                    expressionIndex = tryCatchFinally;
                    return result;
                }

                case TemplateKeyword.Lock: {
                    bool result = ParseLockStatement(ref tokenStream, out ExpressionIndex<LockStatement> lockStatement);
                    expressionIndex = lockStatement;
                    return result;
                }

                case TemplateKeyword.Using: {
                    bool result = ParseUsingStatementEmbeddable(ref tokenStream, out ExpressionIndex<UsingStatement> usingStatement);
                    expressionIndex = usingStatement;
                    return result;
                }

                case TemplateKeyword.Yield: {
                    bool result = ParseYieldStatement(ref tokenStream, out ExpressionIndex<YieldStatement> yieldStatement);
                    expressionIndex = yieldStatement;
                    return result;
                }
            }

            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream terminatedStream)) {
                expressionIndex = default;
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                expressionIndex = default; // maybe a hard error?
                return false;
            }

            //PushRecoveryPoint(terminatedStream.start, terminatedStream.end + 1);
            if (ParseExpression(ref terminatedStream, out expressionIndex) && !terminatedStream.HasMoreTokens) {
                return true;
            }

            RestoreState(ref tokenStream, state);
            return false;

            //return !PopRecoveryPoint(ref terminatedStream) && expressionValid;

        }

        private bool ParseStackAllocInitializer(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // : STACKALLOC type_ '[' expression ']'
            // | STACKALLOC type_? '[' expression? ']' OPEN_BRACE expression (',' expression)* ','? CLOSE_BRACE
            if (tokenStream.Consume(TemplateKeyword.Stackalloc)) {
                throw new NotImplementedException("Stackalloc is not implemented");
            }

            expressionIndex = default;
            return false;
        }

        private bool ParseYieldStatement(ref TokenStream tokenStream, out ExpressionIndex<YieldStatement> yieldStatement) {
            // YIELD (RETURN expression | BREAK) ';'

            int startLocation = tokenStream.location;
            yieldStatement = default;

            if (!tokenStream.Consume(TemplateKeyword.Yield)) {
                return default;
            }

            if (!tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream stream)) {
                return HardError(tokenStream, DiagnosticError.ExpectedReturnOrBreak, HelpType.YieldSyntax);
            }

            PushRecoveryPoint(stream.start, stream.end + 1);

            ExpressionIndex expressionIndex = default;

            if (stream.Consume(TemplateKeyword.Return)) {
                if (!ParseExpression(ref stream, out expressionIndex)) {
                    HardError(stream, DiagnosticError.ExpectedExpression, HelpType.YieldSyntax);
                }
            }
            else if (!stream.Consume(TemplateKeyword.Break)) {
                HardError(stream, DiagnosticError.ExpectedReturnOrBreak, HelpType.YieldSyntax);
            }

            if (PopRecoveryPoint(ref stream)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.YieldSyntax);
            }

            yieldStatement = expressionBuffer.Add(startLocation, tokenStream.location, new YieldStatement() {
                expression = expressionIndex
            });

            return true;

        }

        private bool ParseUsingDeclaration(ref TokenStream tokenStream, out ExpressionIndex<UsingStatement> usingStatement) {
            // : USING resource_acquisition ';'
            usingStatement = default;
            int startLocation = tokenStream.location;
            ExpressionParseState state = SaveState(tokenStream);
            if (!tokenStream.Consume(TemplateKeyword.Using)) {
                return false;
            }

            // can't be a hard error because this need to get picked up by the embedded version
            // probably need to find a better way to report error since this is likely failure case
            // if we got a using and didn't get a resource acquisition then we're in bad shape

            if (tokenStream.Peek(SymbolType.OpenParen)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!ParseResourceAcquisition(ref tokenStream, out ExpressionIndex acquisition)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpressionOrVariableDeclaration, HelpType.UsingStatementSyntax);
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.UsingStatementSyntax);
            }

            usingStatement = expressionBuffer.Add(startLocation, tokenStream.location, new UsingStatement() {
                acquisition = acquisition,
                body = default,
            });

            return true;

        }

        private bool ParseUsingStatementEmbeddable(ref TokenStream tokenStream, out ExpressionIndex<UsingStatement> usingStatement) {
            // : USING OPEN_PARENS resource_acquisition CLOSE_PARENS embedded_statement

            int startLocation = tokenStream.location;
            usingStatement = default;

            if (!tokenStream.Consume(TemplateKeyword.Using)) {
                return false;
            }

            if (tokenStream.Peek(SymbolType.OpenParen)) {

                if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.UsingStatementSyntax, out TokenStream headerStream)) {
                    return false;
                }

                PushRecoveryPoint(headerStream.start, headerStream.end + 1);
                if (!ParseResourceAcquisition(ref headerStream, out ExpressionIndex acquisition)) {
                    HardError(headerStream, DiagnosticError.ExpectedExpressionOrVariableDeclaration, HelpType.UsingStatementSyntax);
                }

                if (PopRecoveryPoint(ref headerStream)) {
                    return false;
                }

                if (!ParseEmbeddedStatementWithHardErrors(ref tokenStream, HelpType.UsingStatementSyntax, out ExpressionIndex body)) {
                    return false;
                }

                usingStatement = expressionBuffer.Add(startLocation, tokenStream.location, new UsingStatement() {
                    acquisition = acquisition,
                    body = body,
                });

                return true;
            }

            return false;

        }

        private bool ParseResourceAcquisition(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // : local_variable_declaration
            // | expression

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseLocalVariableDeclarationRule(tokenStream, false));
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private bool ParseLocalVariableDeclaration(ref TokenStream tokenStream, bool terminated, out ExpressionIndex<VariableDeclaration> variableDeclaration) {
            // : (REF | REF READONLY)? local_variable_type local_variable_declarator ( ','  local_variable_declarator { this.IsLocalVariableDeclaration() }? )*
            // not supporting right now:
            // | FIXED pointer_type fixed_pointer_declarators

            // removing using and putting that in it's own node type 

            // also not supporting assignment chains atm, really just:
            // REF? local_variable_type identifier ('=' REF? local_variable_initializer )?

            variableDeclaration = default;
            ExpressionParseState state = SaveState(tokenStream);

            VariableModifiers variableModifiers = VariableModifiers.None;
            ExpressionIndex<TypePath> variableType = default;
            VariableDeclarationType variableDeclarationType;

            if (tokenStream.Consume(TemplateKeyword.Ref)) {
                variableModifiers = VariableModifiers.Ref;
            }

            if (tokenStream.Consume(TemplateKeyword.Var)) {
                variableDeclarationType = VariableDeclarationType.ImplicitVar;
            }
            else if (ParseTypePath(ref tokenStream, out variableType)) {
                variableDeclarationType = VariableDeclarationType.TypedVar;
            }
            else {
                RestoreState(ref tokenStream, state);
                return false;
            }

            // identifier ('=' REF? local_variable_initializer )?
            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                if (variableDeclarationType == VariableDeclarationType.ImplicitVar) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.VariableSyntax);
                }

                variableDeclaration = default;
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {

                if (terminated && !tokenStream.Consume(SymbolType.SemiColon)) {
                    if (variableDeclarationType == VariableDeclarationType.ImplicitVar) {
                        return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.VariableSyntax);
                    }

                    return false;
                }

                variableDeclaration = expressionBuffer.Add(state.location, tokenStream.location, new VariableDeclaration() {
                    initializer = default,
                    modifiers = variableModifiers,
                    identifierLocation = identifierLocation,
                    declarationType = variableDeclarationType,
                    typePath = variableType
                });
                return true;
            }

            if (!ParseLocalVariableInitializer(ref tokenStream, out ExpressionIndex assignmentExpression)) {
                if (variableDeclarationType == VariableDeclarationType.ImplicitVar) {
                    return HardError(tokenStream, DiagnosticError.ExpectedVariableInitializer, HelpType.VariableSyntax);
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (terminated && !tokenStream.Consume(SymbolType.SemiColon)) {
                if (variableDeclarationType == VariableDeclarationType.ImplicitVar) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.VariableSyntax);
                }
            }

            variableDeclaration = expressionBuffer.Add(state.location, tokenStream.location, new VariableDeclaration() {
                initializer = assignmentExpression,
                modifiers = variableModifiers,
                identifierLocation = identifierLocation,
                declarationType = variableDeclarationType,
                typePath = variableType
            });

            return true;
        }

        private bool ParseVariableInitializer(ref TokenStream tokenStream, out ExpressionIndex initializer) {
            // : expression
            // | array_initializer

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseCollectionInitializerRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out initializer);

        }

        private bool ParseLocalVariableInitializer(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // : expression
            // | array_initializer
            // | stackalloc_initializer

            // same as ParseVariableInitializer except it supports stackalloc 
            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseCollectionInitializerRule(tokenStream));
            expressionBuffer.MakeRule(ParseStackAllocInitializerRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private bool ParseArrayInitializer(ref TokenStream tokenStream, out ExpressionIndex<ArrayInitializer> arrayInitializer) {
            // '{' (variable_initializer (','  variable_initializer)* ','?)? '}'

            arrayInitializer = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                return default;
            }

            using ScopedList<ExpressionIndex> initializerList = scopedAllocator.CreateListScope<ExpressionIndex>(16);

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.ArrayInitSyntax, out TokenStream stream)) {
                return false;
            }

            PushRecoveryPoint(stream.start, stream.end + 1);

            if (ParseVariableInitializer(ref stream, out ExpressionIndex initializer)) {

                initializerList.Add(initializer);

                while (stream.Consume(SymbolType.Comma)) {

                    if (!ParseVariableInitializer(ref stream, out ExpressionIndex next)) {
                        // trailing comm allowed so just break here 
                        break;
                    }

                    initializerList.Add(next);

                }

            }

            if (PopRecoveryPoint(ref stream)) {
                return false;
            }

            arrayInitializer = expressionBuffer.Add(startLocation, tokenStream.location, new ArrayInitializer() {
                initializers = expressionBuffer.AddExpressionList(initializerList)
            });

            return true;

        }

        private RuleResult<ArrayInitializer> ParseArrayInitializerRule(TokenStream tokenStream) {
            // OPEN_BRACE (variable_initializer (','  variable_initializer)* ','?)? CLOSE_BRACE
            // handles cases like { 1, 2, 3 }, but not new int[] { 1, 2, 3 } (that comes from PrimaryExpression 

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(SymbolType.CurlyBraceOpen)) {
                return default;
            }

            // todo -- get required substream etc? 

            using ScopedList<ExpressionIndex> initializerList = scopedAllocator.CreateListScope<ExpressionIndex>(4);

            if (ParseVariableInitializer(ref tokenStream, out ExpressionIndex initializer)) {

                initializerList.Add(initializer);

                while (tokenStream.Consume(SymbolType.Comma)) {

                    if (!ParseVariableInitializer(ref tokenStream, out ExpressionIndex next)) {
                        // trailing comm allowed so just break here 
                        break;
                    }

                    initializerList.Add(next);

                }

            }

            if (!tokenStream.Consume(SymbolType.CurlyBraceClose)) {
                return default;
            }

            return expressionBuffer.AddRuleResult(startLocation, tokenStream.location, new ArrayInitializer() {
                initializers = expressionBuffer.AddExpressionList(initializerList)
            });

        }

        private bool ParseLockStatement(ref TokenStream tokenStream, out ExpressionIndex<LockStatement> lockStatement) {
            // LOCK OPEN_PARENS expression CLOSE_PARENS embedded_statement 

            int startLocation = tokenStream.location;
            lockStatement = default;

            if (!tokenStream.Consume(TemplateKeyword.Lock)) {
                return false;
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.LockStatementSyntax, out ExpressionIndex expression)) {
                return false;
            }

            if (!ParseEmbeddedStatementWithHardErrors(ref tokenStream, HelpType.LockStatementSyntax, out ExpressionIndex bodyExpression)) {
                return default;
            }

            expressionBuffer.Add(startLocation, tokenStream.location, new LockStatement() {
                lockExpression = expression,
                bodyExpression = bodyExpression
            });

            return true;

        }

        private bool ParseTryCatchStatement(ref TokenStream tokenStream, out ExpressionIndex<TryCatchFinally> tryCatchFinally) {
            // TRY block (catch_clauses finally_clause? | finally_clause)

            int startLocation = tokenStream.location;

            tryCatchFinally = default;

            if (!tokenStream.Consume(TemplateKeyword.Try)) {
                return false;
            }

            if (!ParseRequiredBlock(ref tokenStream, HelpType.TryCatchFinallySyntax, out ExpressionIndex<BlockExpression> blockExpression)) {
                return false;
            }

            ParseCatchClauses(ref tokenStream, out ExpressionRange<Catch> catchClauses);
            ParseFinallyClause(ref tokenStream, out ExpressionIndex<BlockExpression> finallyClause);

            // hard error, finally is missing & no catch clauses 
            if (catchClauses.length == 0 && !finallyClause.IsValid) {
                return HardError(tokenStream, DiagnosticError.ExpectedCatchAndOrFinallyBlock, HelpType.TryCatchFinallySyntax);
            }

            tryCatchFinally = expressionBuffer.Add(startLocation, tokenStream.location, new TryCatchFinally() {
                tryBody = blockExpression,
                catchClauses = catchClauses,
                finallyClause = finallyClause
            });

            return true;
        }

        private bool ParseFinallyClause(ref TokenStream tokenStream, out ExpressionIndex<BlockExpression> finallyClause) {
            // FINALLY block
            finallyClause = default;
            return tokenStream.Consume(TemplateKeyword.Finally) && ParseRequiredBlock(ref tokenStream, HelpType.FinallySyntax, out finallyClause);
        }

        private bool ParseCatchClauses(ref TokenStream tokenStream, out ExpressionRange<Catch> catchClauses) {
            // : specific_catch_clause (specific_catch_clause)* general_catch_clause?
            // | general_catch_clause

            if (!tokenStream.Peek(TemplateKeyword.Catch)) {
                catchClauses = default;
                return false;
            }

            using ScopedList<ExpressionIndex<Catch>> catchList = scopedAllocator.CreateListScope<ExpressionIndex<Catch>>(8);

            while (ParseSpecificCatchClause(ref tokenStream, out ExpressionIndex<Catch> next)) {
                catchList.Add(next);
            }

            if (ParseGeneralCatchClause(ref tokenStream, out ExpressionIndex<Catch> catchClause)) {
                catchList.Add(catchClause);
            }

            if (catchList.size > 0) {
                catchClauses = expressionBuffer.AddExpressionList(catchList);
                return true;
            }

            catchClauses = default;
            return false;
        }

        private bool ParseGeneralCatchClause(ref TokenStream tokenStream, out ExpressionIndex<Catch> catchClause) {
            // CATCH exception_filter? block
            catchClause = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Catch)) {
                return false;
            }

            // optional
            ParseExceptionFilter(ref tokenStream, out ExpressionIndex filter);

            if (!ParseRequiredBlock(ref tokenStream, HelpType.CatchSyntax, out ExpressionIndex<BlockExpression> blockExpression)) {
                return false;
            }

            catchClause = expressionBuffer.Add(startLocation, tokenStream.location, new Catch() {
                exceptionFilter = filter,
                body = blockExpression,
                typePath = default,
                identifier = default
            });

            return true;

        }

        private bool ParseSpecificCatchClause(ref TokenStream tokenStream, out ExpressionIndex<Catch> catchExpr) {
            // : CATCH OPEN_PARENS typePath identifier? CLOSE_PARENS exception_filter? block
            // | 'catch' exception_filter block

            ExpressionParseState state = SaveState(tokenStream);
            catchExpr = default;

            if (!tokenStream.Consume(TemplateKeyword.Catch)) {
                return default;
            }

            if (!tokenStream.Consume(SymbolType.OpenParen)) {

                if (!ParseExceptionFilter(ref tokenStream, out ExpressionIndex generalFilter)) {
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                if (!ParseRequiredBlock(ref tokenStream, HelpType.CatchSyntax, out ExpressionIndex<BlockExpression> generalBlock)) {
                    RestoreState(ref tokenStream, state);
                    return false;
                }

                catchExpr = expressionBuffer.Add(state.location, tokenStream.location, new Catch() {
                    body = generalBlock,
                    exceptionFilter = generalFilter,
                    identifier = default,
                    typePath = default
                });

                return true;

            }

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            // optional
            tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation);

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                // hard error
                RestoreState(ref tokenStream, state);
                return false;
            }

            // optional
            ParseExceptionFilter(ref tokenStream, out ExpressionIndex filter);

            if (!ParseRequiredBlock(ref tokenStream, HelpType.CatchSyntax, out ExpressionIndex<BlockExpression> blockExpression)) {
                return false;
            }

            catchExpr = expressionBuffer.Add(state.location, tokenStream.location, new Catch() {
                body = blockExpression,
                exceptionFilter = filter,
                identifier = identifierLocation,
                typePath = typePath
            });

            return true;
        }

        private bool ParseExceptionFilter(ref TokenStream tokenStream, out ExpressionIndex filter) {
            // WHEN OPEN_PARENS expression CLOSE_PARENS

            filter = default;

            if (!tokenStream.Consume(TemplateKeyword.When)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.OpenParen)) {
                // hard error I think
                return false;
            }

            if (!ParseExpression(ref tokenStream, out filter)) {
                // hard error I think
                return false;
            }

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                filter = default;
                // hard error I think
                return false;
            }

            return true;
        }

        private bool ParseThrowStatement(ref TokenStream tokenStream, out ExpressionIndex<ThrowStatement> throwExpression) {
            // THROW expression? ';'   
            int startLocation = tokenStream.location;
            throwExpression = default;

            if (!tokenStream.Consume(TemplateKeyword.Throw)) {
                return false;
            }

            if (!ParseOptionalTerminatedExpression(ref tokenStream, out ExpressionIndex expression)) {
                return false;
            }

            throwExpression = expressionBuffer.Add(startLocation, tokenStream.location, new ThrowStatement() {
                expression = expression
            });

            return true;
        }

        private bool ParseOptionalTerminatedExpression(ref TokenStream tokenStream, out ExpressionIndex expression) {
            // : expression? ';'
            if (tokenStream.Consume(SymbolType.SemiColon)) {
                expression = default;
                return true;
            }

            if (!tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream stream)) {
                expression = default;
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
            }

            PushRecoveryPoint(stream.start, stream.end + 1);
            ParseExpression(ref stream, out expression);
            if (PopRecoveryPoint(ref stream)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
            }

            return true;
        }

        private bool ParseReturnStatement(ref TokenStream tokenStream, out ExpressionIndex<ReturnStatement> returnStatement) {
            // RETURN expression? ';'  
            int startLocation = tokenStream.location;
            returnStatement = default;

            if (!tokenStream.Consume(TemplateKeyword.Return)) {
                return false;
            }

            if (!ParseOptionalTerminatedExpression(ref tokenStream, out ExpressionIndex expression)) {
                return false;
            }

            returnStatement = expressionBuffer.Add(startLocation, tokenStream.location, new ReturnStatement() {
                expression = expression
            });

            return true;

        }

        private RuleResult<GoToStatement> ParseGoToStatementRule(TokenStream tokenStream) {
            // GOTO (identifier | CASE expression | DEFAULT) ';'
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Goto)) {
                return default;
            }

            ExpressionIndex caseExpression = default;

            if (ParseIdentifier(ref tokenStream, out ExpressionIndex<Identifier> target)) { }
            else if (tokenStream.Consume(TemplateKeyword.Default)) { }
            else if (tokenStream.Consume(TemplateKeyword.Case) && ParseExpression(ref tokenStream, out caseExpression)) { }
            else {
                // hard error
                return default;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                // hard error
                return default;
            }

            return new RuleResult<GoToStatement>(tokenStream.location, expressionBuffer.Add(startLocation, tokenStream.location, new GoToStatement() {
                caseJumpTarget = caseExpression,
                labelTarget = target
            }));

        }

        private bool ParseContinueStatement(ref TokenStream tokenStream, out ExpressionIndex<ContinueStatement> continueStatement) {
            // CONTINUE ';' 
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Continue)) {
                continueStatement = default;
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                continueStatement = default;
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
            }

            continueStatement = expressionBuffer.Add(startLocation, tokenStream.location, new ContinueStatement());

            return true;
        }

        private bool ParseBreakStatement(ref TokenStream tokenStream, out ExpressionIndex<BreakStatement> breakStatement) {
            // BREAK ';'    
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Break)) {
                breakStatement = default;
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                breakStatement = default;
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon);
            }

            breakStatement = expressionBuffer.Add(startLocation, tokenStream.location, new BreakStatement());

            return true;
        }

        private bool ParseForeachLoopStatement(ref TokenStream tokenStream, out ExpressionIndex<ForeachLoop> foreachLoop) {
            // AWAIT? FOREACH OPEN_PARENS local_variable_type identifier IN expression CLOSE_PARENS embedded_statement

            int startLocation = tokenStream.location;
            foreachLoop = default;

            if (!tokenStream.Consume(TemplateKeyword.Foreach)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.ForeachLoopSyntax, out TokenStream foreachStream)) {
                return false;
            }

            PushRecoveryPoint(foreachStream.start, foreachStream.end + 1);

            ParseForeachHeader(ref foreachStream, out ExpressionIndex<VariableDeclaration> variable, out ExpressionIndex enumerableExpr);

            if (PopRecoveryPoint(ref foreachStream) == RecoveryPoint.k_HasErrors) {
                return false;
            }

            if (!ParseEmbeddedStatementWithHardErrors(ref tokenStream, HelpType.ForeachLoopSyntax, out ExpressionIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.ForeachLoopSyntax);
            }

            expressionBuffer.Add(startLocation, tokenStream.location, new ForeachLoop() {
                variableDeclaration = variable,
                enumerableExpression = enumerableExpr,
                body = body
            });

            return true;

        }

        private bool ParseForeachHeader(ref TokenStream tokenStream, out ExpressionIndex<VariableDeclaration> variable, out ExpressionIndex enumerableExpr) {

            enumerableExpr = default;
            variable = default;
            int startLocation = tokenStream.location;
            ExpressionIndex<TypePath> typePath = default;
            if (!tokenStream.Consume(TemplateKeyword.Var) && !ParseTypePath(ref tokenStream, out typePath)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedVariableInitializer, HelpType.ForeachLoopSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedIdentifier, HelpType.ForeachLoopSyntax);
            }

            int variableEnd = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.In)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedInKeyword, HelpType.ForeachLoopSyntax);
            }

            if (!ParseExpression(ref tokenStream, out enumerableExpr)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedExpression, HelpType.ForeachLoopSyntax);
            }

            variable = expressionBuffer.Add(startLocation, variableEnd, new VariableDeclaration() {
                modifiers = default,
                initializer = default,
                declarationType = typePath.IsValid ? VariableDeclarationType.TypedVar : VariableDeclarationType.ImplicitVar,
                identifierLocation = identifier,
                typePath = typePath
            });

            return true;

        }

        private bool ParseForLoop(ref TokenStream tokenStream, out ExpressionIndex<ForLoop> forLoop) {
            // FOR OPEN_PARENS for_initializer? ';' expression? ';' for_iterator? CLOSE_PARENS embedded_statement

            int startLocation = tokenStream.location;
            forLoop = default;

            if (!tokenStream.Consume(TemplateKeyword.For)) {
                return default;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.ForLoopSyntax, out TokenStream headerStream)) {
                return false;
            }

            PushRecoveryPoint(headerStream.start, headerStream.end + 1);

            ParseForLoopHeader(ref headerStream, out ExpressionIndex initializer, out ExpressionIndex expression, out ExpressionRange iterator);

            if (PopRecoveryPoint(ref headerStream)) {
                return false;
            }

            if (!ParseEmbeddedStatementWithHardErrors(ref tokenStream, HelpType.ForLoopSyntax, out ExpressionIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.ForLoopSyntax);
            }

            forLoop = expressionBuffer.Add(startLocation, tokenStream.location, new ForLoop() {
                body = body,
                condition = expression,
                initializer = initializer,
                iterator = iterator
            });

            return true;

        }

        private bool ParseForLoopHeader(ref TokenStream tokenStream, out ExpressionIndex initializer, out ExpressionIndex expression, out ExpressionRange iterator) {

            initializer = default;
            expression = default;
            iterator = default;

            if (tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream initializerStream)) {
                PushRecoveryPoint(initializerStream.start, initializerStream.end + 1);
                ParseForInitializer(ref initializerStream, out initializer);
                if (PopRecoveryPoint(ref initializerStream)) {
                    return false;
                }
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.ForLoopSyntax);
            }

            if (tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream expressionStream)) {
                PushRecoveryPoint(expressionStream.start, expressionStream.end + 1);
                ParseExpression(ref expressionStream, out expression);
                if (PopRecoveryPoint(ref expressionStream)) {
                    return false;
                }
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, HelpType.ForLoopSyntax);
            }

            return ParseExpressionListWithHardErrors(ref tokenStream, true, HelpType.ForLoopSyntax, out iterator);
        }

        private bool ParseForInitializer(ref TokenStream tokenStream, out ExpressionIndex forInitializer) {
            // this should probably return an expression range actually 

            // for_initializer
            //     : local_variable_declaration
            //     | statement_expression (','  statement_expression)*

            if (ParseLocalVariableDeclaration(ref tokenStream, false, out ExpressionIndex<VariableDeclaration> declaration)) {
                forInitializer = declaration;
                return true;
            }

            // statement_expression
            //     : null_conditional_invocation_expression
            //     | invocation_expression
            //     | object_creation_expression
            //     | assignment
            //     | post_increment_expression
            //     | post_decrement_expression
            //     | pre_increment_expression
            //     | pre_decrement_expression
            //     | await_expression

            // todo -- implement non variable initializer

            forInitializer = default;
            return false;
        }

        private bool ParseDoWhileStatement(ref TokenStream tokenStream, out ExpressionIndex<WhileLoop> doWhileLoop) {
            // DO embedded_statement WHILE OPEN_PARENS expression CLOSE_PARENS ';' 

            int startLocation = tokenStream.location;
            doWhileLoop = default;

            if (!tokenStream.Consume(TemplateKeyword.Do)) {
                return default;
            }

            if (!ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.DoWhileSyntax);
            }

            if (!tokenStream.Consume(TemplateKeyword.While)) {
                return HardError(tokenStream, DiagnosticError.ExpectedWhileKeywordAfterDoStatement, HelpType.DoWhileSyntax);
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.DoWhileSyntax, out ExpressionIndex condition)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream.location - 1, ErrorLocationAdjustment.TokenEnd, DiagnosticError.ExpectedSemiColon, HelpType.DoWhileSyntax);
            }

            doWhileLoop = expressionBuffer.Add(startLocation, tokenStream.location, new WhileLoop() {
                isDoWhile = true,
                body = body,
                condition = condition
            });

            return true;

        }

        private bool ParseWhileStatement(ref TokenStream tokenStream, out ExpressionIndex<WhileLoop> whileLoop) {
            // WHILE OPEN_PARENS expression CLOSE_PARENS embedded_statement
            int startLocation = tokenStream.location;
            whileLoop = default;

            if (!tokenStream.Consume(TemplateKeyword.While)) {
                return default;
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.WhileLoopSyntax, out ExpressionIndex condition)) {
                return false;
            }

            if (!ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.WhileLoopSyntax);
            }

            whileLoop = expressionBuffer.Add(startLocation, tokenStream.location, new WhileLoop() {
                condition = condition,
                body = body
            });

            return true;
        }

        private bool ParseSwitchStatement(ref TokenStream tokenStream, out ExpressionIndex<SwitchStatement> switchStatement) {
            // SWITCH OPEN_PARENS expression CLOSE_PARENS OPEN_BRACE switch_section* CLOSE_BRACE

            int startLocation = tokenStream.location;

            switchStatement = default;

            if (!tokenStream.Consume(TemplateKeyword.Switch)) {
                return false;
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.SwitchStatementSyntax, out ExpressionIndex condition)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.SwitchStatementSyntax, out TokenStream bodyStream)) {
                return false;
            }

            using ScopedList<ExpressionIndex<SwitchSection>> sections = scopedAllocator.CreateListScope<ExpressionIndex<SwitchSection>>(16);

            PushRecoveryPoint(bodyStream.start, bodyStream.end + 1);

            bool hasCaseGuards = false;

            ExpressionRange defaultRange = default;

            while (ParseSwitchSection(ref bodyStream, out bool hasGuards, out ExpressionIndex<SwitchSection> section, out ExpressionRange defaultBody)) {

                if (section.IsValid) {
                    sections.Add(section);
                    if (hasGuards) {
                        hasCaseGuards = true;
                    }

                }

                if (defaultBody.length != 0) {
                    if (defaultRange.length != 0) {
                        HardError(bodyStream.location, DiagnosticError.DuplicateDefaultLabel, HelpType.SwitchStatementSyntax);
                        break;
                    }

                    defaultRange = defaultBody;
                }
            }

            if (PopRecoveryPoint(ref bodyStream)) {
                return false;
            }

            expressionBuffer.Add(startLocation, tokenStream.location, new SwitchStatement() {
                condition = condition,
                defaultBody = defaultRange,
                hasCaseGuards = hasCaseGuards,
                sections = expressionBuffer.AddExpressionList(sections),
            });
            return true;

        }

        private bool ParseSwitchSection(ref TokenStream tokenStream, out bool hasCaseGuards, out ExpressionIndex<SwitchSection> section, out ExpressionRange defaultSection) {
            // switch_label+ statement_list

            section = default;
            int startLocation = tokenStream.location;
            hasCaseGuards = false;
            defaultSection = default;

            if (!tokenStream.Peek(TemplateKeyword.Default) && !tokenStream.Peek(TemplateKeyword.Case)) {
                return false;
            }

            using ScopedList<ExpressionIndex<SwitchLabel>> labelList = scopedAllocator.CreateListScope<ExpressionIndex<SwitchLabel>>(4);

            ExpressionIndex<SwitchLabel> defaultLabel = default;

            while (ParseSwitchLabel(ref tokenStream, out bool hasGuard, out bool isDefault, out ExpressionIndex<SwitchLabel> label)) {
                if (hasGuard) hasCaseGuards = true;
                if (isDefault) {
                    defaultLabel = label;
                }
                else {
                    labelList.Add(label);
                }
            }

            if (!defaultLabel.IsValid && labelList.size == 0) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedCaseLabelOrDefault, HelpType.SwitchCaseSyntax);
            }

            if (!ParseStatementListWithHardErrors(ref tokenStream, HelpType.SwitchSectionSyntax, out ExpressionRange bodyStatements)) {
                return false;
            }

            if (labelList.size > 0) {
                section = expressionBuffer.Add(startLocation, tokenStream.location, new SwitchSection() {
                    bodyStatements = bodyStatements,
                    labels = expressionBuffer.AddExpressionList(labelList)
                });
            }

            if (defaultLabel.IsValid) {
                defaultSection = bodyStatements;
            }

            return true;

        }

        private bool ParseStatementListWithHardErrors(ref TokenStream tokenStream, HelpType helpData, out ExpressionRange list) {
            // statement+
            list = default;

            if (!ParseStatement(ref tokenStream, out ExpressionIndex statement)) {
                return HardError(tokenStream, DiagnosticError.ExpectedStatement, helpData);
            }

            using ScopedList<ExpressionIndex> buffer = scopedAllocator.CreateListScope<ExpressionIndex>(32);
            buffer.Add(statement);

            while (ParseStatement(ref tokenStream, out ExpressionIndex next)) {
                buffer.Add(next);
            }

            list = expressionBuffer.AddExpressionList(buffer);

            return true;
        }

        private bool ParseSwitchLabel(ref TokenStream tokenStream, out bool hasCaseGuard, out bool isDefault, out ExpressionIndex<SwitchLabel> switchLabel) {
            // : CASE expression case_guard? ':'
            // | DEFAULT ':'

            isDefault = false;
            switchLabel = default;
            hasCaseGuard = false;
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(TemplateKeyword.Case)) {

                if (!ParseExpression(ref tokenStream, out ExpressionIndex caseExpression)) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedExpression, HelpType.SwitchCaseSyntax);
                }

                hasCaseGuard = CaseGuard(ref tokenStream, out ExpressionIndex guardExpression);

                if (!tokenStream.Consume(SymbolType.Colon)) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedColon, HelpType.SwitchCaseSyntax);
                }

                switchLabel = expressionBuffer.Add(state.location, tokenStream.location, new SwitchLabel() {
                    caseExpression = caseExpression,
                    guardExpression = guardExpression
                });

                return true;
            }

            if (tokenStream.Consume(TemplateKeyword.Default)) {

                if (!tokenStream.Consume(SymbolType.Colon)) {
                    // hard error
                    return false;
                }

                switchLabel = expressionBuffer.Add(state.location, tokenStream.location, new SwitchLabel() {
                    caseExpression = default
                });

                isDefault = true;
                return true;

            }

            return false;
        }

        private bool ParseIfStatement(ref TokenStream tokenStream, out ExpressionIndex<IfStatement> ifExpression) {
            // IF OPEN_PARENS expression CLOSE_PARENS if_body (ELSE if_body)? 

            int startLocation = tokenStream.location;
            ifExpression = default;

            if (!tokenStream.Consume(TemplateKeyword.If)) {
                return false;
            }

            if (ParseScopeModifier(ref tokenStream, out ScopeModifier modifier) && modifier != ScopeModifier.None) {
                return HardError(tokenStream.location - 1, DiagnosticError.IfStatementsNotInDirectlyInTemplateBlockCannotHaveScopeModifiers, HelpType.IfExpressionSyntax);
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.IfExpressionSyntax, out ExpressionIndex condition)) {
                return false;
            }

            if (!ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.IfExpressionSyntax);
            }

            ExpressionIndex elseBody = default;
            if (tokenStream.Consume(TemplateKeyword.Else) && !ParseEmbeddedStatement(ref tokenStream, out elseBody)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.IfExpressionSyntax);
            }

            ifExpression = expressionBuffer.Add(startLocation, tokenStream.location, new IfStatement() {
                condition = condition,
                body = body,
                elseBody = elseBody
            });

            return true;

        }

        private bool ParseEmbeddedStatementWithHardErrors(ref TokenStream tokenStream, HelpType helpData, out ExpressionIndex expression) {

            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                bool result = ParseRequiredBlock(ref tokenStream, helpData, out ExpressionIndex<BlockExpression> blockExpression);
                expression = blockExpression;
                return result;
            }

            return ParseSimpleEmbeddedStatement(ref tokenStream, out expression);
        }

        private bool ParseEmbeddedStatement(ref TokenStream tokenStream, out ExpressionIndex expression) {

            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                bool result = ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression);
                expression = blockExpression;
                return result;
            }

            return ParseSimpleEmbeddedStatement(ref tokenStream, out expression);
        }

        private RuleResult ParseDeclarationStatementRule(TokenStream tokenStream) {
            // : local_variable_declaration ';'
            // | local_constant_declaration ';'
            // | local_function_declaration
            // | using_statement

            // maybe this is a declaration node type?

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseLocalVariableDeclarationRule(tokenStream, true));
            expressionBuffer.MakeRule(ParseTerminatedLocalConstantRule(tokenStream));
            expressionBuffer.MakeRule(ParseLocalFunctionDeclarationRule(tokenStream));
            expressionBuffer.MakeRule(ParseUsingDeclaration(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex expressionIndex)
                ? new RuleResult(tokenStream.location, expressionIndex)
                : default;
        }

        private bool ParseLocalFunctionBody(ref TokenStream tokenStream, out ExpressionIndex<BlockExpression> body) {
            // : block
            // | right_arrow throwable_expression ';'

            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.FatArrow)) {
                if (ParseThrowableExpression(ref tokenStream, out ExpressionIndex throwable)) {
                    body = expressionBuffer.Add(state.location, tokenStream.location, new BlockExpression() {
                        statementList = expressionBuffer.AddExpressionList(throwable)
                    });
                    return true;

                }

                RestoreState(ref tokenStream, state);
                // hard error
                body = default;
                return false;
            }

            return ParseBlock(ref tokenStream, out body);

        }

        private bool ParseLocalFunctionDeclaration(ref TokenStream tokenStream, out ExpressionIndex<LocalFunctionDefinition> localFunction) {
            // : local_function_header local_function_body
            // where
            // local_function_header
            //    : local_function_modifiers? return_type identifier type_parameter_list? OPEN_PARENS formal_parameter_list? CLOSE_PARENS type_parameter_constraints_clauses?
            // local_function_modifiers
            //    : (ASYNC | UNSAFE) STATIC?
            //    | STATIC (ASYNC | UNSAFE)
            // 

            ExpressionParseState state = SaveState(tokenStream);

            localFunction = default;
            FunctionTypeModifiers modifiers = ParseLocalFunctionModifiers(ref tokenStream);
            ExpressionIndex<TypePath> typePath = default;

            if (!tokenStream.Consume(TemplateKeyword.Void) && !ParseTypePath(ref tokenStream, out typePath)) {
                if (modifiers != FunctionTypeModifiers.None) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.LocalFunctionSyntax);
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation nameLocation)) {
                if (modifiers != FunctionTypeModifiers.None) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.LocalFunctionSyntax);
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            // note: i'm not actually storing the variance
            ParseVariantTypeParameterList(ref tokenStream, out ExpressionRange<Identifier> typeParameters);

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                if (typeParameters.length != 0) {
                    // if we had type params then its definitely a hard error 
                    return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, HelpType.LocalFunctionSyntax);
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            // hard errors from here on since the grammar cannot match anything else after
            // return_type identifier <>? (

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream parameterStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.LocalFunctionSyntax);
            }

            if (!ParseFormalLambdaParameters(ref parameterStream, out ExpressionRange<Parameter> parameterList)) {
                return HardError(tokenStream, DiagnosticError.ExpectedFormalParameterList, HelpType.LocalFunctionSyntax);
            }

            ParseTypeParameterConstraintClauses(ref tokenStream);

            if (!ParseLocalFunctionBody(ref tokenStream, out ExpressionIndex<BlockExpression> block)) {
                return HardError(tokenStream, DiagnosticError.ExpectedLocalFunctionBody, HelpType.LocalFunctionSyntax);
            }

            localFunction = expressionBuffer.Add(state.location, tokenStream.location, new LocalFunctionDefinition() {
                nameLocation = nameLocation,
                modifiers = modifiers,
                typeParameters = typeParameters,
                parameters = parameterList,
                body = block,
                returnType = typePath
            });

            return true;
        }

        private bool ParseTypeParameterConstraintClauses(ref TokenStream tokenStream) {
            return false;
        }

        private bool ParseVariantTypeParameterList(ref TokenStream tokenStream, out ExpressionRange<Identifier> expressionRange) {
            // '<' variant_type_parameter (',' variant_type_parameter)* '>'
            // where variant_type_parameter
            //       : attributes? variance_annotation? identifier
            // and
            // variance_annotation
            //       : IN | OUT

            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.Consume(SymbolType.LessThan)) {
                expressionRange = default;
                return false;
            }

            // todo -- do something with variance 
            if (tokenStream.Consume(TemplateKeyword.In)) { }
            else if (tokenStream.Consume(TemplateKeyword.Out)) { }

            // I'm just parsing identifiers
            if (!ParseIdentifier(ref tokenStream, out ExpressionIndex<Identifier> first)) {
                RestoreState(ref tokenStream, state);
                expressionRange = default;
                return false;
            }

            using AllocatorScope scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex<Identifier>> list = scope.CreateList<ExpressionIndex<Identifier>>(8);
            list.Add(first);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseIdentifier(ref tokenStream, out ExpressionIndex<Identifier> next)) {
                    RestoreState(ref tokenStream, state);
                    expressionRange = default;
                    return false;
                }

                list.Add(next);

            }

            if (!tokenStream.Consume(SymbolType.GreaterThan)) {
                // hard error
                RestoreState(ref tokenStream, state);
                expressionRange = default;
                return false;
            }

            expressionRange = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private FunctionTypeModifiers ParseLocalFunctionModifiers(ref TokenStream tokenStream) {
            FunctionTypeModifiers modifiers = default;
            if (tokenStream.Peek(TemplateKeyword.Async) || tokenStream.Peek(TemplateKeyword.Unsafe)) {
                if (tokenStream.Consume(TemplateKeyword.Async)) {
                    modifiers |= FunctionTypeModifiers.Async;
                }
                else if (tokenStream.Consume(TemplateKeyword.Unsafe)) {
                    modifiers |= FunctionTypeModifiers.Async;
                }

                if (tokenStream.Consume(TemplateKeyword.Static)) {
                    modifiers |= FunctionTypeModifiers.Static;
                }

                return modifiers;
            }

            if (tokenStream.Consume(TemplateKeyword.Static)) {
                modifiers |= FunctionTypeModifiers.Static;
                if (tokenStream.Consume(TemplateKeyword.Async)) {
                    modifiers |= FunctionTypeModifiers.Async;
                }
                else if (tokenStream.Consume(TemplateKeyword.Unsafe)) {
                    modifiers |= FunctionTypeModifiers.Async;
                }

                return modifiers;
            }

            return modifiers;
        }

        private bool ParseLocalConstant(ref TokenStream tokenStream, out ExpressionIndex<VariableDeclaration> constDeclaration) {
            // CONST type_ constant_declarators ';'

            constDeclaration = default;

            if (tokenStream.Current.Keyword != TemplateKeyword.Const) {
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.location++;
            // this is a hard error if false

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.ConstantSyntax);
            }

            // constant_declarator (','  constant_declarator)*
            // I'm not parsing multiple assignments via comma.
            if (!ParseConstantDeclarator(ref tokenStream, typePath, out ExpressionIndex<VariableDeclaration> declaration)) {
                return HardError(tokenStream, DiagnosticError.ExpectedConstantDeclarator, HelpType.ConstantSyntax);
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.ConstantSyntax);
            }

            constDeclaration = declaration;
            return true;

        }

        private bool ParseConstantDeclarator(ref TokenStream tokenStream, ExpressionIndex<TypePath> typePath, out ExpressionIndex<VariableDeclaration> constDeclaration) {
            //  identifier '=' expression

            constDeclaration = default;
            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                // hard error, constants must be assigned and no other paths could be valid at this point 
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex assignmentExpression)) {
                // hard error, constants must be assigned and no other paths could be valid at this point
                RestoreState(ref tokenStream, state);
                return false;
            }

            constDeclaration = expressionBuffer.Add(state.location, tokenStream.location, new VariableDeclaration() {
                identifierLocation = identifier,
                initializer = assignmentExpression,
                declarationType = VariableDeclarationType.Const,
                typePath = typePath,
                modifiers = VariableModifiers.None
            });

            return true;
        }

        private bool ParseLambdaSignature(ref TokenStream tokenStream, out bool hasInformalParameters, out bool hasFormalParameters, out ExpressionRange<Parameter> parameterList) {
            // : OPEN_PARENS CLOSE_PARENS
            // | OPEN_PARENS explicit_anonymous_function_parameter_list CLOSE_PARENS
            // | OPEN_PARENS implicit_anonymous_function_parameter_list CLOSE_PARENS
            // | identifier
            hasInformalParameters = false;
            hasFormalParameters = false;
            if (tokenStream.Peek(SymbolType.OpenParen)) {
                tokenStream.location++;
                if (tokenStream.Peek(SymbolType.CloseParen)) {
                    tokenStream.location++;
                    parameterList = default;
                    return true;
                }

                tokenStream.location--; // unpeek
            }
            else if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation tokenLocation)) {
                ExpressionIndex<Parameter> parameter = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new Parameter() {
                    modifier = default,
                    defaultExpression = default,
                    isExplicit = false,
                    nameLocation = tokenLocation,
                    typePath = default
                });
                parameterList = expressionBuffer.AddExpressionList(parameter);
                return true;
            }

            int startLocation = tokenStream.location;
            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream parenStream)) {
                // possibly a hard error here 
                parameterList = default;
                return false;
            }

            // maybe a recovery point here?
            if (ParseAnyLambdaParameters(ref parenStream, out hasInformalParameters, out hasFormalParameters, out parameterList)) {
                return true;
            }

            // if (ParseAnonymousParameterList(ref parenStream, out parameterList)) {
            //     return true;
            // }
            // if (ParseFormalLambdaParameters(ref parenStream, out parameterList)) {
            //     hasFormalParameters = true;
            //     return true;
            // }

            // state restoration already handled
            tokenStream.location = startLocation;
            return false;
        }

        private bool ParseAnyLambdaParameters(ref TokenStream tokenStream, out bool hasInformalParameters, out bool hasFormalParameters, out ExpressionRange<Parameter> parameters) {
            ExpressionParseState state = SaveState(tokenStream);

            using ScopedList<ExpressionIndex<Parameter>> list = scopedAllocator.CreateListScope<ExpressionIndex<Parameter>>(8);
            hasInformalParameters = false;
            hasFormalParameters = false;
            while (ParseAnyLambdaParameter(ref tokenStream, out bool isFormal, out ExpressionIndex<Parameter> parameter)) {

                if (!isFormal) hasInformalParameters = true;
                if (isFormal) hasFormalParameters = true;

                list.Add(parameter);

                if (tokenStream.HasMoreTokens && !tokenStream.Consume(SymbolType.Comma)) {
                    RestoreState(ref tokenStream, state);
                    parameters = default;
                    return false;
                }

            }

            if (!tokenStream.IsEmpty) {
                RestoreState(ref tokenStream, state);
                parameters = default;
                return false;
            }

            parameters = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private bool ParseAnonymousParameterList(ref TokenStream tokenStream, out ExpressionRange<Parameter> parameters) {
            // : identifier (',' identifier)*
            ExpressionParseState state = SaveState(tokenStream);

            using ScopedList<ExpressionIndex<Parameter>> list = scopedAllocator.CreateListScope<ExpressionIndex<Parameter>>(8);

            while (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation location)) {

                ExpressionIndex<Parameter> parameter = expressionBuffer.Add(location.index, location.index + 1, new Parameter() {
                    modifier = default,
                    defaultExpression = default,
                    isExplicit = false,
                    nameLocation = location,
                    typePath = default
                });

                list.Add(parameter);

                if (tokenStream.HasMoreTokens && !tokenStream.Consume(SymbolType.Comma)) {
                    RestoreState(ref tokenStream, state);
                    parameters = default;
                    return false;
                }

            }

            if (!tokenStream.IsEmpty) {
                RestoreState(ref tokenStream, state);
                parameters = default;
                return false;
            }

            parameters = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private enum ParameterKind {

            Invalid,
            Formal,
            Anonymous

        }

        private bool ParseAnyLambdaParameter(ref TokenStream tokenStream, out bool isFormal, out ExpressionIndex<Parameter> parameter) {

            isFormal = false;
            if (ParseFormalLambdaParameter(ref tokenStream, out parameter)) {
                isFormal = true;
                return true;
            }

            if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation location)) {
                parameter = expressionBuffer.Add(location.index, location.index + 1, new Parameter() {
                    modifier = default,
                    defaultExpression = default,
                    isExplicit = false,
                    nameLocation = location,
                    typePath = default
                });
                return true;
            }

            return false;
        }

        private bool ParseFormalLambdaParameter(ref TokenStream tokenStream, out ExpressionIndex<Parameter> parameter, bool allowModifiers = true) {
            // 	: (REF | OUT | IN)? type_ identifier
            ExpressionParseState state = SaveState(tokenStream);

            ArgumentModifier modifier = ArgumentModifier.None;
            if (allowModifiers) {
                if (tokenStream.Consume(TemplateKeyword.Ref)) {
                    modifier = ArgumentModifier.Ref;
                }
                else if (tokenStream.Consume(TemplateKeyword.Out)) {
                    modifier = ArgumentModifier.Out;
                }
                else if (tokenStream.Consume(TemplateKeyword.In)) {
                    modifier = ArgumentModifier.In;
                }
            }

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                RestoreState(ref tokenStream, state);
                parameter = default;
                return false;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation nameLocation)) {
                RestoreState(ref tokenStream, state);
                parameter = default;
                return false;
            }

            parameter = expressionBuffer.Add(state.location, tokenStream.location, new Parameter() {
                isExplicit = true,
                modifier = modifier,
                nameLocation = nameLocation,
                typePath = typePath,
                defaultExpression = default
            });

            return true;

        }

        private bool ParseFormalLambdaParameters(ref TokenStream tokenStream, out ExpressionRange<Parameter> parameters) {
            // 	((REF | OUT | IN)? type_ identifier)*

            ExpressionParseState state = SaveState(tokenStream);

            using ScopedList<ExpressionIndex<Parameter>> list = scopedAllocator.CreateListScope<ExpressionIndex<Parameter>>(8);

            while (ParseFormalLambdaParameter(ref tokenStream, out ExpressionIndex<Parameter> parameter)) {

                list.Add(parameter);

                if (tokenStream.HasMoreTokens && !tokenStream.Consume(SymbolType.Comma)) {
                    RestoreState(ref tokenStream, state);
                    parameters = default;
                    return false;
                }

            }

            if (!tokenStream.IsEmpty) {
                RestoreState(ref tokenStream, state);
                parameters = default;
                return false;
            }

            parameters = expressionBuffer.AddExpressionList(list);
            return true;

        }

        private bool ParseNonAssignment(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseLambdaExpressionRule(tokenStream));
            // expressionBuffer.MakeRule(ParseQueryExpressionRule(tokenStream)); not supporting queries
            expressionBuffer.MakeRule(ParseConditionalExpressionRule(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);

        }

        private bool ParseConditionalExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {

            // null_coalescing_expression ('?' throwable_expression ':' throwable_expression)?

            ExpressionParseState state = SaveState(tokenStream);

            if (!ParseNullCoalescingExpression(ref tokenStream, out expressionIndex)) {
                return default;
            }

            if (!tokenStream.Consume(SymbolType.QuestionMark)) {
                return true;
            }

            // maybe a hard error, not sure 
            if (!ParseThrowableExpression(ref tokenStream, out ExpressionIndex trueExpression)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            // maybe a hard error, not sure 
            if (!tokenStream.Consume(SymbolType.Colon)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            // maybe a hard error, not sure 
            if (!ParseThrowableExpression(ref tokenStream, out ExpressionIndex falseExpression)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            expressionIndex = expressionBuffer.Add(state.location, tokenStream.location, new TernaryExpression() {
                condition = expressionIndex,
                trueExpression = trueExpression,
                falseExpression = falseExpression
            });

            return true;
        }

        private bool ParseDeclarationPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : type variable_designation

            ExpressionParseState state = SaveState(tokenStream);

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                pattern = default;
                return false;
            }

            if (!ParseVariableDesignation(ref tokenStream, out ExpressionIndex<VariableDesignation> designation)) {
                pattern = default;
                RestoreState(ref tokenStream, state);
                return false;
            }

            pattern = expressionBuffer.Add(state.location, tokenStream.location, new DeclarationPattern() {
                typePath = typePath,
                designation = designation
            });

            return true;
        }

        private bool ParseDiscardDesignation(ref TokenStream tokenStream, out NonTrivialTokenLocation location) {
            // '_'
            if (tokenStream.Consume(SymbolType.Underscore)) {
                location = new NonTrivialTokenLocation(tokenStream.location - 1);
                return true;
            }

            location = default;
            return false;
        }

        private bool ParseSingleVariableDesignation(ref TokenStream tokenStream, out NonTrivialTokenLocation location) {
            // identifier
            return tokenStream.ConsumeStandardIdentifier(out location);
        }

        private bool ParseVariableDesignation(ref TokenStream tokenStream, out ExpressionIndex<VariableDesignation> expressionIndex) {
            // : discard_designation
            // | parenthesized_variable_designation
            // | single_variable_designation
            int startLocation = tokenStream.location;
            if (ParseDiscardDesignation(ref tokenStream, out NonTrivialTokenLocation location)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new VariableDesignation() {
                    isDiscard = true,
                    singleDesignationLocation = location
                });
                return true;
            }

            if (ParseParenthesizedVariableDesignation(ref tokenStream, out ExpressionRange<VariableDesignation> designations)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new VariableDesignation() {
                    designationList = designations
                });
                return true;
            }

            if (ParseSingleVariableDesignation(ref tokenStream, out location)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new VariableDesignation() {
                    isDiscard = false,
                    singleDesignationLocation = location
                });
                return true;
            }

            expressionIndex = default;
            return false;

        }

        private bool ParseParenthesizedVariableDesignation(ref TokenStream tokenStream, out ExpressionRange<VariableDesignation> expressionRange) {
            // parenthesized_variable_designation : '(' (variable_designation (',' variable_designation)*)? ')'
            expressionRange = default;
            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.VariableDesignationSyntax, out TokenStream stream)) {
                return false;
            }

            if (!ParseVariableDesignation(ref tokenStream, out ExpressionIndex<VariableDesignation> designation)) {
                // likely a hard error if we got this far?
                return false;
            }

            using ScopedList<ExpressionIndex<VariableDesignation>> list = scopedAllocator.CreateListScope<ExpressionIndex<VariableDesignation>>(4);

            list.Add(designation);
            while (stream.Consume(SymbolType.Comma)) {
                if (ParseVariableDesignation(ref stream, out designation)) {
                    list.Add(designation);
                }
                else {
                    // maybe a hard error?
                    RestoreState(ref tokenStream, state);
                    return false;
                }
            }

            if (stream.HasMoreTokens) {
                // maybe a hard error?
                RestoreState(ref tokenStream, state);
                return false;
            }

            expressionRange = expressionBuffer.AddExpressionList(list);
            return true;

        }

        private bool ParseConstantPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : expression
            int start = tokenStream.location;
            if (ParseExpression(ref tokenStream, out var expressionIndex)) {
                pattern = expressionBuffer.Add(start, tokenStream.location, new ConstantPattern() {
                    expression = expressionIndex
                });
                return true;
            }

            pattern = default;
            return false;
        }

        private bool ParseBinaryPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : pattern ('or' | 'and') pattern
            ExpressionParseState state = SaveState(tokenStream);
            pattern = default;

            // todo -- I'm pretty certain this isn't handling precedence right, need to walk the tree the same way operators do
            if (!ParsePattern(ref tokenStream, false, out ExpressionIndex lhs)) {
                return false;
            }

            BinaryPatternOp op;
            if (tokenStream.Consume(TemplateKeyword.Or)) {
                op = BinaryPatternOp.Or;
            }
            else if (tokenStream.Consume(TemplateKeyword.And)) {
                op = BinaryPatternOp.And;
            }
            else {
                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!ParsePattern(ref tokenStream, true, out ExpressionIndex rhs)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            pattern = expressionBuffer.Add(state.location, tokenStream.location, new BinaryPattern() {
                op = op,
                lhs = lhs,
                rhs = rhs
            });

            return true;
        }

        private bool ParseDiscardPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : '_'
            pattern = default;

            if (tokenStream.Consume(SymbolType.Underscore)) {
                pattern = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new DiscardPattern() {
                    location = new NonTrivialTokenLocation(tokenStream.location - 1)
                });
                return true;
            }

            return false;
        }

        private bool ParseParenthesizedPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // '(' pattern ')'
            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                pattern = default;
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);
            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, default, out TokenStream stream)) {
                pattern = default;
                return false;
            }

            if (!ParsePattern(ref stream, true, out pattern) || stream.HasMoreTokens) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            return true;
        }

        private bool ParseRecursivePattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            //  : type? positional_pattern_clause? property_pattern_clause? variable_designation?
            pattern = default;
            return false;
        }

        private bool ParseRelationalPattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : '!=' expression
            // | '<' expression
            // | '<=' expression
            // | '==' expression
            // | '>' expression
            // | '>=' expression

            // these are probably hard errors if we got the relational symbol 

            pattern = default;
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.NotEquals)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.NotEqual,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.LessThan)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.LessThan,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.LessThanEqualTo)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.LessThanOrEqual,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.Equals)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.Equal,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.GreaterThan)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.GreaterThan,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (tokenStream.Consume(SymbolType.GreaterThanEqualTo)) {
                if (ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                    pattern = expressionBuffer.Add(state.location, tokenStream.location, new RelationalPattern() {
                        op = RelationalPatternOp.GreaterThanOrEqual,
                        expression = expressionIndex
                    });
                    return true;
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            pattern = default;
            return false;
        }

        private bool ParseTypePattern(ref TokenStream tokenStream, out ExpressionIndex pattern) {
            // : type
            int startLocation = tokenStream.location;
            if (ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                pattern = expressionBuffer.Add(startLocation, tokenStream.location, new TypePattern() {
                    typePath = typePath
                });
                return true;
            }

            pattern = default;
            return false;
        }

        private bool ParseUnaryPattern(ref TokenStream tokenStream, out ExpressionIndex unary) {
            // 'not' pattern

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Not)) {
                unary = default;
                return false;
            }

            if (!ParsePattern(ref tokenStream, true, out ExpressionIndex pattern)) {
                unary = default;
                return HardError(tokenStream.location, DiagnosticError.ExpectedPattern, HelpType.UnaryPatternSyntax);
            }

            unary = expressionBuffer.Add(startLocation, tokenStream.location, new UnaryNotPattern() {
                pattern = pattern
            });

            return true;
        }

        private bool ParseVarPattern(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // 'var' variable_designation

            if (!tokenStream.Peek(TemplateKeyword.Var)) {
                expressionIndex = default;
                return false;
            }

            int startLocation = tokenStream.location;
            tokenStream.location++;

            if (!ParseVariableDesignation(ref tokenStream, out ExpressionIndex<VariableDesignation> designation)) {
                tokenStream.location = startLocation;
                expressionIndex = default;
                return false;
            }

            expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new VarPattern() {
                designation = designation
            });

            return true;
        }

        private bool ParsePattern(ref TokenStream tokenStream, bool allowBinary, out ExpressionIndex pattern) {
            // : binary_pattern
            // | constant_pattern
            // | declaration_pattern
            // | discard_pattern
            // | parenthesized_pattern
            // | recursive_pattern
            // | relational_pattern
            // | type_pattern
            // | unary_pattern
            // | var_pattern

            expressionBuffer.PushRuleScope();

            if (allowBinary) {
                expressionBuffer.MakeRule(ParseBinaryPatternRule(tokenStream));
            }

            expressionBuffer.MakeRule(ParseConstantPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseDeclarationPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseDiscardPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseParenthesizedPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseRecursivePatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseRelationalPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseTypePatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseUnaryPatternRule(tokenStream));
            expressionBuffer.MakeRule(ParseVarPatternRule(tokenStream));

            return expressionBuffer.PopRuleScope(ref tokenStream, out pattern);
        }

        private bool SwitchArm(ref TokenStream tokenStream, out ExpressionIndex<SwitchArm> arm) {
            // pattern case_guard? right_arrow throwable_expression

            ExpressionParseState state = SaveState(tokenStream);
            arm = default;

            if (!ParsePattern(ref tokenStream, true, out ExpressionIndex expressionIndex)) {
                return false;
            }

            bool hasCaseGuard = CaseGuard(ref tokenStream, out ExpressionIndex guardExpression);

            if (!tokenStream.Consume(SymbolType.FatArrow)) {
                if (hasCaseGuard) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedFatArrow, HelpType.SwitchArmSyntax);
                }

                RestoreState(ref tokenStream, state);
                return false;
            }

            if (!ParseThrowableExpression(ref tokenStream, out ExpressionIndex body)) {
                // hard error now since we've seen => and a 'switch' keyword 
                return HardError(tokenStream.location, DiagnosticError.ExpectedThrowableExpression, HelpType.SwitchArmSyntax);
            }

            arm = expressionBuffer.Add(state.location, tokenStream.location, new SwitchArm() {
                guard = guardExpression,
                body = body,
                pattern = expressionIndex
            });

            return true;
        }

        private bool SwitchArms(ref TokenStream tokenStream, out ExpressionRange<SwitchArm> arms) {
            // switch_expression_arm (',' switch_expression_arm)*

            if (!SwitchArm(ref tokenStream, out ExpressionIndex<SwitchArm> firstArm)) {
                arms = default;
                return default;
            }

            using ScopedList<ExpressionIndex<SwitchArm>> armList = scopedAllocator.CreateListScope<ExpressionIndex<SwitchArm>>(16);
            armList.Add(firstArm);

            while (tokenStream.Consume(SymbolType.Comma)) {

                if (!SwitchArm(ref tokenStream, out ExpressionIndex<SwitchArm> next)) {
                    // allow an optional trailing comma
                    break;
                }

                armList.Add(next);
            }

            arms = expressionBuffer.AddExpressionList(armList);
            return true;
        }

        private bool CaseGuard(ref TokenStream tokenStream, out ExpressionIndex expression) {
            // WHEN expression
            expression = default;

            if (!tokenStream.Peek(TemplateKeyword.When)) {
                return false;
            }

            tokenStream.location++;

            if (!ParseExpression(ref tokenStream, out expression)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedExpression, HelpType.CaseGuardSyntax);
            }

            return true;
        }

        private bool SwitchExpression(ref TokenStream tokenStream, out ExpressionIndex expression) {
            // range_expression ('switch' '{' (switch_expression_arms ','?)? '}')?

            // sample: 
            // public static Orientation ToOrientation(Direction direction) => direction switch
            // {
            //     Direction.Up    => Orientation.North,
            //     Direction.Right => Orientation.East,
            //     Direction.Down  => Orientation.South,
            //     Direction.Left  => Orientation.West,
            //     _ => throw new ArgumentOutOfRangeException(nameof(direction), $"Not expected direction value: {direction}"),
            // };

            ExpressionParseState state = SaveState(tokenStream);
            expression = default;

            if (!RangeExpression(ref tokenStream, out ExpressionIndex lhs)) {
                return default;
            }

            if (!tokenStream.Consume(TemplateKeyword.Switch)) {
                expression = lhs; // not a switch expr, pass along the expression politely 
                return true;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.SwitchExpressionSyntax, out TokenStream stream)) {
                return false;
            }

            PushRecoveryPoint(stream.location, stream.end + 1);
            // optional 
            SwitchArms(ref stream, out ExpressionRange<SwitchArm> switchArms);

            // eat optional comma at the end
            stream.Consume(SymbolType.Comma);

            if (stream.HasMoreTokens) {
                HardError(stream.location, DiagnosticError.UnexpectedToken, HelpType.SwitchExpressionSyntax);
            }

            if (PopRecoveryPoint(ref stream)) {
                return false;
            }

            expression = expressionBuffer.Add(state.location, tokenStream.location, new SwitchExpression() {
                lhs = lhs,
                switchArms = switchArms,
            });
            return true;

        }

        private bool RangeExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // : unary_expression
            // | unary_expression? OP_RANGE unary_expression?
            // C# official grammar says the 2nd is  range_expression? '..' range_expression? which I guess is left recursive so we use unary_expression? instead
            // var slice1 = array[2..^3];    // array[new Range(2, new Index(3, fromEnd: true))]
            // var slice2 = array[..^3];     // array[Range.EndAt(new Index(3, fromEnd: true))]
            // var slice3 = array[2..];      // array[Range.StartAt(2)]
            // var slice4 = array[..];       // array[Range.All]
            // I think the ^ is handled via unary expression so we really only care about the .. part 
            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseUnaryExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseRangeOpExpression(tokenStream));
            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);
        }

        private RuleResult<RangeExpression> ParseRangeOpExpression(TokenStream tokenStream) {
            // unary_expression? OP_RANGE unary_expression?
            int startLocation = tokenStream.location;

            ParseUnaryExpression(ref tokenStream, out ExpressionIndex rangeStart);

            if (!tokenStream.Consume(SymbolType.Range)) {
                return default;
            }

            ParseUnaryExpression(ref tokenStream, out var rangeExpr);

            return new RuleResult<RangeExpression>(tokenStream.location, expressionBuffer.Add(startLocation, tokenStream.location, new RangeExpression() {
                lhs = rangeStart,
                rhs = rangeExpr
            }));

        }

        private bool MultiplicativeExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // (('*' | '/' | '%')  switch_expression)*
            // restructured recursion so we always have full token ranges for the lhs 

            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.Multiply)) {
                op = BinaryOperatorType.Multiply;
            }
            else if (tokenStream.Consume(SymbolType.Divide)) {
                op = BinaryOperatorType.Divide;
            }
            else if (tokenStream.Consume(SymbolType.Modulus)) {
                op = BinaryOperatorType.Modulus;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!SwitchExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (MultiplicativeExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool MultiplicativeExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // switch_expression (('*' | '/' | '%')  switch_expression)*

            int startLocation = tokenStream.location;

            if (!SwitchExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (MultiplicativeExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;

        }

        private bool AdditiveExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // (('+' | '-')  multiplicative_expression)*
            // restructured recursion so we always have full token ranges for the lhs 
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.Plus)) {
                op = BinaryOperatorType.Plus;
            }
            else if (tokenStream.Consume(SymbolType.Minus)) {
                op = BinaryOperatorType.Minus;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!MultiplicativeExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (AdditiveExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool AdditiveExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // multiplicative_expression (('+' | '-')  multiplicative_expression)*

            int startLocation = tokenStream.location;

            if (!MultiplicativeExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (AdditiveExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {

                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;

        }

        private bool ShiftExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // (('<<' | right_shift)  additive_expression)*
            // restructured recursion so we always have full token ranges for the lhs 
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.LessThan, 2)) {
                op = BinaryOperatorType.ShiftLeft;
            }
            else if (tokenStream.Consume(SymbolType.GreaterThan, 2)) {
                op = BinaryOperatorType.ShiftRight;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!AdditiveExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (ShiftExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;

        }

        private bool ShiftExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // additive_expression (('<<' | right_shift)  additive_expression)*

            int startLocation = tokenStream.location;

            if (!AdditiveExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (ShiftExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });

            }

            return true;

        }

        private bool RelationalExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // (('<' | '>' | '<=' | '>=') shift_expression | IS isType | AS type_)*
            // restructured recursion so we always have full token ranges for the lhs

            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.LessThan)) {
                op = BinaryOperatorType.LessThan;
            }
            else if (tokenStream.Consume(SymbolType.GreaterThan)) {
                op = BinaryOperatorType.GreaterThan;
            }
            else if (tokenStream.Consume(SymbolType.GreaterThanEqualTo)) {
                op = BinaryOperatorType.GreaterThanEqualTo;
            }
            else if (tokenStream.Consume(SymbolType.LessThanEqualTo)) {
                op = BinaryOperatorType.LessThanEqualTo;
            }
            else if (tokenStream.Peek(TemplateKeyword.Is)) { // consumed later
                op = BinaryOperatorType.Is;
            }
            else if (tokenStream.Peek(TemplateKeyword.As)) { // consumed later
                op = BinaryOperatorType.As;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            if (op == BinaryOperatorType.Is) {

                tokenStream.location++;

                if (ParseIsNullPattern(ref tokenStream, out ExpressionIndex<IsNullExpression> nullPattern)) {
                    rhs = nullPattern;
                    return true;
                }

                if (IsTypeExpression(ref tokenStream, out ExpressionIndex<IsTypeExpression> isType)) {
                    rhs = isType;
                    return true;
                }

                op = default;
                rhs = default;
                return HardError(tokenStream.location, DiagnosticError.ExpectedIsTypeOrIsNullPattern, HelpType.IsOperatorSyntax);

            }

            if (op == BinaryOperatorType.As) {
                tokenStream.location++;
                if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                    op = default;
                    rhs = default;
                    return HardError(tokenStream.location, DiagnosticError.ExpectedTypePath, HelpType.AsTypeSyntax);
                }

                rhs = typePath;
                return true;
            }

            // probably a hard error here 
            if (!ShiftExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                rhs = default;
                return false;
            }

            if (RelationalExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;

        }

        private bool ParseIsNullPattern(ref TokenStream tokenStream, out ExpressionIndex<IsNullExpression> expressionIndex) {
            ExpressionParseState state = SaveState(tokenStream);
            expressionIndex = default;

            bool not = tokenStream.Consume(TemplateKeyword.Not);
            bool isNullCheck = tokenStream.Consume(TemplateKeyword.Null);

            if (!isNullCheck) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            expressionIndex = expressionBuffer.Add(state.location, tokenStream.location, new IsNullExpression() {
                isNegated = not
            });
            return true;
        }

        private bool RelationalExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // shift_expression (('<' | '>' | '<=' | '>=') shift_expression | IS isType | AS type_)*

            int startLocation = tokenStream.location;

            if (!ShiftExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (RelationalExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;

        }

        private bool EqualityExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // relational_expression ((OP_EQ | OP_NE)  relational_expression)*

            int startLocation = tokenStream.location;

            if (!RelationalExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (EqualityExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;
        }

        private bool EqualityExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // ((OP_EQ | OP_NE)  relational_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.Equals)) {
                op = BinaryOperatorType.Equals;
            }
            else if (tokenStream.Consume(SymbolType.NotEquals)) {
                op = BinaryOperatorType.NotEquals;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!RelationalExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (EqualityExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool BinaryAndExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // ('&' equality_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.BinaryAnd)) {
                op = BinaryOperatorType.BinaryAnd;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!EqualityExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (BinaryAndExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool BinaryAndExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // equality_expression ('&' equality_expression)*
            int startLocation = tokenStream.location;

            if (!EqualityExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (BinaryAndExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;
        }

        private bool ExclusiveOrExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // ('^' and_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.BinaryXor)) {
                op = BinaryOperatorType.BinaryXor;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!BinaryAndExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (ExclusiveOrExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool ExclusiveOrExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            //  and_expression ('^' and_expression)*
            int startLocation = tokenStream.location;

            if (!BinaryAndExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (ExclusiveOrExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {

                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });

            }

            return true;
        }

        private bool InclusiveOrExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // ('|' exclusive_or_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.BinaryOr)) {
                op = BinaryOperatorType.BinaryOr;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!ExclusiveOrExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (InclusiveOrExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool InclusiveOrExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // exclusive_or_expression ('|' exclusive_or_expression)*

            int startLocation = tokenStream.location;

            if (!ExclusiveOrExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (InclusiveOrExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });

            }

            return true;
        }

        private bool ConditionalAndExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            // ('&&' inclusive_or_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.BooleanAnd)) {
                op = BinaryOperatorType.ConditionalAnd;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!InclusiveOrExpression(ref tokenStream, out ExpressionIndex switchExpr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (ConditionalAndExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = switchExpr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = switchExpr;
            }

            return true;
        }

        private bool ConditionalAndExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // inclusive_or_expression ('&&' inclusive_or_expression)*

            int startLocation = tokenStream.location;

            if (!InclusiveOrExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (ConditionalAndExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;
        }

        private bool ParseConditionalOrExpressionRHS(ref TokenStream tokenStream, out BinaryOperatorType op, out ExpressionIndex rhs) {
            //  ('||' conditional_and_expression)*
            ExpressionParseState state = SaveState(tokenStream);

            if (tokenStream.Consume(SymbolType.ConditionalOr)) {
                op = BinaryOperatorType.ConditionalOr;
            }
            else {
                op = default;
                rhs = default;
                return false;
            }

            // probably a hard error here 
            if (!ConditionalAndExpression(ref tokenStream, out ExpressionIndex expr)) {
                RestoreState(ref tokenStream, state);
                op = default;
                rhs = default;
                return false;
            }

            if (ParseConditionalOrExpressionRHS(ref tokenStream, out BinaryOperatorType nextOp, out ExpressionIndex nextExpr)) {
                rhs = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                    lhs = expr,
                    operatorType = nextOp,
                    rhs = nextExpr
                });
            }
            else {
                rhs = expr;
            }

            return true;
        }

        private bool ParseConditionalOrExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            // conditional_and_expression ('||' conditional_and_expression)*
            int startLocation = tokenStream.location;

            if (!ConditionalAndExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (ParseConditionalOrExpressionRHS(ref tokenStream, out BinaryOperatorType operatorType, out ExpressionIndex rhs)) {
                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new BinaryExpression() {
                    lhs = expressionIndex,
                    rhs = rhs,
                    operatorType = operatorType
                });
            }

            return true;
        }

        private bool ParseNullCoalescingExpression(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            //  conditional_or_expression ('??' (null_coalescing_expression | throw_expression))?
            ExpressionParseState state = SaveState(tokenStream);

            if (!ParseConditionalOrExpression(ref tokenStream, out expressionIndex)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Coalesce)) {
                return true;
            }

            ExpressionIndex rhs;
            if (ParseNullCoalescingExpression(ref tokenStream, out ExpressionIndex nullCoalesceIndex)) {
                rhs = nullCoalesceIndex;
            }
            else if (ParseThrowExpression(ref tokenStream, out ExpressionIndex<ThrowStatement> throwExpression)) {
                rhs = throwExpression;
            }
            else {
                // hard error
                RestoreState(ref tokenStream, state);
                return false;
            }

            expressionIndex = expressionBuffer.Add(state.location, tokenStream.location, new BinaryExpression() {
                operatorType = BinaryOperatorType.Coalesce,
                lhs = expressionIndex,
                rhs = rhs
            });

            return true;

        }

        private bool ParseThrowExpression(ref TokenStream tokenStream, out ExpressionIndex<ThrowStatement> throwExpression) {
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Throw)) {
                throwExpression = default;
                return false;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                // hard error
                tokenStream.location = startLocation;
                throwExpression = default;
                return false;
            }

            throwExpression = expressionBuffer.Add(startLocation, tokenStream.location, new ThrowStatement() {
                expression = expressionIndex
            });

            return true;

        }

        private bool ParseMemberInitializerList(ref TokenStream tokenStream, out ExpressionRange<MemberInitializer> list) {

            if (!ParseMemberInitializer(ref tokenStream, out ExpressionIndex<MemberInitializer> head)) {
                list = default;
                return false;
            }

            using ScopedList<ExpressionIndex<MemberInitializer>> members = scopedAllocator.CreateListScope<ExpressionIndex<MemberInitializer>>(16);

            members.Add(head);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseMemberInitializer(ref tokenStream, out ExpressionIndex<MemberInitializer> next)) {
                    // I think this allows an optional trailing comma 
                    break;
                }

                members.Add(next);
            }

            list = expressionBuffer.AddExpressionList(members);

            return true;

        }

        public bool ParseObjectOrCollectionInitializer(ref TokenStream tokenStream, out ExpressionIndex expressionIndex) {
            expressionBuffer.PushRuleScope();

            expressionBuffer.MakeRule(ParseObjectInitializerRule(tokenStream));
            expressionBuffer.MakeRule(ParseCollectionInitializerRule(tokenStream));

            return expressionBuffer.PopRuleScope(ref tokenStream, out expressionIndex);

        }

        private bool ParseCollectionInitializer(ref TokenStream tokenStream, out ExpressionIndex<CollectionInitializer> collectionInit) {
            // OPEN_BRACE element_initializer (',' element_initializer)* ','? CLOSE_BRACE
            ExpressionParseState state = SaveState(tokenStream);
            if (!tokenStream.Consume(SymbolType.CurlyBraceOpen)) {
                collectionInit = default;
                return false;
            }

            if (!ParseElementInitializer(ref tokenStream, out ExpressionIndex<ElementInitializer> firstInitializer)) {
                collectionInit = default;
                return false;
            }

            using ScopedList<ExpressionIndex<ElementInitializer>> initializers = scopedAllocator.CreateListScope<ExpressionIndex<ElementInitializer>>(16);

            initializers.Add(firstInitializer);

            while (tokenStream.Consume(SymbolType.Comma)) {

                if (ParseElementInitializer(ref tokenStream, out ExpressionIndex<ElementInitializer> next)) {
                    initializers.Add(next);
                }

            }

            if (!tokenStream.Consume(SymbolType.CurlyBraceClose)) {
                // hard error probably 
                collectionInit = default;
                return false;
            }

            collectionInit = expressionBuffer.Add(state.location, tokenStream.location, new CollectionInitializer() {
                initializers = expressionBuffer.AddExpressionList(initializers)
            });

            return true;
        }

        private bool ParseElementInitializer(ref TokenStream tokenStream, out ExpressionIndex<ElementInitializer> initializer) {
            // : non_assignment_expression
            // | '{' expression_list '}'

            int startLocation = tokenStream.location;
            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {

                if (ParseCurlyBracedExpressionList(ref tokenStream, out ExpressionRange list)) {
                    initializer = expressionBuffer.Add(startLocation, tokenStream.location, new ElementInitializer() {
                        expressionList = list,
                        initializerType = ElementInitializerType.CurlyBraceList
                    });
                    return true;
                }
            }

            if (ParseNonAssignment(ref tokenStream, out ExpressionIndex expression)) {
                initializer = expressionBuffer.Add(startLocation, tokenStream.location, new ElementInitializer() {
                    expression = expression,
                    initializerType = ElementInitializerType.SingleElement
                });
                return true;
            }

            // hard error probably
            initializer = default;
            return false;
        }

        private bool ParseCurlyBracedExpressionList(ref TokenStream tokenStream, out ExpressionRange list) {
            // : {  expression_list }
            ExpressionParseState state = SaveState(tokenStream);

            list = default;
            if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream braceStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedCurlyBrace);
            }

            PushRecoveryPoint(braceStream.start, braceStream.end + 1);

            bool parsed = ParseExpressionList(ref braceStream, out list);

            if (!PopRecoveryPoint(ref braceStream) && parsed) return true;

            RestoreState(ref tokenStream, state);
            return false;

        }

        private bool ParseSquareBracketExpressionList(ref TokenStream tokenStream, out ExpressionRange list) {
            // : {  expression_list }
            ExpressionParseState state = SaveState(tokenStream);

            list = default;
            if (!tokenStream.TryGetSubStream(SubStreamType.SquareBrackets, out TokenStream braceStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedSquareBracket);
            }

            PushRecoveryPoint(braceStream.start, braceStream.end + 1);

            bool parsed = ParseExpressionList(ref braceStream, out list);

            if (!PopRecoveryPoint(ref braceStream) && parsed) return true;

            RestoreState(ref tokenStream, state);
            return false;

        }

        private bool ParseArgumentList(ref TokenStream tokenStream, out ExpressionRange<Argument> argList) {
            // argument ( ',' argument)*

            ExpressionParseState state = SaveState(tokenStream);
            if (!ParseArgument(ref tokenStream, out ExpressionIndex<Argument> firstArgument)) {
                argList = default;
                return false;
            }

            using AllocatorScope scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex<Argument>> arguments = scope.CreateList<ExpressionIndex<Argument>>(16);
            arguments.Add(firstArgument);

            while (tokenStream.Consume(SymbolType.Comma)) {

                if (!ParseArgument(ref tokenStream, out ExpressionIndex<Argument> chain)) {
                    // hard error? not sure what would respond to this 
                    RestoreState(ref tokenStream, state);
                    argList = default;
                    return false;
                }

                arguments.Add(chain);

            }

            argList = expressionBuffer.AddExpressionList(arguments);

            return true;

        }

        private bool ParseArgument(ref TokenStream tokenStream, out ExpressionIndex<Argument> argument) {
            // (identifier ':')? refout=(REF | OUT | IN)? (VAR | type_)? expression
            // not supporting (identifier :)? and skipping (VAR | type_)? 

            ExpressionParseState state = SaveState(tokenStream);
            Argument.ArgumentModifier modifier = Argument.ArgumentModifier.None;

            if (tokenStream.Consume(TemplateKeyword.Ref)) {
                modifier = Argument.ArgumentModifier.Ref;
            }
            else if (tokenStream.Consume(TemplateKeyword.Out)) {
                modifier = Argument.ArgumentModifier.Out;
            }
            else if (tokenStream.Consume(TemplateKeyword.In)) {
                modifier = Argument.ArgumentModifier.In;
            }

            // if (tokenStream.Consume(TemplateKeyword.Var) || ParseTypePath(ref tokenStream, out var typePath)) {
            //  no clue why these are here in the grammar    
            // }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                argument = default;
                RestoreState(ref tokenStream, state);
                return false;
            }

            argument = expressionBuffer.Add(state.location, tokenStream.location, new Argument() {
                modifier = modifier,
                expression = expressionIndex
            });

            return true;

        }

        private bool ParseThis(ref TokenStream tokenStream, out ExpressionIndex<Identifier> thisExpr) {
            Token current = tokenStream.Current;

            if (current.tokenType == TokenType.DollarIdentifier && current.RuntimeKeyword == RuntimeKeyword.DollarThis) {
                thisExpr = expressionBuffer.Add(tokenStream.location, tokenStream.location + 1, new Identifier() {
                    identifierType = IdentifierType.DollarIdentifier,
                });
                return true;
            }

            if (current.tokenType == TokenType.KeywordOrIdentifier && current.Keyword == TemplateKeyword.This) {
                thisExpr = expressionBuffer.Add(tokenStream.location, tokenStream.location + 1, new Identifier() {
                    identifierType = IdentifierType.This,
                });
                return true;
            }

            thisExpr = default;
            return false;
        }

        private bool ParsePredefinedType(ref TokenStream tokenStream, out ExpressionIndex<PrimaryIdentifier> identifier) {
            // I'm not sure if I really need this to different than an identifier 
            TemplateKeyword keyword = tokenStream.Current.Keyword;
            SimpleTypeName simpleTypeName = GetSimpleTypeName(keyword);
            if (simpleTypeName == SimpleTypeName.None) {
                identifier = default;
                return false;
            }
            identifier = expressionBuffer.Add(tokenStream.location, tokenStream.location + 1, new PrimaryIdentifier() {
                identifierLocation = new NonTrivialTokenLocation(tokenStream.location),
                typeArgumentList = default,
                simpleTypeName = simpleTypeName
            });
            tokenStream.location++;
            return true;

        }

        private bool ParseParenExpression(ref TokenStream tokenStream, out ExpressionIndex<ParenExpression> parenExpression) {
            if (tokenStream.Current.Symbol != SymbolType.OpenParen) {
                parenExpression = default;
                return false;
            }

            ExpressionParseState state = SaveState(tokenStream);

            tokenStream.location++;

            ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex);

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                RestoreState(ref tokenStream, state);
                parenExpression = default;
                return false;
            }

            // todo -- I'm not sure that this needs to be a node itself, it just controls parser flow 
            // maybe it's helpful to have for token info though

            parenExpression = expressionBuffer.Add(state.location, tokenStream.location, new ParenExpression() {
                expression = expressionIndex
            });

            return true;
        }

        private bool ParseBracketExpression(ref TokenStream tokenStream, out ExpressionIndex<BracketExpression> bracketExpression) {
            //  '?'? '[' indexer_argument ( ',' indexer_argument)* ']'
            ExpressionParseState state = SaveState(tokenStream);

            bool isNullable = tokenStream.Consume(SymbolType.QuestionMark);

            if (!tokenStream.Consume(SymbolType.SquareBraceOpen)) {
                RestoreState(ref tokenStream, state);
                bracketExpression = default;
                return false;
            }

            if (!ParseIndexerArgument(ref tokenStream, out var firstArg)) {
                // hard error maybe
                bracketExpression = default;
                return false;
            }

            using var scope = scopedAllocator.PushScope();
            ScopedList<ExpressionIndex> arguments = scope.CreateList<ExpressionIndex>(4);
            arguments.Add(firstArg);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseIndexerArgument(ref tokenStream, out ExpressionIndex arg)) {
                    // hard error
                    RestoreState(ref tokenStream, state);
                    bracketExpression = default;
                    return false;
                }

                arguments.Add(arg);
            }

            if (!tokenStream.Consume(SymbolType.SquareBraceClose)) {
                // hard error
                RestoreState(ref tokenStream, state);
                bracketExpression = default;
                return false;
            }

            bracketExpression = expressionBuffer.Add(state.location, tokenStream.location, new BracketExpression() {
                arguments = expressionBuffer.AddExpressionList(arguments),
                isNullable = isNullable
            });
            return true;
        }

        private bool ParseIndexerArgument(ref TokenStream tokenStream, out ExpressionIndex arg) {
            // rule is (identifier ':')? expression
            // we don't support named identifiers so we just return expression
            return ParseExpression(ref tokenStream, out arg);
        }

        private RuleResult ParsePrimaryExpression(TokenStream tokenStream) {

            // original rule
            // primary_expression_start '!'? bracket_expression* '!'? (((member_access | method_invocation | '++' | '--' | '->' identifier) '!'?) bracket_expression* '!'?)*
            // actual rule
            // primary_expression_start bracket_expression* ((member_access | method_invocation | '++' | '--') bracket_expression* )*

            int startLocation = tokenStream.location;

            if (!ParsePrimaryExpressionStart(ref tokenStream, out ExpressionIndex start)) {
                return default;
            }

            ParseBracketExpressionChain(ref tokenStream, out ExpressionRange<BracketExpression> bracketChain);

            // i'm not sure if we want to left recurse here for token data like we for for operators, maybe not

            using ScopedList<ExpressionIndex<PrimaryExpressionPart>> parts = scopedAllocator.CreateListScope<ExpressionIndex<PrimaryExpressionPart>>(8);

            while (ParsePrimaryPart(ref tokenStream, out ExpressionIndex<PrimaryExpressionPart> part)) {
                parts.Add(part);
            }

            if (bracketChain.length > 0 || parts.size > 0) {
                ExpressionIndex<PrimaryExpression> retn = expressionBuffer.Add(startLocation, tokenStream.location, new PrimaryExpression() {
                    start = start,
                    bracketExpressions = bracketChain,
                    parts = parts.size == 0 ? default : expressionBuffer.AddExpressionList(parts)
                });

                return new RuleResult(tokenStream.location, retn);
            }

            return new RuleResult(tokenStream.location, start);
        }

        private bool ParseBracketExpressionChain(ref TokenStream tokenStream, out ExpressionRange<BracketExpression> bracketExpressions) {

            if (!ParseBracketExpression(ref tokenStream, out ExpressionIndex<BracketExpression> first)) {
                bracketExpressions = default;
                return false;
            }

            using ScopedList<ExpressionIndex<BracketExpression>> bracketList = scopedAllocator.CreateListScope<ExpressionIndex<BracketExpression>>(4);
            bracketList.Add(first);

            while (ParseBracketExpression(ref tokenStream, out ExpressionIndex<BracketExpression> bracketExpression)) {
                bracketList.Add(bracketExpression);
            }

            bracketExpressions = expressionBuffer.AddExpressionList(bracketList);

            return true;

        }

        private bool ParsePrimaryPart(ref TokenStream tokenStream, out ExpressionIndex<PrimaryExpressionPart> expressionPart) {

            // (member_access | method_invocation | '++' | '--') bracket_expression*

            int startLocation = tokenStream.location;

            if (ParseMemberAccess(ref tokenStream, out ExpressionIndex<MemberAccess> memberAccess)) {

                ParseBracketExpressionChain(ref tokenStream, out ExpressionRange<BracketExpression> bracketExpressions);

                expressionPart = expressionBuffer.Add(startLocation, tokenStream.location, new PrimaryExpressionPart() {
                    expression = memberAccess,
                    partType = PrimaryExpressionPartType.MemberAccess,
                    bracketExpressions = bracketExpressions,
                });

                return true;
            }

            if (ParseMethodInvocation(ref tokenStream, out ExpressionIndex<MethodInvocation> methodInvoke)) {

                ParseBracketExpressionChain(ref tokenStream, out ExpressionRange<BracketExpression> bracketExpressions);

                expressionPart = expressionBuffer.Add(startLocation, tokenStream.location, new PrimaryExpressionPart() {
                    expression = methodInvoke,
                    partType = PrimaryExpressionPartType.MethodInvocation,
                    bracketExpressions = bracketExpressions,
                });

                return true;
            }

            if (ParseIncrementDecrement(ref tokenStream, out ExpressionIndex<IncrementDecrement> incrDecr)) {

                ParseBracketExpressionChain(ref tokenStream, out ExpressionRange<BracketExpression> bracketExpressions);

                expressionPart = expressionBuffer.Add(startLocation, tokenStream.location, new PrimaryExpressionPart() {
                    expression = incrDecr,
                    partType = PrimaryExpressionPartType.IncrementDecrement,
                    bracketExpressions = bracketExpressions,
                });

                return true;
            }

            expressionPart = default;
            return false;

        }

        private bool ParseMemberAccess(ref TokenStream tokenStream, out ExpressionIndex<MemberAccess> memberAccess) {
            // '?'? '.' identifier type_argument_list?

            bool isConditionalAccess;
            memberAccess = default;

            ExpressionParseState state = SaveState(tokenStream);
            if (tokenStream.Consume(SymbolType.ConditionalAccess)) {
                isConditionalAccess = true;
            }
            else if (tokenStream.Consume(SymbolType.Dot)) {
                isConditionalAccess = false;
            }
            else {
                memberAccess = default;
                return false;
            }

            if (!ParseIdentifierWithTypeArguments(ref tokenStream, out ExpressionIndex<Identifier> identifier)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            memberAccess = expressionBuffer.Add(state.location, tokenStream.location, new MemberAccess() {
                identifier = identifier,
                isConditionalAccess = isConditionalAccess
            });

            return true;

        }

        private bool ParseMethodInvocation(ref TokenStream tokenStream, out ExpressionIndex<MethodInvocation> invocation) {
            // OPEN_PARENS argument_list? CLOSE_PARENS
            invocation = default;
            ExpressionParseState state = SaveState(tokenStream);

            if (!tokenStream.Consume(SymbolType.OpenParen)) {
                return default;
            }

            // optional 
            ParseArgumentList(ref tokenStream, out ExpressionRange<Argument> argumentList);

            if (!tokenStream.Consume(SymbolType.CloseParen)) {
                RestoreState(ref tokenStream, state);
                return false;
            }

            invocation = expressionBuffer.Add(state.location, tokenStream.location, new MethodInvocation() {
                argumentList = argumentList
            });
            return true;

        }

        private bool ParseIncrementDecrement(ref TokenStream tokenStream, out ExpressionIndex<IncrementDecrement> expressionIndex) {

            if (tokenStream.Consume(SymbolType.Increment)) {
                expressionIndex = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new IncrementDecrement() {
                    isIncrement = true
                });
                return true;
            }

            if (tokenStream.Consume(SymbolType.Decrement)) {
                expressionIndex = expressionBuffer.Add(tokenStream.location - 1, tokenStream.location, new IncrementDecrement() {
                    isIncrement = false
                });
                return true;
            }

            expressionIndex = default;
            return false;
        }

        public bool ParseIdentifier(ref TokenStream stream, out ExpressionIndex<Identifier> identifier) {
            if (stream.ConsumeKeywordOrIdentifier(out Token keywordOrIdentifier)) {
                identifier = expressionBuffer.Add(stream.location - 1, stream.location, new Identifier() {
                    identifierType = default
                });
                return true;
            }

            identifier = default;
            return false;
        }

    }

}