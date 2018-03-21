# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Romantic Versioning](http://blog.legacyteam.info/2015/12/romver-romantic-versioning/).

## [Unreleased]

## [0.8.33]	- 2018/03/22
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

[Unreleased]: https://github.com/sassembla/autoya/compare/0.8.33...HEAD
[0.8.33]: https://github.com/sassembla/autoya/compare/0.8.32...0.8.33
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