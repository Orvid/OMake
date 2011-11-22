using System;

namespace OMake
{
    /// <summary>
    /// The base class for all string manglers.
    /// </summary>
    public abstract class BaseStringMangler
    {
        public abstract string MangleString(string inString);
    }
}
