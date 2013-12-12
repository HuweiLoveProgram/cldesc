/*!
	Copyright (C) 2004-2013 Kody Brown (kody@bricksoft.com).
	
	MIT License:
	
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to
	deal in the Software without restriction, including without limitation the
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bricksoft.PowerCode;

namespace cldesc
{
	public class Program
	{
		private static string appName = "cldesc";
		private static EnvironmentVariables envars;

		public static int Main( string[] args )
		{
			envars = new EnvironmentVariables(appName);

			string file = null;
			SortedDictionary<string, string> dc;
			string line;
			bool exists;
			string name;
			string comment;
			StringBuilder cb;
			int index;
			string a;
			bool vf; // verify file exists
			string sfn; // saved backup file name
			bool sf; // don't delete the backup file
			bool oc; // output to the console, instead of back to the file
			Encoding enc; // the encoding used to open the source descript.ion file

			vf = false;
			sf = false;
			oc = false;

			// Load environment variables first.
			// Command-line arguments will overwrite them when present.
			if (envars.contains("vf")) {
				vf = envars.attr<bool>( "vf");
			}
			if (envars.contains( "sf")) {
				sf = envars.attr<bool>("sf");
			}

			if (args.Length > 0) {
				for (int i = 0; i < args.Length; i++) {
					a = args[i];
					if (a.StartsWith("/") || a.StartsWith("-") || a.StartsWith("!")) {
						while (a.StartsWith("/") || a.StartsWith("-")) {
							a = a.Substring(1);
						}

						if (a.StartsWith("h", StringComparison.CurrentCultureIgnoreCase) || a.Equals("?", StringComparison.CurrentCultureIgnoreCase)) {
							usage();
							return 0;

						} else if (a.Equals("vf", StringComparison.CurrentCultureIgnoreCase)) {
							vf = true;
						} else if (a.Equals("!vf", StringComparison.CurrentCultureIgnoreCase)) {
							vf = false;

						} else if (a.Equals("sf", StringComparison.CurrentCultureIgnoreCase)) {
							sf = true;
						} else if (a.Equals("!sf", StringComparison.CurrentCultureIgnoreCase)) {
							sf = false;

						} else if (a.Equals("oc", StringComparison.CurrentCultureIgnoreCase)) {
							oc = true;
						} else if (a.Equals("!oc", StringComparison.CurrentCultureIgnoreCase)) {
							oc = false;

						}
					} else {
						// get the filename
						file = a;
					}
				}
			}

			if (StdInEx.IsInputRedirected) {
				file = StdInEx.RedirectInputToFile();
				oc = true;
			} else {
				if (file == null || file.Length == 0) {
					//Console.WriteLine("Using: " + Environment.CurrentDirectory);
					file = Path.Combine(Environment.CurrentDirectory, "descript.ion");
				}
			}

			if (!File.Exists(file)) {
				Console.WriteLine("there is no descript.ion file in the current folder");
				return 1;
			}

			try {
				BackupFile(file, "bak_", out sfn);
			} catch (Exception ex) {
				Console.WriteLine("error backing up descript.ion file: " + ex.Message);
				return 2;
			}

			dc = new SortedDictionary<string, string>();
			cb = new StringBuilder();
			enc = Encoding.Default;

			using (StreamReader r = new StreamReader(File.OpenRead(file), Encoding.Default, true)) {
				enc = r.CurrentEncoding;

				while (!r.EndOfStream) {
					if ((line = r.ReadLine()) != null) {
						// do not trim the line itself!
						if (line.Trim().Length == 0) {
							continue;
						}

						// Get the file name and comments
						if (line.StartsWith("\"")) {
							index = line.IndexOf("\" ", 1);
							if (index == -1) {
								Console.WriteLine("--> invalid line:");
								Console.WriteLine("    " + line.Substring(0, Math.Min(80, line.Length)));
								continue;
							}
							index += 2;
						} else {
							index = line.IndexOf(' ');
						}

						if (index == -1) {
							continue;
						}

						name = line.Substring(0, index).Trim().Trim('"', ' ', '\t');
						comment = line.Substring(index).Trim();

						// Verify that the file exists in the current folder.
						if (vf) {
							if (!File.Exists(name) && !Directory.Exists(name)) {
								Console.WriteLine("--> file/folder was not found: " + name);
								sf = true;
								continue;
							}
						}

						// Ensure a unique file name for the dictionary
						exists = false;
						while (dc.ContainsKey(name)) {
							exists = true;
							name += "_";
						}
						if (exists) {
							Console.WriteLine("--> duplicate entry was found.. saved as: " + name);
						}

						// Cleanup the comment
						//while (comment.IndexOf('’') > -1) {
						//   comment = comment.Replace('’', '\'');
						//}

						//cb.Clear();
						//foreach (char ch in comment) {
						//   if (ch < 32 || ch > 126) {
						//      // 9=tab
						//      // 10=new line
						//      // 13=carriage return
						//      // 169=copyright symbol
						//      // 174=registered symbol
						//      //	
						//      if (ch != 9 && ch != 10 && ch != 13 && ch != 169 && ch != 174 && ch != '•') {
						//         //cb.Append(' ');
						//         continue;
						//      }
						//   }
						//   cb.Append(ch);
						//}
						//comment = cb.ToString().Trim();

						while (comment.IndexOf("\\\\n") > -1) {
							comment = comment.Replace("\\\\n", "\\n");
						}

						while (comment.IndexOf((char)4) == comment.Length - 1 || comment.IndexOf((char)194) == comment.Length - 1) {
							comment = comment.Substring(0, comment.Length - 1);
						}

						while (comment.EndsWith("\\n")) {
							comment = comment.Substring(0, comment.Length - 2).TrimEnd();
						}

						dc.Add(name, comment);
					}
				}

				r.Close();
			}

			try {
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			} catch (Exception ex) {
				Console.WriteLine("error: " + ex.Message);
				return 3;
			}

			if (dc.Count > 0) {
				if (oc) {
					foreach (KeyValuePair<string, string> p in dc) {
						Console.Write(string.Format("{0,-30}{1}", p.Key, p.Value));
					}
				} else if (enc != null) {
					try {
						using (FileStream fs = File.Create(file)) {
							foreach (KeyValuePair<string, string> p in dc) {
								byte[] b;

								if (p.Key.Contains(" ")) {
									name = "\"" + p.Key + "\"";
								} else {
									name = p.Key;
								}
								comment = p.Value;

								b = new byte[name.Length];
								enc.GetBytes(name.ToCharArray(), 0, name.Length, b, 0);
								fs.Write(b, 0, b.Length);

								fs.Write(new byte[] { 32 }, 0, 1);

								b = new byte[comment.Length];
								enc.GetBytes(comment.ToCharArray(), 0, comment.Length, b, 0);
								fs.Write(b, 0, b.Length);

								// Write out the special characters to indicate to Total Commander that there are new lines in the comments.
								if (comment.IndexOf("\\n") > -1) {
									fs.Write(new byte[] { 4, 194 }, 0, 2);
								}

								// New line and carriage return.
								fs.Write(new byte[] { 13, 10 }, 0, 2);
							}
							fs.Flush();
							fs.Close();
						}
					} catch (Exception ex) {
						Console.WriteLine("error writing descript.ion file: " + ex.Message);
						if (File.Exists(file)) {
							File.SetAttributes(file, FileAttributes.Normal);
							File.Delete(file);
						}
						File.Move(sfn, file);
						Console.WriteLine("original file was put back");
						return 4;
					}
				} else {
					Console.WriteLine("**** ERROR: failed to get the encoding of the input.");
				}
			}

			// Since we're not updating the file, there is no
			// need to create a backup of it. --> !oc
			if (!sf && !oc) {
				try {
					File.SetAttributes(sfn, FileAttributes.Normal);
					File.Delete(sfn);
				} catch (Exception ex) {
					Console.WriteLine("error deleting backup file: " + ex.Message);
					return 5;
				}
			}

			return 0;
		}

		public static void BackupFile( string fn, string prefix, out string sfn )
		{
			string backup;
			int index;
			string fname;
			string fext;

			index = 0;
			fname = Path.GetDirectoryName(fn) + "\\" + Path.GetFileNameWithoutExtension(fn);
			fext = Path.GetExtension(fn);

			do {
				backup = string.Format("{0}.{3}{2}{1}", fname, fext, (++index).PadLeft(5, '0'), prefix);
			} while (File.Exists(backup));

			sfn = backup;

			File.Copy(fn, backup, true);
		}

		public static void usage()
		{
			string conChar = (Path.DirectorySeparatorChar == '\\') ? ">" : "$";
			int width = Console.WindowWidth,
				inda = 2,
				indb = 10,
				indc = inda + indb,
				indd = inda + inda;
			string format = "{0,-" + indb + "}{1}";
			//string exampleFormat = "{0}\n\n{1}";

			Console.WriteLine(Text.Wrap("cldesc | Cleans up a descript.ion file", width, 0, 12));
			Console.WriteLine(Text.Wrap("       | Created 2007-2013 @wasatchwizard", width, 0, 12));
			Console.WriteLine(Text.Wrap("       | https://www.github.com/kodybrown/cldesc", width, 0, 12));
			Console.WriteLine();
			Console.WriteLine("USAGE:");
			Console.WriteLine(Text.Wrap("cldesc.exe [options] [file]", width, inda));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap(string.Format(format, "--vf", "Remove entries where the entry does not exist.*"), width, inda, indc));
			Console.WriteLine(Text.Wrap(string.Format(format, "--sf", "Save a backup copy of the `descript.ion` file.*"), width, inda, indc));
			Console.WriteLine(Text.Wrap(string.Format(format, "--oc", "Output to the console instead of to the source file. This only works when a <file> is specified."), width, inda, indc));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("* These items can be set as environment variables using:", width, inda, inda + inda));
			Console.WriteLine(Text.Wrap(conChar + " set cldescrip_<arg>=true|false", width, inda + 2, inda + 4));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap(string.Format(format, "[file]", "Specify the file to clean up. If a file is not specified, assumes `descript.ion` in the current directory."), width, inda, indc));
			Console.WriteLine(Text.Wrap("A file (or content) can be piped to `cldesc` via stdin. In such cases the output will always be sent to stdout.", width, indc));
			Console.WriteLine();

			Console.WriteLine("EXAMPLES:");
			Console.WriteLine(Text.Wrap("Sort the `descript.ion` file in the current directory, updating the file's contents.", width, inda));
			Console.WriteLine(Text.Wrap(conChar + " cldesc.exe", width, inda, inda + 2));
			Console.WriteLine();
			Console.WriteLine(Text.Wrap("Sort `file.txt` and output the results to the console.", width, inda));
			Console.WriteLine(Text.Wrap(conChar + " type file.txt |cldesc", width, inda, inda + 2));

		}
	}

	public static class StringExtensions
	{
		public static string Repeat( this string Value, int Count )
		{
			StringBuilder result;

			if (Value == null) {
				throw new ArgumentNullException("Value");
			}

			if (Count > 0) {
				result = new StringBuilder();

				for (int i = 0; i < Count; i++) {
					result.Append(Value);
				}

				return result.ToString();
			}

			return string.Empty;
		}
		public static string Repeat( this Char Value, int Count ) { return Repeat(Value.ToString(), Count); }
		public static string PadLeft( this string me, int Length, char PaddingChar )
		{
			if (me == null) {
				throw new ArgumentNullException("me");
			}

			if (Length <= me.Length) {
				return me;
			}

			return PaddingChar.Repeat(Length - me.Length) + me;
		}
		public static string PadLeft( this int me, int Length, char PaddingChar )
		{
			if (Length <= me.ToString().Length) {
				return me.ToString();
			}

			return PaddingChar.Repeat(Length - me.ToString().Length) + me;
		}
	}
}
