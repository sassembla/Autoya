using System.Collections;

namespace AutoyaFramework
{
    public interface ICoroutineUpdater
    {
        void Commit(IEnumerator iEnum);
        void Commit(params IEnumerator[] iEnums);
        void Destroy();
    }
}