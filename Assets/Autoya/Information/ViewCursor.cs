using UnityEngine;

namespace AutoyaFramework.Information {

    public class ChildPos {
        public float offsetX;
        public float offsetY;
        public float viewWidth;
        public float viewHeight;

        public ChildPos (TagTree baseTree) {
            this.offsetX = baseTree.offsetX;
            this.offsetY = baseTree.offsetY;
            this.viewWidth = baseTree.viewWidth;
            this.viewHeight = baseTree.viewHeight;
        }

        /**
            左詰めで、次の要素の起点となるviewCursorを返す
         */
        public static ChildPos NextRightCursor(ChildPos childView, float viewWidth){
            // オフセットを直前のオフセット + 幅のポイントにずらす。
            childView.offsetX = childView.offsetX + childView.viewWidth;
            
            // offsetYは変わらず

            // コンテンツが取り得る幅を、大元の幅 - 現在のオフセットから計算。
            childView.viewWidth = viewWidth - childView.offsetX;
        
            // offsetYは変わらず、高さに関しては特に厳密な計算をしない。
            childView.viewHeight = 0;
            return childView;
        }
    }

    public struct ViewCursor {
        public readonly float offsetX;
        public readonly float offsetY;
        public readonly float viewWidth;
        public readonly float viewHeight;

        public readonly static ViewCursor Empty = new ViewCursor(-1, -1, -1, -1);

        public bool Equals (ViewCursor source) {
            if (source.offsetX != this.offsetX || 
                source.offsetY != this.offsetY ||
                source.viewWidth != this.viewWidth ||
                source.viewHeight != this.viewHeight
            ) {
                return false;
            }

            return true;
        }

        public ViewCursor (float offsetX, float offsetY, float viewWidth, float viewHeight) {
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.viewWidth = viewWidth;
            this.viewHeight = viewHeight;
        }


        public ViewCursor (ChildPos baseCursor) {
            this.offsetX = baseCursor.offsetX;
            this.offsetY = baseCursor.offsetY;
            this.viewWidth = baseCursor.viewWidth;
            this.viewHeight = baseCursor.viewHeight;
        }

        public ViewCursor (Vector2 size) {
            this.offsetX = 0;
            this.offsetY = 0;
            this.viewWidth = size.x;
            this.viewHeight = size.y;
        }


        public static ViewCursor ContainedViewCursor (ViewCursor viewCursor) {
            return new ViewCursor(0, 0, viewCursor.viewWidth, viewCursor.viewHeight);
        }

        /**
            次の行の起点となるviewCursorを返す
         */
        public static ViewCursor NextLine (float nextLineOffsetY, float viewWidth, float viewHeight) {
            return new ViewCursor(0, nextLineOffsetY, viewWidth, viewHeight);
        }

        
        /**
            起点はそのまま、コンテンツのサイズがない = 0にしたカーソルを返す。
         */
        public static ViewCursor ZeroSizeCursor (ViewCursor baseCursor) {
            return new ViewCursor(baseCursor.offsetX, baseCursor.offsetY, 0, 0);
        }

        override public string ToString () {
            return "offsetX:" + offsetX + " offsetY:" + offsetY + " viewWidth:" + viewWidth + " viewHeight:" + viewHeight;
        }   
    }
}