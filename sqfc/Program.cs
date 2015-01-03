using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Mac.Arma.FileFormats;

namespace sqfc {
	unsafe class Program {

		static bool Listing = false;
		static bool Dumping = true;
		static bool Stripping = false;
		static bool Testing = false;
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
				+ "  -v       show version information\n");
			Environment.Exit(1);
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

		static void Main(string[] Args) {
			int ii = DoArgs(ref Args);
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

		static void Run(string In, string Out, string ChunkName) {

            BISTextTokeniser tokeniser = BISTextTokeniser.FromString(In, true, true);
            SqfParser sqfParser = new SqfParser(tokeniser, null, GrammarType.Sqf);
            sqfParser.Parse();

            foreach (ParseError current in sqfParser.errors)
            {
                var severity = current.Severity.ToString().Substring(0, 1);
                var fixAvailable = "N";

                if (current.Action != ErrorActionType.None)
                {
                    fixAvailable = "Y";
                }

                var lineNumber = In.Take(current.Position).Count(c => c == '\n') + 1;


                string ErrMsg = String.Format("{0}:{1}:{2}: {3}", ChunkName, lineNumber, current.Length, current.Text);

                if (current.Severity < ErrorSeverity.Warning)
                {
                    Console.WriteLine("error: {0}", ErrMsg);
                }
                else
                {
                    Console.WriteLine("warning: {0}", ErrMsg);
                }

                Environment.Exit(2);

                //Console.WriteLine("Sev: " + severity + ", Fix: " + fixAvailable + ", Text:" + current.Text + ", Len:" + current.Length + ", Line #:" + lineNumber);

                //if (current.Action != ErrorActionType.None)
                //{
                //    Console.WriteLine("---- Action: " + current.ActionText + Environment.NewLine);
                //}
            }

			/*IntPtr L = Lua.NewState();
			Lua.OpenLibs(L);
			Lua.AtPanic(L, (LL) => {
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("\n\nPANIC!");
				Console.ResetColor();
				Console.WriteLine(Lua.ToString(LL, -1));
				Console.ReadKey();
				Environment.Exit(0);
				return 0;
			});

			Lua.RegisterCFunction(L, "_G", "dumpBytecode", (LL) => {
				if (Dumping) {
					int Len = 0;
					char* Bytecode = (char*)Lua.ToLString(L, -1, new IntPtr(&Len)).ToPointer();
					string BytecodeStr = new string((sbyte*)Bytecode, 0, Len, Encoding.ASCII);
					File.WriteAllText(Out, BytecodeStr);
				}
				return 0;
			});

			Lua.SetTop(L, 0);
			Lua.GetGlobal(L, "dumpBytecode");
			Lua.GetGlobal(L, "string");
			Lua.PushString(L, "dump"); // Yes, it uses string.dump, and no, i'm not gonna implement proper dump function (lazy)
			Lua.GetTable(L, -2);
			ErrorCheck(L, Lua.LoadBuffer(L, In, ChunkName));
			ErrorCheck(L, Lua.PCall(L, 1, 1, 0));
			Lua.Replace(L, -2);
			ErrorCheck(L, Lua.PCall(L, 1, 0, 0));
			Lua.Close(L);*/
		}
	}
}