using System;
using System.Collections;
using AutoyaFramework.Purchase;
using UnityEngine;

namespace AutoyaFramework {
	public enum PurchaseFeatureState {
		None,
		Loading,
		Ready,
		Reloading,
		Closed
	}
	
	public partial class Autoya {
		/*
			Purchase implementation.
		*/
		private PurchaseRouter _purchaseRouter;
		
		private PurchaseFeatureState purchaseFeatureState = PurchaseFeatureState.None;
		
		private int purchaseReadyRetryCount;
		private void ReloadPurchasability () {
			purchaseFeatureState = PurchaseFeatureState.Loading;

			PurchaseRouter.HttpRequestHeaderDelegate httpRequestHeaderDel = (p1, p2, p3, p4) => {
				return httpRequestHeaderDelegate(p1, p2, p3, p4);
			};
			PurchaseRouter.HttpResponseHandlingDelegate httpResponseHandlingDel = (p1, p2, p3, p4, p5, p6, p7) => {
				httpResponseHandlingDelegate(p1, p2, p3, p4, p5, p6, p7);
			};

			_purchaseRouter = new PurchaseRouter(
				mainthreadDispatcher.Commit,
				productSourceData => {
					/*
						handle received product datas to OverridePoint.
					*/
					return OnLoadProductsResponse(productSourceData);
				},
				ticketData => {
					return OnTicketResponse(ticketData);
				},
				() => {
					purchaseReadyRetryCount = 0;
					purchaseFeatureState = PurchaseFeatureState.Ready;
					OnPurchaseReady();
				}, 
				(err, reason, autoyaStatus) => {
					purchaseFeatureState = PurchaseFeatureState.Reloading;
					
					if (purchaseReadyRetryCount == PurchaseSettings.PEADY_MAX_RETRY_COUNT) {
						purchaseReadyRetryCount = 0;
						OnPurchaseReadyFailed(err, reason, autoyaStatus);
						return;
					}

					mainthreadDispatcher.Commit(ReloadPurchaseFeature());
				},
				httpRequestHeaderDel, 
				httpResponseHandlingDel
			);
		}

		private IEnumerator ReloadPurchaseFeature () {
			purchaseReadyRetryCount++;

			// wait 2 ^ retryCount sec.
			yield return new WaitForSeconds((float)Math.Pow(2, purchaseReadyRetryCount));

			ReloadPurchasability();

			while (_purchaseRouter.IsPurchaseReady()) {
				yield return null;
			}
		}

		private void AttemptRetryPurchaseReady () {
			mainthreadDispatcher.Commit(ReloadPurchaseFeature());
		}


		/*
			public apis.
		*/

		public static PurchaseFeatureState Purchase_State () {
			return autoya.purchaseFeatureState;
		}
		
		public static bool Purchase_IsReady () {
			if (autoya == null) {
				return false;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				return false;
			}

			if (autoya.purchaseFeatureState != PurchaseFeatureState.Ready) {
				return false;
			}
			
			if (!autoya._purchaseRouter.IsPurchaseReady()) {
				return false;
			}

			return true;
		}

		public static ProductInfo[] Purchase_ProductInfos () {
			if (autoya == null) {
				return new ProductInfo[]{};
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				return new ProductInfo[]{};
			}

			return autoya._purchaseRouter.ProductInfos();
		}

		/**
			attempt ready purchase feature if ready purchase was failed.
		*/
		public static void Purchase_AttemptReady () {
			if (Purchase_IsReady()) return;
			autoya.AttemptRetryPurchaseReady();
		}
		
		
		/**
			purchase item in asynchronously.
			
			string purchaseId: you can set id for this purchase. this param will back in done or failed handler.
			string productId: platform-shard product id string.
			Action<string> done: fire when purchase is done in succeessful. string is purchaseId.
			Action<string, PurchaseRouter.PurchaseError, string, AutoyaStatus> failed: fire when purchase is failed. 1st string is purchaseId.
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

			if (autoya.purchaseFeatureState != PurchaseFeatureState.Ready) {
				Debug.LogWarning("not yet. 3");
				return;
			}

			var purchasability = autoya._purchaseRouter.IsPurchaseReady();
			if (!purchasability) {
				Debug.LogWarning("not yet. 4");
				return;
			}

			autoya.mainthreadDispatcher.Commit(autoya._purchaseRouter.PurchaseAsync(purchaseId, productId, done, failed));
		}

		/**
			do not use this method in actual use.
			this method is only for testing.
		*/
		public static void Purchase_Shutdown () {
			if (autoya == null) {
				return;
			}

			autoya.purchaseFeatureState = PurchaseFeatureState.Closed;
			autoya._purchaseRouter = null;
		}
	}
}