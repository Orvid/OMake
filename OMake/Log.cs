using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// A simple logger.
    /// </summary>
    public static class Log
    {
        private static StreamWriter logFile = null;

        /// <summary>
        /// Initializes the log.
        /// </summary>
        public static void Initialize()
        {
            logFile = new StreamWriter("OMakeLog.log", false, System.Text.ASCIIEncoding.Unicode);
        }

        /// <summary>
        /// Writes the specified string to the log.
        /// </summary>
        /// <param name="s">The string to write.</param>
        public static void WriteLine(string s)
        {
            logFile.WriteLine(s);
        }

        /// <summary>
        /// Finalizes the log and flushes the buffer.
        /// </summary>
        public static void Cleanup()
        {
            logFile.Flush();
            logFile.Close();
            logFile.Dispose();
        }
    }
}
