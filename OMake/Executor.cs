#if DEBUG
//#define NO_EXECUTE // If this is defined we don't do the actual execution of the makefile.
#endif
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace OMake
{
    /// <summary>
    /// Does the actual execution of an OMake file.
    /// </summary>
    public class Executor
    {
        private static readonly Regex CustomConstant_Regex;
        /// <summary>
        /// This is used to get an inner list out of
        /// a statement, this must be done AFTER the
        /// processing of the custom constants, otherwise
        /// it will include the constant in the match.
        /// </summary>
        private static readonly Regex InnerList_Regex;
        /// <summary>
        /// This is used to get the list and manglings 
        /// peice out of an inner list.
        /// </summary>
        private static readonly Regex InnerInnerList_Regex;
        /// <summary>
        /// This gets the actual list itself out of the
        /// inner inner list. We really need to find a 
        /// better name for this thing.
        /// </summary>
        private static readonly Regex InnerInnerInnerList_Regex;

        static Executor()
        {
            // We use the compiled versions because we need the speed.
            CustomConstant_Regex = new Regex(@"\$\s*{\s*[A-Za-z_.]+\s*}", RegexOptions.Compiled);
            //InnerList_Regex = new Regex(@"\$\s*\{\s*.*&\s*(\(\s*(\[\s*[A-Za-z0-9_]+\s*\])\s*[A-Za-z0-9_.]*\s*\))\s*.*\}", RegexOptions.Compiled);
            InnerList_Regex = new Regex(@"\$\s*\{.*?\}", RegexOptions.Compiled);
            InnerInnerList_Regex = new Regex(@"\&\(.*?\)", RegexOptions.Compiled);
            InnerInnerInnerList_Regex = new Regex(@"\[[A-Za-z0-9_]+\]", RegexOptions.Compiled);
        }

        private readonly Configuration Config;

        public Executor(Processor proc)
        {
            this.Config = proc.Config;
        }

        #region Resolve Tools
        private void ResolveTools(string platform, string target)
        {
            foreach (Statement st in Config.Targets[target].Statements)
            {
                if (st.Type == StatementType.Standard)
                {
                    string s = st.StatementValue;
                    string tmps = s.Trim();
                    string tool = s.Trim().Substring(0, s.Trim().IndexOf(' ')).Trim();
                    tool = Config.GetTool(platform, target, tool);
                    tool = Path.GetFullPath(tool);
                    string args = s.Trim().Substring(s.Trim().IndexOf(' ')).Trim();
                    st.StatementValue = tool.Trim() + "|" + args.Trim();
                }
            }
        }
        #endregion

        #region Final Decomp
        public void FinalDecomp(string platform, List<string> targets)
        {
            foreach (string target in targets)
            {
                FinalDecompisition(platform, target);
                ResolveTools(platform, target);
            }
        }
        #endregion

        public void Execute(string platform, List<string> targets)
        {
            foreach (string target in targets)
            {
                if (ErrorManager.ErrorCount > 0)
                {
                    ErrorManager.Error(54, Processor.file);
                    return;
                }

                #region Setup the cache
                {
                    MemoryStream m = new MemoryStream();
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter b = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.File));
                    b.Serialize(m, Config);
                    Cache.SetValue("Makefile.ConfigCache", m.GetBuffer());
                }
                #endregion

                foreach (Statement st in Config.Targets[target].Statements)
                {
                    if (ErrorManager.ErrorCount > 0)
                    {
                        ErrorManager.Error(54, Processor.file);
                        return;
                    }

                    switch (st.Type)
                    {
                        case StatementType.File:
                            #region File Statement
                            FileStatement stmt = (FileStatement)st;
                            switch (stmt.Type)
                            {
                                case FileStatementType.Create:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        ErrorManager.Error(94, Processor.file, stmt.Filename);
                                        return;
                                    }
                                    else
                                    {
                                        StreamWriter f = File.CreateText(stmt.Filename);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    break;
                                case FileStatementType.CreateOrTruncate:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        StreamWriter f = new StreamWriter(stmt.Filename);
                                        f.BaseStream.Position = 0;
                                        f.BaseStream.SetLength(0);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    else
                                    {
                                        StreamWriter f = File.CreateText(stmt.Filename);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    break;
                                case FileStatementType.Append:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        StreamWriter f = new StreamWriter(stmt.Filename, true);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    else
                                    {
                                        ErrorManager.Error(95, Processor.file, stmt.Filename);
                                        return;
                                    }
                                    break;
                                case FileStatementType.CreateOrAppend:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        StreamWriter f = new StreamWriter(stmt.Filename, true);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    else
                                    {
                                        StreamWriter f = File.CreateText(stmt.Filename);
                                        f.Write(DecomposeString(stmt.Arg1, platform, target));
                                        f.Flush();
                                        f.Close();
                                    }
                                    break;
                                case FileStatementType.Delete:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        File.Delete(stmt.Filename);
                                    }
                                    else
                                    {
                                        ErrorManager.Error(94, Processor.file, stmt.Filename);
                                    }
                                    break;
                                case FileStatementType.TryDelete:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        File.Delete(stmt.Filename);
                                    }
                                    break;
                                case FileStatementType.Copy:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        if (File.Exists(stmt.Arg1))
                                        {
                                            ErrorManager.Error(94, Processor.file, stmt.Arg1);
                                            return;
                                        }
                                        File.Copy(stmt.Filename, stmt.Arg1);
                                    }
                                    else
                                    {
                                        ErrorManager.Error(94, Processor.file, stmt.Filename);
                                        return;
                                    }
                                    break;
                                case FileStatementType.ForceCopy:
                                    if (File.Exists(stmt.Filename))
                                    {
                                        File.Copy(stmt.Filename, stmt.Arg1, true);
                                    }
                                    else
                                    {
                                        ErrorManager.Error(94, Processor.file, stmt.Filename);
                                        return;
                                    }
                                    break;
                                default:
                                    throw new Exception("Well, an error definately occurred, because this shouldn't ever be getting called.");
                            }

                            #endregion
                            break;

                        case StatementType.Standard:
                            #region Normal Statement
                            if (st.Modified)
                            {
                                string s = st.StatementValue;
                                if (s.Trim() != "")
                                {
                                    ProcessStartInfo psi = new ProcessStartInfo(s.Substring(0, s.IndexOf('|')), s.Substring(s.IndexOf('|') + 1).Trim());
                                    psi.UseShellExecute = false;
                                    psi.RedirectStandardOutput = true;
                                    psi.RedirectStandardError = true;
#if NO_EXECUTE
                                    Process p = new Process();
                                    p.StartInfo = psi;
                                    Console.WriteLine("{0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
                                    Log.WriteLine(string.Format("{0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments));
#else
                            Process p = new Process();
                            p.StartInfo = psi;
                            p.OutputDataReceived += new DataReceivedEventHandler(DataRecieved);
                            p.ErrorDataReceived += new DataReceivedEventHandler(DataRecieved);
                            Console.WriteLine("{0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
                            Log.WriteLine(string.Format("{0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments));
                            p.Start();
                            p.BeginOutputReadLine();
                            p.BeginErrorReadLine();
                            p.WaitForExit();
                            if (p.ExitCode != 0)
                            {
                                ErrorManager.Error(53, Processor.file, p.ExitCode.ToString());
                                return;
                            }
#endif
                                }
                                st.SetCache();
                            }
                            else
                            {
                                Log.WriteLine("Dependancies not modified, so not executing statement '" + st.StatementValue + "'.");
                            }
                            #endregion
                            break;
                    }
                }
            }
        }
        private void DataRecieved(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                Console.WriteLine(args.Data);
                Log.WriteLine(args.Data);
            }
        }

        #region Decompose String
        private string DecomposeString(string str, string plat, string target)
        {
            string tmp = str;
            string buf = "";
        BeforeResolve:
            foreach (Match m in CustomConstant_Regex.Matches(tmp))
            {
                #region Custom Constant
                buf = m.Value.Substring(1).Trim();
                buf = buf.Substring(1).Trim();
                buf = buf.Substring(0, buf.Length - 1).Trim();
                // It's now a valid constant (or it should be)
                tmp = tmp.Replace(m.Value, Config.GetConstant(plat, target, buf));
                #endregion
            }
            if (CustomConstant_Regex.IsMatch(tmp))
                goto BeforeResolve;
        BeforeInnerListResolve:
            // This has to be done after the custom 
            // constants are replaced, otherwise it
            // will include the constants in the match.
            if (InnerList_Regex.IsMatch(tmp))
            {
                #region Inner List
                // Fun, fun, we get to expand a nice list
                buf = InnerList_Regex.Match(tmp).Value.Trim();
                buf = buf.Substring(1).Trim();
                // We need 2 statments here because whitespaces
                // are allowed in between the characters.
                buf = buf.Substring(1).Trim();
                buf = buf.Substring(0, buf.Length - 1).Trim();
                // We've now have removed the brackets around it.
                // We need the location of the list now.
                int indx = buf.IndexOf(InnerInnerList_Regex.Match(tmp).Value);
                // And use this to grab the prefix.
                string prefix = buf.Substring(0, indx);
                // And the list itself.
                string innerLst = InnerInnerList_Regex.Match(tmp).Value;
                // Followed by the suffix.
                string suffix = buf.Substring(indx + innerLst.Length);
                List<StringMangler> ManglersToApply = new List<StringMangler>();
                string innerinnerLst = InnerInnerInnerList_Regex.Match(innerLst).Value.Trim();
                innerinnerLst = innerinnerLst.Substring(1).Trim();
                innerinnerLst = innerinnerLst.Substring(0, innerinnerLst.Length - 1).Trim();
                // Now we have the actual name of the list.
                List<SourceFile> srces = Config.ResolveSource(plat, target, innerinnerLst);
                innerLst = innerLst.Substring(2).Trim();
                innerLst = innerLst.Substring(1).Trim();
                innerLst = innerLst.Substring(innerinnerLst.Length).Trim();
                innerLst = innerLst.Substring(1).Trim();
                innerLst = innerLst.Substring(1).Trim();
                innerLst = innerLst.Substring(0, innerLst.Length - 1).Trim();
                // We now have just the manglings.
                foreach (string strng in innerLst.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Processor.ValidManglers.ContainsKey(strng.ToLower()))
                    {
                        ManglersToApply.Add(Processor.ValidManglers[strng.ToLower()]);
                    }
                    else
                    {
                        throw new Exception("Unknown Mangler '" + strng + "'!");
                    }
                }
                List<string> finalNames = new List<string>();
                foreach (SourceFile sf in srces)
                {
                    string strng = sf.File;
                    string bfr = strng;
                    foreach (StringMangler strmglr in ManglersToApply)
                    {
                        bfr = strmglr(bfr);
                    }
                    finalNames.Add(prefix + bfr + suffix);
                }
                string finalReplaced = "";
                bool first = true;
                foreach (string strng in finalNames)
                {
                    if (first)
                    {
                        finalReplaced = strng;
                        first = false;
                    }
                    else
                    {
                        finalReplaced += " " + strng;
                    }
                }
                tmp = tmp.Replace(InnerList_Regex.Match(tmp).Value, finalReplaced);
                #endregion
            }
            if (InnerList_Regex.IsMatch(tmp))
                goto BeforeInnerListResolve;
            return tmp;
        }
        #endregion

        #region Final Decompisition
        private void FinalDecompisition(string plat, string target)
        {
            foreach (Statement st in Config.Targets[target].Statements)
            {
                st.StatementValue = DecomposeString(st.StatementValue, plat, target);
            }

        }
        #endregion

    }
}
