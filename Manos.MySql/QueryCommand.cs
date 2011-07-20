using System;
namespace Manos.MySql
{
	public class QueryCommand
	{
		internal void FireResponse(ResponsePacket response)
		{
			if (ResponseEvent != null) {
				ResponseEvent(response);
			}
			
		}
		
		internal void FireError(Exception exception)
		{
			if (ErrorEvent != null) {
				ErrorEvent(exception);
			}
		}
		
		internal void FireRow(Row row)
		{
			if (RowEvent != null) {
				RowEvent(row);
			}
			
			if (DynamicRowEvent != null) {
				DynamicRowEvent(row as dynamic);
			}
		}
		
		internal void FireEnd()
		{
			if (EndEvent != null) {
				EndEvent();
			}
		}
		
		public QueryCommand OnRow(Action<Row> onRow)
		{
			RowEvent += onRow;
			return this;
		}
		
		public QueryCommand OnEnd(Action onEnd)
		{
			EndEvent += onEnd;
			return this;
		}
		
		public QueryCommand On(Action<ResponsePacket> response = null, Action<Row> row = null, Action<dynamic> drow = null, Action end = null, Action<Exception> error = null)
		{
			if (response != null) {
				ResponseEvent += response;
			}
			
			if (row != null) {
				RowEvent += row;
			}
			
			if (drow != null) {
				DynamicRowEvent += drow;
			}
			
			if (end != null) {
				EndEvent += end;
			}
			
			if (error != null) {
				ErrorEvent += error;
			}
			
			return this;
		}
		
		public QueryCommand Clear()
		{
			RowEvent = null;
			DynamicRowEvent = null;
			EndEvent = null;
			ErrorEvent = null;
			ResponseEvent = null;
			
			return this;
		}
		
		public event Action<ResponsePacket> ResponseEvent;
		public event Action<Row> RowEvent;
		public event Action<dynamic> DynamicRowEvent;
		public event Action EndEvent;
		public event Action<Exception> ErrorEvent;
	}
}

