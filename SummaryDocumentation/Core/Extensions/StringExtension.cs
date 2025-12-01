using SummaryDocumentation.Core.Generation;

namespace SummaryDocumentation.Core.Extensions
{
    /// <summary>
    /// Represents the <see cref="StringExtension"/> class.
    /// </summary>
    internal static class StringExtension
    {
        /// <summary>
        /// Performs the ToSimple operation.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The string result.</returns>
        public static string ToSimple(this string text)
        {
            return NamePhraseHelper.ToTypeNounPhrase(text);
        }
    }
}
