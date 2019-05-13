using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine.UDP.Editor.Analytics;

#if (UNITY_5_6_OR_NEWER && !UNITY_5_6_0)
namespace UnityEngine.UDP.Editor
{
    [CustomEditor(typeof(AppStoreSettings))]
    public class AppStoreSettingsEditor : UnityEditor.Editor
    {
        [MenuItem("Window/Unity Distribution Portal/Settings", false, 111)]
        public static void CreateAppStoreSettingsAsset()
        {
            if (File.Exists(AppStoreSettings.appStoreSettingsAssetPath))
            {
                AppStoreSettings existedAppStoreSettings = CreateInstance<AppStoreSettings>();
                existedAppStoreSettings =
                    (AppStoreSettings) AssetDatabase.LoadAssetAtPath(AppStoreSettings.appStoreSettingsAssetPath,
                        typeof(AppStoreSettings));
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existedAppStoreSettings;
                return;
            }

            if (!Directory.Exists(AppStoreSettings.appStoreSettingsAssetFolder))
                Directory.CreateDirectory(AppStoreSettings.appStoreSettingsAssetFolder);

            var appStoreSettings = CreateInstance<AppStoreSettings>();
            AssetDatabase.CreateAsset(appStoreSettings, AppStoreSettings.appStoreSettingsAssetPath);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = appStoreSettings;
        }

        [MenuItem("Window/Unity Distribution Portal/Settings", true)]
        public static bool CheckUnityOAuthValidation()
        {
            return enableOAuth;
        }

        private UnityClientInfo unityClientInfo;
        private string clientSecret_in_memory;
        private string callbackUrl_in_memory;
        private string existed_clientId_in_memory;

        private bool ownerAuthed = false;
        private static readonly bool enableOAuth = Utils.FindTypeByName("UnityEditor.Connect.UnityOAuth") != null;
        private string callbackUrl_last;
        private string existedClientId = "";

        private const string STEP_GET_CLIENT = "get_client";
        private const string STEP_UPDATE_CLIENT = "update_client";
        private const string STEP_UPDATE_CLIENT_SECRET = "update_client_secret";

        private static List<TestAccount> testAccounts = new List<TestAccount>();
        private TestAccount testAccount = new TestAccount();
        private RolePermission _rolePermission = new RolePermission();

        private AppItem currentAppItem;
        private bool _checkLink = true;
        private bool _canLink = false;
        private string targetStep;

        public struct ReqStruct
        {
            public string currentStep;
            public string targetStep;
            public string eventName;
            public UnityWebRequest request;
            public GeneralResponse resp;
        }

        private Queue<ReqStruct> requestQueue = new Queue<ReqStruct>();

        private class AppStoreStyles
        {
            public const string kNoUnityProjectIDErrorMessage =
                "Unity Project ID doesn't exist, please go to Window/Services to create one.";

            public const int kUnityProjectIDBoxHeight = 24;
            public const int kUnityProjectLinkBoxHeight = 50;
            public const int kUnityClientBoxHeight = 110;
            public const int kUnityAppItemBoxHeight = 60;

            public const int kUnityClientIDButtonWidth = 160;
            public const int kSaveButtonWidth = 120;

            public const int kClientLabelWidth = 140;
            public const int kClientLabelHeight = 16;
            public const int kClientLabelWidthShort = 80;
            public const int kClientLabelHeightShort = 15;

            public static int kTestAccountBoxHeight = 25;
            public const int kTestAccountTextWidth = 110;
        }

        private SerializedProperty unityProjectID;
        private SerializedProperty unityClientID;
        private SerializedProperty unityClientKey;
        private SerializedProperty unityClientRSAPublicKey;
        private SerializedProperty appName;
        private SerializedProperty appSlug;
        private SerializedProperty appItemId;
        private SerializedProperty permission;

        private bool isOperationRunning = false;

        void OnEnable()
        {
            // For unity client settings.
            unityProjectID = serializedObject.FindProperty("UnityProjectID");
            unityClientID = serializedObject.FindProperty("UnityClientID");
            unityClientKey = serializedObject.FindProperty("UnityClientKey");
            unityClientRSAPublicKey = serializedObject.FindProperty("UnityClientRSAPublicKey");
            appName = serializedObject.FindProperty("AppName");
            appSlug = serializedObject.FindProperty("AppSlug");
            appItemId = serializedObject.FindProperty("AppItemId");
            permission = serializedObject.FindProperty("Permission");

            testAccounts = new List<TestAccount>();
            currentAppItem = new AppItem();

            EditorApplication.update += CheckUpdate;
            if (enableOAuth && !String.IsNullOrEmpty(Application.cloudProjectId))
            {
                InitializeSecrets();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(isOperationRunning);

            GUILayout.BeginHorizontal();
            // Unity project id.
            EditorGUILayout.LabelField(new GUIContent("Unity Project ID"));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(AppStoreStyles.kClientLabelHeight)))
            {
                TextEditor te = new TextEditor();
                te.text = Application.cloudProjectId;
                te.SelectAll();
                te.Copy();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box",
                GUILayout.Height(AppStoreStyles.kUnityProjectIDBoxHeight)))
            {
                GUILayout.FlexibleSpace();

                if (!String.IsNullOrEmpty(this.unityProjectID.stringValue) &&
                    !String.IsNullOrEmpty(Application.cloudProjectId) &&
                    !this.unityProjectID.stringValue.Equals(Application.cloudProjectId))
                {
                    this.unityProjectID.stringValue = "";
                    this.unityClientID.stringValue = "";
                    this.unityClientKey.stringValue = "";
                    this.unityClientRSAPublicKey.stringValue = "";
                    this.appName.stringValue = "";
                    this.appSlug.stringValue = "";
                    this.appItemId.stringValue = "";
                    this.permission.stringValue = "";
                    clientSecret_in_memory = "";
                    callbackUrl_in_memory = "";
                    ownerAuthed = false;
                    callbackUrl_last = "";
                    existedClientId = "";
                    currentAppItem = new AppItem();
                    testAccounts = new List<TestAccount>();
                    testAccount = new TestAccount();
                    AppStoreStyles.kTestAccountBoxHeight = 25;
                    AssetDatabase.SaveAssets();

                    if (!EditorUtility.DisplayDialog("Hint",
                        "Your Project ID has changed.\nYou need to generate a new client first. If you want to link this project to your existed client, "
                        + "please open UDP portal in browser to update your client with this new project ID" +
                        "(Warning: Make sure you finish operations in UDP portal before clicking 'Generate Unity Client' button!).",
                        "I'd like to generate a new client, Go Ahead",
                        "Open UDP portal in Browser"))
                    {
                        Application.OpenURL(BuildConfig.CONNECT_ENDPOINT);
                    }
                    else
                    {
                        _checkLink = true;
                        unityClientID.stringValue = null;
                    }
                }

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

            if (String.IsNullOrEmpty(this.unityClientID.stringValue) &&
                !String.IsNullOrEmpty(Application.cloudProjectId) && _checkLink)
            {
                isOperationRunning = true;
                _checkLink = false;
                targetStep = "LinkProject";
                UnityWebRequest newRequest = AppStoreOnboardApi.GetUnityClientInfo(Application.cloudProjectId);
                UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper();
                ReqStruct newReqStruct = new ReqStruct();
                newReqStruct.request = newRequest;
                newReqStruct.resp = clientRespWrapper;
                newReqStruct.targetStep = targetStep;
                requestQueue.Enqueue(newReqStruct);
            }

            if (String.IsNullOrEmpty(this.unityClientID.stringValue) &&
                !String.IsNullOrEmpty(Application.cloudProjectId) && _canLink)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(10);
                GUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Link Project to Existed Client"));
                EditorGUILayout.EndHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Space(2);
                GUILayout.EndVertical();

                using (new EditorGUILayout.VerticalScope("OL Box",
                    GUILayout.Height(AppStoreStyles.kUnityProjectLinkBoxHeight)))
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Client ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    existed_clientId_in_memory =
                        EditorGUILayout.TextField(String.IsNullOrEmpty(existed_clientId_in_memory)
                            ? ""
                            : existed_clientId_in_memory);
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Link") && !String.IsNullOrEmpty(existed_clientId_in_memory))
                    {
                        isOperationRunning = true;
                        UnityWebRequest newRequest =
                            AppStoreOnboardApi.GetUnityClientInfoByClientId(existed_clientId_in_memory);
                        UnityClientResponse unityClientResponse = new UnityClientResponse();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = unityClientResponse;
                        newReqStruct.targetStep = "LinkProject";
                        requestQueue.Enqueue(newReqStruct);
                    }
                }
            }

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            //control by UnityOAuth
            EditorGUI.BeginDisabledGroup(!enableOAuth);

            // Unity client settings.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("UDP Client Settings"));
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kUnityClientBoxHeight)))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                if (String.IsNullOrEmpty(unityClientID.stringValue))
                {
                    EditorGUILayout.LabelField("Client ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    EditorGUILayout.LabelField("Client ID", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField(strPrefix(unityClientID.stringValue),
                        GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy to Clipboard",
                        GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
                    {
                        TextEditor te = new TextEditor();
                        te.text = unityClientID.stringValue;
                        te.SelectAll();
                        te.Copy();
                    }
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                if (String.IsNullOrEmpty(unityClientKey.stringValue))
                {
                    EditorGUILayout.LabelField("Client Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    EditorGUILayout.LabelField("Client Key", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField(strPrefix(unityClientKey.stringValue),
                        GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy to Clipboard",
                        GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
                    {
                        TextEditor te = new TextEditor();
                        te.text = unityClientKey.stringValue;
                        te.SelectAll();
                        te.Copy();
                    }
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                if (String.IsNullOrEmpty(unityClientRSAPublicKey.stringValue))
                {
                    EditorGUILayout.LabelField("Client RSA Public Key",
                        GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    EditorGUILayout.LabelField("Client RSA Public Key",
                        GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField(strPrefix(unityClientRSAPublicKey.stringValue),
                        GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy to Clipboard",
                        GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
                    {
                        TextEditor te = new TextEditor();
                        te.text = unityClientRSAPublicKey.stringValue;
                        te.SelectAll();
                        te.Copy();
                    }
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                if (String.IsNullOrEmpty(clientSecret_in_memory))
                {
                    EditorGUILayout.LabelField("Client Secret", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    EditorGUILayout.LabelField("Client Secret", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                    if (_rolePermission.manager || _rolePermission.owner)
                    {
                        EditorGUILayout.LabelField(strPrefix(clientSecret_in_memory),
                            GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("********",
                            GUILayout.Width(AppStoreStyles.kClientLabelWidthShort));
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(!(_rolePermission.manager || _rolePermission.owner));
                    if (GUILayout.Button("Copy to Clipboard",
                        GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
                    {
                        TextEditor te = new TextEditor();
                        te.text = clientSecret_in_memory;
                        te.SelectAll();
                        te.Copy();
                    }

                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Callback URL", GUILayout.Width(AppStoreStyles.kClientLabelWidth));
                callbackUrl_in_memory =
                    EditorGUILayout.TextField(String.IsNullOrEmpty(callbackUrl_in_memory) ? "" : callbackUrl_in_memory);
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            bool clientNotExists = String.IsNullOrEmpty(unityClientID.stringValue);
            string buttonLabelString = "Generate Unity Client";
            bool isButtonActive = false;
            string target = STEP_GET_CLIENT;
            if (!clientNotExists)
            {
                if (String.IsNullOrEmpty(clientSecret_in_memory) || !AppStoreOnboardApi.loaded)
                {
                    buttonLabelString = "Load Unity Client";
                    AppStoreStyles.kTestAccountBoxHeight = 25;
                    testAccounts = new List<TestAccount>();
                    testAccount = new TestAccount();
                }
                else
                {
                    buttonLabelString = "Refresh Unity Client";
//                    target = STEP_UPDATE_CLIENT_SECRET;
                    isButtonActive = false;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isButtonActive);
            if (GUILayout.Button(buttonLabelString, GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
            {
                isOperationRunning = true;
                if (target == STEP_UPDATE_CLIENT_SECRET)
                {
                    clientSecret_in_memory = null;
                }

                targetStep = target;
                callApiAsync();

                serializedObject.ApplyModifiedProperties();
                this.Repaint();
                AssetDatabase.SaveAssets();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(!(_rolePermission.manager || _rolePermission.owner));
            if (GUILayout.Button("Update Client Settings", GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
            {
                isOperationRunning = true;
                if (clientNotExists)
                {
                    EditorUtility.DisplayDialog("Error",
                        "Please get/generate Unity Client first.",
                        "OK");
                    isOperationRunning = false;
                }
                else
                {
                    if (callbackUrl_last != callbackUrl_in_memory)
                    {
                        if (String.IsNullOrEmpty(callbackUrl_in_memory))
                        {
                            if (EditorUtility.DisplayDialog("Warning",
                                "Are you sure to clear Callback URL?",
                                "Clear", "Do Not Clear"))
                            {
                                targetStep = STEP_UPDATE_CLIENT;
                                callApiAsync();
                            }
                        }
                        else if (CheckURL(callbackUrl_in_memory))
                        {
                            targetStep = STEP_UPDATE_CLIENT;
                            callApiAsync();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error",
                                "Callback URL is invalid. (http/https is required)",
                                "OK");
                            isOperationRunning = false;
                        }
                    }
                    else
                    {
                        isOperationRunning = false;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                this.Repaint();
                AssetDatabase.SaveAssets();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            // Unity client settings.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent("Game Settings"));
            EditorGUILayout.EndVertical();

            EditorGUI.BeginDisabledGroup(!(_rolePermission.manager || _rolePermission.owner));

            if (GUILayout.Button("Update Game", GUILayout.Width(AppStoreStyles.kUnityClientIDButtonWidth)))
            {
                if (currentAppItem.name == null || currentAppItem.slug == null
                                                || currentAppItem.name == "" || currentAppItem.slug == "")
                {
                    EditorUtility.DisplayDialog("Error",
                        "Please fill in Game Title and Game Id fields.",
                        "OK");
                }
                else
                {
                    isOperationRunning = true;
                    currentAppItem.status = "STAGE";
                    UnityWebRequest newRequest = AppStoreOnboardApi.UpdateAppItem(currentAppItem);
                    AppItemResponse appItemResponse = new AppItemResponse();
                    ReqStruct newReqStruct = new ReqStruct();
                    newReqStruct.request = newRequest;
                    newReqStruct.resp = appItemResponse;
                    requestQueue.Enqueue(newReqStruct);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kUnityAppItemBoxHeight)))
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginHorizontal();
                var slugRect = EditorGUILayout.GetControlRect(true);
                currentAppItem.slug = EditorGUI.TextField(slugRect, "Game Id:", currentAppItem.slug);
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();

                var nameRect = EditorGUILayout.GetControlRect(true);
                currentAppItem.name = EditorGUI.TextField(nameRect, "Game Title:", currentAppItem.name);

                EditorGUI.BeginDisabledGroup(true);
                var clientIdRect = EditorGUILayout.GetControlRect(true);
                EditorGUI.TextField(clientIdRect, "Game Revision:", currentAppItem.revision);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            EditorGUILayout.LabelField(new GUIContent("Test Account Settings"));

            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.EndVertical();

            using (new EditorGUILayout.VerticalScope("OL Box", GUILayout.Height(AppStoreStyles.kTestAccountBoxHeight)))
            {
                for (int i = 0; i < testAccounts.Count; i++)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(testAccounts[i].email,
                        GUILayout.Width(AppStoreStyles.kTestAccountTextWidth));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(10);
                    EditorGUI.BeginDisabledGroup(!testAccounts[i].isUpdate);
                    if (testAccounts[i].isUpdate)
                    {
                        testAccounts[i].password = EditorGUILayout.TextField(testAccounts[i].password,
                            GUILayout.Width(AppStoreStyles.kTestAccountTextWidth));
                    }
                    else
                    {
                        EditorGUILayout.TextField(testAccounts[i].password,
                            GUILayout.Width(AppStoreStyles.kTestAccountTextWidth));
                    }

                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!(_rolePermission.manager || _rolePermission.owner));
                    string buttonString = testAccounts[i].isUpdate ? "Save" : "Update";
                    if (GUILayout.Button(buttonString, GUILayout.Width(EditorGUIUtility.singleLineHeight * 3),
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    {
                        if (testAccounts[i].isUpdate)
                        {
                            if (testAccounts[i].password.Length < 6)
                            {
                                EditorUtility.DisplayDialog("Error", "The min length of password is 6", "OK");
                            }
                            else
                            {
                                testAccounts[i].isUpdate = !testAccounts[i].isUpdate;
                                PlayerChangePasswordRequest player = new PlayerChangePasswordRequest();
                                player.password = testAccounts[i].password;
                                player.playerId = testAccounts[i].playerId;
                                UnityWebRequest request = AppStoreOnboardApi.UpdateTestAccount(player);
                                PlayerChangePasswordResponse playerDeleteResponse = new PlayerChangePasswordResponse();
                                ReqStruct reqStruct = new ReqStruct();
                                reqStruct.request = request;
                                reqStruct.resp = playerDeleteResponse;
                                reqStruct.targetStep = null;
                                requestQueue.Enqueue(reqStruct);
                                isOperationRunning = true;
                            }
                        }
                        else
                        {
                            testAccounts[i].password = "";
                            testAccounts[i].isUpdate = !testAccounts[i].isUpdate;
                        }
                    }

                    if (testAccounts[i].isUpdate)
                    {
                        if (GUILayout.Button("Cancel", GUILayout.Width(EditorGUIUtility.singleLineHeight * 3),
                            GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                        {
                            testAccounts[i].password = "******";
                            testAccounts[i].isUpdate = !testAccounts[i].isUpdate;
                        }
                    }

                    if (GUILayout.Button("-", GUILayout.Width(EditorGUIUtility.singleLineHeight),
                            GUILayout.Height(EditorGUIUtility.singleLineHeight))
                        && EditorUtility.DisplayDialog("Delete Test Account?",
                            "Are you sure you want to delete this test account?",
                            "Delete",
                            "Do Not Delete"))
                    {
                        UnityWebRequest request = AppStoreOnboardApi.DeleteTestAccount(testAccounts[i].playerId);
                        PlayerDeleteResponse playerDeleteResponse = new PlayerDeleteResponse();
                        ReqStruct reqStruct = new ReqStruct();
                        reqStruct.request = request;
                        reqStruct.resp = playerDeleteResponse;
                        reqStruct.targetStep = null;
                        requestQueue.Enqueue(reqStruct);
                        isOperationRunning = true;
                    }

                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();
                }

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(!(_rolePermission.manager || _rolePermission.owner));
                GUILayout.MinWidth(AppStoreStyles.kTestAccountTextWidth * 2 + EditorGUIUtility.singleLineHeight * 2);
                testAccount.email = EditorGUILayout.TextField(
                    String.IsNullOrEmpty(testAccount.email) ? "Email" : testAccount.email,
                    GUILayout.Width(AppStoreStyles.kTestAccountTextWidth));
                GUILayout.Space(10);
                testAccount.password = EditorGUILayout.TextField(
                    String.IsNullOrEmpty(testAccount.password) ? "Password" : testAccount.password,
                    GUILayout.Width(AppStoreStyles.kTestAccountTextWidth));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", GUILayout.Width(EditorGUIUtility.singleLineHeight * 2),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                {
                    bool existed = false;
                    foreach (var TA in testAccounts)
                    {
                        if (TA.email.Equals(testAccount.email))
                        {
                            existed = true;
                            break;
                        }
                    }

                    if (testAccount.email == "Email")
                    {
                        EditorUtility.DisplayDialog("Error", "You must fill in Email of the Test Account", "OK");
                    }
                    else if (testAccount.password == "Password")
                    {
                        EditorUtility.DisplayDialog("Error", "You must fill in Password of the Test Account", "OK");
                    }
                    else if (testAccount.password.Length < 6)
                    {
                        EditorUtility.DisplayDialog("Error", "The min length of password is 6", "OK");
                    }
                    else if (!CheckEmailAddr(testAccount.email))
                    {
                        EditorUtility.DisplayDialog("Error", "Email is not valid", "OK");
                    }
                    else if (existed)
                    {
                        EditorUtility.DisplayDialog("Error", "Email already existed", "OK");
                    }
                    else
                    {
                        if (clientNotExists)
                        {
                            EditorUtility.DisplayDialog("Error", "Please get the Unity Client first", "OK");
                        }
                        else
                        {
                            Player player = new Player();
                            player.email = testAccount.email;
                            player.password = testAccount.password;
                            UnityWebRequest request =
                                AppStoreOnboardApi.SaveTestAccount(player, unityClientID.stringValue);
                            PlayerResponse playerResponse = new PlayerResponse();
                            ReqStruct reqStruct = new ReqStruct();
                            reqStruct.request = request;
                            reqStruct.resp = playerResponse;
                            reqStruct.targetStep = null;
                            requestQueue.Enqueue(reqStruct);
                            isOperationRunning = true;
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
            this.Repaint();

            EditorGUI.EndDisabledGroup();

            //control by UnityOAuth
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Edit Game Information on Portal"))
            {
                Application.OpenURL(BuildConfig.CONSOLE_URL);
            }
        }

        string strPrefix(string str)
        {
            var preIndex = str.Length < 5 ? str.Length : 5;
            return str.Substring(0, preIndex) + "...";
        }

        public void Perform<T>(T response)
        {
            var authCodePropertyInfo = response.GetType().GetProperty("AuthCode");
            var exceptionPropertyInfo = response.GetType().GetProperty("Exception");
            string authCode = (string) authCodePropertyInfo.GetValue(response, null);
            Exception exception = (Exception) exceptionPropertyInfo.GetValue(response, null);

            if (authCode != null)
            {
                UnityWebRequest request = AppStoreOnboardApi.GetAccessToken(authCode);
                TokenInfo tokenInfoResp = new TokenInfo();
                ReqStruct reqStruct = new ReqStruct();
                reqStruct.request = request;
                reqStruct.resp = tokenInfoResp;
                reqStruct.targetStep = targetStep;
                requestQueue.Enqueue(reqStruct);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed: " + exception.ToString(), "OK");
                isOperationRunning = false;
            }
        }

        void callApiAsync()
        {
            if (AppStoreOnboardApi.tokenInfo.access_token == null)
            {
                Type unityOAuthType = Utils.FindTypeByName("UnityEditor.Connect.UnityOAuth");
                Type authCodeResponseType = unityOAuthType.GetNestedType("AuthCodeResponse", BindingFlags.Public);
                var performMethodInfo =
                    typeof(AppStoreSettingsEditor).GetMethod("Perform").MakeGenericMethod(authCodeResponseType);
                var actionT =
                    typeof(Action<>).MakeGenericType(authCodeResponseType); // Action<UnityOAuth.AuthCodeResponse>
                var getAuthorizationCodeAsyncMethodInfo = unityOAuthType.GetMethod("GetAuthorizationCodeAsync");
                var performDelegate = Delegate.CreateDelegate(actionT, this, performMethodInfo);
                try
                {
                    getAuthorizationCodeAsyncMethodInfo.Invoke(null,
                        new object[] {AppStoreOnboardApi.oauthClientId, performDelegate});
                }
                catch (TargetInvocationException ex)
                {
                    if (ex.InnerException is InvalidOperationException)
                    {
                        EditorUtility.DisplayDialog("Error", "You must login with Unity ID first.", "OK");
                        isOperationRunning = false;
                    }
                }
            }
            else
            {
                UnityWebRequest request = AppStoreOnboardApi.GetUserId();
                UserIdResponse userIdResp = new UserIdResponse();
                ReqStruct reqStruct = new ReqStruct();
                reqStruct.request = request;
                reqStruct.resp = userIdResp;
                reqStruct.targetStep = targetStep;
                requestQueue.Enqueue(reqStruct);
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

        bool CheckEmailAddr(String email)
        {
            string pattern =
                @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(email);
        }

        bool CheckURL(String URL)
        {
//            string pattern = @"^(http|https):\/\/(((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:)*@)?(((\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5]))|((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?)(:\d*)?)(\/((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)+(\/(([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)*)*)?)?(\?((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|[\uE000-\uF8FF]|\/|\?)*)?(#((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|\/|\?)*)?$";
            string pattern =
                @"^(https?://[\w\-]+(\.[\w\-]+)+(:\d+)?((/[\w\-]*)?)*(\?[\w\-]+=[\w\-]+((&[\w\-]+=[\w\-]+)?)*)?)?$";
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(URL);
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

            // Start initialization. 
            isOperationRunning = true;
            _checkLink = false;
            UnityWebRequest newRequest = AppStoreOnboardApi.GetUnityClientInfo(Application.cloudProjectId);
            UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper();
            ReqStruct newReqStruct = new ReqStruct();
            newReqStruct.request = newRequest;
            newReqStruct.resp = clientRespWrapper;
            newReqStruct.targetStep = "CheckUpdate";
            requestQueue.Enqueue(newReqStruct);
        }

        private string SHA256(String data)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            return hash.ToString();
        }

        private void saveGameSettingsProps(String clientId)
        {
            if (!Directory.Exists(AppStoreSettings.appStoreSettingsPropFolder))
                Directory.CreateDirectory(AppStoreSettings.appStoreSettingsPropFolder);
            StreamWriter writter = new StreamWriter(AppStoreSettings.appStoreSettingsPropPath, false);
            String warningMessage = "*** DO NOT DELETE OR MODIFY THIS FILE !! ***";
            writter.WriteLine(warningMessage);
            writter.WriteLine(clientId);
            writter.WriteLine(warningMessage);
            writter.Close();
        }

        void CheckRequestUpdate()
        {
            if (requestQueue.Count <= 0)
            {
                return;
            }

            ReqStruct reqStruct = requestQueue.Dequeue();
            UnityWebRequest request = reqStruct.request;
            GeneralResponse resp = reqStruct.resp;

            if (request != null && request.isDone)
            {
                if (request.error != null || request.responseCode / 100 != 2)
                {
                    if (request.downloadHandler.text.Contains(AppStoreOnboardApi.invalidAccessTokenInfo)
                        || request.downloadHandler.text.Contains(AppStoreOnboardApi.forbiddenInfo)
                        || request.downloadHandler.text.Contains(AppStoreOnboardApi.expiredAccessTokenInfo))
                    {
                        UnityWebRequest newRequest = AppStoreOnboardApi.RefreshToken();
                        TokenInfo tokenInfoResp = new TokenInfo();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = tokenInfoResp;
                        newReqStruct.targetStep = reqStruct.targetStep;
                        requestQueue.Enqueue(newReqStruct);
                    }
                    else if (request.downloadHandler.text.Contains(AppStoreOnboardApi.invalidRefreshTokenInfo)
                             || request.downloadHandler.text.Contains(AppStoreOnboardApi.expiredRefreshTokenInfo))
                    {
                        targetStep = STEP_GET_CLIENT;
                        AppStoreOnboardApi.tokenInfo.access_token = null;
                        AppStoreOnboardApi.tokenInfo.refresh_token = null;
                        callApiAsync();
                    }
                    else
                    {
                        isOperationRunning = false;
                        ErrorResponse response = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                        #region Analytics Fails

                        if (resp.GetType() == typeof(EventRequestResponse))
                        {
//                            Debug.Log("[Debug] Event Request Failed: " + reqStruct.eventName);
                            return; // Do not show error dialog
                        }

                        if (resp.GetType() == typeof(UnityClientResponse))
                        {
                            string eventName = null;
                            switch (request.method)
                            {
                                case UnityWebRequest.kHttpVerbPOST:
                                    eventName = EditorAnalyticsApi.k_ClientCreateEventName;
                                    break;
                                case UnityWebRequest.kHttpVerbPUT:
                                    eventName = EditorAnalyticsApi.k_ClientUpdateEventName;
                                    break;
                                default:
                                    eventName = null;
                                    break;
                            }

                            if (eventName != null)
                            {
                                UnityWebRequest analyticsRequest =
                                    EditorAnalyticsApi.ClientEvent(eventName, null, response.message); 
                                    
                                ReqStruct analyticsReqStruct = new ReqStruct
                                {
                                    request = analyticsRequest,
                                    resp = new EventRequestResponse(),
                                    eventName = eventName,
                                };

                                requestQueue.Enqueue(analyticsReqStruct);
                            }
                        }

                        if (resp.GetType() == typeof(AppItemResponse))
                        {
                            string eventName;
                            switch (request.method)
                            {
                                case UnityWebRequest.kHttpVerbPOST:
                                    eventName = EditorAnalyticsApi.k_AppCreateEventName;
                                    break;
                                case UnityWebRequest.kHttpVerbPUT:
                                    eventName = EditorAnalyticsApi.k_AppUpdateEventName;
                                    break;
                                default:
                                    eventName = null;
                                    break;
                            }

                            if (eventName != null)
                            {
                                UnityWebRequest analyticsRequest =
                                    EditorAnalyticsApi.AppEvent(eventName, unityClientID.stringValue, null, response.message);
                                
                                ReqStruct analyticsRequestStruct = new ReqStruct
                                {
                                    request = analyticsRequest,
                                    resp = new EventRequestResponse(),
                                    eventName = eventName,
                                };
                                
                                requestQueue.Enqueue(analyticsRequestStruct);
                            }
                        }

                        #endregion

                        if (response != null && response.message != null && response.details != null &&
                            response.details.Length != 0)
                        {
                            EditorUtility.DisplayDialog("Error",
                                response.details[0].field + ": " + response.message,
                                "OK");
                            this.Repaint();
                        }
                        else if (response != null && response.message != null)
                        {
                            EditorUtility.DisplayDialog("Error",
                                response.message,
                                "OK");
                            this.Repaint();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error",
                                "Unknown error",
                                "OK");
                            this.Repaint();
                        }
                    }

                    // Error Recovery
                    if (resp.GetType() == typeof(PlayerResponseWrapper))
                    {
                        listPlayers();
                    }
                }
                else
                {
                    if (resp.GetType() == typeof(TokenInfo))
                    {
                        resp = JsonUtility.FromJson<TokenInfo>(request.downloadHandler.text);
                        AppStoreOnboardApi.tokenInfo.access_token = ((TokenInfo) resp).access_token;
                        AppStoreOnboardApi.tokenInfo.refresh_token = ((TokenInfo) resp).refresh_token;
                        UnityWebRequest newRequest = AppStoreOnboardApi.GetUserId();
                        UserIdResponse userIdResp = new UserIdResponse();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = userIdResp;
                        newReqStruct.targetStep = reqStruct.targetStep;
                        requestQueue.Enqueue(newReqStruct);
                    }
                    else if (resp.GetType() == typeof(UserIdResponse))
                    {
                        resp = JsonUtility.FromJson<UserIdResponse>(request.downloadHandler.text);
                        AppStoreOnboardApi.userId = ((UserIdResponse) resp).sub;
                        UnityWebRequest newRequest = AppStoreOnboardApi.GetOrgId(Application.cloudProjectId);
                        OrgIdResponse orgIdResp = new OrgIdResponse();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = orgIdResp;
                        newReqStruct.targetStep = reqStruct.targetStep;
                        requestQueue.Enqueue(newReqStruct);
                    }
                    else if (resp.GetType() == typeof(OrgIdResponse))
                    {
                        resp = JsonUtility.FromJson<OrgIdResponse>(request.downloadHandler.text);
                        AppStoreOnboardApi.orgId = ((OrgIdResponse) resp).org_foreign_key;
                        UnityWebRequest newRequest = AppStoreOnboardApi.GetOrgRoles();
                        OrgRoleResponse orgRoleResp = new OrgRoleResponse();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = orgRoleResp;
                        newReqStruct.targetStep = reqStruct.targetStep;
                        requestQueue.Enqueue(newReqStruct);
                    }
                    else if (resp.GetType() == typeof(OrgRoleResponse))
                    {
                        resp = JsonUtility.FromJson<OrgRoleResponse>(request.downloadHandler.text);
                        List<string> roles = ((OrgRoleResponse) resp).roles;
                        if (roles.Contains("owner"))
                        {
                            ownerAuthed = true;
                            permission.stringValue = "owner";
                            _rolePermission.owner = true;
                            if (reqStruct.targetStep == STEP_GET_CLIENT)
                            {
                                UnityWebRequest newRequest =
                                    AppStoreOnboardApi.GetUnityClientInfo(Application.cloudProjectId);
                                UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = clientRespWrapper;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else if (reqStruct.targetStep == STEP_UPDATE_CLIENT)
                            {
                                UnityClientInfo unityClientInfo = new UnityClientInfo();
                                unityClientInfo.ClientId = unityClientID.stringValue;
                                string callbackUrl = callbackUrl_in_memory;
                                UnityWebRequest newRequest =
                                    AppStoreOnboardApi.UpdateUnityClient(Application.cloudProjectId, unityClientInfo,
                                        callbackUrl);
                                UnityClientResponse clientResp = new UnityClientResponse();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = clientResp;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else if (reqStruct.targetStep == STEP_UPDATE_CLIENT_SECRET)
                            {
                                string clientId = unityClientID.stringValue;
                                UnityWebRequest newRequest = AppStoreOnboardApi.UpdateUnityClientSecret(clientId);
                                UnityClientResponse clientResp = new UnityClientResponse();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = clientResp;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else if (reqStruct.targetStep == "LinkProject")
                            {
                                UnityWebRequest newRequest =
                                    AppStoreOnboardApi.GetUnityClientInfoByClientId(existedClientId);
                                UnityClientResponse unityClientResponse = new UnityClientResponse();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = unityClientResponse;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                        }
                        else if (roles.Contains("user") || roles.Contains("manager"))
                        {
                            ownerAuthed = false;
                            if (roles.Contains("manager"))
                            {
                                permission.stringValue = "mananger";
                                _rolePermission.manager = true;
                            }
                            else if (roles.Contains("user"))
                            {
                                permission.stringValue = "user";
                                _rolePermission.user = true;
                            }

                            if (reqStruct.targetStep == STEP_GET_CLIENT)
                            {
                                UnityWebRequest newRequest =
                                    AppStoreOnboardApi.GetUnityClientInfo(Application.cloudProjectId);
                                UnityClientResponseWrapper clientRespWrapper = new UnityClientResponseWrapper();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = clientRespWrapper;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error",
                                    "Permission denied.",
                                    "OK");
                                isOperationRunning = false;
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error",
                                "Permission denied.",
                                "OK");
                            permission.stringValue = "none";
                            isOperationRunning = false;
                        }

                        serializedObject.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                    else if (resp.GetType() == typeof(UnityClientResponseWrapper))
                    {
                        string raw = "{ \"array\": " + request.downloadHandler.text + "}";
                        resp = JsonUtility.FromJson<UnityClientResponseWrapper>(raw);
                        // only one element in the list
                        if (((UnityClientResponseWrapper) resp).array.Length > 0)
                        {
                            if (reqStruct.targetStep != null && reqStruct.targetStep == "CheckUpdate")
                            {
                                targetStep = STEP_GET_CLIENT;
                                callApiAsync();
                            }
                            else
                            {
                                UnityClientResponse unityClientResp = ((UnityClientResponseWrapper) resp).array[0];
                                AppStoreOnboardApi.tps = unityClientResp.channel.thirdPartySettings;
                                unityClientID.stringValue = unityClientResp.client_id;
                                unityClientKey.stringValue = unityClientResp.client_secret;
                                unityClientRSAPublicKey.stringValue = unityClientResp.channel.publicRSAKey;
                                unityProjectID.stringValue = unityClientResp.channel.projectGuid;
                                clientSecret_in_memory = unityClientResp.channel.channelSecret;
                                callbackUrl_in_memory = unityClientResp.channel.callbackUrl;
                                callbackUrl_last = callbackUrl_in_memory;
                                AppStoreOnboardApi.updateRev = unityClientResp.rev;
                                AppStoreOnboardApi.loaded = true;
                                serializedObject.ApplyModifiedProperties();
                                this.Repaint();
                                AssetDatabase.SaveAssets();
                                saveGameSettingsProps(unityClientResp.client_id);
                                UnityWebRequest newRequest = AppStoreOnboardApi.GetAppItem(unityClientID.stringValue);
                                AppItemResponseWrapper appItemResponseWrapper = new AppItemResponseWrapper();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = appItemResponseWrapper;
                                requestQueue.Enqueue(newReqStruct);
                            }
                        }
                        else
                        {
                            if (reqStruct.targetStep != null &&
                                (reqStruct.targetStep == "LinkProject" || reqStruct.targetStep == "CheckUpdate"))
                            {
                                _canLink = true;
                                isOperationRunning = false;
                            }
                            // no client found, generate one.
                            else if (ownerAuthed)
                            {
                                UnityClientInfo unityClientInfo = new UnityClientInfo();
                                string callbackUrl = callbackUrl_in_memory;
                                UnityWebRequest newRequest =
                                    AppStoreOnboardApi.GenerateUnityClient(Application.cloudProjectId, unityClientInfo,
                                        callbackUrl);
                                UnityClientResponse clientResp = new UnityClientResponse();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = clientResp;
                                newReqStruct.targetStep = reqStruct.targetStep;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error",
                                    "Permission denied.",
                                    "OK");
                                isOperationRunning = false;
                            }
                        }
                    }
                    else if (resp.GetType() == typeof(UnityClientResponse))
                    {
                        resp = JsonUtility.FromJson<UnityClientResponse>(request.downloadHandler.text);
                        unityClientID.stringValue = ((UnityClientResponse) resp).client_id;
                        unityClientKey.stringValue = ((UnityClientResponse) resp).client_secret;
                        unityClientRSAPublicKey.stringValue = ((UnityClientResponse) resp).channel.publicRSAKey;
                        unityProjectID.stringValue = ((UnityClientResponse) resp).channel.projectGuid;
                        clientSecret_in_memory = ((UnityClientResponse) resp).channel.channelSecret;
                        callbackUrl_in_memory = ((UnityClientResponse) resp).channel.callbackUrl;
                        callbackUrl_last = callbackUrl_in_memory;
                        AppStoreOnboardApi.tps = ((UnityClientResponse) resp).channel.thirdPartySettings;
                        AppStoreOnboardApi.updateRev = ((UnityClientResponse) resp).rev;
                        serializedObject.ApplyModifiedProperties();
                        this.Repaint();
                        AssetDatabase.SaveAssets();
                        saveGameSettingsProps(((UnityClientResponse) resp).client_id);

                        if (request.method == UnityWebRequest.kHttpVerbPOST) // Generated Client
                        {
                            UnityWebRequest analyticsRequest =
                                EditorAnalyticsApi.ClientEvent(EditorAnalyticsApi.k_ClientCreateEventName,
                                    ((UnityClientResponse) resp).client_id, null);

                            ReqStruct analyticsReqStruct = new ReqStruct
                            {
                                request = analyticsRequest,
                                resp = new EventRequestResponse(),
                                eventName = EditorAnalyticsApi.k_ClientCreateEventName,
                            };

                            requestQueue.Enqueue(analyticsReqStruct);
                        }
                        else if (request.method == UnityWebRequest.kHttpVerbPUT) // Updated Client
                        {
                            UnityWebRequest analyticsRequest =
                                EditorAnalyticsApi.ClientEvent(EditorAnalyticsApi.k_ClientUpdateEventName,
                                    ((UnityClientResponse) resp).client_id, null);

                            ReqStruct analyticsReqStruct = new ReqStruct
                            {
                                request = analyticsRequest,
                                resp = new EventRequestResponse(),
                                eventName = EditorAnalyticsApi.k_ClientUpdateEventName,
                            };

                            requestQueue.Enqueue(analyticsReqStruct);
                        }

                        if (reqStruct.targetStep == "LinkProject")
                        {
                            UnityClientInfo unityClientInfo = new UnityClientInfo();
                            unityClientInfo.ClientId = unityClientID.stringValue;
                            UnityWebRequest newRequest =
                                AppStoreOnboardApi.UpdateUnityClient(Application.cloudProjectId, unityClientInfo,
                                    callbackUrl_in_memory);
                            UnityClientResponse clientResp = new UnityClientResponse();
                            ReqStruct newReqStruct = new ReqStruct();
                            newReqStruct.request = newRequest;
                            newReqStruct.resp = clientResp;
                            newReqStruct.targetStep = "GetRole";
                            requestQueue.Enqueue(newReqStruct);
                        }
                        else if (reqStruct.targetStep == "GetRole")
                        {
                            UnityWebRequest newRequest = AppStoreOnboardApi.GetUserId();
                            UserIdResponse userIdResp = new UserIdResponse();
                            ReqStruct newReqStruct = new ReqStruct();
                            newReqStruct.request = newRequest;
                            newReqStruct.resp = userIdResp;
                            newReqStruct.targetStep = STEP_GET_CLIENT;
                            requestQueue.Enqueue(newReqStruct);
                        }
                        else
                        {
                            if (reqStruct.targetStep == STEP_UPDATE_CLIENT)
                            {
                                EditorUtility.DisplayDialog("Hint",
                                    "Unity Client updated successfully.",
                                    "OK");
                            }

                            if (currentAppItem.status == "STAGE")
                            {
                                UnityWebRequest newRequest = AppStoreOnboardApi.UpdateAppItem(currentAppItem);
                                AppItemResponse appItemResponse = new AppItemResponse();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = appItemResponse;
                                requestQueue.Enqueue(newReqStruct);
                            }
                            else
                            {
                                UnityWebRequest newRequest = AppStoreOnboardApi.GetAppItem(unityClientID.stringValue);
                                AppItemResponseWrapper appItemResponseWrapper = new AppItemResponseWrapper();
                                ReqStruct newReqStruct = new ReqStruct();
                                newReqStruct.request = newRequest;
                                newReqStruct.resp = appItemResponseWrapper;
                                requestQueue.Enqueue(newReqStruct);
                            }
                        }
                    }
                    else if (resp.GetType() == typeof(AppItemResponse))
                    {
                        resp = JsonUtility.FromJson<AppItemResponse>(request.downloadHandler.text);
                        appItemId.stringValue = ((AppItemResponse) resp).id;
                        appName.stringValue = ((AppItemResponse) resp).name;
                        appSlug.stringValue = ((AppItemResponse) resp).slug;
                        currentAppItem.id = ((AppItemResponse) resp).id;
                        currentAppItem.name = ((AppItemResponse) resp).name;
                        currentAppItem.slug = ((AppItemResponse) resp).slug;
                        currentAppItem.ownerId = ((AppItemResponse) resp).ownerId;
                        currentAppItem.ownerType = ((AppItemResponse) resp).ownerType;
                        currentAppItem.status = ((AppItemResponse) resp).status;
                        currentAppItem.type = ((AppItemResponse) resp).type;
                        currentAppItem.clientId = ((AppItemResponse) resp).clientId;
                        currentAppItem.packageName = ((AppItemResponse) resp).packageName;
                        currentAppItem.revision = ((AppItemResponse) resp).revision;
                        serializedObject.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();


                        #region Analytics

                        string eventName = null;
                        if (request.method == UnityWebRequest.kHttpVerbPOST)
                        {
                            eventName = EditorAnalyticsApi.k_AppCreateEventName;
                        }
                        else if (request.method == UnityWebRequest.kHttpVerbPUT)
                        {
                            eventName = EditorAnalyticsApi.k_AppUpdateEventName;
                        }

                        if (eventName != null)
                        {
                            ReqStruct analyticsReqStruct = new ReqStruct
                            {
                                eventName = eventName,
                                request = EditorAnalyticsApi.AppEvent(eventName, currentAppItem.clientId,
                                    (AppItemResponse) resp, null),
                                resp = new EventRequestResponse(),
                            };

                            requestQueue.Enqueue(analyticsReqStruct);
                        }

                        #endregion


                        this.Repaint();
                        publishApp(appItemId.stringValue);
                    }
                    else if (resp.GetType() == typeof(AppItemPublishResponse))
                    {
                        AppStoreOnboardApi.loaded = true;
                        resp = JsonUtility.FromJson<AppItemPublishResponse>(request.downloadHandler.text);
                        currentAppItem.revision = ((AppItemPublishResponse) resp).revision;
                        currentAppItem.status = "PUBLIC";
                        listPlayers();
                    }
                    else if (resp.GetType() == typeof(AppItemResponseWrapper))
                    {
                        resp = JsonUtility.FromJson<AppItemResponseWrapper>(request.downloadHandler.text);
                        if (((AppItemResponseWrapper) resp).total < 1)
                        {
                            // generate app
                            currentAppItem.clientId = unityClientID.stringValue;
                            currentAppItem.name = unityProjectID.stringValue;
                            currentAppItem.slug = Guid.NewGuid().ToString();
                            currentAppItem.ownerId = AppStoreOnboardApi.orgId;
                            UnityWebRequest newRequest = AppStoreOnboardApi.CreateAppItem(currentAppItem);
                            AppItemResponse appItemResponse = new AppItemResponse();
                            ReqStruct newReqStruct = new ReqStruct();
                            newReqStruct.request = newRequest;
                            newReqStruct.resp = appItemResponse;
                            requestQueue.Enqueue(newReqStruct);
                        }
                        else
                        {
                            var appItemResp = ((AppItemResponseWrapper) resp).results[0];
                            appName.stringValue = appItemResp.name;
                            appSlug.stringValue = appItemResp.slug;
                            appItemId.stringValue = appItemResp.id;
                            currentAppItem.id = appItemResp.id;
                            currentAppItem.name = appItemResp.name;
                            currentAppItem.slug = appItemResp.slug;
                            currentAppItem.ownerId = appItemResp.ownerId;
                            currentAppItem.ownerType = appItemResp.ownerType;
                            currentAppItem.status = appItemResp.status;
                            currentAppItem.type = appItemResp.type;
                            currentAppItem.clientId = appItemResp.clientId;
                            currentAppItem.packageName = appItemResp.packageName;
                            currentAppItem.revision = appItemResp.revision;
                            serializedObject.ApplyModifiedProperties();
                            AssetDatabase.SaveAssets();
                            this.Repaint();

                            if (appItemResp.status != "PUBLIC")
                            {
                                publishApp(appItemResp.id);
                            }
                            else
                            {
                                AppStoreOnboardApi.loaded = true;
                                listPlayers();
                            }
                        }
                    }
                    else if (resp.GetType() == typeof(PlayerResponse))
                    {
                        resp = JsonUtility.FromJson<PlayerResponse>(request.downloadHandler.text);

                        var playerId = ((PlayerResponse) resp).id;
                        UnityWebRequest newRequest = AppStoreOnboardApi.VerifyTestAccount(playerId);
                        PlayerVerifiedResponse playerVerifiedResponse = new PlayerVerifiedResponse();
                        ReqStruct newReqStruct = new ReqStruct();
                        newReqStruct.request = newRequest;
                        newReqStruct.resp = playerVerifiedResponse;
                        newReqStruct.targetStep = null;
                        requestQueue.Enqueue(newReqStruct);
                    }
                    else if (resp.GetType() == typeof(PlayerResponseWrapper))
                    {
                        resp = JsonUtility.FromJson<PlayerResponseWrapper>(request.downloadHandler.text);
                        testAccounts = new List<TestAccount>();
                        AppStoreStyles.kTestAccountBoxHeight = 25;
                        if (((PlayerResponseWrapper) resp).total > 0)
                        {
                            var exists = ((PlayerResponseWrapper) resp).results;
                            for (int i = 0; i < exists.Length; i++)
                            {
                                TestAccount existed = new TestAccount();
                                existed.email = exists[i].nickName;
                                existed.password = "******";
                                existed.playerId = exists[i].id;
                                testAccounts.Add(existed);
                                AppStoreStyles.kTestAccountBoxHeight += 22;
                            }

                            this.Repaint();
                        }

                        testAccount = new TestAccount();
                        testAccount.email = "Email";
                        testAccount.password = "Password";
                        this.Repaint();
                        isOperationRunning = false;
                    }
                    else if (resp.GetType() == typeof(PlayerVerifiedResponse))
                    {
                        listPlayers();
                    }
                    else if (resp.GetType() == typeof(PlayerChangePasswordResponse))
                    {
                        EditorUtility.DisplayDialog("Hint",
                            "Password changed successfully.",
                            "OK");
                        listPlayers();
                    }
                    else if (resp.GetType() == typeof(PlayerDeleteResponse))
                    {
                        EditorUtility.DisplayDialog("Hint",
                            "Test account deleted successfully.",
                            "OK");
                        listPlayers();
                    }
                }
            }
            else
            {
                requestQueue.Enqueue(reqStruct);
            }
        }

        private void listPlayers()
        {
            UnityWebRequest newRequest = AppStoreOnboardApi.GetTestAccount(unityClientID.stringValue);
            PlayerResponseWrapper playerResponseWrapper = new PlayerResponseWrapper();
            ReqStruct newReqStruct = new ReqStruct();
            newReqStruct.request = newRequest;
            newReqStruct.resp = playerResponseWrapper;
            newReqStruct.targetStep = null;
            requestQueue.Enqueue(newReqStruct);
        }

        private void publishApp(String appItemId)
        {
            UnityWebRequest newRequest = AppStoreOnboardApi.PublishAppItem(appItemId);
            AppItemPublishResponse appItemPublishResponse = new AppItemPublishResponse();
            ReqStruct newReqStruct = new ReqStruct();
            newReqStruct.request = newRequest;
            newReqStruct.resp = appItemPublishResponse;
            requestQueue.Enqueue(newReqStruct);
        }
    }
}
#endif