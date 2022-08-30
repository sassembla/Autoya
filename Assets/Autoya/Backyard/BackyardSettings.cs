namespace AutoyaFramework
{
    public class BackyardSettings
    {
        /*
			maintenance settings.
		*/
        public const int MAINTENANCE_CODE = 599;

        /*
			http settings.
		*/
        public const double HTTP_TIMEOUT_SEC = 5.0;
        public const int HTTP_TIMEOUT_CODE = 408;

        public const string HTTP_CODE_ERROR_SUFFIX = "httpResponseCodeError:";
        public const string HTTP_TIMEOUT_MESSAGE = "timeout. sec:";

        // UnityWebRequest returns 200 when isNetworkError == true. set this value for valid handling.
        public const int HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE = -200;
    }
}