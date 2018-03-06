#pragma once
#import <StoreKit/StoreKit.h>
#import "LifeCycleListener.h"

@protocol UnityEarlyTransactionObserverDelegate <NSObject>

- (void)promotionalPurchaseAttempted:(SKPayment *)payment;

@end

@interface UnityEarlyTransactionObserver : NSObject<SKPaymentTransactionObserver, LifeCycleListener> {
    NSMutableSet *m_QueuedPayments;
}

@property BOOL readyToReceiveTransactionUpdates;

// The delegate exists so that the observer can notify it of attempted promotional purchases. 
@property(nonatomic, weak) id<UnityEarlyTransactionObserverDelegate> delegate;

+ (UnityEarlyTransactionObserver*)defaultObserver;

- (void)initiateQueuedPayments;

@end
