using System.Text;
using System.Text.RegularExpressions;
using EvolveUI.Compiler;
using EvolveUI.Parsing;
using EvolveUI.Util;

namespace EvolveUI {

#if !UNITY_64
    public class ManagedTypeResolverImpl { }
#endif

    public unsafe struct Diagnostics : IDisposable {

        internal Implementation* implementation;

        internal struct Implementation {

            public int lockRef;
            public fixed byte padding[60];
            public PodList<ErrorInfo> errors;
            public bool cancelled;

            public void CancelCompilation() {
                cancelled = true;
            }

#if UNITY_64
            private void Lock() {
                SpinLock.Lock(ref lockRef);
            }

            private void Unlock() {
                SpinLock.Unlock(ref lockRef);
            }
#else
            private SpinLock spinLock;

            private void Lock() {
                bool taken = false;
                spinLock.Enter(ref taken);
            }

            private void Unlock() {
                spinLock.Exit();
            }

#endif
            public bool HasErrors() {
                if (cancelled) return true;
                Lock();
                bool hasErrors = errors.size > 0;
                Unlock();
                return hasErrors;
            }

            public void LogError(FixedCharacterSpan filePath, FixedCharacterSpan fileSource, CheckedArray<Token> tokens, CheckedArray<Token> nonTrivialTokens, DiagnosticError errorType, NonTrivialTokenRange tokenRange,
                DiagnosticDetails details) {
                Lock();
                errors.Add(new ErrorInfo() {
                    tokens = tokens,
                    nonTrivialTokens = nonTrivialTokens,
                    filePath = filePath,
                    fileSource = fileSource,
                    details = details,
                    errorType = errorType,
                    tokenRange = tokenRange
                });
                Unlock();
            }

            public void Dump(ManagedTypeResolverImpl managedTypeResolver = null) {
                Lock();
                try {
                    DumpErrors(managedTypeResolver);
                }
                finally {
                    Unlock();
                }
            }

            private static string TitleCaseToDisplayString(string input, bool lowerFirstLetter = true) {
                string str = Regex.Replace(Regex.Replace(input, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
                string[] strs = str.Split(' ');
                int start = 1;
                if (lowerFirstLetter) start = 0;
                for (int i = start; i < strs.Length; i++) {
                    strs[i] = strs[i].ToLower()[0] + strs[i].Substring(1);
                }

                return string.Join(' ', strs);
            }

            private void DumpErrors(ManagedTypeResolverImpl resolverImpl) {
                for (int errorIndex = 0; errorIndex < errors.size; errorIndex++) {
                    ErrorInfo error = errors.Get(errorIndex);
                    BuildErrorString(error);
                }
            }

            public static string BuildErrorString(ErrorInfo error) {
                if (error.fileSource.size == 0) {
                    if (error.errorType == DiagnosticError.ErrorString) {
                        return $"[UICompileError] {error.details.errorString.primaryError}";
                    }

                    return string.Empty;
                }

                FixedCharacterSpan fileSource = error.fileSource;

#if UNITY_64
                string filePath = PathUtil.ProjectRelativePath(error.filePath.ToString());
#else
                string filePath = error.filePath.ToString();
#endif

                StringBuilder builder = new StringBuilder(512);

                // we don't have token info for this
                if (error.errorType == DiagnosticError.FailedToTokenize) {
                    builder.Append("Failed to tokenize file");

                    builder.Append("\nFull Context:\n");
                    builder.Append("<color=green>file: ");
                    builder.Append(error.filePath.ToString());
                    builder.Append("</color>\n");
                    return builder.ToString();
                }

                string primaryErrorSpan;
                CheckedArray<Token> tokens = error.tokens;
                int location = error.nonTrivialTokens[error.tokenRange.start.index].tokenIndex;

                builder.Append(filePath);
                builder.Append("(");
                builder.Append(tokens[location].lineIndex);
                builder.Append(":");
                builder.Append(tokens[location].columnIndex);
                builder.Append(") error: ");
                switch (error.errorType) {
                    default: {
                        primaryErrorSpan = TitleCaseToDisplayString(error.errorType.ToString(), false);
                        break;
                    }
#if UNITY_64
                        case DiagnosticError.AmbiguousTypeMatch: {
                            TypeResolutionError typeResolutionError = error.details.typeResolutionError;
                            Type type = managedTypeResolver.Resolve(new ResolvedTypePointer(typeResolutionError.ambiguousType));
                            Type resolved = managedTypeResolver.Resolve(new ResolvedTypePointer(typeResolutionError.resolvedType));
                            primaryErrorSpan = $"Ambiguous type match between `{type.GetTypeName()}` and `{resolved.GetTypeName()}`. Please provide the full namespace path.";
                            break;
                        }

                        case DiagnosticError.AmbiguousElementReference: {
                            ModuleImport ambiguousModule = error.details.tagResolutionError.ambiguousModule;
                            FixedCharacterSpan tag = error.details.tagResolutionError.tagName;
                            primaryErrorSpan = $"Ambiguous reference between `{ambiguousModule.moduleInfo->moduleName}::{tag}` and `{error.details.tagResolutionError.module->moduleName}::{tag}`. You need to add an explicit module prefix to disambiguate this";
                            break;
                        }

                        case DiagnosticError.RequiredArgumentNotProvided: {
                            TypedParameterDesc parameter = error.details.parameterError.availableParameters[error.details.parameterError.parameterIndex];
                            Type type = managedTypeResolver.Resolve(parameter.resolvedTypePointer);
                            primaryErrorSpan = $"Required argument `{error.details.parameterError.parameterName}` of type `{type.GetTypeName()}` was not provided";
                            break;
                        }
                        
                        case DiagnosticError.UnresolvedType: {
                            primaryErrorSpan = "Unresolved type `" + error.details.typeResolutionError.typeName + "`";
                            break;
                        }
#endif

                    case DiagnosticError.DecoratorGenericArgumentCountMismatch: {
                        DecoratorArgumentCountMismatch details = error.details.decoratorArgumentCountMismatch;
                        primaryErrorSpan = $"Incorrect number of generic arguments provided to `{details.decoratorModule}::{details.decoratorTagName}`. Expected {details.maxArgumentCount} generics but {details.givenArgumentCount} were passed.";
                        break;
                    }

                    case DiagnosticError.ErrorString: {
                        if (error.details.errorString.primaryErrorHandle.IsAllocated) {
                            primaryErrorSpan = error.details.errorString.primaryErrorHandle.Get();
                            error.details.errorString.primaryErrorHandle.Dispose();
                        }
                        else {
                            primaryErrorSpan = error.details.errorString.primaryError.ToString();
                        }

                        break;
                    }

                    case DiagnosticError.UnresolvedMemberAccess: {
                        primaryErrorSpan = $"Type {error.details.memberAccessError.typeName} does not declare an accessible member `{error.details.memberAccessError.identifier}`";
                        break;
                    }

                    case DiagnosticError.OperationIsNotAllowedHere: {
                        primaryErrorSpan = "Code operations are not allowed inside template blocks, you probably forgot to add a `run`";
                        break;
                    }


                    case DiagnosticError.VariableNameAlreadyInScope: {
                        primaryErrorSpan = "Variable `" + error.details.variableNameAlreadyInScope.variableName + "` was already defined in scope";
                        break;
                    }

                    case DiagnosticError.UnableToParseStyleLiteral: {
                        primaryErrorSpan = error.details.styleLiteralParseError.primaryError.ToString();
                        break;
                    }

                    case DiagnosticError.UnresolvedFieldOrProperty: {
                        string typeName = error.details.fieldOrPropertyError.typeName.ToString();
                        primaryErrorSpan = "`" + error.details.fieldOrPropertyError.fieldOrPropertyName + "` is not defined or is not publicly accessible on type " + typeName;
                        break;
                    }


                    case DiagnosticError.DuplicateTopLevelName: {
                        DuplicateTopLevelNameDesc info = error.details.topLevelNameError;
                        primaryErrorSpan = $"{info.identifier} was already defined in this module ({info.templateFile->filePath} at line {info.line}:{info.col})";
                        break;
                    }
                }

                builder.Append(primaryErrorSpan);

                builder.Append("\nFull Context:\n");
                builder.Append("<color=green>file: ");
                builder.Append(error.filePath.ToString());
                builder.Append("</color>\n");

                Token endToken = tokens[location];

                List<string> lines = new string(fileSource.ptr, 0, fileSource.size).Split('\n').ToList();
                lines.Insert(0, "");

                const int k_PreLineCount = 5;
                const int k_PostLineCount = 10;

                int lineStart = Math.Max(1, endToken.lineIndex - k_PreLineCount);

                for (int i = lineStart; i <= endToken.lineIndex; i++) {
                    builder.Append(i.ToString("0000"));
                    builder.Append("|");
                    builder.Append(lines[i]);
                    builder.Append("\n");
                }

                builder.Append("<color=#f53333>error");
                builder.Append(new string(' ', endToken.columnIndex));
                builder.Append("^ ");
                builder.Append(primaryErrorSpan);
                builder.Append("</color>");
                builder.Append("\n");
                int maxLine = Math.Min(endToken.lineIndex + k_PostLineCount, lines.Count);

                for (int i = endToken.lineIndex + 1; i < maxLine; i++) {
                    builder.Append(i.ToString("0000"));
                    builder.Append("|");
                    builder.Append(lines[i]);
                    builder.Append("\n");
                }

                switch (error.errorType) {
#if UNITY_64
                        case DiagnosticError.UnresolvedType: {
                            UnresolvedTypeMessage(builder, error);
                            break;
                        }
#endif

                    case DiagnosticError.ErrorString: {
                        if (error.details.errorString.help.size > 0) {
                            builder.Append(error.details.errorString.help.ToString());
                        }

                        break;
                    }


                    case DiagnosticError.VariableNameAlreadyInScope: {
                        builder.Append($"<color={HelpColor}>help: ");
                        VariableNameAlreadyInScopeInfo scopeError = error.details.variableNameAlreadyInScope;
                        builder.Append("Variable ");
                        builder.Append(scopeError.variableName);
                        builder.Append(" was already defined at ");
                        int otherLocation = error.nonTrivialTokens[scopeError.locationA.start.index].tokenIndex;
                        builder.Append(tokens[otherLocation].lineIndex);
                        builder.Append(":");
                        builder.Append(tokens[otherLocation].columnIndex);
                        builder.Append("</color>");
                        builder.Append("\n\n");
                        break;
                    }
                }

                return builder.ToString();
            }

#if UNITY_64
            private static void UnresolvedTypeMessage(StringBuilder builder, ErrorInfo error) {
                builder.Append($"<color={HelpColor}>help: ");
                builder.Append("looked in namespaces [");
                for (int n = 0; n < error.details.typeResolutionError.namespaces.size; n++) {
                    builder.Append(error.details.typeResolutionError.namespaces.Get(n).ToString());
                    if (n != error.details.typeResolutionError.namespaces.size - 1) {
                        builder.Append(", ");
                    }
                }

                builder.Append("]\n\n");

                builder.Append("Found these similar types in other namespaces:\n\n");

                FixedCharacterSpan typeName = error.details.typeResolutionError.typeName;
                char* stackPtr = stackalloc char[typeName.size + 1];
                ReadOnlySpan<char> typeNameSpan = new ReadOnlySpan<char>(stackPtr, typeName.size + 1);
                TypedUnsafe.MemCpy(stackPtr + 1, typeName.ptr, typeName.size);
                stackPtr[0] = '.';
                using NativeArray<FixedCharacterSpan> keys = ApplicationCompiler.s_ManagedTypeResolver.typePointerByFullTypeName->GetKeyArray(Allocator.Persistent);
                for (int k = 0; k < keys.Length; k++) {
                    FixedCharacterSpan key = keys[k];
                    ReadOnlySpan<char> span = new ReadOnlySpan<char>(key.ptr, key.size);
                    if (span.Contains(typeNameSpan, StringComparison.Ordinal)) {
                        builder.Append(key.ToString());
                        builder.Append("\n");
                    }
                }

                builder.Append("</color>\n");
            }

#endif

        }

        internal struct ErrorInfo {

            public FixedCharacterSpan filePath;
            public FixedCharacterSpan fileSource;
            public DiagnosticError errorType;
            public NonTrivialTokenRange tokenRange;
            public DiagnosticDetails details;
            public CheckedArray<Token> tokens;
            public CheckedArray<Token> nonTrivialTokens;

        }

        public static string HelpColor => "blue";

        public void Dispose() {
            if (implementation != null) {
                implementation->errors.Dispose();
                Native.Memory.AlignedFree(implementation, Native.Memory.AlignOf<Implementation>());
            }

            this = default;
        }

        public static Diagnostics Create() {
            Diagnostics diagnostics = new Diagnostics();
            diagnostics.Initialize();
            return diagnostics;
        }
        
        internal void Initialize() {
            implementation = Native.Memory.AlignedMalloc<Implementation>(1);
            *implementation = new Implementation() {
                errors = new PodList<ErrorInfo>(8)
            };
        }

        internal void LogError(DiagnosticError error, TemplateFile* file, NonTrivialTokenRange tokenRange, DiagnosticDetails details = default) {
            file->hasErrors = true;
            implementation->LogError(file->filePath, file->fileSource, file->tokens, file->nonTrivialTokens, error, tokenRange, details);
        }
        
        internal void LogError(TemplateFile* file, NonTrivialTokenRange tokenRange, ErrorStringDesc desc) {
            if (file == null) {
                implementation->LogError(default, default, default, default, DiagnosticError.ErrorString, tokenRange, new DiagnosticDetails() {
                    errorString = desc
                });
            }
            else {
                file->hasErrors = true;
                implementation->LogError(file->filePath, file->fileSource, file->tokens, file->nonTrivialTokens, DiagnosticError.ErrorString, tokenRange, new DiagnosticDetails() {
                    errorString = desc
                });
            }
        }

        internal void LogError(string message) {
            // TODO make this a pretty error message
            LogUtil.Error(message);
        }

        public void LogError(string message, string filePath) {
            // TODO make this a pretty error message
            LogUtil.Error($"Error in `{filePath}`: {message}");
        }

        public void LogError(string message, string filePath, int lineInfoLine, int lineInfoColumn) {
            // do we do a file read? probably not. we'll read later if source is missing 
            // TODO make this a pretty error message
            LogUtil.Error($"Error in `{filePath}` line {lineInfoLine} column {lineInfoColumn}: {message}");
        }
        
        public void Dump(ManagedTypeResolverImpl managedTypeResolver = null) {
            implementation->Dump(managedTypeResolver);
        }

        public bool HasErrors() {
            return implementation->HasErrors();
        }

        public void CancelCompilation() {
            implementation->CancelCompilation();
        }

    }

}