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
		public byte[] Packet { get; set; }
		public QueryCommand Callback { get; set; }
	}
	
	public class MySqlConnection
	{
		Socket Socket { get; set; }
		Stream Stream { get; set; }
		
		ByteBuffers buffers;
		PacketBuilder packetBuilder = new PacketBuilder();
		PacketReader packetReader = new PacketReader();
		
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
		
		IEnumerator<bool> Worker { get; set; }
		
		bool HandleWorker()
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
		
		Queue<CommandInfo> commands = new Queue<CommandInfo>();
		
		void FireNextCommand()
		{
			commands.Dequeue();
			if (commands.Count > 0) {
				Stream.Write(commands.Peek().Packet);
			}
		}
		
		bool FireFirstCommand(CommandInfo info)
		{
			commands.Enqueue(info);
			
			if (commands.Count == 1) {
				Stream.Write(info.Packet);
				return true;
			}
			return false;
		}
		
		public MySqlConnection(Socket socket, ByteBuffers buffers)
		{
			Socket = socket;
			Stream = socket.GetSocketStream();
			this.buffers = buffers;
			
			Encoding = Encoding.Default;
			
			Stream.Read(delegate (ByteBuffer data) {
				buffers.AddCopy(data);
				
				if (!HandleWorker()) {
					return;
				}
				byte[] packet;
				byte packetNumber;
				if (ReadPacket(out packetNumber, out packet)) {
					Worker = ProcessRequest(packetNumber, packet).GetEnumerator();
					Worker.MoveNext();
				}
			}, delegate (Exception exception) {
			}, delegate {
			});
			
		}
		
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
