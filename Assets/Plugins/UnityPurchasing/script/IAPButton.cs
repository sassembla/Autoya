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

                if (!IAPButtonStoreManager.Instance.HasProductInCatalog(productId))
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
                IAPButtonStoreManager.Instance.AddButton(this);
                UpdateText();
            }
        }

        void OnDisable()
        {
            if (buttonType == ButtonType.Purchase)
            {
                IAPButtonStoreManager.Instance.RemoveButton(this);
            }
        }

        void PurchaseProduct()
        {
            if (buttonType == ButtonType.Purchase)
            {
                Debug.Log("IAPButton.PurchaseProduct() with product ID: " + productId);

                IAPButtonStoreManager.Instance.InitiatePurchase(productId);
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
                    IAPButtonStoreManager.Instance.ExtensionProvider.GetExtension<IMicrosoftExtensions>()
                        .RestoreTransactions();
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer ||
                         Application.platform == RuntimePlatform.OSXPlayer ||
                         Application.platform == RuntimePlatform.tvOS)
                {
                    IAPButtonStoreManager.Instance.ExtensionProvider.GetExtension<IAppleExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else if (Application.platform == RuntimePlatform.Android &&
                         StandardPurchasingModule.Instance().appStore == AppStore.SamsungApps)
                {
                    IAPButtonStoreManager.Instance.ExtensionProvider.GetExtension<ISamsungAppsExtensions>()
                        .RestoreTransactions(OnTransactionsRestored);
                }
                else if (Application.platform == RuntimePlatform.Android &&
                         StandardPurchasingModule.Instance().appStore == AppStore.CloudMoolah)
                {
                    IAPButtonStoreManager.Instance.ExtensionProvider.GetExtension<IMoolahExtension>()
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

        private void UpdateText()
        {
            var product = IAPButtonStoreManager.Instance.GetProduct(productId);
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

        public class IAPButtonStoreManager : IStoreListener
        {
            private static IAPButtonStoreManager instance = new IAPButtonStoreManager();
            private ProductCatalog catalog;
            private List<IAPButton> activeButtons = new List<IAPButton>();
            private List<IAPListener> activeListeners = new List<IAPListener> ();

            protected IStoreController controller;
            protected IExtensionProvider extensions;

            private IAPButtonStoreManager()
            {
                catalog = ProductCatalog.LoadDefaultCatalog();

                StandardPurchasingModule module = StandardPurchasingModule.Instance();
                module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

                ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);

                IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder, catalog);

                UnityPurchasing.Initialize(this, builder);
            }

            public static IAPButtonStoreManager Instance
            {
                get { return instance; }
            }

            public IStoreController StoreController
            {
                get { return controller; }
            }

            public IExtensionProvider ExtensionProvider
            {
                get { return extensions; }
            }

            public bool HasProductInCatalog(string productID)
            {
                foreach (var product in catalog.allProducts)
                {
                    if (product.id == productID)
                    {
                        return true;
                    }
                }
                return false;
            }

            public Product GetProduct(string productID)
            {
                if (controller != null && controller.products != null && !string.IsNullOrEmpty(productID))
                {
                    return controller.products.WithID(productID);
                }
                return null;
            }

            public void AddButton(IAPButton button)
            {
                activeButtons.Add(button);
            }

            public void RemoveButton(IAPButton button)
            {
                activeButtons.Remove(button);
            }

            public void AddListener(IAPListener listener)
            {
                activeListeners.Add (listener);
            }

            public void RemoveListener(IAPListener listener)
            {
                activeListeners.Remove (listener);
            }

            public void InitiatePurchase(string productID)
            {
                if (controller == null)
                {
                    Debug.LogError("Purchase failed because Purchasing was not initialized correctly");

                    foreach (var button in activeButtons)
                    {
                        if (button.productId == productID)
                        {
                            button.OnPurchaseFailed(null, Purchasing.PurchaseFailureReason.PurchasingUnavailable);
                        }
                    }
                    return;
                }

                controller.InitiatePurchase(productID);
            }

            public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
            {
                this.controller = controller;
                this.extensions = extensions;

                foreach (var button in activeButtons)
                {
                    button.UpdateText();
                }
            }

            public void OnInitializeFailed(InitializationFailureReason error)
            {
                Debug.LogError(string.Format("Purchasing failed to initialize. Reason: {0}", error.ToString()));
            }

            public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
            {
                PurchaseProcessingResult result;

                // if any receiver consumed this purchase we return the status
                bool consumePurchase = false;
                bool resultProcessed = false;

                foreach (IAPButton button in activeButtons)
                {
                    if (button.productId == e.purchasedProduct.definition.id)
                    {
                        result = button.ProcessPurchase(e);

                        if (result == PurchaseProcessingResult.Complete) {

                            consumePurchase = true;
                        }
                        
                        resultProcessed = true;
                    }
                }

                foreach (IAPListener listener in activeListeners)
                {
                    result = listener.ProcessPurchase(e);

                    if (result == PurchaseProcessingResult.Complete) {

                        consumePurchase = true;
                    }

                    resultProcessed = true;
                }

                // we expect at least one receiver to get this message
                if (!resultProcessed) {

                    Debug.LogWarning("Purchase not correctly processed for product \"" +
                        e.purchasedProduct.definition.id +
                        "\". Add an active IAPButton to process this purchase, or add an IAPListener to receive any unhandled purchase events.");
            
                }

                return (consumePurchase) ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
            {
                bool resultProcessed = false;

                foreach (IAPButton button in activeButtons)
                {
                    if (button.productId == product.definition.id)
                    {
                        button.OnPurchaseFailed(product, reason); 

                        resultProcessed = true;
                    }
                }

                foreach (IAPListener listener in activeListeners)
                {
                    listener.OnPurchaseFailed(product, reason);

                    resultProcessed = true;
                }

                // we expect at least one receiver to get this message
                if (resultProcessed) {
                    
                    Debug.LogWarning ("Failed purchase not correctly handled for product \"" + product.definition.id +
                    "\". Add an active IAPButton to handle this failure, or add an IAPListener to receive any unhandled purchase failures.");
                }
                    
                return;
            }
        }
    }
}
#endif
