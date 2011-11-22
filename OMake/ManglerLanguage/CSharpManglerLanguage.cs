using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// Represents the C# laguage.
    /// </summary>
    public class CSharpManglerLanguage : IManglerLanguage
    {
        /// <summary>
        /// Due to the syntax of C#, we can't just use
        /// String.Format.
        /// </summary>
        private const string Template_1 = "namespace OMake\r\n{\r\npublic class CustomStringMangler : OMake.BaseStringMangler\r\n{\r\npublic override string MangleString(string ";
        private const string Template_2 = ")\r\n{\r\n";
        private const string Template_3 = "\r\n}\r\n}\r\n}";


        /// <summary>
        /// Applies the template for this mangler language.
        /// </summary>
        /// <param name="paramName">
        /// The name of the input string paramater which
        /// is used in the custom string mangler.
        /// </param>
        /// <param name="data">The actual data of the method.</param>
        /// <returns>A string containing the fully compilable mangler.</returns>
        public string ApplyTemplate(string paramName, string data)
        {
            return Template_1 + paramName + Template_2 + data + Template_3;
        }

        /// <summary>
        /// The name of the language, with correct capitolization.
        /// </summary>
        public string LanguageName 
        {
            get { return "CSharp"; }
        }

        /// <summary>
        /// Reads the actual data out of a string mangler,
        /// it's expected to stop once it reaches the end of 
        /// the declaration.
        /// </summary>
        /// <param name="stin">The stream to read from.</param>
        /// <returns>The read data.</returns>
        public string ReadData(StreamReader stin)
        {
            StringBuilder stb = new StringBuilder();
            bool inCommentBlock = false;
            // The processor already read the first bracket.
            ulong BracketDepth = 1;
            char curChar;
            bool EOF = false;
            while (true)
            {
                if (stin.EndOfStream)
                {
                    EOF = true;
                    break;
                }
                else if (BracketDepth == 0)
                {
                    break;
                }
                else
                {
                    curChar = (char)stin.Read();
                    if (curChar == '/')
                    {
                        if (((char)stin.Peek()) == '/')
                        {
                            stb.Append(curChar);
                            stb.Append(stin.ReadLine());
                        }
                        else if (((char)stin.Peek()) == '*')
                        {
                            // We are starting a multi-line comment block.
                            // We do this so that any brackets inside
                            // the comment don't mess with our overall
                            // count.
                            stb.Append(curChar);
                            stb.Append((char)stin.Read());
                            inCommentBlock = true;
                            while (inCommentBlock)
                            {
                                curChar = (char)stin.Read();
                                if (curChar == '*')
                                {
                                    if (((char)stin.Peek()) == '/')
                                    {
                                        inCommentBlock = false;
                                        stb.Append(curChar);
                                        stb.Append((char)stin.Read());
                                    }
                                    else
                                    {
                                        stb.Append(curChar);
                                    }
                                }
                                else
                                {
                                    stb.Append(curChar);
                                }
                            }
                        }
                        else
                        {
                            stb.Append(curChar);
                        }
                    }
                    else if (curChar == '{')
                    {
                        BracketDepth++;
                        stb.Append(curChar);
                    }
                    else if (curChar == '}')
                    {
                        BracketDepth--;
                        stb.Append(curChar);
                    }
                    else
                    {
                        stb.Append(curChar);
                    }
                }
            }
            if (EOF)
            {
                ErrorManager.Error(50, Processor.file);
                return "";
            }
            return stb.ToString().Substring(0, stb.ToString().Length - 1).Trim();
        }

        /// <summary>
        /// Sets up the aliases for this language
        /// so that multiple names can be used.
        /// Keys must always be lowercase.
        /// </summary>
        /// <param name="nameLookup">
        /// The dictionary with the name lookups.
        /// </param>
        public void SetupAliases(Dictionary<string, string> nameLookup)
        {
            nameLookup.Add("csharp", "CSharp");
            nameLookup.Add("c#", "CSharp");
        }

    }
}
