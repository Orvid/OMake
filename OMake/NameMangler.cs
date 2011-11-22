using System;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace OMake
{
    /// <summary>
    /// The delegate that every string mangler must implement.
    /// </summary>
    /// <param name="inString"></param>
    /// <returns></returns>
    public delegate string StringMangler(string inString);

    /// <summary>
    /// This class contains the methods used by the 
    /// processor to perform the requested mangling
    /// on a filename.
    /// </summary>
    public static class NameMangler
    {

        #region Built-in Manglers
        public static string No_Extension(string instring)
        {
            string ext = System.IO.Path.GetExtension(instring);
            return instring.Replace(ext, "");
        }

        public static string Dir_To_Filename(string instring)
        {
            return instring.Replace('\\', '_').Replace('/', '_').Replace('.', '_');
        }
        #endregion


        /// <summary>
        /// This is simply a dummy mangler to keep the
        /// processor happy when it goes to mangle things.
        /// </summary>
        private static string No_Mangling(string inString)
        {
            return inString;
        }


        private static Dictionary<string, IManglerLanguage> ManglerLanguages = new Dictionary<string, IManglerLanguage>();
        private static Dictionary<string, string> languageNameLookup = new Dictionary<string, string>();
        private static readonly CompilerParameters Params;
        static NameMangler()
        {
            #region Setup Paramaters
            Params = new CompilerParameters(new string[] { "mscorlib.dll", "System.dll", System.Reflection.Assembly.GetExecutingAssembly().Location });
            Params.IncludeDebugInformation = false;
            Params.GenerateInMemory = true;
            Params.GenerateExecutable = false;
            #endregion

            #region Get all the valid IManglerLanguage's
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in a.GetTypes())
                {
                    if (t.GetInterface("OMake.IManglerLanguage") != null)
                    {
                        IManglerLanguage mgl = (IManglerLanguage)Activator.CreateInstance(t);
                        if (ManglerLanguages.ContainsKey(mgl.LanguageName))
                        {
                            ErrorManager.Warning(47, Processor.file, mgl.LanguageName);
                            ManglerLanguages[mgl.LanguageName] = mgl;
                        }
                        else
                        {
                            ManglerLanguages.Add(mgl.LanguageName, mgl);
                        }
                    }
                }
            }
            #endregion

            // Finally setup the alias table.
            foreach (KeyValuePair<string, IManglerLanguage> mgl in ManglerLanguages)
            {
                mgl.Value.SetupAliases(languageNameLookup);
            }
        }

        public static string ReadManglerData(string language, System.IO.StreamReader stin)
        {
            if (ManglerLanguages.ContainsKey(FixLanguageName(language)))
            {
                return ManglerLanguages[FixLanguageName(language)].ReadData(stin);
            }
            else
            {
                ErrorManager.Error(49, Processor.file, FixLanguageName(language));
                return "";
            }
        }

        public static StringMangler CompileMangler(string paramName, string data, string language)
        {
            string languageName = FixLanguageName(language);

            if (!CodeDomProvider.IsDefinedLanguage(languageName))
            {
                ErrorManager.Error(42, Processor.file, languageName);
                return new StringMangler(No_Mangling);
            }
            CodeDomProvider cdp = CodeDomProvider.CreateProvider(languageName);
            CompilerResults rslt = cdp.CompileAssemblyFromSource(Params, ApplyTemplate(languageName, paramName, data));
            if (rslt.NativeCompilerReturnValue != 0)
            {
                StringBuilder strb = new StringBuilder();
                foreach (string s in rslt.Output)
                {
                    strb.AppendLine(s);
                }
                ErrorManager.Error(43, Processor.file, rslt.NativeCompilerReturnValue.ToString(), strb.ToString());
                strb = null;
                // We return this rather than null so that we
                // don't cause exceptions in the processor.
                return new StringMangler(No_Mangling);
            }
            else
            {
                return new StringMangler(((BaseStringMangler)Activator.CreateInstance(rslt.CompiledAssembly.GetType("OMake.CustomStringMangler"))).MangleString);
            }
        }

        private static string ApplyTemplate(string language, string paramName, string data)
        {
            if (ManglerLanguages.ContainsKey(language))
            {
                return ManglerLanguages[language].ApplyTemplate(paramName, data);
            }
            else
            {
                ErrorManager.Error(46, Processor.file, language);
                return "";
            }
        }

        private static string FixLanguageName(string lng)
        {
            if (languageNameLookup.ContainsKey(lng.ToLower()))
            {
                return languageNameLookup[lng.ToLower()];
            }
            else
            {
                ErrorManager.Warning(48, Processor.file, lng.ToLower());
                return lng.ToLower();
            }
        }

    }
}
