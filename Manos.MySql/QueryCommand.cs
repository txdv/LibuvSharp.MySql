using System;
namespace Manos.MySql
{
	public class QueryCommand
	{
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
		
		public QueryCommand On(Action<Row> row = null, Action<dynamic> drow = null, Action end = null)
		{
			if (row != null) {
				RowEvent += row;
			}
			
			if (drow != null) {
				DynamicRowEvent += drow;
			}
			
			if (end != null) {
				EndEvent += end;
			}
			
			return this;
		}
		
		public event Action<Row> RowEvent;
		public event Action<dynamic> DynamicRowEvent;
		public event Action EndEvent;
	}
}

