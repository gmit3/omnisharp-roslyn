using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions;
using OmniSharp.Mef;
using OmniSharp.Models.V2;
using OmniSharp.Models.V2.CodeStructure;
using OmniSharp.Roslyn.Utilities;
using OmniSharp.Services;

namespace OmniSharp.Roslyn.CSharp.Services.Structure
{
    [OmniSharpHandler(OmniSharpEndpoints.V2.CodeStructure, LanguageNames.CSharp)]
    public class CodeStructureService : IRequestHandler<CodeStructureRequest, CodeStructureResponse>
    {
        private readonly OmniSharpWorkspace _workspace;
        private readonly IEnumerable<ICodeElementPropertyProvider> _propertyProviders;

        [ImportingConstructor]
        public CodeStructureService(
            OmniSharpWorkspace workspace,
            [ImportMany] IEnumerable<ICodeElementPropertyProvider> propertyProviders)
        {
            _workspace = workspace;
            _propertyProviders = propertyProviders;
        }

        public async Task<CodeStructureResponse> Handle(CodeStructureRequest request)
        {
            // To provide complete code structure for the document wait until all projects are loaded.
            var document = await _workspace.GetDocumentFromFullProjectModelAsync(request.FileName);
            if (document == null)
            {
                return new CodeStructureResponse { Elements = Array.Empty<CodeElement>() };
            }

            // #startup 4 get CodeStructure
            var elements = await GetCodeElementsAsync(document);

            var response = new CodeStructureResponse
            {
                Elements = elements
            };

            return response;
        }

        private async Task<IReadOnlyList<CodeElement>> GetCodeElementsAsync(Document document)
        {
            var mapper = EvolveUI.ShouldProcess(document) ? EvolveUI.GetMapper(document) : null;
            var text = mapper != null ? mapper.original_source : await document.GetTextAsync();
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var semanticModel = await document.GetSemanticModelAsync();

            var results = ImmutableList.CreateBuilder<CodeElement>();

            foreach(var node in ((CompilationUnitSyntax)syntaxRoot).Members)
            {
                foreach (var element in CreateCodeElements(node, text, semanticModel, mapper))
                {
                    if (element != null)
                    {
                        results.Add(element);
                    }
                }
            }

            return results.ToImmutable();
        }

        private IEnumerable<CodeElement> CreateCodeElements(SyntaxNode node, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            switch (node)
            {
                case TypeDeclarationSyntax typeDeclaration:
                    yield return CreateCodeElement(typeDeclaration, text, semanticModel, mapper);
                    break;
                case DelegateDeclarationSyntax delegateDeclaration:
                    yield return CreateCodeElement(delegateDeclaration, text, semanticModel, mapper);
                    break;
                case EnumDeclarationSyntax enumDeclaration:
                    yield return CreateCodeElement(enumDeclaration, text, semanticModel, mapper);
                    break;
                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    yield return CreateCodeElement(namespaceDeclaration, text, semanticModel, mapper);
                    break;
                case BaseMethodDeclarationSyntax baseMethodDeclaration:
                    yield return CreateCodeElement(baseMethodDeclaration, text, semanticModel, mapper);
                    break;
                case BasePropertyDeclarationSyntax basePropertyDeclaration:
                    yield return CreateCodeElement(basePropertyDeclaration, text, semanticModel, mapper);
                    break;
                case BaseFieldDeclarationSyntax baseFieldDeclaration:
                    foreach (var variableDeclarator in baseFieldDeclaration.Declaration.Variables)
                    {
                        yield return CreateCodeElement(variableDeclarator, baseFieldDeclaration, text, semanticModel, mapper);
                    }

                    break;
                case EnumMemberDeclarationSyntax enumMemberDeclarationSyntax:
                    yield return CreateCodeElement(enumMemberDeclarationSyntax, text, semanticModel, mapper);
                    break;
            }
        }

        private CodeElement CreateCodeElement(TypeDeclarationSyntax typeDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortTypeFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.TypeFormat)
            };

            AddRanges(builder, typeDeclaration.AttributeLists.Span, typeDeclaration.Span, typeDeclaration.Identifier.Span, text, mapper);
            AddSymbolProperties(symbol, builder);

            foreach (var member in typeDeclaration.Members)
            {
                foreach (var childElement in CreateCodeElements(member, text, semanticModel, mapper))
                {
                    builder.AddChild(childElement);
                }
            }

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(DelegateDeclarationSyntax delegateDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(delegateDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortTypeFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.TypeFormat),
            };

            AddRanges(builder, delegateDeclaration.AttributeLists.Span, delegateDeclaration.Span, delegateDeclaration.Identifier.Span, text, mapper);
            AddSymbolProperties(symbol, builder);

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(EnumDeclarationSyntax enumDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(enumDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortTypeFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.TypeFormat),
            };

            AddRanges(builder, enumDeclaration.AttributeLists.Span, enumDeclaration.Span, enumDeclaration.Identifier.Span, text, mapper);
            AddSymbolProperties(symbol, builder);

            foreach (var member in enumDeclaration.Members)
            {
                foreach (var childElement in CreateCodeElements(member, text, semanticModel, mapper))
                {
                    builder.AddChild(childElement);
                }
            }

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(BaseNamespaceDeclarationSyntax namespaceDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortTypeFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.TypeFormat),
            };

            AddRanges(builder, attributesSpan: default, namespaceDeclaration.Span, namespaceDeclaration.Name.Span, text, mapper);

            foreach (var member in namespaceDeclaration.Members)
            {
                foreach (var childElement in CreateCodeElements(member, text, semanticModel, mapper))
                {
                    builder.AddChild(childElement);
                }
            }

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(BaseMethodDeclarationSyntax baseMethodDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(baseMethodDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortMemberFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.MemberFormat),
            };

            AddRanges(builder, baseMethodDeclaration.AttributeLists.Span, baseMethodDeclaration.Span, GetNameSpan(baseMethodDeclaration), text, mapper);
            AddSymbolProperties(symbol, builder);

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(BasePropertyDeclarationSyntax basePropertyDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(basePropertyDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortMemberFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.MemberFormat),
            };

            AddRanges(builder, basePropertyDeclaration.AttributeLists.Span, basePropertyDeclaration.Span, GetNameSpan(basePropertyDeclaration), text, mapper);
            AddSymbolProperties(symbol, builder);

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(VariableDeclaratorSyntax variableDeclarator, BaseFieldDeclarationSyntax baseFieldDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortMemberFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.MemberFormat),
            };

            AddRanges(builder, baseFieldDeclaration.AttributeLists.Span, variableDeclarator.Span, variableDeclarator.Identifier.Span, text, mapper);
            AddSymbolProperties(symbol, builder);

            return builder.ToCodeElement();
        }

        private CodeElement CreateCodeElement(EnumMemberDeclarationSyntax enumMemberDeclaration, SourceText text, SemanticModel semanticModel, EvolveUIMapper mapper)
        {
            var symbol = semanticModel.GetDeclaredSymbol(enumMemberDeclaration);
            if (symbol == null)
            {
                return null;
            }

            var builder = new CodeElement.Builder
            {
                Kind = symbol.GetKindString(),
                Name = symbol.ToDisplayString(SymbolDisplayFormats.ShortMemberFormat),
                DisplayName = symbol.ToDisplayString(SymbolDisplayFormats.MemberFormat),
            };

            AddRanges(builder, enumMemberDeclaration.AttributeLists.Span, enumMemberDeclaration.Span, enumMemberDeclaration.Identifier.Span, text, mapper);
            AddSymbolProperties(symbol, builder);

            return builder.ToCodeElement();
        }

        private static TextSpan GetNameSpan(BaseMethodDeclarationSyntax baseMethodDeclaration)
        {
            switch (baseMethodDeclaration)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    return methodDeclaration.Identifier.Span;
                case ConstructorDeclarationSyntax constructorDeclaration:
                    return constructorDeclaration.Identifier.Span;
                case DestructorDeclarationSyntax destructorDeclaration:
                    return destructorDeclaration.Identifier.Span;
                case OperatorDeclarationSyntax operatorDeclaration:
                    return operatorDeclaration.OperatorToken.Span;
                case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                    return conversionOperatorDeclaration.Type.Span;
                default:
                    return default;
            }
        }

        private static TextSpan GetNameSpan(BasePropertyDeclarationSyntax basePropertyDeclaration)
        {
            switch (basePropertyDeclaration)
            {
                case PropertyDeclarationSyntax propertyDeclaration:
                    return propertyDeclaration.Identifier.Span;
                case EventDeclarationSyntax eventDeclaration:
                    return eventDeclaration.Identifier.Span;
                case IndexerDeclarationSyntax indexerDeclaration:
                    return indexerDeclaration.ThisKeyword.Span;
                default:
                    return default;
            }
        }

        private static void AddRanges(CodeElement.Builder builder, TextSpan attributesSpan, TextSpan fullSpan, TextSpan nameSpan, SourceText text, EvolveUIMapper mapper)
        {
            if (attributesSpan != default)
                attributesSpan = mapper?.ConvertModifiedTextSpanToOriginal(attributesSpan) ?? default;
            if (attributesSpan != default)
            {
                builder.AddRange(SymbolRangeNames.Attributes, text.GetRangeFromSpan(attributesSpan));
            }

            if(fullSpan != default)
                fullSpan = mapper?.ConvertModifiedTextSpanToOriginal(fullSpan) ?? default;
            if(fullSpan != default)
            {
                builder.AddRange(SymbolRangeNames.Full, text.GetRangeFromSpan(fullSpan));
            }

            if(nameSpan != default)
                nameSpan = mapper?.ConvertModifiedTextSpanToOriginal(nameSpan) ?? default;
            if(nameSpan != default)
            {
                builder.AddRange(SymbolRangeNames.Name, text.GetRangeFromSpan(nameSpan));
            }
        }

        private void AddSymbolProperties(ISymbol symbol, CodeElement.Builder builder)
        {
            var accessibility = symbol.GetAccessibilityString();
            if (accessibility != null)
            {
                builder.AddProperty(SymbolPropertyNames.Accessibility, accessibility);
            }

            builder.AddProperty(SymbolPropertyNames.Static, symbol.IsStatic);

            foreach (var propertyProvider in _propertyProviders)
            {
                foreach (var (name, value) in propertyProvider.ProvideProperties(symbol))
                {
                    builder.AddProperty(name, value);
                }
            }
        }
    }
}
