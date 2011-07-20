using System;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;
using Manos.IO;

namespace Manos.MySql
{
	class Fields
	{
		internal FieldPacket[] FieldPackets { get; set; }
		
		Dictionary<string, int> dict = new Dictionary<string, int>();
		
		public bool TryGetFieldByName(string name, out int index)
		{
			return dict.TryGetValue(name, out index);
		}
		
		public Fields(FieldPacket[] fieldPackets)
		{
			FieldPackets = fieldPackets;
			for (int i = 0; i < FieldPackets.Length; i++) {
				dict[FieldPackets[i].Name] = i;
			}
		}
	}
	
	class CommandInfo
	{
		public byte[] SerializedCommand { get; set; }
		public Action<ResponsePacket> Callback { get; set; }
	}
	
	public class MySqlClient
	{
		enum ConnectionState {
			WaitForServerGreet,
			WaitForLoginResponse,
			WaitForResponse,
			Unknown
		}
		
		Queue<CommandInfo> commands = new Queue<CommandInfo>();
		
		ByteBuffers buffers = new ByteBuffers();
		PacketBuilder packetBuilder = new PacketBuilder();
		PacketReader packetReader = new PacketReader();
		
		string User { get; set; }
		string Password { get; set; }
		
		IEnumerator<bool> ResponseHandler { get; set; }
		
		public bool HandleResponse()
		{
			if (ResponseHandler != null) {
				ResponseHandler.MoveNext();
				if (ResponseHandler.Current) {
					ResponseHandler = null;
					commands.Dequeue().Callback(null);
					State = ConnectionState.Unknown;
					return true;
				}
			}
			return false;
		}
		
		public Socket Socket { get; private set; }
		public Stream Stream { get; private set; }
		
		Encoding encoding;
		public Encoding Encoding {
			set {
				packetBuilder.Encoding = value;
				packetReader.Encoding = value;
				encoding = value;
			}
			get {
				return encoding;
			}
		}
		
		ConnectionState State { get; set; }
		
		Action<Exception> ConnectionCallBack { get; set; }
		
		QueryCommand QueryCommand { get; set; }
		
		public MySqlClient(Socket socket)
		{
			Socket = socket;
			Encoding = Encoding.ASCII;
		}
		
		public void Connect(string host, int port, string user, string password, Action<Exception> callback)
		{
			User = user;
			Password = password;
			
			ConnectionCallBack = callback;
			
			Socket.Connect(host, port, delegate {
				Stream = Socket.GetSocketStream();
				Stream.Read(delegate (ByteBuffer buffer) {
					byte[] data = new byte[buffer.Length];
					Buffer.BlockCopy(buffer.Bytes, buffer.Position, data, 0, data.Length);
					
					buffers.Add(new ByteBuffer(data, buffer.Position, buffer.Length));
					
					if (HandleResponse()) {
						return;
					}
					byte[] packet;
					byte packetNumber;
					if (ReadPacket(out packetNumber, out packet)) {
						Process(packetNumber, packet);
					}
				}, delegate (Exception exception) {
					
				}, delegate {
					
				});
			});
			
		}
		
		public QueryCommand Query(string queryString, Action<ResponsePacket> callback)
		{
			State = ConnectionState.WaitForResponse;
			Execute(DatabaseCommand.QUERY, queryString, callback);
			return null;
		}
		
		public QueryCommand Query(string queryString)
		{
			State = ConnectionState.WaitForResponse;
			Execute(DatabaseCommand.QUERY, queryString, (res) => { });
			QueryCommand = new QueryCommand();
			return QueryCommand;
		}
		
		void Execute(DatabaseCommand cmd, string commandString, Action<ResponsePacket> callback)
		{
			packetBuilder.NewPacket();
			packetBuilder.WriteByte((byte)cmd);
			packetBuilder.WriteStringNoNull(commandString);
			
			commands.Enqueue(new CommandInfo() {
				SerializedCommand = packetBuilder.Serialize(0),
				Callback = callback
			});
			
			if (commands.Count == 1) {
				Stream.Write(commands.Peek().SerializedCommand);
			}
		}
		
		IEnumerable<bool> WorkRequest(PacketReader packetReader)
		{
			long length = packetReader.ReadLength();
			
			byte[] data;
			byte num;
			
			FieldPacket[] fields = new FieldPacket[length];
			
			for (int i = 0; i < length; i++) {
				while (!ReadPacket(out num, out data)) {
					yield return false;
				}
				
				packetReader.NewPacket(data);
				var field = FieldPacket.Parse(packetReader);
				fields[i] = field;
			}
			
			var f = new Fields(fields);
			
			while (!ReadPacket(out num, out data)) {
				yield return false;	
			}
			
			packetReader.NewPacket(data);
			// TODO: check if the packet is in place
			EOFPacket.Parse(packetReader);
			
			while (true) {
				while (!ReadPacket(out num, out data)) {
					yield return false;	
				}
				
				if (ResponsePacket.GetType(data) == ResponsePacketType.EOF) {
					QueryCommand.FireEnd();
					yield return true;
					yield break;
				}
			
				packetReader.NewPacket(data);
				string[] values = new string[length];
				for (int i = 0; i < length; i++) {
					
					values[i] = packetReader.ReadLengthString();
				}
				
				QueryCommand.FireRow(new Row(f, values));
			}
		}
		
		public void Process(byte packetNumber, byte[] packet)
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
					User = User,
				};
				
				packetBuilder.NewPacket();
				Stream.Write(response.Serialize(packetBuilder));
				State = ConnectionState.WaitForLoginResponse;
				break;
			case ConnectionState.WaitForLoginResponse:
				var res = ResponsePacket.Parse(packetReader);
				
				if (res is OkPacket) {
					State = ConnectionState.Unknown;
					ConnectionCallBack(null);
				} else if (res is Error) {
					throw new Exception((res as Error).Message);
				}
				break;
			case ConnectionState.WaitForResponse:
				
				var type = ResponsePacket.GetType(packet);
				
				if (type != ResponsePacketType.Other) {
					var responsePacket = ResponsePacket.Parse(packetReader);
					commands.Dequeue().Callback(responsePacket);
					return;
				}
				
				ResponseHandler = WorkRequest(packetReader).GetEnumerator();
				
				HandleResponse();
				
				break;
			case ConnectionState.Unknown:
				throw new Exception("Got into the Uknown connection state");
			default:
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
		
		public void Close()
		{
			Execute(DatabaseCommand.QUIT, string.Empty, delegate (ResponsePacket packet) {
				if (packet is OkPacket) {
					Stream.Close();
				}
			});
		}
	}
}
