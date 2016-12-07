namespace AutoyaFramework.Settings.Auth {
    public class AuthSettings {
        public const string AUTH_URL_BOOT = "https://httpbin.org/get";
		public const string AUTH_URL_LOGIN = "https://httpbin.org/get";
		
        public const string AUTH_URL_REFRESH_TOKEN = "https://httpbin.org/get";
		
        public const string SAFE_HOST = "mySafeHost";
        public readonly byte[] AUTH_KEY = new byte[]{100, 101, 102};
    }
}