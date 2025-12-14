using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SSDoc.Roslyn
{
    /// <summary>
    /// Represents the DocumentEditor class.
    /// </summary>
    public sealed class DocumentEditor
    {
        /// <summary>
        /// Inserts the async.
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

            var syntaxRefs = symbol.DeclaringSyntaxReferences;
            if (syntaxRefs == null || syntaxRefs.Length == 0)
                return document.Project.Solution;

            var syntaxRef = syntaxRefs[0];
            var node = await syntaxRef.GetSyntaxAsync(token).ConfigureAwait(false);

            var leading = node.GetLeadingTrivia();
            var filteredTrivia = new List<SyntaxTrivia>();

            foreach (var trivia in leading)
            {
                var structure = trivia.GetStructure();
                var isDocComment = structure is DocumentationCommentTriviaSyntax;

                if (isDocComment)
                {
                    if (filteredTrivia.Count > 0 &&
                        filteredTrivia[filteredTrivia.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        filteredTrivia.RemoveAt(filteredTrivia.Count - 1);
                    }

                    continue;
                }

                filteredTrivia.Add(trivia);
            }

            var baseTrivia = SyntaxFactory.TriviaList(filteredTrivia);

            var indent = ExtractIndent(baseTrivia);
            var prefixTrivia = RemoveLastIndentTrivia(baseTrivia);

            var docTrivia = SyntaxFactory.ParseLeadingTrivia(
                BuildIndentedDocumentationWithTrailingNewLine(documentationText, indent));

            var newLeading = prefixTrivia
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
