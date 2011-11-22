using System;
using System.IO;

namespace OMake
{
	public class Program
	{
		public static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch t = new System.Diagnostics.Stopwatch();
            StreamReader strm = null;
            Log.Initialize();
            string filename = "makefile.omake";

            t.Start();
            #region Load Makefile
            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                {
                    filename = args[0];
                    try
                    {
                        strm = new StreamReader(args[0]);
                    }
                    catch
                    {
                        Console.WriteLine("An error occurred when opening the file! '" + args[0] + "'");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Couldn't find the file at '" + args[0] + "'! Perhaps you forgot to create it?");
                }
            }
            else
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
                }
                else
                {
                    Console.WriteLine("Could not find the default makefile, and no makefile was specified!");
                    return;
                }
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


            t.Start();
            prc.Process();
            t.Stop();
            Log.WriteLine("Processing Time: '" + t.ElapsedMilliseconds.ToString() + "'");
            t.Reset();


            Executor e = new Executor(prc);
            e.Execute("WIN32");


            Log.Cleanup();
            return;
		}
	}
}