#if UNITY_PURCHASING

using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Automatically initializes Unity IAP with the products defined in the IAP Catalog (if enabled in the UI).
    /// Manages IAPButtons and IAPListeners.
    /// </summary>
    public class CodelessIAPStoreListener : IStoreListener
    {
        private static CodelessIAPStoreListener instance;
        private List<IAPButton> activeButtons = new List<IAPButton>();
        private List<IAPListener> activeListeners = new List<IAPListener> ();
        private static bool unityPurchasingInitialized;

        protected IStoreController controller;
        protected IExtensionProvider extensions;
        protected ProductCatalog catalog;

        // Allows outside sources to know whether the full initialization has taken place.
        public static bool initializationComplete;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeCodelessPurchasingOnLoad() {
            ProductCatalog catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog.enableCodelessAutoInitialization && !catalog.IsEmpty() && instance == null)
            {
                CreateCodelessIAPStoreListenerInstance();
            }
        }

        private static void InitializePurchasing()
        {
            StandardPurchasingModule module = StandardPurchasingModule.Instance();
            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

            ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);

            IAPConfigurationHelper.PopulateConfigurationBuilder(ref builder, instance.catalog);

            UnityPurchasing.Initialize(instance, builder);

            unityPurchasingInitialized = true;
        }

        private CodelessIAPStoreListener()
        {
            catalog = ProductCatalog.LoadDefaultCatalog();
        }

        public static CodelessIAPStoreListener Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateCodelessIAPStoreListenerInstance();
                }
                return instance;
            }
        }

        /// <summary>
        /// Creates the static instance of CodelessIAPStoreListener and initializes purchasing
        /// </summary>
        private static void CreateCodelessIAPStoreListenerInstance()
        {
            instance = new CodelessIAPStoreListener();
            if (!unityPurchasingInitialized)
            {
                Debug.Log("Initializing UnityPurchasing via Codeless IAP");
                InitializePurchasing();
            }
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
            Debug.LogError("CodelessIAPStoreListener attempted to get unknown product " + productID);
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
            initializationComplete = true;
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

                Debug.LogError("Purchase not correctly processed for product \"" +
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
            if (!resultProcessed) {

                Debug.LogError("Failed purchase not correctly handled for product \"" + product.definition.id +
                                  "\". Add an active IAPButton to handle this failure, or add an IAPListener to receive any unhandled purchase failures.");
            }

            return;
        }
    }
}

#endif
