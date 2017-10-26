#if UNITY_PURCHASING
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    [AddComponentMenu("Unity IAP/IAP Listener")]
    [HelpURL("https://docs.unity3d.com/Manual/UnityIAP.html")]
    public class IAPListener : MonoBehaviour
    {
        [System.Serializable]
        public class OnPurchaseCompletedEvent : UnityEvent<Product> {};

        [System.Serializable]
        public class OnPurchaseFailedEvent : UnityEvent<Product, PurchaseFailureReason> {};

        [Tooltip("Consume successful purchases immediately")]
        public bool consumePurchase = true;

        [Tooltip("Preserve this GameObject when a new scene is loaded")]
        public bool dontDestroyOnLoad = true;

        [Tooltip("Event fired after a successful purchase of this product")]
        public OnPurchaseCompletedEvent onPurchaseComplete;

        [Tooltip("Event fired after a failed purchase of this product")]
        public OnPurchaseFailedEvent onPurchaseFailed;

        void OnEnable()
        {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            IAPButton.IAPButtonStoreManager.Instance.AddListener(this);
        }

        void OnDisable()
        {
            IAPButton.IAPButtonStoreManager.Instance.RemoveListener(this);
        }

        /**
         *  Invoked to process a purchase of the product associated with this button
         */
        public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
        {
            Debug.Log(string.Format("IAPListener.ProcessPurchase(PurchaseEventArgs {0} - {1})", e, e.purchasedProduct.definition.id));

            onPurchaseComplete.Invoke(e.purchasedProduct);

            return (consumePurchase) ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
        }

        /**
         *  Invoked on a failed purchase of the product associated with this button
         */
        public void OnPurchaseFailed (Product product, PurchaseFailureReason reason)
        {
            Debug.Log(string.Format("IAPListener.OnPurchaseFailed(Product {0}, PurchaseFailureReason {1})", product, reason));

            onPurchaseFailed.Invoke(product, reason);
        }
    }
}
#endif
