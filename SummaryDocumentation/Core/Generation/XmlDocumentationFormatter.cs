using System.Text;
using SummaryDocumentation.Core.Model;

namespace SummaryDocumentation.Core.Generation
{
    /// <summary>
    /// Represents the XmlDocumentationFormatter class.
    /// </summary>
    public static class XmlDocumentationFormatter
    {
        /// <summary>
        /// Performs the Format operation.
        /// </summary>
        /// <param name="model">The model parameter.</param>
        /// <returns>The string result.</returns>
        public static string Format(DocumentationModel model)
        {
            var builder = new StringBuilder();

            // <summary>
            if (!string.IsNullOrWhiteSpace(model.Summary))
            {
                builder.AppendLine("/// <summary>");
                builder.AppendLine($"/// {NormalizeSentence(model.Summary)}");
                builder.AppendLine("/// </summary>");
            }

            // <typeparam>
            foreach (var typeParam in model.TypeParameters)
            {
                builder.AppendLine(
                    $"/// <typeparam name=\"{typeParam.Name}\">{EnsureSentence(typeParam.Description)}</typeparam>");
            }

            // <param>
            foreach (var param in model.Parameters)
            {
                builder.AppendLine(
                    $"/// <param name=\"{param.Name}\">{EnsureSentence(param.Description)}</param>");
            }

            // <returns>
            if (!string.IsNullOrWhiteSpace(model.Returns))
            {
                builder.AppendLine($"/// <returns>{EnsureSentence(model.Returns)}</returns>");
            }

            return builder.ToString().TrimEnd();
        }

        private static string NormalizeSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();

            if (char.IsLower(text[0]))
            {
                text = char.ToUpperInvariant(text[0]) + text.Substring(1);
            }

            if (!text.EndsWith("."))
            {
                text += ".";
            }

            return text;
        }

        private static string EnsureSentence(string text)
        {
            return NormalizeSentence(text);
        }
    }
}
