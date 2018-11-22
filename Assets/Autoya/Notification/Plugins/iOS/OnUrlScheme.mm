#import "AppDelegateListener.h"


@interface OnUrlScheme : NSObject <AppDelegateListener>
@end


@implementation OnUrlScheme

static OnUrlScheme *_instance = nil;

+ (void)load {
    if(!_instance) {
        _instance = [[OnUrlScheme alloc] init];
    }
}

- (id)init {
    self = [super init];
    if(!self)
        return nil;

    _instance = self;

    // register to unity
    UnityRegisterAppDelegateListener(self);

    return self;
}


- (void)onOpenURL:(NSNotification*)notification {
    NSDictionary* dict = [notification userInfo];
//    NSLog(@"dict:%@", dict);
    NSString* urlString = [[dict valueForKey:@"url"]absoluteString];
    NSString* completeString = [NSString stringWithFormat:@"%@%@", @"URLScheme:",
                          urlString];
    /*
     dict:{
         annotation = "";
         sourceApplication = "com.apple.mobilesafari";
         url = "sctest://heheh%3Fherecomes%3Ddaredevil";
     }
     */
     
//    [dict setValue:urlString forKey:@"url"];
//    NSError* error;
//    NSData* data = [NSJSONSerialization dataWithJSONObject:dict options:NSJSONWritingPrettyPrinted error:&error];
//
//    if (!data) {
//        NSLog(@"Got an error: %@", error);
//        return;
//    }
//    NSString* jsonString = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    
    // ここでファイルを作成する。
    NSArray* paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString* documentsDirectory = [paths objectAtIndex:0];
    NSString* filePath = [documentsDirectory stringByAppendingPathComponent:@"URLSchemeFile"];
    NSError* error;
    [completeString writeToFile:filePath
                     atomically:NO
                       encoding:NSUTF8StringEncoding
                          error:&error];
    
    if (error) {
        NSLog(@"error:%@", error);
    }
    
    UnitySendMessage("AutoyaMainthreadDispatcher", "OnNativeEvent", [completeString UTF8String]);
}

@end
