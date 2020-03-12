#import <Foundation/Foundation.h>
#import <AuthenticationServices/AuthenticationServices.h>
#import "UnityAppController.h"

struct UserInfo
{
    const char * userId;
    const char * email;
    const char * displayName;

    const char * idToken;
    const char * error;

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



-(void)startRequest
{
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        ASAuthorizationAppleIDProvider* provider = [[ASAuthorizationAppleIDProvider alloc] init];
        request = [provider createRequest];
        [request setRequestedScopes: @[ASAuthorizationScopeEmail, ASAuthorizationScopeFullName]];

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
            NSString* idToken = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
            NSPersonNameComponents* name = credential.fullName;

            data.idToken = [idToken UTF8String];

            data.displayName = [[NSPersonNameComponentsFormatter localizedStringFromPersonNameComponents:name
                                                                                                   style:NSPersonNameComponentsFormatterStyleDefault
                                                                                                 options:0] UTF8String];
            data.email = [credential.email UTF8String];
            data.userId = [credential.user UTF8String];
            data.userDetectionStatus = credential.realUserStatus;
            data.error = "";
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
        data.error = [error.localizedDescription UTF8String];
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

void SignInWithApple_Signup(SignInWithAppleCallback callback) {
    if (@available(iOS 13.0, tvOS 13.0, *)) {
        SIWAObject* siwa = [SIWAObject instance];
        siwa.signupCallback = callback;
        [siwa startRequest];
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
