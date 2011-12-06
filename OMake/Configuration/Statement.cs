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
        /// The type of statement this actually is.
        /// </summary>
        public StatementType Type = StatementType.Standard;


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
}