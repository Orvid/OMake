using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OMake
{
    /// <summary>
    /// Does the main processing of an OMake file.
    /// </summary>
    public class Processor
    {
        public static Regex BuiltinConstant_Regex;
        public static Regex CustomConstant_Regex;
        public static Dictionary<string, StringMangler> ValidManglers;

        static Processor()
        {
            BuiltinConstant_Regex = new Regex(@"\$\s*\(\s*[A-Za-z_.]+\s*\)", RegexOptions.Compiled);

            #region Setup Mangler Dictionary
            {
                ValidManglers = new Dictionary<string, StringMangler>();
                ValidManglers.Add("no_extension", new StringMangler(NameMangler.No_Extension));
                ValidManglers.Add("dir_to_filename", new StringMangler(NameMangler.Dir_To_Filename));
            }
            #endregion
        }


        private StreamReader stin;
        internal static OMakeFile file = new OMakeFile();
        public Configuration Config = new Configuration();

        public Processor(StreamReader streamIn, string filename)
        {
            this.stin = streamIn;
            file.Filename = filename;
        }

        public void Process()
        {

            #region Process File
            string buf = "";
            while (!stin.EndOfStream)
            {
                buf = stin.ReadLine();
                file.LineNumber++;
                if (buf != "")
                {
                    if (buf.ToLower().StartsWith("#define"))
                    {
                        buf = ProcessDefine(buf);
                    }
                    else if (buf.Trim().StartsWith("//"))
                    {
                        // It's a comment and we ignore it.
                    }
                    else if (buf.ToLower().StartsWith("common"))
                    {
                        // We MUST decompose the statements here, 
                        // otherwise things will get out of order.
                        buf = ProcessCommon(buf);
                    }
                    else
                    {
                        Config.Statements.Add(buf);
                    }
                }
            }
            #endregion

            #region Cleanup
            {
                // Now we do a bit of cleanup to free up what we
                // can, so that the execution isn't impeded in 
                // any way.
#if DEBUG
                // If it's debug we don't want to be cleaning up anything,
                // as that would probably just cause us issues.
#else
                WildcardEvaluator.Cleanup();
#endif
            }
            #endregion

        }

        #region Process Common

        #region Process Common
        private string ProcessCommon(string buf)
        {
            // We MUST decompose the statements here, 
            // otherwise things will get out of order.
            buf = buf.Substring(6).Trim();
            if (buf.ToLower().StartsWith("tool"))
            {
                buf = buf.Substring(4).Trim();
                if (buf.StartsWith("("))
                {
                    buf = buf.Substring(1).Trim();
                    string baseCommand = buf.Substring(0, buf.LastIndexOf(')')).Trim();
                    buf = buf.Substring(buf.LastIndexOf(')') + 1).Trim();
                    if (buf.StartsWith(":"))
                    {
                        buf = buf.Substring(1).Trim();
                        // All that should be left at this point is 
                        // the list we'll be iterating through.
                        if (Config.GlobalSources.ContainsKey(buf))
                        {
                            string SourceListName = buf;
                            // We have a valid list, now we need to
                            // process the options for the list.

                            #region Read the expressions
                            buf = stin.ReadLine().Trim();
                            file.LineNumber++;
                            if (buf != "{")
                            {
                                ErrorManager.Error(21, file, buf);
                            }
                            List<string> expressions = new List<string>();
                            bool inCommonBlock = true;
                            while (inCommonBlock && !stin.EndOfStream)
                            {
                                buf = stin.ReadLine();
                                file.LineNumber++;
                                if (buf.Trim() == "}")
                                {
                                    inCommonBlock = false;
                                    break;
                                }
                                else
                                {
                                    expressions.Add(buf.Trim());
                                }
                            }
                            if (inCommonBlock)
                            {
                                ErrorManager.Error(22, file, null);
                            }
                            #endregion

                            List<string> prefixes = null;
                            List<string> filenames = null;
                            List<string> suffixes = null;

                            #region Process Expressions into specifics
                            // The order doesn't really matter
                            // here.
                            foreach (string s in expressions)
                            {
                                if (s.Trim().ToLower().StartsWith("#prefix"))
                                {
                                    if (prefixes == null)
                                        prefixes = new List<string>();
                                    prefixes.Add(s.Trim().Substring(7).Trim());
                                }
                                else if (s.Trim().ToLower().StartsWith("#filename"))
                                {
                                    if (filenames == null)
                                        filenames = new List<string>();
                                    filenames.Add(s.Trim().Substring(9).Trim());
                                }
                                else if (s.Trim().ToLower().StartsWith("#suffix"))
                                {
                                    if (suffixes == null)
                                        suffixes = new List<string>();
                                    suffixes.Add(s.Trim().Substring(7).Trim());
                                }
                                else
                                {
                                    ErrorManager.Error(23, file, s.Trim());
                                }
                            }
                            #endregion

                            List<string> Sources = new List<string>(Config.GlobalSources[SourceListName]);

                            if (prefixes != null)
                            {
                                ProcessCommon_Prefix(ref buf, prefixes, ref Sources);
                            }
                            if (filenames != null)
                            {
                                ProcessCommon_Filename(ref buf, filenames, ref Sources);
                            }
                            if (suffixes != null)
                            {
                                ProcessCommon_Suffix(ref buf, suffixes, ref Sources);
                            }

                            // Now we can emit the expanded version.
                            foreach (string s in Sources)
                            {
                                Config.Statements.Add(baseCommand + s);
                            }

                            // And now a bit of cleanup
                            expressions.Clear();
                            expressions = null;
                            Sources.Clear();
                            Sources = null;
                            GC.Collect();
                        }
                        else
                        {
                            ErrorManager.Error(24, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(25, file, buf.Substring(0, 1));
                    }
                }
                else
                {
                    ErrorManager.Error(26, file, buf.Substring(0, 1));
                }
            }
            else
            {
                ErrorManager.Error(27, file, buf);
            }
            return buf;
        }
        #endregion

        #region Process Common - Prefix
        private void ProcessCommon_Prefix(ref string buf, List<string> prefixes, ref List<string> Sources)
        {
            foreach (string prefix in prefixes)
            {
                if (BuiltinConstant_Regex.IsMatch(prefix))
                {
                    #region We need to replace some constants
                    if (prefix.StartsWith("{"))
                    {
                        string prefixCommand = prefix.Substring(1, prefix.LastIndexOf(':') - 1).Trim();
                        prefixCommand = prefixCommand.Substring(0, prefixCommand.LastIndexOf('}'));
                        buf = prefix.Substring(prefix.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s) + " " + s);
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s) + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s) + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s) + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(29, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(28, file, prefix.Substring(0, 1));
                    }
                    #endregion
                }
                else
                {
                    #region Don't need to replace any constants
                    if (prefix.StartsWith("{"))
                    {
                        string prefixCommand = prefix.Substring(1, prefix.LastIndexOf(':') - 1).Trim();
                        prefixCommand = prefixCommand.Substring(0, prefixCommand.LastIndexOf('}'));
                        buf = prefix.Substring(prefix.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(prefixCommand + " " + s);
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(prefixCommand + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(prefixCommand + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(prefixCommand + " " + s);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(29, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(28, file, prefix.Substring(0, 1));
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region Process Common - Filename
        private void ProcessCommon_Filename(ref string buf, List<string> filenames, ref List<string> Sources)
        {
            foreach (string filename in filenames)
            {
                if (BuiltinConstant_Regex.IsMatch(filename))
                {
                    #region We need to replace some constants
                    if (filename.StartsWith("{"))
                    {
                        string filenameCommand = filename.Substring(1, filename.LastIndexOf(':') - 1).Trim();
                        filenameCommand = filenameCommand.Substring(0, filenameCommand.LastIndexOf('}'));
                        buf = filename.Substring(filename.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(" " + ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s));
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(" " + ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(" " + ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(" " + ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(37, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(36, file, filename.Substring(0, 1));
                    }
                    #endregion
                }
                else
                {
                    #region Don't need to replace any constants
                    if (filename.StartsWith("{"))
                    {
                        string filenameCommand = filename.Substring(1, filename.LastIndexOf(':') - 1).Trim();
                        filenameCommand = filenameCommand.Substring(0, filenameCommand.LastIndexOf('}'));
                        buf = filename.Substring(filename.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(" " + filenameCommand);
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(" " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(" " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(" " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(37, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(36, file, filename.Substring(0, 1));
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region Process Common - Suffix
        private void ProcessCommon_Suffix(ref string buf, List<string> suffixes, ref List<string> Sources)
        {
            foreach (string suffix in suffixes)
            {
                if (BuiltinConstant_Regex.IsMatch(suffix))
                {
                    #region We need to replace some constants
                    if (suffix.StartsWith("{"))
                    {
                        string suffixCommand = suffix.Substring(1, suffix.LastIndexOf(':') - 1).Trim();
                        suffixCommand = suffixCommand.Substring(0, suffixCommand.LastIndexOf('}'));
                        buf = suffix.Substring(suffix.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(s + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s));
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(s + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(s + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(s + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s));
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(38, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(39, file, suffix.Substring(0, 1));
                    }
                    #endregion
                }
                else
                {
                    #region Don't need to replace any constants
                    if (suffix.StartsWith("{"))
                    {
                        string filenameCommand = suffix.Substring(1, suffix.LastIndexOf(':') - 1).Trim();
                        filenameCommand = filenameCommand.Substring(0, filenameCommand.LastIndexOf('}'));
                        buf = suffix.Substring(suffix.LastIndexOf(':') + 1).Trim();
                        if (buf.ToLower().StartsWith("all"))
                        {
                            #region Process All
                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                newSourceList.Add(s + " " + filenameCommand);
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("filename"))
                        {
                            #region Process Filename
                            string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (s.Trim() == TheFilename)
                                {
                                    newSourceList.Add(s + " " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("wildcard"))
                        {
                            #region Process Wildcard
                            string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.Trim()))
                                {
                                    newSourceList.Add(s + " " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else if (buf.ToLower().StartsWith("regex"))
                        {
                            #region Process Regex
                            string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                            List<string> newSourceList = new List<string>();
                            foreach (string s in Sources)
                            {
                                if (WildcardEvaluator.IsRegexMatch(TheRegex, s.Trim()))
                                {
                                    newSourceList.Add(s + " " + filenameCommand);
                                }
                                else
                                {
                                    newSourceList.Add(s);
                                }
                            }
                            Sources = newSourceList;
                            #endregion
                        }
                        else
                        {
                            ErrorManager.Error(38, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(39, file, suffix.Substring(0, 1));
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region Process Common - Extract Filename
        private string ProcessCommon_ExtractFilename(ref string buf)
        {
            string TheFilename = "";
            buf = buf.Substring(8).Trim();
            if (buf.Substring(0, 1) == "(")
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) == ")")
                {
                    TheFilename = buf.Substring(0, buf.Length - 1).Trim();
                }
                else
                {
                    ErrorManager.Error(30, file, buf.Substring(buf.Length - 1, 1));
                }
            }
            else
            {
                ErrorManager.Error(31, file, buf.Substring(0, 1));
            }
            return TheFilename;
        }
        #endregion

        #region Process Common - Extract Wildcard
        private string ProcessCommon_ExtractWildcard(ref string buf)
        {
            string TheWildcard = "";
            buf = buf.Substring(8).Trim();
            if (buf.Substring(0, 1) == "(")
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) == ")")
                {
                    TheWildcard = buf.Substring(0, buf.Length - 1).Trim();
                }
                else
                {
                    ErrorManager.Error(32, file, buf.Substring(buf.Length - 1, 1));
                }
            }
            else
            {
                ErrorManager.Error(33, file, buf.Substring(0, 1));
            }
            return TheWildcard;
        }
        #endregion

        #region Process Common - Extract Regex
        private string ProcessCommon_ExtractRegex(ref string buf)
        {
            string TheRegex = "";
            buf = buf.Substring(5).Trim();
            if (buf.Substring(0, 1) == "(")
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) == ")")
                {
                    TheRegex = buf.Substring(0, buf.Length - 1).Trim();
                }
                else
                {
                    ErrorManager.Error(34, file, buf.Substring(buf.Length - 1, 1));
                }
            }
            else
            {
                ErrorManager.Error(35, file, buf.Substring(0, 1));
            }
            return TheRegex;
        }
        #endregion

        #region Process Common - Process Builtin Constant
        private string ProcessCommon_ProcessBuiltinConstant(ref string buf, string baseCommand, string s)
        {
            string tmp = baseCommand;
            foreach (Match m in BuiltinConstant_Regex.Matches(baseCommand))
            {
                buf = m.Value.Substring(2, m.Value.Length - 3).Trim();
                if (buf.ToLower().StartsWith("filename"))
                {
                    string curFileName = s;
                    buf = buf.Substring(8).Trim();
                    foreach (string strng in buf.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (ValidManglers.ContainsKey(strng.ToLower()))
                        {
                            curFileName = ValidManglers[strng.ToLower()](curFileName);
                        }
                        else
                        {
                            ErrorManager.Error(40, file, strng);
                        }
                    }
                    tmp = tmp.Replace(m.Value, curFileName);
                }
                else
                {
                    ErrorManager.Error(41, file, buf);
                }
            }
            return tmp;
        }
        #endregion

        #endregion

        #region Process Define

        private string ProcessDefine(string buf)
        {
            buf = buf.Substring(8);
            if (buf.ToLower().StartsWith("platform"))
            {
                buf = ProcessDefinePlatform(buf);
            }
            else if (buf.ToLower().StartsWith("tool"))
            {
                buf = ProcessDefineTool(buf);
            }
            else if (buf.ToLower().StartsWith("const"))
            {
                buf = ProcessDefineConst(buf);
            }
            else if (buf.ToLower().StartsWith("source"))
            {
                buf = ProcessDefineSource(buf);
            }
            else if (buf.ToLower().StartsWith("mangler"))
            {
                buf = ProcessDefineMangler(buf);
            }
            else
            {
                ErrorManager.Error(16, file, buf);
            }
            return buf;
        }

        #region Process Define Mangler
        private string ProcessDefineMangler(string buf)
        {
            buf = buf.Substring(7).Trim();
            if (buf.Substring(0, 1) == "(")
            {
                buf = buf.Substring(1).Trim();
                string lang = buf.Substring(0, buf.IndexOf(',')).Trim();
                buf = buf.Substring(buf.IndexOf(',') + 1).Trim();
                string ManglerName = buf.Substring(0, buf.IndexOf(',')).Trim();
                buf = buf.Substring(buf.IndexOf(',') + 1).Trim();
                string paramName = buf.Substring(0, buf.Length - 1).Trim();
                if (buf.Substring(buf.Length - 1, 1) != ")")
                {
                    ErrorManager.Error(45, file, buf.Substring(buf.Length - 1, 1));
                }
                else
                {
                    if ((buf = stin.ReadLine().Trim())!= "{")
                    {
                        ErrorManager.Error(51, file, buf);
                    }
                    else
                    {
                        string data = NameMangler.ReadManglerData(lang, stin);
                        if (ValidManglers.ContainsKey(ManglerName.ToLower()))
                        {
                            ErrorManager.Warning(52, file, ManglerName.ToLower());
                            ValidManglers[ManglerName.ToLower()] = NameMangler.CompileMangler(paramName, data, lang);
                        }
                        else
                        {
                            ValidManglers.Add(ManglerName.ToLower(), NameMangler.CompileMangler(paramName, data, lang));
                        }
                    }
                }
            }
            else
            {
                ErrorManager.Error(44, file, buf.Substring(0, 1));
            }
            return buf;
        }
        #endregion

        #region Process Define Source
        private string ProcessDefineSource(string buf)
        {
            buf = buf.Substring(6);
            if (!buf.Trim().StartsWith("("))
            {
                ErrorManager.Error(17, file, null);
            }
            else
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) != ")")
                {
                    ErrorManager.Error(18, file, null);
                }
                else
                {
                    buf = buf.Substring(0, buf.Length - 1).Trim();
                    string sourceName = buf;
                    buf = stin.ReadLine().Trim();
                    file.LineNumber++;
                    if (buf != "{")
                    {
                        ErrorManager.Error(19, file, buf);
                    }
                    List<string> sources = new List<string>();
                    bool inSourceBlock = true;
                    while (inSourceBlock && !stin.EndOfStream)
                    {
                        buf = stin.ReadLine();
                        file.LineNumber++;
                        if (buf.Trim() == "}")
                        {
                            inSourceBlock = false;
                            break;
                        }
                        else
                        {
                            if (!buf.Trim().StartsWith("#"))
                            {
                                sources.Add(buf.Trim());
                            }
                        }
                    }
                    if (inSourceBlock)
                    {
                        ErrorManager.Error(20, file, null);
                    }
                    else
                    {
                        Config.GlobalSources.Add(sourceName, sources);
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Define Const
        private string ProcessDefineConst(string buf)
        {
            buf = buf.Substring(5);
            if (buf.StartsWith("_"))
            {
                buf = buf.Substring(1).Trim();
                string plat = buf.Substring(0, buf.IndexOf('('));
                if (!Config.IsValidPlatform(plat))
                {
                    ErrorManager.Error(9, file, plat);
                }
                else
                {
                    buf = buf.Substring(plat.Length, buf.Length - plat.Length).Trim();
                    plat = Config.ResolvePlatform(plat);
                    if (!buf.Trim().StartsWith("("))
                    {
                        ErrorManager.Error(12, file, null);
                    }
                    else
                    {
                        buf = buf.Substring(1).Trim();
                        string cnst = buf.Substring(0, buf.IndexOf(',')).Trim();
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        if (Config.Configs[plat].Constants.ContainsKey(cnst))
                        {
                            ErrorManager.Warning(13, file, cnst);
                            Config.Configs[plat].Constants[cnst] = buf;
                        }
                        else
                        {
                            Config.Configs[plat].Constants.Add(cnst, buf);
                        }
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(14, file, null);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string cnst = buf.Substring(0, buf.IndexOf(',')).Trim();
                    buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                    if (Config.GlobalConstants.ContainsKey(cnst))
                    {
                        ErrorManager.Warning(15, file, cnst);
                        Config.GlobalConstants[cnst] = buf;
                    }
                    else
                    {
                        Config.GlobalConstants.Add(cnst, buf);
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Define Tool
        private string ProcessDefineTool(string buf)
        {
            buf = buf.Substring(4);
            if (buf.StartsWith("_"))
            {
                buf = buf.Substring(1).Trim();
                string plat = buf.Substring(0, buf.IndexOf('('));
                if (!Config.IsValidPlatform(plat))
                {
                    ErrorManager.Error(9, file, plat);
                }
                else
                {
                    buf = buf.Substring(plat.Length, buf.Length - plat.Length).Trim();
                    plat = Config.ResolvePlatform(plat);
                    if (!buf.Trim().StartsWith("("))
                    {
                        ErrorManager.Error(10, file, null);
                    }
                    else
                    {
                        buf = buf.Substring(1).Trim();
                        string tool = buf.Substring(0, buf.IndexOf(',')).Trim();
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        if (Config.Configs[plat].Tools.ContainsKey(tool))
                        {
                            ErrorManager.Warning(7, file, tool);
                            Config.Configs[plat].Tools[tool] = buf;
                        }
                        else
                        {
                            Config.Configs[plat].Tools.Add(tool, buf);
                        }
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(8, file, null);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string tool = buf.Substring(0, buf.IndexOf(',')).Trim();
                    buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                    if (Config.GlobalTools.ContainsKey(tool))
                    {
                        ErrorManager.Warning(11, file, tool);
                        Config.GlobalTools[tool] = buf;
                    }
                    else
                    {
                        Config.GlobalTools.Add(tool, buf);
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Define Platform
        private string ProcessDefinePlatform(string buf)
        {
            buf = buf.Substring(8);
            if (buf.StartsWith("_ALIAS"))
            {
                buf = buf.Substring(6);
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(1, file, null);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string plat = buf.Substring(0, buf.IndexOf(',')).Trim();
                    if (!Config.IsValidPlatform(plat))
                    {
                        ErrorManager.Warning(2, file, plat);
                    }
                    // It may not be a valid platform, but 
                    // we're adding the alias none-the-less.
                    if (Config.PlatformAliases.ContainsKey(buf))
                    {
                        ErrorManager.Warning(6, file, buf);
                    }
                    else
                    {
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        Config.PlatformAliases.Add(buf, plat);
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(3, file, null);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    if (buf.Substring(buf.Length - 2, 2) != ");")
                    {
                        ErrorManager.Error(4, file, null);
                    }
                    else
                    {
                        buf = buf.Substring(0, buf.Length - 2).Trim();
                        if (Config.Platforms.Contains(buf))
                        {
                            ErrorManager.Warning(5, file, buf);
                        }
                        else
                        {
                            Config.Platforms.Add(buf);
                            Config.Configs.Add(buf, new PlatformConfiguration());
                        }
                    }
                }
            }
            return buf;
        }
        #endregion

        #endregion





    }
}
