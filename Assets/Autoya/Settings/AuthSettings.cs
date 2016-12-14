namespace AutoyaFramework.Settings.Auth {
    public class AuthSettings {
        /*
            urls
        */
        public const string AUTH_URL_BOOT = "https://httpbin.org/get";
		public const string AUTH_URL_LOGIN = "https://httpbin.org/get";
		
        public const string AUTH_URL_REFRESH_TOKEN = "https://httpbin.org/get";
		
        public const string SAFE_HOST = "mySafeHost";
        public static readonly byte[] AUTH_BOOT = new byte[]{100, 101, 102};
        
    }
}