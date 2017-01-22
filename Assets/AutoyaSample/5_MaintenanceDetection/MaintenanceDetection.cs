using UnityEngine;
using AutoyaFramework;
using System.Collections;

public class MaintenanceDetection : MonoBehaviour {
	
	IEnumerator Start () {
        var authenticated = false;

        Autoya.Auth_SetOnAuthenticated(
            () => {
                authenticated = true;
            }
        );

        while (!authenticated) {
            yield return null;
        }

        // test method for set fake maintenance mode.
        Autoya.forceMaintenance = true;
        
        /*
            ready for handle maintenance mode.
        */
        Autoya.Maintenance_SetOnMaintenance(
            maintenanceData => {
                Debug.LogError("maintenanceData:" + maintenanceData);
            }
        );

        // start connection -> Maintenance mode notification will return.
        Autoya.Http_Get(
            "https://google.com",
            (conId, data) => {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) => {
                Debug.Log("connection failed.");
            }
        );
	}
}
