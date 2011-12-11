using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents the different possible
    /// types of statements.
    /// </summary>
    public enum StatementType
    {
        /// <summary>
        /// Just a regular statement.
        /// </summary>
        Standard,
        /// <summary>
        /// A statement that does something
        /// with a file on disk.
        /// </summary>
        File,
        /// <summary>
        /// A statement that does something
        /// with a directory on disk.
        /// </summary>
        Directory,
    }
}