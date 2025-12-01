using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SSDoc.Roslyn
{
    /// <summary>
    /// Represents the DocumentEditor class.
    /// </summary>
    public sealed class DocumentEditor
    {
        /// <summary>
        /// Performs the InsertAsync operation.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="symbol">The symbol.</param>
        /// <param name="documentationText">The documentation text.</param>
        /// <param name="token">The token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<Solution> InsertAsync(
            Document document,
            SemanticModel semanticModel,
            ISymbol symbol,
            string documentationText,
            CancellationToken token)
        {
            var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (root == null)
                return document.Project.Solution;

            var syntaxRef = symbol.DeclaringSyntaxReferences.Length > 0
                ? symbol.DeclaringSyntaxReferences[0]
                : null;

            if (syntaxRef == null)
                return document.Project.Solution;

            var node = await syntaxRef.GetSyntaxAsync(token);
            var leading = node.GetLeadingTrivia();

            var indent = ExtractIndent(leading);

            var prefixLeading = RemoveLastIndentTrivia(leading);

            var indentedDoc = BuildIndentedDocumentationWithTrailingNewLine(documentationText, indent);

            var docTrivia = SyntaxFactory.ParseLeadingTrivia(indentedDoc);

            var newLeading = prefixLeading
                .AddRange(docTrivia)
                .Add(SyntaxFactory.Whitespace(indent));


            var newNode = node.WithLeadingTrivia(newLeading);
            var newRoot = root.ReplaceNode(node, newNode);

            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        private static string ExtractIndent(SyntaxTriviaList trivia)
        {
            var ws = default(SyntaxTrivia);

            for (var i = trivia.Count - 1; i >= 0; i--)
            {
                var t = trivia[i];
                if (!t.IsKind(SyntaxKind.WhitespaceTrivia)) continue;
                ws = t;
                break;
            }

            return ws.ToString();
        }

        private static string BuildIndentedDocumentationWithTrailingNewLine(string text, string indent)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var normalized = text.Replace("\r\n", "\n");
            normalized = normalized.TrimEnd();

            var lines = normalized.Split('\n');
            var sb = new StringBuilder();

            foreach (var t in lines)
            {
                sb.Append(indent);
                sb.Append(t);
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        private static SyntaxTriviaList RemoveLastIndentTrivia(SyntaxTriviaList trivia)
        {
            var lastWhitespaceIndex = -1;

            for (var i = trivia.Count - 1; i >= 0; i--)
            {
                if (!trivia[i].IsKind(SyntaxKind.WhitespaceTrivia)) continue;
                lastWhitespaceIndex = i;
                break;
            }

            if (lastWhitespaceIndex < 0)
                return trivia;

            var list = new SyntaxTriviaList();

            return trivia.Where((t, i) => i != lastWhitespaceIndex).Aggregate(list, (current, t) => current.Add(t));
        }

    }
}
