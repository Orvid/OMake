using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// Represents an OMake Makefile.
    /// </summary>
    public struct OMakeFile
    {
        public string Filename;
        public ulong LineNumber;
    }
}
