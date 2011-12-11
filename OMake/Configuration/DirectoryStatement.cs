using System;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents a single statement,
    /// in specific, a directory statement.
    /// </summary>
    [Serializable]
    public class DirectoryStatement : Statement
    {
        /// <summary>
        /// The type of statement this actually is.
        /// </summary>
        // Also, this has to be new, because it's hiding the type member
        // from Statement.
        new public DirectoryStatementType Type;
        /// <summary>
        /// The name of the directory to perform
        /// the operation on.
        /// </summary>
        public string DirectoryName;
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
        /// Creates a new instance of the <see cref="DirectoryStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="directoryName">The DirectoryName of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        public DirectoryStatement(DirectoryStatementType type, string directoryName, string arg1) : base("")
        {
            this.Type = type;
            this.DirectoryName = directoryName;
            this.Arg1 = arg1;
            base.Type = StatementType.Directory;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="directoryName">The DirectoryName of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="arg2">The second argument for this statement.</param>
        public DirectoryStatement(DirectoryStatementType type, string directoryName, string arg1, object arg2) : this(type, directoryName, arg1)
        {
            this.Arg2 = arg2;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="directoryName">The DirectoryName of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="deps">The list of dependancies for this statement.</param>
        public DirectoryStatement(DirectoryStatementType type, string directoryName, string arg1, List<SourceFile> deps) : base("", deps)
        {
            this.Type = type;
            this.DirectoryName = directoryName;
            this.Arg1 = arg1;
            base.Type = StatementType.Directory;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DirectoryStatement"/> class.
        /// </summary>
        /// <param name="type">The type of statement this is.</param>
        /// <param name="directoryName">The DirectoryName of this statement.</param>
        /// <param name="arg1">The first argument for this statement.</param>
        /// <param name="arg2">The second argument for this statement.</param>
        /// <param name="deps">The list of dependancies for this statement.</param>
        public DirectoryStatement(DirectoryStatementType type, string directoryName, string arg1, object arg2, List<SourceFile> deps) : this(type, directoryName, arg1, deps)
        {
            this.Arg2 = arg2;
        }

    }

}