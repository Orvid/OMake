using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents the different types of 
    /// file statements that are possible.
    /// </summary>
    public enum FileStatementType
    {
        /// <summary>
        /// Create a new file.
        /// </summary>
        Create,
        /// <summary>
        /// Creates a new file,
        /// or if the file already exists,
        /// truncate that file and start anew.
        /// </summary>
        CreateOrTruncate,
        /// <summary>
        /// Append data to an existing file.
        /// </summary>
        Append,
        /// <summary>
        /// Append data to an existing file,
        /// or if it doesn't exist, create
        /// a new file.
        /// </summary>
        CreateOrAppend,
        /// <summary>
        /// Delete a file.
        /// </summary>
        Delete,
        /// <summary>
        /// Delete a file if it exists,
        /// otherwise do nothing.
        /// </summary>
        TryDelete,
        /// <summary>
        /// Copy a file from one location to 
        /// another location.
        /// </summary>
        Copy,
        /// <summary>
        /// Copy a file from one location to
        /// another location even if the 
        /// destination file already exists.
        /// </summary>
        ForceCopy,
    }
}