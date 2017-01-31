namespace AutoyaFramework.Purchase {
    public class PurchaseSettings {
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
    }
}