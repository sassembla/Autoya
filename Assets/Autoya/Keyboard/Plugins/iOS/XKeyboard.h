#pragma once

typedef struct
{
    const char* text;
    const char* placeholder;

    UIKeyboardType              keyboardType;
    UITextAutocorrectionType    autocorrectionType;
    UIKeyboardAppearance        appearance;

    BOOL multiline;
    BOOL secure;
}
KeyboardShowParam;


@interface XKeyboardDelegate : NSObject<UITextViewDelegate>
{
}

- (void)textInputDone:(id)sender;
- (void)textInputLostFocus;

+ (XKeyboardDelegate*)Instance;

- (id)init;
- (void)show;
- (void)hide;
- (void)setText:(NSString*)newText;

@property (readonly, nonatomic)                                 BOOL            willHide;
@property (readonly, nonatomic)                                 BOOL            active;
@property (readonly, nonatomic)                                 KeyboardStatus  status;
@property (retain, nonatomic, getter = getText, setter = setText:)  NSString*       text;

@end
