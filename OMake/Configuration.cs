using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents a single statement.
    /// </summary>
    [Serializable]
    public class Statement
    {
        /// <summary>
        /// The actual value of the statement.
        /// </summary>
        public string StatementValue;
        /// <summary>
        /// A list of dependancies.
        /// </summary>
        public List<SourceFile> Dependancies;

        /// <summary>
        /// Creates a new instance of the <see cref="Statement"/> class.
        /// </summary>
        /// <param name="value">The value of the statement.</param>
        public Statement(string value)
        {
            this.StatementValue = value;
            this.Dependancies = new List<SourceFile>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Statement"/> class.
        /// </summary>
        /// <param name="value">The value of the statement.</param>
        /// <param name="dependancyList">
        /// A list containing the files this statement is dependant upon.
        /// </param>
        public Statement(string value, List<SourceFile> dependancyList)
        {
            this.StatementValue = value;
            this.Dependancies = new List<SourceFile>(dependancyList);
        }

        /// <summary>
        /// Sets the cache for this statement's
        /// dependancies.
        /// </summary>
        public void SetCache()
        {
            foreach (SourceFile s in Dependancies)
            {
                s.SetCache();
            }
        }

        /// <summary>
        /// True if one of the statement's
        /// dependancies has been modified.
        /// </summary>
        public bool Modified
        {
            get
            {
                if (Dependancies.Count == 0)
                    return true;
                foreach (SourceFile s in Dependancies)
                {
                    if (s.Modified)
                        return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Represents a single source file.
    /// </summary>
    [Serializable]
    public class SourceFile
    {
        /// <summary>
        /// The location of the file.
        /// </summary>
        public string File;
        /// <summary>
        /// The target this SourceFile is for.
        /// </summary>
        public string Target;
        /// <summary>
        /// The platform this SourceFile is for.
        /// </summary>
        public string Platform;
        /// <summary>
        /// The list of <see cref="SourceFile"/>s that this
        /// <see cref="SourceFile"/> depends on.
        /// </summary>
        public List<SourceFile> Dependancies;

        /// <summary>
        /// Creates a new instance of the <see cref="SourceFile"/> class.
        /// </summary>
        /// <param name="fileName">The location of the file.</param>
        /// <param name="targetName">The target this file is for.</param>
        public SourceFile(string fileName, string targetName)
        {
            this.File = fileName;
            this.Target = targetName;
            this.Platform = "GLOBAL";
            this.Dependancies = new List<SourceFile>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SourceFile"/> class.
        /// </summary>
        /// <param name="fileName">The location of the file.</param>
        /// <param name="targetName">The target this file is for.</param>
        /// <param name="platformName">The platform this file is for.</param>
        public SourceFile(string fileName, string targetName, string platformName)
        {
            this.File = fileName;
            this.Target = targetName;
            this.Platform = platformName;
            this.Dependancies = new List<SourceFile>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SourceFile"/> class.
        /// </summary>
        /// <param name="fileName">The location of the file.</param>
        /// <param name="targetName">The target this file is for.</param>
        /// <param name="dependancyList">The list of dependancies for this file.</param>
        public SourceFile(string fileName, string targetName, List<SourceFile> dependancyList)
        {
            this.File = fileName;
            this.Target = targetName;
            this.Platform = "GLOBAL";
            this.Dependancies = new List<SourceFile>(dependancyList);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SourceFile"/> class.
        /// </summary>
        /// <param name="fileName">The location of the file.</param>
        /// <param name="targetName">The target this file is for.</param>
        /// <param name="platformName">The platform this file is for.</param>
        /// <param name="dependancyList">The list of dependancies for this file.</param>
        public SourceFile(string fileName, string targetName, string platformName, List<SourceFile> dependancyList)
        {
            this.File = fileName;
            this.Target = targetName;
            this.Platform = platformName;
            this.Dependancies = new List<SourceFile>(dependancyList);
        }

        /// <summary>
        /// Sets the cache for this dependancy.
        /// </summary>
        public void SetCache()
        {
            Cache.SetValue("OMake.PartialBuild.ModificationChecker.SourceFile-" + File + "-" + Target + "-" + Platform + ".ModificationTime", System.IO.File.GetLastWriteTimeUtc(File).ToBinary().ToString());
        }

        /// <summary>
        /// True if the file has been
        /// modified since it was last cached.
        /// </summary>
        public bool Modified
        {
            get
            {
                if (System.IO.File.Exists(File))
                {
                    if (Cache.Contains("OMake.PartialBuild.ModificationChecker.SourceFile-" + File + "-" + Target + "-" + Platform + ".ModificationTime"))
                    {
                        string CachedModTime = Cache.GetString("OMake.PartialBuild.ModificationChecker.SourceFile-" + File + "-" + Target + "-" + Platform + ".ModificationTime");
                        if (CachedModTime == System.IO.File.GetLastWriteTimeUtc(File).ToBinary().ToString())
                        {
                            foreach (SourceFile s in Dependancies)
                            {
                                if (s.Modified)
                                {
                                    return true;
                                }
                            }
                            // If it made it this far, then
                            // it and it's dependancies haven't
                            // been modified.
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }
    }

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
        public Dictionary<string, List<SourceFile>> TargetSources = new Dictionary<string, List<SourceFile>>();
        /// <summary>
        /// The constants specific to the target.
        /// </summary>
        public Dictionary<string, string> TargetConstants = new Dictionary<string, string>();
        /// <summary>
        /// The tools specific to the target.
        /// </summary>
        public Dictionary<string, string> TargetTools = new Dictionary<string, string>();
    }

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
