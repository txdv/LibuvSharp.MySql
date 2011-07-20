using System;
using System.Text;

using Manos.IO;

namespace Manos.MySql
{
	public class MySqlConnectionInfo
	{
		public string User     { get; set; }
		public string Password { get; set; }
		public string Host     { get; set; }
		public int    Port     { get; set; }
		public string Database { get; set; }
		public Encoding Encoding { get; set; }
	}
	
	public class MySqlClient
	{
		public Context Context { get; private set; }
		
		public MySqlClient(Context context)
		{
			Context = context;
		}
		
		public void Connect(MySqlConnectionInfo info, Action<Exception, MySqlConnection> callback)
		{
			new MySqlConnector(Context.CreateSocket(), info, callback);
		}
	}
	
	class MySqlConnector
	{
		enum ConnectionState {
			WaitForServerGreet,
			WaitForLoginResponse,
		}
		
		MySqlConnectionInfo Info { get; set; }		 
		Socket Socket { get; set; }
		Stream Stream { get; set; }
		
		ConnectionState State { get; set; }
		ByteBuffers buffers = new ByteBuffers();
		PacketBuilder packetBuilder = new PacketBuilder();
		PacketReader packetReader = new PacketReader();
		Action<Exception, MySqlConnection> ConnectionCallBack { get; set; }

		
		public MySqlConnector(Socket socket, MySqlConnectionInfo info, Action<Exception, MySqlConnection> callback)
		{
			Socket = socket;
			Info = info;
			ConnectionCallBack = callback;
			
			packetReader.Encoding = Encoding.Default;
			packetBuilder.Encoding = Encoding.Default;
			
			// initialize state of connection
			State = ConnectionState.WaitForServerGreet;
			
			Socket.Connect(info.Host, info.Port, delegate {
				Stream = socket.GetSocketStream();
				
				Stream.Read(delegate (ByteBuffer data) {
					buffers.AddCopy(data);
					byte[] packet;
					byte packetNumber;
					if (ReadPacket(out packetNumber, out packet)) {
						Process(packetNumber, packet);
					}
				}, delegate (Exception exception) {
					ConnectionCallBack(exception, null);
				}, delegate {
					ConnectionCallBack(new Exception("Destination closed during connection"), null);
				});
			});
		}
		
		void Process(byte packetNumber, byte[] packet)
		{
			packetReader.NewPacket(packet);
			switch (State) {
			case ConnectionState.WaitForServerGreet:
				var connPacket = MySqlConnectionPacket.Parse(packetReader);
				
				bool version = connPacket.ServerVersion.IsAtLeast(4, 1, 1);
				
				if (!version) {
				}
				
				var response = new ClientAuthResponse() {
					ClientFlags = 0xA685,
					MaxPacketSize = 256*256,
					CharsetNumber = connPacket.ServerLanguage,
					User = Info.User,
				};
				
				packetBuilder.NewPacket();
				Stream.Write(response.Serialize(packetBuilder));
				State = ConnectionState.WaitForLoginResponse;
				break;
			case ConnectionState.WaitForLoginResponse:

				var res = ResponsePacket.Parse(packetReader);
				if (res is OkPacket) {
					//State = ConnectionState.Unknown;
					ConnectionCallBack(null, new MySqlConnection(Socket, buffers));
				} else if (res is Error) {
					throw new Exception((res as Error).Message);
				}
				break;
			}
		}
		
		bool ReadPacket(out byte packetNumber, out byte[] packet)
		{
			long length;
			if (buffers.ReadLong(3, out length)) {
				if (buffers.HasLength((int)length + 3)) {
					packet = new byte[length];
					buffers.Skip(3);
					packetNumber = buffers.CurrentByte;
					buffers.Skip(1);
					buffers.CopyTo(packet, (int)length);
					buffers.Skip((int)length);
					return true;
				}
			}
			packetNumber = 0;
			packet = null;
			return false;
		}
	}}

