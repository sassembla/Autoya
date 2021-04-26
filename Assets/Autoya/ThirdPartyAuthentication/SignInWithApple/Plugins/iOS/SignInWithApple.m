#import <Foundation/Foundation.h>
#import <AuthenticationServices/AuthenticationServices.h>
#import "UnityAppController.h"

struct UserInfo
{
    const char * userId;
    const char * email;
    const char * displayName;
    
    const char * authorizationCode;
    const char * idToken;
    long errorCode;
    const char * reason;

    ASUserDetectionStatus userDetectionStatus;
};

typedef void (*IsSIWAEnabledCallback)(int result);

typedef void (*SignInWithAppleCallback)(int result, struct UserInfo s1);

API_AVAILABLE(ios(13.0), tvos(13.0))
typedef void (*CredentialStateCallback)(ASAuthorizationAppleIDProviderCredentialState state);

API_AVAILABLE(ios(13.0), tvos(13.0))
@interface SIWAObject : NSObject<ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>

@property (nonatomic) SignInWithAppleCallback signupCallback;
@property (nonatomic) CredentialStateCallback credentialStateCallback;

@end

API_AVAILABLE(ios(13.0), tvos(13.0))
static SIWAObject* _sIWAObj;

@implementation SIWAObject
{
    ASAuthorizationAppleIDRequest* request;
}

+(SIWAObject*)instance
{
    if (_sIWAObj == nil) {
        _sIWAObj = [[SIWAObject alloc] init];
    }
    return _sIWAObj;
}



-(void)startRequest:(NSString *)nonce scope:(int) scope
{
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        ASAuthorizationAppleIDProvider* provider = [[ASAuthorizationAppleIDProvider alloc] init];
        request = [provider createRequest];
        
        switch (scope) {
            case 0:
                [request setRequestedScopes: @[ASAuthorizationScopeEmail]];
                break;
            case 1:
                [request setRequestedScopes: @[ASAuthorizationScopeFullName]];
                break;
            case 2:
                [request setRequestedScopes: @[ASAuthorizationScopeEmail, ASAuthorizationScopeFullName]];
                break;
            default:
                [NSException raise:@"unsupported scope" format:@"scope:%d is not supported.", scope];
                break;
        }
        
        // set nonce for verification.
        [request setNonce:nonce];

        ASAuthorizationController* controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        controller.delegate = self;
        controller.presentationContextProvider = self;
        [controller performRequests];
    } else {
        // Fallback on earlier versions
    }
}

- (void)getCredentialState:(NSString *)userID
{
    ASAuthorizationAppleIDProvider* provider = [[ASAuthorizationAppleIDProvider alloc] init];
    [provider getCredentialStateForUserID:userID
                               completion:^(ASAuthorizationAppleIDProviderCredentialState credentialState, NSError * _Nullable error) {
        self.credentialStateCallback(credentialState);
    }];
}



// delegates.

// delegate for presentation anchor.
-(ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller
{
    return _UnityAppController.window;
}

// delegate for authorizationController completed.
-(void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization
{
    if (self.signupCallback)
    {
        struct UserInfo data;

        if (@available(iOS 13.0, tvOS 13.0, *)) {
            
            ASAuthorizationAppleIDCredential* credential = (ASAuthorizationAppleIDCredential*)authorization.credential;
            
            // get authCode
            NSString* authorizationCode = [[NSString alloc]
                                     initWithData:credential.authorizationCode encoding:NSUTF8StringEncoding];
            
            // get idToken
            NSString* idToken = [[NSString alloc]
                                 initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
            
            NSPersonNameComponents* name = credential.fullName;

            // input data.
            data.authorizationCode = [authorizationCode UTF8String];
            data.idToken = [idToken UTF8String];
            data.displayName = [[NSPersonNameComponentsFormatter localizedStringFromPersonNameComponents:name
                                                                                                   style:NSPersonNameComponentsFormatterStyleDefault
                                                                                                 options:0]
                                UTF8String];
            
            data.email = [credential.email UTF8String];
            data.userId = [credential.user UTF8String];
            data.userDetectionStatus = credential.realUserStatus;
            data.errorCode = 0;
            data.reason = "";
            self.signupCallback(1, data);
        } else {
            // Fallback on earlier versions
        }
    }
}

// delegate for authorizationController completed with error.
-(void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error
{
    if (self.signupCallback)
    {
        // All members need to be set to a non-null value.
        struct UserInfo data;
        data.idToken = "";
        data.displayName = "";
        data.email = "";
        data.userId = "";
        data.userDetectionStatus = ASUserDetectionStatusUnknown;
        data.errorCode = error.code;
        data.reason = [error.localizedDescription UTF8String];
        
        self.signupCallback(0, data);
    }
}

@end
// SIWAObject



// externed functions.

void SignInWithApple_CheckIsSIWAEnabled(IsSIWAEnabledCallback callback) {
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        callback(1);
    } else {
        callback(0);
    }
}

void SignInWithApple_Signup(const char *nonce, int scope, SignInWithAppleCallback callback) {
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        SIWAObject* siwa = [SIWAObject instance];
        siwa.signupCallback = callback;
        
        [siwa startRequest:[NSString stringWithUTF8String: nonce] scope:scope];
    } else {
        // do nothing.
    }
}

API_AVAILABLE(ios(13.0), tvos(13.0))
void SignInWithApple_GetCredentialState(const char *userID, CredentialStateCallback callback) {
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        SIWAObject* siwa = [SIWAObject instance];
        siwa.credentialStateCallback = callback;
        [siwa getCredentialState: [NSString stringWithUTF8String: userID]];
    } else {
        // do nothing.
    }
}
