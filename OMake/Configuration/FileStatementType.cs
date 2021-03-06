﻿using System;
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
        /// <summary>
        /// Attempt to copy a file from one 
        /// location to another, and fail 
        /// silently if the destination file
        /// already exists.
        /// </summary>
        TryCopy,
        /// <summary>
        /// Move a file from one location
        /// to another.
        /// </summary>
        Move,
        /// <summary>
        /// Move a file from one location 
        /// to another, and overwrite 
        /// destination file if it already
        /// exists.
        /// </summary>
        ForceMove,
        /// <summary>
        /// Attempt to move a file from one
        /// location to another, and fail 
        /// silently if the destination file
        /// already exists.
        /// </summary>
        TryMove,
        /// <summary>
        /// Rename an existing file.
        /// </summary>
        Rename,
        /// <summary>
        /// Rename an existing file, even 
        /// if a file with the destination
        /// name already exists.
        /// </summary>
        ForceRename,
        /// <summary>
        /// Attempt to rename an existing file,
        /// and fail silently if a file with the
        /// destination name already exists.
        /// </summary>
        TryRename,
    }
}