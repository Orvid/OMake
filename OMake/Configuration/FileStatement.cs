using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents a single statement,
    /// in specific, a file statement.
    /// </summary>
    [Serializable]
    public class FileStatement : Statement
    {
        /// <summary>
        /// The type of statement this actually is.
        /// </summary>
        // Also, this has to be new, because it's hiding the type member
        // from Statement.
        new public FileStatementType Type;
        /// <summary>
        /// The name of the file to perform
        /// the operation on.
        /// </summary>
        public string Filename;
        /// <summary>
        /// The main argument for the 
        /// operation. For most operations
        /// this contains the actual data.
        /// </summary>
        public string Arg1;
        /// <summary>
        /// The secondary argument for the
        /// operation. This is an object so
        /// that complex arguments can be passed
        /// through it.
        /// </summary>
        public object Arg2;

        /// <summary>
        /// Creates a new instance of the <see cref="FileStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="fileName">The filename of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        public FileStatement(FileStatementType type, string fileName, string arg1) : base("")
        {
            this.Type = type;
            this.Filename = fileName;
            this.Arg1 = arg1;
            base.Type = StatementType.File;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="fileName">The filename of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="arg2">The second argument for this statement.</param>
        public FileStatement(FileStatementType type, string fileName, string arg1, object arg2) : this(type, fileName, arg1)
        {
            this.Arg2 = arg2;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="fileName">The filename of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="deps">The list of dependancies for this statement.</param>
        public FileStatement(FileStatementType type, string fileName, string arg1, List<IDependancy> deps) : base("", deps)
        {
            this.Type = type;
            this.Filename = fileName;
            this.Arg1 = arg1;
            base.Type = StatementType.File;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FileStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="fileName">The filename of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="arg2">The second argument for this statement.</param>
        /// <param name="deps">The list of dependancies for this statement.</param>
        public FileStatement(FileStatementType type, string fileName, string arg1, object arg2, List<IDependancy> deps) : this(type, fileName, arg1, deps)
        {
            this.Arg2 = arg2;
        }

    }

}