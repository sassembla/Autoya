#if UNITY_PURCHASING

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// You must obfuscate your secrets using Window > Unity IAP > Receipt Validation Obfuscator
// before receipt validation will compile in this sample.
//#define RECEIPT_VALIDATION
#endif

//#define DELAY_CONFIRMATION // Returns PurchaseProcessingResult.Pending from ProcessPurchase, then calls ConfirmPendingPurchase after a delay
//#define USE_PAYOUTS // Enables use of PayoutDefinitions to specify what the player should receive when a product is purchased
//#define INTERCEPT_PROMOTIONAL_PURCHASES // Enables intercepting promotional purchases that come directly from the Apple App Store
//#define SUBSCRIPTION_MANAGER //Enables subscription product manager for AppleStore and GooglePlay store

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.Store; // UnityChannel

#if RECEIPT_VALIDATION
using UnityEngine.Purchasing.Security;
#endif

/// <summary>
/// An example of Unity IAP functionality.
/// To use with your account, configure the product ids (AddProduct).
/// </summary>
[AddComponentMenu("Unity IAP/Demo")]
public class IAPDemo : MonoBehaviour, IStoreListener
{
    // Unity IAP objects
    private IStoreController m_Controller;

    private IAppleExtensions m_AppleExtensions;
    private IMoolahExtension m_MoolahExtensions;
    private ISamsungAppsExtensions m_SamsungExtensions;
    private IMicrosoftExtensions m_MicrosoftExtensions;
    private IUnityChannelExtensions m_UnityChannelExtensions;
    private ITransactionHistoryExtensions m_TransactionHistoryExtensions;
    private IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;

#pragma warning disable 0414
    private bool m_IsGooglePlayStoreSelected;
#pragma warning restore 0414
    private bool m_IsSamsungAppsStoreSelected;
    private bool m_IsCloudMoolahStoreSelected;
    private bool m_IsUnityChannelSelected;

    private string m_LastTransactionID;
    private bool m_IsLoggedIn;
    private UnityChannelLoginHandler unityChannelLoginHandler; // Helper for interfacing with UnityChannel API
    private bool m_FetchReceiptPayloadOnPurchase = false;

    private bool m_PurchaseInProgress;

    private Dictionary<string, IAPDemoProductUI> m_ProductUIs = new Dictionary<string, IAPDemoProductUI>();

    public GameObject productUITemplate;
    public RectTransform contentRect;

    public Button restoreButton;
    public Button loginButton;
    public Button validateButton;

    public Text versionText;

#if RECEIPT_VALIDATION
    private CrossPlatformValidator validator;
#endif

    /// <summary>
    /// This will be called when Unity IAP has finished initialising.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_Controller = controller;
        m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
        m_SamsungExtensions = extensions.GetExtension<ISamsungAppsExtensions>();
        m_MoolahExtensions = extensions.GetExtension<IMoolahExtension>();
        m_MicrosoftExtensions = extensions.GetExtension<IMicrosoftExtensions>();
        m_UnityChannelExtensions = extensions.GetExtension<IUnityChannelExtensions>();
        m_TransactionHistoryExtensions = extensions.GetExtension<ITransactionHistoryExtensions>();
        m_GooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
        // Sample code for expose product sku details for google play store
        // Key is product Id (Sku), value is the skuDetails json string
        //Dictionary<string, string> google_play_store_product_SKUDetails_json = m_GooglePlayStoreExtensions.GetProductJSONDictionary();
        // Sample code for manually finish a transaction (consume a product on GooglePlay store)
        //m_GooglePlayStoreExtensions.FinishAdditionalTransaction(productId, transactionId);

        InitUI(controller.products.all);

        // On Apple platforms we need to handle deferred purchases caused by Apple's Ask to Buy feature.
        // On non-Apple platforms this will have no effect; OnDeferred will never be called.
        m_AppleExtensions.RegisterPurchaseDeferredListener(OnDeferred);

#if SUBSCRIPTION_MANAGER
        Dictionary<string, string> introductory_info_dict = m_AppleExtensions.GetIntroductoryPriceDictionary();
#endif
        // Sample code for expose product sku details for apple store
        //Dictionary<string, string> product_details = m_AppleExtensions.GetProductDetails();


        Debug.Log("Available items:");
        foreach (var item in controller.products.all)
        {
            if (item.availableToPurchase)
            {
                Debug.Log(string.Join(" - ",
                    new[]
                    {
                        item.metadata.localizedTitle,
                        item.metadata.localizedDescription,
                        item.metadata.isoCurrencyCode,
                        item.metadata.localizedPrice.ToString(),
                        item.metadata.localizedPriceString,
                        item.transactionID,
                        item.receipt
                    }));
#if INTERCEPT_PROMOTIONAL_PURCHASES
                // Set all these products to be visible in the user's App Store according to Apple's Promotional IAP feature
                // https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/StoreKitGuide/PromotingIn-AppPurchases/PromotingIn-AppPurchases.html
                m_AppleExtensions.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
#endif

#if SUBSCRIPTION_MANAGER
                // this is the usage of SubscriptionManager class
                if (item.receipt != null) {
                    if (item.definition.type == ProductType.Subscription) {
                        if (checkIfProductIsAvailableForSubscriptionManager(item.receipt)) {
                            string intro_json = (introductory_info_dict == null || !introductory_info_dict.ContainsKey(item.definition.storeSpecificId)) ? null : introductory_info_dict[item.definition.storeSpecificId];
                            SubscriptionManager p = new SubscriptionManager(item, intro_json);
                            SubscriptionInfo info = p.getSubscriptionInfo();
                            Debug.Log("product id is: " + info.getProductId());
                            Debug.Log("purchase date is: " + info.getPurchaseDate());
                            Debug.Log("subscription next billing date is: " + info.getExpireDate());
                            Debug.Log("is subscribed? " + info.isSubscribed().ToString());
                            Debug.Log("is expired? " + info.isExpired().ToString());
                            Debug.Log("is cancelled? " + info.isCancelled());
                            Debug.Log("product is in free trial peroid? " + info.isFreeTrial());
                            Debug.Log("product is auto renewing? " + info.isAutoRenewing());
                            Debug.Log("subscription remaining valid time until next billing date is: " + info.getRemainingTime());
                            Debug.Log("is this product in introductory price period? " + info.isIntroductoryPricePeriod());
                            Debug.Log("the product introductory localized price is: " + info.getIntroductoryPrice());
                            Debug.Log("the product introductory price period is: " + info.getIntroductoryPricePeriod());
                            Debug.Log("the number of product introductory price period cycles is: " + info.getIntroductoryPricePeriodCycles());
                        } else {
                            Debug.Log("This product is not available for SubscriptionManager class, only products that are purchase by 1.19+ SDK can use this class.");
                        }
                    } else {
                        Debug.Log("the product is not a subscription product");
                    }
                } else {
                    Debug.Log("the product should have a valid receipt");
                }
#endif
            }
        }

        // Populate the product menu now that we have Products
        AddProductUIs(m_Controller.products.all);

        LogProductDefinitions();
    }

#if SUBSCRIPTION_MANAGER
    private bool checkIfProductIsAvailableForSubscriptionManager(string receipt) {
        var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(receipt);
        if (!receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload")) {
            Debug.Log("The product receipt does not contain enough information");
            return false;
        }
        var store = (string)receipt_wrapper ["Store"];
        var payload = (string)receipt_wrapper ["Payload"];

        if (payload != null ) {
            switch (store) {
            case GooglePlay.Name:
                {
                    var payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
                    if (!payload_wrapper.ContainsKey("json")) {
                        Debug.Log("The product receipt does not contain enough information, the 'json' field is missing");
                        return false;
                    }
                    var original_json_payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode((string)payload_wrapper["json"]);
                    if (original_json_payload_wrapper == null || !original_json_payload_wrapper.ContainsKey("developerPayload")) {
                        Debug.Log("The product receipt does not contain enough information, the 'developerPayload' field is missing");
                        return false;
                    }
                    var developerPayloadJSON = (string)original_json_payload_wrapper["developerPayload"];
                    var developerPayload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(developerPayloadJSON);
                    if (developerPayload_wrapper == null || !developerPayload_wrapper.ContainsKey("is_free_trial") || !developerPayload_wrapper.ContainsKey("has_introductory_price_trial")) {
                        Debug.Log("The product receipt does not contain enough information, the product is not purchased using 1.19 or later");
                        return false;
                    }
                    return true;
                }
            case AppleAppStore.Name:
            case AmazonApps.Name:
            case MacAppStore.Name:
                {
                    return true;
                }
            default:
                {
                    return false;
                }
            }
        }
        return false;
    }
#endif

    /// <summary>
    /// This will be called when a purchase completes.
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        Debug.Log("Purchase OK: " + e.purchasedProduct.definition.id);
        Debug.Log("Receipt: " + e.purchasedProduct.receipt);

        m_LastTransactionID = e.purchasedProduct.transactionID;
        m_PurchaseInProgress = false;

        // Decode the UnityChannelPurchaseReceipt, extracting the gameOrderId
        if (m_IsUnityChannelSelected)
        {
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(e.purchasedProduct.receipt);
            if (unifiedReceipt != null && !string.IsNullOrEmpty(unifiedReceipt.Payload))
            {
                var purchaseReceipt = JsonUtility.FromJson<UnityChannelPurchaseReceipt>(unifiedReceipt.Payload);
                Debug.LogFormat(
                    "UnityChannel receipt: storeSpecificId = {0}, transactionId = {1}, orderQueryToken = {2}",
                    purchaseReceipt.storeSpecificId, purchaseReceipt.transactionId, purchaseReceipt.orderQueryToken);
            }
        }

#if RECEIPT_VALIDATION // Local validation is available for GooglePlay, Apple, and UnityChannel stores
        if (m_IsGooglePlayStoreSelected ||
            (m_IsUnityChannelSelected && m_FetchReceiptPayloadOnPurchase) ||
            Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.tvOS) {
            try {
                var result = validator.Validate(e.purchasedProduct.receipt);
                Debug.Log("Receipt is valid. Contents:");
                foreach (IPurchaseReceipt productReceipt in result) {
                    Debug.Log(productReceipt.productID);
                    Debug.Log(productReceipt.purchaseDate);
                    Debug.Log(productReceipt.transactionID);

                    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                    if (null != google) {
                        Debug.Log(google.purchaseState);
                        Debug.Log(google.purchaseToken);
                    }

                    UnityChannelReceipt unityChannel = productReceipt as UnityChannelReceipt;
                    if (null != unityChannel) {
                        Debug.Log(unityChannel.productID);
                        Debug.Log(unityChannel.purchaseDate);
                        Debug.Log(unityChannel.transactionID);
                    }

                    AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                    if (null != apple) {
                        Debug.Log(apple.originalTransactionIdentifier);
                        Debug.Log(apple.subscriptionExpirationDate);
                        Debug.Log(apple.cancellationDate);
                        Debug.Log(apple.quantity);
                    }

                    // For improved security, consider comparing the signed
                    // IPurchaseReceipt.productId, IPurchaseReceipt.transactionID, and other data
                    // embedded in the signed receipt objects to the data which the game is using
                    // to make this purchase.
                }
            } catch (IAPSecurityException ex) {
                Debug.Log("Invalid receipt, not unlocking content. " + ex);
                return PurchaseProcessingResult.Complete;
            }
        }
        #endif

        // Unlock content from purchases here.
#if USE_PAYOUTS
        if (e.purchasedProduct.definition.payouts != null) {
            Debug.Log("Purchase complete, paying out based on defined payouts");
            foreach (var payout in e.purchasedProduct.definition.payouts) {
                Debug.Log(string.Format("Granting {0} {1} {2} {3}", payout.quantity, payout.typeString, payout.subtype, payout.data));
            }
        }
#endif
        // Indicate if we have handled this purchase.
        //   PurchaseProcessingResult.Complete: ProcessPurchase will not be called
        //     with this product again, until next purchase.
        //   PurchaseProcessingResult.Pending: ProcessPurchase will be called
        //     again with this product at next app launch. Later, call
        //     m_Controller.ConfirmPendingPurchase(Product) to complete handling
        //     this purchase. Use to transactionally save purchases to a cloud
        //     game service.
#if DELAY_CONFIRMATION
        StartCoroutine(ConfirmPendingPurchaseAfterDelay(e.purchasedProduct));
        return PurchaseProcessingResult.Pending;
#else
        UpdateProductUI(e.purchasedProduct);
        return PurchaseProcessingResult.Complete;
#endif
    }

#if DELAY_CONFIRMATION
    private HashSet<string> m_PendingProducts = new HashSet<string>();

    private IEnumerator ConfirmPendingPurchaseAfterDelay(Product p)
    {
        m_PendingProducts.Add(p.definition.id);
        Debug.Log("Delaying confirmation of " + p.definition.id + " for 5 seconds.");

		var end = Time.time + 5f;

		while (Time.time < end) {
			yield return null;
			var remaining = Mathf.CeilToInt (end - Time.time);
			UpdateProductPendingUI (p, remaining);
		}

        Debug.Log("Confirming purchase of " + p.definition.id);
        m_Controller.ConfirmPendingPurchase(p);
        m_PendingProducts.Remove(p.definition.id);
		UpdateProductUI (p);
    }
#endif

    /// <summary>
    /// This will be called if an attempted purchase fails.
    /// </summary>
    public void OnPurchaseFailed(Product item, PurchaseFailureReason r)
    {
        Debug.Log("Purchase failed: " + item.definition.id);
        Debug.Log(r);

        // Detailed debugging information
        Debug.Log("Store specific error code: " + m_TransactionHistoryExtensions.GetLastStoreSpecificPurchaseErrorCode());
        if (m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription() != null)
        {
            Debug.Log("Purchase failure description message: " +
                      m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription().message);
        }

        if (m_IsUnityChannelSelected)
        {
            var extra = m_UnityChannelExtensions.GetLastPurchaseError();
            var purchaseError = JsonUtility.FromJson<UnityChannelPurchaseError>(extra);

            if (purchaseError != null && purchaseError.purchaseInfo != null)
            {
                // Additional information about purchase failure.
                var purchaseInfo = purchaseError.purchaseInfo;
                Debug.LogFormat(
                    "UnityChannel purchaseInfo: productCode = {0}, gameOrderId = {1}, orderQueryToken = {2}",
                    purchaseInfo.productCode, purchaseInfo.gameOrderId, purchaseInfo.orderQueryToken);
            }

            // Determine if the user already owns this item and that it can be added to
            // their inventory, if not already present.
#if UNITY_5_6_OR_NEWER
            if (r == PurchaseFailureReason.DuplicateTransaction)
            {
                // Unlock `item` in inventory if not already present.
                Debug.Log("Duplicate transaction detected, unlock this item");
            }
#else // Building using Unity strictly less than 5.6; e.g 5.3-5.5.
            // In Unity 5.3 the enum PurchaseFailureReason.DuplicateTransaction
            // may not be available (is available in 5.6 ... specifically
            // 5.5.1p1+, 5.4.4p2+) and can be substituted with this call.
            if (r == PurchaseFailureReason.Unknown)
            {
                if (purchaseError != null && purchaseError.error != null &&
                    purchaseError.error.Equals("DuplicateTransaction"))
                {
                    // Unlock `item` in inventory if not already present.
                    Debug.Log("Duplicate transaction detected, unlock this item");
                }
            }
#endif
        }

        m_PurchaseInProgress = false;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("Billing failed to initialize!");
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                Debug.LogError("Is your App correctly uploaded on the relevant publisher console?");
                break;
            case InitializationFailureReason.PurchasingUnavailable:
                // Ask the user if billing is disabled in device settings.
                Debug.Log("Billing disabled!");
                break;
            case InitializationFailureReason.NoProductsAvailable:
                // Developer configuration error; check product metadata.
                Debug.Log("No products available for purchase!");
                break;
        }
    }

    [Serializable]
    public class UnityChannelPurchaseError
    {
        public string error;
        public UnityChannelPurchaseInfo purchaseInfo;
    }

    [Serializable]
    public class UnityChannelPurchaseInfo
    {
        public string productCode; // Corresponds to storeSpecificId
        public string gameOrderId; // Corresponds to transactionId
        public string orderQueryToken;
    }

    public void Awake()
    {
        var module = StandardPurchasingModule.Instance();

        // The FakeStore supports: no-ui (always succeeding), basic ui (purchase pass/fail), and
        // developer ui (initialization, purchase, failure code setting). These correspond to
        // the FakeStoreUIMode Enum values passed into StandardPurchasingModule.useFakeStoreUIMode.
        module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

        var builder = ConfigurationBuilder.Instance(module);

        // Set this to true to enable the Microsoft IAP simulator for local testing.
        builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = false;

        m_IsGooglePlayStoreSelected =
            Application.platform == RuntimePlatform.Android && module.appStore == AppStore.GooglePlay;

        // CloudMoolah Configuration setings
        // All games must set the configuration. the configuration need to apply on the CloudMoolah Portal.
        // CloudMoolah APP Key
        builder.Configure<IMoolahConfiguration>().appKey = "d93f4564c41d463ed3d3cd207594ee1b";
        // CloudMoolah Hash Key
        builder.Configure<IMoolahConfiguration>().hashKey = "cc";
        // This enables the CloudMoolah test mode for local testing.
        // You would remove this, or set to CloudMoolahMode.Production, before building your release package.
        builder.Configure<IMoolahConfiguration>().SetMode(CloudMoolahMode.AlwaysSucceed);
        // This records whether we are using Cloud Moolah IAP.
        // Cloud Moolah requires logging in to access your Digital Wallet, so:
        // A) IAPDemo (this) displays the Cloud Moolah GUI button for Cloud Moolah
        m_IsCloudMoolahStoreSelected =
            Application.platform == RuntimePlatform.Android && module.appStore == AppStore.CloudMoolah;

        // UnityChannel, provides access to Xiaomi MiPay.
        // Products are required to be set in the IAP Catalog window.  The file "MiProductCatalog.prop"
        // is required to be generated into the project's
        // Assets/Plugins/Android/assets folder, based off the contents of the
        // IAP Catalog window, for MiPay.
        m_IsUnityChannelSelected =
            Application.platform == RuntimePlatform.Android && module.appStore == AppStore.XiaomiMiPay;
        // UnityChannel supports receipt validation through a backend fetch.
        builder.Configure<IUnityChannelConfiguration>().fetchReceiptPayloadOnPurchase = m_FetchReceiptPayloadOnPurchase;

        // Define our products.
        // Either use the Unity IAP Catalog, or manually use the ConfigurationBuilder.AddProduct API.
        // Use IDs from both the Unity IAP Catalog and hardcoded IDs via the ConfigurationBuilder.AddProduct API.

        // Use the products defined in the IAP Catalog GUI.
        // E.g. Menu: "Window" > "Unity IAP" > "IAP Catalog", then add products, then click "App Store Export".
        var catalog = ProductCatalog.LoadDefaultCatalog();

        foreach (var product in catalog.allValidProducts)
        {
            if (product.allStoreIDs.Count > 0)
            {
                var ids = new IDs();
                foreach (var storeID in product.allStoreIDs)
                {
                    ids.Add(storeID.id, storeID.store);
                }
                builder.AddProduct(product.id, product.type, ids);
            }
            else
            {
                builder.AddProduct(product.id, product.type);
            }
        }

        // In this case our products have the same identifier across all the App stores,
        // except on the Mac App store where product IDs cannot be reused across both Mac and
        // iOS stores.
        // So on the Mac App store our products have different identifiers,
        // and we tell Unity IAP this by using the IDs class.

        builder.AddProduct("100.gold.coins", ProductType.Consumable, new IDs
            {
                {"com.unity3d.unityiap.unityiapdemo.100goldcoins.7", MacAppStore.Name},
                {"000000596586", TizenStore.Name},
                {"com.ff", MoolahAppStore.Name},
                {"100.gold.coins", AmazonApps.Name},
                {"100.gold.coins", AppleAppStore.Name}
            }
#if USE_PAYOUTS
                , new List<PayoutDefinition> {
                new PayoutDefinition(PayoutType.Item, "", 1, "item_id:76543"),
                new PayoutDefinition(PayoutType.Currency, "gold", 50)
                }
#endif //USE_PAYOUTS
                );

        builder.AddProduct("500.gold.coins", ProductType.Consumable, new IDs
            {
                {"com.unity3d.unityiap.unityiapdemo.500goldcoins.7", MacAppStore.Name},
                {"000000596581", TizenStore.Name},
                {"com.ee", MoolahAppStore.Name},
                {"500.gold.coins", AmazonApps.Name},
            }
#if USE_PAYOUTS
        , new PayoutDefinition(PayoutType.Currency, "gold", 500)
#endif //USE_PAYOUTS
        );

        builder.AddProduct("300.gold.coins", ProductType.Consumable, new IDs
            {
            }
#if USE_PAYOUTS
        , new List<PayoutDefinition> {
            new PayoutDefinition(PayoutType.Item, "", 1, "item_id:76543"),
            new PayoutDefinition(PayoutType.Currency, "gold", 50)
        }
#endif //USE_PAYOUTS
        );

        builder.AddProduct("sub1", ProductType.Subscription, new IDs
        {
        });

        builder.AddProduct("sub2", ProductType.Subscription, new IDs
        {
        });


        // Write Amazon's JSON description of our products to storage when using Amazon's local sandbox.
        // This should be removed from a production build.
        //builder.Configure<IAmazonConfiguration>().WriteSandboxJSON(builder.products);

        // This enables simulated purchase success for Samsung IAP.
        // You would remove this, or set to SamsungAppsMode.Production, before building your release package.
        builder.Configure<ISamsungAppsConfiguration>().SetMode(SamsungAppsMode.AlwaysSucceed);
        // This records whether we are using Samsung IAP. Currently ISamsungAppsExtensions.RestoreTransactions
        // displays a blocking Android Activity, so:
        // A) Unity IAP does not automatically restore purchases on Samsung Galaxy Apps
        // B) IAPDemo (this) displays the "Restore" GUI button for Samsung Galaxy Apps
        m_IsSamsungAppsStoreSelected =
            Application.platform == RuntimePlatform.Android && module.appStore == AppStore.SamsungApps;


        // This selects the GroupId that was created in the Tizen Store for this set of products
        // An empty or non-matching GroupId here will result in no products available for purchase
        builder.Configure<ITizenStoreConfiguration>().SetGroupId("100000085616");

#if INTERCEPT_PROMOTIONAL_PURCHASES
        // On iOS and tvOS we can intercept promotional purchases that come directly from the App Store.
        // On other platforms this will have no effect; OnPromotionalPurchase will never be called.
        builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);
        Debug.Log("Setting Apple promotional purchase interceptor callback");
#endif

#if RECEIPT_VALIDATION
        string appIdentifier;
        #if UNITY_5_6_OR_NEWER
        appIdentifier = Application.identifier;
        #else
        appIdentifier = Application.bundleIdentifier;
        #endif
        validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(),
            UnityChannelTangle.Data(), appIdentifier);
        #endif

        Action initializeUnityIap = () =>
        {
            // Now we're ready to initialize Unity IAP.
            UnityPurchasing.Initialize(this, builder);
        };

        bool needExternalLogin = m_IsUnityChannelSelected;

        if (!needExternalLogin)
        {
            initializeUnityIap();
        }
        else
        {
            // Call UnityChannel initialize and (later) login asynchronously

            // UnityChannel configuration settings. Required for Xiaomi MiPay.
            // Collect this app configuration from the Unity Developer website at
            // [2017-04-17 PENDING - Contact support representative]
            // https://developer.cloud.unity3d.com/ providing your Xiaomi MiPay App
            // ID, App Key, and App Secret. This permits Unity to proxy from the
            // user's device into the MiPay system.
            // IMPORTANT PRE-BUILD STEP: For mandatory Chinese Government app auditing
            // and for MiPay testing, enable debug mode (test mode)
            // using the `AppInfo.debug = true;` when initializing Unity Channel.

            AppInfo unityChannelAppInfo = new AppInfo();
            unityChannelAppInfo.appId = "abc123appId";
            unityChannelAppInfo.appKey = "efg456appKey";
            unityChannelAppInfo.clientId = "hij789clientId";
            unityChannelAppInfo.clientKey = "klm012clientKey";
            unityChannelAppInfo.debug = false;

            // Shared handler for Unity Channel initialization, here, and login, later
            unityChannelLoginHandler = new UnityChannelLoginHandler();
            unityChannelLoginHandler.initializeFailedAction = (string message) =>
            {
                Debug.LogError("Failed to initialize and login to UnityChannel: " + message);
            };
            unityChannelLoginHandler.initializeSucceededAction = () => { initializeUnityIap(); };

            StoreService.Initialize(unityChannelAppInfo, unityChannelLoginHandler);
        }
    }

    // For handling initialization and login of UnityChannel, returning control to our store after.
    class UnityChannelLoginHandler : ILoginListener
    {
        internal Action initializeSucceededAction;
        internal Action<string> initializeFailedAction;
        internal Action<UserInfo> loginSucceededAction;
        internal Action<string> loginFailedAction;

        public void OnInitialized()
        {
            initializeSucceededAction();
        }

        public void OnInitializeFailed(string message)
        {
            initializeFailedAction(message);
        }

        public void OnLogin(UserInfo userInfo)
        {
            loginSucceededAction(userInfo);
        }

        public void OnLoginFailed(string message)
        {
            loginFailedAction(message);
        }
    }

    /// <summary>
    /// This will be called after a call to IAppleExtensions.RestoreTransactions().
    /// </summary>
    private void OnTransactionsRestored(bool success)
    {
        Debug.Log("Transactions restored." + success);
    }

    /// <summary>
    /// iOS Specific.
    /// This is called as part of Apple's 'Ask to buy' functionality,
    /// when a purchase is requested by a minor and referred to a parent
    /// for approval.
    ///
    /// When the purchase is approved or rejected, the normal purchase events
    /// will fire.
    /// </summary>
    /// <param name="item">Item.</param>
    private void OnDeferred(Product item)
    {
        Debug.Log("Purchase deferred: " + item.definition.id);
    }

#if INTERCEPT_PROMOTIONAL_PURCHASES
    private void OnPromotionalPurchase(Product item) {
        Debug.Log("Attempted promotional purchase: " + item.definition.id);

        // Promotional purchase has been detected. Handle this event by, e.g. presenting a parental gate.
        // Here, for demonstration purposes only, we will wait five seconds before continuing the purchase.
        StartCoroutine(ContinuePromotionalPurchases());
    }

    private IEnumerator ContinuePromotionalPurchases()
    {
        Debug.Log("Continuing promotional purchases in 5 seconds");
        yield return new WaitForSeconds(5);
        Debug.Log("Continuing promotional purchases now");
        m_AppleExtensions.ContinuePromotionalPurchases (); // iOS and tvOS only; does nothing on Mac
    }
#endif

    private void InitUI(IEnumerable<Product> items)
    {
        // Show Restore, Register, Login, and Validate buttons on supported platforms
        restoreButton.gameObject.SetActive(true);
        loginButton.gameObject.SetActive(NeedLoginButton());
        validateButton.gameObject.SetActive(NeedValidateButton());

        ClearProductUIs();

        restoreButton.onClick.AddListener(RestoreButtonClick);
        loginButton.onClick.AddListener(LoginButtonClick);
        validateButton.onClick.AddListener(ValidateButtonClick);

        versionText.text = "Unity version: " + Application.unityVersion + "\n" +
                           "IAP version: " + StandardPurchasingModule.k_PackageVersion;
    }


    public void PurchaseButtonClick(string productID)
    {
        if (m_PurchaseInProgress == true)
        {
            Debug.Log("Please wait, purchase in progress");
            return;
        }

        if (m_Controller == null)
        {
            Debug.LogError("Purchasing is not initialized");
            return;
        }

        if (m_Controller.products.WithID(productID) == null)
        {
            Debug.LogError("No product has id " + productID);
            return;
        }

        // For platforms needing Login, games utilizing a connected backend
        // game server may wish to login.
        // Standalone games may not need to login.
        if (NeedLoginButton() && m_IsLoggedIn == false)
        {
            Debug.LogWarning("Purchase notifications will not be forwarded server-to-server. Login incomplete.");
        }

        // Don't need to draw our UI whilst a purchase is in progress.
        // This is not a requirement for IAP Applications but makes the demo
        // scene tidier whilst the fake purchase dialog is showing.
        m_PurchaseInProgress = true;

        //Sample code how to add accountId in developerPayload to pass it to getBuyIntentExtraParams
        //Dictionary<string, string> payload_dictionary = new Dictionary<string, string>();
        //payload_dictionary["accountId"] = "Faked account id";
        //payload_dictionary["developerPayload"] = "Faked developer payload";
        //m_Controller.InitiatePurchase(m_Controller.products.WithID(productID), MiniJson.JsonEncode(payload_dictionary));
        m_Controller.InitiatePurchase(m_Controller.products.WithID(productID), "developerPayload");

    }

    public void RestoreButtonClick()
    {
        if (m_IsCloudMoolahStoreSelected)
        {
            if (m_IsLoggedIn == false)
            {
                Debug.LogError("CloudMoolah purchase restoration aborted. Login incomplete.");
            }
            else
            {
                // Restore abnornal transaction identifer, if Client don't receive transaction identifer.
                m_MoolahExtensions.RestoreTransactionID((RestoreTransactionIDState restoreTransactionIDState) =>
                {
                    Debug.Log("restoreTransactionIDState = " + restoreTransactionIDState.ToString());
                    bool success =
                        restoreTransactionIDState != RestoreTransactionIDState.RestoreFailed &&
                        restoreTransactionIDState != RestoreTransactionIDState.NotKnown;
                    OnTransactionsRestored(success);
                });
            }
        }
        else if (m_IsSamsungAppsStoreSelected)
        {
            m_SamsungExtensions.RestoreTransactions(OnTransactionsRestored);
        }
        else if (Application.platform == RuntimePlatform.WSAPlayerX86 ||
                 Application.platform == RuntimePlatform.WSAPlayerX64 ||
                 Application.platform == RuntimePlatform.WSAPlayerARM)
        {
            m_MicrosoftExtensions.RestoreTransactions();
        }
        else if (m_IsGooglePlayStoreSelected)
        {
            m_GooglePlayStoreExtensions.RestoreTransactions(OnTransactionsRestored);
        }
        else
        {
            m_AppleExtensions.RestoreTransactions(OnTransactionsRestored);
        }
    }

    public void LoginButtonClick()
    {
        if (!m_IsUnityChannelSelected)
        {
            Debug.Log("Login is only required for the Xiaomi store");
            return;
        }

        unityChannelLoginHandler.loginSucceededAction = (UserInfo userInfo) =>
        {
            m_IsLoggedIn = true;
            Debug.LogFormat("Succeeded logging into UnityChannel. channel {0}, userId {1}, userLoginToken {2} ",
                userInfo.channel, userInfo.userId, userInfo.userLoginToken);
        };

        unityChannelLoginHandler.loginFailedAction = (string message) =>
        {
            m_IsLoggedIn = false;
            Debug.LogError("Failed logging into UnityChannel. " + message);
        };

        StoreService.Login(unityChannelLoginHandler);
    }

    public void ValidateButtonClick()
    {
        // For local validation, see ProcessPurchase.

        if (!m_IsUnityChannelSelected)
        {
            Debug.Log("Remote purchase validation is only supported for the Xiaomi store");
            return;
        }

        string txId = m_LastTransactionID;
        m_UnityChannelExtensions.ValidateReceipt(txId, (bool success, string signData, string signature) =>
        {
            Debug.LogFormat("ValidateReceipt transactionId {0}, success {1}, signData {2}, signature {3}",
                txId, success, signData, signature);

            // May use signData and signature results to validate server-to-server
        });
    }

    private void ClearProductUIs()
    {
        foreach (var productUIKVP in m_ProductUIs)
        {
            GameObject.Destroy(productUIKVP.Value.gameObject);
        }
        m_ProductUIs.Clear();
    }

    private void AddProductUIs(Product[] products)
    {
        ClearProductUIs();

        var templateRectTransform = productUITemplate.GetComponent<RectTransform>();
        var height = templateRectTransform.rect.height;
        var currPos = templateRectTransform.localPosition;

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, products.Length * height);

        foreach (var p in products)
        {
            var newProductUI = GameObject.Instantiate(productUITemplate.gameObject) as GameObject;
            newProductUI.transform.SetParent(productUITemplate.transform.parent, false);
            var rect = newProductUI.GetComponent<RectTransform>();
            rect.localPosition = currPos;
            currPos += Vector3.down * height;
            newProductUI.SetActive(true);
            var productUIComponent = newProductUI.GetComponent<IAPDemoProductUI>();
            productUIComponent.SetProduct(p, PurchaseButtonClick);

            m_ProductUIs[p.definition.id] = productUIComponent;
        }
    }

    private void UpdateProductUI(Product p)
    {
        if (m_ProductUIs.ContainsKey(p.definition.id))
        {
            m_ProductUIs[p.definition.id].SetProduct(p, PurchaseButtonClick);
        }
    }

    private void UpdateProductPendingUI(Product p, int secondsRemaining)
    {
        if (m_ProductUIs.ContainsKey(p.definition.id))
        {
            m_ProductUIs[p.definition.id].SetPendingTime(secondsRemaining);
        }
    }

    private bool NeedRestoreButton()
    {
        return Application.platform == RuntimePlatform.IPhonePlayer ||
               Application.platform == RuntimePlatform.OSXPlayer ||
               Application.platform == RuntimePlatform.tvOS ||
               Application.platform == RuntimePlatform.WSAPlayerX86 ||
               Application.platform == RuntimePlatform.WSAPlayerX64 ||
               Application.platform == RuntimePlatform.WSAPlayerARM ||
               m_IsSamsungAppsStoreSelected ||
               m_IsCloudMoolahStoreSelected;
    }


    private bool NeedLoginButton()
    {
        return m_IsUnityChannelSelected;
    }

    private bool NeedValidateButton()
    {
        return m_IsUnityChannelSelected;
    }

    private void LogProductDefinitions()
    {
        var products = m_Controller.products.all;
        foreach (var product in products)
        {
#if UNITY_5_6_OR_NEWER
            Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\nenabled: {3}\n", product.definition.id, product.definition.storeSpecificId, product.definition.type.ToString(), product.definition.enabled ? "enabled" : "disabled"));
#else
            Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\n", product.definition.id,
                product.definition.storeSpecificId, product.definition.type.ToString()));
#endif
        }
    }
}

#endif // UNITY_PURCHASING
