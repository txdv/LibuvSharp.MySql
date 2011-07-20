using System;

namespace Manos.MySql
{
	public enum ResponsePacketType {
		OK,
		EOF,
		Error,
		Other
	}
	
	public class ResponsePacket
	{
		public byte FieldCount { get; set; }
		
		public static ResponsePacketType GetType(PacketReader packetReader)
		{
			return GetType(packetReader.CurrentByte);
		}
		
		public static ResponsePacketType GetType(byte[] data)
		{
			return GetType(data[0]);
		}
		
		public static ResponsePacketType GetType(byte b)
		{
			switch (b) {
			case 0:    return ResponsePacketType.OK;
			case 0xFE: return ResponsePacketType.EOF;
			case 0xFF: return ResponsePacketType.Error;
			default:   return ResponsePacketType.Other;
			}
		}
		
		public static ResponsePacket Parse(PacketReader packetReader)
		{
			switch (packetReader.CurrentByte) {
			case 0:	   return OkPacket.Parse(packetReader);
			case 0xFE: return EOFPacket.Parse(packetReader);
			case 0xFF: return Error.Parse(packetReader);
			}
			return null;
		}
	}
	
	public class EOFPacket : ResponsePacket
	{
		public short WarningCount { get; set; }
		public short ServerStatus { get; set; }
		public static new EOFPacket Parse(PacketReader packetReader)
		{
			return new EOFPacket() {
				FieldCount   = packetReader.ReadByte(),
				WarningCount = packetReader.ReadShort(),
				ServerStatus = packetReader.ReadShort()
			};
		}
	}
	
	public class Error : ResponsePacket
	{
		public short  ErrorNumber { get; set; }
		public byte   Marker      { get; set; }
		public string State       { get; set; }
		public string Message     { get; set; }
		
		public static new Error Parse(PacketReader packetReader)
		{
			return new Error() {
				FieldCount  = packetReader.ReadByte(),
				ErrorNumber = packetReader.ReadShort(),
				Marker      = packetReader.ReadByte(),
				State       = packetReader.ReadString(5),
				Message     = packetReader.ReadString(),
			};
		}
	}
	
	public class OkPacket : ResponsePacket
	{
		public long   AffectedRows { get; set; }
		public long   InsertId     { get; set; }
		public short  ServerStatus { get; set; }
		public short  WarningCount { get; set; }
		public string Message      { get; set; }
		
		public static new OkPacket Parse(PacketReader packetReader)
		{
			return new OkPacket() {
				FieldCount   = packetReader.ReadByte(),
				AffectedRows = packetReader.ReadLength(),
				InsertId     = packetReader.ReadLength(),
				ServerStatus = packetReader.ReadShort(),
				Message      = packetReader.ReadString(),
			};
		}
	}
	
    internal enum ColumnFlags : short
    {
        NOT_NULL = 1,
        PRIMARY_KEY = 2,
        UNIQUE_KEY = 4,
        MULTIPLE_KEY = 8,
        BLOB = 16,
        UNSIGNED = 32,
        ZERO_FILL = 64,
        BINARY = 128,
        ENUM = 256,
        AUTO_INCREMENT = 512,
        TIMESTAMP = 1024,
        SET = 2048,
        //NUMBER = 32768
    };
	
	class FieldPacket
	{
		public string Catalog       { get; set; }
		public string Database      { get; set; }
		public string Table         { get; set; }
		public string OriginalTable { get; set; }
		public string Name          { get; set; }
		public string OriginalName  { get; set; }
		
		byte Filler { get; set; }
		
		public short CharsetNumber { get; set; }
		public int   Length        { get; set; }
		public MySqlDbType Type    { get; set; }
		public ColumnFlags Flags   { get; set; }
		public byte  Decimals      { get; set; }
		
		byte[] Filler2 { get; set; }
		
		public byte[] Default { get; set; }
		
		public static FieldPacket Parse(PacketReader packetReader)
		{
			return new FieldPacket() {
				Catalog       = packetReader.ReadLengthString(),
				Database      = packetReader.ReadLengthString(),
				Table         = packetReader.ReadLengthString(),
				OriginalTable = packetReader.ReadLengthString(),
				Name          = packetReader.ReadLengthString(),
				OriginalName  = packetReader.ReadLengthString(),
				
				Filler = packetReader.ReadByte(),
				
				CharsetNumber = packetReader.ReadShort(),
				Length        = packetReader.ReadInt(),
				Type          = (MySqlDbType)packetReader.ReadByte(),
				Flags         = (ColumnFlags)packetReader.ReadShort(),
				Decimals      = packetReader.ReadByte(),
				
				Filler2 = packetReader.ReadBytes(2),
				Default = packetReader.ReadLengthBytes()
			};
		}
	}
}

