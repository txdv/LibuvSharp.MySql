using System;
using System.IO;
using System.Text;

namespace LibuvSharp.MySql
{
	public class PacketReader
	{
		public Encoding Encoding { get; set; }
		
		byte[] data;
		MemoryStream ms;
		BinaryReader br;
		
		public PacketReader()
		{
		}
		
		public bool EndOfData
		{
			get {
				return (ms.Position >= data.Length);
			}
		}
		
		public void NewPacket(byte[] data)
		{
			this.data = data;
			ms = new System.IO.MemoryStream(data);
			ms.Position = 0;
			br = new System.IO.BinaryReader(ms);
		}
		
		public string ReadString()
		{
			return ReadString(Encoding);
		}
		
		public int GetRestLength()
		{
			int pos = Position;
			int start = pos;
			while (pos < data.Length && data[pos] != 0) {
				pos++;
			}
			
			return pos - start;
		}
		
		public string ReadString(Encoding encoding)
		{
			return encoding.GetString(br.ReadBytes(GetRestLength()));
		}
		
		public int Read(byte[] buffer, int offset, int count)
		{
			return br.Read(buffer, offset, count);
		}
		
		public byte ReadByte()
		{
			return br.ReadByte();
		}
		
		public short ReadShort()
		{
			return br.ReadInt16();
		}
		
		public int ReadInt()
		{
			return br.ReadInt32();
		}
		
		public byte[] ReadLengthBytes()
		{
			int len = (int)ReadLength();
			if (len == -1) {
				return null;
			} else {
				return ReadBytes(len);
			}
		}
		
		public byte[] ReadBytes()
		{
			return ReadBytes(GetRestLength());
		}
		
		public byte[] ReadBytes(int size)
		{
			return br.ReadBytes(size);
		}
		
		public long ReadLength()
		{
			if (EndOfData) {
				return 0;
			}
			
			byte c = ReadByte();
			
			switch (c) {
			case 251: return -1;
			case 252: return ReadLong(2);
			case 253: return ReadLong(3);
			case 254: return ReadLong(8);
			default:  return c;
			}
		}
		
		public long ReadLong(int numBytes)
		{
			long val = 0;
			int shift = 0;
			for (int i = 0; i < numBytes; i++) {
				val |= (long)(ReadByte() << shift);
				shift += 8;
			}
			return val;
		}
		
		
		public string ReadString(int length)
		{
			return ReadString(length, Encoding);
		}
		
		public string ReadString(int length, Encoding encoding)
		{
			if (length == 0) {
				return string.Empty;
			}
			
			return encoding.GetString(ReadBytes(length));
		}
		
		public string ReadLengthString()
		{
			return ReadLengthString(Encoding);
		}
		
		public string ReadLengthString(Encoding encoding)
		{
			int len = (int)ReadLength();
			if (len == -1) {
				return null;
			} else {
				return ReadString(len, encoding);
			}
		}
		
		public int Length {
			get {
				return data.Length;
			}
		}
		
		public int Position {
			get {
				return (int)ms.Position;
			}
		}
		
		public byte CurrentByte {
			get {
				return data[ms.Position];
			}
		}
	}
}
