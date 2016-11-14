namespace AutoyaFramework {
    public class AutoyaConsts {
		/*
			keyword settings.
		*/
		public const string key_app_version = "app_version";
		public const string key_asset_version = "asset_version";

		/*
			auth settings.
		*/
		// boot.
		public const string AUTH_URL_BOOT = "https://httpbin.org/get";// 書くとしたらここじゃねえなあ。ユーザー用の設定ファイルが欲しい。
		public const string AUTH_CONNECTIONID_BOOT_PREFIX = "boot_";
		public readonly byte[] AUTH_KEY = new byte[]{100, 101, 102};
		
		// refresh.
		public const string AUTH_URL_REFRESH_TOKEN = "https://httpbin.org/get";// 書くとしたらここじゃねえなあ。ユーザー用の設定ファイルが欲しい。
		public const string AUTH_CONNECTIONID_REFRESH_TOKEN_PREFIX = "ref_token_";

		// login.
		public const string AUTH_URL_LOGIN = "https://httpbin.org/get";// 書くとしたらここじゃねえなあ。ユーザー用の設定ファイルが欲しい。
		public const string AUTH_CONNECTIONID_ATTEMPTLOGIN_PREFIX = "login_";

		public const string AUTH_STORED_FRAMEWORK_DOMAIN = "framework";
		public const string AUTH_STORED_TOKEN_FILENAME = "token.autoya";

		public const string AUTH_HTTP_INTERNALERROR_TYPE_TIMEOUT = "System.TimeoutException";
		public const int AUTH_HTTP_INTERNALERROR_CODE_TIMEOUT = -1;

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
        public const string HTTP_TIMEOUT_MESSAGE = "timeout:";
    }
}