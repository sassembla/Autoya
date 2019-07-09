# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Romantic Versioning](http://blog.legacyteam.info/2015/12/romver-romantic-versioning/).

## [Unreleased]

## [0.9.9] - 2019/07/09
### Changed
- added transactionId parameter when Unity IAP is failed or cancelled if possible. this might be help for IAP consumption logic.

## [0.9.8] - 2019/05/13

### Changed
- Unity IAP is updated to 1.22.0.

### Feature
- save hashes and check the hash is existed or not. This feature is called "HashHit".


## [0.9.7] - 2019/02/06

### Changed
- adding force application update flag for test.
- IAP fail handling is refactorerd.

### Feature
- collect on memory assetBundle name feature is added.


## [0.9.6] - 2018/12/26

### Feature
- Android URLScheme handling feature is added. 


## [0.9.5] - 2018/12/17

### Feature
- link.xml generator is added. link.xml will be generated when some AssetBundle built.
- AssetBundleListVersionEditor is added. open Window > Autoya > Open AssetBundleVersionEditor. this is useful for modifing the version of the AssetBundle List.

### Fixed
- PurchaseRouter, possible bug (fire when user cancel) is fixed.


## [0.9.4] - 2018/12/09
### Changed
- DownloadAssetBundleListIfNeed method signature changed. error code is added.



## [0.9.3] - 2018/11/26
### Feature
- on URLCaching, Caching data with specific key and URL is enabled. this is useful when url of same data is changing by the time.


## [0.9.2] - 2018/11/22

### Changed
- in IAP, the tiketId parameter is added when player received Paid receipt and on memory record is exists. this will help the Paid feature.

- URL Caching API is changed. hard coded request header is deleted and new RequestHeader parameter is added for each URLCaching request.

- AssetBundle hash checking on Unity Editor is failed in some case. not reproducible in actual devices. this change supports it on Unity Editor.


## [0.9.1] - 2018/11/19
### Feature
- URL Caching example is added.


## [0.9.0] - 2018/10/31
### Feature
- URL Caching feature added. download Unity assets from web and persist it.

### Changed
- tUnity IAP is updated to 1.20.1.


## [0.8.47]	- 2018/10/23
### Changed
- the implemtation of getting local ip is changed.
- adopted to .NET framework 4.6.
- AssetBundleList update feature is updated for getting multiple new AssetBundleLists at once.


## [0.8.46]	- 2018/09/03
### Fixed
- authenticated http response header is now adopted to rfc7230.
- build manifest's appVersion was fixed to show user defined appVersion correctly.


## [0.8.45]	- 2018/08/01
### Feature
- URL Scheme for iOS supported in Notification feature.


## [0.8.44]	- 2018/07/06
### Changed
- added new OverridePoint for purchase ticket request format changer.


## [0.8.43]	- 2018/06/08
### Changed
- update IAP library version to 1.9.x
- update AssetGraph to 1.4
- update Unity to 2018.1.0f2


## [0.8.42]	- 2018/04/23
### Changed
- Generate AssetBundles via AssetGraph now export AssetBundles + AssetBundleList only.  not export .manifest files.
- update IAP library version to 1.8.x
- change: Auth_Logout method signature changed for async.
- Logout method is changed from sync to async. callbacks are added.

### Feature
- OverridePoints/OnAppVersionRequired and OnResourceVersionRequired are added for HTTP request.
- OverridePoints/OnUpdateToNewAssetBundleList and OnAssetBundleListUpdated are added for AssetBundleList handling.
- ResourcesController feature is added.

### Fix
- AssetBundle samples are fixed.


## [0.8.41]	- 2018/04/02
### Changed
- OverridePoints/IsFirstBoot method signature is changed to async.

### Feature
- added OverridePoints/OnBootApplication method for run some code before Autoya starts authenticate feature.

- OverridePoints/OnRestoreRuntimeManifest method is added. this method will be executed when start restoring runtimeManifest.

- OverridePoints/OnNewAssetBundleListStoreFailed method is added. this method will be executed when failed to store new-downloaded AssetBundleList to device. 


- AssetBundle_FactoryReset feature is added. this method is pretty good for delete all cached AssetBundles and downloaded AssetBundleList. after this method, the state of the AssetBundle feature is set to "newly installed".

### Fix
- fixed bug for Purchase_AttemptReadyPurcase.

## [0.8.40]	- 2018/03/22
### Changed
- AssetBundle Preload feature method signature is changed.

### Fix
- AssetBundle Preload feature can be use with LoadAsset without conflict of download.


## [0.8.32]	- 2018/03/20
### Changed
- Adopted to Unity 2017.3.x.


## [0.8.31]	- 2018/03/20
### Changed
- Update UUebView to 1.0.4


## [0.8.30]	- 2018/03/08
### Changed
- Adopt to Unity 2017.x
- Purchase failed handler now contains http response code.

## [0.8.29] - 2018/03/07
### Changed
- last version for Unity 5.x
- default AssetBundle List download path of demoProject is changed.


## [0.8.28] - 2018/02/26
### Fix
- Generate AssetBundle feature bug: same name AssetBundle and AssetBundleList causes export error.
- should ignore Persistent filepath starts with ".".

### Feature
- Persistent file delete manager added. Window > Autoya > Persistence > ...

### Changed
- AssetGraph postprocess code for Autoya is renamed from "MyPostprocess.cs" to "AutoyaAssetBundleListGenerateProcess.cs".


## [0.8.27] - 2018/02/23
### Fix
- no need to check AssetBundle feature state when checking App-update.

## [0.8.26] - 2018/01/30
### Fix
- AssetBundle download path combination was not valid.

### Changed
- downloading AssetBundleList manually now requires RuntimeManifest description.


## [0.8.25] - 2018/01/24
### Feature
- add the method for downloading AssetBundleList manually.


## [0.8.24] - 2017/12/18
### Feature
- multiple AssetBundle lists enabled. a lot of method signatures are changed.


## [0.8.23] - 2017/11/21
### Changed
- PurchaseRouter constructor is changed. added the handler for notify when some uncompleted purchase is done in background.
- Udp receiver gets new "send udp API". both send&receive are enabled in receiver.

## [0.8.22] - 2017/11/09
### Added
- Udp receiver/server feature is implemented.

## [0.8.21] - 2017/10/26
### Changed
- Preloader behaviour changed. never preload "using" AssetBundles.

## [0.8.20] - 2017/10/25
### Added
- update old versioned on memory AssetBundle to new one by using LoadAsset or Preload.

## [0.8.19] - 2017/10/13
### Added
- AppManifest feature now supports cloudbuild param.

## [0.8.18] - 2017/10/11
### Changed
- AssetBundles feature signatures and related OverridePoints are dynamically changed.

## [0.8.17] - 2017/10/08
### Added
- AppManifest feature. see AutoyaSample/10_AppManifest/AppManifest.unity.
- Http feature now enable to add user defined connection id for each connection.

### Fixed
- autoyaStatus always returns valid status when Http feature returns authentication failed.

## [0.8.16] - 2017-09-29
### Added
- UUebView library bug fixed. using v1.0.2.

### Changed
- OverridePoint.cs OnPurchaseReadyFailed method parameter is changed.


## [0.8.15] - 2017-09-14
### Changed
- UUebView library bug fixed. using v1.0.2.

## [0.8.14] - 2017-09-14
### Fixed
- UUebView library bug fixed. using v1.0.1.


## [0.8.13] - 2017-09-07

### Changed
- Start using "changelog".
- Rewrite something.
- Improve something.
- Merge something.
- Fix something like typos.

### Added
- Something.

### Fixed
- Something.

### Removed
- Something.


[Unreleased]: https://github.com/sassembla/autoya/compare/0.9.9...HEAD
[0.9.9]: https://github.com/sassembla/autoya/compare/0.9.8...0.9.9
[0.9.8]: https://github.com/sassembla/autoya/compare/0.9.7...0.9.8
[0.9.7]: https://github.com/sassembla/autoya/compare/0.9.6...0.9.7
[0.9.6]: https://github.com/sassembla/autoya/compare/0.9.5...0.9.6
[0.9.5]: https://github.com/sassembla/autoya/compare/0.9.4...0.9.5
[0.9.4]: https://github.com/sassembla/autoya/compare/0.9.3...0.9.4
[0.9.3]: https://github.com/sassembla/autoya/compare/0.9.2...0.9.3
[0.9.2]: https://github.com/sassembla/autoya/compare/0.9.1...0.9.2
[0.9.1]: https://github.com/sassembla/autoya/compare/0.9.0...0.9.1
[0.9.0]: https://github.com/sassembla/autoya/compare/0.8.47...0.9.0
[0.8.47]: https://github.com/sassembla/autoya/compare/0.8.46...0.8.47
[0.8.46]: https://github.com/sassembla/autoya/compare/0.8.45...0.8.46
[0.8.45]: https://github.com/sassembla/autoya/compare/0.8.44...0.8.45
[0.8.44]: https://github.com/sassembla/autoya/compare/0.8.43...0.8.44
[0.8.43]: https://github.com/sassembla/autoya/compare/0.8.42...0.8.43
[0.8.42]: https://github.com/sassembla/autoya/compare/0.8.41...0.8.42
[0.8.41]: https://github.com/sassembla/autoya/compare/0.8.40...0.8.41
[0.8.40]: https://github.com/sassembla/autoya/compare/0.8.32...0.8.40
[0.8.32]: https://github.com/sassembla/autoya/compare/0.8.31...0.8.32
[0.8.31]: https://github.com/sassembla/autoya/compare/0.8.30...0.8.31
[0.8.30]: https://github.com/sassembla/autoya/compare/0.8.29...0.8.30
[0.8.29]: https://github.com/sassembla/autoya/compare/0.8.28...0.8.29
[0.8.28]: https://github.com/sassembla/autoya/compare/0.8.27...0.8.28
[0.8.27]: https://github.com/sassembla/autoya/compare/0.8.26...0.8.27
[0.8.26]: https://github.com/sassembla/autoya/compare/0.8.25...0.8.26
[0.8.25]: https://github.com/sassembla/autoya/compare/0.8.24...0.8.25
[0.8.24]: https://github.com/sassembla/autoya/compare/0.8.23...0.8.24
[0.8.23]: https://github.com/sassembla/autoya/compare/0.8.22...0.8.23
[0.8.22]: https://github.com/sassembla/autoya/compare/0.8.21...0.8.22
[0.8.21]: https://github.com/sassembla/autoya/compare/0.8.20...0.8.21
[0.8.20]: https://github.com/sassembla/autoya/compare/0.8.19...0.8.20
[0.8.19]: https://github.com/sassembla/autoya/compare/0.8.18...0.8.19
[0.8.18]: https://github.com/sassembla/autoya/compare/0.8.17...0.8.18
[0.8.17]: https://github.com/sassembla/autoya/compare/0.8.16...0.8.17
[0.8.16]: https://github.com/sassembla/autoya/compare/0.8.15...0.8.16
[0.8.15]: https://github.com/sassembla/autoya/compare/0.8.14...0.8.15
[0.8.14]: https://github.com/sassembla/autoya/compare/0.8.13...0.8.14
[0.8.13]: https://github.com/sassembla/autoya/compare/0.8.13...0.8.13