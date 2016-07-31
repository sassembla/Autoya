using Miyamasu;
using UnityEditor;

[InitializeOnLoad] public class TestIgniter {
	static TestIgniter () {
		/*
			フレームワークが準備完了かどうかを試したいんだよな〜っていう感じで、

			ログイン/ログアウト
				Authentication(このへん綺麗に切り直したいところ。実際のところなんなんだ？っていう。)

			オンライン/オフライン
				Connection

			手元にアセットあり/なし
				AssetBundle

			サーバとの関係性ステータス、Maintenance/ShouldUpdateApp/PleaseUpdateApp/UpdateAssets
				ConditionAgainstServer(仮)
				
			frameworkが持ってることで使いたい機能
			・通信周り
				通信のヘッダとか保持しててほしい、毎回入れるのめんどい(authと近い)

			・ログイン
				勝手にログインしててほしい、それを待ちたい

			・Asset
				リソース一覧取得したい
				必要なリソース取得したい
				
			・サーバとの関係性を察知して特定の関数を呼びたい
				オフライン > オフラインのハンドラ着火
				サーバがメンテ中 > メンテ中のハンドラ着火
				サーバがお前のAppちょっと古いからどうにかしたら？って返してくる > ShallUpdateApp着火
				サーバがお前のAppほんと古いからアプデしないとダメって返してくる > ShouldUpdateApp着火
				サーバがお前のAppの持ってるリソースリスト古いからリソース取得しないとダメって返してくる > ShouldUpdateAssets着火
				通信の失敗を返してくる
				通信の成功を返してくる

			・ビルド番号
				保持したいねえ。どっかにテキスト吐くか。StandardSettingsとかかな。得意なスタイルだ。
		
			・その他の情報
				urlとかは管理しなくてよくね？ うん、やめよう。
				ただし入れやすいような何かを用意するかな？
				ログインとかはあるんで、それらのための情報をどうにかして入れようっていう感じがする。
				URLHolderみたいなのがあればそれで良い気がする。

			これらのハンドラをちゃんとデザインしよう。
			
		*/

		var testRunner = new MiyamasuTestRunner();
		testRunner.RunTests();
	}
}