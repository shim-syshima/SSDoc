using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using SSDoc.Services;

namespace SSDoc.Roslyn
{
    /// <summary>
    /// Represents the RoslynContext class.
    /// </summary>
    public sealed class RoslynContext
    {
        private readonly VisualStudioWorkspace _workspace;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoslynContext"/> class.
        /// </summary>
        /// <param name="workspace">The workspace parameter.</param>
        public RoslynContext(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        /// <summary>
        /// Gets the document context async.
        /// </summary>
        /// <param name="caret">The caret.</param>
        /// <param name="token">The token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<Tuple<Document, SemanticModel, int>> GetDocumentContextAsync(
            CaretInfo caret,
            CancellationToken token)
        {
            var solution = _workspace.CurrentSolution;

            var documentId = solution.GetDocumentIdsWithFilePath(caret.FilePath);
            var document = solution.GetDocument(documentId.FirstOrDefault());
            if (document == null)
                throw new InvalidOperationException("Document not found: " + caret.FilePath);

            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            if (semanticModel == null)
                throw new InvalidOperationException("SemanticModel not available: " + caret.FilePath);

            var text = await document.GetTextAsync(token).ConfigureAwait(false);

            var lineIndex = caret.Line;
            if (lineIndex < 0) lineIndex = 0;
            if (lineIndex >= text.Lines.Count) lineIndex = text.Lines.Count - 1;

            var line = text.Lines[lineIndex];

            var column = caret.Column;
            if (column < 0) column = 0;
            if (column > line.Span.Length) column = line.Span.Length;

            var position = line.Start + column;

            return Tuple.Create(document, semanticModel, position);
        }
    }
}
