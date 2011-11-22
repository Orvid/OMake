using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OMake
{
    /// <summary>
    /// A simple class to evaluate wildcards. It is 
    /// also used as a caching point for Regex 
    /// expressions.
    /// </summary>
    public static class WildcardEvaluator
    {
        /// <summary>
        /// This cache is for wildcards.
        /// </summary>
        private static Dictionary<string, Regex> WildcardCache = new Dictionary<string, Regex>();
        /// <summary>
        /// This cache is for Regex expressions.
        /// </summary>
        private static Dictionary<string, Regex> RegexCache = new Dictionary<string, Regex>();

        /// <summary>
        /// Checks if the specified string matches the specified wildcard.
        /// </summary>
        /// <param name="wildcard">The wildcard to search with.</param>
        /// <param name="strToCheck">The string to check against.</param>
        /// <returns>True if it's a match, otherwise false.</returns>
        public static bool IsWildcardMatch(string wildcard, string strToCheck)
        {
            if (!WildcardCache.ContainsKey(wildcard))
            {
                WildcardCache.Add(wildcard, new Regex("^" + (Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".")) + "$", RegexOptions.Compiled));
            }
            return WildcardCache[wildcard].IsMatch(strToCheck);
        }

        /// <summary>
        /// Checks if the specified string matches the specified regex.
        /// </summary>
        /// <param name="wildcard">The regex to search with.</param>
        /// <param name="strToCheck">The string to check against.</param>
        /// <returns>True if it's a match, otherwise false.</returns>
        public static bool IsRegexMatch(string regex, string strToCheck)
        {
            if (!RegexCache.ContainsKey(regex))
            {
                RegexCache.Add(regex, new Regex(regex, RegexOptions.Compiled));
            }
            return RegexCache[regex].IsMatch(strToCheck);
        }

        /// <summary>
        /// Cleans out the caches, and runs a full 
        /// garbage collection.
        /// </summary>
        public static void Cleanup()
        {
            WildcardCache.Clear();
            WildcardCache = null;
            RegexCache.Clear();
            RegexCache = null;
            System.GC.Collect();
        }
    }
}
