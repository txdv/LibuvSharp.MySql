using System;

using Manos.IO;
using Manos.MySql;

namespace Manos.MySql.Test
{
	public static class MysqlExtensions
	{
		// Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		
		public static string ToDate(this DateTime dateTime)
		{
			return dateTime.ToString("yyyy-MM-dd");
		}
	}
	
	class MainClass
	{
		static MySqlConnectionInfo info = new MySqlConnectionInfo() {
			IPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3306),
			User     = "bentkus",
			Password = ""
		};
		
		public static void Query(MySqlConnection conn, string database, string table)
		{
			conn.Query(string.Format("SELECT * FROM {0}", table))
			.On(row: delegate (Row data) {
				for (int i = 0; i < data.Length; i++) {
					Console.Write(data.GetRawValue(i));
					Console.Write("\t");
				}
				Console.WriteLine();
			}).On(end: delegate {
				Console.WriteLine("End of data");
			});
		}
		
		public static void Main(string[] args)
		{
			var context = Context.Create();
			MySqlClient client = new MySqlClient(context);
			
			client.Connect(info, delegate (Exception exception, MySqlConnection conn) {
				conn.Query(string.Format("use {0}", args[0]));
				Query(conn, args[0], args[1]);
				Console.WriteLine("Start of data");
			});
			
			context.Start();
		}
	}
}

