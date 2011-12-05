using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;

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
        /// The binary formatter which we use for caching regex's.
        /// </summary>
        private static BinaryFormatter binform = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.File));

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
                if (Cache.Contains("OMake.RegexCache.Wildcard-" + wildcard))
                {
                    MemoryStream ms = new MemoryStream(Cache.GetData("OMake.RegexCache.Wildcard-" + wildcard));
                    WildcardCache.Add(wildcard, (Regex)binform.Deserialize(ms));
                }
                else
                {
                    WildcardCache.Add(wildcard, new Regex("^" + (Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".")) + "$", RegexOptions.Compiled));
                    WildcardCache[wildcard].IsMatch(""); // Ensure it get's fully compiled.
                    MemoryStream ms = new MemoryStream();
                    binform.Serialize(ms, WildcardCache[wildcard]);
                    Cache.SetValue("OMake.RegexCache.Wildcard-" + wildcard, (byte[])ms.GetBuffer());
                }
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
                if (Cache.Contains("OMake.RegexCache.Regex-" + regex))
                {
                    MemoryStream ms = new MemoryStream(Cache.GetData("OMake.RegexCache.Regex-" + regex));
                    RegexCache.Add(regex, (Regex)binform.Deserialize(ms));
                }
                else
                {
                    RegexCache.Add(regex, new Regex(regex, RegexOptions.Compiled));
                    RegexCache[regex].IsMatch(""); // Ensure it get's fully compiled.
                    MemoryStream ms = new MemoryStream();
                    binform.Serialize(ms, RegexCache[regex]);
                    Cache.SetValue("OMake.RegexCache.Regex-" + regex, (byte[])ms.GetBuffer());
                }
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
