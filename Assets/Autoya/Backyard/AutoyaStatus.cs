namespace AutoyaFramework {
    /**
		struct for represents Autoya's specific status.
		
		if inMaintenance == true, server is in maintenance mode. == returns http code for maintenance.
			see OverridePoint.cs "IsUnderMaintenance" method to change this behaviour.

		if isAuthFailed == true, server returns 401.
			see OverridePoint.cs "IsUnauthorized" method to change this behaviour.
	*/
	public struct AutoyaStatus {
		public readonly bool inMaintenance;
		public readonly bool isAuthFailed;
		public AutoyaStatus (bool inMaintenance, bool isAuthFailed, bool userValidateFailed=false) {
			this.inMaintenance = inMaintenance;
			this.isAuthFailed = isAuthFailed;
		}
	}
}