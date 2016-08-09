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
		public const string AUTH_URL_TOKEN = "https://httpbin.org/get";// 書くとしたらここじゃねえなあ。
		public const string AUTH_CONNECTIONID_GETTOKEN_PREFIX = "token_";
		public const string AUTH_URL_LOGIN = "https://httpbin.org/get";// 書くとしたらここじゃねえなあ。
		public const string AUTH_CONNECTIONID_ATTEMPTLOGIN_PREFIX = "login_";

		public const string AUTH_STORED_TOKEN_DOMAIN = "token";
		public const string AUTH_STORED_TOKEN_FILENAME = "token.autoya";

		/*
			http settings.
		*/
		public const double HTTP_TIMEOUT_SEC = 5.0;
        public const string HTTP_401_MESSAGE = "unauthorized:";
        public const string HTTP_TIMEOUT_MESSAGE = "timeout:";
    }
}