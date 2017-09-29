using System;
using System.Collections;
using AutoyaFramework.Purchase;
using UnityEngine;

namespace AutoyaFramework {
	public enum PurchaseFeatureState {
		Loading,

		Ready,
		ReadyRetry,

		ReadyFailed,

		Closing,
		Closed
	}
	
	public partial class Autoya {
		/*
			Purchase implementation.
		*/
		private PurchaseRouter _purchaseRouter;
		
		private PurchaseFeatureState purchaseState;

		private static AutoyaStatus purchaseErrorStatus = new AutoyaStatus();

		private void ReloadPurchasability () {
			purchaseState = PurchaseFeatureState.Loading;

			purchaseErrorStatus = new AutoyaStatus();
			
			PurchaseRouter.HttpRequestHeaderDelegate httpRequestHeaderDel = (p1, p2, p3, p4) => {
				return httpRequestHeaderDelegate(p1, p2, p3, p4);
			};
			
			PurchaseRouter.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
				Action<string, int, string, AutoyaStatus> p7dash = (q1, q2, q3, status) => {
					// set autoyaStatus error if exist.
					if (status.HasError()) {
						purchaseErrorStatus = status;
					}
					
					p7(q1, q2, q3);
				};
				autoya.httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7dash);
			};

			_purchaseRouter = new PurchaseRouter(
				mainthreadDispatcher.Commit,
				productSourceData => OnLoadProductsResponse(productSourceData),
				ticketData => OnTicketResponse(ticketData),
				() => {
					purchaseState = PurchaseFeatureState.Ready;
					OnPurchaseReady();
				}, 
				(err, code, reason) => {
					purchaseState = PurchaseFeatureState.ReadyFailed;

					var cor = OnPurchaseReadyFailed(err, code, reason, purchaseErrorStatus);
					mainthreadDispatcher.Commit(cor);
				},
				httpRequestHeaderDel, 
				httpResponseHandlingDel
			);
		}



		/*
			public apis.
		*/

		public static bool Purchase_IsReady () {
			if (autoya == null) {
				return false;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				return false;
			}

			if (autoya.purchaseState != PurchaseFeatureState.Ready) {
				return false;
			}

			if (!autoya._purchaseRouter.IsPurchaseReady()) {
				return false;
			}

			return true;
		}

		public static bool Purchase_NeedAttemptReadyPurchase () {
			if (autoya.purchaseState == PurchaseFeatureState.ReadyFailed) {
				return true;
			}
			return false;
		}

		public static ProductInfo[] Purchase_ProductInfos () {
			if (autoya == null) {
				return new ProductInfo[]{};
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				return new ProductInfo[]{};
			}
			if (autoya.purchaseState != PurchaseFeatureState.Ready) {
				return new ProductInfo[]{};
			}
			if (!autoya._purchaseRouter.IsPurchaseReady()) {
				return new ProductInfo[]{};
			}

			return autoya._purchaseRouter.ProductInfos();
		}
		
		public static void Purchase_AttemptReadyPurcase () {
			if (autoya.purchaseState == PurchaseFeatureState.ReadyFailed) {
				autoya.ReloadPurchasability();
			}
		}
		
		/**
			purchase item asynchronously.
			
			string purchaseId: the id for this purchase. this param will back in done or failed handler.
			string productId: platform-shard product id string.
			Action<string> done: fire when purchase is done in succeessful. string is purchaseId.
			Action<string, PurchaseRouter.PurchaseError, string> failed: fire when purchase is failed. 1st string is purchaseId.
		*/
		public static void Purchase (string purchaseId, string productId, Action<string> done, Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed) {
			if (autoya == null) {
				Debug.LogWarning("not yet. 1");
				// var cor = new AssetBundleLoadErrorInstance(assetName, "Autoya is null.", loadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);
				return;
			}

			if (!Autoya.Auth_IsAuthenticated()) {
				Debug.LogWarning("not yet. 2");
				// var cor = new AssetBundleLoadErrorInstance(assetName, "not authenticated.", loadFailed).Coroutine();
				// autoya.mainthreadDispatcher.Commit(cor);				
				return;
			}

			if (autoya.purchaseState != PurchaseFeatureState.Ready) {
				Debug.LogWarning("not yet. 3");
				return;
			}

			if (!autoya._purchaseRouter.IsPurchaseReady()) {
				Debug.LogWarning("not yet. 4");
				return;
			}

			purchaseErrorStatus = new AutoyaStatus();

			Action<string, PurchaseRouter.PurchaseError, string> _failed = (p1, p2, p3) => {
				failed(p1, p2, p3, purchaseErrorStatus);
			};

			autoya.mainthreadDispatcher.Commit(
				autoya._purchaseRouter.PurchaseAsync(purchaseId, productId, done, _failed)
			);
		}

		/**
			do not use this method in actual use.
			this method is only for testing.
		*/
		public static void Purchase_DEBUG_Shutdown () {
			if (autoya == null) {
				return;
			}

			autoya._purchaseRouter = null;
		}

		public static void Purchase_DEBUG_Reload () {
			if (autoya == null) {
				return;
			}

			autoya.ReloadPurchasability();
		}
	}
}