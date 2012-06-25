using System;

namespace LibuvSharp.MySql
{
	public class ConnectionCommand
	{
		internal ConnectionCommand()
		{
		}

		internal void FireSuccess(OkPacket ok)
		{
			if (SuccessEvent != null) {
				SuccessEvent(ok);
			}
		}

		internal void FireError(Error error)
		{
			if (ErrorEvent != null) {
				ErrorEvent(error);
			}
		}

		public ConnectionCommand OnSuccess(Action<OkPacket> success)
		{
			if (success != null) {
				SuccessEvent += success;
			}
			return this;
		}

		public ConnectionCommand OnError(Action<Error> error)
		{
			if (error != null) {
				ErrorEvent += error;
			}
			return this;
		}

		public ConnectionCommand On(Action<OkPacket> success = null, Action<Error> error = null)
		{
			OnSuccess(success);
			OnError(error);

			return this;
		}

		public event Action<OkPacket> SuccessEvent;
		public event Action<Error> ErrorEvent;

		public ConnectionCommand Clear()
		{
			SuccessEvent = null;
			ErrorEvent = null;

			return this;
		}
	}
}

