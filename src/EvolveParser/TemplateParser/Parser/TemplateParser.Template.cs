using System;
using EvolveUI.Compiler;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {
    public partial struct TemplateParser {

        /* Kill List
         - figure out how to pre-warn / do better on unmatched braces n stuff 
         */


        public unsafe bool ParseFunctionDeclaration(ref TokenStream tokenStream, out TemplateFunctionDeclaration functionDeclaration) {
            // : 'function' identifier type_parameter_list? (':' type_path)? function_paren_signature? (';' | 'render' template_body | function_declaration_body)

            int startLocation = tokenStream.location;
            functionDeclaration = default;

            int nodeStart = templateBuffer.nodes.size;
            int expressionStart = expressionBuffer.expressions.size;

            if (!tokenStream.Consume(TemplateKeyword.Function)) {
                return false;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier);
            }

            // optional
            ParseTemplateTypeParameterListWithRecovery(ref tokenStream, out ExpressionRange<Identifier> typeParameters);

            NodeIndex<TemplateBlockNode> body = default;
            ExpressionIndex<TypePath> companionTypePath = default;

            if (tokenStream.Consume(SymbolType.Colon) && !ParseTypePath(ref tokenStream, out companionTypePath)) {
                HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.CompanionTypeNameIsMissingOrInvalid);
                return false;
            }

            using ScopedList<NodeIndex> list = scopedAllocator.CreateListScope<NodeIndex>(32);

            ParseFunctionalTemplateArgumentsWithRecovery(ref tokenStream, TopLevelElementType.Function, &list);

            // we either have
            // - a 'render' statement
            // - a function_declaration_body 

            bool isValid = false;

            if (tokenStream.Consume(TemplateKeyword.Render) && ParseTemplateBlockWithRecovery(ref tokenStream, false, out body)) {
                isValid = true;
            }
            else if (ParseStructuredFunctionBlock(ref tokenStream, &list, out body)) {
                isValid = true;
            }

            if (isValid) {
                ValidateMembers(&list);

                functionDeclaration = new TemplateFunctionDeclaration() {
                    identifierLocation = identifierLocation,
                    typeParameters = typeParameters,
                    body = body,
                    tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location),
                    templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart),
                    expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart),
                    signature = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSignatureNode() {
                        companionTypePath = companionTypePath,
                        arguments = templateBuffer.AddNodeList(list),
                    })
                };
            }

            return isValid;
        }

        public unsafe bool ParseVariantDeclaration(ref TokenStream tokenStream, int decoratorNodeStart, int decoratorExpressionStart, NodeRange<DecoratorNode> decorators, out VariantDeclaration variantDeclaration) {
            // : decorator* dotted_identifier type_parameter_list? structural_variant_body 

            int startLocation = tokenStream.location;
            variantDeclaration = default;

            int nodeStart = decorators.length == 0 ? templateBuffer.nodes.size : decoratorNodeStart;
            int expressionStart = decorators.length == 0 ? expressionBuffer.expressions.size : decoratorExpressionStart;

            throw new NotImplementedException();


            // return true;
        }

        public unsafe bool ParseTemplateDeclaration(ref TokenStream tokenStream, int decoratorNodeStart, int decoratorExpressionStart, NodeRange<DecoratorNode> decorators, out TemplateDeclaration templateDeclaration) {
            // : decorator_list? 'template' identifier type_parameter_list? (':' type_path)? template_paren_signature? (';' | 'render' template_body | template_declaration_body)
            // decorators are passed in 

            int startLocation = tokenStream.location;
            templateDeclaration = default;

            int nodeStart = decorators.length == 0 ? templateBuffer.nodes.size : decoratorNodeStart;
            int expressionStart = decorators.length == 0 ? expressionBuffer.expressions.size : decoratorExpressionStart;

            if (!tokenStream.Consume(TemplateKeyword.Template)) {
                return false;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier);
            }

            ParseTemplateTypeParameterListWithRecovery(ref tokenStream, out ExpressionRange<Identifier> typeParameters);

            NodeIndex<TemplateSpawnList> spawnlist = default;
            NodeIndex<TemplateBlockNode> body = default;
            ExpressionIndex<TypePath> companionTypePath = default;

            if (tokenStream.Consume(SymbolType.Colon) && !ParseTypePath(ref tokenStream, out companionTypePath)) {
                HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.CompanionTypeNameIsMissingOrInvalid);
                return false;
            }

            // I want to keep this buffer open since we can accept values from both the function parameter list () or a structured template block { }

            using ScopedList<NodeIndex> list = scopedAllocator.CreateListScope<NodeIndex>(32);

            ParseFunctionalTemplateArgumentsWithRecovery(ref tokenStream, TopLevelElementType.Template, &list);

            if (tokenStream.Consume(SymbolType.Colon) && !ParseTypePath(ref tokenStream, out companionTypePath)) {
                HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.CompanionTypeNameIsMissingOrInvalid);
                return false;
            }

            // we either have
            // - a semi colon
            // - a 'render' statement
            // - a template_declaration_body 

            bool isValid = false;

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                isValid = true;
            }
            else if (tokenStream.Consume(TemplateKeyword.Render) && ParseTemplateBlockWithRecovery(ref tokenStream, false, out body)) {
                isValid = true;
            }
            else if (ParseStructuredTemplateDeclarationBlock(ref tokenStream, &list, out body, out spawnlist)) {
                isValid = true;
            }

            if (isValid) {
                ValidateMembers(&list);

                templateDeclaration = new TemplateDeclaration() {
                    decorators = decorators,
                    identifierLocation = identifierLocation,
                    typeParameters = typeParameters,
                    body = body,
                    tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location),
                    templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart),
                    expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart),
                    spawnList = spawnlist,
                    signature = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSignatureNode() {
                        companionTypePath = companionTypePath,
                        arguments = templateBuffer.AddNodeList(list),
                    })
                };
            }

            return isValid;
        }

        private unsafe bool ParseDecoratorDeclaration(ref TokenStream tokenStream, out DecoratorDeclaration decoratorDeclaration) {
            // : 'decorator' 'typography'? identifier type_arguments? (':' type_path)? decorator_block
            // | 'decorator' identifier ';'

            decoratorDeclaration = default;

            int startLocation = tokenStream.location;

            int nodeStart = templateBuffer.nodes.size;
            int expressionStart = expressionBuffer.expressions.size;

            if (!tokenStream.Consume(TemplateKeyword.Decorator)) {
                return false;
            }

            bool isTypographyDecorator = tokenStream.Consume(TemplateKeyword.Typography);

            if (!tokenStream.ConsumeStandardIdentifier(out decoratorDeclaration.identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.DecoratorDeclarationSyntax);
            }

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                decoratorDeclaration.isMarkerOnly = true;
                decoratorDeclaration.tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
                return true;
            }

            // optional
            ParseTemplateTypeParameterListWithRecovery(ref tokenStream, out ExpressionRange<Identifier> typeParameters);

            ExpressionIndex<TypePath> companionTypePath = default;

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!ParseTypePath(ref tokenStream, out companionTypePath)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.DecoratorDeclarationSyntax);
                }
            }

            using ScopedList<NodeIndex> decoratorMemberList = scopedAllocator.CreateListScope<NodeIndex>(32);

            if (!ParseDecoratorBlock(ref tokenStream, &decoratorMemberList)) {
                return false;
            }

            ValidateMembers(&decoratorMemberList);

            NodeIndex<TemplateSignatureNode> signature = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSignatureNode() {
                companionTypePath = companionTypePath,
                arguments = templateBuffer.AddNodeList(decoratorMemberList),
            });

            decoratorDeclaration.signature = signature;
            decoratorDeclaration.isTypography = isTypographyDecorator;
            decoratorDeclaration.typeParameters = typeParameters;
            decoratorDeclaration.tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location);
            decoratorDeclaration.templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart);
            decoratorDeclaration.expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart);

            decoratorDeclaration.isMarkerOnly = decoratorMemberList.size == 0 && decoratorMemberList.size == 0 && !companionTypePath.IsValid;

            return true;
        }

        public bool ParseTypographyDeclaration(ref TokenStream tokenStream, int decoratorNodeStart, int decoratorExpressionStart, NodeRange<DecoratorNode> decorators, out TypographyDeclaration typographyDeclaration) {
            // : decorator_list? 'typography' identifier type_parameter_list? (';' | structural_typography | functional_typography)
            // decorators are passed in 

            int startLocation = tokenStream.location;
            typographyDeclaration = default;

            int nodeStart = decorators.length == 0 ? templateBuffer.nodes.size : decoratorNodeStart;
            int expressionStart = decorators.length == 0 ? expressionBuffer.expressions.size : decoratorExpressionStart;

            if (!tokenStream.Consume(TemplateKeyword.Typography)) {
                return false;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier);
            }

            ParseTemplateTypeParameterListWithRecovery(ref tokenStream, out ExpressionRange<Identifier> typeParameters);

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                typographyDeclaration = new TypographyDeclaration() {
                    decorators = decorators,
                    identifierLocation = identifierLocation,
                    signature = default,
                    tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location),
                    templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart),
                    expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart),
                    typeParameters = typeParameters
                };

                return true;
            }

            // structural version 
            ExpressionIndex<TypePath> backingTypePath = default;

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!ParseTypePath(ref tokenStream, out backingTypePath)) {
                    HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.CompanionTypeNameIsMissingOrInvalid);
                    return false;
                }

                // todo -- verify that this is not a pointer, array, or nullable type
            }

            if (tokenStream.Peek(SymbolType.OpenParen)) {
                ParseFunctionalTemplateSignatureWithRecovery(ref tokenStream, TopLevelElementType.Typography, out NodeIndex<TemplateSignatureNode> signature);

                if (!tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedSemiColon, HelpType.TypographySyntax);
                }

                typographyDeclaration = new TypographyDeclaration() {
                    decorators = decorators,
                    identifierLocation = identifierLocation,
                    signature = signature,
                    tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location),
                    templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart),
                    expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart),
                    typeParameters = typeParameters
                };
                return true;
            }

            else {
                if (!ParseTypographyBlock(ref tokenStream, out NodeRange memberList)) {
                    return false;
                }

                NodeIndex<TemplateSignatureNode> signature = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSignatureNode() {
                    companionTypePath = backingTypePath,
                    arguments = memberList,
                });

                typographyDeclaration = new TypographyDeclaration() {
                    decorators = decorators,
                    identifierLocation = identifierLocation,
                    signature = signature,
                    tokenRange = new NonTrivialTokenRange(startLocation, tokenStream.location),
                    templateRange = new RangeInt(nodeStart, templateBuffer.nodes.size - nodeStart),
                    expressionRange = new RangeInt(expressionStart, expressionBuffer.expressions.size - expressionStart),
                    typeParameters = typeParameters
                };

                return true;
            }
        }

        private void ParseFunctionalTemplateSignatureWithRecovery(ref TokenStream tokenStream, TopLevelElementType topLevelElementType, out NodeIndex<TemplateSignatureNode> signature) {
            // : element_argument_list? 

            signature = default;
            int startLocation = tokenStream.location;

            ExpressionIndex<TypePath> backingTypePath = default;

            ParseFunctionalTemplateArgumentsWithRecovery(ref tokenStream, topLevelElementType, out NodeRange arguments);

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!ParseTypePath(ref tokenStream, out backingTypePath)) {
                    HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.CompanionTypeNameIsMissingOrInvalid);
                    return;
                }

                // todo -- verify that this is not a pointer, array, or nullable type
            }

            signature = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSignatureNode() {
                companionTypePath = backingTypePath,
                arguments = arguments
            });
        }

        private bool ParseTemplateFunctionParameter(ref TokenStream tokenStream, ref bool requiresDefaultValue, out NodeIndex<TemplateFunctionParameter> nodeIndex) {
            // : (ref | out)? type_path identifier ( '=' expression)? 

            nodeIndex = default;
            int startLocation = tokenStream.location;
            TemplateFnParameterModifier modifier = TemplateFnParameterModifier.None;
            if (tokenStream.Consume(TemplateKeyword.Ref)) {
                modifier = TemplateFnParameterModifier.Ref;
            }
            else if (tokenStream.Consume(TemplateKeyword.Out)) {
                modifier = TemplateFnParameterModifier.Out;
            }

            if (!ParseTypePath(ref tokenStream, out var typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.TemplateFunctionParameterSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.TemplateFunctionParameterSyntax);
            }

            if (requiresDefaultValue && !tokenStream.Peek(SymbolType.Assign)) {
                return HardError(tokenStream, DiagnosticError.DefaultValueExpressionIsRequired, HelpType.TemplateFunctionParameterSyntax);
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateFunctionParameter() {
                    modifier = modifier,
                    defaultValue = default,
                    identifierLocation = identifierLocation,
                    typePath = typePath
                });
                return true;
            }

            requiresDefaultValue = true;

            if (!ParseExpression(ref tokenStream, out ExpressionIndex defaultValue)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.TemplateFunctionParameterSyntax);
            }

            if (modifier != TemplateFnParameterModifier.None) {
                return HardError(startLocation, DiagnosticError.CannotProvideBothAParameterModiferAndADefaultValue, HelpType.TemplateFunctionParameterSyntax);
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateFunctionParameter() {
                modifier = modifier,
                defaultValue = defaultValue,
                identifierLocation = identifierLocation,
                typePath = typePath
            });

            return true;
        }


        private unsafe bool ParseFunctionalTemplateArgumentsWithRecovery(ref TokenStream tokenStream, TopLevelElementType topLevelElementType, ScopedList<NodeIndex>* memberList) {
            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return true;
            }

            int startLocation = tokenStream.location;

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream argStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.ElementTagsRequireAnArgumentList);
            }

            PushRecoveryPoint(startLocation, tokenStream.location);

            ParseFunctionTemplateSignatureArguments(ref argStream, topLevelElementType, memberList);

            return !PopRecoveryPoint(ref argStream);
        }

        private bool ParseFunctionalTemplateArgumentsWithRecovery(ref TokenStream tokenStream, TopLevelElementType topLevelElementType, out NodeRange nodeRange) {
            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                nodeRange = default;
                return true;
            }

            int startLocation = tokenStream.location;

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream argStream)) {
                nodeRange = default;
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.ElementTagsRequireAnArgumentList);
            }

            PushRecoveryPoint(startLocation, tokenStream.location);

            ParseFunctionTemplateSignatureArguments(ref argStream, topLevelElementType, out nodeRange);

            return !PopRecoveryPoint(ref argStream);
        }

        private bool ParseElementArgumentsWithRecovery(ref TokenStream tokenStream, bool isTemplateFn, out NodeRange nodeRange) {
            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                nodeRange = default;
                return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, HelpType.ElementTagsRequireAnArgumentList);
            }

            int startLocation = tokenStream.location;

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream argStream)) {
                nodeRange = default;
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.ElementTagsRequireAnArgumentList);
            }

            PushRecoveryPoint(startLocation, tokenStream.location);

            ParseElementArguments(ref argStream, isTemplateFn, out nodeRange);

            return !PopRecoveryPoint(ref argStream);
        }

        private bool ParseElementBody(ref TokenStream tokenStream, out NodeIndex<TemplateBlockNode> nodeIndex) {
            // : '{' template_statement_list '}'
            // | ';'

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                // accept it but don't make a node 
                nodeIndex = default;
                return true;
            }

            ParseTemplateBlockWithRecovery(ref tokenStream, true, out nodeIndex);
            return true;
        }

        private bool ParseExtrusion(ref TokenStream tokenStream, out NodeIndex<Extrusion> extrusion) {
            // : '^'? identifier ('as' identifier)?
            // | '^'? '&' identifier

            int startLocation = tokenStream.location;

            extrusion = default;

            NonTrivialTokenLocation identifier = default;
            NonTrivialTokenLocation alias = default;

            bool isLifted = tokenStream.Consume(SymbolType.BinaryXor);

            if (tokenStream.Consume(SymbolType.BinaryAnd)) {
                if (!tokenStream.ConsumeAnyIdentifier(out identifier)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.ExtrusionSyntax);
                }

                extrusion = templateBuffer.Add(startLocation, tokenStream.location, new Extrusion() {
                    isElementExtrusion = true,
                    alias = identifier,
                    identifierLocation = identifier,
                    isDiscard = false
                });

                return true;
            }

            bool isDiscard = false;
            if (tokenStream.Consume(SymbolType.Underscore)) {
                isDiscard = true;
            }
            else if (!tokenStream.ConsumeAnyIdentifier(out identifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.ExtrusionSyntax);
            }

            if (tokenStream.Consume(TemplateKeyword.As)) {
                if (!tokenStream.ConsumeAnyIdentifier(out alias)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.ExtrusionSyntax);
                }
            }
            else {
                alias = identifier;
            }

            extrusion = templateBuffer.Add(startLocation, tokenStream.location, new Extrusion() {
                alias = alias,
                isElementExtrusion = false,
                identifierLocation = identifier,
                isDiscard = isDiscard,
                isLifted = isLifted
            });

            return true;
        }

        private bool ParseExtrusionList(ref TokenStream tokenStream, out NodeRange<Extrusion> nodeRange) {
            // (extrusion (',' extrusion)*)?  

            nodeRange = default;

            if (!ParseExtrusion(ref tokenStream, out NodeIndex<Extrusion> extrusion)) {
                return false;
            }

            using ScopedList<NodeIndex<Extrusion>> extrusionList = scopedAllocator.CreateListScope<NodeIndex<Extrusion>>(8);

            extrusionList.Add(extrusion);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseExtrusion(ref tokenStream, out NodeIndex<Extrusion> next)) {
                    return false; // error already reported
                }

                extrusionList.Add(next);
            }

            nodeRange = templateBuffer.AddNodeList(extrusionList);
            return true;
        }

        internal enum TopLevelElementType {

            Template,
            Typography,
            Function,
            Decorator

        }

        private bool ParseTemplateSignatureArgument(ref TokenStream tokenStream, TopLevelElementType toplevelType, out NodeIndex argumentIndex) {
            // : PARAM type_path identifier (USING identifier)? (= (block | non_assignment_expression) )?
            // | STATE type_path identifier (= (block | non_assignment_expression) )?
            // | EXTRUDE type_path identifier (= (block | non_assignment_expression) )?
            // | SLOT identifier '(' parameter_list ')'
            // | ATTR: dashed_identifier (= non_assignment_expression)?
            // | STYLE = '[' style_id (, style_id)* ']'
            // | STYLE ':' style_property_name = style_expression
            // | ONCHANGE ':' identifier = (block | non_assignment_expression) 
            // | (BEFORE | AFTER) ':' lifecycle_event = (block | non_assignment_expression) 
            // | input_event

            TemplateKeyword keyword = tokenStream.Current.Keyword;
            argumentIndex = default;
            switch (keyword) {
                case TemplateKeyword.Slot: {
                    if (toplevelType != TopLevelElementType.Template && toplevelType != TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.OnlyTemplatesAndFunctionsSupportSlots);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseSlotSignature(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedTemplateParameter, HelpType.TemplateSlotSignatureSyntax);
                }

                case TemplateKeyword.State: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseTemplateState(ref nextStream, true, out NodeIndex<TemplateStateDeclaration> stateIndex);
                        argumentIndex = stateIndex;
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedTemplateParameter, HelpType.TemplateParamSyntax);
                }

                case TemplateKeyword.Required:
                case TemplateKeyword.Optional: {
                    if (toplevelType == TopLevelElementType.Typography) {
                        return HardError(tokenStream.location, DiagnosticError.TypographyDoesNotSupportParameters);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseTemplateParameter(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedTemplateParameter, HelpType.TemplateParamSyntax);
                }

                case TemplateKeyword.Computed: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseComputedProperty(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedComputedPropertyExpression, HelpType.TemplateParamSyntax);
                }

                case TemplateKeyword.Method: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseMethod(ref nextStream, out argumentIndex, false);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedMethodDefinition, HelpType.TemplateMethodSyntax);
                }

                case TemplateKeyword.OnChange: {
                    if (toplevelType == TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportOnChange);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseOnChangeBinding(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedOnChangeBinding, HelpType.OnChangeSyntax);
                }

                case TemplateKeyword.Before: {
                    if (toplevelType == TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportLifeCycleEvents);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseBeforeLifeCycleHandler(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                }

                case TemplateKeyword.After: {
                    if (toplevelType == TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportLifeCycleEvents);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseAfterLifeCycleHandler(ref nextStream, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                }

                case TemplateKeyword.Attr: {
                    if (toplevelType == TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportAttributes);
                    }

                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseAttribute(ref nextStream, false, out argumentIndex);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                }

                case TemplateKeyword.Style: {
                    if (toplevelType == TopLevelElementType.Function) {
                        return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportStyles);
                    }

                    return ParseElementStyle(ref tokenStream, out argumentIndex) || HardError(tokenStream, DiagnosticError.ExpectedStyleDefinition);
                }

                default: {
                    if (TryParseInputHandler(ref tokenStream, true, out argumentIndex)) {
                        if (toplevelType == TopLevelElementType.Function) {
                            return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportInputHandlers);
                        }

                        return true;
                    }

                    if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Colon)) {
                        return HardError(tokenStream, DiagnosticError.UnknownPropertyPrefix, HelpType.PropertyPrefixNames);
                    }

                    // we never hit this, right? 
                    // if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                    //     PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                    //     ParsePropertyBinding(ref nextStream, false, out argumentIndex);
                    //     PopRecoveryPoint(ref nextStream);
                    //     return true;
                    // }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedPropertyBinding, HelpType.PropertyBindingSyntax);
                }
            }
        }

        private bool ParseTemplateSlotOverride(ref TokenStream tokenStream, out NodeIndex nodeIndex) {
            // : 'slot' '->' identifier ( '[' extrusion_list ']' )? ( ( '=>' template_statement)  | template_block)
            // | 'slot' '->' '(' csExpression ')' ( '[' extrusion_list ']' )? ( ( '=>' template_statement)  | template_block)

            nodeIndex = default;
            int startLocation = tokenStream.location;
            NonTrivialTokenLocation identifier = default;
            ExpressionIndex slotExpression = default;
            NodeIndex contents = default;
            SlotType slotType = SlotType.Named;

            if (!tokenStream.Consume(TemplateKeyword.Slot)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.ThinArrow)) {
                return false;
            }

            if (tokenStream.Peek(SymbolType.SquareBraceOpen)) {
                slotType = SlotType.Dynamic;

                if (!GetRequiredSubStream(ref tokenStream, SubStreamType.SquareBrackets, HelpType.SlotOverrideSyntax, out TokenStream parenStream)) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedExpression, HelpType.SlotOverrideSyntax);
                }

                if (!ParseExpression(ref parenStream, out slotExpression)) {
                    return HardError(tokenStream.location, DiagnosticError.ExpectedExpression, HelpType.SlotOverrideSyntax);
                }

                if (parenStream.HasMoreTokens) {
                    return HardError(tokenStream.location, DiagnosticError.UnexpectedToken, HelpType.SlotOverrideSyntax);
                }
            }
            else if (tokenStream.Consume(TemplateKeyword.Implicit)) {
                slotType = SlotType.Implicit;
                // explicitly named implicit slot not supported because it's not useful and introduces edge cases that I don't want to handle 
                return HardError(tokenStream.location - 1, DiagnosticError.ImplicitSlotCannotBeExplicitlyUsed);
            }
            else if (!tokenStream.ConsumeAnyIdentifier(out identifier)) {
                slotType = SlotType.Named;
                return HardError(tokenStream.location, DiagnosticError.ExpectedIdentifier, HelpType.SlotOverrideSyntax);
            }

            // optional 
            ParseParentArgumentExtrusionsWithRecovery(ref tokenStream, out NodeRange<Extrusion> extrusions);

            if (tokenStream.Consume(SymbolType.FatArrow)) {
                ParseTemplateEmbeddedStatement(ref tokenStream, out contents);
            }
            else {
                ParseTemplateBlockWithRecovery(ref tokenStream, false, out NodeIndex<TemplateBlockNode> blockNode);
                contents = blockNode;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new SlotOverrideNode() {
                slotType = slotType,
                contents = contents,
                slotName = identifier,
                extrusions = extrusions,
                slotExpression = slotExpression,
            });

            return true;
        }

        private bool ParseSlotSignature(ref TokenStream tokenStream, out NodeIndex nodeIndex) {
            // : 'slot' 'implicit' ( '(' (type_path identifier)* ')' )?
            // | 'slot' identifier ( '(' (type_path identifier)* ')' )?
            // | 'slot' 'dynamic' ( '(' (type_path identifier)* ')' )?

            nodeIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Slot)) {
                return false;
            }

            SlotType slotType;
            NonTrivialTokenLocation identifierLocation = default;

            if (tokenStream.Consume(TemplateKeyword.Implicit)) {
                slotType = SlotType.Implicit;
                identifierLocation = new NonTrivialTokenLocation(tokenStream.location - 1);
            }
            else if (tokenStream.Consume(TemplateKeyword.Dynamic)) {
                slotType = SlotType.Dynamic;
                identifierLocation = new NonTrivialTokenLocation(tokenStream.location - 1);
            }
            else if (tokenStream.ConsumeAnyIdentifier(out identifierLocation)) {
                slotType = SlotType.Named;
            }
            else {
                return HardError(tokenStream, DiagnosticError.ExpectedSlotName, HelpType.SlotDeclarationSyntax);
            }

            using ScopedList<ExpressionIndex<Parameter>> buffer = scopedAllocator.CreateListScope<ExpressionIndex<Parameter>>(4);

            if (tokenStream.Peek(SymbolType.OpenParen)) {
                if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.SlotDeclarationSyntax, out TokenStream blockStream)) {
                    return false;
                }

                PushRecoveryPoint(blockStream.start, blockStream.end + 1);

                while (ParseFormalLambdaParameter(ref blockStream, out ExpressionIndex<Parameter> expressionIndex, false)) {
                    buffer.Add(expressionIndex);
                    blockStream.Consume(SymbolType.Comma);
                }

                if (PopRecoveryPoint(ref blockStream) == RecoveryPoint.k_HasErrors) {
                    return false;
                }
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSlotSignature() {
                parameters = expressionBuffer.AddExpressionList(buffer),
                identifierLocation = identifierLocation,
                slotType = slotType,
            });

            return true;
        }

        private bool ParseElementArgument(ref TokenStream tokenStream, bool isTemplateFn, out NodeIndex argumentIndex) {
            // : type_path identifier (= non_assignment_expression)?
            // | ATTR: dashed_identifier (= non_assignment_expression)?
            // | STYLE = '[' style_id (, style_id)* ']'
            // | STYLE ':' style_property_name = style_expression
            // | ONCHANGE ':' identifier = (block | non_assignment_expression)
            // | SYNC ':' identifier = non_assignment_expression
            // | (BEFORE | AFTER) ':' lifecycle_event => (block | non_assignment_expression) 
            // | input_event

            const LifeCycleEventType k_DisableDestroyEventTypes = LifeCycleEventType.OnBeforeDisable | LifeCycleEventType.OnAfterDisable | LifeCycleEventType.OnBeforeDestroy | LifeCycleEventType.OnAfterDisable;
            TemplateKeyword keyword = tokenStream.Current.Keyword;
            argumentIndex = default;

            if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Colon)) {
                switch (keyword) {
                    case TemplateKeyword.Sync: {
                        if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                            PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                            ParsePropertyBinding(ref nextStream, false, out argumentIndex);
                            PopRecoveryPoint(ref nextStream);
                            return true;
                        }

                        return HardError(tokenStream, DiagnosticError.ExpectedSyncBinding, HelpType.SyncSyntax);
                    }

                    case TemplateKeyword.OnChange: {
                        if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                            PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                            ParseOnChangeBinding(ref nextStream, out argumentIndex);
                            PopRecoveryPoint(ref nextStream);
                            return true;
                        }

                        return HardError(tokenStream, DiagnosticError.ExpectedOnChangeBinding, HelpType.OnChangeSyntax);
                    }

                    case TemplateKeyword.Before: {
                        if (isTemplateFn) {
                            return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportLifeCycleEvents, HelpType.RenderFnSyntax);
                        }

                        if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                            PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                            ParseBeforeLifeCycleHandler(ref nextStream, out argumentIndex);
                            ValidateLifeCycleHandler(argumentIndex, k_DisableDestroyEventTypes, nextStream);
                            PopRecoveryPoint(ref nextStream);
                            return true;
                        }

                        // hard error, but may have been thrown already
                        return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                    }

                    case TemplateKeyword.After: {
                        if (isTemplateFn) {
                            return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportLifeCycleEvents, HelpType.RenderFnSyntax);
                        }

                        if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                            PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                            ParseAfterLifeCycleHandler(ref nextStream, out argumentIndex);
                            ValidateLifeCycleHandler(argumentIndex, k_DisableDestroyEventTypes, nextStream);
                            PopRecoveryPoint(ref nextStream);
                            return true;
                        }

                        // hard error, but may have been thrown already
                        return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                    }

                    case TemplateKeyword.Attr: {
                        if (isTemplateFn) {
                            return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportAttributes, HelpType.RenderFnSyntax);
                        }

                        if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                            PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                            ParseAttribute(ref nextStream, false, out argumentIndex);
                            PopRecoveryPoint(ref nextStream);
                            return true;
                        }

                        // hard error, but may have been thrown already
                        return HardError(tokenStream, DiagnosticError.ExpectedAttributeDefinition, HelpType.AttributeSyntax);
                    }

                    case TemplateKeyword.Style: {
                        if (isTemplateFn) {
                            return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportStyles, HelpType.RenderFnSyntax);
                        }

                        return ParseElementStyle(ref tokenStream, out argumentIndex) || HardError(tokenStream, DiagnosticError.ExpectedStyleDefinition);
                    }

                    default: {
                        if (TryParseInputHandler(ref tokenStream, false, out argumentIndex)) {
                            if (isTemplateFn) {
                                return HardError(tokenStream.location, DiagnosticError.TemplateFunctionsDoNotSupportInputHandlers, HelpType.RenderFnSyntax);
                            }

                            return true;
                        }

                        if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Colon)) {
                            return HardError(tokenStream, DiagnosticError.UnknownPropertyPrefix, HelpType.PropertyPrefixNames);
                        }

                        // if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        //     PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        //     ParsePropertyBinding(ref nextStream, out argumentIndex);
                        //     PopRecoveryPoint(ref nextStream);
                        //     return true;
                        // }

                        // hard error, but may have been thrown already
                        return HardError(tokenStream, DiagnosticError.ExpectedPropertyBinding, HelpType.PropertyBindingSyntax);
                    }
                }
            }
            else {
                if (keyword == TemplateKeyword.Const) {
                    tokenStream.location++;
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream argStream)) {
                        PushRecoveryPoint(argStream.start, argStream.end + 1);
                        ParseConstElementArgument(ref argStream, out argumentIndex);
                        PopRecoveryPoint(ref argStream);
                        return true;
                    }

                    return HardError(tokenStream, DiagnosticError.ExpectedAPropertyBinding_InstanceStyle_OrAttribute, HelpType.ConstBindingSyntax);
                }

                if (keyword == TemplateKeyword.Style) {
                    return ParseElementStyle(ref tokenStream, out argumentIndex) || HardError(tokenStream, DiagnosticError.ExpectedStyleDefinition);
                }

                if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                    PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                    ParsePropertyBinding(ref nextStream, false, out argumentIndex);
                    PopRecoveryPoint(ref nextStream);
                    return true;
                }

                argumentIndex = default;
                // hard error, but may have been thrown already
                return HardError(tokenStream, DiagnosticError.ExpectedPropertyBinding, HelpType.PropertyBindingSyntax);
            }
        }

        private bool ParseConstElementArgument(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            // attr:
            // style:
            // parameter usage 

            if (tokenStream.Current.Keyword == TemplateKeyword.Attr) {
                if (ParseAttribute(ref tokenStream, true, out argumentIndex)) {
                    return true;
                }
            }

            if (tokenStream.Current.Keyword == TemplateKeyword.Style && tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Colon)) {
                tokenStream.location++;
                tokenStream.location++;
                if (ParseInstanceStyle(ref tokenStream, true, out NodeIndex<InstanceStyleNode> instanceNode)) {
                    argumentIndex = instanceNode;
                    return true;
                }
            }

            return ParsePropertyBinding(ref tokenStream, true, out argumentIndex);
        }

        private void ValidateLifeCycleHandler(NodeIndex argumentIndex, LifeCycleEventType k_DisableDestroyEventTypes, TokenStream nextStream) {
            if (argumentIndex.IsValid) {
                LifeCycleEventNode lifeCycle = templateBuffer.nodes.Get(argumentIndex.id).As<LifeCycleEventNode>();

                if ((lifeCycle.eventType & k_DisableDestroyEventTypes) != 0) {
                    SoftError(nextStream.start, ErrorLocationAdjustment.None, DiagnosticError.DisableAndDestroyHandlersAreOnlyAllowedInTopLevelDeclarations);
                }
            }
        }

        private bool TryParseInputHandler(ref TokenStream tokenStream, bool isTopLevel, out NodeIndex argumentIndex) {
            argumentIndex = default;
            switch (tokenStream.Current.Keyword) {
                case TemplateKeyword.Focus: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseFocusInputHandler(ref nextStream, out argumentIndex, isTopLevel);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedFocusEventHandler, HelpType.FocusEventSyntax);
                }

                case TemplateKeyword.Key: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseKeyInputHandler(ref nextStream, out argumentIndex, isTopLevel);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedKeyEventHandler, HelpType.KeyEventSyntax);
                }

                case TemplateKeyword.Text: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseTextInputHandler(ref nextStream, out argumentIndex, isTopLevel);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedTextEventHandler, HelpType.TextEventSyntax);
                }

                case TemplateKeyword.Drag: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseDragInputHandler(ref nextStream, out argumentIndex, isTopLevel);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedDragEventHandler, HelpType.DragEventSyntax);
                }

                case TemplateKeyword.Mouse: {
                    if (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                        PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                        ParseMouseInputHandler(ref nextStream, out argumentIndex, isTopLevel);
                        PopRecoveryPoint(ref nextStream);
                        return true;
                    }

                    // hard error, but may have been thrown already
                    return HardError(tokenStream, DiagnosticError.ExpectedMouseEventHandler, HelpType.MouseEventSyntax);
                }
            }

            return false;
        }

        private bool ParseComputedProperty(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            // : 'computed' visibility_modifier? type_path identifier '=>' expression
            int startLocation = tokenStream.location;
            argumentIndex = default;

            // todo -- remove extrude when plugin updates 
            if (!tokenStream.Consume(TemplateKeyword.Computed)) {
                return false;
            }

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.TemplateComputedPropertySyntax);
            }

            ParseVisibilityModifier(ref tokenStream, out bool isPublic, HelpType.TemplateComputedPropertySyntax);

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.TemplateComputedPropertySyntax);
            }

            if (!tokenStream.Consume(SymbolType.FatArrow)) {
                return HardError(tokenStream, DiagnosticError.ExpectedFatArrow, HelpType.TemplateComputedPropertySyntax);
            }

            if (!ParseNonAssignment(ref tokenStream, out ExpressionIndex expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.TemplateComputedPropertySyntax);
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new ComputedProperty() {
                alias = identifier,
                typePath = typePath,
                isPublic = isPublic,
                expression = expressionIndex
            });

            return true;
        }

        private bool ParseTemplateParameter(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            // : ('required' | 'optional') (type_path | 'var') identifier ('using' identifier)? ('=' non_assignment_expression)?

            int startLocation = tokenStream.location;
            argumentIndex = default;

            bool isRequired = tokenStream.Consume(TemplateKeyword.Required);

            if (!isRequired) {
                tokenStream.Consume(TemplateKeyword.Optional);
            }

            bool isPublic = true;

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (tokenStream.Consume(TemplateKeyword.Public)) {
                    isPublic = true;
                }
                else if (tokenStream.Consume(TemplateKeyword.Private)) {
                    isPublic = false;
                }
                else {
                    return HardError(tokenStream, DiagnosticError.ExpectedPublicOrPrivateAccessModifier, HelpType.TemplateParamSyntax);
                }
            }

            ExpressionIndex<TypePath> typePath = default;
            if (tokenStream.Consume(TemplateKeyword.Var)) {
                typePath = default;
            }
            else if (!ParseTypePath(ref tokenStream, out typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.TemplateParamSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.TemplateParamSyntax);
            }

            NodeIndex<FromMapping> fromMapping = default;

            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.TemplateParamSyntax);
                }
            }

            if (tokenStream.Consume(SymbolType.Assign)) {
                if (!ParseNonAssignment(ref tokenStream, out ExpressionIndex defaultValue)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.TemplateParamSyntax);
                }

                argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateParameter() {
                    isPublic = isPublic,
                    identifier = identifier,
                    typePath = typePath,
                    fromMapping = fromMapping,
                    defaultValue = defaultValue,
                    isRequired = isRequired
                });
                return true;
            }

            if (!typePath.IsValid && !fromMapping.IsValid) {
                return HardError(tokenStream, DiagnosticError.WhenUsingVarAsATypePathYouMustProvideADefaultValueOrMappingExpression, HelpType.TemplateParamSyntax);
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateParameter() {
                isPublic = isPublic,
                identifier = identifier,
                typePath = typePath,
                fromMapping = fromMapping,
                defaultValue = default,
                isRequired = isRequired
            });
            return true;
        }

        private bool ParseFromMapping(ref TokenStream tokenStream, out NodeIndex<FromMapping> mapping) {
            // : any_identifier ('.' standard_identifier)*

            mapping = default;

            int start = tokenStream.location;

            if (!tokenStream.ConsumeAnyIdentifier(out NonTrivialTokenLocation expressionStart)) {
                return false;
            }

            if (!tokenStream.Peek(SymbolType.Dot)) {
                mapping = templateBuffer.Add(start, tokenStream.location, new FromMapping() {
                    start = expressionStart
                });
                return true;
            }

            using ScopedList<ExpressionIndex<Identifier>> chainList = scopedAllocator.CreateListScope<ExpressionIndex<Identifier>>(8);

            int tokenStart = tokenStream.location;

            while (tokenStream.Consume(SymbolType.Dot)) {
                if (tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                    chainList.Add(expressionBuffer.Add(tokenStart, tokenStream.location, new Identifier() {
                        identifierType = IdentifierType.NormalIdentifier,
                        identifierTokenIndex = identifierLocation
                    }));
                }

                else {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.FromMappingSyntax);
                }

                tokenStart = tokenStream.location;
            }

            mapping = templateBuffer.Add(start, tokenStream.location, new FromMapping() {
                start = expressionStart,
                chain = expressionBuffer.AddExpressionList(chainList)
            });

            return true;
        }

        private bool ParseOnChangeBinding(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            // 'onChange' ':' identifier '=' expression
            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.OnChange)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.OnChangeSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.OnChangeSyntax);
            }

            if (!tokenStream.Consume(SymbolType.FatArrow)) {
                return HardError(tokenStream, DiagnosticError.ExpectedFatArrow, HelpType.OnChangeSyntax);
            }

            ExpressionIndex expressionIndex = default;

            if (tokenStream.Current.Symbol == SymbolType.CurlyBraceOpen) {
                if (!ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.OnChangeSyntax);
                }

                expressionIndex = blockExpression;
            }
            else {
                if (!ParseExpression(ref tokenStream, out expressionIndex)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.OnChangeSyntax);
                }
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new OnChangeBindingNode() {
                identifier = identifier,
                expression = expressionIndex
            });

            return true;
        }

        private bool ParsePropertyBinding(ref TokenStream tokenStream, bool isConst, out NodeIndex argumentIndex) {
            // : ('sync' ':')? identifier '=' expression
            // | ('sync' ':')? expression
            // decorator_arg: sync? (identifier '=')? non_assignment_expression

            argumentIndex = default;
            TemplateArgumentModifier modifier = TemplateArgumentModifier.None;

            int startLocation = tokenStream.location;

            if (!isConst && tokenStream.Consume(TemplateKeyword.Sync) && tokenStream.Consume(SymbolType.Colon)) {
                modifier = TemplateArgumentModifier.Sync;
            }
            else {
                tokenStream.location = startLocation;
            }

            if (isConst) {
                modifier = TemplateArgumentModifier.Const;
            }

            NonTrivialTokenLocation identifier = default;
            if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Assign)) {
                tokenStream.ConsumeStandardIdentifier(out identifier);
                tokenStream.location++;
            }

            if (!ParseNonAssignment(ref tokenStream, out ExpressionIndex expression)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new PropertyBindingNode() {
                expression = expression,
                modifier = modifier,
                parameterName = identifier,
            });

            return true;
        }

        private bool ParseBeforeLifeCycleHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            return ParseLifeCycleHandler(ref tokenStream, true, out argumentIndex);
        }

        private bool ParseAfterLifeCycleHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            return ParseLifeCycleHandler(ref tokenStream, false, out argumentIndex);
        }

        private bool ParseLifeCycleHandler(ref TokenStream tokenStream, bool before, out NodeIndex argumentIndex, bool requireTerminatingSemiColon = false) {
            // ('before' | 'after') ':' ('create' | 'enable' | 'update' | 'finish' | 'disable' | 'destroy' | 'input' | 'earlyInput' | 'lateInput')
            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (before) {
                if (!tokenStream.Consume(TemplateKeyword.Before)) {
                    return false;
                }
            }
            else {
                if (!tokenStream.Consume(TemplateKeyword.After)) {
                    return false;
                }
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.LifeCycleSyntax);
            }

            if (!ParseLifeCycleEventName(ref tokenStream, before, out LifeCycleEventType lifeCycleEventType)) {
                return false; // error already thrown 
            }

            ExpressionIndex expressionIndex = default;
            NodeIndex<FromMapping> fromMapping = default;

            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return false;
                }

                if (requireTerminatingSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon);
                }
            }
            else {
                if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                    // makes the => optional if there is a block 
                }
                else if (!tokenStream.Consume(SymbolType.FatArrow)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedFatArrow, HelpType.LifeCycleSyntax);
                }

                // technically we don't want a block, just a statement list
                // emitted code is slightly worse because of this, but we can address that later, nbd 
                if (ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression)) {
                    expressionIndex = blockExpression;
                }
                else if (ParseExpression(ref tokenStream, out expressionIndex)) {
                    if (requireTerminatingSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon);
                    }
                }
                else {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.LifeCycleSyntax);
                }
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new LifeCycleEventNode() {
                expression = expressionIndex,
                eventType = lifeCycleEventType,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseLifeCycleEventName(ref TokenStream tokenStream, bool isBefore, out LifeCycleEventType lifeCycleEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);

            switch (token.Keyword) {
                case TemplateKeyword.Create: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeCreate : LifeCycleEventType.OnAfterCreate;
                    return true;
                }

                case TemplateKeyword.Enable: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeEnable : LifeCycleEventType.OnAfterEnable;
                    return true;
                }

                case TemplateKeyword.Update: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeUpdate : LifeCycleEventType.OnAfterUpdate;
                    return true;
                }

                case TemplateKeyword.Finish: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeFinish : LifeCycleEventType.OnAfterFinish;
                    return true;
                }

                case TemplateKeyword.Disable: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeDisable : LifeCycleEventType.OnAfterDisable;
                    return true;
                }

                case TemplateKeyword.Destroy: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeDestroy : LifeCycleEventType.OnAfterDestroy;
                    return true;
                }

                case TemplateKeyword.EarlyInput: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeEarlyInput : LifeCycleEventType.OnAfterEarlyInput;
                    return true;
                }

                case TemplateKeyword.Input: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeInput : LifeCycleEventType.OnAfterInput;
                    return true;
                }

                case TemplateKeyword.LateInput: {
                    lifeCycleEventType = isBefore ? LifeCycleEventType.OnBeforeLateInput : LifeCycleEventType.OnAfterLateInput;
                    return true;
                }

                default: {
                    lifeCycleEventType = default;
                    return HardError(tokenStream.location - 1, DiagnosticError.InvalidLifeCycleEvent, HelpType.LifeCycleEventName);
                }
            }
        }

        private bool ParseTextInputHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex, bool isTopLevel, bool requireSemiColon = false) {
            // : 'text' ':' 'input' event_modifier_list?  (from_mapping | ('=>' (expression | block))

            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Text)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.TextEventSyntax);
            }

            if (!ParseTextEventName(ref tokenStream, out InputEventType inputEventType)) {
                return false; // error already thrown 
            }

            if (!ParseInputEventModifierList(ref tokenStream, out InputModifiers inputModifiers)) {
                return false; // error already thrown 
            }

            inputModifiers.requireFocus = false; // fix stupid settings without remaking the parser for it

            if (!ParseInputHandlerBody(ref tokenStream, isTopLevel, requireSemiColon, HelpType.TextEventSyntax, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new InputHandlerNode() {
                inputEventType = inputEventType,
                inputModifiers = inputModifiers,
                expression = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseFocusInputHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex, bool isTopLevel, bool requireSemiColon = false) {
            // : 'focus' ':' focus_event event_modifier_list?  (from_mapping | ('=>' (expression | block))
            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Focus)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.FocusEventSyntax);
            }

            if (!ParseFocusEventName(ref tokenStream, out InputEventType inputEventType)) {
                return false; // error already thrown 
            }

            if (!ParseInputEventModifierList(ref tokenStream, out InputModifiers inputModifiers)) {
                return false; // error already thrown 
            }

            inputModifiers.requireFocus = false; // fix stupid settings without remaking the parser for it

            if (!ParseInputHandlerBody(ref tokenStream, isTopLevel, requireSemiColon, HelpType.FocusEventSyntax, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new InputHandlerNode() {
                inputEventType = inputEventType,
                inputModifiers = inputModifiers,
                expression = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseKeyInputHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex, bool isTopLevel, bool requireSemiColon = false) {
            // : 'key' ':' key_event event_modifier_list? (from_mapping | ('=>' (expression | block))
            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Key)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.KeyEventSyntax);
            }

            if (!ParseKeyEventName(ref tokenStream, out InputEventType inputEventType)) {
                return false; // error already thrown 
            }

            if (!ParseInputEventModifierList(ref tokenStream, out InputModifiers inputModifiers)) {
                return false; // error already thrown 
            }

            if (!ParseInputHandlerBody(ref tokenStream, isTopLevel, requireSemiColon, HelpType.KeyEventSyntax, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new InputHandlerNode() {
                inputEventType = inputEventType,
                inputModifiers = inputModifiers,
                expression = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseDragEventType(ref TokenStream tokenStream, out ExpressionIndex<TypePath> typePath) {
            typePath = default;

            if (!tokenStream.Peek(SymbolType.LessThan)) {
                return true;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.AngleBrackets, HelpType.DragEventSyntax, out TokenStream typeStream)) {
                return false;
            }

            if (!ParseTypePath(ref typeStream, out typePath)) {
                return HardError(typeStream, DiagnosticError.ExpectedTypePath, HelpType.DragEventSyntax);
            }

            if (typeStream.HasMoreTokens) {
                return HardError(typeStream, DiagnosticError.UnexpectedToken);
            }

            return true;
        }

        private bool ParseDragInputHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex, bool isTopLevel, bool requireSemiColon = false) {
            // : 'drag' ':' drag_event drag_event_type? event_modifier_list? (from_mapping | ('=>' (expression | block))
            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Drag)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.DragEventSyntax);
            }

            if (!ParseDragEventName(ref tokenStream, out InputEventType inputEventType)) {
                return false; // error already thrown 
            }

            if (!ParseDragEventType(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                return false;
            }

            if (!ParseInputEventModifierList(ref tokenStream, out InputModifiers inputModifiers)) {
                return false; // error already thrown 
            }

            if (!ParseInputHandlerBody(ref tokenStream, isTopLevel, requireSemiColon, HelpType.DragEventSyntax, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new InputHandlerNode() {
                inputEventType = inputEventType,
                inputModifiers = inputModifiers,
                expression = expressionIndex,
                fromMapping = fromMapping,
                dragEventType = typePath,
            });

            return true;
        }

        private bool ParseInputHandlerBody(ref TokenStream tokenStream, bool isTopLevel, bool requireSemiColon, HelpType helpType, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex) {
            fromMapping = default;
            expressionIndex = default;

            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!isTopLevel) {
                    return HardError(tokenStream, DiagnosticError.FromMappingIsOnlyAllowedInTopLevelDeclarations);
                }

                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return false;
                }

                if (requireSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon);
                }
            }
            else {
                if (!tokenStream.Consume(SymbolType.FatArrow)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedFatArrowOrFromMapping, helpType);
                }

                if (tokenStream.Current.Symbol == SymbolType.CurlyBraceOpen) {
                    if (!ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, helpType);
                    }

                    expressionIndex = blockExpression;
                }
                else {
                    if (!ParseExpression(ref tokenStream, out expressionIndex)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedExpression, helpType);
                    }

                    if (requireSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon, helpType);
                    }
                }
            }

            return true;
        }

        private bool ParseMouseInputHandler(ref TokenStream tokenStream, out NodeIndex argumentIndex, bool isTopLevel, bool requireSemiColon = false) {
            // : 'mouse' ':' mouse_event event_modifier_list? (from_mapping | '=>' expression)

            int startLocation = tokenStream.location;
            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Mouse)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.MouseEventSyntax);
            }

            if (!ParseMouseEventName(ref tokenStream, out InputEventType inputEventType)) {
                return false; // error already thrown 
            }

            if (!ParseInputEventModifierList(ref tokenStream, out InputModifiers inputModifiers)) {
                return false; // error already thrown 
            }

            if (!ParseInputHandlerBody(ref tokenStream, isTopLevel, requireSemiColon, HelpType.MouseEventSyntax, out NodeIndex<FromMapping> fromMapping, out ExpressionIndex expressionIndex)) {
                return false;
            }

            argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new InputHandlerNode() {
                inputEventType = inputEventType,
                inputModifiers = inputModifiers,
                expression = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseInputEventModifierList(ref TokenStream tokenStream, out InputModifiers inputModifiers) {
            inputModifiers = default;
            inputModifiers.eventPhase = InputPhase.AfterChildren;

            // event_modifier_list : ('.' ('focus' | 'late' | 'shift' | 'ctrl' | 'cmd' | 'alt') )*
            while (tokenStream.Consume(SymbolType.Dot)) {
                if (tokenStream.ConsumeKeywordOrIdentifier(out Token token)) {
                    switch (token.Keyword) {
                        case TemplateKeyword.Focus:
                            inputModifiers.requireFocus = true;
                            break;

                        case TemplateKeyword.Late:
                            inputModifiers.eventPhase = InputPhase.AfterChildren;
                            break;

                        case TemplateKeyword.Early:
                            inputModifiers.eventPhase = InputPhase.BeforeUpdate;
                            break;

                        case TemplateKeyword.Shift:
                            inputModifiers.keyboardModifiers |= KeyboardModifiers.Shift;
                            break;

                        case TemplateKeyword.Ctrl:
                            inputModifiers.keyboardModifiers |= KeyboardModifiers.Control;
                            break;

                        case TemplateKeyword.Cmd:
                            inputModifiers.keyboardModifiers |= KeyboardModifiers.Command;
                            break;

                        case TemplateKeyword.Alt:
                            inputModifiers.keyboardModifiers |= KeyboardModifiers.Alt;
                            break;

                        default:
                            return HardError(tokenStream, DiagnosticError.ExpectedInputModifier, HelpType.InputModifierList);
                    }
                }
                else {
                    // point at the dot 
                    return HardError(tokenStream.location - 1, DiagnosticError.DanglingDot);
                }
            }

            return true;
        }

        private bool ParseTextEventName(ref TokenStream tokenStream, out InputEventType inputEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);
            switch (token.Keyword) {
                case TemplateKeyword.Input: {
                    inputEventType = InputEventType.TextInput;
                    return true;
                }
            }

            inputEventType = default;
            return HardError(tokenStream, DiagnosticError.InvalidTextEventName, HelpType.TextEventNames);
        }

        private bool ParseFocusEventName(ref TokenStream tokenStream, out InputEventType inputEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);

            switch (token.Keyword) {
                case TemplateKeyword.Gained: {
                    inputEventType = InputEventType.FocusGain;
                    return true;
                }

                case TemplateKeyword.Lost: {
                    inputEventType = InputEventType.FocusLost;
                    return true;
                }

                default: {
                    inputEventType = default;
                    return HardError(tokenStream, DiagnosticError.InvalidFocusEventName, HelpType.FocusEventNames);
                }
            }
        }

        private bool ParseKeyEventName(ref TokenStream tokenStream, out InputEventType inputEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);

            switch (token.Keyword) {
                case TemplateKeyword.Down: {
                    inputEventType = InputEventType.KeyDown;
                    return true;
                }

                case TemplateKeyword.HeldDown: {
                    inputEventType = InputEventType.KeyHeldDown;
                    return true;
                }

                case TemplateKeyword.Up: {
                    inputEventType = InputEventType.KeyUp;
                    return true;
                }

                default: {
                    inputEventType = default;
                    return HardError(tokenStream, DiagnosticError.InvalidKeyInputEvent, HelpType.KeyEventNames);
                }
            }
        }

        private bool ParseDragEventName(ref TokenStream tokenStream, out InputEventType inputEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);

            switch (token.Keyword) {
                case TemplateKeyword.Create: {
                    inputEventType = InputEventType.DragCreate;
                    return true;
                }

                case TemplateKeyword.Move: {
                    inputEventType = InputEventType.DragMove;
                    return true;
                }

                case TemplateKeyword.Update: {
                    inputEventType = InputEventType.DragUpdate;
                    return true;
                }

                case TemplateKeyword.Enter: {
                    inputEventType = InputEventType.DragEnter;
                    return true;
                }

                case TemplateKeyword.Exit: {
                    inputEventType = InputEventType.DragExit;
                    return true;
                }

                case TemplateKeyword.Drop: {
                    inputEventType = InputEventType.DragDrop;
                    return true;
                }

                case TemplateKeyword.Cancel: {
                    inputEventType = InputEventType.DragCancel;
                    return true;
                }

                default: {
                    inputEventType = default;
                    return HardError(tokenStream, DiagnosticError.InvalidDragInputEvent, HelpType.DragEventNames);
                }
            }
        }

        private bool ParseMouseEventName(ref TokenStream tokenStream, out InputEventType inputEventType) {
            tokenStream.ConsumeAnyIdentifier(out Token token);

            switch (token.Keyword) {
                case TemplateKeyword.Down: {
                    inputEventType = InputEventType.MouseDown;
                    return true;
                }

                case TemplateKeyword.HeldDown: {
                    inputEventType = InputEventType.MouseHeldDown;
                    return true;
                }

                case TemplateKeyword.Hover: {
                    inputEventType = InputEventType.MouseHover;
                    return true;
                }

                case TemplateKeyword.Up: {
                    inputEventType = InputEventType.MouseUp;
                    return true;
                }

                case TemplateKeyword.Update: {
                    inputEventType = InputEventType.MouseUpdate;
                    return true;
                }

                case TemplateKeyword.Enter: {
                    inputEventType = InputEventType.MouseEnter;
                    return true;
                }

                case TemplateKeyword.Exit: {
                    inputEventType = InputEventType.MouseExit;
                    return true;
                }

                case TemplateKeyword.Move: {
                    inputEventType = InputEventType.MouseMove;
                    return true;
                }

                case TemplateKeyword.Click: {
                    inputEventType = InputEventType.MouseClick;
                    return true;
                }

                case TemplateKeyword.Context: {
                    inputEventType = InputEventType.MouseContext;
                    return true;
                }

                case TemplateKeyword.Scroll: {
                    inputEventType = InputEventType.MouseScroll;
                    return true;
                }

                default: {
                    inputEventType = default;
                    return HardError(tokenStream.location - 1, DiagnosticError.InvalidMouseInputEvent, HelpType.MouseEventNames);
                }
            }
        }

        private bool ParseParentArgumentExtrusionsWithRecovery(ref TokenStream tokenStream, out NodeRange<Extrusion> nodeRange) {
            // : '(' extrusion_list ')'

            nodeRange = default;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.SlotOverrideSyntax, out TokenStream argStream)) {
                return false;
            }

            if (argStream.IsEmpty) {
                return true;
            }

            PushRecoveryPoint(argStream.start, tokenStream.location);
            ParseExtrusionList(ref argStream, out nodeRange);
            return !PopRecoveryPoint(ref argStream);
        }

        private bool ParseExtrusionsWithRecovery(ref TokenStream tokenStream, out NodeRange<Extrusion> nodeRange) {
            // : '[' extrusion_list ']'

            nodeRange = default;

            if (!tokenStream.Peek(SymbolType.SquareBraceOpen)) {
                return false;
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.SquareBrackets, out TokenStream extrusionStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedSquareBracket, HelpType.ExtrusionSyntax);
            }

            if (extrusionStream.IsEmpty) {
                return true;
            }

            PushRecoveryPoint(extrusionStream.start, tokenStream.location);
            ParseExtrusionList(ref extrusionStream, out nodeRange);
            return !PopRecoveryPoint(ref extrusionStream);
        }

        private bool ParseStyleList(ref TokenStream tokenStream, out ExpressionRange<ResolveIdExpression> styleIdRange) {
            // style_list : (style_id (, style_id)*)?

            styleIdRange = default;

            if (!tokenStream.Peek(SymbolType.At)) {
                return false;
            }

            using ScopedList<ExpressionIndex<ResolveIdExpression>> list = scopedAllocator.CreateListScope<ExpressionIndex<ResolveIdExpression>>(8);

            while (ParseResolveId(ref tokenStream, out ExpressionIndex<ResolveIdExpression> styleIdExpression)) {
                list.Add(styleIdExpression);
                if (!tokenStream.Consume(SymbolType.Comma)) {
                    break;
                }
            }

            styleIdRange = expressionBuffer.AddExpressionList(list);
            return true;
        }

        private bool ParseResolveId(ref TokenStream tokenStream, out ExpressionIndex<ResolveIdExpression> expressionIndex) {
            // : '@' dashed_identifier ('::' dashed_identifier)?

            expressionIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(SymbolType.At)) {
                return false;
            }

            if (!ParseDashedIdentifier(ref tokenStream, out NonTrivialTokenRange moduleOrTagName)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifierFollowingAtSymbol);
            }

            if (tokenStream.Consume(SymbolType.DoubleColon)) {
                if (!ParseDashedIdentifier(ref tokenStream, out NonTrivialTokenRange styleName)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifierFollowingAtSymbol);
                }

                expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new ResolveIdExpression() {
                    moduleName = moduleOrTagName,
                    tagName = styleName
                });
                return true;
            }

            expressionIndex = expressionBuffer.Add(startLocation, tokenStream.location, new ResolveIdExpression() {
                moduleName = default,
                tagName = moduleOrTagName
            });
            return true;
        }

        private bool ParseElementStyle(ref TokenStream tokenStream, out NodeIndex argumentIndex) {
            // : STYLE = '[' resolve_id (, resolve_id)* ']'
            // | STYLE ':' style_property_name = style_expression
            argumentIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Peek(TemplateKeyword.Style)) {
                return false;
            }

            tokenStream.location++;

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!ParseInstanceStyle(ref tokenStream, false, out NodeIndex<InstanceStyleNode> instanceNode)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedInstanceStyle);
                }

                argumentIndex = instanceNode;
                return true;
            }

            if (tokenStream.Consume(SymbolType.Assign)) {
                ParseStyleListWithRecovery(ref tokenStream, out ExpressionRange<ResolveIdExpression> styleList);
                argumentIndex = templateBuffer.Add(startLocation, tokenStream.location, new StyleListNode() {
                    styleIds = styleList
                });
                return true;
            }

            return HardError(tokenStream, DiagnosticError.ExpectedColonOrEqualSignAfterStyleArgument);
        }

        private bool ParseInstanceStyle(ref TokenStream tokenStream, bool isConst, out NodeIndex<InstanceStyleNode> instanceNode) {
            // 'const'? style_property_name `=` csExpression

            int startLocation = tokenStream.location;

            if (!tokenStream.ConsumeAnyIdentifier(out NonTrivialTokenLocation stylePropertyName)) {
                instanceNode = default;
                return HardError(tokenStream, DiagnosticError.ExpectedStylePropertyName);
            }

            // todo -- insert modifer check here (create | enable | update)

            if (!tokenStream.Consume(SymbolType.Assign)) {
                instanceNode = default;
                return HardError(tokenStream, DiagnosticError.ExpectedEqualSign);
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                instanceNode = default;
                return HardError(tokenStream, DiagnosticError.ExpectedExpression);
            }

            instanceNode = templateBuffer.Add(startLocation, tokenStream.location, new InstanceStyleNode() {
                valueExpression = expressionIndex,
                stylePropertyName = stylePropertyName,
                isConst = isConst
            });

            return true;
        }

        private void ParseStyleListWithRecovery(ref TokenStream tokenStream, out ExpressionRange<ResolveIdExpression> expressionRange) {
            // '[' style_id_list ']'
            expressionRange = default;

            if (!tokenStream.Peek(SymbolType.SquareBraceOpen)) {
                HardError(tokenStream, DiagnosticError.StyleListMustBeWrappedInSquareBrackets);
                return;
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.SquareBrackets, out TokenStream styleList)) {
                HardError(tokenStream, DiagnosticError.UnmatchedSquareBracket);
                return;
            }

            if (styleList.IsEmpty) return;

            PushRecoveryPoint(styleList.start - 1, tokenStream.location);

            ParseStyleList(ref styleList, out expressionRange);

            PopRecoveryPoint(ref styleList);
        }

        private bool ParseAttribute(ref TokenStream tokenStream, bool isConst, out NodeIndex argumentIndex) {
            // : ATTR ':' dashed_identifier (= csExpression)?

            ExpressionParseState state = SaveState(tokenStream);

            argumentIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Attr)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.RequireColonAfterAttributeName);
            }

            if (!ParseDashedIdentifier(ref tokenStream, out NonTrivialTokenRange key)) {
                return HardError(tokenStream, DiagnosticError.ExpectedDashedIdentifier, HelpType.AttributeNameCanBeDashed);
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                argumentIndex = templateBuffer.Add(state.location, tokenStream.end, new AttributeAssignment() {
                    key = key,
                });
                return true;
            }

            if (!ParseExpression(ref tokenStream, out ExpressionIndex expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.AttributesRequireAValueWhenEqualSignProvided);
            }

            argumentIndex = templateBuffer.Add(state.location, tokenStream.end, new AttributeAssignment() {
                key = key,
                value = expressionIndex,
                isConst = isConst
            });

            return true;
        }

        private bool ParseElementNode(ref TokenStream tokenStream, out NodeIndex<ElementNode> nodeIndex) {
            // element_node : decorator* module_prefix? tag_name type_argumentList? '(' element_signature ')'

            int startLocation = tokenStream.location;

            ParseDecorators(ref tokenStream, out NodeRange<DecoratorNode> decoratorList);

            if (!ParseQualifiedIdentifier(ref tokenStream, out QualifiedIdentifier tagName)) {
                nodeIndex = default;
                return decoratorList.IsValid && HardError(tokenStream, DiagnosticError.ExpectedElementTag);
            }

            TypeArgumentSnippet typeList = new TypeArgumentSnippet(HelpType.ElementTagSyntax, true);
            ParseAngleBracketListWithRecovery(ref tokenStream, ref typeList, out ExpressionRange<TypePath> typeArguments);

            if (!ParseElementArgumentsWithRecovery(ref tokenStream, false, out NodeRange arguments)) {
                nodeIndex = default;
                return false;
            }

            ParseExtrusionsWithRecovery(ref tokenStream, out NodeRange<Extrusion> extrusions);

            // how do i want to represent the body block? It's scoped this element but I don't want the token range for the whole block when printing the element 
            // probably need 2 ranges, one for the 'header' and one for the 'body'

            if (!ParseElementBody(ref tokenStream, out NodeIndex<TemplateBlockNode> body)) {
                nodeIndex = default;
                return false;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new ElementNode() {
                bindings = arguments,
                typeArguments = typeArguments,
                decorators = decoratorList,
                extrusions = extrusions,
                childBlock = body,
                qualifiedName = tagName,
            });

            return true;
        }

        private bool ParseTypedQualifiedIdentifierNode(ref TokenStream tokenStream, out NodeIndex<TypedQualifiedIdentifierNode> identifier) {
            int startLocation = tokenStream.location;

            NonTrivialTokenLocation firstLocation = new NonTrivialTokenLocation(startLocation);

            if (!tokenStream.ConsumeKeywordOrIdentifier()) {
                identifier = default;
                return false;
            }

            if (tokenStream.Consume(SymbolType.DoubleColon)) {
                NonTrivialTokenLocation moduleName = firstLocation;
                NonTrivialTokenLocation tagName = new NonTrivialTokenLocation(tokenStream.location);

                if (!tokenStream.ConsumeKeywordOrIdentifier()) {
                    identifier = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.QualifiedIdentifierSyntax);
                }

                TypeArgumentSnippet typeList = new TypeArgumentSnippet(HelpType.QualifiedIdentifierSyntax, true);
                ParseAngleBracketListWithRecovery(ref tokenStream, ref typeList, out ExpressionRange<TypePath> typeArguments);

                identifier = templateBuffer.Add(startLocation, tokenStream.location, new TypedQualifiedIdentifierNode() {
                    tagName = tagName,
                    moduleName = moduleName,
                    typeArguments = typeArguments
                });

                return true;
            }
            else {
                TypeArgumentSnippet typeList = new TypeArgumentSnippet(HelpType.QualifiedIdentifierSyntax, true);
                ParseAngleBracketListWithRecovery(ref tokenStream, ref typeList, out ExpressionRange<TypePath> typeArguments);

                identifier = templateBuffer.Add(startLocation, tokenStream.location, new TypedQualifiedIdentifierNode() {
                    tagName = firstLocation,
                    moduleName = new NonTrivialTokenLocation(-1),
                    typeArguments = typeArguments
                });

                return true;
            }
        }

        private bool ParseQualifiedIdentifier(ref TokenStream tokenStream, out QualifiedIdentifier identifier) {
            int startLocation = tokenStream.location;

            NonTrivialTokenLocation firstLocation = new NonTrivialTokenLocation(startLocation);

            if (!tokenStream.ConsumeKeywordOrIdentifier()) {
                identifier = default;
                return false;
            }

            if (tokenStream.Consume(SymbolType.DoubleColon)) {
                NonTrivialTokenLocation secondLocation = new NonTrivialTokenLocation(tokenStream.location);

                if (!tokenStream.ConsumeKeywordOrIdentifier()) {
                    identifier = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.QualifiedIdentifierSyntax);
                }

                identifier = new QualifiedIdentifier() {
                    moduleName = firstLocation,
                    tagName = secondLocation
                };

                return true;
            }

            identifier = new QualifiedIdentifier() {
                tagName = firstLocation,
                moduleName = new NonTrivialTokenLocation(-1)
            };

            return true;
        }

        private bool ParseTemplateMatch(ref TokenStream tokenStream, out NodeIndex<TemplateSwitchStatement> switchStatement) {
            // : 'match' scope_modifier? OPEN_PARENS expression CLOSE_PARENS OPEN_BRACE switch_section* CLOSE_BRACE
            switchStatement = default;
            int startLocation = tokenStream.location;
            if (!tokenStream.Consume(TemplateKeyword.Match)) {
                return false;
            }

            if (!ParseScopeModifier(ref tokenStream, out ScopeModifier modifier)) {
                return false;
            }

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.SwitchStatementSyntax, out ExpressionIndex condition)) {
                return false;
            }

            if (!tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                return HardError(tokenStream, DiagnosticError.ExpectedCurlyBrace, HelpType.SwitchStatementSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream switchBodyStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedCurlyBrace, HelpType.SwitchStatementSyntax);
            }

            if (switchBodyStream.IsEmpty) {
                switchStatement = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSwitchStatement() {
                    modifier = modifier,
                    condition = condition,
                    sections = default
                });
                return true; // i guess this is ok? 
            }

            bool hasDefaultLabel = false;

            using ScopedList<NodeIndex<TemplateSwitchSection>> list = scopedAllocator.CreateListScope<NodeIndex<TemplateSwitchSection>>(16);

            while (switchBodyStream.HasMoreTokens && ParseTemplateSwitchSection(ref switchBodyStream, ref hasDefaultLabel, out NodeIndex<TemplateSwitchSection> section)) {
                list.Add(section);
            }

            if (switchBodyStream.HasMoreTokens) {
                return HardError(switchBodyStream, DiagnosticError.UnexpectedToken, HelpType.SwitchStatementSyntax);
            }

            switchStatement = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSwitchStatement() {
                modifier = modifier,
                condition = condition,
                sections = templateBuffer.AddNodeList(list)
            });

            return true;
        }

        private bool ParseTemplateSwitchLabel(ref TokenStream tokenStream, ref bool hasDefaultLabel, out NodeIndex<TemplateSwitchLabel> nodeIndex) {
            // : ('case' | 'default') expression ':'
            // can consider including a case_guard? here ( 'when' expression )

            int startLocation = tokenStream.location;

            nodeIndex = default;
            bool isDefault = tokenStream.Peek(TemplateKeyword.Default);
            if (!tokenStream.Consume(TemplateKeyword.Case) && !tokenStream.Consume(TemplateKeyword.Default)) {
                return false; // HardError(tokenStream, DiagnosticError.ExpectedCaseOrDefaultKeyword, HardErrorHelpType.SwitchCaseSyntax);
            }

            ExpressionIndex expression = default;

            if (!isDefault && !ParseExpression(ref tokenStream, out expression)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.SwitchCaseSyntax);
            }

            if (isDefault) {
                if (hasDefaultLabel) {
                    return HardError(tokenStream, DiagnosticError.DuplicateDefaultLabel, HelpType.SwitchCaseSyntax);
                }

                hasDefaultLabel = true;
                ExpressionParseState state = SaveState(tokenStream);
                if (ParseExpression(ref tokenStream, out expression)) {
                    RestoreState(ref tokenStream, state);
                    return HardError(state.location, DiagnosticError.DefaultSwitchCasesCannotHaveExpressions, HelpType.SwitchCaseSyntax);
                }
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedColon, HelpType.SwitchCaseSyntax);
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSwitchLabel() {
                isDefault = isDefault,
                expression = expression
            });

            return true;
        }

        private bool ParseTemplateSwitchSection(ref TokenStream tokenStream, ref bool defaultLabel, out NodeIndex<TemplateSwitchSection> nodeIndex) {
            // switch_label+ (template_statement | template_block)

            nodeIndex = default;
            int startLocation = tokenStream.location;

            using ScopedList<NodeIndex<TemplateSwitchLabel>> list = scopedAllocator.CreateListScope<NodeIndex<TemplateSwitchLabel>>(8);

            while (ParseTemplateSwitchLabel(ref tokenStream, ref defaultLabel, out NodeIndex<TemplateSwitchLabel> label)) {
                list.Add(label);
            }

            if (list.size == 0) {
                return HardError(tokenStream, DiagnosticError.ExpectedSwitchLabel, HelpType.SwitchStatementSyntax);
            }

            if (!ParseTemplateBlockOrStatement(ref tokenStream, out NodeIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.SwitchStatementSyntax);
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSwitchSection() {
                body = body,
                labels = templateBuffer.AddNodeList(list)
            });

            return true;
        }

        private bool ParseScopeModifier(ref TokenStream tokenStream, out ScopeModifier scopeModifier) {
            scopeModifier = ScopeModifier.None;

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return true;
            }

            if (tokenStream.Consume(TemplateKeyword.Destructive)) {
                scopeModifier = ScopeModifier.Destructive;
                return true;
            }

            if (tokenStream.Consume(TemplateKeyword.NonDestructive)) {
                scopeModifier = ScopeModifier.Destructive;
                return true;
            }

            return HardError(tokenStream, DiagnosticError.ExpectedScopeModifier, HelpType.ScopeModifierSyntax);
        }

        private bool ParseTemplateBlockOrStatement(ref TokenStream tokenStream, out NodeIndex body) {
            // : template_block
            // | template_statement

            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                ParseTemplateBlockWithRecovery(ref tokenStream, false, out NodeIndex<TemplateBlockNode> blockNode);
                body = blockNode;
                return true;
            }

            if (ParseTemplateEmbeddedStatement(ref tokenStream, out NodeIndex statementNode)) {
                body = statementNode;
                return true;
            }

            body = default;
            return false;
        }

        private bool ParseTemplateIf(ref TokenStream tokenStream, out NodeIndex<TemplateIfStatement> nodeIndex) {
            // IF (':' scope_modifier)? OPEN_PARENS expression CLOSE_PARENS if_body (ELSE if_body)?

            nodeIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.If)) {
                return false;
            }

            if (!ParseScopeModifier(ref tokenStream, out ScopeModifier scopeModifier)) {
                return false;
            }

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, HelpType.IfStatementSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream conditionStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.IfStatementSyntax);
            }

            PushRecoveryPoint(conditionStream.location, tokenStream.location);
            ParseExpression(ref conditionStream, out ExpressionIndex conditionExpression);
            PopRecoveryPoint(ref conditionStream);

            int bodyStartIndex = templateBuffer.nodes.size;
            if (!ParseTemplateBlockOrStatement(ref tokenStream, out NodeIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.IfStatementSyntax);
            }

            int bodyEndIndex = templateBuffer.nodes.size;
            bool isElseIf = false;
            NodeIndex elseIndex = default;
            if (tokenStream.Consume(TemplateKeyword.Else)) {
                
                isElseIf = tokenStream.Peek(TemplateKeyword.If);
                
                if (!ParseTemplateBlockOrStatement(ref tokenStream, out elseIndex)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.ElseStatementSyntax);
                }
                
            }

            int elseEndIndex = templateBuffer.nodes.size;

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateIfStatement() {
                scopeModifier = scopeModifier,
                condition = conditionExpression,
                trueBody = body,
                falseBody = elseIndex,
                bodyStartIndex = bodyStartIndex,
                bodyEndIndex = bodyEndIndex,
                elseEndIndex = elseEndIndex,
                isElseIf = isElseIf
            });

            return true;
        }

        private bool ParseTemplateVar(ref TokenStream tokenStream, out NodeIndex<TemplateVariableDeclaration> nodeIndex) {
            nodeIndex = default;

            int startLocation = tokenStream.location;

            bool hardError = tokenStream.Consume(TemplateKeyword.Var);

            // if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
            //     return hardError && HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.VariableSyntax);
            // }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.VariableSyntax);
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.VariableSyntax);
            }

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseArrayInitializerRule(tokenStream));

            if (!expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.VariableSyntax);
            }

            // semi colon handled in calling scope

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateVariableDeclaration() {
                identifierLocation = identifierLocation,
                typePath = default,
                initializer = expressionIndex
            });

            return true;
        }

        private bool ParseTemplateConst(ref TokenStream tokenStream, out NodeIndex<TemplateConstDeclaration> nodeIndex) {
            nodeIndex = default;

            int startLocation = tokenStream.location;

            bool hardError = tokenStream.Consume(TemplateKeyword.Const);

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> typePath)) {
                return hardError && HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.ConstantSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.ConstantSyntax);
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                return HardError(tokenStream, DiagnosticError.ConstantsMustBeFullyInitialized, HelpType.ConstantSyntax);
            }

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseArrayInitializerRule(tokenStream));

            if (!expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.ConstantSyntax);
            }

            // semi colon handled in calling scope

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateConstDeclaration() {
                identifierLocation = identifierLocation,
                typePath = typePath,
                initializer = expressionIndex
            });

            return true;
        }

        private bool ParseTemplateState(ref TokenStream tokenStream, bool isTopLevel, out NodeIndex<TemplateStateDeclaration> nodeIndex) {
            nodeIndex = default;

            int startLocation = tokenStream.location;

            tokenStream.Consume(TemplateKeyword.State);

            bool isPublic = true;

            if (isTopLevel && !ParseVisibilityModifier(ref tokenStream, out isPublic, HelpType.TopLevelStateSyntax)) {
                return false;
            }

            ExpressionIndex<TypePath> typePath = default;
            if (tokenStream.Consume(TemplateKeyword.Var)) {
                // if (isTopLevel) {
                //     return HardError(tokenStream.location - 1, DiagnosticError.InferredTypeNotAllowedAtTopLevel, HelpType.TopLevelStateSyntax);
                // }
            }
            else if (!ParseTypePath(ref tokenStream, out typePath)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTypePath, HelpType.StateSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.StateSyntax);
            }

            NodeIndex<FromMapping> fromMapping = default;
            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!isTopLevel) {
                    return HardError(tokenStream.location - 1, DiagnosticError.FromMappingIsOnlyValidInTopLevelState, HelpType.StateSyntax);
                }

                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return false;
                }
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                if (!tokenStream.IsEmpty && !isTopLevel) {
                    return HardError(tokenStream, DiagnosticError.UnexpectedToken);
                }

                nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateStateDeclaration() {
                    isPublic = isPublic,
                    identifierLocation = identifierLocation,
                    fromMapping = fromMapping,
                    typePath = typePath,
                    initializer = default
                });

                return true;
            }

            expressionBuffer.PushRuleScope();
            expressionBuffer.MakeRule(ParseExpressionRule(tokenStream));
            expressionBuffer.MakeRule(ParseArrayInitializerRule(tokenStream));

            if (!expressionBuffer.PopRuleScope(ref tokenStream, out ExpressionIndex expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.StateSyntax);
            }

            // semi colon handled in calling scope

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateStateDeclaration() {
                isPublic = isPublic,
                identifierLocation = identifierLocation,
                typePath = typePath,
                initializer = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseVisibilityModifier(ref TokenStream tokenStream, out bool isPublic, HelpType helpType) {
            isPublic = true;
            if (tokenStream.Consume(SymbolType.Colon)) {
                if (tokenStream.Consume(TemplateKeyword.Public)) {
                    isPublic = true;
                }
                else if (tokenStream.Consume(TemplateKeyword.Private)) {
                    isPublic = false;
                }
                else {
                    return HardError(tokenStream, DiagnosticError.ExpectedPublicOrPrivateAccessModifier, helpType);
                }
            }

            return true;
        }

        private bool ParseCreateSection(ref TokenStream tokenStream, out NodeIndex<CreateSection> nodeIndex) {
            // : 'create' { statement_list }
            // | 'create' embedded_statement ';'

            nodeIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Create)) {
                return false;
            }

            if (tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream subStream)) {
                // this is recoverable 
                if (!ParseStatementListWithHardErrors(ref subStream, HelpType.SectionSyntax, out ExpressionRange statements)) {
                    return false;
                }

                if (subStream.HasMoreTokens) {
                    HardError(tokenStream, DiagnosticError.StuckOnUnexpectedToken, HelpType.SectionSyntax);
                    return false;
                }

                nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new CreateSection() {
                    statements = statements
                });

                return true;
            }

            if (!ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex embedded)) {
                HardError(tokenStream, DiagnosticError.ExpectedStatement, HelpType.SectionSyntax);
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.SectionSyntax);
                return false;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new CreateSection() {
                statements = expressionBuffer.AddExpressionList(embedded)
            });

            return true;
        }

        private bool ParseEnableSection(ref TokenStream tokenStream, out NodeIndex<EnableSection> nodeIndex) {
            // : 'enable' { statement_list }
            // | 'enable' embedded_statement ';'

            nodeIndex = default;
            int startLocation = tokenStream.location;
            if (!tokenStream.Consume(TemplateKeyword.Enable)) {
                return false;
            }

            if (tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream subStream)) {
                // this is recoverable 
                if (!ParseStatementListWithHardErrors(ref subStream, HelpType.SectionSyntax, out ExpressionRange statements)) {
                    return false;
                }

                if (subStream.HasMoreTokens) {
                    HardError(tokenStream, DiagnosticError.StuckOnUnexpectedToken, HelpType.SectionSyntax);
                    return false;
                }

                nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new EnableSection() {
                    statements = statements
                });

                return true;
            }

            if (!ParseEmbeddedStatement(ref tokenStream, out ExpressionIndex embedded)) {
                HardError(tokenStream, DiagnosticError.ExpectedStatement, HelpType.SectionSyntax);
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.SectionSyntax);
                return false;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new EnableSection() {
                statements = expressionBuffer.AddExpressionList(embedded)
            });

            return true;
        }

        private bool ParseRepeatStatement(ref TokenStream tokenStream, out NodeIndex<RepeatNode> repeatNode) {
            // : 'repeat' '(' identifier 'in' csExpression (',' repeat_parameter)* ')' ('[' repeat_extrusion* ']')? block_or_embedded
            // | 'repeat' '(' (csExpression | integer_range) (',' repeat_parameter)* ')' ('[' repeat_extrusion* ']')? block_or_embedded
            repeatNode = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Repeat)) {
                return false;
            }

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, HelpType.RepeatSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream argumentListStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.RepeatSyntax);
            }

            if (!argumentListStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream repeatClauseStream)) {
                return HardError(tokenStream, DiagnosticError.ExpectedRepeatExpression, HelpType.RepeatSyntax);
            }

            PushRecoveryPoint(repeatClauseStream.start, repeatClauseStream.end + 1);
            ParseRepeatClause(ref repeatClauseStream, out ExpressionIndex<Identifier> identifier, out ExpressionIndex listExpression);
            PopRecoveryPoint(ref repeatClauseStream);

            if (!ParseRepeatParameters(argumentListStream, out NodeRange<RepeatParameter> parameters)) {
                return false;
            }

            // optional
            ParseExtrusionsWithRecovery(ref tokenStream, out NodeRange<Extrusion> extrusions);

            if (extrusions.length > 2) {
                return HardError(tokenStream, DiagnosticError.RepeatOnlyExtrudesIndexAndIteration, HelpType.RepeatSyntax);
            }

            if (!ParseTemplateBlockOrStatement(ref tokenStream, out NodeIndex body)) {
                return HardError(tokenStream, DiagnosticError.ExpectedBlockOrEmbeddedStatement, HelpType.RepeatSyntax);
            }

            repeatNode = templateBuffer.Add(startLocation, tokenStream.location, new RepeatNode() {
                itemIdentifier = identifier,
                repeatExpression = listExpression,
                parameters = parameters,
                extrusions = extrusions,
                body = body,
            });
            return true;
        }

        private bool ParseRepeatParameters(TokenStream tokenStream, out NodeRange<RepeatParameter> parameters) {
            parameters = default;

            if (!tokenStream.HasMoreTokens) {
                return true;
            }

            if (!tokenStream.Consume(SymbolType.Comma)) {
                return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.RepeatSyntax);
            }

            using ScopedList<NodeIndex<RepeatParameter>> list = scopedAllocator.CreateListScope<NodeIndex<RepeatParameter>>(8);
            while (tokenStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream stream)) {
                PushRecoveryPoint(stream.start, stream.end + 1);
                if (ParseRepeatParameter(ref stream, out NodeIndex<RepeatParameter> parameter)) {
                    list.Add(parameter);
                }

                PopRecoveryPoint(ref stream);

                if (!CheckDanglingComma(ref tokenStream)) {
                    return false;
                }
            }

            if (tokenStream.HasMoreTokens) {
                return HardError(tokenStream, DiagnosticError.UnexpectedToken);
            }

            parameters = templateBuffer.AddNodeList(list);
            return true;
        }

        // private bool ParseRepeatExtrusionsWithRecovery(ref TokenStream tokenStream, out NodeRange<RepeatExtrusion> repeatExtrusions) {
        //     // : ('[' (repeat_extrusion (',' repeat_extrusion)*)? ']')?
        //
        //     repeatExtrusions = default;
        //
        //     if (!tokenStream.Peek(SymbolType.SquareBraceOpen)) {
        //         return true;
        //     }
        //
        //     if (!tokenStream.TryGetSubStream(SubStreamType.SquareBrackets, out TokenStream extrusionStream)) {
        //         return HardError(tokenStream, DiagnosticError.UnmatchedSquareBracket, HelpType.RepeatExtrusionSyntax);
        //     }
        //
        //     if (extrusionStream.IsEmpty) {
        //         return true;
        //     }
        //
        //     using ScopedList<NodeIndex<RepeatExtrusion>> list = scopedAllocator.CreateListScope<NodeIndex<RepeatExtrusion>>(8);
        //
        //     while (extrusionStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
        //         PushRecoveryPoint(nextStream.start, nextStream.end + 1);
        //         if (ParseRepeatExtrusion(ref nextStream, out NodeIndex<RepeatExtrusion> extrusion)) {
        //             list.Add(extrusion);
        //         }
        //
        //         PopRecoveryPoint(ref nextStream);
        //
        //         if (!CheckDanglingComma(ref extrusionStream)) {
        //             return false;
        //         }
        //     }
        //
        //     if (extrusionStream.HasMoreTokens) {
        //         return HardError(extrusionStream, DiagnosticError.UnexpectedToken);
        //     }
        //
        //     repeatExtrusions = templateBuffer.AddNodeList(list);
        //     return true;
        // }
        //
        // private bool ParseRepeatExtrusion(ref TokenStream tokenStream, out NodeIndex<RepeatExtrusion> nodeIndex) {
        //     // repeat_extrusion_name (as identifier)
        //
        //     int startLocation = tokenStream.location;
        //     TemplateKeyword keyword = tokenStream.Current.Keyword;
        //     RepeatParameterName parameterName;
        //     nodeIndex = default;
        //     switch (keyword) {
        //         case TemplateKeyword.KeyFn:
        //             return HardError(tokenStream, DiagnosticError.RepeatKeyFnIsNotExtrudable, HelpType.RepeatExtrusionSyntax);
        //
        //         case TemplateKeyword.StepSize:
        //             parameterName = RepeatParameterName.StepSize;
        //             tokenStream.location++;
        //             break;
        //
        //         case TemplateKeyword.Start:
        //             parameterName = RepeatParameterName.Start;
        //             tokenStream.location++;
        //             break;
        //
        //         case TemplateKeyword.End:
        //             parameterName = RepeatParameterName.End;
        //             tokenStream.location++;
        //             break;
        //
        //         case TemplateKeyword.Count:
        //             parameterName = RepeatParameterName.Count;
        //             tokenStream.location++;
        //             break;
        //
        //         default: {
        //             return HardError(tokenStream, DiagnosticError.ExpectedRepeatExtrusionName, HelpType.RepeatExtrusionSyntax);
        //         }
        //     }
        //
        //     if (!tokenStream.Consume(TemplateKeyword.As)) {
        //         nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new RepeatExtrusion() {
        //             alias = new NonTrivialTokenLocation(-1),
        //             key = parameterName
        //         });
        //         return true;
        //     }
        //
        //     int aliasLocation = tokenStream.location;
        //
        //     if (!tokenStream.ConsumeKeywordOrIdentifier()) {
        //         return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.RepeatExtrusionSyntax);
        //     }
        //
        //     nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new RepeatExtrusion() {
        //         alias = new NonTrivialTokenLocation(aliasLocation),
        //         key = parameterName
        //     });
        //
        //     return true;
        // }

        private bool CheckDanglingComma(ref TokenStream argumentListStream) {
            bool ateComma = argumentListStream.Consume(SymbolType.Comma);

            if (ateComma && !argumentListStream.HasMoreTokens) {
                return HardError(argumentListStream.location - 1, DiagnosticError.DanglingComma);
            }

            if (!ateComma && argumentListStream.HasMoreTokens) {
                return HardError(argumentListStream, DiagnosticError.UnexpectedToken);
            }

            return true;
        }

        private bool ParseRepeatParameter(ref TokenStream tokenStream, out NodeIndex<RepeatParameter> parameter) {
            // : repeat_parameter_name = expression

            parameter = default;

            TemplateKeyword keyword = tokenStream.Current.Keyword;
            RepeatParameterName parameterName = RepeatParameterName.Invalid;
            int startLocation = tokenStream.location;

            switch (keyword) {
                case TemplateKeyword.KeyFn:
                    parameterName = RepeatParameterName.KeyFn;
                    tokenStream.location++;
                    break;

                // note -- removed the parameter names, we support IEnumerables now so we can just provide a list paging enumerable
                // leaving parsing code in because I may bring this back 
                
                case TemplateKeyword.StepSize:
                    parameterName = RepeatParameterName.StepSize;
                    tokenStream.location++;
                    break;
                
                case TemplateKeyword.Start:
                    parameterName = RepeatParameterName.Start;
                    tokenStream.location++;
                    break;
                
                case TemplateKeyword.End:
                    parameterName = RepeatParameterName.End;
                    tokenStream.location++;
                    break;
                
                case TemplateKeyword.Count:
                    parameterName = RepeatParameterName.Count;
                    tokenStream.location++;
                    break;

                default: {
                    return HardError(tokenStream, DiagnosticError.ExpectedRepeatParameterName, HelpType.RepeatParameterNames);
                }
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                return HardError(tokenStream, DiagnosticError.ExpectedEqualSign, HelpType.RepeatParameterSyntax);
            }

            if (!ParseNonAssignment(ref tokenStream, out ExpressionIndex expression)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.RepeatParameterSyntax);
            }

            parameter = templateBuffer.Add(startLocation, tokenStream.location, new RepeatParameter() {
                key = parameterName,
                value = expression
            });
            return true;
        }

        private bool ParseRepeatClause(ref TokenStream tokenStream, out ExpressionIndex<Identifier> identifier, out ExpressionIndex expressionIndex) {
            // : identifier 'in' csExpression
            // | csExpression

            identifier = default;
            expressionIndex = default;

            if (tokenStream.PeekIdentifierFollowedByKeyword(TemplateKeyword.In)) {
                ParseIdentifier(ref tokenStream, out identifier);

                tokenStream.Consume(TemplateKeyword.In);

                if (!ParseExpression(ref tokenStream, out expressionIndex)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.RepeatSyntax);
                }

                return true;
            }

            if (!ParseExpression(ref tokenStream, out expressionIndex)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.RepeatSyntax);
            }

            return true;
        }

        private bool ParseRunStatement(ref TokenStream tokenStream, out NodeIndex<RunSection> runNode) {
            // : 'run' statement ';'
            // | 'run' '{' statement_list '}'

            runNode = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Run)) {
                return false;
            }

            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream runBlockStream)) {
                    return HardError(tokenStream, DiagnosticError.UnmatchedCurlyBrace, HelpType.RunStatementSyntax);
                }

                if (runBlockStream.IsEmpty) {
                    templateBuffer.Add(startLocation, tokenStream.location, new RunSection() {
                        statements = default
                    });

                    return true;
                }

                PushRecoveryPoint(runBlockStream.location, tokenStream.location);
                ParseStatementListWithHardErrors(ref runBlockStream, HelpType.RunStatementSyntax, out ExpressionRange statementList);
                PopRecoveryPoint(ref runBlockStream);

                runNode = templateBuffer.Add(startLocation, tokenStream.location, new RunSection() {
                    statements = statementList
                });
                return true;
            }

            if (!ParseStatement(ref tokenStream, out ExpressionIndex statement)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.RunStatementSyntax);
            }

            runNode = templateBuffer.Add(startLocation, tokenStream.location, new RunSection() {
                statements = expressionBuffer.AddExpressionList(statement)
            });

            return true;
        }

        private bool ParseTeleport(ref TokenStream tokenStream, out NodeIndex<TeleportNode> nodeIndex) {
            // : 'teleport' '->' '(' quoted_string (, teleport_search_method)? ')' template_block

            nodeIndex = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Teleport)) {
                return false;
            }

            ExpressionIndex searchExpression = default;

            if (!tokenStream.Consume(SymbolType.ThinArrow)) {
                return HardError(tokenStream, DiagnosticError.ExpectedThinArrow, HelpType.TeleportSyntax);
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.Parens, HelpType.TeleportSyntax, out TokenStream argStream)) {
                return false;
            }

            if (!ParseNonAssignment(ref argStream, out ExpressionIndex portalExpr)) {
                return HardError(argStream, DiagnosticError.ExpectedExpression, HelpType.TeleportSyntax);
            }

            if (argStream.Consume(SymbolType.Comma) && !ParseNonAssignment(ref argStream, out searchExpression)) {
                return HardError(argStream, DiagnosticError.ExpectedExpression, HelpType.TeleportSyntax);
            }

            if (argStream.HasMoreTokens) {
                return HardError(argStream, DiagnosticError.UnexpectedToken, HelpType.TeleportSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream bodyStream)) {
                return HardError(tokenStream.location, DiagnosticError.UnmatchedCurlyBrace);
            }

            PushRecoveryPoint(startLocation, tokenStream.location);

            ParseTemplateBlock(ref bodyStream, false, out NodeIndex<TemplateBlockNode> block);

            if (PopRecoveryPoint(ref bodyStream)) {
                return false;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TeleportNode() {
                block = block,
                portalExpression = portalExpr,
                searchExpression = searchExpression
            });

            return true;
        }

        private bool ParseMarker(ref TokenStream tokenStream, out NodeIndex<MarkerNode> nodeIndex) {
            // : 'marker' (':' 'defer')? identifier '{' template_statement* '}'

            nodeIndex = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Marker)) {
                return false;
            }

            bool isDeferred = false;
            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!tokenStream.Consume(TemplateKeyword.Defer)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedDeferKeyword, HelpType.MarkerSyntax);
                }

                isDeferred = true;
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.MarkerSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream bodyStream)) {
                return HardError(tokenStream.location, DiagnosticError.UnmatchedCurlyBrace);
            }

            PushRecoveryPoint(startLocation, tokenStream.location);
            ParseTemplateBlock(ref bodyStream, false, out NodeIndex<TemplateBlockNode> block);

            if (PopRecoveryPoint(ref bodyStream)) {
                return false;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new MarkerNode() {
                block = block,
                identifer = identifier,
                isDeferred = isDeferred
            });

            return true;
        }

        private bool ParseTemplateBlockWithRecovery(ref TokenStream tokenStream, bool allowSlots, out NodeIndex<TemplateBlockNode> blockNode) {
            int startLocation = tokenStream.location;
            blockNode = default;

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                return true;
            }

            if (tokenStream.Peek(SymbolType.CurlyBraceOpen)) {
                if (!tokenStream.TryGetSubStream(SubStreamType.CurlyBraces, out TokenStream bodyStream)) {
                    return HardError(tokenStream.location, DiagnosticError.UnmatchedCurlyBrace);
                }

                PushRecoveryPoint(startLocation, tokenStream.location);
                ParseTemplateBlock(ref bodyStream, allowSlots, out blockNode);
                return !PopRecoveryPoint(ref bodyStream);
            }

            return HardError(tokenStream.location - 1, ErrorLocationAdjustment.TokenEnd, DiagnosticError.ExpectedSemiColonOrBlock);
        }

        private bool ParseTemplateBlock(ref TokenStream tokenStream, bool allowSlots, out NodeIndex<TemplateBlockNode> blockNode) {
            blockNode = default;

            using ScopedList<NodeIndex> list = scopedAllocator.CreateListScope<NodeIndex>(16);
            int startLocation = tokenStream.location;

            bool slotOverrideAllowed = allowSlots;

            while (tokenStream.HasMoreTokens && !HasCriticalError) {
                if (ParseTemplateBlockStatement(ref tokenStream, ref slotOverrideAllowed, out NodeIndex templateStatement)) {
                    list.Add(templateStatement);
                }
                else {
                    return false; // error already thrown 
                }
            }

            blockNode = templateBuffer.Add(startLocation, tokenStream.location, new TemplateBlockNode() {
                statements = templateBuffer.AddNodeList(list),
            });
            return true;
        }

        private bool ParseTemplateEmbeddedStatement(ref TokenStream tokenStream, out NodeIndex statement) {
            // : ';'
            // | if
            // | switch
            // | run
            // | foreach/repeat
            // | element_node
            // ----- maybe later ----
            // | foreach_loop
            // | for_loop
            // | while_loop
            // | do_while_loop
            // | return/exit/continue/break whatever jump statement breaks me out of a template
            // ----- explicitly cannot support ----
            // expression because we can't tell the difference between a method invocation and an element 

            statement = default;

            if (tokenStream.Consume(SymbolType.SemiColon)) {
                return true;
            }

            if (tokenStream.Peek(TemplateKeyword.If)) {
                if (ParseTemplateIf(ref tokenStream, out var ifStatement)) {
                    statement = ifStatement;
                    return true;
                }

                return false;
            }

            if (tokenStream.Peek(TemplateKeyword.Switch)) {
                if (ParseTemplateMatch(ref tokenStream, out NodeIndex<TemplateSwitchStatement> switchStatement)) {
                    statement = switchStatement;
                    return true;
                }

                return false;
            }

            if (tokenStream.Peek(TemplateKeyword.Repeat)) {
                if (ParseRepeatStatement(ref tokenStream, out NodeIndex<RepeatNode> repeatStatement)) {
                    statement = repeatStatement;
                    return true;
                }

                return false;
            }

            if (tokenStream.Peek(TemplateKeyword.Run)) {
                if (ParseRunStatement(ref tokenStream, out NodeIndex<RunSection> run)) {
                    statement = run;
                    return true;
                }

                return false;
            }

            if (ParseElementNode(ref tokenStream, out NodeIndex<ElementNode> elementNode)) {
                statement = elementNode;
                return true;
            }

            return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.EmbeddedStatementSyntax);
        }

        private bool GetSemiColonStream(ref TokenStream tokenStream, HelpType help, out TokenStream semiColonStream) {
            if (!tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out semiColonStream)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, help);
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, help);
            }

            return true;
        }

        private bool ParseTemplateBlockStatement(ref TokenStream tokenStream, ref bool slotOverrideAllowed, out NodeIndex statement) {
            statement = default;

            if (tokenStream.PeekIdentifierFollowedByCSharpSymbol()) {
                bool valid = false;
                ExpressionParseState save = SaveState(tokenStream);
                tokenStream.location++;
                if (tokenStream.Peek(SymbolType.QuestionMark)) {
                    tokenStream.location++;
                    valid = tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Assign);
                }

                if (tokenStream.Peek(SymbolType.Dot)) {
                    RestoreState(ref  tokenStream, save);
                    if (ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> path)) {
                        valid = tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Assign);
                    }
                }

                RestoreState(ref  tokenStream, save);
                if (!valid) {
                    return HardError(tokenStream, DiagnosticError.OperationIsNotAllowedHere, HelpType.None);
                }
            }
            
            switch (tokenStream.Current.Keyword) {
                // case TemplateKeyword.Function: {
                //     tokenStream.location++;
                //     if (tokenStream.Peek(SymbolType.ThinArrow)) {
                //         ParseRenderTemplateFunction(ref tokenStream, out statement);
                //         slotOverrideAllowed = false;
                //     }
                //     else {
                //         ParseLocalTemplateFn(ref tokenStream, out NodeIndex<LocalTemplateFn> localFn);
                //         statement = localFn;
                //         slotOverrideAllowed = false;
                //     }
                //
                //     break;
                // }

                // case TemplateKeyword.Static: {
                //     // todo -- not quite right 
                //     ParseLocalTemplateFn(ref tokenStream, out NodeIndex<LocalTemplateFn> localFn);
                //     statement = localFn;
                //     slotOverrideAllowed = false;
                //     break;
                // }

                case TemplateKeyword.If: {
                    ParseTemplateIf(ref tokenStream, out NodeIndex<TemplateIfStatement> ifStatement);
                    statement = ifStatement;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.State: {
                    if (!GetSemiColonStream(ref tokenStream, HelpType.StateSyntax, out TokenStream stateStream)) {
                        return false;
                    }

                    PushRecoveryPoint(stateStream.start, stateStream.end + 1);
                    ParseTemplateState(ref stateStream, false, out NodeIndex<TemplateStateDeclaration> stateIndex);
                    PopRecoveryPoint(ref stateStream);
                    statement = stateIndex;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Var: {
                    if (!GetSemiColonStream(ref tokenStream, HelpType.VariableSyntax, out TokenStream variableStream)) {
                        return false;
                    }

                    PushRecoveryPoint(variableStream.start, variableStream.end + 1);
                    ParseTemplateVar(ref variableStream, out NodeIndex<TemplateVariableDeclaration> varIndex);
                    PopRecoveryPoint(ref variableStream);
                    statement = varIndex;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Const: {
                    if (!GetSemiColonStream(ref tokenStream, HelpType.ConstantSyntax, out TokenStream constStream)) {
                        return false;
                    }

                    PushRecoveryPoint(constStream.start, constStream.end + 1);
                    ParseTemplateConst(ref constStream, out NodeIndex<TemplateConstDeclaration> constIndex);
                    PopRecoveryPoint(ref constStream);
                    statement = constIndex;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Enable: {
                    ParseEnableSection(ref tokenStream, out NodeIndex<EnableSection> enableSection);
                    statement = enableSection;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Create: {
                    ParseCreateSection(ref tokenStream, out NodeIndex<CreateSection> createSection);
                    statement = createSection;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Run: {
                    ParseRunStatement(ref tokenStream, out NodeIndex<RunSection> runNode);
                    statement = runNode;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Element: {
                    if (ParseElementNode(ref tokenStream, out NodeIndex<ElementNode> elementNode)) {
                        statement = elementNode;
                        slotOverrideAllowed = true;
                    }

                    break;
                }

                case TemplateKeyword.Repeat: {
                    ParseRepeatStatement(ref tokenStream, out NodeIndex<RepeatNode> repeatNode);
                    statement = repeatNode;
                    slotOverrideAllowed = false;
                    break;
                }

                // maybe call this jump or select or something so we don't have vocabulary overload w/ C#
                // or treat it identically to C# and be smarter about figuring out in scopes if we need to use disable

                case TemplateKeyword.Match: {
                    ParseTemplateMatch(ref tokenStream, out NodeIndex<TemplateSwitchStatement> switchStatement);
                    statement = switchStatement;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Render: {
                    ParseRender(ref tokenStream, out statement);
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Teleport: {
                    ParseTeleport(ref tokenStream, out NodeIndex<TeleportNode> teleport);
                    statement = teleport;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Marker: {
                    ParseMarker(ref tokenStream, out NodeIndex<MarkerNode> marker);
                    statement = marker;
                    slotOverrideAllowed = false;
                    break;
                }

                case TemplateKeyword.Slot: {
                    // there are positional requirements to slots, can only appear directly after an element (or fn) body 
                    // can only appear as the first child of an element node or as a sibling to another slot override
                    if (!slotOverrideAllowed) {
                        return HardError(tokenStream, DiagnosticError.InvalidSlotPlacement, HelpType.SlotOverrideLocation);
                    }

                    ParseTemplateSlotOverride(ref tokenStream, out NodeIndex slotOverride);
                    statement = slotOverride;
                    slotOverrideAllowed = true;
                    break;
                }

                default: {
                    // @
                    // element
                    // identifier :: identifier
                    // identifier( 

                    if (tokenStream.Peek(SymbolType.At)
                        || tokenStream.Peek(TemplateKeyword.Element)
                        || tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.OpenParen)
                        || tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.DoubleColon)) {
                        if (ParseElementNode(ref tokenStream, out NodeIndex<ElementNode> elementNode)) {
                            statement = elementNode;
                            slotOverrideAllowed = true;
                            break;
                        }

                        return HardError(tokenStream, DiagnosticError.ExpectedElementTag, HelpType.ElementTagSyntax);
                    }

                    ParseGenericElementTemplateOrVariable(ref tokenStream, out statement, out slotOverrideAllowed);

                    break;
                }
            }

            return statement.IsValid;
        }

        private bool ParseRender(ref TokenStream tokenStream, out NodeIndex statement) {
            // : render slot identifier ('(' argument_list? ')')? ';'
            // | render marker '(' expression ')' ';'
            // | render portal '(' expression ')' (';' | template_block)

            statement = default;
            if (!tokenStream.Consume(TemplateKeyword.Render)) {
                return false;
            }

            switch (tokenStream.Current.Keyword) {
                case TemplateKeyword.Slot:
                    tokenStream.location++;
                    return ParseRenderSlot(ref tokenStream, out statement);

                case TemplateKeyword.Portal:
                    tokenStream.location++;
                    return ParseRenderPortal(ref tokenStream, out statement);

                // rolled this into a more uniform slot syntax 
                // case TemplateKeyword.Dynamic:
                //     tokenStream.location++;
                //     return ParseRenderDynamic(ref tokenStream, out statement);

                // case TemplateKeyword.Implicit:
                //     tokenStream.location++;
                //     return ParseRenderImplicit(ref tokenStream, out statement);

                case TemplateKeyword.Marker:
                    tokenStream.location++;
                    return ParseRenderMarker(ref tokenStream, out statement);

                default: {
                    return HardError(tokenStream, DiagnosticError.UnknownRenderEntity, HelpType.RenderEntityNames);
                }
            }
        }

        private bool ParseSingleExpressionInParens(ref TokenStream tokenStream, HelpType helpData, out ExpressionIndex expression) {
            expression = default;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, helpData);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream markerStream)) {
                return HardError(markerStream, DiagnosticError.UnmatchedParentheses, helpData);
            }

            if (!ParseExpression(ref markerStream, out expression)) {
                return HardError(markerStream, DiagnosticError.ExpectedExpression, helpData);
            }

            return !markerStream.HasMoreTokens || HardError(tokenStream, DiagnosticError.UnexpectedToken, helpData);
        }

        private bool ParseRenderMarker(ref TokenStream tokenStream, out NodeIndex statement) {
            // : 'render' 'marker' '(' expression ')' ';'
            // 'render' and 'marker' were already consumed

            statement = default;
            int startLocation = tokenStream.location;

            if (!ParseSingleExpressionInParens(ref tokenStream, HelpType.RenderMarkerSyntax, out ExpressionIndex expression)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.RenderMarkerSyntax);
            }

            statement = templateBuffer.Add(startLocation, tokenStream.location, new RenderMarkerNode() {
                expression = expression
            });

            return true;
        }

        private bool ParseRenderPortal(ref TokenStream tokenStream, out NodeIndex statement) {
            // 'render' 'portal' '(' expression ')' ';'
            // 'render' and 'portal' were already consumed 

            statement = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return HardError(tokenStream, DiagnosticError.ExpectedParenthesis, HelpType.RenderPortalSyntax);
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream portalStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.RenderPortalSyntax);
            }

            if (!ParseExpression(ref portalStream, out ExpressionIndex expression)) {
                return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.RenderPortalSyntax);
            }

            if (portalStream.HasMoreTokens) {
                return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.RenderPortalSyntax);
            }

            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.RenderPortalSyntax);
            }

            statement = templateBuffer.Add(startLocation, tokenStream.location, new RenderPortalNode() {
                expression = expression
            });

            return true;
        }

        private bool ParseRenderSlot(ref TokenStream tokenStream, out NodeIndex statement) {
            // : 'render' 'slot' '->' ( '[' non_assignment_expression ']' | identifier) ('(' ')')? (';' | template_block)

            // 'render' and 'slot' were already consumed

            statement = default;
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(SymbolType.ThinArrow)) {
                return HardError(tokenStream, DiagnosticError.ExpectedThinArrow, HelpType.RenderSlotSyntax);
            }

            ExpressionIndex dynamicExpression = default;
            NonTrivialTokenLocation slotNameLocation = default;

            if (tokenStream.Peek(SymbolType.SquareBraceOpen)) {
                if (!GetRequiredSubStream(ref tokenStream, SubStreamType.SquareBrackets, HelpType.RenderDynamicSlotSyntax, out TokenStream expressionStream)) {
                    return false;
                }

                if (!ParseNonAssignment(ref expressionStream, out dynamicExpression)) {
                    return HardError(expressionStream, DiagnosticError.ExpectedExpression, HelpType.RenderDynamicSlotSyntax);
                }

                if (expressionStream.HasMoreTokens) {
                    return HardError(expressionStream, DiagnosticError.UnexpectedToken, HelpType.RenderDynamicSlotSyntax);
                }
            }
            else if (!tokenStream.ConsumeStandardIdentifier(out slotNameLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.RenderSlotSyntax);
            }

            ExpressionListSnippet snippet = new ExpressionListSnippet(HelpType.RenderSlotSyntax, true);
            ParseParenExpressionListWithRecovery(ref tokenStream, ref snippet, out ExpressionRange parameters);

            NodeIndex<TemplateBlockNode> block = default;
            if (!tokenStream.Consume(SymbolType.SemiColon)) {
                ParseTemplateBlockWithRecovery(ref tokenStream, false, out block);
            }

            statement = templateBuffer.Add(startLocation, tokenStream.location, new RenderSlotNode() {
                nameLocation = slotNameLocation,
                parameters = parameters,
                defaultBlock = block,
                dynamicExpression = dynamicExpression
            });

            return true;
        }

        private bool ParseGenericElementTemplateOrVariable(ref TokenStream tokenStream, out NodeIndex nodeIndex, out bool slotOverrideAllowed) {
            nodeIndex = default;
            int startLocation = tokenStream.location;

            slotOverrideAllowed = false;

            ExpressionParseState typeSave = SaveState(tokenStream);

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> path)) {
                return false;
            }

            // it's an element definition w/ generics
            if (tokenStream.Peek(SymbolType.OpenParen)) {
                RestoreState(ref tokenStream, typeSave);
                bool retn = ParseElementNode(ref tokenStream, out var elementIndex);
                nodeIndex = elementIndex;
                if (retn) {
                    slotOverrideAllowed = true;
                }

                return retn;
            }

            // string s = something();
            if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Assign)) {
                if (!tokenStream.TryGetNextTraversedStream(SymbolType.SemiColon, out TokenStream variableStream)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.VariableSyntax);
                }

                PushRecoveryPoint(variableStream.start, variableStream.end + 1);

                variableStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation);
                variableStream.Consume(SymbolType.Assign);

                if (!ParseVariableInitializer(ref variableStream, out ExpressionIndex initializer)) {
                    HardError(variableStream, DiagnosticError.ExpectedVariableInitializer, HelpType.VariableSyntax);
                }

                // I think its ok to add this even in the fail case 
                nodeIndex = templateBuffer.Add(startLocation, variableStream.location, new TemplateVariableDeclaration() {
                    initializer = initializer,
                    identifierLocation = identifierLocation,
                    typePath = path
                });

                if (variableStream.HasMoreTokens) {
                    return HardError(variableStream, DiagnosticError.UnexpectedToken);
                }

                if (!tokenStream.Consume(SymbolType.SemiColon)) {
                    HardError(tokenStream.location - 1, ErrorLocationAdjustment.TokenEnd, DiagnosticError.ExpectedTerminatingSemiColon, HelpType.VariableSyntax);
                }

                return !PopRecoveryPoint(ref variableStream);
            }

            // string s;
            if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.SemiColon)) {
                tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation);
                tokenStream.Consume(SymbolType.SemiColon);
                nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateVariableDeclaration() {
                    initializer = default,
                    identifierLocation = identifierLocation,
                    typePath = path
                });
                return true;
            }

            return false;
        }

        private bool ParseMethodDefinition(ref TokenStream tokenStream, ref NodeIndex nodeIndex, bool isPublic, int startLocation, ExpressionIndex<TypePath> returnType, bool requireSemiColon) {
            // : return_type identifier '(' parameter_list ')' (from_mapping |  block_expression | ('=>' cs_expression) )

            // return type is passed in from calling context

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifierLocation)) {
                return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.TemplateMethodSyntax);
            }

            FormalFunctionParameterSnippet parameterSnippet = new FormalFunctionParameterSnippet();

            if (!ParseParenListWithRecovery(ref tokenStream, ref parameterSnippet, out NodeRange<TemplateFunctionParameter> parameterList)) {
                return false; // forward error if there was one
            }

            ExpressionIndex body = default;
            NodeIndex<FromMapping> fromMapping = default;
            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return false;
                }

                if (requireSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                }
            }
            else if (tokenStream.Consume(SymbolType.FatArrow)) {
                if (!ParseNonAssignment(ref tokenStream, out body)) {
                    return false;
                }

                if (requireSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                }
            }
            else {
                RequireBlockWithRecovery(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression);
                body = blockExpression;
            }

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new MethodDeclaration() {
                isPublic = isPublic,
                returnType = returnType,
                parameters = parameterList,
                body = body,
                fromMapping = fromMapping,
                identifierLocation = identifierLocation
            });

            return true;
        }

        private unsafe bool ParseFunctionTemplateSignatureArguments(ref TokenStream tokenStream, TopLevelElementType topLevelElementType, ScopedList<NodeIndex>* list) {
            if (!tokenStream.HasMoreTokens) {
                return true;
            }

            if (!ParseTemplateSignatureArgument(ref tokenStream, topLevelElementType, out NodeIndex firstArg)) {
                return false;
            }

            list->Add(firstArg);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (tokenStream.HasMoreTokens) {
                    if (!ParseTemplateSignatureArgument(ref tokenStream, topLevelElementType, out NodeIndex argumentIndex)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedElementArgumentOrNoTrailingComma);
                    }

                    list->Add(argumentIndex);
                }
            }

            if (tokenStream.HasMoreTokens) {
                return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.UnprocessedTokensAfterElementArgumentList);
            }

            return true;
        }

        private bool ParseFunctionTemplateSignatureArguments(ref TokenStream tokenStream, TopLevelElementType topLevelElementType, out NodeRange nodeRange) {
            // template_signature_argument*

            if (!tokenStream.HasMoreTokens) {
                nodeRange = default;
                return true;
            }

            if (!ParseTemplateSignatureArgument(ref tokenStream, topLevelElementType, out NodeIndex firstArg)) {
                nodeRange = default;
                return false;
            }

            using ScopedList<NodeIndex> list = scopedAllocator.CreateListScope<NodeIndex>(16);
            list.Add(firstArg);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (tokenStream.HasMoreTokens) {
                    if (!ParseTemplateSignatureArgument(ref tokenStream, topLevelElementType, out NodeIndex argumentIndex)) {
                        nodeRange = default;
                        return HardError(tokenStream, DiagnosticError.ExpectedElementArgumentOrNoTrailingComma);
                    }

                    list.Add(argumentIndex);
                }
            }

            if (tokenStream.HasMoreTokens) {
                nodeRange = default;
                return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.UnprocessedTokensAfterElementArgumentList);
            }

            nodeRange = templateBuffer.AddNodeList(list);
            return true;
        }

        private bool ParseElementArguments(ref TokenStream tokenStream, bool isTemplateFn, out NodeRange nodeRange) {
            // element_argument*

            if (!tokenStream.HasMoreTokens) {
                nodeRange = default;
                return true;
            }

            if (!ParseElementArgument(ref tokenStream, isTemplateFn, out NodeIndex firstArg)) {
                nodeRange = default;
                return false;
            }

            using ScopedList<NodeIndex> list = scopedAllocator.CreateListScope<NodeIndex>(16);
            list.Add(firstArg);

            while (tokenStream.Consume(SymbolType.Comma)) {
                if (!ParseElementArgument(ref tokenStream, isTemplateFn, out NodeIndex argumentIndex)) {
                    nodeRange = default;
                    return HardError(tokenStream, DiagnosticError.ExpectedElementArgumentOrNoTrailingComma);
                }

                list.Add(argumentIndex);
            }

            if (tokenStream.HasMoreTokens) {
                nodeRange = default;
                return HardError(tokenStream, DiagnosticError.UnexpectedToken, HelpType.UnprocessedTokensAfterElementArgumentList);
            }

            nodeRange = templateBuffer.AddNodeList(list);
            return true;
        }

        private bool ParseDecorators(ref TokenStream tokenStream, out NodeRange<DecoratorNode> nodeRange) {
            // '@' qualified_name ( '(' decorator_arguments ')' )?

            if (tokenStream.Current.Symbol != SymbolType.At) {
                nodeRange = default;
                return false;
            }

            using ScopedList<NodeIndex<DecoratorNode>> decoratorList = scopedAllocator.CreateListScope<NodeIndex<DecoratorNode>>(8);

            while (ParseDecorator(ref tokenStream, out NodeIndex<DecoratorNode> decorator)) {
                decoratorList.Add(decorator);
            }

            if (decoratorList.size == 0) {
                // hard error probably 
                nodeRange = default;
                return false;
            }

            nodeRange = templateBuffer.AddNodeList(decoratorList);
            return true;
        }

        private bool ParseDecorator(ref TokenStream tokenStream, out NodeIndex<DecoratorNode> nodeIndex) {
            // : '@' qualified_name ('(' decorator_argument_list ')')? extrusion_list?

            int startLocation = tokenStream.location;

            nodeIndex = default;

            if (!tokenStream.Consume(SymbolType.At)) {
                return false;
            }

            if (!ParseQualifiedIdentifier(ref tokenStream, out QualifiedIdentifier qualifiedIdentifier)) {
                return HardError(tokenStream, DiagnosticError.ExpectedQualifiedIdentifier, HelpType.DecoratorSyntax);
            }

            ParseTypeArgumentList(ref tokenStream, out ExpressionRange<TypePath> typeArguments);

            // optional
            ParseDecoratorArgumentsWithRecovery(ref tokenStream, out NodeRange<DecoratorArgumentNode> arguments);

            // optional
            ParseExtrusionsWithRecovery(ref tokenStream, out NodeRange<Extrusion> extrusions);

            nodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new DecoratorNode() {
                qualifiedName = qualifiedIdentifier,
                typeArguments = typeArguments,
                arguments = arguments,
                extrusions = extrusions
            });

            return true;
        }

        private bool ParseDecoratorArgumentsWithRecovery(ref TokenStream tokenStream, out NodeRange<DecoratorArgumentNode> nodeRange) {
            // '(' (decorator_arg (',' decorator_arg)*)? ')'

            nodeRange = default;

            if (!tokenStream.Peek(SymbolType.OpenParen)) {
                return true;
            }

            if (!tokenStream.TryGetSubStream(SubStreamType.Parens, out TokenStream argumentStream)) {
                return HardError(tokenStream, DiagnosticError.UnmatchedParentheses, HelpType.DecoratorSyntax);
            }

            if (argumentStream.IsEmpty) {
                return true;
            }

            using ScopedList<NodeIndex<DecoratorArgumentNode>> list = scopedAllocator.CreateListScope<NodeIndex<DecoratorArgumentNode>>(8);

            while (argumentStream.TryGetNextTraversedStream(SymbolType.Comma, out TokenStream nextStream)) {
                PushRecoveryPoint(nextStream.start, nextStream.end + 1);
                if (ParseDecoratorArgument(ref nextStream, out NodeIndex<DecoratorArgumentNode> argument)) {
                    list.Add(argument);
                }

                PopRecoveryPoint(ref nextStream);
                if (!CheckDanglingComma(ref argumentStream)) {
                    return false;
                }
            }

            if (argumentStream.HasMoreTokens) {
                return HardError(argumentStream, DiagnosticError.UnexpectedToken);
            }

            nodeRange = templateBuffer.AddNodeList(list);
            return true;
        }

        private bool ParseDecoratorArgument(ref TokenStream tokenStream, out NodeIndex<DecoratorArgumentNode> argument) {
            // decorator_arg: (const | sync)? (identifier '=')? non_assignment_expression

            argument = default;
            TemplateArgumentModifier modifier = TemplateArgumentModifier.None;

            int startLocation = tokenStream.location;

            if (tokenStream.Consume(TemplateKeyword.Sync)) {
                return SoftError(startLocation, ErrorLocationAdjustment.None, DiagnosticError.DecoratorsCannotSupportSyncParameters, HelpType.DecoratorSyntax);
            }

            if (tokenStream.Consume(TemplateKeyword.Const)) {
                modifier = TemplateArgumentModifier.Const;
            }

            NonTrivialTokenLocation identifier = default;
            if (tokenStream.PeekIdentifierFollowedBySymbol(SymbolType.Assign)) {
                tokenStream.ConsumeStandardIdentifier(out identifier);
                tokenStream.location++;
            }

            if (!ParseNonAssignment(ref tokenStream, out ExpressionIndex expression)) {
                return false;
            }

            argument = templateBuffer.Add(startLocation, tokenStream.location, new DecoratorArgumentNode() {
                expression = expression,
                modifier = modifier,
                name = identifier
            });
            return true;
        }

        private struct ExpressionListSnippet : IExpressionSnippetParser {

            public ExpressionListSnippet(HelpType help, bool isOptional) {
                this.SyntaxHelp = help;
                this.Optional = isOptional;
            }

            public HelpType SyntaxHelp { get; }
            public bool Optional { get; }

            public bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out ExpressionIndex result) {
                return parser.ParseExpression(ref tokenStream, out result);
            }

        }

        private struct FormalFunctionParameterSnippet : ISnippetParser<TemplateFunctionParameter> {

            public bool requireDefaultValue;

            public HelpType SyntaxHelp => HelpType.TemplateFunctionParameterSyntax;
            public bool Optional { get; }

            public bool Parse(ref TokenStream tokenStream, ref TemplateParser parser, out NodeIndex<TemplateFunctionParameter> result) {
                return parser.ParseTemplateFunctionParameter(ref tokenStream, ref requireDefaultValue, out result);
            }

        }

        [Flags]
        private enum MemberMask {

            State = 1 << 0,
            Param = 1 << 1,
            Computed = 1 << 2,
            Method = 1 << 3,
            Attribute = 1 << 4,
            Style = 1 << 5,
            LifeCycle = 1 << 6,
            InputHandler = 1 << 7,
            SlotDeclaration = 1 << 8,
            RenderBody = 1 << 9,
            InvokeVariantBase = 1 << 10,
            Spawn = 1 << 11,
            OnChange = 1 << 12

        }

        private const MemberMask k_TypographyMemberTypes = ~(MemberMask.SlotDeclaration | MemberMask.RenderBody | MemberMask.InvokeVariantBase);
        private const MemberMask k_DecoratorMemberTypes = ~(MemberMask.SlotDeclaration | MemberMask.RenderBody | MemberMask.InvokeVariantBase);
        private const MemberMask k_FunctionMemberTypes = ~(MemberMask.Attribute | MemberMask.Style | MemberMask.LifeCycle | MemberMask.InputHandler | MemberMask.InvokeVariantBase);
        private const MemberMask k_VariantMemberTypes = ~(MemberMask.RenderBody);

        private unsafe bool ParseStructuredFunctionBlock(ref TokenStream tokenStream, ScopedList<NodeIndex>* list, out NodeIndex<TemplateBlockNode> body) {
            body = default;

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.TemplateFunctionSyntax, out TokenStream contents)) {
                return false;
            }

            while (contents.HasMoreTokens) {
                if (!ParseMember(ref contents, HelpType.TemplateFunctionSyntax, true, out NodeIndex memberNodeIndex, out MemberMask memberType)) {
                    return false;
                }

                int location = contents.location;

                if (memberType == MemberMask.RenderBody) {
                    body = new NodeIndex<TemplateBlockNode>(memberNodeIndex);
                }
                else if ((memberType & k_FunctionMemberTypes) == 0) {
                    SoftError(location, default, DiagnosticError.MemberTypeNotAllowedInFunction, HelpType.TemplateFunctionSyntax);
                }
                else {
                    list->Add(memberNodeIndex);
                }
            }

            return true;
        }

        private bool ParseTypographyBlock(ref TokenStream tokenStream, out NodeRange memberList) {
            memberList = default;

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.ElementDeclarationSyntax, out TokenStream contents)) {
                return false;
            }

            using ScopedList<NodeIndex> elementMemberList = scopedAllocator.CreateListScope<NodeIndex>(32);

            while (contents.HasMoreTokens) {
                if (!ParseMember(ref contents, HelpType.TypographySyntax, true, out NodeIndex memberNodeIndex, out MemberMask memberType)) {
                    return false;
                }

                int location = contents.location;

                if ((memberType & k_TypographyMemberTypes) == 0) {
                    SoftError(location, default, DiagnosticError.MemberTypeNotAllowedInTypography, HelpType.TypographySyntax);
                }
                else {
                    elementMemberList.Add(memberNodeIndex);
                }
            }

            memberList = templateBuffer.AddNodeList(elementMemberList);

            return true;
        }

        private unsafe bool ParseStructuredTemplateDeclarationBlock(ref TokenStream tokenStream, ScopedList<NodeIndex>* memberList, out NodeIndex<TemplateBlockNode> body, out NodeIndex<TemplateSpawnList> spawnList) {
            body = default;
            spawnList = default;

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.ElementDeclarationSyntax, out TokenStream contents)) {
                return false;
            }

            while (contents.HasMoreTokens) {
                if (!ParseMember(ref contents, HelpType.ElementDeclarationSyntax, true, out NodeIndex memberNodeIndex, out MemberMask memberType)) {
                    return false;
                }

                if (memberType == MemberMask.RenderBody) {
                    if (body.IsValid) {
                        // error 
                    }

                    body = new NodeIndex<TemplateBlockNode>(memberNodeIndex);
                }
                else if (memberType == MemberMask.Spawn) {
                    if (spawnList.IsValid) {
                        // error 
                    }

                    spawnList = new NodeIndex<TemplateSpawnList>(memberNodeIndex);
                }
                else {
                    memberList->Add(memberNodeIndex);
                }
            }


            return true;
        }

        private unsafe void ValidateMembers(ScopedList<NodeIndex>* memberList) {
            bool hasOptional = false;

            LifeCycleEventType eventTypes = 0;

            InputEventType inputEventType = 0;

            for (int i = 0; i < memberList->size; i++) {
                UntypedTemplateNode node = templateBuffer.nodes.Get(memberList->Get(i).id);

                if (node.meta.nodeType == TemplateNodeType.InputHandlerNode) {
                    InputHandlerNode inputNode = node.As<InputHandlerNode>();

                    if ((inputEventType & inputNode.inputEventType) != 0) {
                        SoftError(inputNode.meta.tokenRange.start.index, ErrorLocationAdjustment.None, DiagnosticError.InputHandlerWasAlreadyDefined);
                    }

                    inputEventType |= inputNode.inputEventType;

                    if (inputNode.expression.IsValid) {
                        UntypedExpressionNode expr = expressionBuffer.expressions.Get(inputNode.expression.id);
                        if (expr.meta.type == ExpressionNodeType.BlockExpression) {
                            BlockExpression block = expr.As<BlockExpression>();
                            if (block.statementList.length == 0) {
                                memberList[i--] = memberList[--memberList->size];
                            }
                        }
                    }

                    continue;
                }

                if (node.meta.nodeType == TemplateNodeType.LifeCycleEventNode) {
                    LifeCycleEventNode lifeCycleNode = node.As<LifeCycleEventNode>();

                    if ((eventTypes & lifeCycleNode.eventType) != 0) {
                        SoftError(lifeCycleNode.meta.tokenRange.start.index, ErrorLocationAdjustment.None, DiagnosticError.LifeCycleHandlerWasAlreadyDefined);
                    }

                    eventTypes |= lifeCycleNode.eventType;
                    if (lifeCycleNode.expression.IsValid) {
                        UntypedExpressionNode expr = expressionBuffer.expressions.Get(lifeCycleNode.expression.id);
                        if (expr.meta.type == ExpressionNodeType.BlockExpression) {
                            BlockExpression block = expr.As<BlockExpression>();
                            if (block.statementList.length == 0) {
                                memberList[i--] = memberList[--memberList->size];
                            }
                        }
                    }

                    continue;
                }

                if (node.meta.nodeType == TemplateNodeType.TemplateParameter) {
                    TemplateParameter parameter = node.As<TemplateParameter>();

                    if (parameter.isRequired) {
                        if (parameter.defaultValue.IsValid) {
                            SoftError(parameter.meta.tokenRange.start.index, ErrorLocationAdjustment.None, DiagnosticError.RequiredParametersCannotDefineDefaultValues);
                        }

                        if (hasOptional) {
                            SoftError(parameter.meta.tokenRange.start.index, ErrorLocationAdjustment.None, DiagnosticError.RequiredParametersMustBeDefinedBeforeOptionalOnes);
                        }
                    }
                    else {
                        hasOptional = true;
                    }
                }
            }
        }

        private unsafe bool ParseDecoratorBlock(ref TokenStream tokenStream, ScopedList<NodeIndex>* memberList) {
            // : '{' decorator_contents '}'

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.CurlyBraces, HelpType.DecoratorDeclarationSyntax, out TokenStream contents)) {
                return false;
            }

            while (contents.HasMoreTokens) {
                int location = contents.location;

                if (!ParseMember(ref contents, HelpType.DecoratorDeclarationSyntax, true, out NodeIndex memberNodeIndex, out MemberMask memberType)) {
                    return false;
                }

                if ((memberType & k_DecoratorMemberTypes) == 0) {
                    SoftError(location, default, DiagnosticError.MemberTypeNotAllowedInDecorator, HelpType.DecoratorDeclarationSyntax);
                }
                else {
                    memberList->Add(memberNodeIndex);
                }
            }


            return true;
        }

        private bool ParseMember(ref TokenStream tokenStream, HelpType helpType, bool requireSemiColons, out NodeIndex memberNodeIndex, out MemberMask memberMask) {
            switch (tokenStream.Current.Keyword) {
                case TemplateKeyword.OnChange: {
                    memberMask = MemberMask.OnChange;
                    return ParseOnChangeMember(ref tokenStream, true, out memberNodeIndex);
                }

                case TemplateKeyword.State: {
                    memberMask = MemberMask.State;
                    bool result = ParseTemplateState(ref tokenStream, true, out NodeIndex<TemplateStateDeclaration> stateDecl);
                    memberNodeIndex = stateDecl;
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Spawns: {
                    memberMask = MemberMask.Spawn;
                    bool result = ParseSpawnList(ref tokenStream, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Required:
                case TemplateKeyword.Optional: {
                    memberMask = MemberMask.Param;
                    bool result = ParseTemplateParameter(ref tokenStream, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Computed: {
                    memberMask = MemberMask.Computed;
                    bool result = ParseComputedProperty(ref tokenStream, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Style: {
                    memberMask = MemberMask.Style;
                    bool result = ParseStyle(ref tokenStream, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Attr: {
                    memberMask = MemberMask.Attribute;
                    bool result = ParseAttribute(ref tokenStream, false, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Before: {
                    memberMask = MemberMask.LifeCycle;
                    return ParseLifeCycleHandler(ref tokenStream, true, out memberNodeIndex, true);
                }

                case TemplateKeyword.After: {
                    memberMask = MemberMask.LifeCycle;
                    return ParseLifeCycleHandler(ref tokenStream, false, out memberNodeIndex, true);
                }

                case TemplateKeyword.Mouse: {
                    memberMask = MemberMask.InputHandler;
                    return ParseMouseInputHandler(ref tokenStream, out memberNodeIndex, true, true);
                }

                case TemplateKeyword.Focus: {
                    memberMask = MemberMask.InputHandler;
                    return ParseFocusInputHandler(ref tokenStream, out memberNodeIndex, true, true);
                }

                case TemplateKeyword.Text: {
                    memberMask = MemberMask.InputHandler;
                    return ParseTextInputHandler(ref tokenStream, out memberNodeIndex, true, true);
                }

                case TemplateKeyword.Drag: {
                    memberMask = MemberMask.InputHandler;
                    return ParseDragInputHandler(ref tokenStream, out memberNodeIndex, true, true);
                }

                case TemplateKeyword.Key: {
                    memberMask = MemberMask.InputHandler;
                    return ParseKeyInputHandler(ref tokenStream, out memberNodeIndex, true, true);
                }

                case TemplateKeyword.Method: {
                    memberMask = MemberMask.Method;
                    return ParseMethod(ref tokenStream, out memberNodeIndex, true);
                }

                case TemplateKeyword.Slot: {
                    memberMask = MemberMask.SlotDeclaration;
                    bool result = ParseSlotSignature(ref tokenStream, out memberNodeIndex);
                    if (result && requireSemiColons && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedSemiColon);
                    }

                    return result;
                }

                case TemplateKeyword.Render: {
                    memberMask = MemberMask.RenderBody;
                    return ParseRenderBody(ref tokenStream, out memberNodeIndex);
                }

                case TemplateKeyword.Base: {
                    memberMask = MemberMask.InvokeVariantBase;
                    return ParseVariantBase(ref tokenStream, out memberNodeIndex);
                }
            }

            memberMask = default;
            memberNodeIndex = default;
            return HardError(tokenStream, DiagnosticError.ExpectedMemberDeclaration, helpType);
        }

        private bool ParseOnChangeMember(ref TokenStream tokenStream, bool requireTerminatingSemiColon, out NodeIndex memberNodeIndex) {
            // : 'onChange' ':' identifier (from_mapping | '=>' expression) 
            memberNodeIndex = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.OnChange)) {
                return false;
            }

            if (!tokenStream.Consume(SymbolType.Colon)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedColon, HelpType.OnChangeSyntax);
            }

            if (!tokenStream.ConsumeStandardIdentifier(out NonTrivialTokenLocation identifier)) {
                return HardError(tokenStream.location, DiagnosticError.ExpectedIdentifier, HelpType.OnChangeSyntax);
            }

            NodeIndex<FromMapping> fromMapping = default;
            ExpressionIndex expressionIndex = default;

            if (tokenStream.Consume(TemplateKeyword.From)) {
                if (!ParseFromMapping(ref tokenStream, out fromMapping)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedIdentifier, HelpType.OnChangeSyntax);
                }
            }
            else if (tokenStream.Consume(SymbolType.FatArrow)) {
                // technically we don't want a block, just a statement list
                // emitted code is slightly worse because of this, but we can address that later, nbd 
                if (ParseBlock(ref tokenStream, out ExpressionIndex<BlockExpression> blockExpression)) {
                    expressionIndex = blockExpression;
                }
                else if (ParseExpression(ref tokenStream, out expressionIndex)) {
                    if (requireTerminatingSemiColon && !tokenStream.Consume(SymbolType.SemiColon)) {
                        return HardError(tokenStream, DiagnosticError.ExpectedTerminatingSemiColon);
                    }
                }
                else {
                    return HardError(tokenStream, DiagnosticError.ExpectedExpression, HelpType.OnChangeSyntax);
                }
            }
            else {
                return HardError(tokenStream, DiagnosticError.ExpectedFatArrowOrFromMapping, HelpType.OnChangeSyntax);
            }

            memberNodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new OnChangeBindingNode() {
                identifier = identifier,
                expression = expressionIndex,
                fromMapping = fromMapping
            });

            return true;
        }

        private bool ParseSpawnList(ref TokenStream tokenStream, out NodeIndex memberNodeIndex) {
            // : 'spawns' '[' qualified_identifier (',' qualified_identifier)* ']'
            int startLocation = tokenStream.location;
            memberNodeIndex = default;

            if (!tokenStream.Consume(TemplateKeyword.Spawns)) {
                return false;
            }

            if (!GetRequiredSubStream(ref tokenStream, SubStreamType.SquareBrackets, HelpType.SpawnsSyntax, out var spawnStream)) {
                return false;
            }

            if (!ParseTypedQualifiedIdentifierNode(ref spawnStream, out NodeIndex<TypedQualifiedIdentifierNode> identifier)) {
                return false;
            }

            using ScopedList<NodeIndex<TypedQualifiedIdentifierNode>> spawnList = scopedAllocator.CreateListScope<NodeIndex<TypedQualifiedIdentifierNode>>(8);

            spawnList.Add(identifier);

            while (spawnStream.Consume(SymbolType.Comma)) {
                if (!ParseTypedQualifiedIdentifierNode(ref spawnStream, out NodeIndex<TypedQualifiedIdentifierNode> next)) {
                    return false; // error already reported
                }

                spawnList.Add(next);
            }

            memberNodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new TemplateSpawnList() {
                spawnList = templateBuffer.AddNodeList(spawnList)
            });

            return true;
        }

        private bool ParseVariantBase(ref TokenStream tokenStream, out NodeIndex memberNodeIndex) {
            // : 'target' '=>' '(' expression_list ')' ';'
            throw new NotImplementedException();
        }

        private bool ParseRenderBody(ref TokenStream tokenStream, out NodeIndex memberNodeIndex) {
            if (!tokenStream.Consume(TemplateKeyword.Render)) {
                memberNodeIndex = default;
                return false;
            }

            bool retn = ParseTemplateBlockWithRecovery(ref tokenStream, false, out NodeIndex<TemplateBlockNode> blockNode);
            memberNodeIndex = blockNode;
            return retn;
        }

        private bool ParseMethod(ref TokenStream tokenStream, out NodeIndex methodNodeIndex, bool requireSemiColon) {
            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Method)) {
                methodNodeIndex = default;
                return false;
            }

            if (!ParseVisibilityModifier(ref tokenStream, out bool isPublic, HelpType.TemplateMethodSyntax)) {
                methodNodeIndex = default;
                return false;
            }

            bool isVoid = tokenStream.Consume(TemplateKeyword.Void);
            methodNodeIndex = default;

            if (isVoid) {
                return ParseMethodDefinition(ref tokenStream, ref methodNodeIndex, isPublic, startLocation, default, requireSemiColon);
            }

            if (!ParseTypePath(ref tokenStream, out ExpressionIndex<TypePath> path)) {
                return true;
            }

            return ParseMethodDefinition(ref tokenStream, ref methodNodeIndex, isPublic, startLocation, path, requireSemiColon);
        }

        private bool ParseStyle(ref TokenStream tokenStream, out NodeIndex memberNodeIndex) {
            memberNodeIndex = default;

            int startLocation = tokenStream.location;

            if (!tokenStream.Consume(TemplateKeyword.Style)) {
                return false;
            }

            if (tokenStream.Consume(SymbolType.Colon)) {
                if (!ParseInstanceStyle(ref tokenStream, false, out NodeIndex<InstanceStyleNode> instanceNode)) {
                    return HardError(tokenStream, DiagnosticError.ExpectedInstanceStyle);
                }

                memberNodeIndex = instanceNode;
                return true;
            }

            if (!tokenStream.Consume(SymbolType.Assign)) {
                return HardError(tokenStream, DiagnosticError.ExpectedEqualSign, HelpType.DecoratorStyleSyntax);
            }

            ParseStyleListWithRecovery(ref tokenStream, out ExpressionRange<ResolveIdExpression> styleList);

            memberNodeIndex = templateBuffer.Add(startLocation, tokenStream.location, new StyleListNode() {
                styleIds = styleList,
            });

            return true;
        }

    }

}
