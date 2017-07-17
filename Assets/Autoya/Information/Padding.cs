using UnityEngine;

namespace AutoyaFramework.Information {
    public class Padding {
        public float top;
        public float right;
        public float bottom; 
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
            return left + width + right;
        }

        /**
            hight of padding.
        */
        public float PadHeight () {
            return top + height + bottom;
        }
    }
}