#define MB (1024*1024)

extern "C" {
	int _iOSStorage_AvailableMb();
	int _iOSStorage_TotalMb();
}

/*
 * ストレージ空き容量の取得
 */
int _iOSStorage_AvailableMb() {
	NSArray *paths = NSSearchPathForDirectoriesInDomains(NSLibraryDirectory, NSUserDomainMask, YES);
	NSDictionary *dict = [[NSFileManager defaultManager] attributesOfFileSystemForPath:[paths lastObject] error:nil];

	if (dict) {
		float freeStorage = [[dict objectForKey: NSFileSystemFreeSize] floatValue] / MB;
		return (int)freeStorage;
	}

	return 0;
}

/*
 * ストレージ全容量の取得
 */
int _iOSStorage_TotalMb() {
	NSArray *paths = NSSearchPathForDirectoriesInDomains(NSLibraryDirectory, NSUserDomainMask, YES);
	NSDictionary *dict = [[NSFileManager defaultManager] attributesOfFileSystemForPath:[paths lastObject] error:nil];
	
	if (dict) {
		float totalStorage = [[dict objectForKey: NSFileSystemSize] floatValue] / MB;
		return (int)totalStorage;
	}
	
	return 0;
}