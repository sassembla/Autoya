using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
	実験中。
 */
public class NotificationSample : MonoBehaviour
{

#if UNITY_IOS
	public void UnregisterRemoteNotification () {
		// この処理はlocalには関係がないので、うーーん、リモートのみの解除っていうものすごく特殊な奴になるな。
		// この概念のlocalに該当するものは無い。なぜなら登録してないから。なるほど。
		// メニューとかからdisableにした時に関連するねえ。

		// これを実行すると、なにはともあれremoteNotificationが届かなくなる。
		UnityEngine.iOS.NotificationServices.UnregisterForRemoteNotifications();
	}
	
	
	/*
		設定画面とかから、localもremoteも個別に消せたほうがいいんだよね。まあ簡単か。

		API面で無視する -> ignored
			local -> ignore状態
			remote -> ignore状態

		Disableにする -> disabled 
			local -> send,listenなし
			remote -> listenなし, 登録抹消

		みたいな概念で動かす。
	*/


	public void PushAfter10Sec () {
		/*
			local notificationをセットする仕掛け。
			んんーー、、まあ、ゲーム中どこからでも呼べればそれでいいよね。日時指定とかかな。date渡せればそれでよさげ。
		 */
		// create notification.
		SendLocalNotification(DateTime.Now.AddSeconds(10), "通知テストだよ！", "act:" + Guid.NewGuid().ToString());
	}






	/**
		local notificationを送る。
		どんだけ積まれてるかをどうやって制御しようかな。
		とりあえず追加するだけだな。
	 */
	public void SendLocalNotification (DateTime localNotificationDate, string message, string action) {
		var notif = new UnityEngine.iOS.LocalNotification();
		notif.fireDate = localNotificationDate;
		notif.alertBody = message;
		notif.alertAction = action;
		UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(notif);
	}



	/*
		remote notificationに関しては、定期的なインターバルでそれを得る仕掛けを提供すればいいかしら。
		・N秒ごとにチェック
		みたいな。fps考慮するのめんどくさいんだけどどうだろう。

		・この期間中は受け取らない
		・受け取りを再開する
		とかは欲しいところ。
	 */
	public void ReadyReceivingRemoteNotification () {
		// MainThreadDispatcherでのタイミング計測を開始する。
	}

	/**
		特定の期間中、RemoteNofifを受け取らない
	 */
	public void IgnoreRemoteNotification () {
		// MainThreadDispatcherでのremoteのタイミング計測を停止する。
	}



	public void ReadyReceivingLocalNotification () {
		// MainThreadDispatcherでのタイミング計測を開始する。
	}

	/**
		特定の期間中、LocalNotifを受け取らない
	 */
	public void IgnoreLocalNotification () {
		// MainThreadDispatcherでのlocalのタイミング計測を停止する。
	}






	// Use this for initialization
	void Start () {
		try {
			// register.
			UnityEngine.iOS.NotificationServices.RegisterForNotifications(UnityEngine.iOS.NotificationType.Alert | UnityEngine.iOS.NotificationType.Badge | UnityEngine.iOS.NotificationType.Sound, true);
			
			var token = UnityEngine.iOS.NotificationServices.deviceToken;
			Debug.LogError("token:" + token);// たぶんremote = trueでないと取得できない気がする。

			
		} catch (Exception e) {
			Debug.LogError("e:" + e);
		}
	}
	
	int frame = 0;

	// Update is called once per frame
	void Update () {
		// 適当なインターバルで云々。
		if (frame % 100 == 0) {
			var count = UnityEngine.iOS.NotificationServices.localNotificationCount;
			Debug.LogError("count:" + count);
			if (0 < count) {
				var localNotifs = new List<UnityEngine.iOS.LocalNotification>();

				for (var i = 0; i < count; i++) {
					var t = UnityEngine.iOS.NotificationServices.GetLocalNotification(i);
					localNotifs.Add(t);
				}

				foreach (var localNotif in localNotifs) {
					if (localNotif.hasAction) {
						var act = localNotif.alertAction;
						Debug.LogError("has action. act:" + act);
					}

					// consume anyway.
					UnityEngine.iOS.NotificationServices.CancelLocalNotification(localNotif);
				}
			}
		}
		frame++;
	}
#endif
}
