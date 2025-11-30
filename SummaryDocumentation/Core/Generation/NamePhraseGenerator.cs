using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SummaryDocumentation.Core.Generation
{
    /// <summary>
    /// Represents the NamePhraseGenerator class.
    /// </summary>
    public static class NamePhraseGenerator
    {
        private static readonly Regex PascalCaseSplitRegex =
            new Regex("([A-Z][a-z0-9]*)", RegexOptions.Compiled);

        public static string CreateDisplayName(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            var cleanedName = RemoveAsyncSuffix(identifier);
            var words = SplitPascalCase(cleanedName);
            if (words.Count == 0)
            {
                return identifier.ToLowerInvariant();
            }

            if (IsCommonVerb(words[0]))
            {
                words.RemoveAt(0);
            }

            if (words.Count == 0)
            {
                return identifier.ToLowerInvariant();
            }

            var builder = new StringBuilder();
            for (var index = 0; index < words.Count; index++)
            {
                var word = words[index].ToLowerInvariant();
                if (index == 0)
                {
                    builder.Append(word);
                }
                else
                {
                    builder.Append(' ');
                    builder.Append(word);
                }
            }

            return builder.ToString();
        }

        public static string CreateMethodSummaryVerb(string methodName, bool isAsync)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return isAsync ? "Asynchronously executes" : "Executes";
            }

            var cleanedName = RemoveAsyncSuffix(methodName);
            var words = SplitPascalCase(cleanedName);
            if (words.Count == 0)
            {
                return isAsync ? "Asynchronously executes" : "Executes";
            }

            var first = words[0].ToLowerInvariant();
            if (isAsync)
            {
                if (first == "get")
                {
                    return "Asynchronously gets";
                }

                if (first == "set")
                {
                    return "Asynchronously sets";
                }

                if (first == "create")
                {
                    return "Asynchronously creates";
                }

                return "Asynchronously " + first;
            }

            if (first == "get")
            {
                return "Gets";
            }

            if (first == "set")
            {
                return "Sets";
            }

            if (first == "create")
            {
                return "Creates";
            }

            return char.ToUpperInvariant(first[0]) + first.Substring(1);
        }

        public static string RemoveAsyncSuffix(string identifier)
        {
            if (identifier.EndsWith("Async", StringComparison.Ordinal))
            {
                return identifier.Substring(0, identifier.Length - "Async".Length);
            }

            return identifier;
        }

        private static List<string> SplitPascalCase(string identifier)
        {
            var matches = PascalCaseSplitRegex.Matches(identifier);
            if (matches.Count == 0)
            {
                return new List<string> { identifier };
            }

            return matches
                .Cast<Match>()
                .Select(match => match.Value)
                .ToList();
        }

        private static bool IsCommonVerb(string word)
        {
            var lower = word.ToLowerInvariant();
            if (lower == "get")
            {
                return true;
            }

            if (lower == "set")
            {
                return true;
            }

            if (lower == "create")
            {
                return true;
            }

            if (lower == "build")
            {
                return true;
            }

            if (lower == "add")
            {
                return true;
            }

            if (lower == "remove")
            {
                return true;
            }

            return false;
        }
    }
}
