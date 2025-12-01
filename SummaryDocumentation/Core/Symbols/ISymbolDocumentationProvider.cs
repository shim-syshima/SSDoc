using Microsoft.CodeAnalysis;
using SummaryDocumentation.Core.Model;

namespace SummaryDocumentation.Core.Symbols
{
    /// <summary>
    /// Defines the <see cref="ISymbolDocumentationProvider"/> interface.
    /// </summary>
    public interface ISymbolDocumentationProvider
    {
        /// <summary>
        /// Performs the CanHandle operation.
        /// </summary>
        /// <param name="symbol">The symbol parameter.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        bool CanHandle(ISymbol symbol);

        /// <summary>
        /// Performs the CreateDocumentation operation.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The documentation model result.</returns>
        DocumentationModel CreateDocumentation(ISymbol symbol);
    }
}
