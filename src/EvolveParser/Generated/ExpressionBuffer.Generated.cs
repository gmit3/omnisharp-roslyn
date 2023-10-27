using System;
using System.Diagnostics;
using EvolveUI.Util.Unsafe;
using System.Text;
using EvolveUI.Util;

namespace EvolveUI.Parsing {

    internal unsafe ref partial struct TemplateParser {
    
        private partial struct ExpressionBuffer : IDisposable {
    
            partial void CompactNodes(int expressionShift, int rangeShift, int nodeStart, int nodeEnd) {
                
                for(int i = nodeStart; i < nodeEnd; i++) {
                    UntypedExpressionNode* node = expressions.GetPointer(i);
                    switch(node->meta.type) {

                        case ExpressionNodeType.BlockExpression: {
                            BlockExpression* data = (BlockExpression*)node;
                            data->statementList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.ReturnStatement: {
                            ReturnStatement* data = (ReturnStatement*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.UsingStatement: {
                            UsingStatement* data = (UsingStatement*)node;
                            data->acquisition.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.IfStatement: {
                            IfStatement* data = (IfStatement*)node;
                            data->condition.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            data->elseBody.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.SwitchExpression: {
                            SwitchExpression* data = (SwitchExpression*)node;
                            data->lhs.id -= (ushort)expressionShift;
                            data->switchArms.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.SwitchStatement: {
                            SwitchStatement* data = (SwitchStatement*)node;
                            data->condition.id -= (ushort)expressionShift;
                            data->sections.start -= (ushort)rangeShift;
                            data->defaultBody.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.SwitchArm: {
                            SwitchArm* data = (SwitchArm*)node;
                            data->pattern.id -= (ushort)expressionShift;
                            data->guard.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.SwitchLabel: {
                            SwitchLabel* data = (SwitchLabel*)node;
                            data->caseExpression.id -= (ushort)expressionShift;
                            data->guardExpression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.SwitchSection: {
                            SwitchSection* data = (SwitchSection*)node;
                            data->labels.start -= (ushort)rangeShift;
                            data->bodyStatements.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.WhileLoop: {
                            WhileLoop* data = (WhileLoop*)node;
                            data->condition.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ForLoop: {
                            ForLoop* data = (ForLoop*)node;
                            data->initializer.id -= (ushort)expressionShift;
                            data->condition.id -= (ushort)expressionShift;
                            data->iterator.start -= (ushort)rangeShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ForeachLoop: {
                            ForeachLoop* data = (ForeachLoop*)node;
                            data->variableDeclaration.id -= (ushort)expressionShift;
                            data->enumerableExpression.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.GoToStatement: {
                            GoToStatement* data = (GoToStatement*)node;
                            data->caseJumpTarget.id -= (ushort)expressionShift;
                            data->labelTarget.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.YieldStatement: {
                            YieldStatement* data = (YieldStatement*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.TernaryExpression: {
                            TernaryExpression* data = (TernaryExpression*)node;
                            data->condition.id -= (ushort)expressionShift;
                            data->trueExpression.id -= (ushort)expressionShift;
                            data->falseExpression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.Argument: {
                            Argument* data = (Argument*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ThrowStatement: {
                            ThrowStatement* data = (ThrowStatement*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.RangeExpression: {
                            RangeExpression* data = (RangeExpression*)node;
                            data->lhs.id -= (ushort)expressionShift;
                            data->rhs.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.Catch: {
                            Catch* data = (Catch*)node;
                            data->exceptionFilter.id -= (ushort)expressionShift;
                            data->body.id -= (ushort)expressionShift;
                            data->typePath.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.TryCatchFinally: {
                            TryCatchFinally* data = (TryCatchFinally*)node;
                            data->tryBody.id -= (ushort)expressionShift;
                            data->catchClauses.start -= (ushort)rangeShift;
                            data->finallyClause.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.BinaryPattern: {
                            BinaryPattern* data = (BinaryPattern*)node;
                            data->lhs.id -= (ushort)expressionShift;
                            data->rhs.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.RelationalPattern: {
                            RelationalPattern* data = (RelationalPattern*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ConstantPattern: {
                            ConstantPattern* data = (ConstantPattern*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.VariableDesignation: {
                            VariableDesignation* data = (VariableDesignation*)node;
                            data->_designationList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.DeclarationPattern: {
                            DeclarationPattern* data = (DeclarationPattern*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->designation.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.VarPattern: {
                            VarPattern* data = (VarPattern*)node;
                            data->designation.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.TypePattern: {
                            TypePattern* data = (TypePattern*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.UnaryNotPattern: {
                            UnaryNotPattern* data = (UnaryNotPattern*)node;
                            data->pattern.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.LockStatement: {
                            LockStatement* data = (LockStatement*)node;
                            data->lockExpression.id -= (ushort)expressionShift;
                            data->bodyExpression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.BracketExpression: {
                            BracketExpression* data = (BracketExpression*)node;
                            data->arguments.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.DefaultExpression: {
                            DefaultExpression* data = (DefaultExpression*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.SizeOfExpression: {
                            SizeOfExpression* data = (SizeOfExpression*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.NameOfExpression: {
                            NameOfExpression* data = (NameOfExpression*)node;
                            data->identifier.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.BaseAccessExpression: {
                            BaseAccessExpression* data = (BaseAccessExpression*)node;
                            data->identifier.id -= (ushort)expressionShift;
                            data->indexExpressions.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.TupleExpression: {
                            TupleExpression* data = (TupleExpression*)node;
                            data->arguments.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.Identifier: {
                            Identifier* data = (Identifier*)node;
                            data->typeArgumentList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.TypePath: {
                            TypePath* data = (TypePath*)node;
                            data->baseTypePath.id -= (ushort)expressionShift;
                            data->modifiers.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.TypeNamePart: {
                            TypeNamePart* data = (TypeNamePart*)node;
                            data->_argumentList.start -= (ushort)rangeShift;
                            data->_partList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.DirectCast: {
                            DirectCast* data = (DirectCast*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.AssignmentExpression: {
                            AssignmentExpression* data = (AssignmentExpression*)node;
                            data->lhs.id -= (ushort)expressionShift;
                            data->rhs.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ParenExpression: {
                            ParenExpression* data = (ParenExpression*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.MethodInvocation: {
                            MethodInvocation* data = (MethodInvocation*)node;
                            data->argumentList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.PrimaryExpressionPart: {
                            PrimaryExpressionPart* data = (PrimaryExpressionPart*)node;
                            data->expression.id -= (ushort)expressionShift;
                            data->bracketExpressions.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.NewExpression: {
                            NewExpression* data = (NewExpression*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->argList.start -= (ushort)rangeShift;
                            data->arraySpecs.start -= (ushort)rangeShift;
                            data->initializer.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ArrayCreationRank: {
                            ArrayCreationRank* data = (ArrayCreationRank*)node;
                            data->expressionList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.PrimaryExpression: {
                            PrimaryExpression* data = (PrimaryExpression*)node;
                            data->start.id -= (ushort)expressionShift;
                            data->parts.start -= (ushort)rangeShift;
                            data->bracketExpressions.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.PrimaryIdentifier: {
                            PrimaryIdentifier* data = (PrimaryIdentifier*)node;
                            data->typeArgumentList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.UnboundTypeNameExpression: {
                            UnboundTypeNameExpression* data = (UnboundTypeNameExpression*)node;
                            data->_next.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.TypeOfExpression: {
                            TypeOfExpression* data = (TypeOfExpression*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->unboundTypeName.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.PrefixUnaryExpression: {
                            PrefixUnaryExpression* data = (PrefixUnaryExpression*)node;
                            data->expression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.LambdaExpression: {
                            LambdaExpression* data = (LambdaExpression*)node;
                            data->parameters.start -= (ushort)rangeShift;
                            data->body.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.BinaryExpression: {
                            BinaryExpression* data = (BinaryExpression*)node;
                            data->lhs.id -= (ushort)expressionShift;
                            data->rhs.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.IsTypeExpression: {
                            IsTypeExpression* data = (IsTypeExpression*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->typePatternArms.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.TypePatternArm: {
                            TypePatternArm* data = (TypePatternArm*)node;
                            data->expression.id -= (ushort)expressionShift;
                            data->identifier.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.TypeArgumentList: {
                            TypeArgumentList* data = (TypeArgumentList*)node;
                            data->typePaths.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.MemberAccess: {
                            MemberAccess* data = (MemberAccess*)node;
                            data->identifier.id -= (ushort)expressionShift;
                            data->argumentList.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ArrayInitializer: {
                            ArrayInitializer* data = (ArrayInitializer*)node;
                            data->initializers.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.LocalFunctionDefinition: {
                            LocalFunctionDefinition* data = (LocalFunctionDefinition*)node;
                            data->typeParameters.start -= (ushort)rangeShift;
                            data->body.id -= (ushort)expressionShift;
                            data->returnType.id -= (ushort)expressionShift;
                            data->parameters.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.VariableDeclaration: {
                            VariableDeclaration* data = (VariableDeclaration*)node;
                            data->typePath.id -= (ushort)expressionShift;
                            data->initializer.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.LiteralAccess: {
                            LiteralAccess* data = (LiteralAccess*)node;
                            data->access.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.StringInterpolation: {
                            StringInterpolation* data = (StringInterpolation*)node;
                            data->parts.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.StringInterpolationPart: {
                            StringInterpolationPart* data = (StringInterpolationPart*)node;
                            data->expression.id -= (ushort)expressionShift;
                            data->alignmentExpression.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.ElementInitializer: {
                            ElementInitializer* data = (ElementInitializer*)node;
                            data->expression.id -= (ushort)expressionShift;
                            data->expressionList.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.MemberInitializer: {
                            MemberInitializer* data = (MemberInitializer*)node;
                            data->lhsExpressionList.start -= (ushort)rangeShift;
                            data->rhs.id -= (ushort)expressionShift;
                            break;
                        }

                        case ExpressionNodeType.CollectionInitializer: {
                            CollectionInitializer* data = (CollectionInitializer*)node;
                            data->initializers.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.ObjectInitializer: {
                            ObjectInitializer* data = (ObjectInitializer*)node;
                            data->memberInit.start -= (ushort)rangeShift;
                            break;
                        }

                        case ExpressionNodeType.Parameter: {
                            Parameter* data = (Parameter*)node;
                            data->defaultExpression.id -= (ushort)expressionShift;
                            data->typePath.id -= (ushort)expressionShift;
                            break;
                        }

                    }
                }
                
            }
            
        }
        
    }
    
    internal unsafe partial struct ExpressionPrintingVisitor {
        
        partial void VisitImpl(IndentedStringBuilder builder, ExpressionIndex index) {
            
             if(index.id <= 0) return;
             
             UntypedExpressionNode node = tree->Get(index);
             
             switch (node.meta.type) {

                case ExpressionNodeType.BlockExpression: {
                    BlockExpression* data = (BlockExpression*)&node;
                    builder.Append("BlockExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->statementList.start, data->statementList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ReturnStatement: {
                    ReturnStatement* data = (ReturnStatement*)&node;
                    builder.Append("ReturnStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.UsingStatement: {
                    UsingStatement* data = (UsingStatement*)&node;
                    builder.Append("UsingStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->acquisition);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.IfStatement: {
                    IfStatement* data = (IfStatement*)&node;
                    builder.Append("IfStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->condition);
                    VisitImpl(builder, data->body);
                    VisitImpl(builder, data->elseBody);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SwitchExpression: {
                    SwitchExpression* data = (SwitchExpression*)&node;
                    builder.Append("SwitchExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lhs);
                    VisitRange(builder, data->switchArms.start, data->switchArms.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SwitchStatement: {
                    SwitchStatement* data = (SwitchStatement*)&node;
                    builder.Append("SwitchStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->condition);
                    VisitRange(builder, data->sections.start, data->sections.length);
                    VisitRange(builder, data->defaultBody.start, data->defaultBody.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SwitchArm: {
                    SwitchArm* data = (SwitchArm*)&node;
                    builder.Append("SwitchArm");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->pattern);
                    VisitImpl(builder, data->guard);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SwitchLabel: {
                    SwitchLabel* data = (SwitchLabel*)&node;
                    builder.Append("SwitchLabel");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->caseExpression);
                    VisitImpl(builder, data->guardExpression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SwitchSection: {
                    SwitchSection* data = (SwitchSection*)&node;
                    builder.Append("SwitchSection");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->labels.start, data->labels.length);
                    VisitRange(builder, data->bodyStatements.start, data->bodyStatements.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.WhileLoop: {
                    WhileLoop* data = (WhileLoop*)&node;
                    builder.Append("WhileLoop");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->condition);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ForLoop: {
                    ForLoop* data = (ForLoop*)&node;
                    builder.Append("ForLoop");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->initializer);
                    VisitImpl(builder, data->condition);
                    VisitRange(builder, data->iterator.start, data->iterator.length);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ForeachLoop: {
                    ForeachLoop* data = (ForeachLoop*)&node;
                    builder.Append("ForeachLoop");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->variableDeclaration);
                    VisitImpl(builder, data->enumerableExpression);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.BreakStatement: {
                    BreakStatement* data = (BreakStatement*)&node;
                    builder.Append("BreakStatement");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.ContinueStatement: {
                    ContinueStatement* data = (ContinueStatement*)&node;
                    builder.Append("ContinueStatement");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.GoToStatement: {
                    GoToStatement* data = (GoToStatement*)&node;
                    builder.Append("GoToStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->caseJumpTarget);
                    VisitImpl(builder, data->labelTarget);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.YieldStatement: {
                    YieldStatement* data = (YieldStatement*)&node;
                    builder.Append("YieldStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TernaryExpression: {
                    TernaryExpression* data = (TernaryExpression*)&node;
                    builder.Append("TernaryExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->condition);
                    VisitImpl(builder, data->trueExpression);
                    VisitImpl(builder, data->falseExpression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.Argument: {
                    Argument* data = (Argument*)&node;
                    builder.Append("Argument");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ThrowStatement: {
                    ThrowStatement* data = (ThrowStatement*)&node;
                    builder.Append("ThrowStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.RangeExpression: {
                    RangeExpression* data = (RangeExpression*)&node;
                    builder.Append("RangeExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lhs);
                    VisitImpl(builder, data->rhs);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.Catch: {
                    Catch* data = (Catch*)&node;
                    builder.Append("Catch");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->exceptionFilter);
                    VisitImpl(builder, data->body);
                    VisitImpl(builder, data->typePath);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TryCatchFinally: {
                    TryCatchFinally* data = (TryCatchFinally*)&node;
                    builder.Append("TryCatchFinally");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->tryBody);
                    VisitRange(builder, data->catchClauses.start, data->catchClauses.length);
                    VisitImpl(builder, data->finallyClause);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.BinaryPattern: {
                    BinaryPattern* data = (BinaryPattern*)&node;
                    builder.Append("BinaryPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lhs);
                    VisitImpl(builder, data->rhs);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.RelationalPattern: {
                    RelationalPattern* data = (RelationalPattern*)&node;
                    builder.Append("RelationalPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.DiscardPattern: {
                    DiscardPattern* data = (DiscardPattern*)&node;
                    builder.Append("DiscardPattern");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.ConstantPattern: {
                    ConstantPattern* data = (ConstantPattern*)&node;
                    builder.Append("ConstantPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.VariableDesignation: {
                    VariableDesignation* data = (VariableDesignation*)&node;
                    builder.Append("VariableDesignation");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->designationList.start, data->designationList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.DeclarationPattern: {
                    DeclarationPattern* data = (DeclarationPattern*)&node;
                    builder.Append("DeclarationPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitImpl(builder, data->designation);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.VarPattern: {
                    VarPattern* data = (VarPattern*)&node;
                    builder.Append("VarPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->designation);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TypePattern: {
                    TypePattern* data = (TypePattern*)&node;
                    builder.Append("TypePattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.UnaryNotPattern: {
                    UnaryNotPattern* data = (UnaryNotPattern*)&node;
                    builder.Append("UnaryNotPattern");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->pattern);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.LockStatement: {
                    LockStatement* data = (LockStatement*)&node;
                    builder.Append("LockStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lockExpression);
                    VisitImpl(builder, data->bodyExpression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.BracketExpression: {
                    BracketExpression* data = (BracketExpression*)&node;
                    builder.Append("BracketExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->arguments.start, data->arguments.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.DefaultExpression: {
                    DefaultExpression* data = (DefaultExpression*)&node;
                    builder.Append("DefaultExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.SizeOfExpression: {
                    SizeOfExpression* data = (SizeOfExpression*)&node;
                    builder.Append("SizeOfExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.NameOfExpression: {
                    NameOfExpression* data = (NameOfExpression*)&node;
                    builder.Append("NameOfExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->identifier);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.AnonymousMethodExpression: {
                    AnonymousMethodExpression* data = (AnonymousMethodExpression*)&node;
                    builder.Append("AnonymousMethodExpression");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.CheckedExpression: {
                    CheckedExpression* data = (CheckedExpression*)&node;
                    builder.Append("CheckedExpression");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.BaseAccessExpression: {
                    BaseAccessExpression* data = (BaseAccessExpression*)&node;
                    builder.Append("BaseAccessExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->identifier);
                    VisitRange(builder, data->indexExpressions.start, data->indexExpressions.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TupleExpression: {
                    TupleExpression* data = (TupleExpression*)&node;
                    builder.Append("TupleExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->arguments.start, data->arguments.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.Identifier: {
                    Identifier* data = (Identifier*)&node;
                    builder.Append("Identifier");
                    data->Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->typeArgumentList.start, data->typeArgumentList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TypeModifier: {
                    TypeModifier* data = (TypeModifier*)&node;
                    builder.Append("TypeModifier");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.TypePath: {
                    TypePath* data = (TypePath*)&node;
                    builder.Append("TypePath");
                    data->Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->baseTypePath);
                    VisitRange(builder, data->modifiers.start, data->modifiers.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TypeNamePart: {
                    TypeNamePart* data = (TypeNamePart*)&node;
                    builder.Append("TypeNamePart");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->argumentList.start, data->argumentList.length);
                    VisitRange(builder, data->partList.start, data->partList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.DirectCast: {
                    DirectCast* data = (DirectCast*)&node;
                    builder.Append("DirectCast");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.AssignmentExpression: {
                    AssignmentExpression* data = (AssignmentExpression*)&node;
                    builder.Append("AssignmentExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lhs);
                    VisitImpl(builder, data->rhs);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ParenExpression: {
                    ParenExpression* data = (ParenExpression*)&node;
                    builder.Append("ParenExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.MethodInvocation: {
                    MethodInvocation* data = (MethodInvocation*)&node;
                    builder.Append("MethodInvocation");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->argumentList.start, data->argumentList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.IncrementDecrement: {
                    IncrementDecrement* data = (IncrementDecrement*)&node;
                    builder.Append("IncrementDecrement");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.PrimaryExpressionPart: {
                    PrimaryExpressionPart* data = (PrimaryExpressionPart*)&node;
                    builder.Append("PrimaryExpressionPart");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    VisitRange(builder, data->bracketExpressions.start, data->bracketExpressions.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.NewExpression: {
                    NewExpression* data = (NewExpression*)&node;
                    builder.Append("NewExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitRange(builder, data->argList.start, data->argList.length);
                    VisitRange(builder, data->arraySpecs.start, data->arraySpecs.length);
                    VisitImpl(builder, data->initializer);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ArrayCreationRank: {
                    ArrayCreationRank* data = (ArrayCreationRank*)&node;
                    builder.Append("ArrayCreationRank");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->expressionList.start, data->expressionList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.PrimaryExpression: {
                    PrimaryExpression* data = (PrimaryExpression*)&node;
                    builder.Append("PrimaryExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->start);
                    VisitRange(builder, data->parts.start, data->parts.length);
                    VisitRange(builder, data->bracketExpressions.start, data->bracketExpressions.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.PrimaryIdentifier: {
                    PrimaryIdentifier* data = (PrimaryIdentifier*)&node;
                    builder.Append("PrimaryIdentifier");
                    data->Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->typeArgumentList.start, data->typeArgumentList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.UnboundTypeNameExpression: {
                    UnboundTypeNameExpression* data = (UnboundTypeNameExpression*)&node;
                    builder.Append("UnboundTypeNameExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->next);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TypeOfExpression: {
                    TypeOfExpression* data = (TypeOfExpression*)&node;
                    builder.Append("TypeOfExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitRange(builder, data->unboundTypeName.start, data->unboundTypeName.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.PrefixUnaryExpression: {
                    PrefixUnaryExpression* data = (PrefixUnaryExpression*)&node;
                    builder.Append("PrefixUnaryExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.LambdaExpression: {
                    LambdaExpression* data = (LambdaExpression*)&node;
                    builder.Append("LambdaExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->parameters.start, data->parameters.length);
                    VisitImpl(builder, data->body);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.BinaryExpression: {
                    BinaryExpression* data = (BinaryExpression*)&node;
                    builder.Append("BinaryExpression");
                    data->Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->lhs);
                    VisitImpl(builder, data->rhs);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.IsTypeExpression: {
                    IsTypeExpression* data = (IsTypeExpression*)&node;
                    builder.Append("IsTypeExpression");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitRange(builder, data->typePatternArms.start, data->typePatternArms.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.IsNullExpression: {
                    IsNullExpression* data = (IsNullExpression*)&node;
                    builder.Append("IsNullExpression");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.TypePatternArm: {
                    TypePatternArm* data = (TypePatternArm*)&node;
                    builder.Append("TypePatternArm");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    VisitImpl(builder, data->identifier);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.TypeArgumentList: {
                    TypeArgumentList* data = (TypeArgumentList*)&node;
                    builder.Append("TypeArgumentList");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->typePaths.start, data->typePaths.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.MemberAccess: {
                    MemberAccess* data = (MemberAccess*)&node;
                    builder.Append("MemberAccess");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->identifier);
                    VisitImpl(builder, data->argumentList);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ArrayInitializer: {
                    ArrayInitializer* data = (ArrayInitializer*)&node;
                    builder.Append("ArrayInitializer");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->initializers.start, data->initializers.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.LocalFunctionDefinition: {
                    LocalFunctionDefinition* data = (LocalFunctionDefinition*)&node;
                    builder.Append("LocalFunctionDefinition");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->typeParameters.start, data->typeParameters.length);
                    VisitImpl(builder, data->body);
                    VisitImpl(builder, data->returnType);
                    VisitRange(builder, data->parameters.start, data->parameters.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.VariableDeclaration: {
                    VariableDeclaration* data = (VariableDeclaration*)&node;
                    builder.Append("VariableDeclaration");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->typePath);
                    VisitImpl(builder, data->initializer);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.LiteralAccess: {
                    LiteralAccess* data = (LiteralAccess*)&node;
                    builder.Append("LiteralAccess");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->access);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.Literal: {
                    Literal* data = (Literal*)&node;
                    builder.Append("Literal");
                    data->Print(builder, tree, index);
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.StringInterpolation: {
                    StringInterpolation* data = (StringInterpolation*)&node;
                    builder.Append("StringInterpolation");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->parts.start, data->parts.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.StringInterpolationPart: {
                    StringInterpolationPart* data = (StringInterpolationPart*)&node;
                    builder.Append("StringInterpolationPart");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    VisitImpl(builder, data->alignmentExpression);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ElementInitializer: {
                    ElementInitializer* data = (ElementInitializer*)&node;
                    builder.Append("ElementInitializer");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->expression);
                    VisitRange(builder, data->expressionList.start, data->expressionList.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.MemberInitializer: {
                    MemberInitializer* data = (MemberInitializer*)&node;
                    builder.Append("MemberInitializer");
                    data->Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->lhsExpressionList.start, data->lhsExpressionList.length);
                    VisitImpl(builder, data->rhs);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.CollectionInitializer: {
                    CollectionInitializer* data = (CollectionInitializer*)&node;
                    builder.Append("CollectionInitializer");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->initializers.start, data->initializers.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ObjectInitializer: {
                    ObjectInitializer* data = (ObjectInitializer*)&node;
                    builder.Append("ObjectInitializer");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data->memberInit.start, data->memberInit.length);
                    builder.Outdent();
                    break;
                }

                case ExpressionNodeType.ResolveIdExpression: {
                    ResolveIdExpression* data = (ResolveIdExpression*)&node;
                    builder.Append("ResolveIdExpression");
                    builder.NewLine();
                    break;
                }

                case ExpressionNodeType.Parameter: {
                    Parameter* data = (Parameter*)&node;
                    builder.Append("Parameter");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data->defaultExpression);
                    VisitImpl(builder, data->typePath);
                    builder.Outdent();
                    break;
                }
                 
             }
        
        }
    }
    
    internal partial interface IExpressionVisitor {

        VisitorAction VisitBlockExpression(BlockExpression node);

        VisitorAction VisitReturnStatement(ReturnStatement node);

        VisitorAction VisitUsingStatement(UsingStatement node);

        VisitorAction VisitIfStatement(IfStatement node);

        VisitorAction VisitSwitchExpression(SwitchExpression node);

        VisitorAction VisitSwitchStatement(SwitchStatement node);

        VisitorAction VisitSwitchArm(SwitchArm node);

        VisitorAction VisitSwitchLabel(SwitchLabel node);

        VisitorAction VisitSwitchSection(SwitchSection node);

        VisitorAction VisitWhileLoop(WhileLoop node);

        VisitorAction VisitForLoop(ForLoop node);

        VisitorAction VisitForeachLoop(ForeachLoop node);

        VisitorAction VisitBreakStatement(BreakStatement node);

        VisitorAction VisitContinueStatement(ContinueStatement node);

        VisitorAction VisitGoToStatement(GoToStatement node);

        VisitorAction VisitYieldStatement(YieldStatement node);

        VisitorAction VisitTernaryExpression(TernaryExpression node);

        VisitorAction VisitArgument(Argument node);

        VisitorAction VisitThrowStatement(ThrowStatement node);

        VisitorAction VisitRangeExpression(RangeExpression node);

        VisitorAction VisitCatch(Catch node);

        VisitorAction VisitTryCatchFinally(TryCatchFinally node);

        VisitorAction VisitBinaryPattern(BinaryPattern node);

        VisitorAction VisitRelationalPattern(RelationalPattern node);

        VisitorAction VisitDiscardPattern(DiscardPattern node);

        VisitorAction VisitConstantPattern(ConstantPattern node);

        VisitorAction VisitVariableDesignation(VariableDesignation node);

        VisitorAction VisitDeclarationPattern(DeclarationPattern node);

        VisitorAction VisitVarPattern(VarPattern node);

        VisitorAction VisitTypePattern(TypePattern node);

        VisitorAction VisitUnaryNotPattern(UnaryNotPattern node);

        VisitorAction VisitLockStatement(LockStatement node);

        VisitorAction VisitBracketExpression(BracketExpression node);

        VisitorAction VisitDefaultExpression(DefaultExpression node);

        VisitorAction VisitSizeOfExpression(SizeOfExpression node);

        VisitorAction VisitNameOfExpression(NameOfExpression node);

        VisitorAction VisitAnonymousMethodExpression(AnonymousMethodExpression node);

        VisitorAction VisitCheckedExpression(CheckedExpression node);

        VisitorAction VisitBaseAccessExpression(BaseAccessExpression node);

        VisitorAction VisitTupleExpression(TupleExpression node);

        VisitorAction VisitIdentifier(Identifier node);

        VisitorAction VisitTypeModifier(TypeModifier node);

        VisitorAction VisitTypePath(TypePath node);

        VisitorAction VisitTypeNamePart(TypeNamePart node);

        VisitorAction VisitDirectCast(DirectCast node);

        VisitorAction VisitAssignmentExpression(AssignmentExpression node);

        VisitorAction VisitParenExpression(ParenExpression node);

        VisitorAction VisitMethodInvocation(MethodInvocation node);

        VisitorAction VisitIncrementDecrement(IncrementDecrement node);

        VisitorAction VisitPrimaryExpressionPart(PrimaryExpressionPart node);

        VisitorAction VisitNewExpression(NewExpression node);

        VisitorAction VisitArrayCreationRank(ArrayCreationRank node);

        VisitorAction VisitPrimaryExpression(PrimaryExpression node);

        VisitorAction VisitPrimaryIdentifier(PrimaryIdentifier node);

        VisitorAction VisitUnboundTypeNameExpression(UnboundTypeNameExpression node);

        VisitorAction VisitTypeOfExpression(TypeOfExpression node);

        VisitorAction VisitPrefixUnaryExpression(PrefixUnaryExpression node);

        VisitorAction VisitLambdaExpression(LambdaExpression node);

        VisitorAction VisitBinaryExpression(BinaryExpression node);

        VisitorAction VisitIsTypeExpression(IsTypeExpression node);

        VisitorAction VisitIsNullExpression(IsNullExpression node);

        VisitorAction VisitTypePatternArm(TypePatternArm node);

        VisitorAction VisitTypeArgumentList(TypeArgumentList node);

        VisitorAction VisitMemberAccess(MemberAccess node);

        VisitorAction VisitArrayInitializer(ArrayInitializer node);

        VisitorAction VisitLocalFunctionDefinition(LocalFunctionDefinition node);

        VisitorAction VisitVariableDeclaration(VariableDeclaration node);

        VisitorAction VisitLiteralAccess(LiteralAccess node);

        VisitorAction VisitLiteral(Literal node);

        VisitorAction VisitStringInterpolation(StringInterpolation node);

        VisitorAction VisitStringInterpolationPart(StringInterpolationPart node);

        VisitorAction VisitElementInitializer(ElementInitializer node);

        VisitorAction VisitMemberInitializer(MemberInitializer node);

        VisitorAction VisitCollectionInitializer(CollectionInitializer node);

        VisitorAction VisitObjectInitializer(ObjectInitializer node);

        VisitorAction VisitResolveIdExpression(ResolveIdExpression node);

        VisitorAction VisitParameter(Parameter node);

    }
        
    internal unsafe partial struct ExpressionTree {
            
        partial void VisitImpl(Action<UntypedExpressionNode> action, ExpressionIndex index) {
            
             if(index.id <= 0) return;
             
             UntypedExpressionNode node = Get(index);
             
             switch (node.meta.type) {

                case ExpressionNodeType.BlockExpression: {
                    BlockExpression* data = (BlockExpression*)&node;
                    action(node);
                    VisitRange(action, data->statementList.start, data->statementList.length);
                    break;
                }

                case ExpressionNodeType.ReturnStatement: {
                    ReturnStatement* data = (ReturnStatement*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.UsingStatement: {
                    UsingStatement* data = (UsingStatement*)&node;
                    action(node);
                    VisitImpl(action, data->acquisition);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.IfStatement: {
                    IfStatement* data = (IfStatement*)&node;
                    action(node);
                    VisitImpl(action, data->condition);
                    VisitImpl(action, data->body);
                    VisitImpl(action, data->elseBody);
                    break;
                }

                case ExpressionNodeType.SwitchExpression: {
                    SwitchExpression* data = (SwitchExpression*)&node;
                    action(node);
                    VisitImpl(action, data->lhs);
                    VisitRange(action, data->switchArms.start, data->switchArms.length);
                    break;
                }

                case ExpressionNodeType.SwitchStatement: {
                    SwitchStatement* data = (SwitchStatement*)&node;
                    action(node);
                    VisitImpl(action, data->condition);
                    VisitRange(action, data->sections.start, data->sections.length);
                    VisitRange(action, data->defaultBody.start, data->defaultBody.length);
                    break;
                }

                case ExpressionNodeType.SwitchArm: {
                    SwitchArm* data = (SwitchArm*)&node;
                    action(node);
                    VisitImpl(action, data->pattern);
                    VisitImpl(action, data->guard);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.SwitchLabel: {
                    SwitchLabel* data = (SwitchLabel*)&node;
                    action(node);
                    VisitImpl(action, data->caseExpression);
                    VisitImpl(action, data->guardExpression);
                    break;
                }

                case ExpressionNodeType.SwitchSection: {
                    SwitchSection* data = (SwitchSection*)&node;
                    action(node);
                    VisitRange(action, data->labels.start, data->labels.length);
                    VisitRange(action, data->bodyStatements.start, data->bodyStatements.length);
                    break;
                }

                case ExpressionNodeType.WhileLoop: {
                    WhileLoop* data = (WhileLoop*)&node;
                    action(node);
                    VisitImpl(action, data->condition);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.ForLoop: {
                    ForLoop* data = (ForLoop*)&node;
                    action(node);
                    VisitImpl(action, data->initializer);
                    VisitImpl(action, data->condition);
                    VisitRange(action, data->iterator.start, data->iterator.length);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.ForeachLoop: {
                    ForeachLoop* data = (ForeachLoop*)&node;
                    action(node);
                    VisitImpl(action, data->variableDeclaration);
                    VisitImpl(action, data->enumerableExpression);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.BreakStatement: {
                    BreakStatement* data = (BreakStatement*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.ContinueStatement: {
                    ContinueStatement* data = (ContinueStatement*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.GoToStatement: {
                    GoToStatement* data = (GoToStatement*)&node;
                    action(node);
                    VisitImpl(action, data->caseJumpTarget);
                    VisitImpl(action, data->labelTarget);
                    break;
                }

                case ExpressionNodeType.YieldStatement: {
                    YieldStatement* data = (YieldStatement*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.TernaryExpression: {
                    TernaryExpression* data = (TernaryExpression*)&node;
                    action(node);
                    VisitImpl(action, data->condition);
                    VisitImpl(action, data->trueExpression);
                    VisitImpl(action, data->falseExpression);
                    break;
                }

                case ExpressionNodeType.Argument: {
                    Argument* data = (Argument*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.ThrowStatement: {
                    ThrowStatement* data = (ThrowStatement*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.RangeExpression: {
                    RangeExpression* data = (RangeExpression*)&node;
                    action(node);
                    VisitImpl(action, data->lhs);
                    VisitImpl(action, data->rhs);
                    break;
                }

                case ExpressionNodeType.Catch: {
                    Catch* data = (Catch*)&node;
                    action(node);
                    VisitImpl(action, data->exceptionFilter);
                    VisitImpl(action, data->body);
                    VisitImpl(action, data->typePath);
                    break;
                }

                case ExpressionNodeType.TryCatchFinally: {
                    TryCatchFinally* data = (TryCatchFinally*)&node;
                    action(node);
                    VisitImpl(action, data->tryBody);
                    VisitRange(action, data->catchClauses.start, data->catchClauses.length);
                    VisitImpl(action, data->finallyClause);
                    break;
                }

                case ExpressionNodeType.BinaryPattern: {
                    BinaryPattern* data = (BinaryPattern*)&node;
                    action(node);
                    VisitImpl(action, data->lhs);
                    VisitImpl(action, data->rhs);
                    break;
                }

                case ExpressionNodeType.RelationalPattern: {
                    RelationalPattern* data = (RelationalPattern*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.DiscardPattern: {
                    DiscardPattern* data = (DiscardPattern*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.ConstantPattern: {
                    ConstantPattern* data = (ConstantPattern*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.VariableDesignation: {
                    VariableDesignation* data = (VariableDesignation*)&node;
                    action(node);
                    VisitRange(action, data->designationList.start, data->designationList.length);
                    break;
                }

                case ExpressionNodeType.DeclarationPattern: {
                    DeclarationPattern* data = (DeclarationPattern*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitImpl(action, data->designation);
                    break;
                }

                case ExpressionNodeType.VarPattern: {
                    VarPattern* data = (VarPattern*)&node;
                    action(node);
                    VisitImpl(action, data->designation);
                    break;
                }

                case ExpressionNodeType.TypePattern: {
                    TypePattern* data = (TypePattern*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    break;
                }

                case ExpressionNodeType.UnaryNotPattern: {
                    UnaryNotPattern* data = (UnaryNotPattern*)&node;
                    action(node);
                    VisitImpl(action, data->pattern);
                    break;
                }

                case ExpressionNodeType.LockStatement: {
                    LockStatement* data = (LockStatement*)&node;
                    action(node);
                    VisitImpl(action, data->lockExpression);
                    VisitImpl(action, data->bodyExpression);
                    break;
                }

                case ExpressionNodeType.BracketExpression: {
                    BracketExpression* data = (BracketExpression*)&node;
                    action(node);
                    VisitRange(action, data->arguments.start, data->arguments.length);
                    break;
                }

                case ExpressionNodeType.DefaultExpression: {
                    DefaultExpression* data = (DefaultExpression*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    break;
                }

                case ExpressionNodeType.SizeOfExpression: {
                    SizeOfExpression* data = (SizeOfExpression*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    break;
                }

                case ExpressionNodeType.NameOfExpression: {
                    NameOfExpression* data = (NameOfExpression*)&node;
                    action(node);
                    VisitImpl(action, data->identifier);
                    break;
                }

                case ExpressionNodeType.AnonymousMethodExpression: {
                    AnonymousMethodExpression* data = (AnonymousMethodExpression*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.CheckedExpression: {
                    CheckedExpression* data = (CheckedExpression*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.BaseAccessExpression: {
                    BaseAccessExpression* data = (BaseAccessExpression*)&node;
                    action(node);
                    VisitImpl(action, data->identifier);
                    VisitRange(action, data->indexExpressions.start, data->indexExpressions.length);
                    break;
                }

                case ExpressionNodeType.TupleExpression: {
                    TupleExpression* data = (TupleExpression*)&node;
                    action(node);
                    VisitRange(action, data->arguments.start, data->arguments.length);
                    break;
                }

                case ExpressionNodeType.Identifier: {
                    Identifier* data = (Identifier*)&node;
                    action(node);
                    VisitRange(action, data->typeArgumentList.start, data->typeArgumentList.length);
                    break;
                }

                case ExpressionNodeType.TypeModifier: {
                    TypeModifier* data = (TypeModifier*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.TypePath: {
                    TypePath* data = (TypePath*)&node;
                    action(node);
                    VisitImpl(action, data->baseTypePath);
                    VisitRange(action, data->modifiers.start, data->modifiers.length);
                    break;
                }

                case ExpressionNodeType.TypeNamePart: {
                    TypeNamePart* data = (TypeNamePart*)&node;
                    action(node);
                    VisitRange(action, data->argumentList.start, data->argumentList.length);
                    VisitRange(action, data->partList.start, data->partList.length);
                    break;
                }

                case ExpressionNodeType.DirectCast: {
                    DirectCast* data = (DirectCast*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.AssignmentExpression: {
                    AssignmentExpression* data = (AssignmentExpression*)&node;
                    action(node);
                    VisitImpl(action, data->lhs);
                    VisitImpl(action, data->rhs);
                    break;
                }

                case ExpressionNodeType.ParenExpression: {
                    ParenExpression* data = (ParenExpression*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.MethodInvocation: {
                    MethodInvocation* data = (MethodInvocation*)&node;
                    action(node);
                    VisitRange(action, data->argumentList.start, data->argumentList.length);
                    break;
                }

                case ExpressionNodeType.IncrementDecrement: {
                    IncrementDecrement* data = (IncrementDecrement*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.PrimaryExpressionPart: {
                    PrimaryExpressionPart* data = (PrimaryExpressionPart*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    VisitRange(action, data->bracketExpressions.start, data->bracketExpressions.length);
                    break;
                }

                case ExpressionNodeType.NewExpression: {
                    NewExpression* data = (NewExpression*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitRange(action, data->argList.start, data->argList.length);
                    VisitRange(action, data->arraySpecs.start, data->arraySpecs.length);
                    VisitImpl(action, data->initializer);
                    break;
                }

                case ExpressionNodeType.ArrayCreationRank: {
                    ArrayCreationRank* data = (ArrayCreationRank*)&node;
                    action(node);
                    VisitRange(action, data->expressionList.start, data->expressionList.length);
                    break;
                }

                case ExpressionNodeType.PrimaryExpression: {
                    PrimaryExpression* data = (PrimaryExpression*)&node;
                    action(node);
                    VisitImpl(action, data->start);
                    VisitRange(action, data->parts.start, data->parts.length);
                    VisitRange(action, data->bracketExpressions.start, data->bracketExpressions.length);
                    break;
                }

                case ExpressionNodeType.PrimaryIdentifier: {
                    PrimaryIdentifier* data = (PrimaryIdentifier*)&node;
                    action(node);
                    VisitRange(action, data->typeArgumentList.start, data->typeArgumentList.length);
                    break;
                }

                case ExpressionNodeType.UnboundTypeNameExpression: {
                    UnboundTypeNameExpression* data = (UnboundTypeNameExpression*)&node;
                    action(node);
                    VisitImpl(action, data->next);
                    break;
                }

                case ExpressionNodeType.TypeOfExpression: {
                    TypeOfExpression* data = (TypeOfExpression*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitRange(action, data->unboundTypeName.start, data->unboundTypeName.length);
                    break;
                }

                case ExpressionNodeType.PrefixUnaryExpression: {
                    PrefixUnaryExpression* data = (PrefixUnaryExpression*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    break;
                }

                case ExpressionNodeType.LambdaExpression: {
                    LambdaExpression* data = (LambdaExpression*)&node;
                    action(node);
                    VisitRange(action, data->parameters.start, data->parameters.length);
                    VisitImpl(action, data->body);
                    break;
                }

                case ExpressionNodeType.BinaryExpression: {
                    BinaryExpression* data = (BinaryExpression*)&node;
                    action(node);
                    VisitImpl(action, data->lhs);
                    VisitImpl(action, data->rhs);
                    break;
                }

                case ExpressionNodeType.IsTypeExpression: {
                    IsTypeExpression* data = (IsTypeExpression*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitRange(action, data->typePatternArms.start, data->typePatternArms.length);
                    break;
                }

                case ExpressionNodeType.IsNullExpression: {
                    IsNullExpression* data = (IsNullExpression*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.TypePatternArm: {
                    TypePatternArm* data = (TypePatternArm*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    VisitImpl(action, data->identifier);
                    break;
                }

                case ExpressionNodeType.TypeArgumentList: {
                    TypeArgumentList* data = (TypeArgumentList*)&node;
                    action(node);
                    VisitRange(action, data->typePaths.start, data->typePaths.length);
                    break;
                }

                case ExpressionNodeType.MemberAccess: {
                    MemberAccess* data = (MemberAccess*)&node;
                    action(node);
                    VisitImpl(action, data->identifier);
                    VisitImpl(action, data->argumentList);
                    break;
                }

                case ExpressionNodeType.ArrayInitializer: {
                    ArrayInitializer* data = (ArrayInitializer*)&node;
                    action(node);
                    VisitRange(action, data->initializers.start, data->initializers.length);
                    break;
                }

                case ExpressionNodeType.LocalFunctionDefinition: {
                    LocalFunctionDefinition* data = (LocalFunctionDefinition*)&node;
                    action(node);
                    VisitRange(action, data->typeParameters.start, data->typeParameters.length);
                    VisitImpl(action, data->body);
                    VisitImpl(action, data->returnType);
                    VisitRange(action, data->parameters.start, data->parameters.length);
                    break;
                }

                case ExpressionNodeType.VariableDeclaration: {
                    VariableDeclaration* data = (VariableDeclaration*)&node;
                    action(node);
                    VisitImpl(action, data->typePath);
                    VisitImpl(action, data->initializer);
                    break;
                }

                case ExpressionNodeType.LiteralAccess: {
                    LiteralAccess* data = (LiteralAccess*)&node;
                    action(node);
                    VisitImpl(action, data->access);
                    break;
                }

                case ExpressionNodeType.Literal: {
                    Literal* data = (Literal*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.StringInterpolation: {
                    StringInterpolation* data = (StringInterpolation*)&node;
                    action(node);
                    VisitRange(action, data->parts.start, data->parts.length);
                    break;
                }

                case ExpressionNodeType.StringInterpolationPart: {
                    StringInterpolationPart* data = (StringInterpolationPart*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    VisitImpl(action, data->alignmentExpression);
                    break;
                }

                case ExpressionNodeType.ElementInitializer: {
                    ElementInitializer* data = (ElementInitializer*)&node;
                    action(node);
                    VisitImpl(action, data->expression);
                    VisitRange(action, data->expressionList.start, data->expressionList.length);
                    break;
                }

                case ExpressionNodeType.MemberInitializer: {
                    MemberInitializer* data = (MemberInitializer*)&node;
                    action(node);
                    VisitRange(action, data->lhsExpressionList.start, data->lhsExpressionList.length);
                    VisitImpl(action, data->rhs);
                    break;
                }

                case ExpressionNodeType.CollectionInitializer: {
                    CollectionInitializer* data = (CollectionInitializer*)&node;
                    action(node);
                    VisitRange(action, data->initializers.start, data->initializers.length);
                    break;
                }

                case ExpressionNodeType.ObjectInitializer: {
                    ObjectInitializer* data = (ObjectInitializer*)&node;
                    action(node);
                    VisitRange(action, data->memberInit.start, data->memberInit.length);
                    break;
                }

                case ExpressionNodeType.ResolveIdExpression: {
                    ResolveIdExpression* data = (ResolveIdExpression*)&node;
                    action(node);
                    break;
                }

                case ExpressionNodeType.Parameter: {
                    Parameter* data = (Parameter*)&node;
                    action(node);
                    VisitImpl(action, data->defaultExpression);
                    VisitImpl(action, data->typePath);
                    break;
                }
                 
             }
        
        }
        
        partial void InterfaceVisitImpl<T>(ref T visitor, ExpressionIndex index) where T : IExpressionVisitor {
                        
                if(index.id <= 0) return;
                
                UntypedExpressionNode * node = untypedNodes.GetPointer(index.id);
                
                switch (node->meta.type) {
        
                case ExpressionNodeType.BlockExpression: {
                    BlockExpression* data = (BlockExpression*)node;
                    if(visitor.VisitBlockExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->statementList);
                    }
                    break;
                }

                case ExpressionNodeType.ReturnStatement: {
                    ReturnStatement* data = (ReturnStatement*)node;
                    if(visitor.VisitReturnStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.UsingStatement: {
                    UsingStatement* data = (UsingStatement*)node;
                    if(visitor.VisitUsingStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->acquisition);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.IfStatement: {
                    IfStatement* data = (IfStatement*)node;
                    if(visitor.VisitIfStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->condition);
                        Visit(ref visitor, data->body);
                        Visit(ref visitor, data->elseBody);
                    }
                    break;
                }

                case ExpressionNodeType.SwitchExpression: {
                    SwitchExpression* data = (SwitchExpression*)node;
                    if(visitor.VisitSwitchExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhs);
                        Visit(ref visitor, data->switchArms);
                    }
                    break;
                }

                case ExpressionNodeType.SwitchStatement: {
                    SwitchStatement* data = (SwitchStatement*)node;
                    if(visitor.VisitSwitchStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->condition);
                        Visit(ref visitor, data->sections);
                        Visit(ref visitor, data->defaultBody);
                    }
                    break;
                }

                case ExpressionNodeType.SwitchArm: {
                    SwitchArm* data = (SwitchArm*)node;
                    if(visitor.VisitSwitchArm(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->pattern);
                        Visit(ref visitor, data->guard);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.SwitchLabel: {
                    SwitchLabel* data = (SwitchLabel*)node;
                    if(visitor.VisitSwitchLabel(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->caseExpression);
                        Visit(ref visitor, data->guardExpression);
                    }
                    break;
                }

                case ExpressionNodeType.SwitchSection: {
                    SwitchSection* data = (SwitchSection*)node;
                    if(visitor.VisitSwitchSection(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->labels);
                        Visit(ref visitor, data->bodyStatements);
                    }
                    break;
                }

                case ExpressionNodeType.WhileLoop: {
                    WhileLoop* data = (WhileLoop*)node;
                    if(visitor.VisitWhileLoop(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->condition);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.ForLoop: {
                    ForLoop* data = (ForLoop*)node;
                    if(visitor.VisitForLoop(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->initializer);
                        Visit(ref visitor, data->condition);
                        Visit(ref visitor, data->iterator);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.ForeachLoop: {
                    ForeachLoop* data = (ForeachLoop*)node;
                    if(visitor.VisitForeachLoop(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->variableDeclaration);
                        Visit(ref visitor, data->enumerableExpression);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.BreakStatement: {
                    BreakStatement* data = (BreakStatement*)node;
                    if(visitor.VisitBreakStatement(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.ContinueStatement: {
                    ContinueStatement* data = (ContinueStatement*)node;
                    if(visitor.VisitContinueStatement(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.GoToStatement: {
                    GoToStatement* data = (GoToStatement*)node;
                    if(visitor.VisitGoToStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->caseJumpTarget);
                        Visit(ref visitor, data->labelTarget);
                    }
                    break;
                }

                case ExpressionNodeType.YieldStatement: {
                    YieldStatement* data = (YieldStatement*)node;
                    if(visitor.VisitYieldStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.TernaryExpression: {
                    TernaryExpression* data = (TernaryExpression*)node;
                    if(visitor.VisitTernaryExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->condition);
                        Visit(ref visitor, data->trueExpression);
                        Visit(ref visitor, data->falseExpression);
                    }
                    break;
                }

                case ExpressionNodeType.Argument: {
                    Argument* data = (Argument*)node;
                    if(visitor.VisitArgument(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.ThrowStatement: {
                    ThrowStatement* data = (ThrowStatement*)node;
                    if(visitor.VisitThrowStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.RangeExpression: {
                    RangeExpression* data = (RangeExpression*)node;
                    if(visitor.VisitRangeExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhs);
                        Visit(ref visitor, data->rhs);
                    }
                    break;
                }

                case ExpressionNodeType.Catch: {
                    Catch* data = (Catch*)node;
                    if(visitor.VisitCatch(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->exceptionFilter);
                        Visit(ref visitor, data->body);
                        Visit(ref visitor, data->typePath);
                    }
                    break;
                }

                case ExpressionNodeType.TryCatchFinally: {
                    TryCatchFinally* data = (TryCatchFinally*)node;
                    if(visitor.VisitTryCatchFinally(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->tryBody);
                        Visit(ref visitor, data->catchClauses);
                        Visit(ref visitor, data->finallyClause);
                    }
                    break;
                }

                case ExpressionNodeType.BinaryPattern: {
                    BinaryPattern* data = (BinaryPattern*)node;
                    if(visitor.VisitBinaryPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhs);
                        Visit(ref visitor, data->rhs);
                    }
                    break;
                }

                case ExpressionNodeType.RelationalPattern: {
                    RelationalPattern* data = (RelationalPattern*)node;
                    if(visitor.VisitRelationalPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.DiscardPattern: {
                    DiscardPattern* data = (DiscardPattern*)node;
                    if(visitor.VisitDiscardPattern(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.ConstantPattern: {
                    ConstantPattern* data = (ConstantPattern*)node;
                    if(visitor.VisitConstantPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.VariableDesignation: {
                    VariableDesignation* data = (VariableDesignation*)node;
                    if(visitor.VisitVariableDesignation(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->designationList);
                    }
                    break;
                }

                case ExpressionNodeType.DeclarationPattern: {
                    DeclarationPattern* data = (DeclarationPattern*)node;
                    if(visitor.VisitDeclarationPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->designation);
                    }
                    break;
                }

                case ExpressionNodeType.VarPattern: {
                    VarPattern* data = (VarPattern*)node;
                    if(visitor.VisitVarPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->designation);
                    }
                    break;
                }

                case ExpressionNodeType.TypePattern: {
                    TypePattern* data = (TypePattern*)node;
                    if(visitor.VisitTypePattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                    }
                    break;
                }

                case ExpressionNodeType.UnaryNotPattern: {
                    UnaryNotPattern* data = (UnaryNotPattern*)node;
                    if(visitor.VisitUnaryNotPattern(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->pattern);
                    }
                    break;
                }

                case ExpressionNodeType.LockStatement: {
                    LockStatement* data = (LockStatement*)node;
                    if(visitor.VisitLockStatement(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lockExpression);
                        Visit(ref visitor, data->bodyExpression);
                    }
                    break;
                }

                case ExpressionNodeType.BracketExpression: {
                    BracketExpression* data = (BracketExpression*)node;
                    if(visitor.VisitBracketExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->arguments);
                    }
                    break;
                }

                case ExpressionNodeType.DefaultExpression: {
                    DefaultExpression* data = (DefaultExpression*)node;
                    if(visitor.VisitDefaultExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                    }
                    break;
                }

                case ExpressionNodeType.SizeOfExpression: {
                    SizeOfExpression* data = (SizeOfExpression*)node;
                    if(visitor.VisitSizeOfExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                    }
                    break;
                }

                case ExpressionNodeType.NameOfExpression: {
                    NameOfExpression* data = (NameOfExpression*)node;
                    if(visitor.VisitNameOfExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->identifier);
                    }
                    break;
                }

                case ExpressionNodeType.AnonymousMethodExpression: {
                    AnonymousMethodExpression* data = (AnonymousMethodExpression*)node;
                    if(visitor.VisitAnonymousMethodExpression(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.CheckedExpression: {
                    CheckedExpression* data = (CheckedExpression*)node;
                    if(visitor.VisitCheckedExpression(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.BaseAccessExpression: {
                    BaseAccessExpression* data = (BaseAccessExpression*)node;
                    if(visitor.VisitBaseAccessExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->identifier);
                        Visit(ref visitor, data->indexExpressions);
                    }
                    break;
                }

                case ExpressionNodeType.TupleExpression: {
                    TupleExpression* data = (TupleExpression*)node;
                    if(visitor.VisitTupleExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->arguments);
                    }
                    break;
                }

                case ExpressionNodeType.Identifier: {
                    Identifier* data = (Identifier*)node;
                    if(visitor.VisitIdentifier(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typeArgumentList);
                    }
                    break;
                }

                case ExpressionNodeType.TypeModifier: {
                    TypeModifier* data = (TypeModifier*)node;
                    if(visitor.VisitTypeModifier(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.TypePath: {
                    TypePath* data = (TypePath*)node;
                    if(visitor.VisitTypePath(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->baseTypePath);
                        Visit(ref visitor, data->modifiers);
                    }
                    break;
                }

                case ExpressionNodeType.TypeNamePart: {
                    TypeNamePart* data = (TypeNamePart*)node;
                    if(visitor.VisitTypeNamePart(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->argumentList);
                        Visit(ref visitor, data->partList);
                    }
                    break;
                }

                case ExpressionNodeType.DirectCast: {
                    DirectCast* data = (DirectCast*)node;
                    if(visitor.VisitDirectCast(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.AssignmentExpression: {
                    AssignmentExpression* data = (AssignmentExpression*)node;
                    if(visitor.VisitAssignmentExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhs);
                        Visit(ref visitor, data->rhs);
                    }
                    break;
                }

                case ExpressionNodeType.ParenExpression: {
                    ParenExpression* data = (ParenExpression*)node;
                    if(visitor.VisitParenExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.MethodInvocation: {
                    MethodInvocation* data = (MethodInvocation*)node;
                    if(visitor.VisitMethodInvocation(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->argumentList);
                    }
                    break;
                }

                case ExpressionNodeType.IncrementDecrement: {
                    IncrementDecrement* data = (IncrementDecrement*)node;
                    if(visitor.VisitIncrementDecrement(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.PrimaryExpressionPart: {
                    PrimaryExpressionPart* data = (PrimaryExpressionPart*)node;
                    if(visitor.VisitPrimaryExpressionPart(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                        Visit(ref visitor, data->bracketExpressions);
                    }
                    break;
                }

                case ExpressionNodeType.NewExpression: {
                    NewExpression* data = (NewExpression*)node;
                    if(visitor.VisitNewExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->argList);
                        Visit(ref visitor, data->arraySpecs);
                        Visit(ref visitor, data->initializer);
                    }
                    break;
                }

                case ExpressionNodeType.ArrayCreationRank: {
                    ArrayCreationRank* data = (ArrayCreationRank*)node;
                    if(visitor.VisitArrayCreationRank(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expressionList);
                    }
                    break;
                }

                case ExpressionNodeType.PrimaryExpression: {
                    PrimaryExpression* data = (PrimaryExpression*)node;
                    if(visitor.VisitPrimaryExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->start);
                        Visit(ref visitor, data->parts);
                        Visit(ref visitor, data->bracketExpressions);
                    }
                    break;
                }

                case ExpressionNodeType.PrimaryIdentifier: {
                    PrimaryIdentifier* data = (PrimaryIdentifier*)node;
                    if(visitor.VisitPrimaryIdentifier(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typeArgumentList);
                    }
                    break;
                }

                case ExpressionNodeType.UnboundTypeNameExpression: {
                    UnboundTypeNameExpression* data = (UnboundTypeNameExpression*)node;
                    if(visitor.VisitUnboundTypeNameExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->next);
                    }
                    break;
                }

                case ExpressionNodeType.TypeOfExpression: {
                    TypeOfExpression* data = (TypeOfExpression*)node;
                    if(visitor.VisitTypeOfExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->unboundTypeName);
                    }
                    break;
                }

                case ExpressionNodeType.PrefixUnaryExpression: {
                    PrefixUnaryExpression* data = (PrefixUnaryExpression*)node;
                    if(visitor.VisitPrefixUnaryExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                    }
                    break;
                }

                case ExpressionNodeType.LambdaExpression: {
                    LambdaExpression* data = (LambdaExpression*)node;
                    if(visitor.VisitLambdaExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->parameters);
                        Visit(ref visitor, data->body);
                    }
                    break;
                }

                case ExpressionNodeType.BinaryExpression: {
                    BinaryExpression* data = (BinaryExpression*)node;
                    if(visitor.VisitBinaryExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhs);
                        Visit(ref visitor, data->rhs);
                    }
                    break;
                }

                case ExpressionNodeType.IsTypeExpression: {
                    IsTypeExpression* data = (IsTypeExpression*)node;
                    if(visitor.VisitIsTypeExpression(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->typePatternArms);
                    }
                    break;
                }

                case ExpressionNodeType.IsNullExpression: {
                    IsNullExpression* data = (IsNullExpression*)node;
                    if(visitor.VisitIsNullExpression(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.TypePatternArm: {
                    TypePatternArm* data = (TypePatternArm*)node;
                    if(visitor.VisitTypePatternArm(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                        Visit(ref visitor, data->identifier);
                    }
                    break;
                }

                case ExpressionNodeType.TypeArgumentList: {
                    TypeArgumentList* data = (TypeArgumentList*)node;
                    if(visitor.VisitTypeArgumentList(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePaths);
                    }
                    break;
                }

                case ExpressionNodeType.MemberAccess: {
                    MemberAccess* data = (MemberAccess*)node;
                    if(visitor.VisitMemberAccess(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->identifier);
                        Visit(ref visitor, data->argumentList);
                    }
                    break;
                }

                case ExpressionNodeType.ArrayInitializer: {
                    ArrayInitializer* data = (ArrayInitializer*)node;
                    if(visitor.VisitArrayInitializer(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->initializers);
                    }
                    break;
                }

                case ExpressionNodeType.LocalFunctionDefinition: {
                    LocalFunctionDefinition* data = (LocalFunctionDefinition*)node;
                    if(visitor.VisitLocalFunctionDefinition(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typeParameters);
                        Visit(ref visitor, data->body);
                        Visit(ref visitor, data->returnType);
                        Visit(ref visitor, data->parameters);
                    }
                    break;
                }

                case ExpressionNodeType.VariableDeclaration: {
                    VariableDeclaration* data = (VariableDeclaration*)node;
                    if(visitor.VisitVariableDeclaration(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->typePath);
                        Visit(ref visitor, data->initializer);
                    }
                    break;
                }

                case ExpressionNodeType.LiteralAccess: {
                    LiteralAccess* data = (LiteralAccess*)node;
                    if(visitor.VisitLiteralAccess(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->access);
                    }
                    break;
                }

                case ExpressionNodeType.Literal: {
                    Literal* data = (Literal*)node;
                    if(visitor.VisitLiteral(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.StringInterpolation: {
                    StringInterpolation* data = (StringInterpolation*)node;
                    if(visitor.VisitStringInterpolation(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->parts);
                    }
                    break;
                }

                case ExpressionNodeType.StringInterpolationPart: {
                    StringInterpolationPart* data = (StringInterpolationPart*)node;
                    if(visitor.VisitStringInterpolationPart(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                        Visit(ref visitor, data->alignmentExpression);
                    }
                    break;
                }

                case ExpressionNodeType.ElementInitializer: {
                    ElementInitializer* data = (ElementInitializer*)node;
                    if(visitor.VisitElementInitializer(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->expression);
                        Visit(ref visitor, data->expressionList);
                    }
                    break;
                }

                case ExpressionNodeType.MemberInitializer: {
                    MemberInitializer* data = (MemberInitializer*)node;
                    if(visitor.VisitMemberInitializer(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->lhsExpressionList);
                        Visit(ref visitor, data->rhs);
                    }
                    break;
                }

                case ExpressionNodeType.CollectionInitializer: {
                    CollectionInitializer* data = (CollectionInitializer*)node;
                    if(visitor.VisitCollectionInitializer(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->initializers);
                    }
                    break;
                }

                case ExpressionNodeType.ObjectInitializer: {
                    ObjectInitializer* data = (ObjectInitializer*)node;
                    if(visitor.VisitObjectInitializer(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->memberInit);
                    }
                    break;
                }

                case ExpressionNodeType.ResolveIdExpression: {
                    ResolveIdExpression* data = (ResolveIdExpression*)node;
                    if(visitor.VisitResolveIdExpression(*data) == VisitorAction.Visit) {
                    }
                    break;
                }

                case ExpressionNodeType.Parameter: {
                    Parameter* data = (Parameter*)node;
                    if(visitor.VisitParameter(*data) == VisitorAction.Visit) {
                        Visit(ref visitor, data->defaultExpression);
                        Visit(ref visitor, data->typePath);
                    }
                    break;
                }
                 
                }
                    
        }
            
    }
}
