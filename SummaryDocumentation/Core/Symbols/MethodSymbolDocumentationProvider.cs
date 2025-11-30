using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Model;
using System.Globalization;
using SummaryDocumentation.Core.Extensions;
using SummaryDocumentation.Core.Generation;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the MethodSymbolDocumentationProvider class.
    /// </summary>
    public sealed class MethodSymbolDocumentationProvider : ISymbolDocumentationProvider
    {
        /// <summary>
        /// Performs the CanHandle operation.
        /// </summary>
        /// <param name="symbol">The symbol parameter.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool CanHandle(ISymbol symbol)
        {
            return symbol is IMethodSymbol;
        }

        /// <summary>
        /// Performs the CreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The documentationmodel result.</returns>
        public DocumentationModel CreateDocumentation(ISymbol symbol)
        {
            var method = (IMethodSymbol)symbol;

            var model = new DocumentationModel
            {
                Summary = GetMethodSummary(method)
            };

            // type parameters
            foreach (var typeParameter in method.TypeParameters)
            {
                model.TypeParameters.Add(
                    new TypeParameterDocumentation(
                        typeParameter.Name,
                        $"The {typeParameter.Name.ToLowerInvariant()} type parameter."));
            }

            // parameters
            foreach (var parameter in method.Parameters)
            {
                model.Parameters.Add(
                    new ParameterDocumentation(
                        parameter.Name,
                        GetParameterDescription(parameter)));
            }

            // returns
            if (!method.ReturnsVoid)
            {
                model.Returns = GetReturnDescription(method);
            }

            return model;
        }

        private static string GetMethodSummary(IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.Constructor when !method.IsStatic:
                {
                    var typeName = method.ContainingType != null
                        ? method.ContainingType.Name
                        : "UnknownType";

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Initializes a new instance of the <see cref=\"{0}\"/> class.",
                        typeName);
                }
                case MethodKind.StaticConstructor:
                case MethodKind.Constructor when method.IsStatic:
                {
                    var typeName = method.ContainingType != null
                        ? method.ContainingType.Name
                        : "UnknownType";

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Initializes static members of the <see cref=\"{0}\"/> class.",
                        typeName);
                }
                default:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Performs the {0} operation.",
                        method.Name);
            }
        }

        private static string GetParameterDescription(IParameterSymbol parameter)
        {
            if (parameter.Type.SpecialType == SpecialType.System_Boolean)
            {
                var core = NamePhraseHelper.ToBoolCorePhrase(parameter.Name);
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "A value indicating whether {0}.",
                    core);
            }

            var phrase = NamePhraseHelper.ToNounPhrase(parameter.Name);

            return string.Format(
                CultureInfo.InvariantCulture,
                "The {0}.",
                phrase);

        }

        private static string GetReturnDescription(IMethodSymbol method)
        {
            var returnType = method.ReturnType;

            if (returnType.Name == "Task" && returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                return "A task that represents the asynchronous operation.";
            }

            if (returnType is INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } named &&
                named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                return $"A task that represents the asynchronous operation. The task result contains the {named.TypeArguments[0].Name.ToLowerInvariant()}.";
            }

            return method.ReturnType.SpecialType == SpecialType.System_Boolean ? "True if the operation succeeds; otherwise, false." : $"The {returnType.Name.ToSimple().ToLowerInvariant()} result.";
        }
    }
}
