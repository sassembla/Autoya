#pragma once
#import <StoreKit/StoreKit.h>
#import "LifeCycleListener.h"

@interface UnityEarlyTransactionObserver : NSObject<SKPaymentTransactionObserver, LifeCycleListener> {
    NSMutableSet *m_QueuedPayments;
}

@property BOOL readyToReceiveTransactionUpdates;

+ (UnityEarlyTransactionObserver*)defaultObserver;

- (void)initiateQueuedPayments;

@end
