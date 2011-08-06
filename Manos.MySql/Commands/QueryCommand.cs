using System;
namespace Manos.MySql
{
	public class QueryCommand
	{
		internal QueryCommand()
		{
		}

		internal void FireResponse(ResponsePacket response)
		{
			if (ResponseEvent != null) {
				ResponseEvent(response);
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

		internal void FireError(Exception exception)
		{
			if (ErrorEvent != null) {
				ErrorEvent(exception);
			}
		}

		public QueryCommand OnResponse(Action<ResponsePacket> response)
		{
			if (response != null) {
				ResponseEvent += response;
			}
			return this;
		}

		public QueryCommand OnRow(Action<Row> onRow)
		{
			if (onRow != null) {
				RowEvent += onRow;
			}
			return this;
		}

		public QueryCommand OnDynamicRow(Action<dynamic> onDynamicRow)
		{
			if (onDynamicRow != null) {
				DynamicRowEvent += onDynamicRow;
			}
			return this;
		}

		public QueryCommand OnEnd(Action onEnd)
		{
			if (onEnd != null) {
				EndEvent += onEnd;
			}
			return this;
		}

		public QueryCommand OnError(Action<Exception> error)
		{
			if (error != null) {
				ErrorEvent += error;
			}
			return this;
		}

		public QueryCommand On(Action<ResponsePacket> response = null, Action<Row> row = null, Action<dynamic> drow = null, Action end = null, Action<Exception> error = null)
		{
			OnResponse(response);
			OnDynamicRow(drow);
			OnRow(row);
			OnEnd(end);
			OnError(error);

			return this;
		}

		public QueryCommand Clear()
		{
			ResponseEvent = null;
			RowEvent = null;
			DynamicRowEvent = null;
			EndEvent = null;
			ErrorEvent = null;

			return this;
		}

		public event Action<ResponsePacket> ResponseEvent;
		public event Action<Row> RowEvent;
		public event Action<dynamic> DynamicRowEvent;
		public event Action EndEvent;
		public event Action<Exception> ErrorEvent;
	}
}

