

using System;

namespace Connection.HTTP {
	/**
		HTTP header structure.
	*/
	public struct HTTPHeader {
		public string app_version;
		public string asset_version;

		public HTTPHeader (string app_version, string asset_version) {
			this.app_version = app_version;
			this.asset_version = asset_version;
		}
	}

	/**
		んー、起動時になんか必須のハンドラを渡すとかするか。エラー時にエラー内容を判別するのはAutoya側にしてしまえば、通信に専念できる。
		あとAssetBundleの通信もできる、、んだよな確か。
	*/
	public class HTTPConnection {
		private HTTPHeader baseHeader;

		public HTTPConnection (HTTPHeader baseHeader) {
			this.baseHeader = baseHeader;	
		}

		public void ResetHTTPHeader (HTTPHeader newBaseHeader) {
			this.baseHeader = newBaseHeader;
		}

		public string Get (HTTPHeader additionalHeader, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "additionalHeader + dummyConnectionId";
		}

		public string Get (string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "dummyConnectionId";
		}

		public string Post (HTTPHeader additionalHeader, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "additionalHeader + dummyConnectionId";
		}

		public string Post (string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			return "dummyConnectionId";
		}
	}

}