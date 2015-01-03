using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Mac.Arma.FileFormats;
using System.Text.RegularExpressions;

namespace sqfc {
	unsafe class Program {

		static bool Listing = false;
		static bool Dumping = true;
		static bool Stripping = false;
		static bool Testing = false;
        static bool Updating = false;
		static string DefaultOut = "sqfc.out";
		static string Out = "";

		static int DoArgs(ref string[] Argv) {
			int i = 0;
			for (i = 0; i < Argv.Length; i++) {
				if (Argv[i][0] != '-')                    /* end of options */
					break;
				else if (Argv[i] == ("-"))                     /* end of options; use stdin */
					return i;
				else if (Argv[i] == ("-l"))                    /* list */
					Listing = true;
				else if (Argv[i] == ("-o"))                    /* output file */ {
					Out = Argv[++i];
					if (Out.Length == 0)
						Usage("", "");
				} else if (Argv[i] == ("-p"))                    /* parse only */
					Dumping = false;
				else if (Argv[i] == ("-s"))                    /* strip debug information */
					Stripping = true;
				else if (Argv[i] == ("-t"))                    /* test */ {
					Testing = true;
					Dumping = false;
               } else if (Argv[i] == ("-u") || Argv[i] == ("-update"))                    /* update */ {
					Updating = true;
				} else if (Argv[i] == ("-v"))                    /* show version */ {
					Console.WriteLine("{0}  {1}\n", "SQFC X.Y", "<insert copyright here>");
					if (Argv.Length == 2)
						Environment.Exit(0);
				} else                                  /* unknown option */
					Usage("unrecognized option ", Argv[i]);
			}
			if (i == Argv.Length && (Listing || Testing)) {
				Dumping = false;
				Argv[--i] = DefaultOut;
			}
			return i;
		}

		static void Usage(string Msg, string Arg) {
			if (Msg.Length > 0)
				Console.WriteLine("sqfc: {0}{1}", Msg, Arg);
			Console.WriteLine("usage: sqfc [options] [filenames].  Available options are:\n"
				+ "  -        process stdin\n"
				+ "  -l       list [disabled]\n"
				+ "  -o file  output file (default is \"" + DefaultOut + "\")\n"
				+ "  -p       parse only\n"
				+ "  -s       strip debug information [disabled]\n"
				+ "  -t       test code integrity [disabled]\n"
                + "  -u       update and download dependencies\n"
				+ "  -v       show version information\n");
			Environment.Exit(1);
		}

        static string GetLine(string text, int lineNo)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            return lines.Length >= lineNo ? (lines[lineNo - 1]) + '\n' : null;
        }

		static bool Exists(string Name) {
			if (File.Exists(Path.GetFullPath(Name)))
				return true;
			foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';')) {
				string path = test.Trim();
				if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, Name)))
					return true;
			}
			return false;
		}

		static void Warn(string Msg) {
			Console.WriteLine("WARNING: {0}", Msg);
		}

		static string Read() {
			return Console.In.ReadToEnd();
		}

		static void Main(string[] Args)
		{
			int ii = DoArgs(ref Args);

            if (Updating)
		    {
		        if (Updater.CheckForUpdate() == UpdateStatus.NewVersionAvailable)
		        {
                    Console.WriteLine("Updating sqfc...");
                    Updater.LaunchUpdater(Updater.Manifest);
                    Environment.Exit(1);
		        }
		    }

			int Argc = Args.Length;
			if (Argc <= 0)
				Usage("no input files given", "");

			if (Out.Length == 0)
				Out = DefaultOut;

			for (int i = ii; i < Argc; i++) {
				string In = (Args[i] == "-" ? Read() : File.ReadAllText(Args[i]));
				string ChunkName = "stdin";
				if (Args[i] != "-")
					ChunkName = Path.GetFileName(Path.GetFullPath(Args[i]));

				Run(In, Out, ChunkName);
			}
		}

		static void Run(string In, string Out, string ChunkName) 
        {
            PreProcessor preProcessor = new PreProcessor("", new DefaultFileFetcher());
            preProcessor.PreProcessTopLevelFromString(In, "**scratch**");

            string mainString = preProcessor.folded.Text(true);

            BISTextTokeniser tokeniser = BISTextTokeniser.FromString(mainString, true, true);
            SqfParser sqfParser = new SqfParser(tokeniser, preProcessor.Errors, GrammarType.Sqf);
            var tree = sqfParser.Parse();

            var globalVars = sqfParser.GlobalVars();
            //this.Folded.FoldTopLevel();

            sqfParser.CheckErrors(globalVars);

            //File.WriteAllText(DefaultOut, "");

            foreach (ParseError current in sqfParser.errors)
            {
                var severity = current.Severity.ToString().Substring(0, 1);
                var fixAvailable = "N";

                if (current.Action != ErrorActionType.None)
                {
                    fixAvailable = "Y";
                }

                var lineNumber = mainString.Take(current.Position).Count(c => c == '\n') + 1;
                //var lineText = GetLine(mainString, lineNumber);

                var column = 0; //(current.Position - (current.Position - current.Length)) + 2;

                string ErrMsg = "";
                if (current.Severity < ErrorSeverity.Warning)
                {
                    ErrMsg = String.Format("E:{0}:{1}:{2}: {3}", ChunkName, lineNumber, column, current.Text);
                }
                else
                {
                    if (current.Text.Contains("Unknown variable ")) { continue; }

                    ErrMsg = String.Format("W:{0}:{1}:{2}: {3}", ChunkName, lineNumber, column, current.Text);
                }

                Console.WriteLine(ErrMsg);
                //File.AppendAllText(DefaultOut, ErrMsg + "\n");

                //Console.WriteLine("Sev: " + severity + ", Fix: " + fixAvailable + ", Text:" + current.Text + ", Len:" + current.Length + ", Line #:" + lineNumber);

                //if (current.Action != ErrorActionType.None)
                //{
                //    Console.WriteLine("---- Action: " + current.ActionText + Environment.NewLine);
                //}
            }

            Environment.Exit(2);
		}
	}
}