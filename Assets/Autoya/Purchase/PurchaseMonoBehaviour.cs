using UnityEngine;

namespace AutoyaFramework.Purchase {
    public class PurchaseMonoBehaviour : MonoBehaviour {
        void Awake () {
            DontDestroyOnLoad(gameObject);
        }
    }
}