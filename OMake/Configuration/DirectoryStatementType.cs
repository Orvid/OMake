using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents the different types of 
    /// directory statements that are possible.
    /// </summary>
    public enum DirectoryStatementType
    {
        /// <summary>
        /// Create a new directory.
        /// </summary>
        Create,
        /// <summary>
        /// Try to create a new directory.
        /// If it already exists, remove it's
        /// contents.
        /// </summary>
        ForceCreate,
        /// <summary>
        /// Try to create a new directory,
        /// and fail silently if it already
        /// exists.
        /// </summary>
        TryCreate,
        /// <summary>
        /// Delete a folder.
        /// </summary>
        Delete,
        /// <summary>
        /// Delete a folder if it exists,
        /// otherwise do nothing.
        /// </summary>
        TryDelete,
        /// <summary>
        /// Copy a folder from one location to 
        /// another location.
        /// </summary>
        Copy,
        /// <summary>
        /// Copy a folder from one location to
        /// another location even if the 
        /// destination folder already exists.
        /// </summary>
        ForceCopy,
        /// <summary>
        /// Attempt to copy a folder from one 
        /// location to another, and fail 
        /// silently if the destination directory
        /// already exists.
        /// </summary>
        TryCopy,
        /// <summary>
        /// Move a directory from one location
        /// to another.
        /// </summary>
        Move,
        /// <summary>
        /// Move a directory from one location 
        /// to another, and overwrite 
        /// destination directory if it already
        /// exists.
        /// </summary>
        ForceMove,
        /// <summary>
        /// Attempt to move a directory from one
        /// location to another, and fail 
        /// silently if the destination directory
        /// already exists.
        /// </summary>
        TryMove,
        /// <summary>
        /// Rename an existing directory.
        /// </summary>
        Rename,
        /// <summary>
        /// Rename an existing directory, even 
        /// if a directory with the destination
        /// name already exists.
        /// </summary>
        ForceRename,
        /// <summary>
        /// Attempt to rename an existing directory,
        /// and fail silently if a directory with the
        /// destination name already exists.
        /// </summary>
        TryRename,
    }
}