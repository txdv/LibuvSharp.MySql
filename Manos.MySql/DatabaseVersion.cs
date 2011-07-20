using System;

namespace Manos.MySql
{
	public class MySqlException
	{
		public MySqlException()
		{
		}
	}
	
	public class DatabaseVersion
	{
		public int Major { get; private set; }
		public int Minor { get; private set; }
		public int Build { get; private set; }
		public string Suffix { get; private set; }
		public string Source { get; private set; }
		
		public DatabaseVersion(string source, int major, int minor, int build, string suffix)
		{
			Source = source;
			
			Major = major;
			Minor = minor;
			Build = build;
			Suffix = suffix;
		}

		public static DatabaseVersion Parse(string versionString)
		{
			int start = 0;
			int index = versionString.IndexOf('.', start);
			if (index == -1) {
				throw new Exception();
				//throw new MySqlException(Resources.BadVersionFormat);
			}
				
			string val = versionString.Substring(start, index-start).Trim();
			int major = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);
			
			start = index + 1;
			index = versionString.IndexOf('.', start);
			if (index == -1) {
				throw new Exception();
				//throw new MySqlException(Resources.BadVersionFormat);
			}
				
			val = versionString.Substring(start, index-start).Trim();
			
			int minor = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);
			
			start = index + 1;
			int i = start;
			while (i < versionString.Length && Char.IsDigit(versionString, i)) {
				i++;
			}
			val = versionString.Substring(start, i-start).Trim();
			int build = Convert.ToInt32(val, System.Globalization.NumberFormatInfo.InvariantInfo);
			
			return new DatabaseVersion(versionString, major, minor, build, versionString.Substring(i));
		}
		
		public bool IsAtLeast(int major, int minor, int build)
		{
			return (Major > major) ||
			       (Major == major && Minor > minor) ||
			       (Major == major && Minor == minor && Build >= build);
		}
	}
}

