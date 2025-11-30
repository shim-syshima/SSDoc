using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Generation;
using SummaryDocumentation.Core.Model;
using System.Globalization;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the PropertySymbolDocumentationProvider class.
    /// </summary>
    public sealed class PropertySymbolDocumentationProvider : ISymbolDocumentationProvider
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
            var hasGetter = property.GetMethod != null;
            var hasSetter = property.SetMethod != null;

            var isBool = property.Type.SpecialType == SpecialType.System_Boolean;

            if (isBool)
            {
                var phrase = NamePhraseHelper.ToBoolCorePhrase(property.Name);

                if (hasGetter && hasSetter)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Gets or sets a value indicating whether {0}.",
                        phrase);
                }

                if (hasGetter)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Gets a value indicating whether {0}.",
                        phrase);
                }

                if (hasSetter)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "Sets a value indicating whether {0}.",
                        phrase);
                }
            }

            // 非 bool プロパティ
            var noun = NamePhraseHelper.ToNounPhrase(property.Name);

            if (hasGetter && hasSetter)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Gets or sets the {0}.",
                    noun);
            }

            if (hasGetter)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Gets the {0}.",
                    noun);
            }

            if (hasSetter)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Sets the {0}.",
                    noun);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "Represents the {0}.",
                noun);
        }
    }
}
