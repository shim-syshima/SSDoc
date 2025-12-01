using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SummaryDocumentation.Core.Generation
{
    /// <summary>
    /// Represents the <see cref="NamePhraseHelper"/> class.
    /// </summary>
    internal static class NamePhraseHelper
    {
        /// <summary>
        /// Performs the SplitIdentifier operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The list result.</returns>
        public static IList<string> SplitIdentifier(string name)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(name))
                return result;

            var sb = new StringBuilder();

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];

                if (c is '_' or '-')
                {
                    Flush(sb, result);
                    continue;
                }

                if (i > 0)
                {
                    var prev = name[i - 1];

                    if ((char.IsLower(prev) && char.IsUpper(c)) || (char.IsDigit(prev) && char.IsLetter(c)) || (char.IsLetter(prev) && char.IsDigit(c)))
                    {
                        Flush(sb, result);
                    }
                    else if (char.IsUpper(prev) && char.IsUpper(c))
                    {
                        if (i + 1 < name.Length)
                        {
                            var next = name[i + 1];
                            if (char.IsLower(next))
                            {
                                Flush(sb, result);
                            }
                        }
                    }
                }

                sb.Append(c);
            }

            Flush(sb, result);
            return result;
        }

        private static void Flush(StringBuilder sb, IList<string> result)
        {
            if (sb.Length == 0)
                return;

            result.Add(sb.ToString());
            sb.Length = 0;
        }

        /// <summary>
        /// Performs the ToSimpleWords operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToSimpleWords(string name)
        {
            var words = SplitIdentifier(name);
            if (words.Count == 0)
                return name;

            for (var i = 0; i < words.Count; i++)
            {
                var w = words[i];

                if (IsAllUpper(w) && w.Length <= 4)
                {
                    words[i] = w;
                }
                else
                {
                    words[i] = w.ToLowerInvariant();
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Performs the ToNounPhrase operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToNounPhrase(string name)
        {
            return ToSimpleWords(name);
        }

        /// <summary>
        /// Performs the ToTypeNounPhrase operation.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The string result.</returns>
        public static string ToTypeNounPhrase(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName) &&
                typeName.Length > 1 &&
                typeName[0] == 'I' &&
                char.IsUpper(typeName[1]))
            {
                typeName = typeName.Substring(1);
            }

            return ToSimpleWords(typeName);
        }

        /// <summary>
        /// Performs the ToBoolCorePhrase operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToBoolCorePhrase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "the value is set";
            }

            var lower = name.ToLowerInvariant();

            if (lower.StartsWith("is") && lower.Length > 2)
            {
                // isEnabled → enabled
                var core = name.Substring(2);
                return core + " is enabled";
            }

            if (lower.StartsWith("has") && lower.Length > 3)
            {
                // hasItems → items are available
                var core = name.Substring(3);
                return core + " are available";
            }

            if (lower.StartsWith("can") && lower.Length > 3)
            {
                // canRead → reading is allowed
                var core = name.Substring(3);
                return core + " is allowed";
            }

            if (!lower.StartsWith("use") || lower.Length <= 3)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "the {0} is set",
                    name);
            {
                // useCache → the cache is used
                var core = name.Substring(3);
                return "the " + core + " is used";
            }
        }

        private static bool IsAllUpper(string s)
        {
            return s.All(t => !char.IsLetter(t) || char.IsUpper(t));
        }
    }
}
