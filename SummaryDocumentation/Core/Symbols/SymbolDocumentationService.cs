using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Generation;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Represents the SymbolDocumentationService class.
    /// </summary>
    public sealed class SymbolDocumentationService
    {
        private readonly IDictionary<SymbolKind, ISymbolDocumentationProvider> _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDocumentationService"/> class.
        /// </summary>
        public SymbolDocumentationService()
        {
            var dict = new Dictionary<SymbolKind, ISymbolDocumentationProvider>
            {
                [SymbolKind.NamedType] = new TypeSymbolDocumentationProvider(),
                [SymbolKind.Method] = new MethodSymbolDocumentationProvider(),
                [SymbolKind.Property] = new PropertySymbolDocumentationProvider(),
                [SymbolKind.Event] = new EventSymbolDocumentationProvider(),
                [SymbolKind.Field] = new FieldSymbolDocumentationProvider()
            };

            _providers = dict;
        }

        /// <summary>
        /// Performs the TryCreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="documentationText">The documentation text.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool TryCreateDocumentation(ISymbol symbol, out string documentationText)
        {
            documentationText = string.Empty;

            var current = symbol;
            while (current != null)
            {
                if (_providers.TryGetValue(current.Kind, out var provider))
                {
                    var model = provider.CreateDocumentation(current);
                    if (model == null)
                    {
                        return false;
                    }

                    documentationText = XmlDocumentationFormatter.Format(model);
                    return true;
                }

                current = current.ContainingSymbol;
            }

            return false;
        }
    }
}