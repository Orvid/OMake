using System;
using System.Collections.Generic;

namespace OMake
{
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
}