namespace AutoyaFramework.Information {
    public struct HandlePoint {
        public float nextLeftHandle;
        public float nextTopHandle;

        public float viewWidth;
        public float viewHeight;

        public HandlePoint (float nextLeftHandle, float nextTopHandle, float width, float height) {
            this.nextLeftHandle = nextLeftHandle;
            this.nextTopHandle = nextTopHandle;
            this.viewWidth = width;
            this.viewHeight = height;
        }
    }
}