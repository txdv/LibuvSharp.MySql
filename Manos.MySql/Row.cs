using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Manos.MySql
{
	public class Row : DynamicObject 
	{
		Fields Fields { get; set; }
		string[] Values { get; set; }

		public int Length { get; private set; }

		internal Row(Fields fields, string[] values)
		{
			Fields = fields;
			Values = values;
			
			Length = Fields.FieldPackets.Length;
		}

		public int GetIndex(string name)
		{
			int index;
			if (Fields.TryGetFieldByName(name, out index)) {
				return index;
			} else {
				return -1;
			}
		}

		public string GetRawValue(int index)
		{
			if (index < 0 || index > Values.Length) {
				throw new Exception("No such field");
			}
			return Values[index];
		}

		public object GetValue(int index)
		{
			if (index < 0 || index > Values.Length) {
				throw new Exception("No such field");
			}
			return Convert(Fields.FieldPackets[index], Values[index]);
		}

		public string GetRawValue(string fieldName)
		{
			int index = GetIndex(fieldName);
			if (index == -1) {
				throw new Exception("No such field");
			}
			return Values[index];
		}

		public object GetValue(string fieldName)
		{
			int index = GetIndex(fieldName);
			if (index == -1) {
				throw new Exception("No such field");
			}
			return Convert(Fields.FieldPackets[index], Values[index]);
		}

		public object this[string fieldName] {
			get {
				return GetValue(fieldName);
			}
		}

		public object this[int index] {
			get {
				return GetValue(index);
			}
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = this[binder.Name];
			return true;
		}

		object Convert(FieldPacket field, string val)
		{
			if (val == null) {
				return null;
			}

			if ((field.Flags & ColumnFlags.UNSIGNED) > 0) {
				switch (field.Type) {
				case MySqlDbType.Int16:
					return UInt16.Parse(val);
				case MySqlDbType.Int24:
					return UInt32.Parse(val);
				case MySqlDbType.Int32:
					return UInt32.Parse(val);
				case MySqlDbType.Int64:
					return UInt64.Parse(val);
				case MySqlDbType.Byte:
					return val == "1";
				default:
					throw new Exception(string.Format("(Unsigned) Not supported type {0}:{1}", field.Type, val));
				}
			} else {
				switch (field.Type) {
				case MySqlDbType.Int16:
					return Int16.Parse(val);
				case MySqlDbType.Int24:
					return Int32.Parse(val);
				case MySqlDbType.Int32:
					return Int32.Parse(val);
				case MySqlDbType.Int64:
					return Int64.Parse(val);
				case MySqlDbType.Byte:
					return Byte.Parse(val);
				case MySqlDbType.Float:
					return float.Parse(val);
				case MySqlDbType.Date:
					return DateTime.Parse(val);
				case MySqlDbType.DateTime:
					return DateTime.Parse(val);
				case MySqlDbType.String:
				case MySqlDbType.VarChar:
				case MySqlDbType.Blob:
					return val;
				default:
					throw new Exception(string.Format("Not supported type {0}:{1}", field.Type, val));
				}
			}
		}
	}
}

