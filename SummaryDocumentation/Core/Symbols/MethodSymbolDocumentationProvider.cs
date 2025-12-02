// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MethodSymbolDocumentationProvider.cs" company="https://github.com/shiri47s/SummaryDocumentation">
//  Copyright (c) shiri47s. All rights reserved.
// </copyright>
// <summary>
//  This code is an implementation of the ASOBITicket.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Extensions;
using SummaryDocumentation.Core.Generation;
using SummaryDocumentation.Core.Model;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the MethodSymbolDocumentationProvider class.
    /// </summary>
    public sealed class MethodSymbolDocumentationProvider : ISymbolDocumentationProvider
    {
        /// <summary>
        /// Gets the can handle.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool CanHandle(ISymbol symbol)
        {
            return symbol is IMethodSymbol;
        }

        /// <summary>
        /// Performs the CreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The documentation model result.</returns>
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
                    var typeName = method.ContainingType.Name;

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Initializes a new instance of the <see cref=\"{0}\"/> class.",
                        typeName);
                }

                case MethodKind.Constructor when method.IsStatic:
                {
                    var typeName = method.ContainingType.Name;

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Initializes static members of the <see cref=\"{0}\"/> class.",
                        typeName);
                }

                default:
                {
                    var summary = NamePhraseHelper.ToMethodSummary(method);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        return summary;
                    }

                    var noun = NamePhraseHelper.ToNounPhrase(method.Name);
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Performs the {0} operation.",
                        noun);
                }
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

            var phrase = NamePhraseHelper.ToSimpleWords(parameter.Name);

            return string.Format(
                CultureInfo.InvariantCulture,
                "The {0}.",
                phrase);
        }

        private static string GetReturnDescription(IMethodSymbol method)
        {
            var returnType = method.ReturnType;

            if (returnType.Name == "Task" &&
                returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                return "A task that represents the asynchronous operation.";
            }

            if (returnType is INamedTypeSymbol { Name: "Task", TypeArguments.Length: 1 } named &&
                named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                return
                    $"A task that represents the asynchronous operation. The task result contains the {named.TypeArguments[0].Name.ToLowerInvariant()}.";
            }

            return method.ReturnType.SpecialType == SpecialType.System_Boolean
                ? "True if the operation succeeds; otherwise, false."
                : $"The {returnType.Name.ToSimple().ToLowerInvariant()} result.";
        }
    }
}