namespace AutoyaFramework {
    public class BackyardSettings {
		/*
			maintenance settings.
		*/
		public const int MAINTENANCE_CODE = 599;
		
		/*
			http settings.
		*/
		public const double HTTP_TIMEOUT_SEC = 5.0;
		public const int HTTP_TIMEOUT_CODE = 408;
        public const string HTTP_401_MESSAGE = "unauthorized:";
        public const string HTTP_TIMEOUT_MESSAGE = "timeout. sec:";
    }

	/*
		http request header delegate.
	*/
	public enum HttpMethod {
		Get,
		Post,
		Put,
		Delete
	}

	public struct AutoyaStatus {
		public readonly bool inMaintenance;
		public readonly bool isAuthFailed;
		public AutoyaStatus (bool inMaintenance, bool isAuthFailed) {
			this.inMaintenance = inMaintenance;
			this.isAuthFailed = isAuthFailed;
		}
	}
}