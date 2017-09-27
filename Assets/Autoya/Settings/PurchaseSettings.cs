namespace AutoyaFramework.Purchase {
	public class PurchaseSettings {
		
		/*
			immutable purchasable item infos.
			*/
		public static readonly ProductInfos IMMUTABLE_PURCHASE_ITEM_INFOS = new ProductInfos {
			productInfos = new ProductInfo[] {
				new ProductInfo("100_gold_coins", "100_gold_coins_iOS", true, "one hundled of coins."),
				new ProductInfo("1000_gold_coins", "1000_gold_coins_iOS", true, "one ton of coins."),
				new ProductInfo("10000_gold_coins", "10000_gold_coins_iOS", false, "ten tons of coins."),// this product setting is example of not allow to buy for this player, disable to buy but need to be displayed.
			}
		};

		/*
			urls and prefixies.
		*/
		public const string PURCHASE_URL_READY = "https://httpbin.org/get";
		public const string PURCHASE_CONNECTIONID_READY_PREFIX = "purchase_ready_";

		public const string PURCHASE_URL_TICKET = "https://httpbin.org/post";
		public const string PURCHASE_CONNECTIONID_TICKET_PREFIX = "purchase_start_";
		
		public const string PURCHASE_URL_PURCHASE = "https://httpbin.org/post";
		public const string PURCHASE_CONNECTIONID_PURCHASE_PREFIX = "purchase_succeeded_";

		public const string PURCHASE_URL_PAID = "https://httpbin.org/post";
		public const string PURCHASE_CONNECTIONID_PAID_PREFIX = "purchase_paid_";

		public const string PURCHASE_URL_CANCEL = "https://httpbin.org/post";
		public const string PURCHASE_CONNECTIONID_CANCEL_PREFIX = "purchase_cancelled_";

		public const double TIMEOUT_SEC = 10.0;

		public const int PEADY_MAX_RETRY_COUNT = 3;
		public const int PURCHASED_MAX_RETRY_COUNT = 3;
	}
}