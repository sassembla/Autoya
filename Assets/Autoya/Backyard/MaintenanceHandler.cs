using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
	detect & notify if the server side services are no longer available by maintenance.
*/
namespace AutoyaFramework
{
    public partial class Autoya
    {
        private bool CheckMaintenance(int httpCode, Dictionary<string, string> responseHeader)
        {
            if (IsMaintenance(httpCode, responseHeader))
            {
                // start running onMaintenance action.
                var cor = onMaintenanceAction();
                mainthreadDispatcher.Commit(cor);

                return true;
            }
            return false;
        }

        private bool IsMaintenance(int httpCode, Dictionary<string, string> responseHeader)
        {
            if (forceMaintenance)
            {
                return true;
            }

            return IsUnderMaintenance(httpCode, responseHeader);
        }


        private static IEnumerator DefaultOnMaintenance()
        {
            // do nothing.
            yield break;
        }

        private Func<IEnumerator> onMaintenanceAction = () => { return DefaultOnMaintenance(); };


        /*
			public api.
		*/

        public static void Maintenance_SetOnMaintenance(Func<IEnumerator> onMaintenance)
        {
            autoya.onMaintenanceAction = onMaintenance;
        }
    }
}