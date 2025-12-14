using Microsoft.CodeAnalysis;
using System;
using System.Buffers;
using System.Collections.Generic;
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
            "Load", "Save", "Read", "Write", "Unload", "Restore",
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
            "Initialize", "Init", "Finalize", "Terminate",
            "Execute", "Invoke", "Call", "Raise", "Handle",
            "Schedule", "Cancel", "Abort",
            "Lock", "Unlock",
            "Attach", "Detach",
            "Authorize", "Authenticate",
            "Extract",
            "Protect", "Unprotect",
            "Import", "Export",
            "Press", "Release", "Toggle",
            "Swap", "Match"
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

        private const string AsyncSuffix = "Async";
        private const string ThePrefix = "the ";

        private struct Token
        {
            public int Start;
            public int Length;

            public Token(int start, int length)
            {
                Start = start;
                Length = length;
            }
        }

        // ---------------------------
        // Public API (compat)
        // ---------------------------

        /// <summary>
        /// Performs the SplitIdentifier operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The list result.</returns>
        public static IList<string> SplitIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new List<string>(0);
            }

            Token[] rented = null;
            int count = 0;

            try
            {
                rented = RentTokens(name.Length, out int capacity);
                count = Tokenize(name, rented);
                var list = new List<string>(count);

                for (int i = 0; i < count; i++)
                {
                    Token t = rented[i];
                    if (t.Length > 0)
                    {
                        list.Add(name.Substring(t.Start, t.Length));
                    }
                }

                return list;
            }
            finally
            {
                ReturnTokens(rented);
            }
        }

        public static string ToSimpleWords(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            Token[] rented = null;

            try
            {
                rented = RentTokens(name.Length, out _);
                int count = Tokenize(name, rented);
                if (count == 0)
                {
                    return string.Empty;
                }

                var sb = new StringBuilder(name.Length + 8);
                AppendWordsFromTokens(sb, name, rented, 0, count);
                return sb.ToString();
            }
            finally
            {
                ReturnTokens(rented);
            }
        }

        public static string ToNounPhrase(string name)
        {
            var core = ToSimpleWords(name);
            if (string.IsNullOrEmpty(core))
            {
                return string.Empty;
            }

            return ThePrefix + core;
        }

        public static string ToTypeNounPhrase(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return string.Empty;
            }

            // I*** -> interface name convention
            if (typeName.Length > 2 &&
                typeName[0] == 'I' &&
                IsUpper(typeName[1]))
            {
                typeName = typeName.Substring(1);
            }

            return ToNounPhrase(typeName);
        }

        /// <remarks>
        /// Intended to be used as:
        /// "A value indicating whether {0}."
        /// </remarks>
        public static string ToBoolCorePhrase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "the value is set";
            }

            // Fast prefix checks w/o lowercasing the entire string
            if (StartsWithIgnoreCase(name, "Is") && name.Length > 2)
            {
                return "the value is " + ToSimpleWords(name.Substring(2));
            }

            if (StartsWithIgnoreCase(name, "Has") && name.Length > 3)
            {
                return "the value has " + ToSimpleWords(name.Substring(3));
            }

            if (StartsWithIgnoreCase(name, "Can") && name.Length > 3)
            {
                return "the value can " + ToSimpleWords(name.Substring(3));
            }

            if (StartsWithIgnoreCase(name, "Should") && name.Length > 6)
            {
                return "the value should " + ToSimpleWords(name.Substring(6));
            }

            if (StartsWithIgnoreCase(name, "Must") && name.Length > 4)
            {
                return "the value must " + ToSimpleWords(name.Substring(4));
            }

            if (StartsWithIgnoreCase(name, "Use") && name.Length > 3)
            {
                return "the " + ToSimpleWords(name.Substring(3)) + " is used";
            }

            return "the " + ToSimpleWords(name) + " is set";
        }

        // ---------------------------
        // Method summary generation (FAST PATH)
        // ---------------------------

        public static string ToMethodSummary(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return string.Empty;
            }

            bool isAsync = false;

            // Strip Async first (avoid tokenizing twice)
            if (methodName.Length > AsyncSuffix.Length &&
                methodName.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                methodName = methodName.Substring(0, methodName.Length - AsyncSuffix.Length);
                isAsync = true;
            }

            Token[] rented = null;

            try
            {
                rented = RentTokens(methodName.Length, out _);
                int count = Tokenize(methodName, rented);
                if (count == 0)
                {
                    return string.Empty;
                }

                string summary = BuildCoreMethodSummary(methodName, rented, count);
                if (string.IsNullOrEmpty(summary))
                {
                    return string.Empty;
                }

                if (isAsync)
                {
                    summary = TrimTrailingPeriod(summary) + " asynchronously.";
                }

                return summary;
            }
            finally
            {
                ReturnTokens(rented);
            }
        }

        public static string ToMethodSummary(IMethodSymbol method)
        {
            if (method == null)
            {
                return string.Empty;
            }

            string name = method.Name;

            if (string.Equals(name, "Initialize", StringComparison.Ordinal) &&
                method.Parameters.Length == 0 &&
                method.ContainingType != null)
            {
                string typeName = method.ContainingType.Name;
                return "Initializes a new instance of the <see cref=\"" + typeName + "\"/> class.";
            }

            if (string.Equals(name, "Terminate", StringComparison.Ordinal) &&
                method.Parameters.Length == 0 &&
                method.ContainingType != null)
            {
                string typeName = method.ContainingType.Name;
                return "Terminates an instance of the <see cref=\"" + typeName + "\"/> class.";
            }

            // bool-returning methods get bool-oriented summary, even if not Has/Can/Is prefix
            if (method.ReturnType != null && method.ReturnType.SpecialType == SpecialType.System_Boolean)
            {
                return ToBoolMethodSummary(name);
            }

            return ToMethodSummary(name);
        }

        private static string ToBoolMethodSummary(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return string.Empty;
            }

            bool isAsync = false;

            if (methodName.Length > AsyncSuffix.Length &&
                methodName.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                methodName = methodName.Substring(0, methodName.Length - AsyncSuffix.Length);
                isAsync = true;
            }

            Token[] rented = null;

            try
            {
                rented = RentTokens(methodName.Length, out _);
                int count = Tokenize(methodName, rented);
                if (count == 0)
                {
                    return string.Empty;
                }

                // Try* is already canonical for bool methods
                if (count >= 2 && TokenEqualsIgnoreCase(methodName, rented[0], "Try"))
                {
                    var sb = new StringBuilder(64);
                    sb.Append("Tries to ");
                    AppendLowerToken(sb, methodName, rented[1]); // base verb
                    sb.Append(' ');

                    if (count > 2)
                    {
                        AppendNounPhraseFromTokens(sb, methodName, rented, 2, count - 2);
                    }
                    else
                    {
                        sb.Append("the value");
                    }

                    sb.Append('.');
                    string s = sb.ToString();
                    return isAsync ? (TrimTrailingPeriod(s) + " asynchronously.") : s;
                }

                // Otherwise: prefer bool prefix patterns; fallback to generic "Determines whether ..."
                string core = BuildBoolPrefixSummary(methodName, rented, count);
                if (string.IsNullOrEmpty(core))
                {
                    // Minimal, but safe English
                    return isAsync
                        ? ("Determines whether " + ToSimpleWords(methodName) + " asynchronously.")
                        : ("Determines whether " + ToSimpleWords(methodName) + ".");
                }

                return isAsync ? (TrimTrailingPeriod(core) + " asynchronously.") : core;
            }
            finally
            {
                ReturnTokens(rented);
            }
        }

        private static string BuildCoreMethodSummary(string source, Token[] tokens, int count)
        {
            // We only allocate strings for comparisons when strictly needed (first token mainly).
            // Extract first token as string only once (small allocation).
            string first = source.Substring(tokens[0].Start, tokens[0].Length);

            // 1) TryXxx => "Tries to get ..."
            if (count >= 2 && EqualsIgnoreCase(first, "Try"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Tries to ");
                AppendLowerToken(sb, source, tokens[1]); // base verb
                sb.Append(' ');

                if (count > 2)
                {
                    AppendNounPhraseFromTokens(sb, source, tokens, 2, count - 2);
                }
                else
                {
                    sb.Append("the value");
                }

                sb.Append('.');
                return sb.ToString();
            }

            // 2) ToXxx => "Converts to the xxx."
            if (count >= 2 && EqualsIgnoreCase(first, "To"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Converts to ");
                AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            // 3) AsXxx => "Treats the value as xxx."
            if (count >= 2 && EqualsIgnoreCase(first, "As"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Treats the value as ");
                AppendBareNounPhraseFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            // 4) FromXxx => "Creates an instance from the xxx."
            if (count >= 2 && EqualsIgnoreCase(first, "From"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Creates an instance from ");
                AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            // 5) OnXxxChanged/Modified/Changing
            if (count >= 3 && EqualsIgnoreCase(first, "On"))
            {
                Token last = tokens[count - 1];

                if (TokenEqualsIgnoreCase(source, last, "Changed"))
                {
                    var sb = new StringBuilder(64);
                    sb.Append("Occurs when ");
                    AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 2);
                    sb.Append(" changes.");
                    return sb.ToString();
                }

                if (TokenEqualsIgnoreCase(source, last, "Modified"))
                {
                    var sb = new StringBuilder(64);
                    sb.Append("Occurs when ");
                    AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 2);
                    sb.Append(" is modified.");
                    return sb.ToString();
                }

                if (TokenEqualsIgnoreCase(source, last, "Changing"))
                {
                    var sb = new StringBuilder(64);
                    sb.Append("Occurs when ");
                    AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 2);
                    sb.Append(" is changing.");
                    return sb.ToString();
                }
            }

            // 6) Bool-ish method naming (method-only)
            string boolPrefix = BuildBoolPrefixSummary(source, tokens, count);
            if (!string.IsNullOrEmpty(boolPrefix))
            {
                return boolPrefix;
            }

            // 7) Known verbs
            if (KnownVerbs.Contains(first))
            {
                string verb3rd = ToThirdPersonSingular(first);
                var sb = new StringBuilder(64);
                sb.Append(verb3rd);
                sb.Append(' ');

                if (count > 1)
                {
                    AppendNounPhraseFromTokens(sb, source, tokens, 1, count - 1);
                }
                else
                {
                    sb.Append("the value");
                }

                sb.Append('.');
                return sb.ToString();
            }

            // 8) fallback (keep simple & cheap)
            return "Perform " + ToNounPhrase(source) + ".";
        }

        private static string BuildBoolPrefixSummary(string source, Token[] tokens, int count)
        {
            if (count < 2)
            {
                return string.Empty;
            }

            Token first = tokens[0];

            // IsXxx / HasXxx / CanXxx / ShouldXxx / NeedsXxx / AllowsXxx / SupportsXxx
            if (TokenEqualsIgnoreCase(source, first, "Is"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value is ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Has"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value has ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Can"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value can ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Should"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value should ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Needs"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value needs ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Allows"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value allows ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            if (TokenEqualsIgnoreCase(source, first, "Supports"))
            {
                var sb = new StringBuilder(64);
                sb.Append("Determines whether the value supports ");
                AppendWordsFromTokens(sb, source, tokens, 1, count - 1);
                sb.Append('.');
                return sb.ToString();
            }

            return string.Empty;
        }

        // ---------------------------
        // Tokenization (NO StringBuilder, NO Flush)
        // ---------------------------

        private static int Tokenize(string name, Token[] tokens)
        {
            int n = name.Length;
            int tokenStart = 0;
            int outCount = 0;

            for (int i = 0; i < n; i++)
            {
                char c = name[i];

                if (c == '_' || c == '-')
                {
                    AddTokenIfAny(tokens, ref outCount, tokenStart, i);
                    tokenStart = i + 1;
                    continue;
                }

                if (i == tokenStart)
                {
                    continue;
                }

                char prev = name[i - 1];

                // lower -> Upper
                if (IsLower(prev) && IsUpper(c))
                {
                    AddTokenIfAny(tokens, ref outCount, tokenStart, i);
                    tokenStart = i;
                    continue;
                }

                // Letter <-> Digit
                if ((IsLetter(prev) && IsDigit(c)) || (IsDigit(prev) && IsLetter(c)))
                {
                    AddTokenIfAny(tokens, ref outCount, tokenStart, i);
                    tokenStart = i;
                    continue;
                }

                // Acronym boundary: XMLDocument => XML + Document
                if (IsUpper(prev) && IsUpper(c))
                {
                    int nextIndex = i + 1;
                    if (nextIndex < n)
                    {
                        char next = name[nextIndex];
                        if (IsLower(next))
                        {
                            AddTokenIfAny(tokens, ref outCount, tokenStart, i);
                            tokenStart = i;
                            continue;
                        }
                    }
                }
            }

            AddTokenIfAny(tokens, ref outCount, tokenStart, n);
            return outCount;
        }

        private static void AddTokenIfAny(Token[] tokens, ref int outCount, int start, int endExclusive)
        {
            int len = endExclusive - start;
            if (len <= 0)
            {
                return;
            }

            tokens[outCount++] = new Token(start, len);
        }

        // ---------------------------
        // Append helpers (NO intermediate strings)
        // ---------------------------

        private static void AppendWordsFromTokens(StringBuilder sb, string source, Token[] tokens, int start, int length)
        {
            bool firstWord = true;

            for (int i = 0; i < length; i++)
            {
                Token t = tokens[start + i];
                if (t.Length <= 0)
                {
                    continue;
                }

                if (!firstWord)
                {
                    sb.Append(' ');
                }
                else
                {
                    firstWord = false;
                }

                AppendWordToken(sb, source, t);
            }
        }

        private static void AppendNounPhraseFromTokens(StringBuilder sb, string source, Token[] tokens, int start, int length)
        {
            sb.Append(ThePrefix);
            AppendWordsFromTokens(sb, source, tokens, start, length);
        }

        private static void AppendBareNounPhraseFromTokens(StringBuilder sb, string source, Token[] tokens, int start, int length)
        {
            // same as noun phrase but without "the "
            AppendWordsFromTokens(sb, source, tokens, start, length);
        }

        private static void AppendWordToken(StringBuilder sb, string source, Token token)
        {
            // If token is all upper and known acronym -> keep original casing
            if (IsAllUpper(source, token))
            {
                // allocate only for acronym candidates (usually tiny and rare)
                string text = source.Substring(token.Start, token.Length);
                if (KnownAcronyms.Contains(text))
                {
                    sb.Append(text);
                    return;
                }

                // otherwise: lower it
                AppendLowerToken(sb, source, token);
                return;
            }

            AppendLowerToken(sb, source, token);
        }

        private static void AppendLowerToken(StringBuilder sb, string source, Token token)
        {
            int end = token.Start + token.Length;
            for (int i = token.Start; i < end; i++)
            {
                sb.Append(char.ToLowerInvariant(source[i]));
            }
        }

        private static bool IsAllUpper(string source, Token token)
        {
            int end = token.Start + token.Length;
            for (int i = token.Start; i < end; i++)
            {
                char c = source[i];
                if (IsLetter(c) && !IsUpper(c))
                {
                    return false;
                }
            }

            return true;
        }

        // ---------------------------
        // String/Token comparisons (avoid allocations)
        // ---------------------------

        private static bool TokenEqualsIgnoreCase(string source, Token token, string text)
        {
            if (token.Length != text.Length)
            {
                return false;
            }

            for (int i = 0; i < token.Length; i++)
            {
                char a = source[token.Start + i];
                char b = text[i];

                if (a == b)
                {
                    continue;
                }

                if (char.ToUpperInvariant(a) != char.ToUpperInvariant(b))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EqualsIgnoreCase(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool StartsWithIgnoreCase(string s, string prefix)
        {
            return s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // ---------------------------
        // ArrayPool
        // ---------------------------

        private static Token[] RentTokens(int nameLength, out int capacity)
        {
            // Worst case tokens <= length (but separators reduce). Practical upper bound: length is safe but too big.
            // Use a heuristic: length/2 + 4 (e.g., "VeryLongMethodName" => ~4-6 tokens)
            capacity = (nameLength >> 1) + 4;
            return ArrayPool<Token>.Shared.Rent(capacity);
        }

        private static void ReturnTokens(Token[] tokens)
        {
            if (tokens != null)
            {
                ArrayPool<Token>.Shared.Return(tokens, clearArray: false);
            }
        }

        // ---------------------------
        // Misc helpers
        // ---------------------------

        private static string TrimTrailingPeriod(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            int len = s.Length;
            if (len > 0 && s[len - 1] == '.')
            {
                return s.Substring(0, len - 1);
            }

            return s;
        }

        private static bool IsUpper(char c) => c >= 'A' && c <= 'Z';
        private static bool IsLower(char c) => c >= 'a' && c <= 'z';
        private static bool IsDigit(char c) => c >= '0' && c <= '9';
        private static bool IsLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        private static string ToThirdPersonSingular(string verb)
        {
            if (string.IsNullOrEmpty(verb))
            {
                return string.Empty;
            }

            if (IrregularVerbs.TryGetValue(verb, out string irregular))
            {
                return irregular;
            }

            string lower = verb.ToLowerInvariant();

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
                char beforeY = lower[lower.Length - 2];
                if (beforeY != 'a' && beforeY != 'e' && beforeY != 'i' && beforeY != 'o' && beforeY != 'u')
                {
                    return verb.Substring(0, verb.Length - 1) + "ies";
                }
            }

            return verb + "s";
        }
    }
}
