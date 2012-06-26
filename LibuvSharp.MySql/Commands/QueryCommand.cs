using System;

namespace LibuvSharp.MySql
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
			if (response is Error) {
				FireError(response as Error);
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

		internal void FireException(Exception exception)
		{
			if (ExceptionEvent != null) {
				ExceptionEvent(exception);
			}
		}

		internal void FireError(Error error)
		{
			if (ErrorEvent != null) {
				ErrorEvent(error);
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

		public QueryCommand OnException(Action<Exception> exception)
		{
			if (exception != null) {
				ExceptionEvent += exception;
			}
			return this;
		}

		public QueryCommand OnError(Action<Error> error)
		{
			if (error != null) {
				ErrorEvent += error;
			}
			return this;
		}

		public QueryCommand On(Action<ResponsePacket> response = null, Action<Row> row = null, Action<dynamic> drow = null, Action end = null, Action<Exception> exception = null, Action<Error> error = null)
		{
			OnResponse(response);
			OnDynamicRow(drow);
			OnRow(row);
			OnEnd(end);
			OnException(exception);
			OnError(error);

			return this;
		}

		public QueryCommand Clear()
		{
			ResponseEvent = null;
			RowEvent = null;
			DynamicRowEvent = null;
			EndEvent = null;
			ExceptionEvent = null;

			return this;
		}

		public event Action<ResponsePacket> ResponseEvent;
		public event Action<Row> RowEvent;
		public event Action<dynamic> DynamicRowEvent;
		public event Action EndEvent;
		public event Action<Exception> ExceptionEvent;
		public event Action<Error> ErrorEvent;
	}
}

