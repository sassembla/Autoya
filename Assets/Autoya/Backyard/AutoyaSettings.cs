namespace AutoyaFramework {
    public class AutoyaConsts {
		public const string key_app_version = "app_version";
		public const string key_asset_version = "asset_version";

		public const string AUTH_CONNECTIONID_ATTEMPTIDENTIFY_PREFIX = "token_";
		public const string AUTH_URL_LOGIN = "https://httpbin.org/auth/a/b";// ここじゃねえなあ。
		public const string AUTH_CONNECTIONID_ATTEMPTLOGIN_PREFIX = "login_";

		public const double HTTP_TIMEOUT_SEC = 5.0;
        public const string HTTP_401_MESSAGE = "unauthorized:";
        public const string HTTP_TIMEOUT_MESSAGE = "timeout:";
    }
}