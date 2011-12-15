using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents a single directory.
    /// </summary>
    [Serializable]
    public class DirectoryDependancy
    {
        /// <summary>
        /// The location of the file.
        /// </summary>
        public string Directory;
        /// <summary>
        /// The target this DirectoryDependancy is for.
        /// </summary>
        public string Target;
        /// <summary>
        /// The platform this DirectoryDependancy is for.
        /// </summary>
        public string Platform;
        /// <summary>
        /// The list of <see cref="IDependancy"/>s that this
        /// <see cref="DirectoryDependancy"/> depends on.
        /// </summary>
        public List<IDependancy> Dependancies;

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryDependancy"/> class.
        /// </summary>
        /// <param name="dirName">The location of the directory.</param>
        /// <param name="targetName">The target this directory is for.</param>
        public DirectoryDependancy(string dirName, string targetName)
        {
            this.Directory = dirName;
            this.Target = targetName;
            this.Platform = "GLOBAL";
            this.Dependancies = new List<IDependancy>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryDependancy"/> class.
        /// </summary>
        /// <param name="dirName">The location of the directory.</param>
        /// <param name="targetName">The target this directory is for.</param>
        /// <param name="platformName">The platform this directory is for.</param>
        public DirectoryDependancy(string dirName, string targetName, string platformName)
        {
            this.Directory = dirName;
            this.Target = targetName;
            this.Platform = platformName;
            this.Dependancies = new List<IDependancy>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryDependancy"/> class.
        /// </summary>
        /// <param name="dirName">The location of the directory.</param>
        /// <param name="targetName">The target this directory is for.</param>
        /// <param name="dependancyList">The list of dependancies for this directory.</param>
        public DirectoryDependancy(string dirName, string targetName, List<IDependancy> dependancyList)
        {
            this.Directory = dirName;
            this.Target = targetName;
            this.Platform = "GLOBAL";
            this.Dependancies = new List<IDependancy>(dependancyList);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryDependancy"/> class.
        /// </summary>
        /// <param name="dirName">The location of the directory.</param>
        /// <param name="targetName">The target this directory is for.</param>
        /// <param name="platformName">The platform this directory is for.</param>
        /// <param name="dependancyList">The list of dependancies for this directory.</param>
        public DirectoryDependancy(string dirName, string targetName, string platformName, List<IDependancy> dependancyList)
        {
            this.Directory = dirName;
            this.Target = targetName;
            this.Platform = platformName;
            this.Dependancies = new List<IDependancy>(dependancyList);
        }

        /// <summary>
        /// Sets the cache for this dependancy.
        /// </summary>
        public void SetCache()
        {
            Cache.SetValue("OMake.PartialBuild.ModificationChecker.DirectoryDependancy-" + Directory + "-" + Target + "-" + Platform + ".ModificationTime", System.IO.File.GetLastWriteTimeUtc(Directory).ToBinary().ToString());
        }

        /// <summary>
        /// True if the directory or it's dependancies 
        /// have been modified since it was last cached.
        /// </summary>
        public bool Modified
        {
            get
            {
                if (System.IO.Directory.Exists(Directory))
                {
                    if (Cache.Contains("OMake.PartialBuild.ModificationChecker.DirectoryDependancy-" + Directory + "-" + Target + "-" + Platform + ".ModificationTime"))
                    {
                        string CachedModTime = Cache.GetString("OMake.PartialBuild.ModificationChecker.DirectoryDependancy-" + Directory + "-" + Target + "-" + Platform + ".ModificationTime");
                        if (CachedModTime == System.IO.Directory.GetLastWriteTimeUtc(Directory).ToBinary().ToString())
                        {
                            foreach (IDependancy s in Dependancies)
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
}