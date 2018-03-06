#import "UnityEarlyTransactionObserver.h"
#import "UnityPurchasing.h"

void Log(NSString *message) {
    NSLog(@"UnityIAP UnityEarlyTransactionObserver: %@\n", message);
}

@implementation UnityEarlyTransactionObserver

static UnityEarlyTransactionObserver *s_Observer = nil;

+(void)load {
    if (!s_Observer) {
        s_Observer = [[UnityEarlyTransactionObserver alloc] init];
        Log(@"Created");
        
        [s_Observer registerLifeCycleListener];
    }
}

+ (UnityEarlyTransactionObserver*)defaultObserver {
    return s_Observer;
}

- (void)registerLifeCycleListener {
    UnityRegisterLifeCycleListener(self);
    Log(@"Registered for lifecycle events");
}

- (void)didFinishLaunching:(NSNotification*)notification {
    Log(@"Added to the payment queue");
    [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
}

- (void)setDelegate:(id<UnityEarlyTransactionObserverDelegate>)delegate {
    _delegate = delegate;
    [self sendQueuedPaymentsToInterceptor];
}

- (BOOL)paymentQueue:(SKPaymentQueue *)queue shouldAddStorePayment:(SKPayment *)payment forProduct:(SKProduct *)product {
    Log(@"Payment queue shouldAddStorePayment");
    if (self.readyToReceiveTransactionUpdates && !self.delegate) {
        return YES;
    } else {
        if (m_QueuedPayments == nil) {
            m_QueuedPayments = [[NSMutableSet alloc] init];
        }
        // If there is a delegate and we have not seen this payment yet, it means we should intercept promotional purchases
        // and just return the payment to the delegate.
        // Do not try to process it now.
        if (self.delegate && [m_QueuedPayments member:payment] == nil) {
            [self.delegate promotionalPurchaseAttempted:payment];
        }
        [m_QueuedPayments addObject:payment];
        return NO;
    }
    return YES;
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray<SKPaymentTransaction *> *)transactions {}

- (void)initiateQueuedPayments {
    Log(@"Request to initiate queued payments");
    if (m_QueuedPayments != nil) {
        Log(@"Initiating queued payments");
        for (SKPayment *payment in m_QueuedPayments) {
            [[SKPaymentQueue defaultQueue] addPayment:payment];
        }
        [m_QueuedPayments removeAllObjects];
    }
}

- (void)sendQueuedPaymentsToInterceptor {
    Log(@"Request to send queued payments to interceptor");
    if (m_QueuedPayments != nil) {
        Log(@"Sending queued payments to interceptor");
        for (SKPayment *payment in m_QueuedPayments) {
            if (self.delegate) {
                [self.delegate promotionalPurchaseAttempted:payment];
            }
        }
    }
}

@end
