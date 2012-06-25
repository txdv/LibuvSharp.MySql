using System;
using System.Text;
using System.IO;

namespace LibuvSharp.MySql
{
	public class PacketBuilder
	{
		public Encoding Encoding { get; set; }
		
		MemoryStream ms = null;
		BinaryWriter bw = null;
		
		public PacketBuilder()
		{
			Encoding = Encoding.ASCII;
		}
		
		public void NewPacket()
		{
			if (bw != null) {
				bw.Dispose();
				bw.Close();
			}
			
			ms = new MemoryStream();
			bw = new BinaryWriter(ms);
			
			// this is the placeholder for the header
			bw.Write(0);
		}
		
		public byte[] Serialize(byte packetNumber)
		{
			byte[] packet = ms.ToArray();
			
			SetHeader(packet, packet.Length - sizeof(int), packetNumber); 
			
			return packet;
		}
		
		public void WriteInt(int val)
		{
			bw.Write(val);
		}
		
		public void WriteShort(short val)
		{
			bw.Write(val);
		}
		
		public void WriteByte(byte b)
		{
			ms.WriteByte(b);
		}
		
		public void Write(byte[] bytes)
		{
			ms.Write(bytes, 0, bytes.Length);
		}
		
		public void Write(byte[] bytes, int offset, int count)
		{
			ms.Write(bytes, offset, count);
		}
		
		public void WriteLong(long v, int numBytes)
		{
			long val = v;
			
			for (int x = 0; x < numBytes; x++) {
				WriteByte((byte)(val & 0xFF));
				val >>= 8;
			}
		}
		
		public void WriteLength(long length)
		{
			if (length < 251) {
				WriteByte((byte)length);
			} else if (length < 65536L) {
				WriteByte(252);
				WriteLong(length, 2);
			} else if (length < 16777216L) {
				WriteByte(253);
				WriteLong(length, 3);
			} else {
				WriteByte(254);
				WriteLong(length, 4);
			}
		}
		
		public void WriteLengthString(string str)
		{
			byte[] bytes = Encoding.GetBytes(str);
			WriteLength(bytes.Length);
			Write(bytes);
		}
		
		public void WriteStringNoNull(string str)
		{
			Write(Encoding.GetBytes(str));
		}
		
		public void WriteString(string str)
		{
			WriteStringNoNull(str);
			WriteByte(0);
		}
		
		public void WriteLengthCodedBinary(byte[] arr)
		{
			WriteLength(arr.Length);
			Write(arr);
		}
		
		void SetHeader(byte[] packet, int length, byte packetNumber)
		{
			packet[0] = (byte)(length & 0xFF);
			packet[1] = (byte)((length >> 8) & 0xFF);
			packet[1] = (byte)((length >> 16) & 0xFF);
			packet[3] = packetNumber;
		}
	}
}

