using UnityEngine;

namespace AutoyaFramework.Information {
    public class Padding {
        public float top;
        public float left;

        public float width;
        public float height;
		
        public Vector2 LeftTopPoint () {
            return new Vector2(left, top);
        }

        /**
            width of padding.
        */
        public float PadWidth () {
            return width;
        }

        /**
            hight of padding.
        */
        public float PadHeight () {
            return height;
        }
    }
}