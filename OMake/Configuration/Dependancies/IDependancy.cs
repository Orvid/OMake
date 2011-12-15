using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents a single dependancy.
    /// </summary>
    public interface IDependancy
    {
        /// <summary>
        /// Sets the cache for this dependancy.
        /// </summary>
        void SetCache();

        /// <summary>
        /// True if the dependancy has been
        /// modified since it was last cached.
        /// </summary>
        bool Modified { get; }

    }
}