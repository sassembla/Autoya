#if UNITY_PURCHASING
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("Unity IAP/IAP Button")]
    [HelpURL("https://docs.unity3d.com/Manual/UnityIAP.html")]
    public class IAPButton : MonoBehaviour
    {
        public enum ButtonType
        {
            Purchase,
            Restore
        }

        [System.Serializable]
        public class OnPurchaseCompletedEvent : UnityEvent<Product>
        {
        };

        [System.Serializable]
        public class OnPurchaseFailedEvent : UnityEvent<Product, PurchaseFailureReason>
        {
        };

        [HideInInspector]
        public string productId;

        [Tooltip("The type of this button, can be either a purchase or a restore button")]
        public ButtonType buttonType = ButtonType.Purchase;

        [Tooltip("Consume the product immediately after a successful purchase")]
        public bool consumePurchase = true;

        [Tooltip("Event fired after a successful purchase of this product")]
        public OnPurchaseCompletedEvent onPurchaseComplete;

        [Tooltip("Event fired after a failed purchase of this product")]
        public OnPurchaseFailedEvent onPurchaseFailed;

        [Tooltip("[Optional] Displays the localized title from the app store")]
        public Text titleText;

        [Tooltip("[Optional] Displays the localized description from the app store")]
        public Text descriptionText;

        [Tooltip("[Optional] Displays the localized price from the app store")]
        public Text priceText;

        void Start()
        {
            Button button = GetComponent<Button>();

            if (buttonType == ButtonType.Purchase)
            {
                if (button)
                {
                    button.onClick.AddListener(PurchaseProduct);
                }

                if (string.IsNullOrEmpty(productId))
                {
                    Debug.LogError("IAPButton productId is empty");
                }

                if (!CodelessIAPStoreListener.Instance.HasProductInCatalog(productId))
                {
                    Debug.LogWarning("The product catalog has no product with the ID \"" + productId + "\"");
                }
            }
            else if (buttonType == ButtonType.Restore)
            {
                if (button)
                {
                    button.onClick.AddListener(Restore);
                }
            }
        }

        void OnEnable()
        {
            if (buttonType == ButtonType.Purchase)
            {
                CodelessIAPStoreListener.Instance.AddButton(this);
                if (CodelessIAPStoreListener.initializationComplete) {
                    UpdateText();
                }
            }
        }

        void OnDisable()
        {
            if (buttonType == ButtonType.Purchase)
            {
                CodelessIAPStoreListener.Instance.RemoveButton(this);
            }
        }

        void PurchaseProduct()
        {
            if (buttonType == ButtonType.Purchase)
            {
                Debug.Log("IAPButton.PurchaseProduct() with product ID: " + productId);

                CodelessIAPStoreListener.Instance.InitiatePurchase(productId);
            }
        }

        void Restore()
        {
            if (buttonType == ButtonType.Restore)
            {
                if (Application.platform == RuntimePlatform.WSAPlayerX86 ||
                    Application.platform == RuntimePlatform.WSAPlayerX64 ||
                    Application.platform == RuntimePlatform.WSAPlayerARM)
                {
                    CodelessIAPStoreListener.Instance.ExtensionProvider.GetExtension<IMicrosoftExtensions>()
                        .RestoreTransactions();
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer ||
                         Application.platform == RuntimePlatform.OSXPlayer ||
                         Application.platform == RuntimePlatform.tvOS)
                {
                    CodelessIAPStoreListener.Instance.ExtensionProvider.GetExtension<IAppleExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else if (Application.platform == RuntimePlatform.Android &&
                         StandardPurchasingModule.Instance().appStore == AppStore.SamsungApps)
                {
                    CodelessIAPStoreListener.Instance.ExtensionProvider.GetExtension<ISamsungAppsExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else if (Application.platform == RuntimePlatform.Android &&
                         StandardPurchasingModule.Instance().appStore == AppStore.CloudMoolah)
                {
                    CodelessIAPStoreListener.Instance.ExtensionProvider.GetExtension<IMoolahExtension>()
                        .RestoreTransactionID((restoreTransactionIDState) =>
                        {
                            OnTransactionsRestored(
                                restoreTransactionIDState != RestoreTransactionIDState.RestoreFailed &&
                                restoreTransactionIDState != RestoreTransactionIDState.NotKnown);
                        });
                }
                else
                {
                    Debug.LogWarning(Application.platform.ToString() +
                                     " is not a supported platform for the Codeless IAP restore button");
                }
            }
        }

        void OnTransactionsRestored(bool success)
        {
            Debug.Log("Transactions restored: " + success);
        }

        /**
         *  Invoked to process a purchase of the product associated with this button
         */
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            Debug.Log(string.Format("IAPButton.ProcessPurchase(PurchaseEventArgs {0} - {1})", e,
                e.purchasedProduct.definition.id));

            onPurchaseComplete.Invoke(e.purchasedProduct);

            return (consumePurchase) ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
        }

        /**
         *  Invoked on a failed purchase of the product associated with this button
         */
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.Log(string.Format("IAPButton.OnPurchaseFailed(Product {0}, PurchaseFailureReason {1})", product,
                reason));

            onPurchaseFailed.Invoke(product, reason);
        }

        internal void UpdateText()
        {
            var product = CodelessIAPStoreListener.Instance.GetProduct(productId);
            if (product != null)
            {
                if (titleText != null)
                {
                    titleText.text = product.metadata.localizedTitle;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = product.metadata.localizedDescription;
                }

                if (priceText != null)
                {
                    priceText.text = product.metadata.localizedPriceString;
                }
            }
        }
    }
}
#endif
