using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

// Original source:
// http://stackoverflow.com/questions/3453220/how-to-detect-if-console-in-stdin-has-been-redirected/3453272#3453272
public class StdInEx
{
	private static List<DeleteFileWhenDone> _deleteFiles = new List<DeleteFileWhenDone>();

	public static bool IsOutputRedirected
	{
		get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdout)); }
	}

	public static bool IsInputRedirected
	{
		get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stdin)); }
	}

	public static bool IsErrorRedirected
	{
		get { return FileType.Char != GetFileType(GetStdHandle(StdHandle.Stderr)); }
	}

	public static string RedirectInputToFile()
	{
		string f = Path.GetTempFileName();
		File.WriteAllText(f, Console.In.ReadToEnd());
		_deleteFiles.Add(new DeleteFileWhenDone(f));
		return f;
	}

	// P/Invoke:
	private enum FileType { Unknown, Disk, Char, Pipe };
	private enum StdHandle { Stdin = -10, Stdout = -11, Stderr = -12 };

	[DllImport("kernel32.dll")]
	private static extern FileType GetFileType( IntPtr hdl );

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetStdHandle( StdHandle std );

	private class DeleteFileWhenDone
	{
		public string _file { get; private set; }

		public DeleteFileWhenDone( string file )
		{
			_file = file;
		}

		~DeleteFileWhenDone()
		{
			if (File.Exists(_file)) {
				File.SetAttributes(_file, FileAttributes.Normal);
				File.Delete(_file);
			}
		}
	}
}
