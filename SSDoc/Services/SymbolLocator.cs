using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSDoc.Services
{
    public sealed class SymbolLocator
    {
        private readonly VisualStudioWorkspace workspace;
        private readonly IServiceProvider serviceProvider;

        public SymbolLocator(VisualStudioWorkspace workspace, IServiceProvider serviceProvider)
        {
            this.workspace = workspace;
            this.serviceProvider = serviceProvider;
        }

        public async Task<ISymbol> FindSymbolAtCaretAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var textView = GetActiveView();
            if (textView == null)
            {
                return null;
            }

            textView.GetCaretPos(out var lineNumber, out var column);
            textView.GetBuffer(out var lines);

            if (!TryGetFilePath(lines, out var filePath))
            {
                return null;
            }

            var document = FindDocumentByFilePath(filePath);
            if (document == null)
            {
                return null;
            }

            var sourceText = await document.GetTextAsync(cancellationToken);
            if (!TryGetPosition(sourceText, lineNumber, column, out var position))
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                return null;
            }

            var token = root.FindToken(position);
            var node = token.Parent;
            if (node == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return null;
            }

            var declaredSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (declaredSymbol != null)
            {
                return declaredSymbol;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            return symbolInfo.Symbol;
        }

        private IVsTextView GetActiveView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textManager = serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null)
            {
                return null;
            }

            textManager.GetActiveView(1, null, out var view);
            return view;
        }

        private static bool TryGetFilePath(IVsTextLines lines, out string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            filePath = null;

            if (lines is IPersistFileFormat persistFileFormat)
            {
                persistFileFormat.GetCurFile(out filePath, out _);
            }

            return !string.IsNullOrEmpty(filePath);
        }

        private Document FindDocumentByFilePath(string filePath)
        {
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (string.Equals(document.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return document;
                    }
                }
            }

            return null;
        }

        private static bool TryGetPosition(
            SourceText sourceText,
            int lineNumber,
            int column,
            out int position)
        {
            position = 0;

            if (lineNumber < 0 || lineNumber >= sourceText.Lines.Count)
            {
                return false;
            }

            var line = sourceText.Lines[lineNumber];

            if (column < 0)
            {
                column = 0;
            }

            var lineLength = line.End - line.Start;
            if (column > lineLength)
            {
                column = lineLength;
            }

            position = line.Start + column;
            return true;
        }
    }
}
