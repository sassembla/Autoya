using AutoyaFramework.Persistence.Files;
using UnityEngine;

namespace AutoyaFramework
{
    public partial class Autoya
    {
        /*
			persistence.
				privides  sync/async persistent operation.

				File persistence
		*/

        private FilePersistence _autoyaFilePersistence;

        public static bool Persist_Update(string domain, string filePath, string data)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                return false;
            }
            return autoya._autoyaFilePersistence.Update(domain, filePath, data);
        }

        public static bool Persist_Append(string domain, string filePath, string data)
        {
            var isEnough = false;
            if (isEnough)
            {// estimate size over
                Debug.LogError("Persist_Update save size overed.");
                return false;
            }
            return autoya._autoyaFilePersistence.Append(domain, filePath, data);
        }

        public static string Persist_Load(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.Load(domain, filePath);
        }

        public static bool Persist_Delete(string domain, string filePath)
        {
            return autoya._autoyaFilePersistence.Delete(domain, filePath);
        }

        public static bool Persist_DeleteByDomain(string domain)
        {
            return autoya._autoyaFilePersistence.DeleteByDomain(domain);
        }

        public static string[] Persist_FileNamesInDomain(string domain)
        {
            return autoya._autoyaFilePersistence.FileNamesInDomain(domain);
        }
    }
}