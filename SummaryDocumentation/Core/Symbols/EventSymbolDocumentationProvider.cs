using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Generation;
using SummaryDocumentation.Core.Model;
using System.Globalization;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the EventSymbolDocumentationProvider class.
    /// </summary>
    public sealed class EventSymbolDocumentationProvider : ISymbolDocumentationProvider
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
            var noun = NamePhraseHelper.ToNounPhrase(property.Name);
            return string.Format(
                CultureInfo.InvariantCulture,
                "Occurs when the {0}.",
                noun);
        }
    }
}
