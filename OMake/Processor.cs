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
                if (buf.Trim() != "")
                {
                    if (buf.ToLower().Trim().StartsWith("#define"))
                    {
                        buf = ProcessDefine(buf);
                    }
                    else if (buf.Trim().StartsWith("//"))
                    {
                        // It's a comment and we ignore it.
                    }
                    else if (buf.ToLower().Trim().StartsWith("file"))
                    {
                        buf = ProcessFile(buf, "all");
                    }
                    else if (buf.ToLower().Trim().StartsWith("directory"))
                    {
                        buf = ProcessDirectory(buf, "all");
                    }
                    else if (buf.ToLower().Trim().StartsWith("common"))
                    {
                        // We MUST decompose the statements here, 
                        // otherwise things will get out of order.
                        buf = ProcessCommon(buf);
                    }
                    else
                    {
                        Config.Targets["all"].Statements.Add(new Statement(buf));
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

        #region Process File
        private string ProcessFile(string bfr, string target)
        {
            string buf = bfr.Trim().Substring(4).Trim();
            string filename = "";
            FileStatementType tp;
            string arg1 = "";

            #region Process Type
            if (buf.ToLower().StartsWith("create"))
            {
                buf = buf.Substring(6).Trim();
                if (buf.ToLower().StartsWith("_or_truncate"))
                {
                    tp = FileStatementType.CreateOrTruncate;
                    buf = buf.Substring(12).Trim();
                }
                else if (buf.ToLower().StartsWith("_or_append"))
                {
                    tp = FileStatementType.CreateOrAppend;
                    buf = buf.Substring(10).Trim();
                }
                else if (buf.ToLower().StartsWith("("))
                {
                    tp = FileStatementType.Create;
                }
                else
                {
                    ErrorManager.Error(100, file, buf);
                    return "";
                }
            }
            else if (buf.ToLower().StartsWith("append"))
            {
                tp = FileStatementType.Append;
                buf = buf.Substring(6).Trim();
            }
            else if (buf.ToLower().StartsWith("delete"))
            {
                tp = FileStatementType.Delete;
                buf = buf.Substring(6).Trim();
            }
            else if (buf.ToLower().StartsWith("copy"))
            {
                tp = FileStatementType.Copy;
                buf = buf.Substring(4).Trim();
            }
            else if (buf.ToLower().StartsWith("try_"))
            {
                buf = buf.Substring(4).Trim();
                if (buf.ToLower().StartsWith("delete"))
                {
                    tp = FileStatementType.TryDelete;
                    buf = buf.Substring(6).Trim();
                }
                else if (buf.ToLower().StartsWith("copy"))
                {
                    tp = FileStatementType.TryCopy;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("move"))
                {
                    tp = FileStatementType.TryMove;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("rename"))
                {
                    tp = FileStatementType.TryRename;
                    buf = buf.Substring(6).Trim();
                }
                else
                {
                    ErrorManager.Error(101, file, buf);
                    return "";
                }

            }
            else if (buf.ToLower().StartsWith("force_"))
            {
                buf = buf.Substring(6).Trim();
                if (buf.ToLower().StartsWith("copy"))
                {
                    tp = FileStatementType.ForceCopy;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("move"))
                {
                    tp = FileStatementType.ForceMove;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("rename"))
                {
                    tp = FileStatementType.ForceRename;
                    buf = buf.Substring(6).Trim();
                }
                else
                {
                    ErrorManager.Error(102, file, buf);
                    return "";
                }
            }
            else
            {
                ErrorManager.Error(97, file, buf);
                return "";
            }
            #endregion


            if (!buf.StartsWith("("))
            {
                ErrorManager.Error(96, file, "(");
                return "";
            }
            else
            {
                buf = buf.Substring(1).Trim();
                #region Check Semantics
                switch (tp)
                {
                    case FileStatementType.Append:
                    case FileStatementType.Create:
                    case FileStatementType.CreateOrAppend:
                    case FileStatementType.CreateOrTruncate:
                        if (!buf.EndsWith(")"))
                        {
                            ErrorManager.Error(96, file, ")");
                            return "";
                        }
                        buf = buf.Substring(0, buf.Length - 1).Trim();
                        break;

                    case FileStatementType.Copy:
                    case FileStatementType.Delete:
                    case FileStatementType.Move:
                    case FileStatementType.Rename:
                    case FileStatementType.ForceCopy:
                    case FileStatementType.ForceMove:
                    case FileStatementType.ForceRename:
                    case FileStatementType.TryCopy:
                    case FileStatementType.TryDelete:
                    case FileStatementType.TryMove:
                    case FileStatementType.TryRename:
                        if (!buf.EndsWith(");"))
                        {
                            ErrorManager.Error(96, file, ");");
                            return "";
                        }
                        buf = buf.Substring(0, buf.Length - 2).Trim();
                        break;

                    default:
                        throw new Exception("Unknown FileStatementType!");
                }
                #endregion

                // Extract the args
                switch (tp)
                {
                    // These have an argument, not
                    // just a filename.
                    case FileStatementType.Move:
                    case FileStatementType.Copy:
                    case FileStatementType.Rename:
                    case FileStatementType.ForceCopy:
                    case FileStatementType.ForceMove:
                    case FileStatementType.ForceRename:
                    case FileStatementType.TryCopy:
                    case FileStatementType.TryMove:
                    case FileStatementType.TryRename:
                        filename = buf.Split(',')[0].Trim();
                        arg1 = buf.Split(',')[1].Trim();
                        break;
                    default:
                        filename = buf;
                        break;
                }
                // Now read the data.
                switch (tp)
                {
                    case FileStatementType.Append:
                    case FileStatementType.Create:
                    case FileStatementType.CreateOrAppend:
                    case FileStatementType.CreateOrTruncate:
                        buf = stin.ReadLine();
                        file.LineNumber++;
                        if (buf.Trim() != "{")
                        {
                            ErrorManager.Error(98, file, "{", buf);
                            return "";
                        }
                        else
                        {
                            bool inFileBlock = true;
                            while (inFileBlock && !stin.EndOfStream)
                            {
                                buf = stin.ReadLine();
                                file.LineNumber++;
                                if (buf.Trim() == "}")
                                {
                                    inFileBlock = false;
                                    break;
                                }
                                else
                                {
                                    arg1 += buf.Trim() + "\r\n";
                                }
                            }
                            if (inFileBlock)
                            {
                                ErrorManager.Error(99, file);
                                return "";
                            }
                        }
                        break;
                }
                Config.Targets[target].Statements.Add(new FileStatement(tp, filename, arg1));

            }
            return buf;
        }
        #endregion


        #region Process Directory
        private string ProcessDirectory(string bfr, string target)
        {
            string buf = bfr.Trim().Substring(9).Trim();
            string DirectoryName = "";
            DirectoryStatementType tp;
            string arg1 = "";

            #region Process Type
            if (buf.ToLower().StartsWith("create"))
            {
                buf = buf.Substring(6).Trim();
                tp = DirectoryStatementType.Create;
            }
            else if (buf.ToLower().StartsWith("delete"))
            {
                tp = DirectoryStatementType.Delete;
                buf = buf.Substring(6).Trim();
            }
            else if (buf.ToLower().StartsWith("copy"))
            {
                tp = DirectoryStatementType.Copy;
                buf = buf.Substring(4).Trim();
            }
            else if (buf.ToLower().StartsWith("try_"))
            {
                buf = buf.Substring(4).Trim();
                if (buf.ToLower().StartsWith("create"))
                {
                    tp = DirectoryStatementType.TryCreate;
                    buf = buf.Substring(6).Trim();
                }
                else if (buf.ToLower().StartsWith("delete"))
                {
                    tp = DirectoryStatementType.TryDelete;
                    buf = buf.Substring(6).Trim();
                }
                else if (buf.ToLower().StartsWith("copy"))
                {
                    tp = DirectoryStatementType.TryCopy;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("move"))
                {
                    tp = DirectoryStatementType.TryMove;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("rename"))
                {
                    tp = DirectoryStatementType.TryRename;
                    buf = buf.Substring(6).Trim();
                }
                else
                {
                    ErrorManager.Error(101, file, buf);
                    return "";
                }

            }
            else if (buf.ToLower().StartsWith("force_"))
            {
                buf = buf.Substring(6).Trim();
                if (buf.ToLower().StartsWith("create"))
                {
                    tp = DirectoryStatementType.ForceCreate;
                    buf = buf.Substring(6).Trim();
                }
                else if (buf.ToLower().StartsWith("copy"))
                {
                    tp = DirectoryStatementType.ForceCopy;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("move"))
                {
                    tp = DirectoryStatementType.ForceMove;
                    buf = buf.Substring(4).Trim();
                }
                else if (buf.ToLower().StartsWith("rename"))
                {
                    tp = DirectoryStatementType.ForceRename;
                    buf = buf.Substring(6).Trim();
                }
                else
                {
                    ErrorManager.Error(102, file, buf);
                    return "";
                }
            }
            else
            {
                ErrorManager.Error(106, file, buf);
                return "";
            }
            #endregion


            if (!buf.StartsWith("("))
            {
                ErrorManager.Error(107, file, "(");
                return "";
            }
            else
            {
                buf = buf.Substring(1).Trim();
                #region Check Semantics
                switch (tp)
                {
                    case DirectoryStatementType.Create:
                    case DirectoryStatementType.Copy:
                    case DirectoryStatementType.Delete:
                    case DirectoryStatementType.Move:
                    case DirectoryStatementType.Rename:
                    case DirectoryStatementType.TryCopy:
                    case DirectoryStatementType.TryCreate:
                    case DirectoryStatementType.TryDelete:
                    case DirectoryStatementType.TryMove:
                    case DirectoryStatementType.TryRename:
                    case DirectoryStatementType.ForceCopy:
                    case DirectoryStatementType.ForceCreate:
                    case DirectoryStatementType.ForceMove:
                    case DirectoryStatementType.ForceRename:
                        if (!buf.EndsWith(");"))
                        {
                            ErrorManager.Error(107, file, ");");
                            return "";
                        }
                        buf = buf.Substring(0, buf.Length - 2).Trim();
                        break;

                    default:
                        throw new Exception("Unknown FileStatementType!");
                }
                #endregion

                // Extract the args
                switch (tp)
                {
                    // These have an argument, not
                    // just a filename.
                    case DirectoryStatementType.Copy:
                    case DirectoryStatementType.Move:
                    case DirectoryStatementType.Rename:
                    case DirectoryStatementType.TryCopy:
                    case DirectoryStatementType.TryMove:
                    case DirectoryStatementType.TryRename:
                    case DirectoryStatementType.ForceCopy:
                    case DirectoryStatementType.ForceMove:
                    case DirectoryStatementType.ForceRename:
                        DirectoryName = buf.Split(',')[0].Trim();
                        arg1 = buf.Split(',')[1].Trim();
                        break;
                    default:
                        DirectoryName = buf;
                        break;
                }
                Config.Targets[target].Statements.Add(new DirectoryStatement(tp, DirectoryName, arg1));
            }
            return buf;
        }
        #endregion

        #region Process Define Source - Source Lines
        private List<FileDependancy> Process_Define_Source_SourceLines(List<string> lines, string target)
        {
            List<FileDependancy> FinalSources = new List<FileDependancy>();
            foreach (string s in lines)
            {
                if (s.Contains(":"))
                {
                    string flname = s.Split(':')[0].Trim();
                    string buf = s.Substring(s.IndexOf(':') + 1).Trim();
                    List<string> dependancies = new List<string>(buf.Split(','));
                    List<IDependancy> deps = new List<IDependancy>();
                    foreach (string dp in dependancies)
                    {
                        deps.Add(new FileDependancy(dp.Trim(), target));
                    }
                    FinalSources.Add(new FileDependancy(flname, "all", deps));
                }
                else
                {
                    FinalSources.Add(new FileDependancy(s.Trim(), "all"));
                }
            }
            return FinalSources;
        }
        #endregion


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
                    string baseCommand = buf.Substring(0, buf.LastIndexOf(')'));
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

                            List<FileDependancy> Srces = new List<FileDependancy>(Config.ResolveSource("WIN32", "all", SourceListName));
                            List<FileDependancy> Sources = new List<FileDependancy>();

                            foreach (FileDependancy s in Srces)
                            {
                                Sources.Add(new FileDependancy(s.File, s.Target, s.Platform, s.Dependancies));
                            }

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

                            int indxs = 0;
                            // Now we can emit the expanded version.
                            foreach (FileDependancy s in Sources)
                            {
                                List<IDependancy> sr = new List<IDependancy>();
                                sr.Add(Srces[indxs]);
                                Config.Targets["all"].Statements.Add(new Statement(baseCommand + s.File, sr));
                                indxs++;
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
        private void ProcessCommon_Prefix(ref string buf, List<string> prefixes, ref List<FileDependancy> Sources)
        {
            foreach (string prefix in prefixes)
            {
                if (prefix.StartsWith("{"))
                {
                    string prefixCommand = prefix.Substring(1, prefix.LastIndexOf(':') - 1).Trim();
                    prefixCommand = prefixCommand.Substring(0, prefixCommand.LastIndexOf('}'));
                    buf = prefix.Substring(prefix.LastIndexOf(':') + 1).Trim();
                    if (buf.ToLower().StartsWith("all"))
                    {
                        #region Process All
                        foreach (FileDependancy s in Sources)
                        {
                            s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s.File) + " " + s.File;
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("filename"))
                    {
                        #region Process Filename
                        string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (s.File.Trim() == TheFilename)
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s.File) + " " + s.File;
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("wildcard"))
                    {
                        #region Process Wildcard
                        string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.File.Trim()))
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s.File) + " " + s.File;
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("regex"))
                    {
                        #region Process Regex
                        string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsRegexMatch(TheRegex, s.File.Trim()))
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, prefixCommand, s.File) + " " + s.File;
                            }
                        }
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
            }
        }
        #endregion

        #region Process Common - Filename
        private void ProcessCommon_Filename(ref string buf, List<string> filenames, ref List<FileDependancy> Sources)
        {
            foreach (string filename in filenames)
            {
                if (filename.StartsWith("{"))
                {
                    string filenameCommand = filename.Substring(1, filename.LastIndexOf(':') - 1).Trim();
                    filenameCommand = filenameCommand.Substring(0, filenameCommand.LastIndexOf('}'));
                    buf = filename.Substring(filename.LastIndexOf(':') + 1).Trim();
                    if (buf.ToLower().StartsWith("all"))
                    {
                        #region Process All
                        foreach (FileDependancy s in Sources)
                        {
                            s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s.File);
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("filename"))
                    {
                        #region Process Filename
                        string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (s.File.Trim() == TheFilename)
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s.File);
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("wildcard"))
                    {
                        #region Process Wildcard
                        string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.File.Trim()))
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s.File);
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("regex"))
                    {
                        #region Process Regex
                        string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsRegexMatch(TheRegex, s.File.Trim()))
                            {
                                s.File = ProcessCommon_ProcessBuiltinConstant(ref buf, filenameCommand, s.File);
                            }
                        }
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
            }
        }
        #endregion

        #region Process Common - Suffix
        private void ProcessCommon_Suffix(ref string buf, List<string> suffixes, ref List<FileDependancy> Sources)
        {
            foreach (string suffix in suffixes)
            {
                if (suffix.StartsWith("{"))
                {
                    string suffixCommand = suffix.Substring(1, suffix.LastIndexOf(':') - 1).Trim();
                    suffixCommand = suffixCommand.Substring(0, suffixCommand.LastIndexOf('}'));
                    buf = suffix.Substring(suffix.LastIndexOf(':') + 1).Trim();
                    if (buf.ToLower().StartsWith("all"))
                    {
                        #region Process All
                        foreach (FileDependancy s in Sources)
                        {
                            s.File = s.File + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s.File);
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("filename"))
                    {
                        #region Process Filename
                        string TheFilename = ProcessCommon_ExtractFilename(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (s.File.Trim() == TheFilename)
                            {
                                s.File = s.File + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s.File);
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("wildcard"))
                    {
                        #region Process Wildcard
                        string TheWildcard = ProcessCommon_ExtractWildcard(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsWildcardMatch(TheWildcard, s.File.Trim()))
                            {
                                s.File = s.File + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s.File);
                            }
                        }
                        #endregion
                    }
                    else if (buf.ToLower().StartsWith("regex"))
                    {
                        #region Process Regex
                        string TheRegex = ProcessCommon_ExtractRegex(ref buf);

                        foreach (FileDependancy s in Sources)
                        {
                            if (WildcardEvaluator.IsRegexMatch(TheRegex, s.File.Trim()))
                            {
                                s.File = s.File + " " + ProcessCommon_ProcessBuiltinConstant(ref buf, suffixCommand, s.File);
                            }
                        }
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

        #region Process Define
        private string ProcessDefine(string buf)
        {
            buf = buf.Substring(8).Trim();
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
            else if (buf.ToLower().StartsWith("target"))
            {
                buf = ProcessTarget(buf);
            }
            else
            {
                ErrorManager.Error(16, file, buf);
            }
            return buf;
        }
        #endregion

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
                    if ((buf = stin.ReadLine().Trim()) != "{")
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
                        Config.GlobalSources.Add(sourceName, Process_Define_Source_SourceLines(sources, "all"));
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

        #region Process Define Target

        #region Process Target
        private string ProcessTarget(string buf)
        {
            buf = buf.Substring(6).Trim();
            string Target;
            if (!buf.Trim().StartsWith("("))
            {
                ErrorManager.Error(57, file);
            }
            else
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) != ")")
                {
                    ErrorManager.Error(58, file);
                }
                else
                {
                    buf = buf.Substring(0, buf.Length - 1).Trim();
                    Target = buf;
                    if (Config.Targets.ContainsKey(Target))
                    {
                        if (Target != "all")
                        {
                            ErrorManager.Error(59, file, Target);
                            return buf;
                        }
                    }
                    else
                    {
                        Config.Targets.Add(Target, new TargetConfiguration());
                    }
                    buf = stin.ReadLine().Trim();
                    file.LineNumber++;
                    if (buf != "{")
                    {
                        ErrorManager.Error(60, file, buf);
                        return "";
                    }
                    //throw new Exception();
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
                            if (buf.Trim() != "")
                            {
                                if (buf.ToLower().Trim().StartsWith("#define"))
                                {
                                    buf = ProcessTarget_Define(buf.Trim(), Target);
                                }
                                else if (buf.ToLower().Trim().StartsWith("file"))
                                {
                                    buf = ProcessFile(buf, Target);
                                }
                                else if (buf.ToLower().Trim().StartsWith("directory"))
                                {
                                    buf = ProcessDirectory(buf, "all");
                                }
                                else if (buf.Trim().StartsWith("//"))
                                {
                                    // It's a comment and we ignore it.
                                }
                                else if (buf.ToLower().Trim().StartsWith("common"))
                                {
                                    // We MUST decompose the statements here, 
                                    // otherwise things will get out of order.
                                    buf = ProcessTarget_Common(buf.Trim(), Target);
                                }
                                else
                                {
                                    Config.Targets[Target].Statements.Add(new Statement(buf.Trim()));
                                }
                            }
                        }
                    }
                    if (inSourceBlock)
                    {
                        ErrorManager.Error(61, file);
                        return "";
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Target Define
        private string ProcessTarget_Define(string buf, string target)
        {
            buf = buf.Substring(7).Trim();
            if (buf.ToLower().StartsWith("platform"))
            {
                buf = ProcessTarget_DefinePlatform(buf, target);
            }
            else if (buf.ToLower().StartsWith("tool"))
            {
                buf = ProcessTarget_DefineTool(buf, target);
            }
            else if (buf.ToLower().StartsWith("const"))
            {
                buf = ProcessTarget_DefineConst(buf, target);
            }
            else if (buf.ToLower().StartsWith("source"))
            {
                buf = ProcessTarget_DefineSource(buf, target);
            }
            else if (buf.ToLower().StartsWith("mangler"))
            {
                ErrorManager.Warning(62, file);
                buf = ProcessDefineMangler(buf);
            }
            else
            {
                ErrorManager.Error(63, file, buf);
            }
            return buf;
        }
        #endregion

        #region Process Target Define Const
        private string ProcessTarget_DefineConst(string buf, string target)
        {
            buf = buf.Substring(5);
            if (buf.StartsWith("_"))
            {
                buf = buf.Substring(1).Trim();
                string plat = buf.Substring(0, buf.IndexOf('('));
                if (!Config.IsValidPlatform(plat, target))
                {
                    ErrorManager.Error(64, file, plat, target);
                }
                else
                {
                    buf = buf.Substring(plat.Length, buf.Length - plat.Length).Trim();
                    plat = Config.ResolvePlatform(plat, target);
                    if (!buf.Trim().StartsWith("("))
                    {
                        ErrorManager.Error(65, file);
                    }
                    else
                    {
                        buf = buf.Substring(1).Trim();
                        string cnst = buf.Substring(0, buf.IndexOf(',')).Trim();
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        if (Config.Targets[target].Configs[plat].Constants.ContainsKey(cnst))
                        {
                            ErrorManager.Warning(13, file, cnst);
                            Config.Targets[target].Configs[plat].Constants[cnst] = buf;
                        }
                        else
                        {
                            Config.Targets[target].Configs[plat].Constants.Add(cnst, buf);
                        }
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(66, file, target);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string cnst = buf.Substring(0, buf.IndexOf(',')).Trim();
                    buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                    if (Config.Targets[target].TargetConstants.ContainsKey(cnst))
                    {
                        ErrorManager.Warning(67, file, cnst);
                        Config.Targets[target].TargetConstants[cnst] = buf;
                    }
                    else
                    {
                        Config.Targets[target].TargetConstants.Add(cnst, buf);
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Target Define Source
        private string ProcessTarget_DefineSource(string buf, string target)
        {
            buf = buf.Substring(6);
            if (!buf.Trim().StartsWith("("))
            {
                ErrorManager.Error(68, file);
            }
            else
            {
                buf = buf.Substring(1).Trim();
                if (buf.Substring(buf.Length - 1, 1) != ")")
                {
                    ErrorManager.Error(69, file);
                }
                else
                {
                    buf = buf.Substring(0, buf.Length - 1).Trim();
                    string sourceName = buf;
                    buf = stin.ReadLine().Trim();
                    file.LineNumber++;
                    if (buf != "{")
                    {
                        ErrorManager.Error(70, file, buf);
                        return "";
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
                        ErrorManager.Error(71, file);
                    }
                    else
                    {
                        Config.Targets[target].TargetSources.Add(sourceName, Process_Define_Source_SourceLines(sources, target));
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Target Define Tool
        private string ProcessTarget_DefineTool(string buf, string target)
        {
            buf = buf.Substring(4);
            if (buf.StartsWith("_"))
            {
                buf = buf.Substring(1).Trim();
                string plat = buf.Substring(0, buf.IndexOf('('));
                if (!Config.IsValidPlatform(plat, target))
                {
                    ErrorManager.Error(72, file, plat);
                }
                else
                {
                    buf = buf.Substring(plat.Length, buf.Length - plat.Length).Trim();
                    plat = Config.ResolvePlatform(plat, target);
                    if (!buf.Trim().StartsWith("("))
                    {
                        ErrorManager.Error(73, file);
                    }
                    else
                    {
                        buf = buf.Substring(1).Trim();
                        string tool = buf.Substring(0, buf.IndexOf(',')).Trim();
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        if (Config.Targets[target].Configs[plat].Tools.ContainsKey(tool))
                        {
                            ErrorManager.Warning(74, file, tool);
                            Config.Targets[target].Configs[plat].Tools[tool] = buf;
                        }
                        else
                        {
                            Config.Targets[target].Configs[plat].Tools.Add(tool, buf);
                        }
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(75, file);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string tool = buf.Substring(0, buf.IndexOf(',')).Trim();
                    buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                    if (Config.Targets[target].TargetTools.ContainsKey(tool))
                    {
                        ErrorManager.Warning(76, file, tool);
                        Config.Targets[target].TargetTools[tool] = buf;
                    }
                    else
                    {
                        Config.Targets[target].TargetTools.Add(tool, buf);
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Target Define Platform
        private string ProcessTarget_DefinePlatform(string buf, string target)
        {
            buf = buf.Substring(8);
            if (buf.StartsWith("_ALIAS"))
            {
                buf = buf.Substring(6);
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(77, file);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    string plat = buf.Substring(0, buf.IndexOf(',')).Trim();
                    if (!Config.IsValidPlatform(plat, target))
                    {
                        ErrorManager.Warning(2, file, plat);
                    }
                    // It may not be a valid platform, but 
                    // we're adding the alias none-the-less.
                    if (Config.Targets[target].PlatformAliases.ContainsKey(buf))
                    {
                        ErrorManager.Warning(78, file, buf);
                    }
                    else
                    {
                        buf = buf.Substring(buf.IndexOf(',') + 1, (buf.Length - buf.IndexOf(',')) - 3).Trim();
                        Config.Targets[target].PlatformAliases.Add(buf, plat);
                    }
                }
            }
            else
            {
                if (!buf.Trim().StartsWith("("))
                {
                    ErrorManager.Error(79, file);
                }
                else
                {
                    buf = buf.Substring(1).Trim();
                    if (buf.Substring(buf.Length - 2, 2) != ");")
                    {
                        ErrorManager.Error(80, file, buf.Substring(buf.Length - 2, 2));
                    }
                    else
                    {
                        buf = buf.Substring(0, buf.Length - 2).Trim();
                        if (Config.Targets[target].Platforms.Contains(buf))
                        {
                            ErrorManager.Warning(81, file, buf);
                        }
                        else
                        {
                            Config.Targets[target].Platforms.Add(buf);
                            Config.Targets[target].Configs.Add(buf, new PlatformConfiguration());
                        }
                    }
                }
            }
            return buf;
        }
        #endregion

        #region Process Target Common
        private string ProcessTarget_Common(string buf, string target)
        {
            // We MUST decompose the statements here, 
            // otherwise things will get out of order.
            buf = buf.Substring(6).Trim();
            if (buf.ToLower().StartsWith("tool"))
            {
                #region Process Tool
                buf = buf.Substring(4).Trim();
                if (buf.StartsWith("("))
                {
                    buf = buf.Substring(1).Trim();
                    string baseCommand = buf.Substring(0, buf.LastIndexOf(')'));
                    buf = buf.Substring(buf.LastIndexOf(')') + 1).Trim();
                    if (buf.StartsWith(":"))
                    {
                        buf = buf.Substring(1).Trim();
                        // All that should be left at this point is 
                        // the list we'll be iterating through.
                        if (Config.ResolveSource("WIN32", target, buf) != null)
                        {
                            string SourceListName = buf;
                            // We have a valid list, now we need to
                            // process the options for the list.

                            #region Read the expressions
                            buf = stin.ReadLine().Trim();
                            file.LineNumber++;
                            if (buf != "{")
                            {
                                ErrorManager.Error(86, file, buf);
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
                                ErrorManager.Error(87, file);
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
                                    ErrorManager.Error(88, file, s.Trim());
                                }
                            }
                            #endregion

#warning Give correct location when we support platform specific sources.

                            List<FileDependancy> Srces = new List<FileDependancy>(Config.ResolveSource("WIN32", target, SourceListName));
                            List<FileDependancy> Sources = new List<FileDependancy>();

                            foreach (FileDependancy s in Srces)
                            {
                                Sources.Add(new FileDependancy(s.File, s.Target, s.Platform, s.Dependancies));
                            }

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


                            int indxs = 0;
                            // Now we can emit the expanded version.
                            foreach (FileDependancy s in Sources)
                            {
                                List<IDependancy> sr = new List<IDependancy>();
                                sr.Add(Srces[indxs]);
                                Config.Targets[target].Statements.Add(new Statement(baseCommand + s.File, sr));
                                indxs++;
                            }

                            // And now a bit of cleanup
                            expressions = null;
                            Sources = null;
#warning CHECK WHICH MODE IS BEST FOR GIANT MAKEFILES
                            //GC.Collect(0, GCCollectionMode.Forced);
                        }
                        else
                        {
                            ErrorManager.Error(83, file, buf);
                        }
                    }
                    else
                    {
                        ErrorManager.Error(84, file, buf.Substring(0, 1));
                    }
                }
                else
                {
                    ErrorManager.Error(85, file, buf.Substring(0, 1));
                }
                #endregion
            }
            else
            {
                ErrorManager.Error(82, file, buf);
            }
            return buf;
        }
        #endregion

        #endregion

    }
}
