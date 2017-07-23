using System;

namespace AutoyaFramework.Information {
	public class OldHandlePoint {
		public float nextLeftHandle;
		public float nextTopHandle;

		public float viewWidth;
		public float viewHeight;

		public OldHandlePoint (float nextLeftHandle, float nextTopHandle, float width, float height) {
			this.nextLeftHandle = nextLeftHandle;
			this.nextTopHandle = nextTopHandle;
			this.viewWidth = width;
			this.viewHeight = height;
		}
	}
}