using System;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace LibuvSharp.MySql
{
	public class MySqlConnectionPacket
	{
		public byte   ProtocolVersion    { get; set; }
		public DatabaseVersion ServerVersion { get; set; }
		public int    ThreadId           { get; set; }
		public byte[] ScrambleBuffer     { get; set; }
		public byte   Filler             { get; set; }
		public short  ServerCapabilities { get; set; }
		public byte   ServerLanguage     { get; set; }
		public short  ServerStatus       { get; set; }
		public short  ServerCapabilitiesUpper { get; set; }
		public byte   ScrambleLength     { get; set; }
		public byte[] SecondFiller       { get; set; }
		public byte[] SecondScramble     { get; set; }
		
		public static MySqlConnectionPacket Parse(PacketReader packetReader)
		{
			MySqlConnectionPacket packet = new MySqlConnectionPacket();
			packet.ProtocolVersion    = packetReader.ReadByte();
			packet.ServerVersion      = DatabaseVersion.Parse(packetReader.ReadString(Encoding.ASCII));
			packet.ThreadId           = packetReader.ReadInt();
			packet.ScrambleBuffer     = packetReader.ReadBytes(sizeof(long));
			packet.Filler             = packetReader.ReadByte();
			packet.ServerCapabilities = packetReader.ReadShort();
			packet.ServerLanguage     = packetReader.ReadByte();
			packet.ServerStatus       = packetReader.ReadShort();
			packet.ServerCapabilitiesUpper = packetReader.ReadShort();
			packet.ScrambleLength     = packetReader.ReadByte();
			packet.SecondFiller       = packetReader.ReadBytes(10);
			
			packetReader.ReadByte();
			
			packet.SecondScramble  = packetReader.ReadBytes(packetReader.Length - packetReader.Position - 1);
			
			return packet;
		}
		
		public byte[] Get411Password(string password)
		{
			var sha = SHA1.Create();
			sha.ComputeHash(Encoding.ASCII.GetBytes(password));
			
			byte[] firstHash = sha.Hash;
			byte[] secondHash = sha.ComputeHash(firstHash);
			
			byte[] seedBytes = new byte[20];
			Buffer.BlockCopy(ScrambleBuffer, 0, seedBytes, 0, ScrambleBuffer.Length);
			Buffer.BlockCopy(SecondScramble, 0, seedBytes, ScrambleBuffer.Length, SecondScramble.Length);
			
			byte[] input = new byte[seedBytes.Length + secondHash.Length];
			Buffer.BlockCopy(seedBytes, 0, input, 0, seedBytes.Length);
			Buffer.BlockCopy(secondHash, 0, input, seedBytes.Length, secondHash.Length);
			
			byte[] thirdHash = sha.ComputeHash(input);
			
			byte[] finalHash = new byte[thirdHash.Length + 1];
			finalHash[0] = 0x14;
			
			for (int i = 1; i < finalHash.Length; i++) {
				finalHash[i] = (byte) (finalHash[i] ^ firstHash[i - 1]);
			}
			
			return finalHash;
			
		}
		
		public byte[] CalculatePassword(string password)
		{
			SHA1 sha = SHA1.Create();
			var stage1 = sha.ComputeHash(Encoding.ASCII.GetBytes(password));
			
			byte[] newBuffer = new byte[ScrambleBuffer.Length + stage1.Length];
			Buffer.BlockCopy(ScrambleBuffer, 0, newBuffer, 0, ScrambleBuffer.Length);
			Buffer.BlockCopy(stage1, 0, newBuffer, ScrambleBuffer.Length, stage1.Length);
			
			var token = sha.ComputeHash(newBuffer);
			
			for (int i = 0; i < token.Length; i++) {
				token[i] ^= stage1[i];
			}
			
			return token;
		}
	}
	
	public class ClientAuthResponse
	{
		public int ClientFlags { get; set; }
		public int MaxPacketSize { get; set; }
		public byte CharsetNumber { get; set; }
		byte[] Filler { get; set; }
		public string User { get; set; }
		public byte[] ScrambleBuffer { get; set; }
		public string Databasename { get; set; }
		
		public byte[] Serialize(PacketBuilder packetBuilder)
		{
			packetBuilder.NewPacket();
			
			packetBuilder.WriteInt(ClientFlags);
			packetBuilder.WriteInt(MaxPacketSize);
			packetBuilder.WriteByte(CharsetNumber);
			packetBuilder.Write(new byte[23]);
			packetBuilder.WriteString(User);
			
			if (ScrambleBuffer != null) {
				packetBuilder.WriteLengthCodedBinary(ScrambleBuffer);
			} else {
				packetBuilder.WriteByte(0);
			}
			
			if (!string.IsNullOrEmpty(Databasename)) {
				packetBuilder.WriteString(Databasename);
			}
			
			return packetBuilder.Serialize(1);
		}
	}
}
