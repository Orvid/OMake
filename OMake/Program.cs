using System;
using System.IO;
using System.Collections.Generic;

namespace OMake
{
	public class Program
	{
		public static void Main(string[] args)
        {
            StreamReader strm = null;
            System.Diagnostics.Stopwatch t = new System.Diagnostics.Stopwatch();
#if UseLog
            Log.Initialize();
#else
            Log.Initialize_NoLog();
#endif
            string filename = "";
            string platform = "";
            List<string> targets = new List<string>();
            

            t.Start();
            #region Load Makefile
            if (args.Length > 0)
            {
                #region We have arguments.
                int i = 0;
                string arg;
                while (i < args.Length)
                {
                    arg = args[i];
                    i++;
                    if (arg.Trim() == "-f")
                    {
                        if (filename != "")
                        {
                            Console.WriteLine("Filename already specified!");
                            return;
                        }
                        else
                        {
                            if (i >= args.Length)
                            {
                                Console.WriteLine("Expected filename after -f, but didn't get it!");
                                return;
                            }
                            else
                            {
                                arg = args[i];
                                i++;
                                if (File.Exists(arg))
                                {
                                    filename = arg;
                                    try
                                    {
                                        strm = new StreamReader(arg);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("An error occurred when opening the file! '" + arg + "'");
                                        return;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Couldn't find the file at '" + arg + "'! Perhaps you forgot to create it?");
                                    return;
                                }
                            }
                        }
                    }
                    else if (arg.Trim() == "-p")
                    {
                        if (platform != "")
                        {
                            Console.WriteLine("Platform already specified!");
                            return;
                        }
                        else
                        {
                            if (i >= args.Length)
                            {
                                Console.WriteLine("Expected platform after -p, but didn't get it!");
                                return;
                            }
                            else
                            {
                                arg = args[i];
                                i++;
                                platform = arg;
                            }
                        }
                    }
                    else // else it's a target.
                    {
                        if (targets.Contains(arg.Trim()))
                        {
                            Console.WriteLine("Target '" + arg.Trim() + "' already specified!");
                            return;
                        }
                        else
                        {
                            targets.Add(arg.Trim());
                        }
                    }
                }

                #region Setup Defaults
                if (targets.Count == 0)
                {
                    targets.Add("all");
                }
                if (platform == "")
                {
                    platform = "WIN32";
                }
                if (filename == "")
                {
                    if (File.Exists("makefile.omake"))
                    {
                        try
                        {
                            strm = new StreamReader("makefile.omake");
                        }
                        catch
                        {
                            Console.WriteLine("An error occurred when opening the file! '" + args[0] + "'");
                            return;
                        }
                        filename = "makefile.omake";
                    }
                    else
                    {
                        Console.WriteLine("Could not find the default makefile, and no makefile was specified!");
                        return;
                    }

                }
                #endregion

                #endregion
            }
            else
            {
                #region We have no arguments
                if (File.Exists("makefile.omake"))
                {
                    try
                    {
                        strm = new StreamReader("makefile.omake");
                    }
                    catch
                    {
                        Console.WriteLine("An error occurred when opening the file! '" + args[0] + "'");
                        return;
                    }
                    filename = "makefile.omake";
                    platform = "WIN32";
                    targets.Add("all");
                }
                else
                {
                    Console.WriteLine("Could not find the default makefile, and no makefile was specified!");
                    return;
                }
                #endregion
            }
            #endregion
            t.Stop();
            Log.WriteLine("Time to Locate Makefile: '" + t.ElapsedMilliseconds.ToString() + "'");
            t.Reset();


            t.Start();
            Processor prc = new Processor(strm, filename);
            System.GC.Collect();
            t.Stop();
            Log.WriteLine("Processor Initialization Time: '" + t.ElapsedMilliseconds.ToString() + "'");
            t.Reset();

            Cache.Initialize();

            t.Start();
            bool NeedToReParse = true;
            byte[] chksum = System.Security.Cryptography.SHA256.Create().ComputeHash(strm.BaseStream);
            if (Cache.Contains("MakefileChecksum"))
            {
                byte[] oldChksum = Cache.GetData("MakefileChecksum");
                if (Helpers.ByteArrayEqual(chksum, oldChksum))
                {
                    if (Cache.Contains("Makefile.ConfigCache"))
                    {
#if !DEBUG
                        MemoryStream m = new MemoryStream(Cache.GetData("Makefile.ConfigCache"));
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter b = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.File));
                        prc.Config = (Configuration)b.Deserialize(m);
                        NeedToReParse = false;
#endif
                    }
                }
                else
                {
                    Cache.SetValue("MakefileChecksum", chksum);
                }
            }
            else
            {
                Cache.SetValue("MakefileChecksum", chksum);
            }
            strm.BaseStream.Position = 0;

            if (NeedToReParse)
            {
                prc.Process();
            }
            t.Stop();
            Log.WriteLine("Processing Time: '" + t.ElapsedMilliseconds.ToString() + "'");
            t.Reset();


            Executor e = new Executor(prc);
            if (NeedToReParse)
            {
                e.FinalDecomp(platform, targets);
            }
            e.Execute(platform, targets);

            Log.Cleanup();
            return;
		}
    }
}