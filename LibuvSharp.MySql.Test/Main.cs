using System;
using System.Net;
using LibuvSharp;

namespace LibuvSharp.MySql.Test
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
		public static void Query(MySqlClient client, string database, string table)
		{
			client.Query(string.Format("SELECT * FROM {0}", table))
				.On(fields: (fields) => {
					foreach (var field in fields) {
						Console.Write("{0}({1})\t", field.Type, field.Length);
					}
					Console.WriteLine();
				})
				.On(row: (data) => {
					for (int i = 0; i < data.Length; i++) {
						Console.Write(data.GetRawValue(i));
						Console.Write("\t");
					}
					Console.WriteLine();
				}).On(end: () => {
					client.Disconnect();
				}).On(error: (error) => {
					Console.WriteLine("Error: {0}", error.Message);
					client.Disconnect();
				});
		}
		
		public static void Main(string[] args)
		{
			if (args.Length < 2) {
				Console.WriteLine("<database> <tablename>");
				return;
			}

			var loop = Loop.Default;

			MySqlClient client = new MySqlClient(loop);
			
			client.IPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3306);
			client.Username = "bentkus";
			client.Password = "";

			client.Connect()
				.OnError((error) => {
					Console.WriteLine(error.Message);
					client.Disconnect();
				});

			client.Query(string.Format("use {0}", args[0]));
			Query(client, args[0], args[1]);

			loop.Run();
		}
	}
}

