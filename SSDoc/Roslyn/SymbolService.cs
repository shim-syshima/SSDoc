using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SSDoc.Roslyn
{
    /// <summary>
    /// Represents the SymbolService class.
    /// </summary>
    public sealed class SymbolService
    {
        /// <summary>
        /// Performs the FindSymbolAtPositionAsync operation.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="position">The position.</param>
        /// <param name="token">The token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<ISymbol> FindSymbolAtPositionAsync(
            Document document,
            SemanticModel semanticModel,
            int position,
            CancellationToken token)
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (root == null)
                return null;

            var tokenNode = root.FindToken(position);
            var node = tokenNode.Parent;

            SyntaxNode targetNode = null;

            while (node != null)
            {
                if (node is BaseMethodDeclarationSyntax ||
                    node is PropertyDeclarationSyntax ||
                    node is EventDeclarationSyntax ||
                    node is IndexerDeclarationSyntax ||
                    node is BaseTypeDeclarationSyntax) // class / struct / interface / enum
                {
                    targetNode = node;
                    break;
                }

                node = node.Parent;
            }

            if (targetNode == null)
                return await SymbolFinder.FindSymbolAtPositionAsync(document, position, token).ConfigureAwait(false);
            
            var declared = semanticModel.GetDeclaredSymbol(targetNode, token);
            if (declared != null)
                return declared;

            return await SymbolFinder.FindSymbolAtPositionAsync(
                document, position, token).ConfigureAwait(false);
        }
    }
}