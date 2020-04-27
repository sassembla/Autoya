namespace AutoyaFramework.Settings.Auth
{
    public class AuthSettings
    {
        /*
			authenticated http request header keys.
		 */
        public const string AUTH_REQUESTHEADER_APPVERSION = "appversion";
        public const string AUTH_REQUESTHEADER_RESVERSION = "resversion";

        /*
			authenticated http response header keys.
		 */
        public const string AUTH_RESPONSEHEADER_APPVERSION = "appversion";
        public const string AUTH_RESPONSEHEADER_RESVERSION = "resversion";

        /*
			auth urls and prefixies.
		*/
        public static string AUTH_URL_BOOT = "https://httpbin.org/post";
        public const string AUTH_CONNECTIONID_BOOT_PREFIX = "boot_";

        public static string AUTH_URL_REFRESH_TOKEN = "https://httpbin.org/post";
        public const string AUTH_CONNECTIONID_REFRESH_TOKEN_PREFIX = "ref_token_";

        /*
			auth authentication persist settings.
		*/
        public const string AUTH_STORED_FRAMEWORK_DOMAIN = "framework";
        public const string AUTH_STORED_TOKEN_FILENAME = "token.autoya";


        public const int AUTH_FIRSTBOOT_MAX_RETRY_COUNT = 3;
        public const int AUTH_TOKENREFRESH_MAX_RETRY_COUNT = 3;

        public const int AUTH_HTTP_CODE_UNAUTHORIZED = 401;
        public const int AUTOYA_HTTP_CODE_INTERNAL_UNAUTHORIZED = 10401;// Autoya's original internal response code. emit until authentication feature is ready.

        /*
			sample authentication key.
			TODO 起動時暗号化バイナリをOverridePointsとかに移した方がいいかも。
		*/
        public static readonly byte[] AUTH_BOOT = new byte[] { 100, 101, 102 };


        public const int FORCE_FAIL_FIRSTBOOT_CODE = 499;

    }
}
