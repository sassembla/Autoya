using UnityEngine;
using AutoyaFramework;
using System.Collections;

public class MaintenanceDetection : MonoBehaviour {
	
	private IEnumerator MyOnMaintenance () {
		/*
			run your own maintenance behaviour.

			this sample runs http get method for getting the maintenance information from www.
		*/
		var http = new AutoyaFramework.Connections.HTTP.HTTPConnection();
		return http.Get(
			string.Empty,
			null,
			"https://google.com",// fake url. please use your own url for serve maintenance information data.
			(conId, code, responseHeader, result) => {
				Debug.Log("here you've got maintenance information from your host(recommend to set that server is not same with your game server).");
				Debug.Log("maintenance info:" + result);
			},
			(conId, code, reason, responseHeader) => {
				// do something.. 
			},
			10
		);
	}

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
			you can set the method which will be called on maintenance.
		*/
		Autoya.Maintenance_SetOnMaintenance(
			MyOnMaintenance
		);

		// start connection -> Maintenance mode notification will return.
		Autoya.Http_Get(
			"https://github.com",
			(conId, data) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				/*
					you can check if service is in maintenance mode or not from autoyaStatus.
				*/
				var isUnderMaintenance = autoyaStatus.inMaintenance;
				Debug.Log("connection failed by maintenance:" + isUnderMaintenance);

				// reset for end test.
				Autoya.forceMaintenance = false;
			}
		);
	}
}
