using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// The configuration for a single platform.
    /// </summary>
    [Serializable]
    public class PlatformConfiguration
    {
        /// <summary>
        /// Constant overrides for this platform.
        /// </summary>
        public Dictionary<string, string> Constants = new Dictionary<string, string>();
        /// <summary>
        /// Tool overrides for this platform.
        /// </summary>
        public Dictionary<string, string> Tools = new Dictionary<string, string>();
        /// <summary>
        /// Source overrides for this platform.
        /// </summary>
        public Dictionary<string, List<SourceFile>> Sources = new Dictionary<string, List<SourceFile>>();
        /// <summary>
        /// Dependancy list overrides for this platform.
        /// </summary>
        public Dictionary<string, List<string>> DependancyLists = new Dictionary<string, List<string>>();
    }
}