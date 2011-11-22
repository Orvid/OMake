using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// Represents a single langauge for
    /// use in a custom string mangler.
    /// </summary>
    public interface IManglerLanguage
    {
        /// <summary>
        /// Applies the template for this mangler language.
        /// </summary>
        /// <param name="paramName">
        /// The name of the input string paramater which
        /// is used in the custom string mangler.
        /// </param>
        /// <param name="data">The actual data of the method.</param>
        /// <returns>A string containing the fully compilable mangler.</returns>
        string ApplyTemplate(string paramName, string data);

        /// <summary>
        /// The name of the language, with correct capitolization.
        /// </summary>
        string LanguageName { get; }

        /// <summary>
        /// Reads the actual data out of a string mangler,
        /// it's expected to stop once it reaches the end of 
        /// the declaration.
        /// </summary>
        /// <param name="stin">The stream to read from.</param>
        /// <returns>The read data.</returns>
        string ReadData(StreamReader stin);

        /// <summary>
        /// Sets up the aliases for this language
        /// so that multiple names can be used.
        /// Keys must always be lowercase.
        /// </summary>
        /// <param name="nameLookup">
        /// The dictionary with the name lookups.
        /// </param>
        void SetupAliases(Dictionary<string, string> nameLookup);
    }
}
