using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// This class manages the outputing of errors.
    /// It also handles the NLS.
    /// </summary>
    public static class ErrorManager
    {
        /// <summary>
        /// The total number of errors encountered.
        /// </summary>
        public static ulong ErrorCount = 0;
        /// <summary>
        /// The total number of warnings encountered.
        /// </summary>
        public static ulong WarningCount = 0;

        /// <summary>
        /// The word 'Error' in the native language.
        /// </summary>
        private static readonly string errString = NLS.GetErrString("~error");
        /// <summary>
        /// The word 'Warning' in the native language.
        /// </summary>
        private static readonly string warnString = NLS.GetWarnString("~warning");

        /// <summary>
        /// Logs the specified error.
        /// </summary>
        /// <param name="errorNumber">The error number.</param>
        /// <param name="fle">The file in which the error occured.</param>
        /// <param name="args">The parameters for the formatting.</param>
        public static void Error(ulong errorNumber, OMakeFile fle, params object[] args)
        {
            string err = NLS.GetErrString(errorNumber.ToString().PadLeft(5, '0'));
            // The check is only here because I'm lazy when I type.
            if (args != null)
                err = string.Format(err, args);
            Console.WriteLine(fle.Filename + ":" + fle.LineNumber.ToString() + " " + errString + " OM" + errorNumber.ToString() + ": " + err);
            Log.WriteLine(fle.Filename + ":" + fle.LineNumber.ToString() + " " + errString + " OM" + errorNumber.ToString() + ": " + err);
            ErrorCount++;
        }

        /// <summary>
        /// Logs the specified warning.
        /// </summary>
        /// <param name="warningNumber">The warning number.</param>
        /// <param name="fle">The file in which the warning occured.</param>
        /// <param name="args">The parameters for the formatting.</param>
        public static void Warning(ulong warningNumber, OMakeFile fle, params object[] args)
        {
            string warn = NLS.GetWarnString(warningNumber.ToString().PadLeft(5, '0'));
            // The check is only here because I'm lazy when I type.
            if (args != null)
                warn = string.Format(warn, args);
            Console.WriteLine(fle.Filename + ":" + fle.LineNumber.ToString() + " " + warnString + " OM" + warningNumber.ToString() + ": " + warn);
            Log.WriteLine(fle.Filename + ":" + fle.LineNumber.ToString() + " " + warnString + " OM" + warningNumber.ToString() + ": " + warn);
            WarningCount++;
        }
    }
}
