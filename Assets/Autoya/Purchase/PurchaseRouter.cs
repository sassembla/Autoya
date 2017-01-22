using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;
using UnityEngine.Purchasing;

/**
    purchase feature of Autoya.
    should be use as singleton.

    depends on HTTP api.
    not depends on Autoya itself.
*/
namespace AutoyaFramework.Purchase {
    [Serializable] public struct ProductInfos {
        [SerializeField] public ProductInfo[] productInfos;
    }

    [Serializable] public struct ProductInfo {
        [SerializeField] public string productId;
        [SerializeField] public string platformProductId;

        public ProductInfo (string productId, string platformProductId) {
            this.productId = productId;
            this.platformProductId = platformProductId;
        }
    }
    
    public class PurchaseRouter : IStoreListener {
        
        [Serializable] public struct PurchaseFailed {
            public string ticketId;
            public string reason;
            public PurchaseFailed (string ticketId, string reason) {
                this.ticketId = ticketId;
                this.reason = reason;
            }
        }
        
        public enum RouterState {
            None,
            LoadingReady,
            
            LoadingProducts,
            FailedToLoadProducts,
            
            LoadingStore,
            FailedToLoadStore,
            

            PurchaseReady,

            
            GettingTransaction,
            Purchasing,
        }

        public enum PurchaseError {
            Offline,
            FailedToBoot,
            UnavailableProduct,
            AlreadyPurchasing,
            TicketGetError,

            /*
                Unty IAP initialize errors.
            */
            UnityIAP_Initialize_AppNowKnown,
            UnityIAP_Initialize_NoProductsAvailable,
            UnityIAP_Initialize_PurchasingUnavailable,


            /*
                Unity IAP Purchase errors.
            */
            UnityIAP_Purchase_PurchasingUnavailable,
            UnityIAP_Purchase_ExistingPurchasePending,
            UnityIAP_Purchase_ProductUnavailable,
            UnityIAP_Purchase_SignatureInvalid,
            UnityIAP_Purchase_UserCancelled,
            UnityIAP_Purchase_PaymentDeclined,
            UnityIAP_Purchase_Unknown,

            UnknownError
        }

        private RouterState routerState = RouterState.None;
        public RouterState State () {
            return routerState;
        }
        
        private readonly string storeKind;

        private readonly Autoya.HttpRequestHeaderDelegate requestHeader;
        private Dictionary<string, string> BasicRequestHeaderDelegate (HttpMethod method, string url, Dictionary<string, string> requestHeader, string data) {
            return requestHeader;
        }

        private readonly HTTPConnection http;


        private Action readyPurchase;
        private Action<PurchaseError, string, AutoyaStatus> failedToReady;

        private readonly Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate;

        private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed) {
            if (200 <= httpCode && httpCode < 299) {
                succeeded(connectionId, data);
                return;
            }
            failed(connectionId, httpCode, errorReason, new AutoyaStatus());
        }

        private readonly string storeId;
        private readonly Action<IEnumerator> enumExecutor;

        private readonly Func<string, string> onTicketResponse;
        /**
            constructor.

            Func<string, Dictionary<string, string>> requestHeader func is used to get request header from outside of this feature. 
            by default it returns empty headers.

            also you can modify http error handling via httpResponseHandlingDelegate.
            by default, http response code 200 ~ 299 is treated as success, and other codes are treated as network error.
         */
        public PurchaseRouter (
            Action<IEnumerator> executor,
            Func<string, ProductInfo[]> onLoadProducts,
            Func<string, string> onTicketResponse,
            Action onPurchaseReady, 
            Action<PurchaseError, string, AutoyaStatus> onPurchaseReadyFailed,
            Autoya.HttpRequestHeaderDelegate httpGetRequestHeaderDeletage=null, 
            Autoya.HttpResponseHandlingDelegate httpResponseHandlingDelegate =null
        ) {
            this.storeId = Guid.NewGuid().ToString();
            // Debug.LogError("start storeId:" + storeId);
            this.enumExecutor = executor;
            
            /*
                set store kind by platform.
            */
            #if UNITY_EDITOR
                this.storeKind = AppleAppStore.Name;
            #elif UNITY_IOS
                this.storeKind = AppleAppStore.Name;
            #elif UNITY_ANDROID
                this.storeKind = GooglePlay.Name;
            #endif

            if (httpGetRequestHeaderDeletage != null) {
                this.requestHeader = httpGetRequestHeaderDeletage;
            } else {
                this.requestHeader = BasicRequestHeaderDelegate;
            }

            this.http = new HTTPConnection();
            
            if (httpResponseHandlingDelegate != null) {
                this.httpResponseHandlingDelegate = httpResponseHandlingDelegate;
            } else {
                this.httpResponseHandlingDelegate = BasicResponseHandlingDelegate;
            }

            this.onTicketResponse = onTicketResponse;

            var cor = _Ready(onLoadProducts, onPurchaseReady, onPurchaseReadyFailed);
            enumExecutor(cor);
        }
        
        private IEnumerator _Ready (
            Func<string, ProductInfo[]> onLoadProducts,
            Action reloaded, 
            Action<PurchaseError, string, AutoyaStatus> reloadFailed
        ) {
            if (routerState == RouterState.PurchaseReady) {
                // IAP features are already running.
                // no need to reload.
                reloaded();
                yield break;
            }

            this.readyPurchase = reloaded;
            this.failedToReady = reloadFailed;
            routerState = RouterState.LoadingProducts;
            
            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_READY_PREFIX + Guid.NewGuid().ToString();
            var url = PurchaseSettings.PURCHASE_URL_READY;
            
            
            var cor = HttpGet(
                connectionId,
                url, 
                (conId, data) => {
                    var products = onLoadProducts(data);
                    ReadyIAPFeature(products);
                },
                (conId, code, reason, autoyaStatus) => {
                    Debug.LogError("failed, code:" + code + " reason:" + reason);
                    routerState = RouterState.FailedToLoadProducts;
                    failedToReady(PurchaseError.UnknownError, reason, autoyaStatus);
                }
            );

            while (cor.MoveNext()) {
                yield return null;
            }
        }
        
        private void ReadyIAPFeature (ProductInfo[] productInfos) {
            routerState = RouterState.LoadingStore;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var productInfo in productInfos) {
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
            if (Application.internetReachability == NetworkReachability.NotReachable) {
                failedToReady(PurchaseError.Offline, "network is offline.", new AutoyaStatus());
                return;
            }

            UnityPurchasing.Initialize(this, builder);
        }
        private IStoreController controller;
        private IExtensionProvider extensions;
        
        public bool IsPurchaseReady () {
            return routerState == RouterState.PurchaseReady;
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        public void OnInitialized (IStoreController controller, IExtensionProvider extensions) {
            this.controller = controller;
            this.extensions = extensions;

            routerState = RouterState.PurchaseReady;
            if (readyPurchase != null) readyPurchase();
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        public void OnInitializeFailed (InitializationFailureReason error) {
            routerState = RouterState.FailedToLoadStore;
            switch (error) {
                case InitializationFailureReason.AppNotKnown: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_AppNowKnown, "The store reported the app as unknown.", new AutoyaStatus());
                    break;
                }
                case InitializationFailureReason.NoProductsAvailable: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_NoProductsAvailable, "No products available for purchase.", new AutoyaStatus());
                    break;
                }
                case InitializationFailureReason.PurchasingUnavailable: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_PurchasingUnavailable, "In-App Purchases disabled in device settings.", new AutoyaStatus());
                    break;
                }
            }
        }
        
        /**
            start purchase.
        */
        

        public IEnumerator PurchaseAsync (
            string purchaseId, 
            string productId, 
            Action<string> purchaseSucceeded, 
            Action<string, PurchaseError, string, AutoyaStatus> purchaseFailed
        ) {
            if (Application.internetReachability == NetworkReachability.NotReachable) {
                purchaseFailed(purchaseId, PurchaseError.Offline, "network is offline.", new AutoyaStatus());
                yield break;
            }

            if (routerState != RouterState.PurchaseReady) {
                switch (routerState) {
                    case RouterState.GettingTransaction:
                    case RouterState.Purchasing: {
                        purchaseFailed(purchaseId, PurchaseError.AlreadyPurchasing, "purchasing another product now. wait then retry.", new AutoyaStatus());
                        break;
                    }
                    default: {
                        purchaseFailed(purchaseId, PurchaseError.UnknownError, "state is:" + routerState, new AutoyaStatus());
                        break;
                    }
                }
                yield break;
            }

            
            if (false) {
                purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "this product is not available.", new AutoyaStatus());
                yield break;
            }
            
            // renew callback.
            callbacks = new Callbacks(null, string.Empty, string.Empty, tId => {}, (tId, error, reason, autoyaStatud) => {});
            
            var ticketUrl = PurchaseSettings.PURCHASE_URL_TICKET;
            var data = productId;

            routerState = RouterState.GettingTransaction;

            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_TICKET_PREFIX + Guid.NewGuid().ToString();

            var cor = HttpPost(
                connectionId,
                ticketUrl,
                data,
                (conId, resultData) => {
                    var ticketId = onTicketResponse(resultData);
                    
                    TicketReceived(purchaseId, productId, ticketId, purchaseSucceeded, purchaseFailed);
                },
                (conId, code, reason, autoyaStatus) => {
                    Debug.LogWarning("ふおーーー場合分けエラー出すの忘れてた、怖い。conId:" + conId + " code:" + code + " reason:" + reason);
                    purchaseFailed(purchaseId, PurchaseError.TicketGetError, "failed to purchase.", autoyaStatus);
                    routerState = RouterState.PurchaseReady;
                }
            );
            while (cor.MoveNext()) {
                yield return null;
            }
        }
        
        private struct Callbacks {
            public readonly Product p;
            public readonly string ticketId;
            public readonly Action purchaseSucceeded;
            public readonly Action<PurchaseError, string, AutoyaStatus> purchaseFailed;
            public Callbacks (Product p, string purchaseId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, string, AutoyaStatus> purchaseFailed) {
                this.p = p;
                this.ticketId = ticketId;
                this.purchaseSucceeded = () => {
                    purchaseSucceeded(purchaseId);
                };
                this.purchaseFailed = (err, reason, autoyaStatus) => {
                    purchaseFailed(purchaseId, err, reason, autoyaStatus);
                };
            }
        }

        private Callbacks callbacks = new Callbacks(null, string.Empty, string.Empty, tId => {}, (tId, error, reason, autoyaStatus) => {});
        private void TicketReceived (string purchaseId, string productId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, string, AutoyaStatus> purchaseFailed) {
            var product = this.controller.products.WithID(productId);
            if (product != null) {
                if (product.availableToPurchase) {
                    routerState = RouterState.Purchasing;

                    /*
                        renew callback.
                    */
                    callbacks = new Callbacks(product, purchaseId, ticketId, purchaseSucceeded, purchaseFailed);
                    this.controller.InitiatePurchase(product, ticketId);
                    return;
                }
            }
            
            purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "selected product is not available.", new AutoyaStatus());
            routerState = RouterState.PurchaseReady;
        }
        
        [Serializable] private struct Ticket {
            [SerializeField] private string ticketId;
            [SerializeField] private string data;
            public Ticket (string ticketId, string data) {
                this.ticketId = ticketId;
                this.data = data;
            }
            public Ticket (string data) {
                this.ticketId = string.Empty;
                this.data = data;
            }
        }

        /// <summary>
        /// Called when a purchase completes.
        ///
        /// May be called at any time after OnInitialized().
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e) {
            if (callbacks.p == null) {
                SendPaidTicket(e);
                return PurchaseProcessingResult.Pending;
            }

            if (e.purchasedProduct.transactionID != callbacks.p.transactionID) {
                SendPaidTicket(e);
                return PurchaseProcessingResult.Pending;
            }
            
            /*
                this process is the process for the purchase which this router is just retrieving.
                proceed deploy asynchronously.
            */
            switch (routerState) {
                case RouterState.Purchasing: {
                    var purchasedUrl = PurchaseSettings.PURCHASE_URL_PURCHASE;
                    var dataStr = JsonUtility.ToJson(new Ticket(callbacks.ticketId, e.purchasedProduct.receipt));

                    var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PURCHASE_PREFIX + Guid.NewGuid().ToString();
                    
                    var cor = HttpPost(
                        connectionId,
                        purchasedUrl,
                        dataStr,
                        (conId, responseData) => {
                            var product = e.purchasedProduct;
                            controller.ConfirmPendingPurchase(e.purchasedProduct);

                            if (callbacks.purchaseSucceeded != null) {
                                callbacks.purchaseSucceeded();
                            }
                            routerState = RouterState.PurchaseReady;
                        },
                        (conId, code, reason, autoyaStatus) => {
                            // 通信が失敗したら、アイテムがdeployできてないので、再度送り出す必要がある。自動リトライが必須。
                            Debug.LogError("failed to deploy. code:" + code + " reason:" + reason);
                        }
                    );
                    enumExecutor(cor);
                    break;
                }
                default: {
                    Debug.LogError("ここにくるケースを見切れていない。");
                    if (callbacks.purchaseFailed != null) {
                        callbacks.purchaseFailed(PurchaseError.UnknownError, "failed to deploy product. state:" + routerState, new AutoyaStatus());
                    }
                    break;
                }
            }

            /*
                always pending.
            */
            return PurchaseProcessingResult.Pending;
        }

        private void SendPaidTicket (PurchaseEventArgs e) {
            Debug.LogError("storeId:" + storeId + " get paid & uncompleted purchase. e:" + e.purchasedProduct.definition.id);// 確認したいところ。ストアインスタンスが他にもある、という状況に陥っている。
            // どうすることで回避できるか。ストアインスタンスを2つ作らない、とかか。うーん、、あ、停止させればいいのか。わざわざ。
            var purchasedUrl = PurchaseSettings.PURCHASE_URL_PAID;
            var dataStr = JsonUtility.ToJson(new Ticket(e.purchasedProduct.receipt));

            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PAID_PREFIX + Guid.NewGuid().ToString();

            var cor = HttpPost(
                connectionId,
                purchasedUrl,
                dataStr,
                (conId, responseData) => {
                    var product = e.purchasedProduct;
                    controller.ConfirmPendingPurchase(e.purchasedProduct);
                },
                (conId, code, reason, autoyaStatus) => {
                    // systems do this process again automatically.
                    // no need to do something.
                }
            );
            enumExecutor(cor);
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        public void OnPurchaseFailed (Product i, PurchaseFailureReason failReason) {
            // no retrieving product == not purchasing.
            if (callbacks.p == null) {
                // do nothing here.
                return;
            }

            // transactionID does not match to retrieving product's transactionID,
            // it's not the product which should be notice to user.
            if (i.transactionID != callbacks.p.transactionID) {
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
            
            switch (failReason) {
                case PurchaseFailureReason.PurchasingUnavailable: {
                    error = PurchaseError.UnityIAP_Purchase_PurchasingUnavailable;
                    reason = "The system purchasing feature is unavailable.";
                    break;
                }
                case PurchaseFailureReason.ExistingPurchasePending: {
                    error = PurchaseError.UnityIAP_Purchase_ExistingPurchasePending;
                    reason = "A purchase was already in progress when a new purchase was requested.";
                    break;
                }
                case PurchaseFailureReason.ProductUnavailable: {
                    error = PurchaseError.UnityIAP_Purchase_ProductUnavailable;
                    reason = "The product is not available to purchase on the store.";
                    break;
                }
                case PurchaseFailureReason.SignatureInvalid: {
                    error = PurchaseError.UnityIAP_Purchase_SignatureInvalid;
                    reason = "Signature validation of the purchase's receipt failed.";
                    break;
                }
                case PurchaseFailureReason.UserCancelled: {
                    error = PurchaseError.UnityIAP_Purchase_UserCancelled;
                    reason = "The user opted to cancel rather than proceed with the purchase.";
                    break;
                }
                case PurchaseFailureReason.PaymentDeclined: {
                    error = PurchaseError.UnityIAP_Purchase_PaymentDeclined;
                    reason = "There was a problem with the payment.";
                    break;
                }
                case PurchaseFailureReason.Unknown: {
                    error = PurchaseError.UnityIAP_Purchase_Unknown;
                    reason = "A catch-all for unrecognized purchase problems.";
                    break;
                }
            }

            switch (routerState) {
                default: {
                    Debug.LogError("ここにくるケースを見切れていない2。");
                    if (callbacks.purchaseFailed != null) { 
                        callbacks.purchaseFailed(error, reason, new AutoyaStatus());
                    }
                    break;
                }
                case RouterState.Purchasing: {
                    if (callbacks.purchaseFailed != null) {
                        callbacks.purchaseFailed(error, reason, new AutoyaStatus());
                    }
                    routerState = RouterState.PurchaseReady;
                    break;
                }
            }

            /*
                send failed/cancelled ticketId if possible.
            */
            var purchaseCancelledUrl = PurchaseSettings.PURCHASE_URL_CANCEL;
            var dataStr = JsonUtility.ToJson(new PurchaseFailed(callbacks.ticketId, reason));
            var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_CANCEL_PREFIX + Guid.NewGuid().ToString();

            var cor = HttpPost(
                connectionId,
                purchaseCancelledUrl,
                dataStr,
                (conId, responseData) => {
                    // do nothing.
                },
                (conId, code, errorReason, autoyaStatus) => {
                    // do nothing.
                }
            );
            enumExecutor(cor);
        }


        /*
            http functions for purchase.
        */
        private IEnumerator HttpGet (string connectionId, string url, Action<string, string> succeeded, Action<string, int, string, AutoyaStatus> failed) {
            var header = this.requestHeader(HttpMethod.Get, url, new Dictionary<string, string>(), string.Empty);

            Action<string, object> onSucceeded = (conId, result) => {
                succeeded(conId, result as string);
            };

            return http.Get(
                connectionId,
                header,
                url,
                (conId, code, respHeaders, result) => {
                    httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                },
                (conId, code, reason, respHeaders) => {
                    httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                },
                PurchaseSettings.TIMEOUT_SEC
            );
        }
    
        private IEnumerator HttpPost (string connectionId, string url, string data, Action<string, string> succeeded, Action<string, int, string, AutoyaStatus> failed) {
            var header = this.requestHeader(HttpMethod.Post, url, new Dictionary<string, string>(), data);
            
            Action<string, object> onSucceeded = (conId, result) => {
                succeeded(conId, result as string);
            };

            return http.Post(
                connectionId,
                header,
                url,
                data,
                (conId, code, respHeaders, result) => {
                    httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                },
                (conId, code, reason, respHeaders) => {
                    httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                },
                PurchaseSettings.TIMEOUT_SEC
            );
        }
    }
}