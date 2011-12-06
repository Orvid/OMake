using System;
using System.Collections.Generic;

namespace OMake
{
	/// <summary>
	/// An overall configuration.
	/// </summary>
    [Serializable]
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
        /// The configurations for each target.
        /// </summary>
        public Dictionary<string, TargetConfiguration> Targets = new Dictionary<string, TargetConfiguration>();

        /// <summary>
        /// The lists of dependancies.
        /// </summary>
        public Dictionary<string, List<string>> GlobalDependancyLists = new Dictionary<string, List<string>>();
        /// <summary>
        /// The globally registered lists of sources.
        /// </summary>
        public Dictionary<string, List<SourceFile>> GlobalSources = new Dictionary<string, List<SourceFile>>();
        /// <summary>
        /// The globally registered constants.
        /// </summary>
        public Dictionary<string, string> GlobalConstants = new Dictionary<string, string>();
        /// <summary>
        /// The globally registered tools.
        /// </summary>
        public Dictionary<string, string> GlobalTools = new Dictionary<string, string>();

        public Configuration()
        {
            Targets.Add("all", new TargetConfiguration());
        }

        /// <summary>
        /// Gets the value of the specified comment for the specified platform.
        /// </summary>
        /// <param name="platform">The platform to retrieve for. This can be an alias.</param>
        /// <param name="constant">The name of the constant to get the value of.</param>
        /// <returns>The value of the specified constant.</returns>
        public string GetConstant(string platform, string target, string constant)
        {
            if (IsValidPlatform(platform, target))
            {
                string plat = ResolvePlatform(platform, target);
                if (Targets[target].Platforms.Contains(plat))
                {
                    if (Targets[target].Configs[plat].Constants.ContainsKey(constant))
                    {
                        return Targets[target].Configs[plat].Constants[constant];
                    }
                }
                if (Targets[target].TargetConstants.ContainsKey(constant))
                {
                    return Targets[target].TargetConstants[constant];
                }
                else if (Configs[plat].Constants.ContainsKey(constant))
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
                ErrorManager.Error(55, Processor.file, constant, platform, ResolvePlatform(platform, target));
                return "";
            }
        }

        public string GetTool(string platform, string target, string toolname)
        {
            if (IsValidPlatform(platform, target))
            {
                string plat = ResolvePlatform(platform, target);
                if (Targets[target].Platforms.Contains(plat))
                {
                    if (Targets[target].Configs[plat].Tools.ContainsKey(toolname))
                    {
                        return Targets[target].Configs[plat].Tools[toolname];
                    }
                }
                if (Targets[target].TargetTools.ContainsKey(toolname))
                {
                    return Targets[target].TargetTools[toolname];
                }
                else if (Configs[plat].Tools.ContainsKey(toolname))
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
                ErrorManager.Error(56, Processor.file, toolname, platform, ResolvePlatform(platform, target));
                return "";
            }
        }

        public List<SourceFile> ResolveSource(string platform, string target, string srcName)
        {
            if (!Targets.ContainsKey(target))
            {
                throw new Exception("An internal error occured! A target was passed that doesn't exist!");
            }
            string plat = ResolvePlatform(platform, target);
            if (Targets[target].Configs.ContainsKey(plat))
            {
                if (Targets[target].Configs[plat].Sources.ContainsKey(srcName))
                {
                    return Targets[target].Configs[plat].Sources[srcName];
                }
            }
            if (Targets[target].TargetSources.ContainsKey(srcName))
            {
                return Targets[target].TargetSources[srcName];
            }
            else if (Configs[plat].Sources.ContainsKey(srcName))
            {
                return Configs[plat].Sources[srcName];
            }
            else
            {
                return GlobalSources[srcName];
            }
        }

        public string ResolvePlatform(string platform, string target)
        {
            if (!Targets.ContainsKey(target))
            {
                throw new Exception("An internal error occured! A target was passed that doesn't exist!");
            }
            // First we attempt to resolve to a target specific version,
            // then we try the global stuff.
            if (Targets[target].Platforms.Contains(platform))
            {
                return platform;
            }
            else if (Targets[target].PlatformAliases.ContainsKey(platform))
            {
                return Targets[target].PlatformAliases[platform];
            }
            else
            {
                if (Platforms.Contains(platform))
                {
                    return platform;
                }
                else if (PlatformAliases.ContainsKey(platform))
                {
                    return PlatformAliases[platform];
                }
                else
                {
                    throw new Exception("Unable to resolve specified platform!");
                }
            }
        }

        public string ResolvePlatform(string platform)
        {
            if (Platforms.Contains(platform))
            {
                return platform;
            }
            else if (PlatformAliases.ContainsKey(platform))
            {
                return PlatformAliases[platform];
            }
            else
            {
                throw new Exception("Unable to resolve specified platform!");
            }
        }

        public bool IsValidPlatform(string platform)
        {
            if (Platforms.Contains(platform) || PlatformAliases.ContainsKey(platform))
            {
                return true;
            }
            return false;
        }

        public bool IsValidPlatform(string platform, string target)
        {
            if (!Targets.ContainsKey(target))
            {
                throw new Exception("An internal error occured! A target was passed that doesn't exist!");
            }
            if (Targets[target].Platforms.Contains(platform) || Targets[target].PlatformAliases.ContainsKey(platform) || Platforms.Contains(platform) || PlatformAliases.ContainsKey(platform))
            {
                return true;
            }
            return false;
        }


	}
}
