using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;

namespace SSDoc.Services
{
    public sealed class DocumentEditorService
    {
        private readonly VisualStudioWorkspace workspace;

        public DocumentEditorService(VisualStudioWorkspace workspace)
        {
            this.workspace = workspace;
        }

        public async Task<bool> AddDocumentationAsync(
            Document document,
            ISymbol symbol,
            string documentationText,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                return false;
            }

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            var syntaxReference = symbol.DeclaringSyntaxReferences[0];
            var node = await syntaxReference.GetSyntaxAsync(cancellationToken);
            if (node == null)
            {
                return false;
            }

            var sourceText = await document.GetTextAsync(cancellationToken);

            // 対象メンバー行のインデント
            var indent = GetIndentation(sourceText, node.SpanStart);

            // 元の LeadingTrivia を解析して、
            //  - 先頭の空行など（行間）は prefix として保持
            //  - 最後のインデント空白は取り除く（後で付け直す）
            var originalLeading = node.GetLeadingTrivia();
            var (prefixTrivia, _) = SplitLeadingTrivia(originalLeading);

            // コメントテキストにインデントを付与して解析
            var indentedDocumentation = IndentDocumentation(documentationText, indent);
            var documentationTrivia = SyntaxFactory.ParseLeadingTrivia(indentedDocumentation);

            // プロパティ行用のインデントトリビア
            var indentTrivia = string.IsNullOrEmpty(indent)
                ? default
                : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indent));

            // 空行(元々の prefix) → コメント → インデント → 宣言
            var newLeading = prefixTrivia
                .AddRange(documentationTrivia)
                .AddRange(indentTrivia);

            var newNode = node.WithLeadingTrivia(newLeading);

            var newRoot = root.ReplaceNode(node, newNode);
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);

            return workspace.TryApplyChanges(newSolution);
        }

        /// <summary>
        /// 行頭のインデント（空白／タブ）を取得。
        /// </summary>
        private static string GetIndentation(SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);
            var lineText = line.ToString();

            var i = 0;
            while (i < lineText.Length &&
                   char.IsWhiteSpace(lineText[i]) &&
                   lineText[i] != '\r' &&
                   lineText[i] != '\n')
            {
                i++;
            }

            return lineText.Substring(0, i);
        }

        /// <summary>
        /// コメントテキストを各行インデント付きに整形。
        /// </summary>
        private static string IndentDocumentation(string documentationText, string indent)
        {
            var normalized = documentationText.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');

            var builder = new StringBuilder();

            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(indent);
                builder.Append(lines[i]);
            }

            // コメントブロックの最後に改行を 1 行入れる
            builder.AppendLine();

            return builder.ToString();
        }

        /// <summary>
        /// 先頭側の空行などはそのまま、最後のインデント空白だけ切り離す。
        /// </summary>
        private static (SyntaxTriviaList prefix, SyntaxTrivia indentTrivia) SplitLeadingTrivia(SyntaxTriviaList original)
        {
            if (original.Count == 0)
            {
                return (original, default);
            }

            var last = original[original.Count - 1];

            // 最後が WhitespaceTrivia の場合はそれをインデント扱いとして外す
            if (last.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                var prefix = original.RemoveAt(original.Count - 1);
                return (prefix, last);
            }

            // それ以外なら全部 prefix として扱う
            return (original, default);
        }
    }
}
