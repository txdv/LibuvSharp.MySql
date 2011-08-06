using System;
using System.Text;
using System.Collections.Generic;

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
		public byte[] Packet { get; set; }
		public QueryCommand Callback { get; set; }
	}

	public class MySqlClient
	{
		enum ConnectionState {
			WaitForServerGreet,
			WaitForLoginResponse,
			ParsePackets
		}

		public Context Context { get; private set; }

		private ITcpSocket Socket { get; set; }
		private IByteStream Stream { get; set; }
		private ConnectionState State { get; set; }

		private ByteBuffers buffers = new ByteBuffers();
		private PacketReader packetReader = new PacketReader();
		private PacketBuilder packetBuilder = new PacketBuilder();

		public string     Username   { get; set; }
		public string     Password   { get; set; }
		public IPEndPoint IPEndPoint { get; set; }
		public string     Database   { get; set; }
		public string     Table      { get; set; }

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

		private ConnectionCommand ConnectionCommand { get; set; }

		public IPAddress IPAddress {
			get {
				return IPEndPoint.Address;
			}
		}

		public int Port {
			get {
				return IPEndPoint.Port;
			}
		}

		public MySqlClient(Context context)
		{
			Context = context;
			Encoding = Encoding.Default;
		}

		public MySqlClient(Context context, IPEndPoint endpoint, string username, string password)
			: this(context)
		{
			IPEndPoint = endpoint;
			Username = username;
			Password = password;

		}

		public ConnectionCommand Connect()
		{
			ConnectionCommand = new ConnectionCommand();

			State = ConnectionState.WaitForServerGreet;
			Socket = Context.CreateTcpSocket(IPAddress.AddressFamily);
			Socket.Connect(IPEndPoint, delegate {
				Stream = Socket.GetSocketStream();
				Stream.Read(delegate (ByteBuffer buffer) {
					buffers.Add(buffer);
					byte[] packet;
					byte packetNumber;

					switch (State) {
					case ConnectionState.ParsePackets:
						if (!HandleWorker()) {
							return;
						}
						if (ReadPacket(out packetNumber, out packet)) {
							Worker = ProcessRequest(packetNumber, packet).GetEnumerator();
							Worker.MoveNext();
						}
						break;
					default:
						if (ReadPacket(out packetNumber, out packet)) {
							ConnectionRead(packetNumber, packet);
						}
						break;
					}
				}, delegate (Exception exception) {
				}, delegate {

				});
			}, delegate (Exception exception) {

			});

			return ConnectionCommand;
		}

		public void Disconnect()
		{
			if (Socket.IsConnected) {
				Socket.Close();
				Socket.Dispose();
			}
		}

		private void ConnectionRead(byte packetNumber, byte[] packet)
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
					User = Username,
				};

				packetBuilder.NewPacket();
				Stream.Write(response.Serialize(packetBuilder));
				State = ConnectionState.WaitForLoginResponse;
				break;
			case ConnectionState.WaitForLoginResponse:
				var res = ResponsePacket.Parse(packetReader);
				if (res is OkPacket) {
					ConnectionCommand.FireSuccess(res as OkPacket);
					State = ConnectionState.ParsePackets;
					FireFirstCommand();
				} else if (res is Error) {
					ConnectionCommand.FireError(res as Error);
				}
				break;
			}
		}

		#region Query Processing

		IEnumerable<bool> ProcessRequest(byte packetNumber, byte[] packet)
		{
			var queryCommand = commands.Peek().Callback;

			packetReader.NewPacket(packet);

			var type = ResponsePacket.GetType(packet);

			if (type != ResponsePacketType.Other) {
				var responsePacket = ResponsePacket.Parse(packetReader);

				queryCommand.FireResponse(responsePacket);
				FireNextCommand();
				yield return true;
				yield break;
			}

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
					queryCommand.FireEnd();
					FireNextCommand();
					yield return true;
					yield break;
				}

				packetReader.NewPacket(data);
				string[] values = new string[length];
				for (int i = 0; i < length; i++) {
					values[i] = packetReader.ReadLengthString();
				}

				queryCommand.FireRow(new Row(f, values));
			}
		}

		public QueryCommand Query(string queryString)
		{
			packetBuilder.NewPacket();
			packetBuilder.WriteByte((byte)DatabaseCommand.QUERY);
			packetBuilder.WriteStringNoNull(queryString);

			var info = new CommandInfo() {
				Callback = new QueryCommand(),
				Packet = packetBuilder.Serialize(0)
			};

			FireFirstCommand(info);

			return info.Callback;
		}

		private Queue<CommandInfo> commands = new Queue<CommandInfo>();

		private void FireNextCommand()
		{
			commands.Dequeue();
			if (commands.Count > 0) {
				Stream.Write(commands.Peek().Packet);
			}
		}

		bool FireFirstCommand()
		{
			if (commands.Count <= 0) {
				return false;
			}

			if (State == ConnectionState.ParsePackets) {
				Stream.Write(commands.Peek().Packet);
				return true;
			} else {
				return false;
			}
		}

		bool FireFirstCommand(CommandInfo info)
		{
			if (info == null) {
				return false;
			}

			commands.Enqueue(info);

			if (commands.Count == 1) {
				if (State == ConnectionState.ParsePackets) {
					Stream.Write(info.Packet);
				}
				return true;
			}
			return false;
		}

		private IEnumerator<bool> Worker { get; set; }

		private bool HandleWorker()
		{
			if (Worker != null) {
				Worker.MoveNext();
				if (Worker.Current) {
					Worker = null;
					return true;
				} else {
					return false;
				}
			} else {
				return true;
			}
		}

		#endregion

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
	}
}

