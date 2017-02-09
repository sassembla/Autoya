#if UNITY_PURCHASING
using UnityEngine.Events;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
	[RequireComponent (typeof (Button))]
	[AddComponentMenu("Unity IAP/IAP Button")]
	[HelpURL("https://docs.unity3d.com/Manual/UnityIAP.html")]
	public class IAPButton : MonoBehaviour
	{
		[System.Serializable]
		public class OnPurchaseCompletedEvent : UnityEvent<Product> {};

		[System.Serializable]
		public class OnPurchaseFailedEvent : UnityEvent<Product, PurchaseFailureReason> {};

		[HideInInspector]
		public string productId;

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

		void Start ()
		{
			Button button = GetComponent<Button>();
			if (button) {
				button.onClick.AddListener(PurchaseProduct);
			}

			if (string.IsNullOrEmpty(productId)) {
				Debug.LogError("IAPButton productId is empty");
			}

			if (!IAPButtonStoreManager.Instance.HasProductInCatalog(productId)) {
				Debug.LogWarning("The product catalog has no product with the ID \"" + productId + "\"");
			}
		}

		void OnEnable()
		{
			IAPButtonStoreManager.Instance.AddButton(this);

			var product = IAPButtonStoreManager.Instance.GetProduct(productId);
			if (product != null) {
				if (titleText != null) {
					titleText.text = product.metadata.localizedTitle;
				}

				if (descriptionText != null) {
					descriptionText.text = product.metadata.localizedDescription;
				}

				if (priceText != null) {
					priceText.text = product.metadata.localizedPriceString;
				}
			}
		}

		void OnDisable()
		{
			IAPButtonStoreManager.Instance.RemoveButton(this);
		}

		void PurchaseProduct()
		{
			Debug.Log("IAPButton.PurchaseProduct() with product ID: " + productId);

			IAPButtonStoreManager.Instance.InitiatePurchase(productId);
		}

		/**
		 *  Invoked to process a purchase of the product associated with this button
		 */
		public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
		{
			Debug.Log(string.Format("IAPButton.ProcessPurchase(PurchaseEventArgs {0} - {1})", e, e.purchasedProduct.definition.id));

			onPurchaseComplete.Invoke(e.purchasedProduct);

			return (consumePurchase) ? PurchaseProcessingResult.Complete : PurchaseProcessingResult.Pending;
		}

		/**
		 *  Invoked on a failed purchase of the product associated with this button
		 */
		public void OnPurchaseFailed (Product product, PurchaseFailureReason reason)
		{
			Debug.Log(string.Format("IAPButton.OnPurchaseFailed(Product {0}, PurchaseFailureReason {1})", product, reason));

			onPurchaseFailed.Invoke(product, reason);
		}

		public class IAPButtonStoreManager : IStoreListener
		{
			private static IAPButtonStoreManager instance = new IAPButtonStoreManager();
			private ProductCatalog catalog;
			private List<IAPButton> activeButtons = new List<IAPButton>();
			
			protected IStoreController controller;
			protected IExtensionProvider extensions;

			private IAPButtonStoreManager()
			{
				catalog = ProductCatalog.LoadDefaultCatalog();

				StandardPurchasingModule module = StandardPurchasingModule.Instance();
				module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

				ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);
				foreach (var product in catalog.allProducts) {
					if (product.allStoreIDs.Count > 0) {
						var ids = new IDs();
						foreach (var storeID in product.allStoreIDs) {
							ids.Add(storeID.id, storeID.store);
						}
						builder.AddProduct(product.id, product.type, ids);
					} else {
						builder.AddProduct(product.id, product.type);
					}
				}
				UnityPurchasing.Initialize (this, builder);
			}

			public static IAPButtonStoreManager Instance {
				get {
					return instance;
				}
			}

			public IStoreController StoreController {
				get {
					return controller;
				}
			}

			public IExtensionProvider ExtensionProvider {
				get {
					return extensions;
				}
			}

			public bool HasProductInCatalog(string productID)
			{
				foreach (var product in catalog.allProducts) {
					if (product.id == productID) {
						return true;
					}
				}
				return false;
			}

			public Product GetProduct(string productID)
			{
				if (controller != null) {
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

			public void InitiatePurchase(string productID)
			{
				controller.InitiatePurchase(productID);
			}

			public void OnInitialized (IStoreController controller, IExtensionProvider extensions)
			{
				this.controller = controller;
				this.extensions = extensions;
			}

			public void OnInitializeFailed (InitializationFailureReason error)
			{
				Debug.LogError("Purchasing failed to initialize.");
			}

			public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
			{
				foreach (var button in activeButtons) {
					if (button.productId == e.purchasedProduct.definition.id) {
						return button.ProcessPurchase(e);
					}
				}
				return PurchaseProcessingResult.Complete; // TODO: Maybe this shouldn't return complete
			}

			public void OnPurchaseFailed (Product product, PurchaseFailureReason reason)
			{ 
				foreach (var button in activeButtons) {
					if (button.productId == product.definition.id) {
						button.OnPurchaseFailed(product, reason);
					}
				} 
			}
		}
	}
}
#endif
