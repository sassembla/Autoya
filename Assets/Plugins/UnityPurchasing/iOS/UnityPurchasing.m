#import "UnityPurchasing.h"
#if MAC_APPSTORE
#import "Base64.h"
#endif

#if !MAC_APPSTORE
#import "UnityEarlyTransactionObserver.h"
#endif

@implementation ProductDefinition

@synthesize id;
@synthesize storeSpecificId;
@synthesize type;

@end

void UnityPurchasingLog(NSString *format, ...) {
    va_list args;
    va_start(args, format);
    NSString *message = [[NSString alloc] initWithFormat:format arguments:args];
    va_end(args);

    NSLog(@"UnityIAP: %@", message);
}


@implementation ReceiptRefresher

-(id) initWithCallback:(void (^)(BOOL))callbackBlock {
    self.callback = callbackBlock;
    return [super init];
}

-(void) requestDidFinish:(SKRequest *)request {
    self.callback(true);
}

-(void) request:(SKRequest *)request didFailWithError:(NSError *)error {
    self.callback(false);
}

@end

#if !MAC_APPSTORE
@interface UnityPurchasing ()<UnityEarlyTransactionObserverDelegate>
@end
#endif

@implementation UnityPurchasing

// The max time we wait in between retrying failed SKProductRequests.
static const int MAX_REQUEST_PRODUCT_RETRY_DELAY = 60;

// Track our accumulated delay.
int delayInSeconds = 2;

-(NSString*) getAppReceipt {

    NSBundle* bundle = [NSBundle mainBundle];
    if ([bundle respondsToSelector:@selector(appStoreReceiptURL)]) {
        NSURL *receiptURL = [bundle appStoreReceiptURL];
        if ([[NSFileManager defaultManager] fileExistsAtPath:[receiptURL path]]) {
            NSData *receipt = [NSData dataWithContentsOfURL:receiptURL];

#if MAC_APPSTORE
            // The base64EncodedStringWithOptions method was only added in OSX 10.9.
            NSString* result = [receipt mgb64_base64EncodedString];
#else
            NSString* result = [receipt base64EncodedStringWithOptions:0];
#endif

            return result;
        }
    }

    UnityPurchasingLog(@"No App Receipt found");
    return @"";
}

-(NSString*) getTransactionReceiptForProductId:(NSString *)productId {
    NSString *result = transactionReceipts[productId];
    if (!result) {
        UnityPurchasingLog(@"No Transaction Receipt found for product %@", productId);
    }
    return result ?: @"";
}

-(void) UnitySendMessage:(NSString*) subject payload:(NSString*) payload {
    messageCallback(subject.UTF8String, payload.UTF8String, @"".UTF8String, @"".UTF8String);
}

-(void) UnitySendMessage:(NSString*) subject payload:(NSString*) payload receipt:(NSString*) receipt {
    messageCallback(subject.UTF8String, payload.UTF8String, receipt.UTF8String, @"".UTF8String);
}

-(void) UnitySendMessage:(NSString*) subject payload:(NSString*) payload receipt:(NSString*) receipt transactionId:(NSString*) transactionId {
    messageCallback(subject.UTF8String, payload.UTF8String, receipt.UTF8String, transactionId.UTF8String);
}

-(void) setCallback:(UnityPurchasingCallback)callback {
    messageCallback = callback;
}

#if !MAC_APPSTORE
-(BOOL) isiOS6OrEarlier {
    float version = [[[UIDevice currentDevice] systemVersion] floatValue];
    return version < 7;
}
#endif

// Retrieve a receipt for the transaction, which will either
// be the old style transaction receipt on <= iOS 6,
// or the App Receipt in OSX and iOS 7+.
-(NSString*) selectReceipt:(SKPaymentTransaction*) transaction {
#if MAC_APPSTORE
    return [self getAppReceipt];
#else
    if ([self isiOS6OrEarlier]) {
        if (nil == transaction) {
            return @"";
        }
        NSString* receipt;
        receipt = [[NSString alloc] initWithData:transaction.transactionReceipt encoding: NSUTF8StringEncoding];

        return receipt;
    } else {
        return [self getAppReceipt];
    }
#endif
}

-(void) refreshReceipt {
    #if !MAC_APPSTORE
    if ([self isiOS6OrEarlier]) {
        UnityPurchasingLog(@"RefreshReceipt not supported on iOS < 7!");
        return;
    }
    #endif

    self.receiptRefresher = [[ReceiptRefresher alloc] initWithCallback:^(BOOL success) {
        UnityPurchasingLog(@"RefreshReceipt status %d", success);
        if (success) {
            [self UnitySendMessage:@"onAppReceiptRefreshed" payload:[self getAppReceipt]];
        } else {
            [self UnitySendMessage:@"onAppReceiptRefreshFailed" payload:nil];
        }
    }];
    self.refreshRequest = [[SKReceiptRefreshRequest alloc] init];
    self.refreshRequest.delegate = self.receiptRefresher;
    [self.refreshRequest start];
}

// Handle a new or restored purchase transaction by informing Unity.
- (void)onTransactionSucceeded:(SKPaymentTransaction*)transaction {
    NSString* transactionId = transaction.transactionIdentifier;

    // This should never happen according to Apple's docs, but it does!
    if (nil == transactionId) {
        // Make something up, allowing us to identifiy the transaction when finishing it.
        transactionId = [[NSUUID UUID] UUIDString];
        UnityPurchasingLog(@"Missing transaction Identifier!");
    }

    // This transaction was marked as finished, but was not cleared from the queue. Try to clear it now, then pass the error up the stack as a DuplicateTransaction
    if ([finishedTransactions containsObject:transactionId]) {
        [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
        UnityPurchasingLog(@"DuplicateTransaction error with product %@ and transactionId %@", transaction.payment.productIdentifier, transactionId);
        [self onPurchaseFailed:transaction.payment.productIdentifier reason:@"DuplicateTransaction" errorCode:@"" errorDescription:@"Duplicate transaction occurred"];
        return; // EARLY RETURN
    }

    // Item was successfully purchased or restored.
    if (nil == [pendingTransactions objectForKey:transactionId]) {
        [pendingTransactions setObject:transaction forKey:transactionId];
    }

    [self UnitySendMessage:@"OnPurchaseSucceeded" payload:transaction.payment.productIdentifier receipt:[self selectReceipt:transaction] transactionId:transactionId];
}

// Called back by managed code when the tranaction has been logged.
-(void) finishTransaction:(NSString *)transactionIdentifier {
    SKPaymentTransaction* transaction = [pendingTransactions objectForKey:transactionIdentifier];
    if (nil != transaction) {
        UnityPurchasingLog(@"Finishing transaction %@", transactionIdentifier);
        [[SKPaymentQueue defaultQueue] finishTransaction:transaction]; // If this fails (user not logged into the store?), transaction is already removed from pendingTransactions, so future calls to finishTransaction will not retry
        [pendingTransactions removeObjectForKey:transactionIdentifier];
        [finishedTransactions addObject:transactionIdentifier];
    } else {
        UnityPurchasingLog(@"Transaction %@ not pending, nothing to finish here", transactionIdentifier);
    }
}

// Request information about our products from Apple.
-(void) requestProducts:(NSSet*)paramIds
{
    productIds = paramIds;
    UnityPurchasingLog(@"Requesting %lu products", (unsigned long) [productIds count]);
    // Start an immediate poll.
    [self initiateProductPoll:0];
}

// Execute a product metadata retrieval request via GCD.
-(void) initiateProductPoll:(int) delayInSeconds
{
    dispatch_time_t popTime = dispatch_time(DISPATCH_TIME_NOW, delayInSeconds * NSEC_PER_SEC);
    dispatch_after(popTime, dispatch_get_main_queue(), ^(void) {
        UnityPurchasingLog(@"Requesting product data...");
        request = [[SKProductsRequest alloc] initWithProductIdentifiers:productIds];
        request.delegate = self;
        [request start];
    });
}

// Called by managed code when a user requests a purchase.
-(void) purchaseProduct:(ProductDefinition*)productDef
{
    // Look up our corresponding product.
    SKProduct* requestedProduct = [validProducts objectForKey:productDef.storeSpecificId];

    if (requestedProduct != nil) {
        UnityPurchasingLog(@"PurchaseProduct: %@", requestedProduct.productIdentifier);

        if ([SKPaymentQueue canMakePayments]) {
            SKMutablePayment *payment = [SKMutablePayment paymentWithProduct:requestedProduct];

            // Modify payment request for testing ask-to-buy
            if (_simulateAskToBuyEnabled) {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wundeclared-selector"
                if ([payment respondsToSelector:@selector(setSimulatesAskToBuyInSandbox:)]) {
                    UnityPurchasingLog(@"Queueing payment request with simulatesAskToBuyInSandbox enabled");
                    [payment performSelector:@selector(setSimulatesAskToBuyInSandbox:) withObject:@YES];
                    //payment.simulatesAskToBuyInSandbox = YES;
                }
#pragma clang diagnostic pop
            }

            // Modify payment request with "applicationUsername" for fraud detection
            if (_applicationUsername != nil) {
                if ([payment respondsToSelector:@selector(setApplicationUsername:)]) {
                    UnityPurchasingLog(@"Setting applicationUsername to %@", _applicationUsername);
                    [payment performSelector:@selector(setApplicationUsername:) withObject:_applicationUsername];
                    //payment.applicationUsername = _applicationUsername;
                }
            }

            [[SKPaymentQueue defaultQueue] addPayment:payment];
        } else {
            UnityPurchasingLog(@"PurchaseProduct: IAP Disabled");
            [self onPurchaseFailed:productDef.storeSpecificId reason:@"PurchasingUnavailable" errorCode:@"SKErrorPaymentNotAllowed" errorDescription:@"User is not authorized to make payments"];
        }

    } else {
        [self onPurchaseFailed:productDef.storeSpecificId reason:@"ItemUnavailable" errorCode:@"" errorDescription:@"Unity IAP could not find requested product"];
    }
}

// Initiate a request to Apple to restore previously made purchases.
-(void) restorePurchases
{
    UnityPurchasingLog(@"RestorePurchase");
    [[SKPaymentQueue defaultQueue] restoreCompletedTransactions];
}

// A transaction observer should be added at startup (by managed code)
// and maintained for the life of the app, since transactions can
// be delivered at any time.
-(void) addTransactionObserver {
    SKPaymentQueue* defaultQueue = [SKPaymentQueue defaultQueue];

    // Detect whether an existing transaction observer is in place.
    // An existing observer will have processed any transactions already pending,
    // so when we add our own storekit will not call our updatedTransactions handler.
    // We workaround this by explicitly processing any existing transactions if they exist.
    BOOL processExistingTransactions = false;
    if (defaultQueue != nil && defaultQueue.transactions != nil)
    {
        if ([[defaultQueue transactions] count] > 0) {
            processExistingTransactions = true;
        }
    }

    [defaultQueue addTransactionObserver:self];
    if (processExistingTransactions) {
        [self paymentQueue:defaultQueue updatedTransactions:defaultQueue.transactions];
    }

#if !MAC_APPSTORE
    UnityEarlyTransactionObserver *observer = [UnityEarlyTransactionObserver defaultObserver];
    if (observer) {
        observer.readyToReceiveTransactionUpdates = YES;
        if (self.interceptPromotionalPurchases) {
            observer.delegate = self;
        } else {
            [observer initiateQueuedPayments];
        }
    }
#endif
}

- (void)initiateQueuedEarlyTransactionObserverPayments {
#if !MAC_APPSTORE
    [[UnityEarlyTransactionObserver defaultObserver] initiateQueuedPayments];
#endif
}

#if !MAC_APPSTORE
#pragma mark -
#pragma mark UnityEarlyTransactionObserverDelegate Methods

- (void)promotionalPurchaseAttempted:(SKPayment *)payment {
    UnityPurchasingLog(@"Promotional purchase attempted");
    [self UnitySendMessage:@"onPromotionalPurchaseAttempted" payload:payment.productIdentifier];
}

#endif

#pragma mark -
#pragma mark SKProductsRequestDelegate Methods

// Store Kit returns a response from an SKProductsRequest.
- (void)productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response {

    UnityPurchasingLog(@"Received %lu products", (unsigned long) [response.products count]);
    // Add the retrieved products to our set of valid products.
    NSDictionary* fetchedProducts = [NSDictionary dictionaryWithObjects:response.products forKeys:[response.products valueForKey:@"productIdentifier"]];
    [validProducts addEntriesFromDictionary:fetchedProducts];

    NSString* productJSON = [UnityPurchasing serializeProductMetadata:response.products];

    // Send the app receipt as a separate parameter to avoid JSON parsing a large string.
    [self UnitySendMessage:@"OnProductsRetrieved" payload:productJSON receipt:[self selectReceipt:nil] ];
}


#pragma mark -
#pragma mark SKPaymentTransactionObserver Methods
// A product metadata retrieval request failed.
// We handle it by retrying at an exponentially increasing interval.
- (void)request:(SKRequest *)request didFailWithError:(NSError *)error {
    delayInSeconds = MIN(MAX_REQUEST_PRODUCT_RETRY_DELAY, 2 * delayInSeconds);
    UnityPurchasingLog(@"SKProductRequest::didFailWithError: %ld, %@. Unity Purchasing will retry in %i seconds", (long)error.code, error.description, delayInSeconds);

    [self initiateProductPoll:delayInSeconds];
}

- (void)requestDidFinish:(SKRequest *)req {
    request = nil;
}

- (void)onPurchaseFailed:(NSString*) productId reason:(NSString*)reason errorCode:(NSString*)errorCode errorDescription:(NSString*)errorDescription {
    NSMutableDictionary* dic = [[NSMutableDictionary alloc] init];
    [dic setObject:productId forKey:@"productId"];
    [dic setObject:reason forKey:@"reason"];
    [dic setObject:errorCode forKey:@"storeSpecificErrorCode"];
    [dic setObject:errorDescription forKey:@"message"];

    NSData* data = [NSJSONSerialization dataWithJSONObject:dic options:0 error:nil];
    NSString* result = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];

    [self UnitySendMessage:@"OnPurchaseFailed" payload:result];
}

- (NSString*)purchaseErrorCodeToReason:(NSInteger) errorCode {
    switch (errorCode) {
        case SKErrorPaymentCancelled:
            return @"UserCancelled";
        case SKErrorPaymentInvalid:
            return @"PaymentDeclined";
        case SKErrorPaymentNotAllowed:
            return @"PurchasingUnavailable";
    }

    return @"Unknown";
}

// The transaction status of the SKPaymentQueue is sent here.
- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions {
    UnityPurchasingLog(@"UpdatedTransactions");
    for(SKPaymentTransaction *transaction in transactions) {
        switch (transaction.transactionState) {

            case SKPaymentTransactionStatePurchasing:
                // Item is still in the process of being purchased
                break;

            case SKPaymentTransactionStatePurchased: {
#if MAC_APPSTORE
                // There is no transactionReceipt on Mac
                NSString* receipt = @"";
#else
                // The transactionReceipt field is deprecated, but is being used here to validate Ask-To-Buy purchases
                NSString* receipt = [transaction.transactionReceipt base64EncodedStringWithOptions:0];
#endif
                if (transaction.payment.productIdentifier != nil) {
                    transactionReceipts[transaction.payment.productIdentifier] = receipt;
                }
                [self onTransactionSucceeded:transaction];
                break;
            }
            case SKPaymentTransactionStateRestored: {
                [self onTransactionSucceeded:transaction];
                break;
            }
            case SKPaymentTransactionStateDeferred:
                UnityPurchasingLog(@"PurchaseDeferred");
                [self UnitySendMessage:@"onProductPurchaseDeferred" payload:transaction.payment.productIdentifier];
                break;
            case SKPaymentTransactionStateFailed: {
                // Purchase was either cancelled by user or an error occurred.
                NSString* errorCode = [NSString stringWithFormat:@"%ld",(long)transaction.error.code];
                UnityPurchasingLog(@"PurchaseFailed: %@", errorCode);

                NSString* reason = [self purchaseErrorCodeToReason:transaction.error.code];
                NSString* errorCodeString = [UnityPurchasing storeKitErrorCodeNames][@(transaction.error.code)];
                if (errorCodeString == nil) {
                    errorCodeString = @"SKErrorUnknown";
                }
                NSString* errorDescription = [NSString stringWithFormat:@"APPLE_%@", transaction.error.localizedDescription];
                [self onPurchaseFailed:transaction.payment.productIdentifier reason:reason errorCode:errorCodeString errorDescription:errorDescription];

                // Finished transactions should be removed from the payment queue.
                [[SKPaymentQueue defaultQueue] finishTransaction: transaction];
            }
                break;
        }
    }
}

// Called when one or more transactions have been removed from the queue.
- (void)paymentQueue:(SKPaymentQueue *)queue removedTransactions:(NSArray *)transactions
{
    // Nothing to do here.
}

// Called when SKPaymentQueue has finished sending restored transactions.
- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue *)queue {

    UnityPurchasingLog(@"PaymentQueueRestoreCompletedTransactionsFinished");
    [self UnitySendMessage:@"onTransactionsRestoredSuccess" payload:@""];
}

// Called if an error occurred while restoring transactions.
- (void)paymentQueue:(SKPaymentQueue *)queue restoreCompletedTransactionsFailedWithError:(NSError *)error
{
    UnityPurchasingLog(@"restoreCompletedTransactionsFailedWithError");
    // Restore was cancelled or an error occurred, so notify user.

    [self UnitySendMessage:@"onTransactionsRestoredFail" payload:error.localizedDescription];
}

- (void)updateStorePromotionOrder:(NSArray*)productIds
{
#if __IPHONE_OS_VERSION_MAX_ALLOWED >= 110000
    if (@available(iOS 11_0, *))
    {
        NSMutableArray* products = [[NSMutableArray alloc] init];

        for (NSString* productId in productIds) {
            SKProduct* product = [validProducts objectForKey:productId];
            if (product)
                [products addObject:product];
        }

        SKProductStorePromotionController* controller = [SKProductStorePromotionController defaultController];
        [controller updateStorePromotionOrder:products completionHandler:^(NSError* error) {
            if (error)
                UnityPurchasingLog(@"Error in updateStorePromotionOrder: %@ - %@ - %@", [error code], [error domain], [error localizedDescription]);
        }];
    }
    else
#endif
    {
        UnityPurchasingLog(@"Update store promotion order is only available on iOS and tvOS 11 or later");
    }
}

// visibility should be one of "Default", "Hide", or "Show"
- (void)updateStorePromotionVisibility:(NSString*)visibility forProduct:(NSString*)productId
{
#if __IPHONE_OS_VERSION_MAX_ALLOWED >= 110000
    if (@available(iOS 11_0, *))
    {
        SKProduct *product = [validProducts objectForKey:productId];
        if (!product) {
            UnityPurchasingLog(@"updateStorePromotionVisibility unable to find product %@", productId);
            return;
        }

        SKProductStorePromotionVisibility v = SKProductStorePromotionVisibilityDefault;
        if ([visibility isEqualToString:@"Hide"])
            v = SKProductStorePromotionVisibilityHide;
        else if ([visibility isEqualToString:@"Show"])
            v = SKProductStorePromotionVisibilityShow;

        SKProductStorePromotionController* controller = [SKProductStorePromotionController defaultController];
        [controller updateStorePromotionVisibility:v forProduct:product completionHandler:^(NSError* error) {
            if (error)
                UnityPurchasingLog(@"Error in updateStorePromotionVisibility: %@ - %@ - %@", [error code], [error domain], [error localizedDescription]);
        }];
    }
    else
#endif
    {
        UnityPurchasingLog(@"Update store promotion visibility is only available on iOS and tvOS 11 or later");
    }
}


- (BOOL)paymentQueue:(SKPaymentQueue *)queue shouldAddStorePayment:(SKPayment *)payment forProduct:(SKProduct *)product {
#if __IPHONE_OS_VERSION_MAX_ALLOWED >= 110000
    if (@available(iOS 11_0, *)) {
        // Just defer to the early transaction observer. This should have no effect, just return whatever the observer returns.
        return [[UnityEarlyTransactionObserver defaultObserver] paymentQueue:queue shouldAddStorePayment:payment forProduct:product];
    }
#endif
    return YES;
}

+(ProductDefinition*) decodeProductDefinition:(NSDictionary*) hash
{
    ProductDefinition* product = [[ProductDefinition alloc] init];
    product.id = [hash objectForKey:@"id"];
    product.storeSpecificId = [hash objectForKey:@"storeSpecificId"];
    product.type = [hash objectForKey:@"type"];
    return product;
}

+ (NSArray*) deserializeProductDefs:(NSString*)json
{
    NSData* data = [json dataUsingEncoding:NSUTF8StringEncoding];
    NSArray* hashes = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];

    NSMutableArray* result = [[NSMutableArray alloc] init];
    for (NSDictionary* hash in hashes) {
        [result addObject:[self decodeProductDefinition:hash]];
    }

    return result;
}

+ (ProductDefinition*) deserializeProductDef:(NSString*)json
{
    NSData* data = [json dataUsingEncoding:NSUTF8StringEncoding];
    NSDictionary* hash = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [self decodeProductDefinition:hash];
}

+ (NSString*) serializeProductMetadata:(NSArray*)appleProducts
{
    NSMutableArray* hashes = [[NSMutableArray alloc] init];
    for (id product in appleProducts) {
        if (NULL == [product productIdentifier]) {
            UnityPurchasingLog(@"Product is missing an identifier!");
            continue;
        }

        NSMutableDictionary* hash = [[NSMutableDictionary alloc] init];
        [hashes addObject:hash];

        [hash setObject:[product productIdentifier] forKey:@"storeSpecificId"];

        NSMutableDictionary* metadata = [[NSMutableDictionary alloc] init];
        [hash setObject:metadata forKey:@"metadata"];

        if (NULL != [product price]) {
            [metadata setObject:[product price] forKey:@"localizedPrice"];
        }

        if (NULL != [product priceLocale]) {
            NSString *currencyCode = [[product priceLocale] objectForKey:NSLocaleCurrencyCode];
            [metadata setObject:currencyCode forKey:@"isoCurrencyCode"];
        }

#if __IPHONE_OS_VERSION_MAX_ALLOWED >= 110000 || __TV_OS_VERSION_MAX_ALLOWED >= 110000 || __MAC_OS_X_VERSION_MAX_ALLOWED >= 101300
        if ((@available(iOS 11_2, macOS 10_13_2, tvOS 11_2, *)) && (nil != [product introductoryPrice]))  {
            [metadata setObject:[[product introductoryPrice] price] forKey:@"introductoryPrice"];
            if (nil != [[product introductoryPrice] priceLocale]) {
                NSString *currencyCode = [[[product introductoryPrice] priceLocale] objectForKey:NSLocaleCurrencyCode];
                [metadata setObject:currencyCode forKey:@"introductoryPriceLocale"];
            } else {
                [metadata setObject:@"" forKey:@"introductoryPriceLocale"];
            }
            if (nil != [[product introductoryPrice] numberOfPeriods]) {
                NSNumber *numberOfPeriods = [NSNumber numberWithInt:[[product introductoryPrice] numberOfPeriods]];
                [metadata setObject:numberOfPeriods forKey:@"introductoryPriceNumberOfPeriods"];
            } else {
                [metadata setObject:@"" forKey:@"introductoryPriceNumberOfPeriods"];
            }
            if (nil != [[product introductoryPrice] subscriptionPeriod]) {
                if (nil != [[[product introductoryPrice] subscriptionPeriod] numberOfUnits]) {
                    NSNumber *numberOfUnits = [NSNumber numberWithInt:[[[product introductoryPrice] subscriptionPeriod] numberOfUnits]];
                    [metadata setObject:numberOfUnits forKey:@"numberOfUnits"];
                } else {
                    [metadata setObject:@"" forKey:@"numberOfUnits"];
                }
                if (nil != [[[product introductoryPrice] subscriptionPeriod] unit]) {
                    NSNumber *unit = [NSNumber numberWithInt:[[[product introductoryPrice] subscriptionPeriod] unit]];
                    [metadata setObject:unit forKey:@"unit"];
                } else {
                    [metadata setObject:@"" forKey:@"unit"];
                }
            } else {
                [metadata setObject:@"" forKey:@"numberOfUnits"];
                [metadata setObject:@"" forKey:@"unit"];
            }
        } else {
            [metadata setObject:@"" forKey:@"introductoryPrice"];
            [metadata setObject:@"" forKey:@"introductoryPriceLocale"];
            [metadata setObject:@"" forKey:@"introductoryPriceNumberOfPeriods"];
            [metadata setObject:@"" forKey:@"numberOfUnits"];
            [metadata setObject:@"" forKey:@"unit"];
        }
#else
        [metadata setObject:@"" forKey:@"introductoryPrice"];
        [metadata setObject:@"" forKey:@"introductoryPriceLocale"];
        [metadata setObject:@"" forKey:@"introductoryPriceNumberOfPeriods"];
        [metadata setObject:@"" forKey:@"numberOfUnits"];
        [metadata setObject:@"" forKey:@"unit"];
#endif


        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        [numberFormatter setFormatterBehavior:NSNumberFormatterBehavior10_4];
        [numberFormatter setNumberStyle:NSNumberFormatterCurrencyStyle];
        [numberFormatter setLocale:[product priceLocale]];
        NSString *formattedString = [numberFormatter stringFromNumber:[product price]];

        if (NULL == formattedString) {
            UnityPurchasingLog(@"Unable to format a localized price");
            [metadata setObject:@"" forKey:@"localizedPriceString"];
        } else {
            [metadata setObject:formattedString forKey:@"localizedPriceString"];
        }
        if (NULL == [product localizedTitle]) {
            UnityPurchasingLog(@"No localized title for: %@. Have your products been disapproved in itunes connect?", [product productIdentifier]);
            [metadata setObject:@"" forKey:@"localizedTitle"];
        } else {
            [metadata setObject:[product localizedTitle] forKey:@"localizedTitle"];
        }

        if (NULL == [product localizedDescription]) {
            UnityPurchasingLog(@"No localized description for: %@. Have your products been disapproved in itunes connect?", [product productIdentifier]);
            [metadata setObject:@"" forKey:@"localizedDescription"];
        } else {
            [metadata setObject:[product localizedDescription] forKey:@"localizedDescription"];
        }
    }


    NSData *data = [NSJSONSerialization dataWithJSONObject:hashes options:0 error:nil];
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}

+ (NSArray*) deserializeProductIdList:(NSString*)json
{
    NSData* data = [json dataUsingEncoding:NSUTF8StringEncoding];
    NSDictionary* dict = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [[dict objectForKey:@"products"] copy];
}

// Note: this will need to be updated if Apple ever adds more StoreKit error codes.
+ (NSDictionary<NSNumber *, NSString *> *)storeKitErrorCodeNames
{
    return @{
             @(SKErrorUnknown) : @"SKErrorUnknown",
             @(SKErrorClientInvalid) : @"SKErrorClientInvalid",
             @(SKErrorPaymentCancelled) : @"SKErrorPaymentCancelled",
             @(SKErrorPaymentInvalid) : @"SKErrorPaymentInvalid",
             @(SKErrorPaymentNotAllowed) : @"SKErrorPaymentNotAllowed",
#if !MAC_APPSTORE
             @(SKErrorStoreProductNotAvailable) : @"SKErrorStoreProductNotAvailable",
             @(SKErrorCloudServicePermissionDenied) : @"SKErrorCloudServicePermissionDenied",
             @(SKErrorCloudServiceNetworkConnectionFailed) : @"SKErrorCloudServiceNetworkConnectionFailed",
#endif
#if !MAC_APPSTORE && (__IPHONE_OS_VERSION_MAX_ALLOWED >= 103000 || __TV_OS_VERSION_MAX_ALLOWED >= 103000)
             @(SKErrorCloudServiceRevoked) : @"SKErrorCloudServiceRevoked",
#endif
             };
}

#pragma mark - Internal Methods & Events

- (id)init {
    if ( self = [super init] ) {
        validProducts = [[NSMutableDictionary alloc] init];
        pendingTransactions = [[NSMutableDictionary alloc] init];
        finishedTransactions = [[NSMutableSet alloc] init];
        transactionReceipts = [[NSMutableDictionary alloc] init];
    }
    return self;
}

@end

UnityPurchasing* UnityPurchasing_instance = NULL;

UnityPurchasing* UnityPurchasing_getInstance() {
    if (NULL == UnityPurchasing_instance) {
        UnityPurchasing_instance = [[UnityPurchasing alloc] init];
    }
    return UnityPurchasing_instance;
}

// Make a heap allocated copy of a string.
// This is suitable for passing to managed code,
// which will free the string when it is garbage collected.
// Stack allocated variables must not be returned as results
// from managed to native calls.
char* UnityPurchasingMakeHeapAllocatedStringCopy (NSString* string)
{
    if (NULL == string) {
        return NULL;
    }
    char* res = (char*)malloc([string length] + 1);
    strcpy(res, [string UTF8String]);
    return res;
}

void setUnityPurchasingCallback(UnityPurchasingCallback callback) {
    [UnityPurchasing_getInstance() setCallback:callback];
}

void unityPurchasingRetrieveProducts(const char* json) {
    NSString* str = [NSString stringWithUTF8String:json];
    NSArray* productDefs = [UnityPurchasing deserializeProductDefs:str];
    NSMutableSet* productIds = [[NSMutableSet alloc] init];
    for (ProductDefinition* product in productDefs) {
        [productIds addObject:product.storeSpecificId];
    }
    [UnityPurchasing_getInstance() requestProducts:productIds];
}

void unityPurchasingPurchase(const char* json, const char* developerPayload) {
    NSString* str = [NSString stringWithUTF8String:json];
    ProductDefinition* product = [UnityPurchasing deserializeProductDef:str];
    [UnityPurchasing_getInstance() purchaseProduct:product];
}

void unityPurchasingFinishTransaction(const char* productJSON, const char* transactionId) {
    if (transactionId == NULL)
        return;
    NSString* tranId = [NSString stringWithUTF8String:transactionId];
    [UnityPurchasing_getInstance() finishTransaction:tranId];
}

void unityPurchasingRestoreTransactions() {
    UnityPurchasingLog(@"Restore transactions");
    [UnityPurchasing_getInstance() restorePurchases];
}

void unityPurchasingAddTransactionObserver() {
    UnityPurchasingLog(@"Add transaction observer");
    [UnityPurchasing_getInstance() addTransactionObserver];
}

void unityPurchasingRefreshAppReceipt() {
    UnityPurchasingLog(@"Refresh app receipt");
    [UnityPurchasing_getInstance() refreshReceipt];
}

char* getUnityPurchasingAppReceipt () {
    NSString* receipt = [UnityPurchasing_getInstance() getAppReceipt];
    return UnityPurchasingMakeHeapAllocatedStringCopy(receipt);
}

char* getUnityPurchasingTransactionReceiptForProductId (const char *productId) {
    NSString* receipt = [UnityPurchasing_getInstance() getTransactionReceiptForProductId:[NSString stringWithUTF8String:productId]];
    return UnityPurchasingMakeHeapAllocatedStringCopy(receipt);
}

BOOL getUnityPurchasingCanMakePayments () {
    return [SKPaymentQueue canMakePayments];
}

void setSimulateAskToBuy(BOOL enabled) {
    UnityPurchasingLog(@"Set simulate Ask To Buy %@", enabled ? @"true" : @"false");
    UnityPurchasing_getInstance().simulateAskToBuyEnabled = enabled;
}

BOOL getSimulateAskToBuy() {
    return UnityPurchasing_getInstance().simulateAskToBuyEnabled;
}

void unityPurchasingSetApplicationUsername(const char *username) {
    if (username == NULL)
        return;
    UnityPurchasing_getInstance().applicationUsername = [NSString stringWithUTF8String:username];
}

// Expects json in this format:
// { "products": ["storeSpecificId1", "storeSpecificId2"] }
void unityPurchasingUpdateStorePromotionOrder(const char *json) {
    NSString* str = [NSString stringWithUTF8String:json];
    NSArray* productIds = [UnityPurchasing deserializeProductIdList:str];
    [UnityPurchasing_getInstance() updateStorePromotionOrder:productIds];
}

void unityPurchasingUpdateStorePromotionVisibility(const char *productId, const char *visibility) {
    NSString* prodId = [NSString stringWithUTF8String:productId];
    NSString* visibilityStr = [NSString stringWithUTF8String:visibility];
    [UnityPurchasing_getInstance() updateStorePromotionVisibility:visibilityStr forProduct:prodId];
}

void unityPurchasingInterceptPromotionalPurchases() {
    UnityPurchasingLog(@"Intercept promotional purchases");
    UnityPurchasing_getInstance().interceptPromotionalPurchases = YES;
}

void unityPurchasingContinuePromotionalPurchases() {
    UnityPurchasingLog(@"Continue promotional purchases");
    [UnityPurchasing_getInstance() initiateQueuedEarlyTransactionObserverPayments];
}
