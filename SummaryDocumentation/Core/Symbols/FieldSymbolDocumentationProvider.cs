using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Generation;
using SummaryDocumentation.Core.Model;
using System.Globalization;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the FieldSymbolDocumentationProvider class.
    /// </summary>
    public sealed class FieldSymbolDocumentationProvider : ISymbolDocumentationProvider
    {
        /// <summary>
        /// Performs the CanHandle operation.
        /// </summary>
        /// <param name="symbol">The symbol parameter.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool CanHandle(ISymbol symbol)
        {
            return symbol is IPropertySymbol;
        }

        /// <summary>
        /// Performs the CreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The documentation model result.</returns>
        public DocumentationModel CreateDocumentation(ISymbol symbol)
        {
            if (symbol is not IPropertySymbol property)
            {
                return null;
            }

            var model = new DocumentationModel
            {
                Summary = CreateSummary(property)
            };


            return model;
        }

        private static string CreateSummary(IPropertySymbol property)
        {
            var isBool = property.Type.SpecialType == SpecialType.System_Boolean;

            if (isBool)
            {
                var phrase = NamePhraseHelper.ToBoolCorePhrase(property.Name);
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "A value indicating whether {0}.",
                    phrase);
            }

            var noun = NamePhraseHelper.ToNounPhrase(property.Name);
            return string.Format(
                CultureInfo.InvariantCulture,
                "The {0}.",
                noun);
        }
    }
}
