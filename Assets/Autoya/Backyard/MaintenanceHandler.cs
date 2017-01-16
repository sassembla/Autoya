using System;
using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Connections.HTTP;
using UnityEngine;



/*
    detect & notify if the server side services are no longer available by maintenance.
*/
namespace AutoyaFramework {
    public partial class Autoya {
        private bool CheckMaintenance (int httpCode, Dictionary<string, string> responseHeader) {
			if (IsMaintenance(httpCode, responseHeader)) {
				OnMaintenance();
                return true;
			}
            return false;
		}

		private bool IsMaintenance (int httpCode, Dictionary<string, string> responseHeader) {
            #if UNITY_EDITOR
            {
                if (forceMaintenance) {
                    return true;
                }
            }
            #endif
			return IsUnderMaintenance(httpCode, responseHeader);
		}
        
        private void OnMaintenance () {
            mainthreadDispatcher.Commit(GetMaintenanceInfo());
        }

        /*
            public api.
        */
        private IEnumerator GetMaintenanceInfo () {
            Debug.LogWarning("まだメンテ時アクセス用urlとかを定数に切り出してない。");
            // use raw http connection. no need to authenticate.
            var http = new HTTPConnection();
            var connectionId = "maintenance_" + Guid.NewGuid().ToString();
            var maintenanceUrl = "http://google.com";

            return http.Get(
                connectionId,
                null,
                maintenanceUrl,
                (conId, code, respHeader, data) => {
                    onMaintenanceAction(data);
                },
                (conId, code, reason, respHeader) => {
                    Debug.LogWarning("maintenanceモードの情報取得に失敗するという辛いケース、どうしよう。");
                    onMaintenanceAction(code + "_" + reason);
                }
            );
        }

        private Action<string> onMaintenanceAction = maintenanceReason => {};

        public static void Maintenance_SetOnMaintenance (Action<string> onMaintenance) {
            autoya.onMaintenanceAction = onMaintenance;
        }
    }
}