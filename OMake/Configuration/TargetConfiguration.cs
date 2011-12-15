using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// The configuration for a single target.
    /// </summary>
    [Serializable]
    public class TargetConfiguration
    {
        /// <summary>
        /// The valid platforms.
        /// </summary>
        public List<string> Platforms = new List<string>();
        /// <summary>
        /// A lookup for the aliases of various platforms.
        /// </summary>
        public Dictionary<string, string> PlatformAliases = new Dictionary<string, string>();
        /// <summary>
        /// The configurations for each platform.
        /// </summary>
        public Dictionary<string, PlatformConfiguration> Configs = new Dictionary<string, PlatformConfiguration>();
        /// <summary>
        /// The actual things to do.
        /// </summary>
        public List<Statement> Statements = new List<Statement>();

        /// <summary>
        /// The lists of dependancies.
        /// </summary>
        public Dictionary<string, List<string>> TargetDependancyLists = new Dictionary<string, List<string>>();
        /// <summary>
        /// The lists of sources specific to the target.
        /// </summary>
        public Dictionary<string, List<FileDependancy>> TargetSources = new Dictionary<string, List<FileDependancy>>();
        /// <summary>
        /// The constants specific to the target.
        /// </summary>
        public Dictionary<string, string> TargetConstants = new Dictionary<string, string>();
        /// <summary>
        /// The tools specific to the target.
        /// </summary>
        public Dictionary<string, string> TargetTools = new Dictionary<string, string>();

    }
}