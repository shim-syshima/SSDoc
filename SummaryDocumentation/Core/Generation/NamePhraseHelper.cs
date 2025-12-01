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
        private static readonly HashSet<string> KnownVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Get", "Set", "Add", "Remove", "Delete", "Clear",
            "Find", "Search", "Lookup", "Resolve",
            "Create", "Build", "Generate", "Construct",
            "Update", "Refresh", "Reload",
            "Load", "Save", "Read", "Write",
            "Open", "Close",
            "Start", "Stop", "Begin", "End",
            "Reset", "Restart",
            "Insert", "Append", "Prepend", "Merge", "Split", "Join",
            "Move", "Copy", "Clone", "Replace",
            "Calculate", "Compute", "Evaluate", "Measure",
            "Convert", "Cast", "Map",
            "Format", "Parse", "Serialize", "Deserialize",
            "Send", "Post", "Publish", "Dispatch",
            "Receive", "Subscribe", "Unsubscribe",
            "Render", "Draw", "Paint", "Layout",
            "Validate", "Verify", "Check", "Ensure",
            "Register", "Unregister",
            "Enable", "Disable", "Activate", "Deactivate",
            "Initialize", "Init", "Finalize",
            "Execute", "Invoke", "Call", "Raise", "Handle",
            "Schedule", "Cancel", "Abort",
            "Lock", "Unlock",
            "Attach", "Detach",
            "Authorize", "Authenticate",
        };

        private static readonly HashSet<string> KnownAcronyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID", "UID",
            "XML", "HTML", "JSON", "YAML",
            "URI", "URL",
            "CPU", "GPU", "RAM", "ROM",
            "UI", "UX",
            "DB", "SQL",
            "IO",
            "IP", "TCP", "UDP", "HTTP", "HTTPS",
            "RGB", "RGBA",
            "VM", "OS",
            "VS",
        };

        private static readonly Dictionary<string, string> IrregularVerbs =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "be", "is" },
                { "have", "has" },
                { "do", "does" },
                { "go", "goes" },
            };

        /// <summary>
        /// Performs the SplitIdentifier operation.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>The split tokens.</returns>
        public static IList<string> SplitIdentifier(string name)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(name))
                return result;

            var sb = new StringBuilder();

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];

                // '_' / '-' は強制区切り
                if (c == '_' || c == '-')
                {
                    Flush(sb, result);
                    continue;
                }

                if (i > 0)
                {
                    var prev = name[i - 1];

                    // lower → Upper （Pascal / camel）
                    if (char.IsLower(prev) && char.IsUpper(c))
                    {
                        Flush(sb, result);
                    }
                    // Letter → Digit / Digit → Letter
                    else if ((char.IsLetter(prev) && char.IsDigit(c)) ||
                             (char.IsDigit(prev) && char.IsLetter(c)))
                    {
                        Flush(sb, result);
                    }
                    // Acronym + 普通の単語: XMLDocument → XML + Document
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

        /// <summary>
        /// Gets the to simple words.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToSimpleWords(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var tokens = SplitIdentifier(name);

            var words = tokens
                .Where(t => t.Length > 0)
                .Select(t =>
                {
                    if (IsAllUpper(t) && KnownAcronyms.Contains(t))
                    {
                        return t;
                    }

                    return t.ToLowerInvariant();
                });

            return string.Join(" ", words);
        }

        /// <summary>
        /// Converts to the noun phrase.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToNounPhrase(string name)
        {
            var core = ToSimplePhrase(name);
            if (string.IsNullOrEmpty(core))
                return string.Empty;

            return "the " + core;
        }

        /// <summary>
        /// Converts to the simple phrase.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        public static string ToSimplePhrase(string name)
        {
            var core = ToSimpleWords(name);
            if (string.IsNullOrEmpty(core))
                return string.Empty;

            return core;
        }

        /// <summary>
        /// Converts to the type noun phrase.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The string result.</returns>
        public static string ToTypeNounPhrase(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return string.Empty;

            if (typeName.Length > 2 &&
                typeName[0] == 'I' &&
                char.IsUpper(typeName[1]))
            {
                typeName = typeName.Substring(1);
            }

            return ToNounPhrase(typeName);
        }

        /// <summary>
        /// Performs the ToBoolCorePhrase operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The string result.</returns>
        /// <remarks>
        /// Used with: "A value indicating whether {0}."
        /// </remarks>
        public static string ToBoolCorePhrase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "the value is set";
            }

            var lower = name.ToLowerInvariant();

            // isEnabled → "the value is enabled"
            if (lower.StartsWith("is", StringComparison.Ordinal) && name.Length > 2)
            {
                var core = name.Substring(2);
                var words = ToSimpleWords(core);
                return "the value is " + words;
            }

            // hasChildren → "the value has children"
            if (lower.StartsWith("has", StringComparison.Ordinal) && name.Length > 3)
            {
                var core = name.Substring(3);
                var words = ToSimpleWords(core);
                return "the value has " + words;
            }

            // canExecute → "the value can execute"
            if (lower.StartsWith("can", StringComparison.Ordinal) && name.Length > 3)
            {
                var core = name.Substring(3);
                var words = ToSimpleWords(core);
                return "the value can " + words;
            }

            // shouldSerialize → "the value should serialize"
            if (lower.StartsWith("should", StringComparison.Ordinal) && name.Length > 6)
            {
                var core = name.Substring(6);
                var words = ToSimpleWords(core);
                return "the value should " + words;
            }

            // mustXXX → "the value must XXX"
            if (lower.StartsWith("must", StringComparison.Ordinal) && name.Length > 4)
            {
                var core = name.Substring(4);
                var words = ToSimpleWords(core);
                return "the value must " + words;
            }

            // useCache → "the cache is used"
            if (lower.StartsWith("use", StringComparison.Ordinal) && name.Length > 3)
            {
                var core = name.Substring(3);
                var words = ToSimpleWords(core);
                return "the " + words + " is used";
            }

            var simple = ToSimpleWords(name);
            return string.Format(
                CultureInfo.InvariantCulture,
                "the {0} is set",
                simple);
        }

        /// <summary>
        /// Builds a method summary sentence such as:
        /// "Applies the value." / "Finds the font file."
        /// </summary>
        /// <param name="methodName">The method name.</param>
        /// <returns>The summary sentence.</returns>
        public static string ToMethodSummary(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return string.Empty;

            var tokens = SplitIdentifier(methodName);
            if (tokens.Count == 0)
                return string.Empty;

            var first = tokens[0];

            // TryGetValue → Tries to get the value.
            if (string.Equals(first, "Try", StringComparison.OrdinalIgnoreCase) &&
                tokens.Count > 1)
            {
                var innerVerb = tokens[1];
                var verb = ToThirdPersonSingular(innerVerb);
                var nounTokens = tokens.Skip(2).ToList();
                var nounPhrase = nounTokens.Count > 0
                    ? ToNounPhraseFromTokens(nounTokens)
                    : "the value";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Tries to {0} {1}.",
                    verb.ToLowerInvariant(),
                    nounPhrase);
            }

            if (string.Equals(first, "To", StringComparison.OrdinalIgnoreCase) &&
                tokens.Count > 1)
            {
                var nounTokens = tokens.Skip(1).ToList();
                var nounPhrase = ToNounPhraseFromTokens(nounTokens);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Converts to {0}.",
                    nounPhrase);
            }

            if (KnownVerbs.Contains(first))
            {
                var verb = ToThirdPersonSingular(first);
                var nounTokens = tokens.Skip(1).ToList();
                var nounPhrase = nounTokens.Count > 0
                    ? ToNounPhraseFromTokens(nounTokens)
                    : "the value";

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1}.",
                    verb,
                    nounPhrase);
            }

            var defaultNoun = ToNounPhrase(methodName);
            if (string.IsNullOrEmpty(defaultNoun))
                return string.Empty;

            return string.Format(
                CultureInfo.InvariantCulture,
                "Gets {0}.",
                defaultNoun);
        }

        private static void Flush(StringBuilder sb, IList<string> result)
        {
            if (sb.Length == 0)
                return;

            result.Add(sb.ToString());
            sb.Clear();
        }

        private static string ToNounPhraseFromTokens(IList<string> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return "the value";

            var parts = tokens.Select(t =>
            {
                if (IsAllUpper(t) && KnownAcronyms.Contains(t))
                    return t;

                return t.ToLowerInvariant();
            });

            var core = string.Join(" ", parts);
            return "the " + core;
        }

        private static string ToThirdPersonSingular(string verb)
        {
            if (string.IsNullOrEmpty(verb))
                return string.Empty;

            string irregular;
            if (IrregularVerbs.TryGetValue(verb, out irregular))
            {
                return irregular;
            }

            var lower = verb.ToLowerInvariant();

            if (lower.EndsWith("s", StringComparison.Ordinal) ||
                lower.EndsWith("sh", StringComparison.Ordinal) ||
                lower.EndsWith("ch", StringComparison.Ordinal) ||
                lower.EndsWith("x", StringComparison.Ordinal) ||
                lower.EndsWith("z", StringComparison.Ordinal) ||
                lower.EndsWith("o", StringComparison.Ordinal))
            {
                return verb + "es";
            }

            if (lower.EndsWith("y", StringComparison.Ordinal) && lower.Length > 1)
            {
                var beforeY = lower[lower.Length - 2];
                if (!"aeiou".Contains(beforeY))
                {
                    return verb.Substring(0, verb.Length - 1) + "ies";
                }
            }

            return verb + "s";
        }

        private static bool IsAllUpper(string s)
        {
            return s.All(t => !char.IsLetter(t) || char.IsUpper(t));
        }
    }
}
