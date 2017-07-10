using UnityEngine;

namespace AutoyaFramework.Information {
    /**
        あり方が大きく変わりそうなパディング。ビュー内のオブジェクトの指定位置から適当に割り出して内部で適応される値に格下げされて欲しい。
     */
    public class Padding {
        public float top;
        public float right;
        public float bottom; 
        public float left;

		public void Adjust (float top, float right, float bottom, float left) {
			this.top += top;
			this.right += right;
			this.bottom += bottom;
			this.left += left;
		}

        public Vector2 LeftTopPoint () {
            return new Vector2(left, top);
        }

        /**
            width of padding.
        */
        public float PadWidth () {
            return left + right;
        }

        /**
            hight of padding.
        */
        public float PadHeight () {
            return top + bottom;
        }
    }
}