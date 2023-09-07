using UnityEngine;

/**
	Autoya framework entry point.
	This method will be called from Unity when the App starts running.
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {
        private static Autoya autoya;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EntryPoint()
        {
            autoya = new Autoya(Autoya.OnPersistentPathRequired());
        }


        /**
			this method can be called for only testing.
		*/
        public static void TestEntryPoint(string basePath)
        {
            autoya = new Autoya(basePath);
        }

        public static bool forceFailFirstBoot = false;
        public static bool forceFailTokenRefresh = false;
        public static bool forceFailAuthentication = false;
        public static bool forceFailHttp = false;
        public static bool forceSetHttpCodeAsUnauthorized = false;
        public static bool forceMaintenance = false;
        public static bool forceAppUpdated = false;

        public static void ResetAllForceSetting()
        {
            forceFailFirstBoot = false;
            forceFailTokenRefresh = false;
            forceFailAuthentication = false;
            forceFailHttp = false;
            forceSetHttpCodeAsUnauthorized = false;
            forceMaintenance = false;
            forceAppUpdated = false;
        }
    }
}
