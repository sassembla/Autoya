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
