#include "XKeyboard.h"
#include "DisplayManager.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>

// このクラス自体のシングルトン
static XKeyboardDelegate*    _keyboard = nil;

@implementation XKeyboardDelegate {
    
    UITextView*         textView;
    UIView*             baseView;
    UIView*             paddingView;
    UIButton*           sendButton;
    
    NSString*           initialText;
    UIKeyboardType      keyboardType;

    BOOL                _willHide;
    BOOL                _active;
    KeyboardStatus      _status;
    
    CGFloat textViewOriginalHeight;
    CGFloat baseViewOriginalY;
    CGFloat baseViewOriginalHeight;
}

// プロパティの定義
@synthesize willHide    = _willHide;
@synthesize active      = _active;
@synthesize status      = _status;
@synthesize text;


- (id)init {
    // NSLog(@"init");
    NSAssert(_keyboard == nil, @"You can have only one instance of XKeyboardDelegate");
    self = [super init];
    
    
    if (self) {
        int launchCount = 0;
        NSLog(@"この辺で名前を自動的に拾ってきたい。名前を変えれば拾えるようにしたい。jsonとかに吐いておく？");
        baseView = [[[NSBundle mainBundle] loadNibNamed:@"DefaultKeyboardView" owner:self options:nil] objectAtIndex:launchCount];
        
        // 型指定でTextViewとButtonをと取得する。これはxibに依存するので、そのままでいい。
        for (UIView *i in baseView.subviews){
            if([i isKindOfClass:[UITextView class]]){
                textView = (UITextView *)i ;
                continue;
            }
            
            if([i isKindOfClass:[UIButton class]]){
                sendButton = (UIButton *)i;
//                ボタンアクションのセット
                [sendButton addTarget:self action:@selector(textInputDone:) forControlEvents:UIControlEventTouchUpInside];
                
//                このボタンの背景はグラデカラーだけがあればいいので、下地の背景カラーを透明にする。
                sendButton.backgroundColor = UIColor.clearColor;
                continue;
            }
        }
        
        paddingView = [[UIView alloc] initWithFrame:CGRectMake(0, 0, baseView.frame.size.width, baseView.frame.size.height)];
        [paddingView setUserInteractionEnabled:NO];
        
        // textViewにbaseView相当のパディングをセットする。
        textView.inputAccessoryView = paddingView;
        
        textView.delegate = self;
        textView.returnKeyType = UIReturnKeySend;
        
        
        // 通知系のセット
        
        // キーボードが画面内に入りそうなタイミングの処理
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardWillAppear:) name:UIKeyboardWillChangeFrameNotification object:nil];
        
        // キーボードが画面に表示されきってから発生する処理
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboadDidAppear:) name:UIKeyboardDidChangeFrameNotification object:nil];
        
        // キーボードが画面外に消えるタイミングの処理
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(willHide:) name:UIKeyboardWillHideNotification object:nil];
        
        // キーボードが画面外に消えた後の処理
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(didHide:) name:UIKeyboardDidHideNotification object:nil];
        
        textViewOriginalHeight = textView.frame.size.height;
        baseViewOriginalY = baseView.frame.origin.y;
        baseViewOriginalHeight = baseView.frame.size.height;
    }
    
    return self;
}

// 入力完了後に呼ばれるメソッド。
- (void)textInputDone:(id)sender {
    if (_status == Visible)
        _status = Done;
    [self hide];
}

// Unityからhideを行うメソッド
- (void)textInputLostFocus {
    // NSLog(@"textInputLostFocus");
    if (_status == Visible)
        _status = LostFocus;
    [self hide];
}



/*
    notification系
 */
// キーボードがどこに出るかを表示する。微細な位置変更も、その完了前に着火する。
- (void)keyboardWillAppear:(NSNotification*)notification {
    if (!_keyboard) {
        return;
    }
    
    CGRect srcRect  = [[notification.userInfo objectForKey: UIKeyboardFrameEndUserInfoKey] CGRectValue];
    CGRect rect     = [UnityGetGLView() convertRect: srcRect fromView: nil];
    CGRect target = CGRectMake(rect.origin.x, rect.origin.y, rect.size.width, baseViewOriginalHeight);
//    NSLog(@"rect:%@", NSStringFromCGRect(rect));
    
    baseView.frame = target;
    baseViewOriginalY = baseView.frame.origin.y;

    [self textViewDidChange:nil];
}

- (void)keyboadDidAppear:(NSNotification*)notification {
    //    ボタンのグラデーションレイヤーのサイズ調整(ここでしかできない)
    for (CALayer *layer in sendButton.layer.sublayers){
        if ([layer isKindOfClass:CAGradientLayer.class]) {
            CAGradientLayer* gLayer = (CAGradientLayer*)layer;
            gLayer.frame = CGRectMake(sendButton.bounds.origin.x, sendButton.bounds.origin.y, sendButton.bounds.size.width, sendButton.bounds.size.height);
        }
    }
}

- (void)willHide:(NSNotification*)notif {
    _willHide = YES;
}

- (void)didHide:(NSNotification*)notif {
    // ^マークでの予測変換時にもキーボードの消失が発生する。statusで区別し、動作を変更することで対処できる。
    if (_status != Visible) {
        [baseView removeFromSuperview];
        [self setText:@""];
        _active     = NO;
    }
}



/*
    delegateによるオーバーライド系
 */
- (BOOL)textViewShouldBeginEditing:(UITextView*)view {
    _willHide = NO;
    return YES;
}

// Unityからのtext取得
- (NSString*)getText {
    if (_status == Canceled)
        return initialText;
    else
    {
        return [textView text];
    }
}

- (void)setText:(NSString*)newText {
    textView.text = newText;
    [self textViewDidChange:nil];
}

- (void)textViewDidChange:(UITextView*)_textView {
    //    入力済みの文字によってTextViewの高さを更新する
    CGSize size = CGSizeMake(textView.frame.size.width, CGFLOAT_MAX);
    CGSize preffered =[textView sizeThatFits:size];
    int height = preffered.height;
    CGFloat diff = height - textViewOriginalHeight;
    
//    マイナスになるケースがあるのでそれらは無視する。
    if (diff < 0) {
        diff = 0;
    }
    
    baseView.frame = CGRectMake(
                                baseView.frame.origin.x,
                                baseViewOriginalY - diff,
                                baseView.frame.size.width,
                                baseViewOriginalHeight + diff
                                );
    
//     textView自体のサイズに対してそのままセットすると、なんと行が見切れる。お前、、というわけで、heightの2倍をセットして見切れを消す。よくないが。
    textView.frame = CGRectMake(
                                textView.frame.origin.x,
                                textView.frame.origin.y,
                                textView.frame.size.width,
                                height * 2
                                );
}

- (BOOL)textView:(UITextView *)textView shouldChangeTextInRange:(NSRange)range replacementText:(NSString *)text {
    // 改行コード入力でのキーボード終了
    if ([text isEqualToString:@"\n"]) {
        [self textInputDone:nil];
        return YES;
    }
    
    return YES;
}







/*
     Unityから叩くブリッジ
 */
+ (XKeyboardDelegate*)Instance {
//    NSLog(@"Instance");
    if (!_keyboard)
        _keyboard = [[XKeyboardDelegate alloc] init];
    
    return _keyboard;
}



// 開く
- (void)show {
    
    // 初期化
    _status     = Visible;
    _active     = YES;
    
    // 位置の初期化、画面外下にもっていき、アニメーションに耐えるようにする。
    baseView.frame = CGRectMake(0, UnityGetGLView().frame.size.height, baseView.frame.size.width, baseView.frame.size.height);
    
    // テキストビューを画面に載せ替え、フォーカスを渡すことでキーボードが出る。これ以外の方法ではキーボードが出現しない。
    [UnityGetGLView() addSubview: baseView];
    [textView becomeFirstResponder];
}

// 閉じる
- (void)hide {
    [textView resignFirstResponder];
}

- (CGRect) getFrame {
    return baseView.frame;
}

@end









//==============================================================================
//
//  Unity Interface:


extern "C" void XKeyboard_Show() {
    [[XKeyboardDelegate Instance] show];
}

extern "C" void XKeyboard_Hide() {
    if (!_keyboard)
        return;

    [[XKeyboardDelegate Instance] textInputLostFocus];
}

extern "C" void XKeyboard_SetText(const char* text) {
    // NSLog(@"XKeyboard_SetText:%s", text);
    if (!text) {
        return;
    }
    
    if (strlen(text) == 0) {
        return;
    }
    
    [XKeyboardDelegate Instance].text = [NSString stringWithUTF8String: text];
}

extern "C" char* XKeyboard_GetText() {
    // NSLog(@"XKeyboard_GetText");
    const char *str = [[XKeyboardDelegate Instance].text UTF8String];
    char* result = (char*)malloc(strlen(str)+1);
    strcpy(result, str);
    return result;
}

extern "C" int XKeyboard_IsActive() {
    // NSLog(@"_keyboard.active:%d", _keyboard.active);
    return (_keyboard && _keyboard.active) ? 1 : 0;
}

extern "C" int XKeyboard_IsDone() {
    // NSLog(@"XKeyboard_IsDone, _keyboard.status:%d", _keyboard.status);
    return (_keyboard && _keyboard.status != Visible) ? 1 : 0;
}

extern "C" void XKeyboard_GetRect(float* x, float* y, float* w, float* h) {
   
    CGRect area =  _keyboard ? [[XKeyboardDelegate Instance] getFrame] : CGRectMake(0, 0, 0, 0);

    // convert to unity coord system

    float   multX   = (float)GetMainDisplaySurface()->targetW / UnityGetGLView().bounds.size.width;
    float   multY   = (float)GetMainDisplaySurface()->targetH / UnityGetGLView().bounds.size.height;

    *x = 0;
    *y = (UnityGetGLView().bounds.size.height - area.origin.y) * multY;
    if (_keyboard.willHide) {
        *y = 0;
    }
    *w = area.size.width * multX;
    *h = area.size.height * multY;
}