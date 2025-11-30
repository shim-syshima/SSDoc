using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Generation;
using SummaryDocumentation.Core.Model;
using System.Globalization;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the TypeSymbolDocumentationProvider class.
    /// </summary>
    public sealed class TypeSymbolDocumentationProvider : ISymbolDocumentationProvider
    {
        /// <summary>
        /// Performs the CanHandle operation.
        /// </summary>
        /// <param name="symbol">The symbol parameter.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool CanHandle(ISymbol symbol)
        {
            return symbol is INamedTypeSymbol;
        }

        /// <summary>
        /// Performs the CreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The documentation model result.</returns>
        public DocumentationModel CreateDocumentation(ISymbol symbol)
        {
            if (symbol is not INamedTypeSymbol namedType)
            {
                return null;
            }

            var model = new DocumentationModel
            {
                Summary = CreateSummary(namedType)
            };

            return model;
        }

        private static string CreateSummary(INamedTypeSymbol type)
        {
            var typeName = type.Name;
            var nounPhrase = NamePhraseHelper.ToTypeNounPhrase(typeName);

            switch (type.TypeKind)
            {
                case TypeKind.Class:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Represents the <see cref=\"{0}\"/> class.",
                        typeName);

                case TypeKind.Struct:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Represents the <see cref=\"{0}\"/> struct.",
                        typeName);

                case TypeKind.Interface:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Defines the <see cref=\"{0}\"/> interface.",
                        typeName);

                case TypeKind.Enum:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Specifies values that represent {0}.",
                        nounPhrase);

                default:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Represents the {0}.",
                        nounPhrase);
            }
        }
    }
}