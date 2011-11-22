using System;
using System.Collections.Generic;

namespace OMake
{
	/// <summary>
	/// An overall configuration.
	/// </summary>
	public class Configuration
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
        public List<string> Statements = new List<string>();

        /// <summary>
        /// The globally registered lists of sources.
        /// </summary>
        public Dictionary<string, List<string>> GlobalSources = new Dictionary<string, List<string>>();
        /// <summary>
        /// The globally registered constants.
        /// </summary>
        public Dictionary<string, string> GlobalConstants = new Dictionary<string, string>();
        /// <summary>
        /// The globally registered tools.
        /// </summary>
        public Dictionary<string, string> GlobalTools = new Dictionary<string, string>();

        /// <summary>
        /// Gets the value of the specified comment for the specified platform.
        /// </summary>
        /// <param name="platform">The platform to retrieve for. This can be an alias.</param>
        /// <param name="constant">The name of the constant to get the value of.</param>
        /// <returns>The value of the specified constant.</returns>
        public string GetConstant(string platform, string constant)
        {
            if (IsValidPlatform(platform))
            {
                string plat = ResolvePlatform(platform);
                if (Configs[plat].Constants.ContainsKey(constant))
                {
                    return Configs[plat].Constants[constant];
                }
                else
                {
                    return GlobalConstants[constant];
                }
            }
            else
            {
                throw new Exception("Something went wrong internally! A constant was requested for a platform that doesn't exist!");
            }
        }

        public string GetTool(string platform, string toolname)
        {
            if (IsValidPlatform(platform))
            {
                string plat = ResolvePlatform(platform);
                if (Configs[plat].Tools.ContainsKey(toolname))
                {
                    return Configs[plat].Tools[toolname];
                }
                else
                {
                    return GlobalTools[toolname];
                }
            }
            else
            {
                throw new Exception("Something went wrong internally! A tool was requested for a platform that doesn't exist!");
            }
        }

        public string ResolvePlatform(string platform)
        {
            if (Platforms.Contains(platform))
                return platform;
            else
                return PlatformAliases[platform];
        }

        public bool IsValidPlatform(string platform)
        {
            if (Platforms.Contains(platform) || PlatformAliases.ContainsKey(platform))
            {
                return true;
            }
            return false;
        }
	}
}
