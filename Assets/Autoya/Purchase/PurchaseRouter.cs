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
namespace AutoyaFramework.Purchase {
	/**
		struct for product data.
	*/
	[Serializable] public struct ProductInfo {
		[SerializeField] public string productId;
		[SerializeField] public string platformProductId;
		[SerializeField] public bool isAvailableToThisPlayer;
		[SerializeField] public string info;

		public ProductInfo (string productId, string platformProductId, bool isPurchasableToThisPlayer, string info) {
			this.productId = productId;
			this.platformProductId = platformProductId;
			this.isAvailableToThisPlayer = isPurchasableToThisPlayer;
			this.info = info;
		}
	}

	/**
		serialized product info struct.
	 */
	[Serializable] public struct ProductInfos {
		[SerializeField] public ProductInfo[] productInfos;
	}

	
	
	public class PurchaseRouter : IStoreListener {
		
		/*
			delegate for supply http request header generate func for modules.
		*/
		public delegate Dictionary<string, string> HttpRequestHeaderDelegate (string method, string url, Dictionary<string, string> requestHeader, string data);
		
		/*
			delegate for handle http response for modules.
		*/
		public delegate void HttpResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed);
		
		private readonly HttpRequestHeaderDelegate httpRequestHeaderDelegate;
		private readonly HttpResponseHandlingDelegate httpResponseHandlingDelegate;
		
		
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

		public enum PurchaseReadyError {
			Offline,
			FailedToGetProducts,
			
			/*
				Unty IAP initialize errors.
			*/
			UnityIAP_Initialize_AppNowKnown,
			UnityIAP_Initialize_NoProductsAvailable,
			UnityIAP_Initialize_PurchasingUnavailable,
		}

		public enum PurchaseError {
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
		public RouterState State () {
			return routerState;
		}

		private readonly string storeKind;

		private Dictionary<string, string> BasicRequestHeaderDelegate (string method, string url, Dictionary<string, string> requestHeader, string data) {
			return requestHeader;
		}

		private readonly HTTPConnection http;


		private Action readyPurchase;
		private Action<PurchaseReadyError, int, string> failedToReady;

		private void BasicResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed) {
			if (200 <= httpCode && httpCode < 299) {
				succeeded(connectionId, data);
				return;
			}
			failed(connectionId, httpCode, errorReason);
		}

		private readonly string storeId;
		private readonly Action<IEnumerator> enumExecutor;
		private readonly Func<string, string> onTicketResponse;

		private ProductInfo[] verifiedProducts;

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
			Action<PurchaseReadyError, int, string> onPurchaseReadyFailed,
			HttpRequestHeaderDelegate httpGetRequestHeaderDeletage=null, 
			HttpResponseHandlingDelegate httpResponseHandlingDelegate =null
		) {
			this.storeId = Guid.NewGuid().ToString();
			
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
				this.httpRequestHeaderDelegate = httpGetRequestHeaderDeletage;
			} else {
				this.httpRequestHeaderDelegate = BasicRequestHeaderDelegate;
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

		public ProductInfo[] ProductInfos () {
			return verifiedProducts;
		}
		
		private IEnumerator _Ready (
			Func<string, ProductInfo[]> onLoadProducts,
			Action reloaded, 
			Action<PurchaseReadyError, int, string> failedToReady
		) {
			if (routerState == RouterState.PurchaseReady) {
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
			
			
			var cor = HttpGet(
				connectionId,
				url, 
				(conId, data) => {
					var products = onLoadProducts(data);
					this.verifiedProducts = products;
					ReadyIAPFeature(products);
				},
				(conId, code, reason) => {
					routerState = RouterState.FailedToGetProducts;
					this.failedToReady(PurchaseReadyError.FailedToGetProducts, code, reason);
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
				failedToReady(PurchaseReadyError.Offline, 0, "network is offline.");
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
			if (readyPurchase != null) {
				readyPurchase();
			}
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
					failedToReady(PurchaseReadyError.UnityIAP_Initialize_AppNowKnown, 0, "The store reported the app as unknown.");
					break;
				}
				case InitializationFailureReason.NoProductsAvailable: {
					failedToReady(PurchaseReadyError.UnityIAP_Initialize_NoProductsAvailable, 0, "No products available for purchase.");
					break;
				}
				case InitializationFailureReason.PurchasingUnavailable: {
					failedToReady(PurchaseReadyError.UnityIAP_Initialize_PurchasingUnavailable, 0, "In-App Purchases disabled in device settings.");
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
			Action<string, PurchaseError, string> purchaseFailed
		) {
			if (Application.internetReachability == NetworkReachability.NotReachable) {
				if (purchaseFailed != null) {
					purchaseFailed(purchaseId, PurchaseError.Offline, "network is offline.");
				}
				yield break;
			}

			if (routerState != RouterState.PurchaseReady) {
				switch (routerState) {
					case RouterState.GettingTransaction:
					case RouterState.Purchasing: {
						if (purchaseFailed != null) {
							purchaseFailed(purchaseId, PurchaseError.AlreadyPurchasing, "purchasing another product now. wait then retry.");
						}
						break;
					}
					default: {
						if (purchaseFailed != null) {
							purchaseFailed(purchaseId, PurchaseError.UnknownError, "state is:" + routerState);
						}
						break;
					}
				}
				yield break;
			}

			
			if (verifiedProducts != null) {
				var verified = false;
				foreach (var verifiedProduct in verifiedProducts) {
					if (verifiedProduct.productId == productId && verifiedProduct.isAvailableToThisPlayer) {
						verified = true;
					}
				}

				if (!verified) {
					if (purchaseFailed != null) {
						purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "this product is not available. productId:" + productId);
					}
					yield break;
				}
			}
			
			// renew callback.
			callbacks = new Callbacks(null, string.Empty, string.Empty, tId => {}, (tId, error, reason) => {});
			

			/*
				start getting ticket for purchase.
			 */
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
				(conId, code, reason) => {
					routerState = RouterState.PurchaseReady;
					if (purchaseFailed != null) {
						purchaseFailed(purchaseId, PurchaseError.TicketGetError, "code:" + code + " reason:" + reason);
					}
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
			
			routerState = RouterState.PurchaseReady;
			if (purchaseFailed != null) {
				purchaseFailed(purchaseId, PurchaseError.UnavailableProduct, "selected product is not available.");
			}
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
				SendPaid(e);
				return PurchaseProcessingResult.Pending;
			}

			if (e.purchasedProduct.transactionID != callbacks.p.transactionID) {
				SendPaid(e);
				return PurchaseProcessingResult.Pending;
			}
			
			/*
				this process is the purchase just retrieving now.
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
							routerState = RouterState.PurchaseReady;

							if (callbacks.purchaseSucceeded != null) {
								callbacks.purchaseSucceeded();
							}
						},
						(conId, code, reason) => {
							// OK以外のコードについてはcompleteを行わず、リトライ。
							routerState = RouterState.RetrySending;
							
							StartRetry(callbacks, e);
						}
					);
					enumExecutor(cor);
					break;
				}
				default: {
					// maybe never comes here.
					if (callbacks.purchaseFailed != null) {
						callbacks.purchaseFailed(PurchaseError.UnknownError, "failed to deploy purchased item case 2. state:" + routerState);
					}
					break;
				}
			}

			/*
				always pending.
			*/
			return PurchaseProcessingResult.Pending;
		}

		private void StartRetry (Callbacks callbacks, PurchaseEventArgs e) {
			var cor = RetryCoroutine(callbacks, e);
			enumExecutor(cor);
		}

		private IEnumerator RetryCoroutine (Callbacks callbacks, PurchaseEventArgs e) {
			var count = 0;

			retry: {
				var waitSec = Mathf.Pow(count, 2);
				count++;
				
				yield return new WaitForSeconds(waitSec);

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

						routerState = RouterState.PurchaseReady;

						if (callbacks.purchaseSucceeded != null) {
							callbacks.purchaseSucceeded();
						}
					},
					(conId, code, reason) => {
						// still in RetrySending state.
					}
				);

				while (cor.MoveNext()) {
					yield return null;
				}

				if (routerState != RouterState.RetrySending) {
					// retry finished.
					yield break;
				}

				// still in retry.
				if (count == PurchaseSettings.PURCHASED_MAX_RETRY_COUNT) {
					routerState = RouterState.RetryFailed;

					if (callbacks.purchaseFailed != null) {
						callbacks.purchaseFailed(PurchaseError.RetryFailed, "failed to retry purchase process.");
					}
					yield break;
				}

				// continue retry.
				goto retry;
			}
		}

		public Action _completed;

		private void SendPaid (PurchaseEventArgs e) {
			var purchasedUrl = PurchaseSettings.PURCHASE_URL_PAID;
			var dataStr = JsonUtility.ToJson(new Ticket(e.purchasedProduct.receipt));

			var connectionId = PurchaseSettings.PURCHASE_CONNECTIONID_PAID_PREFIX + Guid.NewGuid().ToString();
			
			var cor = HttpPost(
				connectionId,
				purchasedUrl,
				dataStr,
				(conId, responseData) => {
					controller.ConfirmPendingPurchase(e.purchasedProduct);
					if (_completed != null) {
						_completed();
					}
				},
				(conId, code, reason) => {
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
				case PurchaseFailureReason.DuplicateTransaction: {
					error = PurchaseError.UnityIAP_Purchase_DuplicateTransaction;
					reason = "A duplicate transaction error when the transaction has already been completed successfully.";
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
					// maybe never comes here.
					routerState = RouterState.PurchaseReady;

					if (callbacks.purchaseFailed != null) { 
						callbacks.purchaseFailed(error, reason);
					}
					break;
				}
				case RouterState.Purchasing:
				case RouterState.RetrySending: {
					routerState = RouterState.PurchaseReady;

					if (callbacks.purchaseFailed != null) {
						callbacks.purchaseFailed(error, reason);
					}
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
				(conId, code, errorReason) => {
					// do nothing.
				}
			);
			enumExecutor(cor);
		}


		/*
			http functions for purchase.
		*/
		private IEnumerator HttpGet (string connectionId, string url, Action<string, string> succeeded, Action<string, int, string> failed) {
			var header = this.httpRequestHeaderDelegate("GET", url, new Dictionary<string, string>(), string.Empty);

			Action<string, object> onSucceeded = (conId, result) => {
				succeeded(conId, result as string);
			};

			return http.Get(
				connectionId,
				header,
				url,
				(string conId, int code, Dictionary<string, string> respHeaders, string result) => {
					httpResponseHandlingDelegate(conId, respHeaders, code, result, string.Empty, onSucceeded, failed);
				},
				(conId, code, reason, respHeaders) => {
					httpResponseHandlingDelegate(conId, respHeaders, code, string.Empty, reason, onSucceeded, failed);
				},
				PurchaseSettings.TIMEOUT_SEC
			);
		}
	
		private IEnumerator HttpPost (string connectionId, string url, string data, Action<string, string> succeeded, Action<string, int, string> failed) {
			var header = this.httpRequestHeaderDelegate("POST", url, new Dictionary<string, string>(), data);
			
			Action<string, object> onSucceeded = (conId, result) => {
				succeeded(conId, result as string);
			};

			return http.Post(
				connectionId,
				header,
				url,
				data,
				(string conId, int code, Dictionary<string, string> respHeaders, string result) => {
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