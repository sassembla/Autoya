#if UNITY_5_6_OR_NEWER && !UNITY_5_6_0
using System;
using UnityEditor;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AppStoreModel;

namespace AppStoresSupport
{
    [CustomEditor(typeof(AppStoreSettings))]
	public class AppStoreSettingsEditor : Editor
    {
		private UnityClientInfo unityClientInfo;
		private XiaomiSettings xiaomi;

		private string clientSecret_in_memory;
		private string callbackUrl_in_memory;
		private string appSecret_in_memory;
		private bool appSecret_hidden = true;
		private bool ownerAuthed = false;

		private string callbackUrl_last;
		private string appId_last;
		private string appKey_last;
		private string appSecret_last;

		private const string STEP_GET_CLIENT = "get_client";
		private const string STEP_UPDATE_CLIENT = "update_client";
		private const string STEP_UPDATE_CLIENT_SECRET = "update_client_secret";

		struct ReqStruct {
			public string currentStep;
			public string targetStep;
			public UnityWebRequest request;
			public GeneralResponse resp;
		}

		private Queue<ReqStruct> requestQueue = new Queue<ReqStruct>();

        private class AppStoreStyles
        {
            public const string kNoUnityProjectIDErrorMessage = "Unity Project ID doesn't exist, please go to Window/Services to create one.";

            public const int kUnityProjectIDBoxHeight = 24;
            public const int kUnityClientBoxHeight = 110;
            public const int kXiaomiBoxHeight = 90;

            public const int kUnityClientIDButtonWidth = 160;
            public const int kSaveButtonWidth = 120;

			public const int kClientLabelWidth = 140;
			public const int kClientLabelHeight = 16;
			public const int kClientLabelWidthShort = 50;
			public const int kClientLabelHeightShort = 15;
        }

        private SerializedProperty unityClientID;
        private SerializedProperty unityClientKey;
        private SerializedProperty unityClientRSAPublicKey;

        private SerializedProperty xiaomiAppID;
        private SerializedProperty xiaomiAppKey;
        private SerializedProperty xiaomiIsTestMode;

        private bool isOperationRunning = false;

        void OnEnable()
        {
            // For unity client settings.
            unityClientID = serializedObject.FindProperty("UnityClientID");
            unityClientKey = serializedObject.FindProperty("UnityClientKey");
            unityClientRSAPublicKey = serializedObject.FindProperty("UnityClientRSAPublicKey");

            // For Xiaomi settings.
            SerializedProperty xiaomiAppStoreSetting = serializedObject.FindProperty("XiaomiAppStoreSetting");
            xiaomiAppID = xiaomiAppStoreSetting.FindPropertyRelative("AppID");
            xiaomiAppKey = xiaomiAppStoreSetting.FindPropertyRelative("AppKey");
            xiaomiIsTestMode = xiaomiAppStoreSetting.FindPropertyRelative("IsTestMode");

            EditorApplication.update += CheckUpdate;
			InitializeSecrets();
        }

        public override void OnInspectorGUI()
        {            
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(isOperationRunning);

            // Unity project id.
            EditorGUILayout.LabelField(new GUIContent("Unity Project ID"));
            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kUnityProjectIDBoxHeight)))
            {
                GUILayout.FlexibleSpace();

                string unityProjectID = Application.cloudProjectId;
                if (String.IsNullOrEmpty(unityProjectID))
                {
                    EditorGUILayout.LabelField(new GUIContent(AppStoreStyles.kNoUnityProjectIDErrorMessage));
                    GUILayout.FlexibleSpace();
                    return;
                }
                EditorGUILayout.LabelField(new GUIContent(Application.cloudProjectId));                
                GUILayout.FlexibleSpace();
            }

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            // Unity client settings.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Unity Client Settings"));
            bool clientNotExists = String.IsNullOrEmpty(unityClientID.stringValue);
			string buttonLableString = "Generate Unity Client";
			string target = STEP_GET_CLIENT;
			if (!clientNotExists) {
				if (String.IsNullOrEmpty (clientSecret_in_memory)  || !AppStoreOnboardApi.loaded) {
					buttonLableString = "Load Unity Client";
				} else {
					buttonLableString = "Update Client Secret";
					target = STEP_UPDATE_CLIENT_SECRET;
				}
			}
            if (GUILayout.Button(buttonLableString, GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
            {
				isOperationRunning = true;
				Debug.Log (buttonLableString + "...");
				if (target == STEP_UPDATE_CLIENT_SECRET) {
					clientSecret_in_memory = null;
				}
				callApiAsync (target);
               	
                serializedObject.ApplyModifiedProperties();
				this.Repaint ();
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kUnityClientBoxHeight)))
            {
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				if (String.IsNullOrEmpty (unityClientID.stringValue)) {
					EditorGUILayout.LabelField ("Client ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField ("None");
				} else {
					EditorGUILayout.LabelField ("Client ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField (strPrefix(unityClientID.stringValue), GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
					if (GUILayout.Button ("Copy to Clipboard", GUILayout.Height(AppStoreStyles.kClientLabelHeight))) {
						TextEditor te = new TextEditor ();
						te.text = unityClientID.stringValue;
						te.SelectAll ();
						te.Copy ();
					}
				}
				EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				if (String.IsNullOrEmpty (unityClientKey.stringValue)) {
					EditorGUILayout.LabelField ("Client Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField ("None");
				} else {
					EditorGUILayout.LabelField ("Client Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField (strPrefix(unityClientKey.stringValue), GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
					if (GUILayout.Button ("Copy to Clipboard", GUILayout.Height(AppStoreStyles.kClientLabelHeight))) {
						TextEditor te = new TextEditor ();
						te.text = unityClientKey.stringValue;
						te.SelectAll ();
						te.Copy ();
					}
				}
				EditorGUILayout.EndHorizontal();
                
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				if (String.IsNullOrEmpty (unityClientRSAPublicKey.stringValue)) {
					EditorGUILayout.LabelField ("Client RSA Public Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField ("None");
				} else {
					EditorGUILayout.LabelField ("Client RSA Public Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField (strPrefix(unityClientRSAPublicKey.stringValue), GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
					if (GUILayout.Button ("Copy to Clipboard", GUILayout.Height(AppStoreStyles.kClientLabelHeight))) {
						TextEditor te = new TextEditor ();
						te.text = unityClientRSAPublicKey.stringValue;
						te.SelectAll ();
						te.Copy ();
					}
				}
				EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				if (String.IsNullOrEmpty (clientSecret_in_memory)) {
					EditorGUILayout.LabelField ("Client Secret", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField ("None");
				} else {
					EditorGUILayout.LabelField ("Client Secret", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
					EditorGUILayout.LabelField (strPrefix(clientSecret_in_memory), GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
					if (GUILayout.Button ("Copy to Clipboard", GUILayout.Height(AppStoreStyles.kClientLabelHeight))) {
						TextEditor te = new TextEditor ();
						te.text = clientSecret_in_memory;
						te.SelectAll ();
						te.Copy ();
					}
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Callback URL", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
				callbackUrl_in_memory = EditorGUILayout.TextField (String.IsNullOrEmpty(callbackUrl_in_memory)? "" : callbackUrl_in_memory);
				GUILayout.EndHorizontal();
				GUILayout.FlexibleSpace();
            }

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            // Xiaomi application settings.
            EditorGUILayout.LabelField(new GUIContent("Xiaomi App Settings"));

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kXiaomiBoxHeight)))
            {
                GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.Label("App ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
				EditorGUILayout.PropertyField (xiaomiAppID, GUIContent.none);
				GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.Label("App Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
				EditorGUILayout.PropertyField (xiaomiAppKey, GUIContent.none);
				GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.Label("App Secret", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
				if (appSecret_hidden) {
					GUILayout.Label ("App Secret is Hidden");
				} else {
					appSecret_in_memory = EditorGUILayout.TextField (String.IsNullOrEmpty (appSecret_in_memory) ? "" : appSecret_in_memory);
				}
				string hiddenButtonLabel = appSecret_hidden ? "Show" : "Hide";
				if (GUILayout.Button (hiddenButtonLabel, GUILayout.Width(AppStoreStyles.kClientLabelWidthShort), GUILayout.Height(AppStoreStyles.kClientLabelHeightShort))) {
					appSecret_hidden = !appSecret_hidden;
				}
				GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Test Mode", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
				EditorGUILayout.PropertyField (xiaomiIsTestMode, GUIContent.none);
				GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            // Save the settings.
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save All Settings", GUILayout.Width(AppStoreStyles.kSaveButtonWidth)))
            {
				isOperationRunning = true;
				Debug.Log ("Saving...");
				if (clientNotExists) {
					Debug.LogError ("Please get/generate Unity Client first.");
				} else {
					if (callbackUrl_last != callbackUrl_in_memory ||
					    appId_last != xiaomiAppID.stringValue ||
					    appKey_last != xiaomiAppKey.stringValue ||
					    appSecret_last != appSecret_in_memory) {
						callApiAsync (STEP_UPDATE_CLIENT);
					} else {
						isOperationRunning = false;
						Debug.Log ("Unity Client Refreshed. Finished: " + STEP_UPDATE_CLIENT);
					}
				}

                serializedObject.ApplyModifiedProperties();
				this.Repaint ();
                AssetDatabase.SaveAssets();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
			this.Repaint ();

            EditorGUI.EndDisabledGroup();
        }

		string strPrefix(string str) {
			var preIndex = str.Length < 5 ? str.Length : 5;
			return str.Substring (0, preIndex) + "...";
		}

		void callApiAsync(String targetStep) {
			if (targetStep == STEP_GET_CLIENT) {
				AppStoreOnboardApi.tokenInfo.access_token = null;
			}
			if (AppStoreOnboardApi.tokenInfo.access_token == null) {
				UnityOAuth.GetAuthorizationCodeAsync (AppStoreOnboardApi.oauthClientId, (response) => {
					if (response.AuthCode != null) {
						string authcode = response.AuthCode;
						UnityWebRequest request = AppStoreOnboardApi.GetAccessToken (authcode);
						TokenInfo tokenInfoResp = new TokenInfo ();
						ReqStruct reqStruct = new ReqStruct ();
						reqStruct.request = request;
						reqStruct.resp = tokenInfoResp;
						reqStruct.targetStep = targetStep;
						requestQueue.Enqueue (reqStruct);
					} else {
						Debug.Log ("Failed: " + response.Exception.ToString ());
						isOperationRunning = false;
					}
				});
			} else {
				UnityWebRequest request = AppStoreOnboardApi.GetUserId ();
				UserIdResponse userIdResp = new UserIdResponse ();
				ReqStruct reqStruct = new ReqStruct ();
				reqStruct.request = request;
				reqStruct.resp = userIdResp;
				reqStruct.targetStep = targetStep;
				requestQueue.Enqueue (reqStruct);
			}
		}

		void OnDestroy()
		{
            EditorApplication.update -= CheckUpdate;
		}

        void CheckUpdate()
        {
            CheckRequestUpdate();
        }

        void InitializeSecrets()
        {
			// No need to initialize for invalid client settings.
			if (String.IsNullOrEmpty(unityClientID.stringValue))
			{
				return;
			}

			if (!String.IsNullOrEmpty(clientSecret_in_memory))
			{
				return;
			}

            // Start secret initialization. 
			isOperationRunning = true;
			Debug.Log ("Loading existed client info...");
			callApiAsync (STEP_GET_CLIENT);
        }

		void CheckRequestUpdate()
        {
		    if (requestQueue.Count <= 0)
		    {
		        return;
		    }

		    ReqStruct reqStruct = requestQueue.Dequeue ();
			UnityWebRequest request = reqStruct.request;
			GeneralResponse resp = reqStruct.resp;

			if (request != null && request.isDone) {
				if (request.error != null) {
					if (request.responseCode == 404) {
						Debug.LogError ("Resouce not found.");
						isOperationRunning = false;
					} else if (request.responseCode == 403) {
						Debug.LogError ("Permision denied.");
						isOperationRunning = false;
					} else {
						Debug.LogError (request.error);
						isOperationRunning = false;
					}
				} else {
					if (request.downloadHandler.text.Contains(AppStoreOnboardApi.tokenExpiredInfo)) {
						UnityWebRequest newRequest = AppStoreOnboardApi.RefreshToken();
						TokenInfo tokenInfoResp = new TokenInfo();
						ReqStruct newReqStruct = new ReqStruct();
						newReqStruct.request = newRequest;
						newReqStruct.resp = tokenInfoResp;
						newReqStruct.targetStep = reqStruct.targetStep;
						requestQueue.Enqueue(newReqStruct);
					} else {
						if (resp.GetType () == typeof(TokenInfo)) {
							resp = JsonUtility.FromJson<TokenInfo> (request.downloadHandler.text);
							AppStoreOnboardApi.tokenInfo.access_token = ((TokenInfo)resp).access_token;
							if (AppStoreOnboardApi.tokenInfo.refresh_token == null || AppStoreOnboardApi.tokenInfo.refresh_token == "") {
								AppStoreOnboardApi.tokenInfo.refresh_token = ((TokenInfo)resp).refresh_token;
							}
							UnityWebRequest newRequest = AppStoreOnboardApi.GetUserId ();
							UserIdResponse userIdResp = new UserIdResponse ();
							ReqStruct newReqStruct = new ReqStruct ();
							newReqStruct.request = newRequest;
							newReqStruct.resp = userIdResp;
							newReqStruct.targetStep = reqStruct.targetStep;
							requestQueue.Enqueue (newReqStruct);
						} else if (resp.GetType () == typeof(UserIdResponse)) {
							resp = JsonUtility.FromJson<UserIdResponse> (request.downloadHandler.text);
							AppStoreOnboardApi.userId = ((UserIdResponse)resp).sub;
							UnityWebRequest newRequest = AppStoreOnboardApi.GetOrgId (Application.cloudProjectId);
							OrgIdResponse orgIdResp = new OrgIdResponse ();
							ReqStruct newReqStruct = new ReqStruct ();
							newReqStruct.request = newRequest;
							newReqStruct.resp = orgIdResp;
							newReqStruct.targetStep = reqStruct.targetStep;
							requestQueue.Enqueue (newReqStruct);
						} else if (resp.GetType () == typeof(OrgIdResponse)) {
							resp = JsonUtility.FromJson<OrgIdResponse> (request.downloadHandler.text);
							AppStoreOnboardApi.orgId = ((OrgIdResponse)resp).org_foreign_key;
							UnityWebRequest newRequest = AppStoreOnboardApi.GetOrgRoles ();
							OrgRoleResponse orgRoleResp = new OrgRoleResponse ();
							ReqStruct newReqStruct = new ReqStruct ();
							newReqStruct.request = newRequest;
							newReqStruct.resp = orgRoleResp;
							newReqStruct.targetStep = reqStruct.targetStep;
							requestQueue.Enqueue (newReqStruct);
						} else if (resp.GetType () == typeof(OrgRoleResponse)) {
							resp = JsonUtility.FromJson<OrgRoleResponse> (request.downloadHandler.text);
							if (resp == null) {
								Debug.LogError ("Permision denied.");
								isOperationRunning = false;
							}
							List<string> roles = ((OrgRoleResponse)resp).roles;
							if (roles.Contains ("owner")) {
								ownerAuthed = true;
								if (reqStruct.targetStep == STEP_GET_CLIENT) {
									UnityWebRequest newRequest = AppStoreOnboardApi.GetUnityClientInfo (Application.cloudProjectId);
									UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper ();
									ReqStruct newReqStruct = new ReqStruct ();
									newReqStruct.request = newRequest;
									newReqStruct.resp = clientRespWrapper;
									newReqStruct.targetStep = reqStruct.targetStep;
									requestQueue.Enqueue (newReqStruct);
								} else if (reqStruct.targetStep == STEP_UPDATE_CLIENT) {
									UnityClientInfo unityClientInfo = new UnityClientInfo();
									unityClientInfo.ClientId = unityClientID.stringValue;
									string callbackUrl = callbackUrl_in_memory;
									// read xiaomi from user input
									XiaomiSettings xiaomi = new XiaomiSettings();
									xiaomi.appId = xiaomiAppID.stringValue;
									xiaomi.appKey = xiaomiAppKey.stringValue;
									xiaomi.appSecret = appSecret_in_memory;
									UnityWebRequest newRequest = AppStoreOnboardApi.UpdateUnityClient (Application.cloudProjectId, unityClientInfo, xiaomi, callbackUrl);
									UnityClientResponse clientResp = new UnityClientResponse ();
									ReqStruct newReqStruct = new ReqStruct ();
									newReqStruct.request = newRequest;
									newReqStruct.resp = clientResp;
									newReqStruct.targetStep = reqStruct.targetStep;
									requestQueue.Enqueue (newReqStruct);
								} else if (reqStruct.targetStep == STEP_UPDATE_CLIENT_SECRET) {
									string clientId = unityClientID.stringValue;
									UnityWebRequest newRequest = AppStoreOnboardApi.UpdateUnityClientSecret (clientId);
									UnityClientResponse clientResp = new UnityClientResponse ();
									ReqStruct newReqStruct = new ReqStruct ();
									newReqStruct.request = newRequest;
									newReqStruct.resp = clientResp;
									newReqStruct.targetStep = reqStruct.targetStep;
									requestQueue.Enqueue (newReqStruct);
								}
							} else if (roles.Contains ("user") || roles.Contains ("manager")) {
								ownerAuthed = false;
								if (reqStruct.targetStep == STEP_GET_CLIENT) {
									UnityWebRequest newRequest = AppStoreOnboardApi.GetUnityClientInfo (Application.cloudProjectId);
									UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper ();
									ReqStruct newReqStruct = new ReqStruct ();
									newReqStruct.request = newRequest;
									newReqStruct.resp = clientRespWrapper;
									newReqStruct.targetStep = reqStruct.targetStep;
									requestQueue.Enqueue (newReqStruct);
								} else {
									Debug.LogError ("Permision denied.");
									isOperationRunning = false;
								}
							} else {
								Debug.LogError ("Permision denied.");
								isOperationRunning = false;
							}
						} else if (resp.GetType () == typeof(UnityClientResponseWrapper)) {
							string raw = "{ \"array\": " + request.downloadHandler.text + "}";
							resp = JsonUtility.FromJson<UnityClientResponseWrapper> (raw);
							// only one element in the list
							if (((UnityClientResponseWrapper)resp).array.Length > 0) {
								UnityClientResponse unityClientResp = ((UnityClientResponseWrapper)resp).array [0];
								unityClientID.stringValue = unityClientResp.client_id;
								unityClientKey.stringValue = unityClientResp.client_secret;
								unityClientRSAPublicKey.stringValue = unityClientResp.channel.publicRSAKey;
								clientSecret_in_memory = unityClientResp.channel.channelSecret;
								callbackUrl_in_memory = unityClientResp.channel.callbackUrl;
								callbackUrl_last = callbackUrl_in_memory;
								foreach (ThirdPartySettingsResponse thirdPartySetting in unityClientResp.channel.thirdPartySettings) {
									if (thirdPartySetting.appType.Equals (AppStoreOnboardApi.xiaomiAppType, StringComparison.InvariantCultureIgnoreCase)) {
										xiaomiAppID.stringValue = thirdPartySetting.appId;
										xiaomiAppKey.stringValue = thirdPartySetting.appKey;
										appSecret_in_memory = thirdPartySetting.appSecret;
										appId_last = xiaomiAppID.stringValue;
										appKey_last = xiaomiAppKey.stringValue;
										appSecret_last = appSecret_in_memory;
									}
								}
								AppStoreOnboardApi.updateRev = unityClientResp.rev;
								Debug.Log ("Unity Client Refreshed. Finished: " + reqStruct.targetStep);
								AppStoreOnboardApi.loaded = true;
								isOperationRunning = false;
								serializedObject.ApplyModifiedProperties();
								this.Repaint ();
								AssetDatabase.SaveAssets();
							} else {
								// no client found, generate one.
								if (ownerAuthed) {
									UnityClientInfo unityClientInfo = new UnityClientInfo ();
									string callbackUrl = callbackUrl_in_memory;
									// read xiaomi from user input
									XiaomiSettings xiaomi = new XiaomiSettings ();
									xiaomi.appId = xiaomiAppID.stringValue;
									xiaomi.appKey = xiaomiAppKey.stringValue;
									xiaomi.appSecret = appSecret_in_memory;
									UnityWebRequest newRequest = AppStoreOnboardApi.GenerateUnityClient (Application.cloudProjectId, unityClientInfo, xiaomi, callbackUrl);
									UnityClientResponse clientResp = new UnityClientResponse ();
									ReqStruct newReqStruct = new ReqStruct ();
									newReqStruct.request = newRequest;
									newReqStruct.resp = clientResp;
									newReqStruct.targetStep = reqStruct.targetStep;
									requestQueue.Enqueue (newReqStruct);
								} else {
									Debug.LogError ("Permision denied.");
									isOperationRunning = false;
								}
							}
						} else if (resp.GetType () == typeof(UnityClientResponse)) {
							resp = JsonUtility.FromJson<UnityClientResponse> (request.downloadHandler.text);
							unityClientID.stringValue = ((UnityClientResponse)resp).client_id;
							unityClientKey.stringValue = ((UnityClientResponse)resp).client_secret;
							unityClientRSAPublicKey.stringValue = ((UnityClientResponse)resp).channel.publicRSAKey;
							clientSecret_in_memory = ((UnityClientResponse)resp).channel.channelSecret;
							callbackUrl_in_memory = ((UnityClientResponse)resp).channel.callbackUrl;
							callbackUrl_last = callbackUrl_in_memory;
							foreach (ThirdPartySettingsResponse thirdPartySetting in ((UnityClientResponse)resp).channel.thirdPartySettings) {
								if (thirdPartySetting.appType.Equals (AppStoreOnboardApi.xiaomiAppType, StringComparison.InvariantCultureIgnoreCase)) {
									xiaomiAppID.stringValue = thirdPartySetting.appId;
									xiaomiAppKey.stringValue = thirdPartySetting.appKey;
									appSecret_in_memory = thirdPartySetting.appSecret;
									appId_last = xiaomiAppID.stringValue;
									appKey_last = xiaomiAppKey.stringValue;
									appSecret_last = appSecret_in_memory;
								}
							}
							AppStoreOnboardApi.updateRev = ((UnityClientResponse)resp).rev;
							Debug.Log ("Unity Client Refreshed. Finished: " + reqStruct.targetStep);
							AppStoreOnboardApi.loaded = true;
							isOperationRunning = false;
							serializedObject.ApplyModifiedProperties();
							this.Repaint ();
							AssetDatabase.SaveAssets();
						}
					}
				}
			} else {
				requestQueue.Enqueue (reqStruct);
			}
		}
    }
}
#endif