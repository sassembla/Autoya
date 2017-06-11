using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;



/**
	tests for Maintenance feature.
*/
public class MaintenanceTests : MiyamasuTestRunner {

	private void DeleteAllData (string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, true);
		}
	}
	
	[MSetup] public void Setup () {
		Autoya.ResetAllForceSetting();
		
		DeleteAllData(AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
		
		var authorized = false;
		Action onMainThread = () => {
			var dataPath = Application.persistentDataPath;
			Autoya.TestEntryPoint(dataPath);

			Autoya.Auth_SetOnAuthenticated(
				() => {
					authorized = true;
				}
			);
		};

		RunOnMainThread(onMainThread);
		
		WaitUntil(
			() => {
				return authorized;
			},
			5,
			"timeout in setup."
		);

		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
		
		Autoya.forceMaintenance = true;
	}
	[MTeardown] public void Teardown () {
		Autoya.ResetAllForceSetting();
	}


	[MTest] public void Maintenance () {
		var isUnderMaintenance = false;

		// start connection -> Maintenance mode notification will return.
		Autoya.Http_Get(
			"https://github.com",
			(string conId, string data) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				isUnderMaintenance = autoyaStatus.inMaintenance;
			}
		);

		WaitUntil(() => isUnderMaintenance, 5, "not in maintenance.");
	}



	private bool onMaintenanceCalled = false;
	[MTest] public void SetOnMaintenance () {
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
			(string conId, string data) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				// do nothing.
			}
		);

		WaitUntil(() => onMaintenanceCalled, 5, "onMaintenanceCalled does not be called.");
	}



	private IEnumerator MyOnMaintenance () {
		onMaintenanceCalled = true;
		yield break;
	}
	
}