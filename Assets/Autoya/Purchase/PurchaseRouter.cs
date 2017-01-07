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
    public class PurchaseRouter : IStoreListener {
        private class HandleErrorFlowClass : IHTTPErrorFlow {
            // define nothing. use default error handling.
        }

        private readonly HandleErrorFlowClass flowInstance;
        
        private void HandleErrorFlow (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed) {
            flowInstance.HandleErrorFlow(connectionId, responseHeaders, httpCode, data, errorReason, succeeded, failed);
        }
        
        [Serializable] public struct ProductInfo {
            [SerializeField] public string productId;
            [SerializeField] public string platformProductId;

            public ProductInfo (string productId, string platformProductId) {
                this.productId = productId;
                this.platformProductId = platformProductId;
            }
        }

        [Serializable] public struct PurchaseFailed {
            public string ticketId;
            public string reason;
            public PurchaseFailed (string ticketId, string reason) {
                this.ticketId = ticketId;
                this.reason = reason;
            }
        }
        
        private enum RouterState {
            None,
            LoadingProducts,
            LoadingStore,

            PurchaseReady,
            NotReady,
            GettingTransaction,
            Purchasing,
            PurchaseCancelled,
        }

        public enum PurchaseError {
            Offline,
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

        private readonly string storeKind;
        
        private readonly Action<IEnumerator> runEnumerator;

        private readonly Func<string, Dictionary<string, string>> requestHeader = data => {
            return new Dictionary<string, string>();
        };
        private readonly HTTPConnection http;


        private Action readyPurchase;
        private Action<PurchaseError, string> failedToReady;
        
        /**
            constructor.

            it's good to set non-null newEnumRunner for running http IEnumerator under detailed control.
            if newEnumRunner is null, this class creates own GameObject("PurchaseRouter"), and then add MonoBehaviour for running.

            Func<string, Dictionary<string, string>> requestHeader func is used to get request header from outside of this feature. 
            by default it returns empty headers.

            also you can modify http error handling via flowInstance.
            by default, http response code 200 ~ 299 is treated as success, and other codes are treated as network error.
         */
        public PurchaseRouter (Action onPurchaseReady, Action<PurchaseError, string> onPurchaseReadyFailed, Action<IEnumerator> newEnumRunner=null, Func<string, Dictionary<string, string>> newRequestHeader=null, IHTTPErrorFlow flowInstance=null) {
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

            
            if (newEnumRunner != null) {
                this.runEnumerator = newEnumRunner;
            } else {
                /*
                    make naked MonoBehaviour and set DontDestroyOnLoad.
                */
                var go = GameObject.Find("PurchaseRouter");
                if (go == null) {
                    go = new GameObject("PurchaseRouter");
                    GameObject.DontDestroyOnLoad(go);
                }
                
                var runner = go.AddComponent<MonoBehaviour>();
                this.runEnumerator = iEnum => runner.StartCoroutine(iEnum);
            }

            if (newRequestHeader != null) {
                this.requestHeader = newRequestHeader;
            }

            this.http = new HTTPConnection();
            
            /*
                if flowInstance is set, use these http error handling. else, use default http error handling.
            */
            if (flowInstance != null) {
                this.flowInstance = flowInstance as HandleErrorFlowClass;
            } else {
                this.flowInstance = new HandleErrorFlowClass();
            }

            Reload(
                () => onPurchaseReady(),
                (err, reason) => onPurchaseReadyFailed(err, reason)
            );
        }
        
        public void Reload (Action reloaded, Action<PurchaseError, string> reloadFailed) {
            if (routerState == RouterState.PurchaseReady) {
                // IAP features are already running.
                // no need to reload.
                reloaded();
                return;
            }

            this.readyPurchase = reloaded;
            this.failedToReady = reloadFailed;
            routerState = RouterState.LoadingProducts;
            
            var connectionId = "purchase_ready_" + Guid.NewGuid().ToString();

            var url = "https://httpbin.org/get";
            HttpGet(
                connectionId,
                url, 
                (conId, data) => {
                    Debug.LogWarning("アイテム取得したのでデータを入れる。現在はダミーAPIが適当なデータを入れてくるのを想定してる。サーバがプラットフォームdependsなデータを返してくる想定。そのほうがいいっしょ。");
                    var productInfos = new ProductInfo[]{
                        new ProductInfo("100_gold_coins", "100_gold_coins_iOS")
                    };

                    ReadyIAPFeature(productInfos);
                },
                (conId, code, reason) => {
                    Debug.LogError("failed, code:" + code + " reason:" + reason);
                    routerState = RouterState.NotReady;
                    failedToReady(PurchaseError.UnknownError, reason);
                }
            );
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
                failedToReady(PurchaseError.Offline, "network is offline.");
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
            routerState = RouterState.NotReady;
            switch (error) {
                case InitializationFailureReason.AppNotKnown: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_AppNowKnown, "The store reported the app as unknown.");
                    break;
                }
                case InitializationFailureReason.NoProductsAvailable: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_NoProductsAvailable, "No products available for purchase.");
                    break;
                }
                case InitializationFailureReason.PurchasingUnavailable: {
                    failedToReady(PurchaseError.UnityIAP_Initialize_PurchasingUnavailable, "In-App Purchases disabled in device settings.");
                    break;
                }
            }
        }
        
        /**
            start purchase.
        */
        public void PurchaseAsync (string purchaseId, string productId, Action<string> purchaseSucceeded, Action<string, PurchaseError, string> purchaseFailed) {
            if (Application.internetReachability == NetworkReachability.NotReachable) {
                purchaseFailed(purchaseId, PurchaseError.Offline, "network is offline.");
                return;
            }

            if (routerState != RouterState.PurchaseReady) {
                switch (routerState) {
                    case RouterState.GettingTransaction:
                    case RouterState.Purchasing: {
                        purchaseFailed(purchaseId, PurchaseError.AlreadyPurchasing, "purchasing another product now. wait then retry.");
                        break;
                    }
                    default: {
                        purchaseFailed(purchaseId, PurchaseError.UnknownError, "state is:" + routerState);
                        break;
                    }
                }
                return;
            }

            /*
                該当するproductを購買させる許可があるかどうか、という以前に、このクラスにいろいろbindするチャンス。
            */
            Debug.LogWarning("該当するproductを購買させる許可があるかどうか。事前に取得しておいたアイテムリストで判断する。");
            if (false) {
                purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "this product is not available.");
                return;
            }
            
            // renew callback.
            callbacks = new Callbacks(null, string.Empty, string.Empty, tId => {}, (tId, error, reason) => {});
            
            var transactionUrl = "https://httpbin.org/post";
            var data = productId;

            routerState = RouterState.GettingTransaction;

            var connectionId = "purchase_start_" + Guid.NewGuid().ToString();

            HttpPost(
                connectionId,
                transactionUrl,
                data,
                (conId, resultData) => {
                    Debug.LogWarning("ticketの取得完了 resultData:" + resultData);
                    var ticketId = resultData;
                    
                    TicketReceived(purchaseId, productId, ticketId, purchaseSucceeded, purchaseFailed);
                },
                (conId, code, reason) => {
                    Debug.LogWarning("ふおーーー場合分けエラー出すの忘れてた、怖い。conId:" + conId + " code:" + code + " reason:" + reason);
                    purchaseFailed(purchaseId, PurchaseError.TicketGetError, "failed to purchase.");
                    routerState = RouterState.PurchaseReady;
                }
            );
        }
        
        private struct Callbacks {
            public readonly Product p;
            public readonly string ticketId;
            public readonly Action purchaseSucceeded;
            public readonly Action<PurchaseError, string> purchaseFailed;
            public Callbacks (Product p, string purchaseId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, string> purchaseFailed) {
                this.p = p;
                this.ticketId = ticketId;
                this.purchaseSucceeded = () => {
                    purchaseSucceeded(purchaseId);
                };
                this.purchaseFailed = (err, reason) => {
                    purchaseFailed(purchaseId, err, reason);
                };
            }
        }

        private Callbacks callbacks = new Callbacks(null, string.Empty, string.Empty, tId => {}, (tId, error, reason) => {});
        private void TicketReceived (string purchaseId, string productId, string ticketId, Action<string> purchaseSucceeded, Action<string, PurchaseError, string> purchaseFailed) {
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
            
            purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "selected product is not available.");
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
                    var purchasedUrl = "https://httpbin.org/post";
                    var dataStr = JsonUtility.ToJson(new Ticket(callbacks.ticketId, e.purchasedProduct.receipt));

                    var connectionId = "purchase_succeeded_" + Guid.NewGuid().ToString();
                    
                    HttpPost(
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
                        (conId, code, reason) => {
                            // 通信が失敗したら、アイテムがdeployできてないので、再度送り出す必要がある。自動リトライが必須。
                            Debug.LogError("failed to deploy. code:" + code + " reason:" + reason);
                        }
                    );
                    break;
                }
                default: {
                    Debug.LogError("ここにくるケースを見切れていない。");
                    if (callbacks.purchaseFailed != null) {
                        callbacks.purchaseFailed(PurchaseError.UnknownError, "failed to deploy product. state:" + routerState);
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
            Debug.LogError("get paid & uncompleted purchase. e:" + e);
            var purchasedUrl = "https://httpbin.org/post";
            var dataStr = JsonUtility.ToJson(new Ticket(e.purchasedProduct.receipt));

            var connectionId = "purchase_paid_" + Guid.NewGuid().ToString();

            HttpPost(
                connectionId,
                purchasedUrl,
                dataStr,
                (conId, responseData) => {
                    var product = e.purchasedProduct;
                    controller.ConfirmPendingPurchase(e.purchasedProduct);
                },
                (conId, code, reason) => {
                    // systems do this process again automatically.
                    // no need to do something.
                }
            );
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
                        callbacks.purchaseFailed(error, reason);
                    }
                    break;
                }
                case RouterState.Purchasing: {
                    if (callbacks.purchaseFailed != null) {
                        callbacks.purchaseFailed(error, reason);
                    }
                    routerState = RouterState.PurchaseReady;
                    break;
                }
            }

            /*
                send failed/cancelled ticketId if possible.
            */
            var purchaseCancelledUrl = "https://httpbin.org/post";
            var dataStr = JsonUtility.ToJson(new PurchaseFailed(callbacks.ticketId, reason));
            var connectionId = "purchase_cancelled_" + Guid.NewGuid().ToString();

            HttpPost(
                connectionId,
                purchaseCancelledUrl,
                dataStr,
                (conId, responseData) => {
                    // do nothing.
                },
                (conId, code, errorReason) => {
                    // do nothing.
                }
            );
        }


        /*
            http functions for purchase.
        */
        private void HttpGet (string connectionId, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
            var header = this.requestHeader(string.Empty);
            Action<string, object> onSucceeded = (conId, result) => {
                succeeded(conId, result as string);
            };

            var httpGetEnum = http.Get(
                connectionId,
                header,
                url,
                (conId, code, respHeaders, result) => {
                    flowInstance.HandleErrorFlow(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                },
                (conId, code, reason, respHeaders) => {
                    flowInstance.HandleErrorFlow(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                },
                10.0
            );

            this.runEnumerator(httpGetEnum);
        }
    
        private void HttpPost (string connectionId, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
            var header = this.requestHeader(data);
            Action<string, object> onSucceeded = (conId, result) => {
                succeeded(conId, result as string);
            };

            var httpGetEnum = http.Post(
                connectionId,
                header,
                url,
                data,
                (conId, code, respHeaders, result) => {
                    flowInstance.HandleErrorFlow(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
                },
                (conId, code, reason, respHeaders) => {
                    flowInstance.HandleErrorFlow(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
                },
                10.0
            );

            this.runEnumerator(httpGetEnum);
        }
    }
}