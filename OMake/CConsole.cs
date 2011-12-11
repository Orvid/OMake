using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// This is used to be able to output to both
    /// the console, and to the log.
    /// </summary>
    public static class CConsole
    {

        /// <summary>
        /// Writes a string only to the log.
        /// </summary>
        public static void LWriteLine(string s)
        {
            Log.WriteLine(s);
        }

        /// <summary>
        /// Writes a string to both the 
        /// console and the log.
        /// </summary>
        public static void WriteLine(string s)
        {
            Console.WriteLine(s);
            Log.WriteLine(s);
        }

    }
}
