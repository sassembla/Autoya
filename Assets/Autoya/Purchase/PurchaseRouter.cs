using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;
using UnityEngine.Purchasing;

/**
	purchase feature of 
	should be use as singleton.

	depends on HTTP api.
	not depends on Autoya itself.
*/
namespace AutoyaFramework.Purchase
{
    /**
		struct for product data.
	*/
    [Serializable]
    public struct ProductInfo
    {
        [SerializeField] public string productId;
        [SerializeField] public string platformProductId;
        [SerializeField] public bool isAvailableToThisPlayer;
        [SerializeField] public string info;

        public ProductInfo(string productId, string platformProductId, bool isPurchasableToThisPlayer, string info)
        {
            this.productId = productId;
            this.platformProductId = platformProductId;
            this.isAvailableToThisPlayer = isPurchasableToThisPlayer;
            this.info = info;
        }
    }

    /**
		serialized product info struct.
	 */
    [Serializable]
    public struct ProductInfos
    {
        [SerializeField] public ProductInfo[] productInfos;
    }



    public class PurchaseRouter : IStoreListener
    {

        /*
			delegate for supply http request header generate func for modules.
		*/
        public delegate Dictionary<string, string> HttpRequestHeaderDelegate(string method, string url, Dictionary<string, string> requestHeader, object data);

        /*
			delegate for handle http response for modules.
		*/
        public delegate void HttpResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed);

        private readonly HttpRequestHeaderDelegate httpRequestHeaderDelegate;
        private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        private readonly Func<string, object> onTicketRequest;

        [Serializable]
        public struct PurchaseFailed
        {
            public string ticketId;
            public string transactionId;
            public string reason;
            public PurchaseFailed(string ticketId, string transactionId, string reason)
            {
                this.ticketId = ticketId;
                this.transactionId = transactionId;
                this.reason = reason;
            }
        }

        public enum RequestProductInfosAs
        {
            String,
            Binary
        }

        public enum RouterState
        {
            None,

            LoadingProducts,
            FailedToGetProducts,

            LoadingStore,
            FailedToLoadStore,


            PurchaseReady,
            RetrySending,
            RetryFailed,


            GettingTransaction,
            Purchasing,
        }

        public enum PurchaseReadyError
        {
            Offline,
            FailedToGetProducts,

            /*
				Unty IAP initialize errors.
			*/
            UnityIAP_Initialize_AppNowKnown,
            UnityIAP_Initialize_NoProductsAvailable,
            UnityIAP_Initialize_PurchasingUnavailable,
        }

        public enum PurchaseError
        {
            Offline,

            AlreadyPurchasing,
            TicketGetError,

            UnavailableProduct,
            RetryFailed,

            /*
				Unity IAP Purchase errors.
			*/
            UnityIAP_Purchase_PurchasingUnavailable,
            UnityIAP_Purchase_ExistingPurchasePending,
            UnityIAP_Purchase_ProductUnavailable,
            UnityIAP_Purchase_SignatureInvalid,
            UnityIAP_Purchase_UserCancelled,
            UnityIAP_Purchase_PaymentDeclined,
            UnityIAP_Purchase_DuplicateTransaction,
            UnityIAP_Purchase_Unknown,

            UnknownError
        }

        private RouterState routerState = RouterState.None;
        public RouterState State()
        {
            return routerState;
        }

        private readonly string storeKind;

        private Dictionary<string, string> BasicRequestHeaderDelegate(string method, string url, Dictionary<string, string> requestHeader, object data)
        {
            return requestHeader;
        }

        private readonly HTTPConnection http;


        private Action readyPurchase;
        private Action<PurchaseReadyError, int, string> failedToReady;

        private void BasicResponseHandlingDelegate(string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed)
        {
            if (200 <= httpCode && httpCode < 299)
            {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason);
        }

        private Dictionary<string, string> onMemoryFailedLog = new Dictionary<string, string>();

        private readonly string storeId;
        public string StoreId()
        {
            return storeId;
        }

        private readonly Action<IEnumerator> enumExecutor;
        private readonly Func<object, string> onTicketResponse;
        private readonly Func<TicketAndReceipt, object> onPurchaseDeployRequest;
        private readonly Action<string, object> onPurchaseCompletedInBackground;

        private ProductInfo[] verifiedProducts;

        /**
			constructor.

			Func<string, Dictionary<string, string>> requestHeader func is used to get request header from outside of this feature. 
			by default it returns empty headers.

			also you can modify http error handling via httpResponseHandlingDelegate.
			by default, http response code 200 ~ 299 is treated as success, and other codes are treated as network error.
		 */
        public PurchaseRouter(
            Action<IEnumerator> executor,
            Func<RequestProductInfosAs> getProductInfosAs,
            Func<object, ProductInfo[]> onLoadProducts,
            Func<string, object> onTicketRequest,
            Func<object, string> onTicketResponse,
            Action onPurchaseReady,
            Action<PurchaseReadyError, int, string> onPurchaseReadyFailed,
            Func<TicketAndReceipt, object> onPurchaseDeployRequest,
            Action<string, object> onPurchaseCompletedInBackground = null,
            HttpRequestHeaderDelegate httpGetRequestHeaderDeletage = null,
            HttpResponseHandlingDelegate httpResponseHandlingDelegate = null
        )
        {
            this.storeId = Guid.NewGuid().ToString();

            this.enumExecutor = executor;

            /*
				set store kind by platform.
			*/
#if Update_PurchaseLib            
            this.storeKind = "dummy";
#elif UNITY_EDITOR
            this.storeKind = AppleAppStore.Name;
#elif UNITY_IOS
				this.storeKind = AppleAppStore.Name;
#elif UNITY_ANDROID
				this.storeKind = GooglePlay.Name;
#endif

            if (httpGetRequestHeaderDeletage != null)
            {
                this.httpRequestHeaderDelegate = httpGetRequestHeaderDeletage;
            }
            else
            {
                this.httpRequestHeaderDelegate = BasicRequestHeaderDelegate;
            }

            this.http = new HTTPConnection();

            if (httpResponseHandlingDelegate != null)
            {
                this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
            }
            else
            {
                this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
            }

            this.onTicketRequest = onTicketRequest;
            this.onTicketResponse = onTicketResponse;
            this.onPurchaseDeployRequest = onPurchaseDeployRequest;
            this.onPurchaseCompletedInBackground = onPurchaseCompletedInBackground;


            var cor = _Ready(
                getProductInfosAs,
                onLoadProducts,
                onPurchaseReady,
                onPurchaseReadyFailed
            );
            enumExecutor(cor);
        }

        public ProductInfo[] ProductInfos()
        {
            return verifiedProducts;
        }

        private IEnumerator _Ready(
            Func<RequestProductInfosAs> getProductInfosAs,
            Func<object, ProductInfo[]> onLoadProducts,
            Action reloaded,
            Action<PurchaseReadyError, int, string> failedToReady
        )
        {
            if (routerState == RouterState.PurchaseReady)
            {
                // IAP features are already running.
                // no need to reload.
                reloaded();
                yield break;
            }

            this.readyPurchase = reloaded;
            this.failedToReady = failedToReady;
            routerState = RouterState.LoadingProducts;

            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_READY_PREFIX + Guid.NewGuid().ToString();
            var url = PurchaseSettings.PURCHASE_URL_READY;

            var getAs = getProductInfosAs();

            var cor = HttpGet(
                connectionId,
                url,
                (conId, data) =>
                {
                    if (data is string)
                    {
                        var productsFromStr = onLoadProducts((string)data);
                        this.verifiedProducts = productsFromStr;
                        ReadyIAPFeature(productsFromStr);
                        return;
                    }

                    var products = onLoadProducts((byte[])data);
                    this.verifiedProducts = products;
                    ReadyIAPFeature(products);
                },
                (conId, code, reason) =>
                {
                    routerState = RouterState.FailedToGetProducts;
                    this.failedToReady(PurchaseReadyError.FailedToGetProducts, code, reason);
                },
                getAs
            );

            while (cor.MoveNext())
            {
                yield return null;
            }
        }


        // public class MyPurchasingModule : UnityEngine.Purchasing.Extension.IPurchasingModule {
        // 	public void Configure (UnityEngine.Purchasing.Extension.IPurchasingBinder binder) {
        // 		binder.RegisterStore("MyManufacturerAppStore", InstantiateMyManufacturerAppStore());
        // 		// Our Purchasing service implementation provides the real implementation.
        // 		binder.RegisterExtension<IStoreExtension>(new FakeManufacturerExtensions());
        // 	}

        // 	UnityEngine.Purchasing.Extension.IStore InstantiateMyManufacturerAppStore () {
        // 		// Check for Manufacturer. "Android" used here for the sake of example.
        // 		// In your implementation, return platform-appropriate store instead of "null".
        // 		if (Application.platform == RuntimePlatform.Android) { return null; }
        // 		else { return null; }
        // 	}

        // 	public IStoreExtension IManufacturerExtensions() { return null; }
        // }

        // public class FakeManufacturerExtensions : IStoreExtension {

        // }


        private void ReadyIAPFeature(ProductInfo[] productInfos)
        {
            routerState = RouterState.LoadingStore;
#if !Update_PurchaseLib
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var productInfo in productInfos)
            {
                builder.AddProduct(
                    productInfo.productId,
                    ProductType.Consumable, new IDs{
                        {productInfo.platformProductId, storeKind}
                    }
                );
            }

            /*
				check network connectivity again. because Unity IAP never tells offline.
			*/
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                failedToReady(PurchaseReadyError.Offline, 0, "network is offline.");
                return;
            }

            UnityPurchasing.Initialize(this, builder);
#endif
        }
        private IStoreController controller;
        // private IExtensionProvider extensions;

        public bool IsPurchaseReady()
        {
            return routerState == RouterState.PurchaseReady;
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            this.controller = controller;
            // this.extensions = extensions;

            routerState = RouterState.PurchaseReady;
            if (readyPurchase != null)
            {
                readyPurchase();
            }
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            routerState = RouterState.FailedToLoadStore;
            switch (error)
            {
                case InitializationFailureReason.AppNotKnown:
                    {
                        failedToReady(PurchaseReadyError.UnityIAP_Initialize_AppNowKnown, 0, "The store reported the app as unknown.");
                        break;
                    }
                case InitializationFailureReason.NoProductsAvailable:
                    {
                        failedToReady(PurchaseReadyError.UnityIAP_Initialize_NoProductsAvailable, 0, "No products available for purchase.");
                        break;
                    }
                case InitializationFailureReason.PurchasingUnavailable:
                    {
                        failedToReady(PurchaseReadyError.UnityIAP_Initialize_PurchasingUnavailable, 0, "In-App Purchases disabled in device settings.");
                        break;
                    }
            }
        }

        /**
			start purchase.
		*/


        public IEnumerator PurchaseAsync(
            string purchaseId,
            string productId,
            Action<string> purchaseSucceeded,
            Action<string, PurchaseError, int, string> purchaseFailed
        )
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (purchaseFailed != null)
                {
                    purchaseFailed(purchaseId, PurchaseError.Offline, -1, "network is offline.");
                }
                yield break;
            }

            if (routerState != RouterState.PurchaseReady)
            {
                switch (routerState)
                {
                    case RouterState.GettingTransaction:
                    case RouterState.Purchasing:
                        {
                            if (purchaseFailed != null)
                            {
                                purchaseFailed(purchaseId, PurchaseError.AlreadyPurchasing, -1, "purchasing another product now. wait then retry.");
                            }
                            break;
                        }
                    case RouterState.RetryFailed:
                        {
                            if (purchaseFailed != null)
                            {
                                purchaseFailed(purchaseId, PurchaseError.RetryFailed, -1, "purchasing completed and send it to server is retried and totally failed.");
                            }
                            break;
                        }
                    default:
                        {
                            if (purchaseFailed != null)
                            {
                                purchaseFailed(purchaseId, PurchaseError.UnknownError, -1, "state is:" + routerState);
                            }
                            break;
                        }
                }
                yield break;
            }


            if (verifiedProducts != null)
            {
                var verified = false;
                foreach (var verifiedProduct in verifiedProducts)
                {
                    if (verifiedProduct.productId == productId && verifiedProduct.isAvailableToThisPlayer)
                    {
                        verified = true;
                    }
                }

                if (!verified)
                {
                    if (purchaseFailed != null)
                    {
                        purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, -1, "this product is not available. productId:" + productId);
                    }
                    yield break;
                }
            }

            // renew callback.
            callbacks = new Callbacks(null, string.Empty, string.Empty, tId => { }, (tId, error, code, reason) => { });


            /*
				start getting ticket for purchase.
			 */
            var ticketUrl = PurchaseSettings.PURCHASE_URL_TICKET;
            var data = onTicketRequest(productId);

            routerState = RouterState.GettingTransaction;

            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_TICKET_PREFIX + Guid.NewGuid().ToString();

            if (data is string || data is byte[])
            {
                var cor = HttpPost(
                    connectionId,
                    ticketUrl,
                    data,
                    (conId, resultData) =>
                    {
                        var ticketId = onTicketResponse(resultData);

                        TicketReceived(purchaseId, productId, ticketId, purchaseSucceeded, purchaseFailed);
                    },
                    (conId, code, reason) =>
                    {
                        routerState = RouterState.PurchaseReady;
                        if (purchaseFailed != null)
                        {
                            purchaseFailed(purchaseId, PurchaseError.TicketGetError, code, reason);
                        }
                    }
               );

                while (cor.MoveNext())
                {
                    yield return null;
                }

                // done sending.
                yield break;
            }

            throw new Exception("request data should be string or byte[].");
        }

        private struct Callbacks
        {
            public readonly Product p;
            public readonly string ticketId;
            public readonly Action purchaseSucceeded;
            public readonly Action<PurchaseError, int, string> purchaseFailed;
            public Callbacks(Product p, string purchaseId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, int, string> purchaseFailed)
            {
                this.p = p;
                this.ticketId = ticketId;
                this.purchaseSucceeded = () =>
                {
                    purchaseSucceeded(purchaseId);
                };
                this.purchaseFailed = (err, code, reason) =>
                {
                    purchaseFailed(purchaseId, err, code, reason);
                };
            }
        }

        private Callbacks callbacks = new Callbacks(null, string.Empty, string.Empty, tId => { }, (tId, error, code, reason) => { });
        private void TicketReceived(string purchaseId, string productId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, int, string> purchaseFailed)
        {
            var product = this.controller.products.WithID(productId);
            if (product != null)
            {
                if (product.availableToPurchase)
                {
                    routerState = RouterState.Purchasing;

                    /*
						renew callback.
					*/
                    callbacks = new Callbacks(product, purchaseId, ticketId, purchaseSucceeded, purchaseFailed);
                    this.controller.InitiatePurchase(product);
                    return;
                }
            }

            routerState = RouterState.PurchaseReady;
            if (purchaseFailed != null)
            {
                purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, -1, "selected product is not available.");
            }
        }

        [Serializable]
        public struct TicketAndReceipt
        {
            [SerializeField] private string ticketId;
            [SerializeField] private string data;
            public TicketAndReceipt(string ticketId, string data)
            {
                this.ticketId = ticketId;
                this.data = data;
            }
        }

        /// <summary>
        /// Called when a purchase completes.
        ///
        /// May be called at any time after OnInitialized().
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            if (callbacks.p == null)
            {
                SendPaid(e);
                return PurchaseProcessingResult.Pending;
            }

            if (e.purchasedProduct.transactionID != callbacks.p.transactionID)
            {
                SendPaid(e);
                return PurchaseProcessingResult.Pending;
            }

            /*
				this process is the purchase just retrieving now.
				proceed deploy asynchronously.
			*/
            switch (routerState)
            {
                case RouterState.Purchasing:
                    {
                        var purchasedUrl = PurchaseSettings.PURCHASE_URL_PURCHASE;

                        var data = onPurchaseDeployRequest(new TicketAndReceipt(callbacks.ticketId, e.purchasedProduct.receipt));

                        var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PURCHASE_PREFIX + Guid.NewGuid().ToString();

                        if (data is string || data is byte[])
                        {
                            var cor = HttpPost(
                                connectionId,
                                purchasedUrl,
                                data,
                                (conId, responseData) =>
                                {
                                    var product = e.purchasedProduct;
                                    controller.ConfirmPendingPurchase(e.purchasedProduct);
                                    routerState = RouterState.PurchaseReady;

                                    if (callbacks.purchaseSucceeded != null)
                                    {
                                        callbacks.purchaseSucceeded();
                                    }
                                },
                                (conId, code, reason) =>
                                {
                                    // OK以外のコードについてはcompleteを行わず、リトライ。
                                    routerState = RouterState.RetrySending;

                                    StartRetry(callbacks, e);
                                }
                            );
                            enumExecutor(cor);
                            break;
                        }

                        throw new Exception("request data should be string or byte[].");
                    }
                default:
                    {
                        // maybe never comes here.
                        if (callbacks.purchaseFailed != null)
                        {
                            callbacks.purchaseFailed(PurchaseError.UnknownError, -1, "failed to deploy purchased item case 2. state:" + routerState);
                        }
                        break;
                    }
            }

            /*
				always pending.
			*/
            return PurchaseProcessingResult.Pending;
        }

        private void StartRetry(Callbacks callbacks, PurchaseEventArgs e)
        {
            var cor = RetryCoroutine(callbacks, e);
            enumExecutor(cor);
        }

        private IEnumerator RetryCoroutine(Callbacks callbacks, PurchaseEventArgs e)
        {
            var count = 0;
            var retryFailedHttpCode = -1;
            var retryFailedHttpReason = string.Empty;
        retry:
            {
                var waitSec = Mathf.Pow(count, 2);
                count++;

                yield return new WaitForSeconds(waitSec);

                var purchasedUrl = PurchaseSettings.PURCHASE_URL_PURCHASE;
                var data = onPurchaseDeployRequest(new TicketAndReceipt(callbacks.ticketId, e.purchasedProduct.receipt));

                var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PURCHASE_PREFIX + Guid.NewGuid().ToString();

                IEnumerator cor = null;
                if (data is string || data is byte[])
                {
                    cor = HttpPost(
                        connectionId,
                        purchasedUrl,
                        data,
                        (conId, responseData) =>
                        {
                            var product = e.purchasedProduct;
                            controller.ConfirmPendingPurchase(e.purchasedProduct);

                            routerState = RouterState.PurchaseReady;

                            if (callbacks.purchaseSucceeded != null)
                            {
                                callbacks.purchaseSucceeded();
                            }
                        },
                        (conId, code, reason) =>
                        {
                            // still in RetrySending state.
                            // update failed httpCode for show last failed reason.
                            retryFailedHttpCode = code;
                            retryFailedHttpReason = reason;
                        }
                    );
                }
                else
                {
                    throw new Exception("request data should be string or byte[].");
                }

                while (cor.MoveNext())
                {
                    yield return null;
                }

                if (routerState != RouterState.RetrySending)
                {
                    // retry finished.
                    yield break;
                }

                // still in retry.
                if (count == PurchaseSettings.PURCHASED_MAX_RETRY_COUNT)
                {
                    routerState = RouterState.RetryFailed;

                    if (callbacks.purchaseFailed != null)
                    {
                        callbacks.purchaseFailed(PurchaseError.RetryFailed, retryFailedHttpCode, retryFailedHttpReason);
                    }
                    yield break;
                }

                // continue retry.
                goto retry;
            }
        }

        private void SendPaid(PurchaseEventArgs e)
        {
            // set if need.
            var ticketId = string.Empty;

            // set ticketId if failed ticketId is allocated with same transaction id.
            if (onMemoryFailedLog.ContainsKey(e.purchasedProduct.transactionID))
            {
                ticketId = onMemoryFailedLog[e.purchasedProduct.transactionID];

                // exhaust.
                onMemoryFailedLog.Remove(e.purchasedProduct.transactionID);
            }

            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PAID_PREFIX + Guid.NewGuid().ToString();
            var purchasedUrl = PurchaseSettings.PURCHASE_URL_PAID;

            var data = onPurchaseDeployRequest(new TicketAndReceipt(ticketId, e.purchasedProduct.receipt));

            if (data is string || data is byte[])
            {
                var cor = HttpPost(
                    connectionId,
                    purchasedUrl,
                    data,
                    (conId, responseData) =>
                    {
                        // complete paid product transaction.
                        controller.ConfirmPendingPurchase(e.purchasedProduct);

                        if (onPurchaseCompletedInBackground != null)
                        {
                            onPurchaseCompletedInBackground(e.purchasedProduct.definition.id, responseData);
                        }
                    },
                    (conId, code, reason) =>
                    {
                        // systems do this process again automatically.
                        // no need to do something.
                    }
                );
                enumExecutor(cor);
            }
            else
            {
                throw new Exception("request data should be string or byte[].");
            }
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        public void OnPurchaseFailed(Product i, PurchaseFailureReason failReason)
        {
            // no retrieving product == not purchasing.
            if (callbacks.p == null)
            {
                // do nothing here.
                return;
            }

            // transactionID does not match to retrieving product's transactionID,
            // it's not the product which should be notice to user.
            if (i.transactionID != callbacks.p.transactionID)
            {
                // do nothing here.
                return;
            }

            /*
				this purchase failed product is just retrieving purchase.
			*/

            /*
				detect errors.
			*/
            var error = PurchaseError.UnityIAP_Purchase_Unknown;
            var reason = string.Empty;

            switch (failReason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    {
                        error = PurchaseError.UnityIAP_Purchase_PurchasingUnavailable;
                        reason = "The system purchasing feature is unavailable.";
                        break;
                    }
                case PurchaseFailureReason.ExistingPurchasePending:
                    {
                        error = PurchaseError.UnityIAP_Purchase_ExistingPurchasePending;
                        reason = "A purchase was already in progress when a new purchase was requested.";
                        break;
                    }
                case PurchaseFailureReason.ProductUnavailable:
                    {
                        error = PurchaseError.UnityIAP_Purchase_ProductUnavailable;
                        reason = "The product is not available to purchase on the store.";
                        break;
                    }
                case PurchaseFailureReason.SignatureInvalid:
                    {
                        error = PurchaseError.UnityIAP_Purchase_SignatureInvalid;
                        reason = "Signature validation of the purchase's receipt failed.";
                        break;
                    }
                case PurchaseFailureReason.UserCancelled:
                    {
                        error = PurchaseError.UnityIAP_Purchase_UserCancelled;
                        reason = "The user opted to cancel rather than proceed with the purchase.";
                        break;
                    }
                case PurchaseFailureReason.PaymentDeclined:
                    {
                        error = PurchaseError.UnityIAP_Purchase_PaymentDeclined;
                        reason = "There was a problem with the payment.";
                        break;
                    }
                case PurchaseFailureReason.DuplicateTransaction:
                    {
                        error = PurchaseError.UnityIAP_Purchase_DuplicateTransaction;
                        reason = "A duplicate transaction error when the transaction has already been completed successfully.";
                        break;
                    }
                case PurchaseFailureReason.Unknown:
                    {
                        error = PurchaseError.UnityIAP_Purchase_Unknown;
                        reason = "A catch-all for unrecognized purchase problems.";
                        break;
                    }
            }

            switch (routerState)
            {
                default:
                    {
                        // maybe never comes here.
                        routerState = RouterState.PurchaseReady;

                        if (callbacks.purchaseFailed != null)
                        {
                            callbacks.purchaseFailed(error, -1, reason);
                        }
                        break;
                    }
                case RouterState.Purchasing:
                case RouterState.RetrySending:
                    {
                        routerState = RouterState.PurchaseReady;

                        if (callbacks.purchaseFailed != null)
                        {
                            callbacks.purchaseFailed(error, -1, reason);
                        }
                        break;
                    }
            }


            // set failed ticketId to log. this will be used if Paid is occured.
            var currentTransactionId = string.Empty;
            if (i != null && !string.IsNullOrEmpty(i.transactionID))
            {
                onMemoryFailedLog[i.transactionID] = callbacks.ticketId;
                currentTransactionId = i.transactionID;
            }

            /*
                send failed/cancelled ticketId if possible.
            */
            var purchaseCancelledUrl = PurchaseSettings.PURCHASE_URL_CANCEL;
            var dataStr = JsonUtility.ToJson(new PurchaseFailed(callbacks.ticketId, currentTransactionId, reason));
            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_CANCEL_PREFIX + Guid.NewGuid().ToString();

            var cor = HttpPost(
                connectionId,
                purchaseCancelledUrl,
                dataStr,
                (conId, responseData) =>
                {
                    // do nothing.
                },
                (conId, code, errorReason) =>
                {
                    // do nothing.
                }
            );
            enumExecutor(cor);
        }


        /*
			http functions for purchase.
		*/
        private IEnumerator HttpGet(string connectionId, string url, Action<string, object> succeeded, Action<string, int, string> failed, RequestProductInfosAs getAs)
        {
            var header = this.httpRequestHeaderDelegate("GET", url, new Dictionary<string, string>(), string.Empty);

            Action<string, object> onSucceeded = (conId, result) =>
            {
                succeeded(conId, result);
            };

            switch (getAs)
            {
                case RequestProductInfosAs.String:// request string and got string result.
                    return http.Get(
                        connectionId,
                        header,
                        url,
                        (string conId, int code, Dictionary<string, string> respHeaders, string result) =>
                        {
                            httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                        },
                        (conId, code, reason, respHeaders) =>
                        {
                            httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                        },
                        PurchaseSettings.TIMEOUT_SEC
                    );
                case RequestProductInfosAs.Binary:// request byte[] and got byte[] result.
                    return http.GetByBytes(
                        connectionId,
                        header,
                        url,
                        (string conId, int code, Dictionary<string, string> respHeaders, byte[] result) =>
                        {
                            httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                        },
                        (conId, code, reason, respHeaders) =>
                        {
                            httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                        },
                        PurchaseSettings.TIMEOUT_SEC
                    );
                default:
                    throw new Exception("request data should be string or byte[].");
            }
        }

        private IEnumerator HttpPost(string connectionId, string url, object data, Action<string, object> succeeded, Action<string, int, string> failed)
        {
            var header = this.httpRequestHeaderDelegate("POST", url, new Dictionary<string, string>(), data);

            Action<string, object> onSucceeded = (conId, result) =>
            {
                succeeded(conId, result);
            };

            // send string or byte[] to server via http.
            if (data is string)
            {
                return http.Post(
                    connectionId,
                    header,
                    url,
                    (string)data,
                    (string conId, int code, Dictionary<string, string> respHeaders, string result) =>
                    {
                        httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                    },
                    (conId, code, reason, respHeaders) =>
                    {
                        httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                    },
                    PurchaseSettings.TIMEOUT_SEC
                );
            }

            if (data is byte[])
            {
                return http.Post(
                    connectionId,
                    header,
                    url,
                    (byte[])data,
                    (string conId, int code, Dictionary<string, string> respHeaders, byte[] result) =>
                    {
                        httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                    },
                    (conId, code, reason, respHeaders) =>
                    {
                        httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                    },
                    PurchaseSettings.TIMEOUT_SEC
                );
            }

            throw new Exception("should set string or byte[] to data.");
        }
    }
}
