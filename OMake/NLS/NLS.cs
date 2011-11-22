using System;
using System.IO;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// A simple Native Language Service.
    /// </summary>
    public static class NLS
    {
        private static ResourceManager errorsResources = new ResourceManager("OMake.NLS.Errors", Assembly.GetExecutingAssembly());
        private static ResourceManager warningsResources = new ResourceManager("OMake.NLS.Warnings", Assembly.GetExecutingAssembly());

        public static string GetErrString(string s)
        {
            return errorsResources.GetString(s);
        }

        public static string GetWarnString(string s)
        {
            return warningsResources.GetString(s);
        }
    }
}
