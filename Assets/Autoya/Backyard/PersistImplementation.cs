using AutoyaFramework.Persistence.Files;

namespace AutoyaFramework {
    public partial class Autoya {
		/*
			persistence.
				privides persistent async operation.

				File persistence
		*/
		
		private FilePersistence _autoyaFilePersistence;

		public static bool Persist_Update (string domain, string filePath, string data) {
			if (false) {// size over

				return false;
			}
			return autoya._autoyaFilePersistence.Update(domain, filePath, data);
		}

		public static string Persist_Load (string domain, string filePath) {
			return autoya._autoyaFilePersistence.Load(domain, filePath);
		}

		public static bool Persist_Delete (string domain, string filePath) {
			return autoya._autoyaFilePersistence.Delete(domain, filePath);
		}

		public static bool Persist_DeleteByDomain (string domain) {
			return autoya._autoyaFilePersistence.DeleteByDomain(domain);
		}
	}
}