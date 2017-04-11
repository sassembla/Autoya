#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// You must obfuscate your secrets using Window > Unity IAP > Receipt Validation Obfuscator
// before receipt validation will compile in this sample.
// #define RECEIPT_VALIDATION
#endif

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.UI;
#if RECEIPT_VALIDATION
using UnityEngine.Purchasing.Security;
#endif

/// <summary>
/// An example of basic Unity IAP functionality.
/// To use with your account, configure the product ids (AddProduct)
/// and Google Play key (SetPublicKey).
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

	#pragma warning disable 0414
	private bool m_IsGooglePlayStoreSelected;
	#pragma warning restore 0414
	private bool m_IsSamsungAppsStoreSelected;
	private bool m_IsCloudMoolahStoreSelected; 

	private string m_LastTransationID;
	private string m_LastReceipt;
	private string m_CloudMoolahUserName;
	private bool m_IsLoggedIn = false;

	private int m_SelectedItemIndex = -1; // -1 == no product
	private bool m_PurchaseInProgress;
	private Selectable m_InteractableSelectable; // Optimization used for UI state management

	#if RECEIPT_VALIDATION
	private CrossPlatformValidator validator;
	#endif

	/// <summary>
	/// This will be called when Unity IAP has finished initialising.
	/// </summary>
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
		m_Controller = controller;
		m_AppleExtensions = extensions.GetExtension<IAppleExtensions> ();
		m_SamsungExtensions = extensions.GetExtension<ISamsungAppsExtensions> ();
		m_MoolahExtensions = extensions.GetExtension<IMoolahExtension> ();
		m_MicrosoftExtensions = extensions.GetExtension<IMicrosoftExtensions> ();

		InitUI(controller.products.all);

		// On Apple platforms we need to handle deferred purchases caused by Apple's Ask to Buy feature.
		// On non-Apple platforms this will have no effect; OnDeferred will never be called.
		m_AppleExtensions.RegisterPurchaseDeferredListener(OnDeferred);

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
			}
		}

		// Prepare model for purchasing
		if (m_Controller.products.all.Length > 0) 
		{
			m_SelectedItemIndex = 0;
		}

		// Populate the product menu now that we have Products
		for (int t = 0; t < m_Controller.products.all.Length; t++)
		{
			var item = m_Controller.products.all[t];
			var description = string.Format("{0} | {1} => {2}", item.metadata.localizedTitle, item.metadata.localizedPriceString, item.metadata.localizedPrice);

			// NOTE: my options list is created in InitUI
			GetDropdown().options[t] = new Dropdown.OptionData(description);
		}

		// Ensure I render the selected list element
		GetDropdown().RefreshShownValue();

		// Now that I have real products, begin showing product purchase history
		UpdateHistoryUI();

		LogProductDefinitions();
	}

	/// <summary>
	/// This will be called when a purchase completes.
	/// </summary>
	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
	{
		Debug.Log("Purchase OK: " + e.purchasedProduct.definition.id);
		Debug.Log("Receipt: " + e.purchasedProduct.receipt);

		m_LastTransationID = e.purchasedProduct.transactionID;
		m_LastReceipt = e.purchasedProduct.receipt;
		m_PurchaseInProgress = false;

		// Now that my purchase history has changed, update its UI
		UpdateHistoryUI();

		#if RECEIPT_VALIDATION
		// Local validation is available for GooglePlay and Apple stores
		if (m_IsGooglePlayStoreSelected ||
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

					AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
					if (null != apple) {
						Debug.Log(apple.originalTransactionIdentifier);
						Debug.Log(apple.subscriptionExpirationDate);
						Debug.Log(apple.cancellationDate);
						Debug.Log(apple.quantity);
					}
				}
			} catch (IAPSecurityException) {
				Debug.Log("Invalid receipt, not unlocking content");
				return PurchaseProcessingResult.Complete;
			}
		}
		#endif

		// CloudMoolah purchase completion / finishing currently requires using the API 
		// extension IMoolahExtension.RequestPayout to finish a transaction.
		if (m_IsCloudMoolahStoreSelected)
		{
			// Finish transaction with CloudMoolah server
			m_MoolahExtensions.RequestPayOut(e.purchasedProduct.transactionID, 
				(string transactionID, RequestPayOutState state, string message) => {
					if (state == RequestPayOutState.RequestPayOutSucceed) {
						// Finally, finish transaction with Unity IAP's local
						// transaction log, recording the transaction id there
						m_Controller.ConfirmPendingPurchase(e.purchasedProduct);

						// Unlock content here.
					} else {
						Debug.Log("RequestPayOut: failed. transactionID: " + transactionID + 
							", state: " + state + ", message: " + message);
						// Finishing failed. Retry later.
					}
			});
		}

		// You should unlock the content here.

		// Indicate if we have handled this purchase. 
		//   PurchaseProcessingResult.Complete: ProcessPurchase will not be called
		//     with this product again, until next purchase.
		//   PurchaseProcessingResult.Pending: ProcessPurchase will be called 
		//     again with this product at next app launch. Later, call 
		//     m_Controller.ConfirmPendingPurchase(Product) to complete handling
		//     this purchase. Use to transactionally save purchases to a cloud
		//     game service. 
		return PurchaseProcessingResult.Complete;
	}

	/// <summary>
	/// This will be called is an attempted purchase fails.
	/// </summary>
	public void OnPurchaseFailed(Product item, PurchaseFailureReason r)
	{
		Debug.Log("Purchase failed: " + item.definition.id);
		Debug.Log(r);

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

	public void Awake()
	{
		var module = StandardPurchasingModule.Instance();

		// The FakeStore supports: no-ui (always succeeding), basic ui (purchase pass/fail), and 
		// developer ui (initialization, purchase, failure code setting). These correspond to 
		// the FakeStoreUIMode Enum values passed into StandardPurchasingModule.useFakeStoreUIMode.
		module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

		var builder = ConfigurationBuilder.Instance(module);

		// This enables the Microsoft IAP simulator for local testing.
		// You would remove this before building your release package.
		builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = true;
		builder.Configure<IGooglePlayConfiguration>().SetPublicKey("MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2O/9/H7jYjOsLFT/uSy3ZEk5KaNg1xx60RN7yWJaoQZ7qMeLy4hsVB3IpgMXgiYFiKELkBaUEkObiPDlCxcHnWVlhnzJBvTfeCPrYNVOOSJFZrXdotp5L0iS2NVHjnllM+HA1M0W2eSNjdYzdLmZl1bxTpXa4th+dVli9lZu7B7C2ly79i/hGTmvaClzPBNyX+Rtj7Bmo336zh2lYbRdpD5glozUq+10u91PMDPH+jqhx10eyZpiapr8dFqXl5diMiobknw9CgcjxqMTVBQHK6hS0qYKPmUDONquJn280fBs1PTeA6NMG03gb9FLESKFclcuEZtvM8ZwMMRxSLA9GwIDAQAB");
		m_IsGooglePlayStoreSelected = Application.platform == RuntimePlatform.Android && module.androidStore == AndroidStore.GooglePlay;

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
		m_IsCloudMoolahStoreSelected = Application.platform == RuntimePlatform.Android && module.androidStore == AndroidStore.CloudMoolah;

		// Define our products.
		// In this case our products have the same identifier across all the App stores,
		// except on the Mac App store where product IDs cannot be reused across both Mac and
		// iOS stores.
		// So on the Mac App store our products have different identifiers,
		// and we tell Unity IAP this by using the IDs class.
		builder.AddProduct("100.gold.coins", ProductType.Consumable, new IDs
		{
			{"100.gold.coins.mac", MacAppStore.Name},
			{"000000596586", TizenStore.Name},
			{"com.ff", MoolahAppStore.Name},
		});

		builder.AddProduct("500.gold.coins", ProductType.Consumable, new IDs
		{
			{"500.gold.coins.mac", MacAppStore.Name},
			{"000000596581", TizenStore.Name},
			{"com.ee", MoolahAppStore.Name},
		});

		builder.AddProduct("sword", ProductType.NonConsumable, new IDs
		{
			{"sword.mac", MacAppStore.Name},
			{"000000596583", TizenStore.Name},
		});

		builder.AddProduct("subscription", ProductType.Subscription, new IDs
		{
			{"subscription.mac", MacAppStore.Name}
		});
		
		// Write Amazon's JSON description of our products to storage when using Amazon's local sandbox.
		// This should be removed from a production build.
		builder.Configure<IAmazonConfiguration>().WriteSandboxJSON(builder.products);

		// This enables simulated purchase success for Samsung IAP.
		// You would remove this, or set to SamsungAppsMode.Production, before building your release package.
		builder.Configure<ISamsungAppsConfiguration>().SetMode(SamsungAppsMode.AlwaysSucceed);
		// This records whether we are using Samsung IAP. Currently ISamsungAppsExtensions.RestoreTransactions
		// displays a blocking Android Activity, so: 
		// A) Unity IAP does not automatically restore purchases on Samsung Galaxy Apps
		// B) IAPDemo (this) displays the "Restore" GUI button for Samsung Galaxy Apps
		m_IsSamsungAppsStoreSelected = Application.platform == RuntimePlatform.Android && module.androidStore == AndroidStore.SamsungApps;


		// This selects the GroupId that was created in the Tizen Store for this set of products
		// An empty or non-matching GroupId here will result in no products available for purchase
		builder.Configure<ITizenStoreConfiguration>().SetGroupId("100000085616");


		#if RECEIPT_VALIDATION
		string appIdentifier;
		#if UNITY_5_6_OR_NEWER
		appIdentifier = Application.identifier;
		#else
		appIdentifier = Application.bundleIdentifier;
		#endif
		validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), appIdentifier);
		#endif

		// Now we're ready to initialize Unity IAP.
		UnityPurchasing.Initialize(this, builder);
	}

	/// <summary>
	/// This will be called after a call to IAppleExtensions.RestoreTransactions().
	/// </summary>
	private void OnTransactionsRestored(bool success)
	{
		Debug.Log("Transactions restored.");
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

	private void InitUI(IEnumerable<Product> items)
	{
		// Disable the UI while IAP is initializing
		// See also UpdateInteractable()
		m_InteractableSelectable = GetDropdown(); // References any one of the disabled components

		// Show Restore button on supported platforms
		if (! (Application.platform == RuntimePlatform.IPhonePlayer ||
			   Application.platform == RuntimePlatform.OSXPlayer ||
			   Application.platform == RuntimePlatform.tvOS || 
			   Application.platform == RuntimePlatform.WSAPlayerX86 ||
			   Application.platform == RuntimePlatform.WSAPlayerX64 ||
			   Application.platform == RuntimePlatform.WSAPlayerARM ||
			m_IsSamsungAppsStoreSelected  || m_IsCloudMoolahStoreSelected) )
		{
			GetRestoreButton().gameObject.SetActive(false);
		}

		// Show Register, Login, and Validate buttons on CloudMoolah platform
		GetRegisterButton().gameObject.SetActive(m_IsCloudMoolahStoreSelected);
		GetLoginButton().gameObject.SetActive(m_IsCloudMoolahStoreSelected);
		GetValidateButton().gameObject.SetActive(m_IsCloudMoolahStoreSelected);

		foreach (var item in items)
		{
			// Add initial pre-IAP-initialization content. Update later in OnInitialized.
			var description = string.Format("{0} - {1}", item.definition.id, item.definition.type);

			GetDropdown().options.Add(new Dropdown.OptionData(description));
		}

		// Ensure I render the selected list element
		GetDropdown().RefreshShownValue();

		GetDropdown().onValueChanged.AddListener((int selectedItem) => {
			Debug.Log("OnClickDropdown item " + selectedItem);
			m_SelectedItemIndex = selectedItem;
		});

		// Initialize my button event handling
		GetBuyButton().onClick.AddListener(() => { 
			if (m_PurchaseInProgress == true) {
				Debug.Log("Please wait, purchasing ...");
				return;
			}

			// For CloudMoolah, games utilizing a connected backend game server may wish to login.
			// Standalone games may not need to login.
			if (m_IsCloudMoolahStoreSelected && m_IsLoggedIn == false)
			{
				Debug.LogWarning("CloudMoolah purchase notifications will not be forwarded server-to-server. Login incomplete.");
			}

			// Don't need to draw our UI whilst a purchase is in progress.
			// This is not a requirement for IAP Applications but makes the demo
			// scene tidier whilst the fake purchase dialog is showing.
			m_PurchaseInProgress = true;
			m_Controller.InitiatePurchase(m_Controller.products.all[m_SelectedItemIndex], "aDemoDeveloperPayload"); 
		});

		if (GetRestoreButton() != null)
		{
			GetRestoreButton().onClick.AddListener(() => {
				if (m_IsCloudMoolahStoreSelected)
				{
					if (m_IsLoggedIn == false)
					{
						Debug.LogError("CloudMoolah purchase restoration aborted. Login incomplete.");
					}
					else
					{
						// Restore abnornal transaction identifer, if Client don't receive transaction identifer.
						m_MoolahExtensions.RestoreTransactionID((RestoreTransactionIDState restoreTransactionIDState) => {
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
				else
				{
					m_AppleExtensions.RestoreTransactions(OnTransactionsRestored);
				}
			});
		}

		// CloudMoolah requires user registration and supports login to manage the user's
		// digital wallet. The CM store also supports remote receipt validation.

		// CloudMoolah user registration extension, to establish digital wallet
		// This is a "fast" registration, requiring only a password. Users may provide 
		// more detail including an email address during the purchase flow, a "slow" registration, if desired. 
		if (GetRegisterButton() != null)
		{
			GetRegisterButton().onClick.AddListener (() => {
				// Provide a unique password to establish the user's account.
				// Typically, connected games (with backend game servers), may already
				// have available a user-token, which could be supplied here.
				m_MoolahExtensions.FastRegister("CMPassword", RegisterSucceeded, RegisterFailed);
			});
		}

		// CloudMoolah user login extension, to access existing digital wallet
		if (GetLoginButton() != null)
		{
			GetLoginButton().onClick.AddListener (() => {
				m_MoolahExtensions.Login(m_CloudMoolahUserName, "CMPassword", LoginResult);
			});
		}

		// CloudMoolah remote purchase receipt validation, to determine if the purchase is fraudulent 
		// NOTE: Remote validation only available for CloudMoolah currently. For local validation, 
		// see ProcessPurchase.
		if (GetValidateButton() != null)
		{
			GetValidateButton ().onClick.AddListener (() => {
				// Remotely validate the last transaction and receipt.
				m_MoolahExtensions.ValidateReceipt(m_LastTransationID, m_LastReceipt, 
					(string transactionID, ValidateReceiptState state, string message) => {
						Debug.Log("ValidtateReceipt transactionID:" + transactionID 
							+ ", state:" + state.ToString() + ", message:" + message);
				});
			});
		}
	}

	public void LoginResult (LoginResultState state, string errorMsg)
	{
		if(state == LoginResultState.LoginSucceed)
		{
			m_IsLoggedIn = true;
		}
		else
		{
			m_IsLoggedIn = false;
		}	
		Debug.Log ("LoginResult: state: " + state.ToString () + " errorMsg: " + errorMsg);
	}

	public void RegisterSucceeded(string cmUserName)
	{
		Debug.Log ("RegisterSucceeded: cmUserName = " + cmUserName);
		m_CloudMoolahUserName = cmUserName;
	}

	public void RegisterFailed (FastRegisterError error, string errorMessage)
	{
		Debug.Log ("RegisterFailed: error = " + error.ToString() + ", errorMessage = " + errorMessage);
	}

	public void UpdateHistoryUI()
	{
		if (m_Controller == null)
		{
			return;
		}

		var itemText = "Item\n\n";
		var countText = "Purchased\n\n";

		for (int t = 0; t < m_Controller.products.all.Length; t++)
		{
			var item = m_Controller.products.all [t];

			// Collect history status report

			itemText += "\n\n" + item.definition.id;
			countText += "\n\n" + item.hasReceipt.ToString();
		}

		// Show history
		GetText(false).text = itemText;
		GetText(true).text = countText;
	}

	protected void UpdateInteractable()
	{
		if (m_InteractableSelectable == null)
		{
			return;
		}

		bool interactable = m_Controller != null;
		if (interactable != m_InteractableSelectable.interactable)
		{
			if (GetRestoreButton() != null)
			{
				GetRestoreButton().interactable = interactable;
			}
			GetBuyButton().interactable = interactable;
			GetDropdown().interactable = interactable;
			GetRegisterButton().interactable = interactable;
			GetLoginButton().interactable = interactable;
		}
	}

	public void Update()
	{
		UpdateInteractable();
	}

	private Dropdown GetDropdown()
	{
		return GameObject.Find("Dropdown").GetComponent<Dropdown>();
	}

	private Button GetBuyButton()
	{
		return GameObject.Find("Buy").GetComponent<Button>();
	}

	/// <summary>
	/// Gets the restore button when available
	/// </summary>
	/// <returns><c>null</c> or the restore button.</returns>
	private Button GetRestoreButton()
	{
		return GetButton ("Restore");
	}

	private Button GetRegisterButton()
	{
		return GetButton ("Register");
	}

	private Button GetLoginButton()
	{
		return GetButton ("Login");
	}

	private Button GetValidateButton()
	{
		return GetButton ("Validate");
	}

	private  Button GetButton(string buttonName)
	{
		GameObject obj = GameObject.Find(buttonName);
		if (obj != null)
		{
			return obj.GetComponent <Button>();
		}
		else
		{
			return null;
		}
	}

	private Text GetText(bool right)
	{
		var which = right ? "TextR" : "TextL";
		return GameObject.Find(which).GetComponent<Text>();
	}

	private void LogProductDefinitions()
	{
		var products = m_Controller.products.all;
		foreach (var product in products) {
#if UNITY_5_6_OR_NEWER
			Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\nenabled: {3}\n", product.definition.id, product.definition.storeSpecificId, product.definition.type.ToString(), product.definition.enabled ? "enabled" : "disabled"));
#else
			Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\n", product.definition.id, product.definition.storeSpecificId, product.definition.type.ToString()));
#endif
		}
	}
}
