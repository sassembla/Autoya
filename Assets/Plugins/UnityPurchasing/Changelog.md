## [2.2.7] - 2021-01-27

### Changed

- Amazon - Include SDK version 2.0.76. Continuing to investigate sensitivity to slow-network conditions.

### Fixed
- GooglePlay - Purchasing silently failing, showing `Failed to purchase unknown product {0}: System.Collections.Generic.List``1[UnityEngine.AndroidJavaObject]` in log, not calling `IStoreListener.OnPurchaseFailed`, after prior purchase when offline, after Initializing, until app has been restarted.
- GooglePlay - `IStoreController.FetchAdditionalProducts` cleared receipts inappropriately.
- Apple - Exception seen when purchasing and initializing with existing purchases (e.g. Non-Consumables) on devices set to Thai language. Internal logging of `DateTime` used `CurrentCulture` instead of `InvariantCulture`. Removed logs.

## [2.2.6] - 2021-01-14

### Changed
- GooglePlay - OnSetupFailed called immediately when Unity IAP fails to connect to the underlying service. To handle users who are logged-out of Google Play we stop the initialization process quickly.

### Fixed
- GooglePlay - ConfirmPendingPurchase failed to acknowledge purchases after purchase and app restart.
- GooglePlay - ProcessPurchase unexpectely called when player backgrounded and foregrounded app before the initialization phase finished.
- GooglePlay - Initialization incorrectly reported as failing, while race-condition returned fetched product metadata simultaneously.
- Apple - "ArgumentOutOfRangeException: Not a valid calendar for the given culture" during Apple receipt validation (`CrossPlatformValidator.Validate` > `AppleValidator.Validate` > `AppleReceiptParser.Parse`) on devices set to Thai locale.

## [2.2.5] - 2020-12-14

### Fixed
- GooglePlay - Fails to initialize when the player has made no purchases
- GooglePlay - Missing ProcessPurchase callback at app start when the transaction is (1) purchased, (2) processed by the app with a ProcessPurchaseResult.Pending result, (3) the app is terminated, and (4) the app is restarted
- GooglePlay - NullReferenceException from "FillPurchases" (logged internal API) when returning from background, unpredictably
- Apple - Unity IAP 2.2.2's Apple Silicon fix not included in release; continuous integration pipeline fixed
- `StandardPurchasingModule.appStore` returns `AppStore.MacAppStore` for Mac App Store, `AppStore.AppleAppStore` for iOS App Store, and `AppStore.WinRT` for Windows Desktop. (No change to 
`AppStore.SamsungApps`, `AppStore.AmazonAppStore`, or `AppStore.GooglePlay`.)

## [2.2.4] - 2020-12-03

### Fixed
- GooglePlay - `IStoreListener.ProcessPurchase` called more than once for any purchase which is not consumed, i.e. when `ProcessPurchaseResult.Pending` is returned, by fixing a race-condition.

### Changed
- GooglePlay - To receive `ProcessPurchase` calls after foregrounding the app, when a purchase is made outside the app (e.g. in the Play Store app), please upgrade the core package via the Package Manager to `com.unity.purchasing@2.2.1` or higher. 

## [2.2.3] - 2020-12-01

### Fixed
- GooglePlay - `IStoreListener.OnInitialized` is now called after all purchases have been fetched from the store.
- GooglePlay - `IStoreListener.OnPurchaseFailed` would not be called in projects where the purchased product's ID differed from its Store Specific ID.
- GooglePlay - All receipts were corrupt after a purchase of a Consumable, until the next Android `Activity.onResume` lifecycle event, if the user had previously bought a Subscription.
- GooglePlay - Fix `MissingMethodException` seen in projects using `com.unity.purchasing@2.0.3` through `2.2.0` when a user launches their game after having previously purchased a Product.

## [2.2.2] - 2020-11-20

### Fixed
- Mac App Store - Support for Apple Silicon, `arm64` slice added to binary
- GooglePlay - ProductMetadata.localizedPrice no longer floors fractions, e.g. "$19.99" was "$19.00"
- GooglePlay - Fix several purchasing-flow issues
   - OnPurchaseFailed not called after canceled purchase
   - Exceptions shown in logcat after normal purchase
   - Purchases of consumables left incomplete, resulting in "product already owned" errors
- GooglePlay - Fix initialization when [Android] "Player Settings > Publishing Settings > Minification: Proguard" is enabled. "AndroidJavaException: java.lang.ClassNotFoundException: com.android.billingclient.api.PurchasesUpdatedListener"


## [2.2.1] - 2020-11-13

### Fixed
- GooglePlay - ProductMetadata.localizedPrice always `0` 
- GooglePlay - "Main" thread warning seen in IStoreListener.OnInitialized and related callbacks.

### Added
- GooglePlay - Subscription metadata is now available in `GoogleProductMetadata` from `ProductMetadata.GetGoogleProductMetadata()` via `IStoreController.products`.
   - For example, use `GoogleProductMetadata googleMetadata = storeController.product.all[0].metadata.GetGoogleProductMetadata();` now instead of the deprecated, `IGooglePlayStoreExtensions.GetProductJSONDictionary`.
   - string originalJson - Note, a single SkuDetails JSON, instead of multiple from `GetProductJSONDictionary` 
   - string subscriptionPeriod
   - string freeTrialPeriod
   - string introductoryPrice
   - string introductoryPricePeriod
   - int introductoryPriceCycles

## [2.2.0] - 2020-11-06

### Changed
- GooglePlay - Upgrades to Google Play Billing Library v3.0.1. This replaces the Google Play AIDL implementation.
   - The transaction identifier is always purchaseToken. This replaces the v2.1.0 orderId behavior. See `IGooglePlayConfiguration` changes below.
   - Adds new and deprecates `IGooglePlayStoreExtensions` APIs
      - Deprecated - disabled.
         - `IGooglePlayStoreExtensions.IsOwned`
         - `IGooglePlayStoreExtensions.SetLogLevel`
         - `IGooglePlayStoreExtensions.GetProductJSONDictionary` - use the `ProductMetadata` of `product.metadata` from `IStoreController.products`
      - Deprecated - will be removed in future release.
         - `IGooglePlayStoreExtensions.FinishAdditionalTransaction`
      - New variant (signature)
         - `IGooglePlayStoreExtensions.UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode)` - allow refined proration mode for subscription upgrade or downgrade as described in the [Google ProrationMode documentation](https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.ProrationMode)
      - New methods
         - `IGooglePlayStoreExtensions.ConfirmSubscriptionPriceChange(string productId, Action<bool> callback)` - prompt users to confirm a price change for their subscription
         - `IGooglePlayStoreExtensions.SetDeferredPurchaseListener(Action<Product> action)` - listen for new pending out-of-app purchases, consider notifying users here
         - `IGooglePlayStoreExtensions.SetObfuscatedAccountId(string accountId)` - to help Google detect and reduce irregular activities when making a purchase
         - `IGooglePlayStoreExtensions.SetObfuscatedProfileId(string profileId)` - to help Google detect and reduce irregular activities when making a purchase
   - deprecates `IGooglePlayConfiguration` APIs
      - Deprecated - disabled.
         - `IGooglePlayConfiguration.SetPublicKey`
         - `IGooglePlayConfiguration.aggressivelyRecoverLostPurchases`
         - `IGooglePlayConfiguration.UsePurchaseTokenForTransactionId`

### Fixed
- IAP Catalog - GooglePlay - pricing template when exporting to CSV, now sets autofill pricing to `false` instead of `true`
- GooglePlay - Subscription receipts will update, e.g. after an upgrade or downgrade, whenever the player pauses or resumes their app. See this change reflected in the `product.receipt` of `IStoreController.products`.

### Added 
- Apple Macos - Support for building IL2CPP on MacOS

## [2.1.1] - 2020-10-23

### Fixed
- Amazon - Fix build failure caused by duplicate classes
- Amazon - Fix ResponseReceiver flaw reported by Amazon APK audit caused by permission attribute location in AndroidManifest.xml 

## [2.1.0] - 2020-10-14
### Future
- GooglePlay - Transaction IDs for all Google Play transactions will be switched to use Google's Purchase Token in a future version of Unity IAP. Google's Order ID previously was used when possible. This version introduces a feature to switch to Purchase Token now, and also to automatically use Purchase Token if `aggressivelyRecoverLostPurchases = true`.

### Changed

- GooglePlay - Live payments using `aggressivelyRecoverLostPurchases = true` - switched to Google's Purchase Token from using Google's Order ID to represent all transaction IDs. Automatically sets `Product.transactionID` to GooglePlay `purchaseToken` when `aggressivelyRecoverLostPurchases` is `true`. Continues to use `orderId`, otherwise. CLARIFICATION: To reinforce the preferred usage of `aggressivelyRecoverLostPurchases`, a de-duplicating backend purchase verification server is recommended to be added to a game's transaction verification pipeline when using this feature. Without such a server the recovered purchases may not be easily or safely de-duplicated by a client.
   - When upgrading from previous versions of Unity IAP, and if enabled `bool IGooglePlayConfiguration.aggressivelyRecoverLostPurchases`, any purchases your users have made will be processed again by Unity IAP; ProcessPurchase will be called for all these purchases.
   - Afterwards, for future purchases, this change reduces future duplicate processing calls; ProcessPurchase should no longer be called after an interrupted purchase for an item already purcahsed by the user. 
  - Update purchase verification servers to treat orderId and purchaseToken as the same ID. Extract the purchaseToken from the JSON receipt, or fetch the orderId using a purchaseToken and the server API [`purchases.products` Google Play Developer API](https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.products). 
  - Override this behavior with `UsePurchaseTokenForTransactionId`, below.
- GooglePlay Security - Add GooglePlayReceipt.orderID and obsolete GooglePlayReceipt.transactionID for the local receipt validator, for clarity.
- GooglePlay - Reduce the frequency of double-processing when a purchase is canceled, if using `aggressivelyRecoverLostPurchases = true`. Always records purchaseToken in TransactionLog when a transaction is completed.

### Added
- GooglePlay - Ability to override the `Product.transactionID` and use either Google's Purchase Token, or the legacy Order ID when possible, with `UsePurchaseTokenForTransactionId`.
  - Call `void IGooglePlayConfiguration.UsePurchaseTokenForTransactionId(bool usePurchaseToken)` to disable the default behavior
     - a) `false` to use the `orderId`, when possible; this is the legacy and non-unique transaction ID behavior. This must switch to the purchaseToken when the orderId is not available from Google, which is when `aggressivelyRecoverLostPurchases = true`. 
     - b) `true` to always use the unique purchaseToken in transactionID. NOTE: this is the preferred option, and it will be the only behavior available in a future version of Unity IAP.
  - **Background:** The GooglePlay purchaseToken is the unique identifier for all GooglePlay purchases; it is always available for all purchase types and all purchase records. The GooglePlay orderId is not available for all purchase types, notably sandbox and promo code, and also is currently not available for those purchase records which are returned for `aggressivelyRecoverLostPurchases = true` recovered purchases: the Google Play Billing history API used by this feature does not return orderId and therefore cannot be set as the Product.transactionID at that time. Historically, Unity IAP chose to prefer orderId for transactionID in its original implementation. And when the orderId was missing from purchase records (sandbox test purchases, and other no-money purchases), Unity IAP would use the purchaseToken as the transactionID. 
  - **Impact:** Since Unity IAP version 1.23.3 this resulted in non-unique transactionIDs for purchases made which were "aggressively" restored: two distinct ProcessPurchase calls for one purchase could be generated, where Product.transactionID would change between two values (orderId and purchaseToken) and be non-unique. With this version, Unity IAP is starting a transition to only using purchaseToken, and avoids the impact for any new purchases made by a user.

### Fixed
- Receipt Validation Obfuscator - Compilation of `GoolgPlayTangle.cs` and `AppleTangle.cs` files. The generated files have been restored to their pre-2.0.0 location.
- GooglePlay - 2020.2 support, removing usage of obsolete UnityEngine.VR.VRSettings API.

## [2.0.0] - 2020-07-15
### Removed
- UnityChannel / Xiaomi - Completed deprecation, removing related APIs: `UnityEngine.Store`, `IUnityChannelExtensions`, `IUnityChannelConfiguration`.
- CloudMoolah - Removed.
- Tizen - Removed.
- UDP package is no longer installed as a prerequisite

### Added
- UWP - Additional logging during initialization to diagnose developer portal misconfigurations. See https://docs.microsoft.com/en-us/windows/uwp/monetize/in-app-purchases-and-trials#how-to-use-product-ids-for-add-ons-in-your-code for a broad discussion of Windows.ApplicationModel.Store configuration.
- UDP - if not installed or up-to-date, a menu will prompt you to install or update it upon selecting UDP support features. (See below)

### Fixed
- Reduced logging during initialization and purchasing
- Amazon - Removed KeyNotFoundException after purchase failure
- UDP - Removed one NullPointerException vulnerability
- GooglePlay developerPayload spurious JSON parsing exception in log

### Changed
- IAP Updater "Updater Settings..." button now reads "More Information..." to be more accurate
- UDP store implementation is still available, but must be installed in a separate module, available in either Asset Store or Package manager. The UDP module will need to be updated manually. (See above) 
- Visibility of INativeStores is now public, mainly to support the new UDP package's needs
- Resource files - The Product Catalog, Android Target Billing Mode, and Receipt Obfuscator Tangle files will be moved out of the plugins folder.

### Important upgrade note
- **Upgrading from prior Unity IAP?** - Unity may generate a _bad Android build_ once, immediately after installing the upgrade to this version of Unity IAP. Unity will be misconfigured to incorrectly include multiple Andriod app store Java implementations. So **if a dialog appears asking permission to update e.g. the "SamsungApps.aar" file's metadata, during the first build after the upgrade to this version of Unity IAP,** then click Yes, cancel the build, and rebuild. This dialog undesirably defeats Unity IAP's system for including one Android app store (Window > Unity IAP > Android > Target {app store}), resulting in the inclusion of Android Java code for two app stores. Android app stores such as Google Play and Samsung Apps may block publication of an APK which is detected to include Java code for two app store. Restarting Unity after upgrading will also avoid this dialog.

## [1.23.5] - 2020-08-12
### Fixed
- GooglePlay - Fixed `IGooglePlayConfiguration.aggressivelyRecoverLostPurchases == false` (default) to reward players for currently in-flight purchases only, and not historical purchases, when the player cleans their device's TransactionLog, starts and cancels a purchase, and restarts the app.

## [1.23.4] - 2020-07-13
### Added
- Security - Supports receipts from GooglePlay which omit `packageName`. These as are seen from v1.23.2's purchase-recovery features.
   - The purchasing receipt's `packageName` is omitted by a GooglePlay historical purchase query APIs used by v1.23.2. When the RSASSA-PKCS1-v1_5 signature is valid and the receipt's `packageName` is not included, the `appBundleId` / `googleBundleId` input into `UnityEngine.Purchasing.Security.CrossPlatformValidator` is ignored. To avoid replay attacks we encourage developers continue heuristically scrutinizing the returned `purchaseTime` and `productId` values found in decoded receipts.

### Removed
- Analytics - For publication to Kids Category on Google Play and Apple App Store, removed `SystemInfo.deviceUniqueIdentifier` collection and sharing with `ecommerce.iap.unity3d.com` server.

## [1.23.3] - 2020-06-28
### Changed
- GooglePlay - Default the failed-purchase recovery behavior to be disabled, which was introduced in `1.23.2`, to address players deleting their game's TransactionLog and receiving multiple products for a single purchase. Opt-in to the `1.23.2` behavior with `bool IGooglePlayConfiguration.aggressivelyRecoverLostPurchases = true;`, when also using a game server capable of validating each transaction and deduplicate based upon `Product.transactionID`, to reward players one time for a single purchase and support quickly recovering from interrupted-purchases.
   - When a Google Play purchase is made the transaction can become out of synchronization with the Google Play server, appearing to be both failed on the game, and contradictorily successful on the Google Play store — an email message indicating purchase success may be received by the player. This desynchronization occurs when a purchase is interrupted while the native purchasing dialog is displayed to the player. Enable this feature to more aggressively detect these lost purchases.
   - NOTICE: It is strongly recommended this feature be used with an off-device transaction de-duplication mechanism to avoid double-rewarding the player. An example is a game server which records Product.transactionID for each successful purchase and reports to the game whether a new transaction has already been processed for this player during a previous gameplay session. The scenario this occurs is when a player purchases a product, uninstalls and reinstalls their game — erasing Unity IAP’s on-device TransactionLog de-duplication database — and then the player attempts and aborts a re-purchase by cancelling it. The purchase cancellation is one trigger for this recovery feature which may result in recovering and notifying a duplicate success to the game for only the initial purchase. The game could then deny rewarding the player for this detected duplicate purchase.
   - KNOWN-ISSUE: CrossPlatformValidator may show these historically-restored transactions as invalid.

## [1.23.2] - 2020-06-17
### Added
- GooglePlay - Improves the chance of successfully purchasing a Consumable or NonConsumable when the _purchase flow_ is interrupted. Also addresses the dialog, "Your order is still being processed".
   - Unity IAP will now detect this _purchasing_ failure. It will call the `IStoreListener.OnPurchaseFailed` API, initially. Then it will query Google Play for purchase success during the current app session until network is restored, and it will continue querying in the next app session, after a restart. It will finally call the `IStoreListener.ProcessPurchase` API if it finds a successful, unaccounted purchase.
   - Addresses the case where (1) a consumable or nonconsumable purchase flow is started and (2) a network disruption occurs, or the app is sent to the background and the purchasing Activity is canceled, or the app is terminated. 
- GooglePlay - Improves the chance of successfully repurchasing a Consumable whose successful transaction failed however to be _completed_ during the current app session.
   - Unity IAP will now detect this _consumption_ failure. It will automatically retry completing the purchase until it succeeds. Note that `DuplicateTransaction` may still be reported while the retry is ongoing, until the user's product is repurchasable again. See below for new APIs to monitor the consumption flow.
   - Addresses the case where (1) a Consumable purchase calls `IStoreListener.ProcessPurchase`, then (2) the transaction is completed by returning `ProcessPurchaseResult.Complete` from `IStoreListener.ProcessPurchase` or by directly calling `IStoreController.ConfirmPendingPurchase` [internally this always records the transaction identifier to the TransactionLog], and finally (3) an interruption (network or exit) aborts the transaction consumption. Only restarting the app or refunding the purchase would reliably resolve this case.
- GooglePlay - Adds an `"isOwned" : <boolean>` sub-entry to the `Product.receipt`'s `"Payload"` JSON entry in order to help developers understand this product's current ownership state. 
   - Contains `true` if the product is owned by the user. And please note that `true` may also indicate that Unity IAP is actively retrying consumption. Its boolean value will be `false` if the product is available for repurchase, or if we do not yet know Google Play's current status for this product. To clarify the receipt structure, `"isOwned"` is located in the Google Play-specific escaped-JSON sub-document. Sample `Product.receipt`, abbreviated: `{"Payload":"{\"json\": ..., \"signature\": ..., \"isOwned\":true}}"`. See the Google Play section of the [Unity IAP Receipt receipt documentation](https://docs.unity3d.com/Manual/UnityIAPPurchaseReceipts.html) for more on the receipt JSON structure.
- GooglePlay - Adds `boolean IGooglePlayStoreExtensions.IsOwned(Product)` API to conveniently extract the new ownership state, above, from the Google Play JSON receipt. 
   - Returns `true` if the product is still owned by the user. Returns `false` if the product is available for repurchase. Example: 
```extensionProvider.GetExtension<IGooglePlayStoreExtensions>()```
```.IsOwned(storeController.products.WithID("100.gold.coins"));```.
- GooglePlay - Adds `void IGooglePlayStoreExtensions.SetLogLevel(int level)` API to reduce logging. 
   - `level` defaults to the legacy value of `0` and configures the Google Play Java store integration to emit debug, info, warning, and error logs. Setting `1` will restrict logging to emit only warnings and errors. Example: `extensionProvider.GetExtension<IGooglePlayStoreExtensions>().SetLogLevel(1)`.
- GooglePlay - After the purchasing dialog, "You already own this product" from Google Play is shown, the `IStoreListener.OnPurchaseFailed` API is calls with an error of `PurchaseFailureReason.DuplicateTransaction`.
   - Unity IAP now treats "You already own this product" as a successful purchase, and _also_ calls `IStoreListener.ProcessPurchase`. Note: This amends the related behavior introduced in 1.23.1.
   - Addresses the scenario where (1) a Consumable is purchased, and during purchasing (2) the Google Play store is interrupted by e.g. a network disruption. (3) Unity IAP correctly calls `IStoreListener.OnPurchaseFailed`, reporting the interruption as a purchase failure. (4) The user restores the network, attempts to re-purchase, Google Play shows "You already own this product", and Unity IAP reports the message as an error, calling `IStoreListener.OnPurchaseFailed` again. (4.1) Repeated re-purchase attempts fail, also potentially failing even after restarting the app.

## [1.23.1] - 2019-11-18
### Added
- UWP - Additional logging during initialization to diagnose developer portal misconfigurations. See https://docs.microsoft.com/en-us/windows/uwp/monetize/in-app-purchases-and-trials#how-to-use-product-ids-for-add-ons-in-your-code for a broad discussion of Windows.ApplicationModel.Store configuration.

### Fixed
- GooglePlay - Fix offline purchases inconsistently generating OnPurchaseFailed callbacks. Changes 1.22.0 "Fixed GooglePlay store consumable products already owned error due to network issues." - developers may choose to handle the `PurchaseFailureReason.DuplicateTransaction` for a ProductType.Consumable by rewarding the user with the product, and presuming that Unity IAP will automatically complete the transaction.
- Improved compatibility with Unity 5.3 and 5.4.

## [1.23.0] - 2019-10-16
### Added
- UDP - Upgrade to version 1.2.0: new installer to manage previously-installed versions in Project; new UI for UDP Settings window; injection of SDK version information into app manifest; premium game support; user permissions aligned between Unity editor and UDP console; improved security around the transmission of telemetry data (the data you see in your reporting dashboard) between the repacked games and the UDP backend.

### Changed
- UnityChannel / Xiaomi - Please use Unity Distributation Platform (UDP) for Xiaomi functionality. Removed UnityChannel.unitypackage from installer. Disabled and deprecated related APIs: `UnityEngine.Store`, `IUnityChannelExtensions`, `IUnityChannelConfiguration`.
- Tizen - NOTICE Tizen Store support will be removed in an upcoming release.

### Fixed
- Improved installer compatibility with Unity 2018.4 and 2019.x
- GooglePlay - Automatic product restoration across devices when logged into the same Google account.
- GooglePlay - SubscriptionInfo.getSubscriptionInfo() KeyNotFoundException when parsing receipts which omit expected fields.
- GooglePlay - IStoreListener.OnInitializeFailed / IStoreCallback.OnSetupFailed should return InitializationFailureReason.AppNotKnown error when user changes password off-device - user must login. Previously erroneously generated infinite error 6 codes when fetching purchase history after password change.
- OverflowException when initializing if device locale used the comma (“,”) character as decimal separator.

## [1.22.1] - 2019-08-13
### Fixed
- GooglePlay - SubscriptionInfo.getSubscriptionInfo() KeyNotFoundException when parsing receipts which omit expected fields.
- GooglePlay - IStoreListener.OnInitializeFailed / IStoreCallback.OnSetupFailed should return InitializationFailureReason.AppNotKnown error when user changes password off-device - user must login. Previously erroneously generated infinite error 6 codes when fetching purchase history after password change.
- OverflowException when initializing if device locale used the comma (“,”) character as decimal separator. 

## [1.22.0] - 2019-03-18
### Added
- Added Unity Distribution Portal (UDP) module as an Android build target. Unity Distribution Portal streamlines your distribution process. UDP allows you to only build one version of your game, centralize the management of your marketing assets and metadata, and submit your content to multiple app stores, all in the same workflow. For more details, please refer to https://docs.unity3d.com/Packages/com.unity.purchasing.udp@1.0/manual/index.html.
- Added extension function for Apple store to expose products' sku details
- Added support for developer to include accountId in getBuyIntentExtraParams, this data helps Google analyze fraud attempts and prevent fraudulent transactions.
- Added GooglePlay store extension function to support restore purchases.
- Added GooglePlay store extension function to support consume(finish transaction) a purchase manually.

### Fixed
- Fixed UWP build errors.
- Fixed errors when initializing with two purchasing modules on WebGL & Windows Standalone.
- Fixed not "re-importing required assets" when switching build targets with IAP.
- Re-enabled Facebook IAP implementation for non-Gameroom Canvas apps.
- Fixed GooglePlay store consumable products already owned error due to network issues.
- Fixed wrong product id when cancel a subscription product purchase.

## [1.21.0] - 2018-08-14
- Limited release, for closed beta of UDP.

## [1.20.1] - 2018-10-5
### Added
- Added a callback function that allows developers to check the state of the upgrade/downgrade process of subscriptions on GooglePlay.

### Fixed
- Google Daydream - Correctly Displays IAP Prompt in 3d VR version instead of native 2D. 
- Fixed issue where IAP catalog prevented deletion of Price under Google Configuration.
- Amazon Store - Fixed bug where Amazon store could not correctly parse currencies for certain countries.
- MacOS - Fixed bug that causes non-consumables to auto-restore on MacOS apps after re-install, instead of requiring the the Restore button to be clicked.
- Updated Android Response Code to return correct message whenever an activity is cancelled.
- Fixed Mono CIL linker error causing initialization failure in Unity 5.3 
- Fixed inefficient Apple Receipt Parser that was slowing down when a large number of transactions were parsed on auto-restore.

## [1.20.0] - 2018-06-29
### Added
- API for developers to check SkuDetails for all GooglePlay store products, including those that have not been purchased.
- Error Code Support for Amazon.
- Support upgrade/downgrade Subscription Tiers for GooglePlayStore.
- Support Subscription status check (valid/invalid) for Amazon Store. 

### Changed
- Location of Product Catalog from Assets/Plugins/UnityPurchasing/Resources folder to Assets/Resources.
- Amazon Receipt with enriched product details and receipt details.

### Fixed
- Issue where Unknown products (including non-consumables) were consumed during initialization. 
- ArgumentException where currency was set to null string when purchase was made.

## [1.19.0] - 2018-04-17
### Added
- For GooglePlay store, `developerPayload` has been encoded to base64 string and formatted to a JSON string with two other information of the product. When extract `developerPayload` from the product receipt, firstly decode the json string and get the `developerPayload` field base64 string, secondly decode the base64 string to the original `developerPayload`.
- `SubscriptionManager` - This new class allows developer to query the purchased subscription product's information. (available for AppleStore and GooglePlay store) 
    - For GooglePlay store, this class can only be used on products purchased using IAP 1.19.0 SDK. Products purchased on previous SDKs do not have the fields in the "developerPayload" that are needed to parse the subscription information.
        - If the "Payload" json string field in the product's json string receipt has a "skuDetails" filed, then this product can use `SubscriptionManager` to get its subscription information.
- Added the `StoreSpecificPurchaseErrorCode` enum. Currently contains values for all Apple and Google Play error codes that are returned directly from the store.
- Added the `ITransactionHistoryExtensions` extension. Developers can call `GetLastPurchaseFailureDescription()` and `GetLastStoreSpecificPurchaseErrorCode()` to get extended debugging/error information.
- Codeless IAP - Adds an `Automatically initialize UnityPurchasing` checkbox to the IAP Catalog. Checking this box will cause IAP to automatically initialize on game start using the products contained in the catalog.

## [1.18.0] - 2018-03-27
### Added
- Unity IAP E-Commerce - [Closed Beta] Supports new "managed store" functionality. Contact <iapsupport@unity3d.com> to learn more.

## [1.17.0] - 2018-02-21
### Added
- Unity IAP Promo - [Beta] Supports new Unity Ads feature to advertise IAPs inside advert placements.

### Changed
- Codeless IAP - Allow developers to use both IAPButton and IAPListener simultaneously. Broadcasts ProcessPurchase and OnPurchaseFailed to all productId-matching IAPButtons and to all IAPListeners. Allow multiple IAPListeners to be set using the AddListener method. Note: This change may increase the chance one rewards users multiple times for the same purchase.

## [1.16.0] - 2018-01-25
### Changed
- GooglePlay - Gradle builds will 'just work'. Internalized Proguard warning-suppression configurations. (Moved `proguard-user.txt.OPTIONAL.txt` into GooglePlay.aar, effectively.)
- Replaced Apple Application Loader product catalog exporter with Apple XML Delivery product catalog exporter, because submitting IAP via Application Loader is now deprecated

### Added
- Security - Adds the `IAppleExtensions.GetTransactionReceiptForProduct` method that returns the most recent iOS 6 style transaction receipt for a given product. This is used to validate Ask-to-buy purchases. [Preliminary documentation](https://docs.google.com/document/d/1YM0Nyy-kTEM2YGpOxVj20kUBtyyNKndaXWslV0RF_V8) is available.
- Apple - Adds an optional callback, `IAppleConfiguration.SetApplePromotionalPurchaseInterceptorCallback`, that intercepts Apple Promotional purchases in iOS and tvOS. Developers who implement the callback should call `IAppleExtensions.ContinuePromotionalPurchases` to resume the purchase flow. [Preliminary documentation](https://docs.google.com/document/d/1wQDRYoQnTYoDWw4G64V-V6EZbczpkq0moRf27GKeUuY) is available.
- Xiaomi - Add support for retrieving developer payload. [Preliminary documentation](https://docs.google.com/document/d/1V0oCuCbb7ritK8BTAMQjgMmDTrsascR2MDoUPXPAUnw) is available.

### Fixed
- Removed Debug log from UnityIAP StandardPurchasingModule
- Xiaomi - Remove unnecessary Android WRITE_EXTERNAL_STORAGE permission.

## [1.15.0] - 2017-11-13
### Added
- IAP Updates - GUI to control plugin updates in Window > Unity IAP > IAP Updates menu. Supports viewing changelog, skipping this update, disabling automatic updates, and showing current version number. Writes preferences to Assets/Plugins/UnityPurchasing/Resources/IAPUpdaterPreferences.json.

# Changed
- IAP Demo - Improved UI and cleaned up code in the IAP Demo sample scene
- Version Log - Changed logging of Unity IAP version (e.g. "1.15.0") to be only at runtime and not while in the Editor

### Fixed
- Facebook - Correctly handles situations where the number of available products exceeds the Facebook server response page size 
- Updater will no longer prompt for updates when Unity is running in batch mode
- Gradle - Include and relocate sample Proguard configuration file to Assets/Plugins/UnityPurchasing/Android/proguard-user.txt.OPTIONAL.txt; was missing from 1.13.2
- Security - Upgrades project to autogenerate UnityChannelTangle class if missing when GooglePlayTangle obfuscated secret receipt validation support class is present.
- UnityIAPUpdater - Fix a FormatException sensitivity for DateTime parsing
- Xiaomi Catalog - Fix a NullReferenceException seen when exporting an empty catalog
- Xiaomi Receipt Validation - Fix missing UnityChannelTangle class for Unity IAP projects which used receipt validation


## [1.14.1] - 2017-10-02
### Fixed
- Apple Application Loader product catalog exporter now correctly exports tab-separated values for catalogs containing more than one product
- JSONSerializer - Unity 5.3 build-time regression - missing "type" field on ProductDescription. Field is available in 5.4 and higher.

## [1.14.0] - 2017-09-18
### Added
- Codeless IAP - Added an `IAPListener` Component to extend Codeless IAP functionality. Normally with Codeless IAP, purchase events are dispatched to an `IAPButton` UI Component that is associated with a particular product. The `IAPListener` does not show any UI. It will receive purchase events that do not correspond to any active `IAPButton`.
    - The active `IAPListener` is a fallback—it will receive any successful or failed purchase events (calls to `ProcessPurchase` or `OnPurchaseFailed`) that are _not_ handled by an active Codeless `IAPButton` Component. 
    - When using the `IAPListener`, you should create it early in the lifecycle of your app, and not destroy it. By default, it will set its `GameObject` to not be destroyed when a new scene is loaded, by calling `DontDestroyOnLoad`. This behavior can be changed by setting the `dontDestroyOnLoad` field in the Inspector.
    - If you use an `IAPListener`, it should be ready to handle purchase events at any time, for any product. Promo codes, interrupted purchases, and slow store behavior are only a few of the reasons why you might receive a purchase event when you are not showing a corresponding `IAPButton` to handle the event.
    - Example use: If a purchase is completed successfully with the platform's app store but the user quits the app before the purchase is processed by Unity, Unity IAP will call `ProcessPurchase` the next time it is initialized—typically the next time the app is run. If your app creates an `IAPListener`, the `IAPListener` will be available to receive this `ProcessPurchase` callback, even if you are not yet ready to create and show an `IAPButton` in your UI.
- Xiaomi - IAP Catalog emitted at build-time to APK when Xiaomi Mi Game Pay is the targeted Android store.
- Xiaomi - Support multiple simultaneous calls to IUnityChannelExtensions.ConfirmPurchase and IUnityChannelExtensions.ValidateReceipt.

### Changed
- CloudMoolah - Upgraded to 2017-07-31 SDK. Compatible with the [2017 Cloud Moolah developer portal](https://dev.cloudmoolah.com/). Add `IMoolahConfiguration.notificationURL`. Removed deprecated `IMoolahExtensions.Login`, `IMoolahExtensions.FastRegister`, `IMoolahExtensions.RequestPayOut`. [Preliminary updated documentation](https://docs.google.com/document/d/1g-wI2gOc208tQCEPOxOrC0rYgZA0_S6qDUGOYyhmmYw) is available.
- Receipt Validation Obfuscator - Improved UI for collecting Google Play License Keys for receipt validation.
- Xiaomi - Removed deprecated MiProductCatalog.prop file generation in favor of MiGameProductCatalog.prop for Xiaomi Mi Game Pay Android targets.
- IAPDemo - Guard PurchaseFailureReason.DuplicateTransaction enum usage to be on Unity 5.6 and higher.
- Internal - Namespace more root-level incidental classes under UnityEngine.Purchasing or UnityEditor.Purchasing as appropriate.

### Fixed
- Invoke the onPurchaseFailed event on a Codeless IAP button if the button is clicked but the store was not initialized correctly

## [1.13.3] - 2017-09-14
### Fixed
- Fixed a bug that caused some iOS 11 promoted in-app purchase attempts to fail when the app was not already running in the background

## [1.13.2] - 2017-09-07
### Added
- Android Gradle - Optional Proguard configuration file to support Gradle release builds on Unity 2017.1+: "Assets/Plugins/Android/proguard-user.txt.OPTIONAL.txt". See contents of file for more detail.
- Installer - Compatibility with Unity 2017.2's Build Settings > Android > Xiaomi Mi Game Center SDK package add button, avoiding duplicate class definitions if previously added to `Packages/manifest.json` (new Package Manager).

### Fixed
- Windows (UWP) Store - Updates error handling for failed purchases to correctly call `OnPurchaseFailed()` with an informative `PurchaseFailureReason`
- Fixed prices that were incorrectly parsed when the device's culture specified a number format using a comma for the decimal separator
- XiaomiMiPay - Limit product identifier length to 40 characters in IAP Catalog, matching store requirements.
- Receipt Validation - Address aggressive class stripping NullReferenceException when validating receipt with preservation in Assets/Plugins/UnityPurchasing/scripts/link.xml.

## [1.13.1] - 2017-08-18
### Fixed
- Android platforms - Fix Unity crash by stack-overflow when using the `UnityPurchasingEditor.TargetAndroidStore(AndroidStore store)` method or the Window > Unity IAP > Android > Xiaomi Mi Game Pay targeting menu.

## [1.13.0] - 2017-07-31
### Added
- iOS and tvOS - Added support for purchases initiated from the App Store using the new API in iOS 11 and tvOS 11. For more information about this feature, watch the ["What's New in StoreKit" video from WWDC 2017](https://developer.apple.com/videos/play/wwdc2017/303/). If you intend to support this feature in your app, it is important that you initialize Unity Purchasing and be prepared to handle purchases as soon as possible when your app is launched.
- Apple platforms - The IAP Catalog tool will now export translations when exporting to the Apple Application Loader format.
- Apple platforms - Add support for controlling promoted items in the App Store through IAppleExtensions. This feature is available on iOS and tvOS 11. Set the order of promoted items in the App Store with IAppleExtensions.SetStorePromotionOrder, or control visiblility with IAppleExtensions.SetStorePromotionVisibility.
```csharp
public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
{
	// Set the order of the promoted items
	var appleExtensions = extensions.GetExtension<IAppleExtensions>();
	appleExtensions.SetStorePromotionOrder(new List<Product>{
		controller.products.WithID("sword"),
		controller.products.WithID("subscription")
	});
}
```
```csharp
public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
{
	// Set the visibility of promoted items
	var appleExtensions = extensions.GetExtension<IAppleExtensions>();
	appleExtensions.SetStorePromotionVisibility(controller.products.WithID("subscription"), AppleStorePromotionVisibility.Hide);
	appleExtensions.SetStorePromotionVisibility(controller.products.WithID("100.gold.coins"), AppleStorePromotionVisibility.Default);
}
```

### Changed
- AndroidStore enum - Obsoleted by superset AppStore enum. Cleans up logging mentioning Android on non-Android platforms.
- Codeless IAP - IAP Button notifies just one purchase button for purchase failure, logging additional detail.

### Fixed
- Apple platforms - Catch NullReferenceException thrown during initialization when the app receipt cannot be correctly parsed.
- IAP Catalog - Fix GUI slowdown when typing details for large number of products on Windows.
- Fix internal MiniJSON namespace exposure, regression introduced in 1.11.2.

## [1.12.0] - 2017-07-25
### Added
- XiaomiMiPay - Add Xiaomi Mi Game Pay app store purchasing for Android devices in China. Add the "Unity Channel" support library and service. Unity Channel helps non-China developers access the Unity-supported Chinese app store market by featuring app store user login and payment management. Use Unity Channel directly for login to Xiaomi. Unity IAP internally uses Unity Channel for Xiaomi payment. [Preliminary documentation](https://docs.google.com/document/d/1VjKatN5ZAn6xZ1KT_PIvgylmAKcXvKvf4jgJqi3OuuY) is available. See also [Xiaomi's portal](https://unity.mi.com/) and [Unity's partner guide](https://unity3d.com/partners/xiaomi/guide).

### Fixed
- FacebookStore - Fix login and initialization corner-case abnormally calling RetrieveProducts internally
- Tizen Store - Fix purchasing regression introduced after 1.11.1
- Mac App Store - Fixes "libmono.0.dylib not found" errors at launch if built via Unity 2017.1. See also Known Issues, below.

### Known Issues
- Mac App Store - Incompatible with Unity 2017.1.0f3: exception will be thrown during purchasing. Fixed in Unity 2017.1.0p1.

## [1.11.4] - 2017-06-21
### Fixed
- Apple platforms - Fix a blocking bug when building from Unity 5.3.

## [1.11.3] - 2017-06-20
### Fixed
- Amazon - Purchase attempts for owned non-consumable products are now treated as successful purchases.

## [1.11.2] - 2017-05-30
### Added
- Apple platforms - Parse the app receipt when retrieving product information and attempt to set receipt fields on Product. With this change the hasReceipt field on Apple platforms will work more like it does on non-Apple platforms.

### Fixed
- FacebookStore - Better error handling for cases where store configuration changes after purchases have already been made.
- General - Better momentary memory performance for local receipt validation and other JSON parsing situations.
- Editor menus - Targeted Android store menu checkmark are set and valid more often.
- Installer - Fix error seen during install, `ReflectionTypeLoadException[...]UnityEditor.Purchasing.UnityIAPInstaller.<k_Purchasing>`.

## [1.11.1] - 2017-05-23
### Fixed
- GooglePlay - Fix regression seen during purchasing where GooglePlay Activity forces screen orientation to portrait and turns background black. Restores neutral orientation and transparent background behavior.

## [1.11.0] - 2017-05-01
### Added
- FacebookStore - Facebook Gameroom Payments Lite support. Available on Unity 5.6+ when building for Facebook Platform on Gameroom (Windows) and WebGL. Preliminary documentation is available [here](https://docs.google.com/document/d/1FaYwKvdnMHxkh47YVuXx9dMbc6ZtLX53mtgyAIn6WfU/)
- Apple platforms - Added experimental support for setting "simulatesAskToBuyInSandbox". Please let us know how this impacts ask-to-buy testability for you.
```csharp
extensions.GetExtension<IAppleExtensions>().simulateAskToBuy = true;
```
- Apple platforms - Added support for setting "applicationUsername" field which will be added to every payment request to help the store detect fraud.
```csharp
// Set the applicationUsername to help Apple detect fraud
extensions.GetExtension<IAppleExtensions>().SetApplicationUsername(hashedUsername);
```

### Requirement
- GooglePlay - "Android SDK API Level 24 (7.0)" (or higher) must now be installed. To upgrade, either perform the one-time step of setting the project's "Android Player Settings > Other Settings > Minimum API Level" to 24, building an APK, then resetting to the project's previous value. Or, run the `android` Android SDK Manager tool manually and install "Android 7.0 (API 24)". Addresses build error messages: "Unable to merge android manifests." and "Main manifest has \<uses-sdk android:targetSdkVersion='23'> but library uses targetSdkVersion='24'". Note the Minimum API Level support is unchanged; merely the installation of API 24 SDK is now required for Daydream VR.

### Fixed
- GooglePlay Daydream VR - Uses decoration-free Activity for purchasing
- GooglePlay - Avoids sporadic price serialization exception
- Apple App Stores - Improve handling of the situation where an attempt to finish a transaction fails (if the user is signed out of the store and cancels the sign in dialog, for example). The Apple store implementation will now remember that the transaction should be finished, and attempt to call finishTransaction again if the transaction is retrieved from the queue again. When this happens, the store will call OnPurchaseFailed with the reason "DuplicateTransaction"—this prevents a situation where a call to InitiatePurchase could result in no call to ProcessPurchase or OnPurchaseFailed.
- Amazon - Fix for a crash when loading product metadata for subscription parent products

## [1.10.1] - 2017-03-29
### Fixed
- GooglePlay - Suspending and resuming from app-icon while purchase dialog displayed no longer generates both OnPurchaseFailed then ProcessPurchase messages, only whichever callback is correct.
- Remove cloud JSON exporter that was erroneously showing in the IAP Catalog export list
- Fixed a bug when parsing localized prices when the device's localization does not match the number format rules for the currency
- Resolved DLL name conflict by renaming Assets/Plugins/UnityPurchasing/Bin/Common.dll to Purchasing.Common.dll
- Installer - Suppressed multiple redundant dialogs

## [1.10.0] - 2017-01-23
### Added
- Samsung Galaxy Apps - In-App Purchase SDK v4. Simplifies flow for first-time payment users. See [Samsung Developer IAP Documentation](http://developer.samsung.com/iap) for more.
- Tizen Store - Add support for subscriptions
- StandardPurchasingModule - Add `bool useFakeStoreAlways` property to override native stores with the local debug FakeStore for rapid prototyping. Will not connect to any App Store when enabled.

```csharp
// Enable the FakeStore for all IAP activity
var module = StandardPurchasingModule.Instance();
module.useFakeStoreAlways = true;
```

* Editor Updater - Notify the developer when updates to Unity IAP are available with an actionable dialog. Periodically check the Asset Store version and prompt with an upgrade dialog capable of downloading the latest plugin.
* Editor Installer - Simplify integration of Unity IAP into a Project, avoiding unexpected breakage of the scripting build environment after package installation. Detect and warn if Unity IAP Core Service is "Off" during installation.

### Removed
- Samsung Galaxy Apps - remove In-App Purchase SDK v3 and replaced with v4, above.

### Fixed
- GooglePlay - Fix a problem that occurred when suspending the application during a successful transaction. Previously a consumable product could get stuck in a state where it could not be purchased again until the Google Play cache was cleared.

## [1.9.3] - 2017-01-03
### Added
- Windows Store - support for UWP apps running while logged-out of Windows Store. Now fetches app's product metadata if logged out, and requests user sign in to make purchase or to fetch user's purchase history.
- Editor - diagnostic log at build-time when IAP Service disabled: "Unity IAP plugin is installed, but Unity IAP is not enabled. Please enable Unity IAP in the Services window." Fewer redundant errors.

### Fixed
- Editor - checkmarks refresh for Targeted Android Store after Editor Play/Stop
- Editor - hides spurious Component MenuItems
- Linux Editor - BillingMode.json path case-sensitivity 
- IAP Catalog - clearer text for Export button: "App Store Export"

## [1.9.2] - 2016-11-29
### Fixed
- GooglePlay - addresses warning about usage of WebViewClient.onReceivedSslError if CloudMoolah.aar is included
- CloudMoolah - simplify Login API and rename LoginError enum to LoginResultState
- Android - remove READ_PHONE_STATE permission from AndroidManifest.xml simplifying logic around CloudMoolah Register and Login by removing automatic SystemInfo.deviceUniqueIdentifier calls. Developers may now choose to include this permission using this API to collect a user identifer, or provide an alternate long-lived user identifier, in a CloudMoolah supporting game for the Register and Login API password parameter.

## [1.9.1] - 2016-11-17
### Added
- [Beta] Codeless IAP — UI fields show title, price, and description downloaded from the platform store
- IAP Catalog now includes a store ID field for the CloudMoolah store

### Fixed
- IAPButton component now updates product ID list as the IAP Catalog is being edited
- Fixed a problem with opening a project containing the Unity IAP plugin while IAP was disabled in the Services window
- IAPButton inspector field for Product ID now works correctly with Undo
- Set GooglePlay as default Android store AAR fileset. Excludes other store's assets (Java, resource XML, localization), saving ~196kb in default APK. Creates Assets/Plugins/UnityPurchasing/Resources/BillingMode.json in Project. Configure manually with Window > Unity IAP > Android menu, or UnityPurchasingEditor.TargetAndroidStore(AndroidStore).
- CloudMoolah - update Window > Unity IAP > Android menu checkmarks when CloudMoolah selected

## [1.9.0] - 2016-10-31
### Added
- CloudMoolah support. CloudMoolah website [here](http://www.cloudmoolah.com). Preliminary store guide available [here](https://docs.google.com/document/d/1T9CEZe6eNCwgWkq7lLwrEw7rpSbu3_EjcUVgJJL6xA0/edit). Preliminary configuration document available [here](https://docs.google.com/document/d/1dpc3zqsyROeFUVBy9W9pc0sskCPyfhcRnsGxtyITmyQ/edit).
- [Beta] Codeless IAP tools. Implement IAP by adding IAP Buttons to your project (Window > Unity IAP > Create IAP Button) and configure your catalog of IAP products without writing a line of code (Window > Unity IAP > IAP Catalog). Preliminary documentation is available [here](https://docs.google.com/document/d/1597oxEI1UkZ1164j1lR7s-2YIrJyidbrfNwTfSI1Ksc/edit).
- [Beta] Google Play - Support for Daydream VR. Requires Unity 5.4+ "GVR" Technical Preview, enabling VR, and including the Daydream SDK. Additional details [here](https://unity3d.com/partners/google/daydream).
- Samsung Galaxy Store - Added support for receiving auto-recurring subscriptions
- Highlights chosen Android store in menu Window > Unity IAP > Android

### Fixed
- Remove the menu item to select Android store at runtime
- Fix an exception that occurred when parsing prices while culture was set to use commas as a decimal separator

## [1.8.3] - 2016-10-13
### Fixed
- iOS crash when calling PurchasingManager.ConfirmPendingPurchase with a product that does not have a transaction ID
- Ensure tvOS build uses correct stub DLL
- Support transaction receipt logging for all store platforms. Requires corresponding Unity Engine: currently unreleased Unity 5.3/5.4 patch, or Unity 5.5.0b7+.

## [1.8.2] - 2016-09-23
### Fixed
- Tizen Store - Product list not delivered to new app or new user

## [1.8.1] - 2016-08-30
### Fixed
- Windows Store - Windows App Compatibility Kit Supported API failure with exposure of Tizen API.
- Tizen Store - Added sample products and GroupId to `IAPDemo.cs`

## [1.8.0] - 2016-08-23
### Added
- Tizen Store support. Preliminary documentation is available [here](https://docs.google.com/document/d/1A2TidgeV4lY16IcjdU7lX4EIvx6NNfONaph12iT8KyY).

### Fixed
- Google Play - Promo code redemptions not being detected whilst the App is running.
- Google Play - Guard against spurious SecurityException (additional details [here](https://github.com/googlesamples/android-play-billing/issues/26).)

## [1.7.0] - 2016-08-07
### Added
- Samsung Galaxy store support. Preliminary documentation is available [here](https://docs.google.com/document/d/1kUq-AHKyJftUA68xr44mrp7gs_MNxNiQ693s0b7qDdM).
- Google Play - failed purchases - the [Google Play server response code](https://developer.android.com/google/play/billing/billing_reference.html#billing-codes) is now supplied as the [PurchaseFailedEventArgs.message](https://docs.unity3d.com/ScriptReference/Purchasing.PurchaseFailedEventArgs-message.html) property for failed purchases.
- Android - it is now possible to choose the store implementation to use at runtime.
    - Make a build containing all store implementations by choosing Window > Unity IAP > Android > "Select store at runtime"

```csharp
// Pass the desired store to the module, e.g. Amazon Apps.
var module = StandardPurchasingModule.Instance(AndroidStore.AmazonAppStore);
```

### Fixed
- Google Play - PurchaseFailureReason.ItemUnavailable and PurchaseFailureReason.BillingUnavailable being reported as 'Unknown' errors.

## [1.6.1] - 2016-07-18
### Fixed
- Google Play - fixed non fatal 'IllegalArgumentException: Receiver not registered' warning appearing in crashlogs.

## [1.6.0] - 2016-7-7
### Added
- Support for redeeming [Google Play promo codes](https://developer.android.com/google/play/billing/billing_promotions.html) for IAPs.
- IAndroidStoreSelection extended configuration for accessing the currently selected Android store.

```csharp
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
Debug.Log(builder.Configure<IAndroidStoreSelection>().androidStore);
```

### Fixed
- Apple Stores - ProcessPurchase not being called on initialize for existing transactions if another storekit transaction observer is added elsewhere in the App. This addresses a number of issues when using third party SDKs, including Facebook's.
- Google Play - sandbox purchases. In Google's sandbox Unity IAP now uses Google's purchase token instead of Order ID to represent transaction IDs.
- iOS not initializing when IAP purchase restrictions are active. IAP will now initialise if restrictions are active, enabling browsing of IAP metadata, although purchases will fail until restrictions are disabled.
- Instantiating multiple ConfigurationBuilders causing purchasing to break on Google Play & iOS.

## [1.5.0] - 2016-5-10
### Added
- Amazon stores - Added NotifyUnableToFulfillUnavailableProduct(string transactionID) to IAmazonExtensions.

You should use this method if your App cannot fulfill an Amazon purchase and you need to call [notifyFulfillment](https://developer.amazon.com/public/apis/earn/in-app-purchasing/docs-v2/implementing-iap-2.0) method with a FulfillmentResult of UNAVAILABLE.

### Fixed
- Google Play - purchase failure event not firing if the Google Play purchase dialog was destroyed when backgrounding and relaunching the App.

### Changed
- Updated to V2.0.61 of Amazon's IAP API.
- Apple stores, Google Play - removed logging of products details on startup.

## [1.4.1] - 2016-4-12
### Fixed
- Amazon stores - "App failed to call Purchasing Fullfillment" error caused by Unity IAP misuse of Amazon's notifyFulfillment mechanism.

### Added
- Editor API call for toggling between Android billing platforms in build scripts; UnityPurchasingEditor.TargetAndroidStore(AndroidStore). See below for usage.

```csharp
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEditor;

// A sample Editor script.
public class MyEditorScript {
	void AnEditorMethod() {
		// Set the store to Google Play.
		UnityPurchasingEditor.TargetAndroidStore(AndroidStore.GooglePlay);
	}
}
```

## [1.4.0] - 2016-4-5
### Added
- Amazon Apps & Amazon underground support. Preliminary documentation is available [here](https://docs.google.com/document/d/1QxHRo7DdjwNIUAm0Gb4J3EW3k1vODJ8dGdZZfJwetYk/edit?ts=56f97483).

## [1.3.2] - 2016-4-4
### Fixed
- Apple stores; AppleReceiptValidator not parsing AppleInAppPurchaseReceipt subscriptionExpirationDate and cancellationDate fields.

## [1.3.1] - 2016-3-10
### Changed
- Google Play - Google's auto generated IInAppBillingService types have been moved to a separate Android archive; GoogleAIDL. If other plugins define IInAppBillingService, generating duplicate class errors when building for Android, you can delete this AAR to resolve them.

## [1.3.0] - 2016-3-3
### Added
- Receipt validation & parsing library for Google Play and Apple stores. Preliminary documentation can be found [here](https://docs.google.com/document/d/1dJzeoGPeUIUetvFCulsvRz1TwRNOcJzwTDVf23gk8Rg/edit#)

## [1.2.4] - 2016-2-26
### Fixed
- Demo scene error when running on IL2CPP.
- Fixed Use of app_name in Google Play Android manifest causing build errors when exported to Android studio.

## [1.2.3] - 2016-2-11
### Added
- iOS, Mac & Google Play - support for fetching products incrementally with the IStoreController.FetchAdditionalProducts() method that is new to Unity 5.4. Note you will need to be running Unity 5.4 to use this functionality.

## [1.2.2] - 2016-2-9
### Fixed
- Setting IMicrosoftConfiguration.useMockBillingSystem not correctly toggling the local Microsoft IAP simulator.
- Deprecated WinRT.Name and WindowsPhone8.Name; WindowsStore.Name should be used instead for Universal Windows Platform 8.1/10 builds.
- Unnecessary icons and string resources removed from Android archives.

## [1.2.1] - 2016-1-26
### Fixed
- IAP Demo scene not registering its deferred purchase listener.

## [1.2.0] - 2016-1-15
### Added
- tvOS Support. tvOS behaves identically to the iOS and Mac App Stores and shares IAPs with iOS; any IAPs defined for an iOS App will also work when the app is deployed on tvOS.
- Apple Platforms - a method to check whether payment restrictions are in place; [SKPaymentQueue canMakePayments].

```csharp
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
// Detect if IAPs are enabled in device settings on Apple platforms (iOS, Mac App Store & tvOS).
// On all other platforms this will always return 'true'.
bool canMakePayments = builder.Configure<IAppleConfiguration> ().canMakePayments;
```

### Changed
- Price of fake Editor IAPs from $123.45 to $0.01.

## [1.1.1] - 2016-1-7
### Fixed
- iOS & Mac App Store - Clean up global namespace avoiding symbol conflicts (e.g `Log`)
- Google Play - Activity lingering on the stack when attempting to purchase an already owned non-consumable (Application appeared frozen until back was pressed).
- 'New Game Object' being created by IAP; now hidden in hierarchy and inspector views.

## [1.1.0] - 2015-12-4
### Fixed
- Mac App Store - Base64 receipt payload containing newlines.
- Hiding of internal store implementation classes not necessary for public use.

### Added
- IAppleConfiguration featuring an 'appReceipt' string property for reading the App Receipt from the device, if any;

```csharp
var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
// On iOS & Mac App Store, receipt will be a Base64 encoded App Receipt, or null
// if no receipt is present on the device.
// On other platforms, receipt will be a dummy placeholder string.
string receipt = builder.Configure<IAppleConfiguration> ().appReceipt;
```

## [1.0.2] - 2015-11-6
### Added
- Demo scene uses new GUI (UnityEngine.UI).
- Fake IAP confirmation dialog when running in the Editor to allow you to test failed purchases and initialization failures.

## [1.0.1] - 2015-10-21
### Fixed
- Google Play: Application IStoreListener methods executing on non scripting thread.
- Apple Stores: NullReferenceException when a user owns a product that was not requested by the Application during initialization.
- Tizen, WebGL, Samsung TV: compilation errors when building a project that uses Unity IAP.

## [1.0.0] - 2015-10-01
### Added
- Google Play
- Apple App Store
- Mac App Store
- Windows Store (Universal)
