using UnityEngine;

namespace AutoyaFramework.Information {

    public class ViewCursor {
        public float offsetX;
        public float offsetY;
        public float viewWidth;
        public float viewHeight;
        
        public ViewCursor (float offsetX, float offsetY, float viewWidth, float viewHeight) {
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.viewWidth = viewWidth;
            this.viewHeight = viewHeight;
        }

        public ViewCursor (ViewCursor baseCursor) {
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
        public static ViewCursor NextLine (ViewCursor baseCursor, float nextLineOffsetY, float viewWidth, float viewHeight) {
            baseCursor.offsetX = 0;
            baseCursor.offsetY = nextLineOffsetY;

            baseCursor.viewWidth = viewWidth;

            // 次の行の高さに関しては特に厳密な計算をしない。
            baseCursor.viewHeight = viewHeight;
            return baseCursor;
        }

        /**
            左詰めで、次の要素の起点となるviewCursorを返す
         */
        public static ViewCursor NextRightCursor(ViewCursor childView, float viewWidth){
            // オフセットを直前のオフセット + 幅のポイントにずらす。
            childView.offsetX = childView.offsetX + childView.viewWidth;
            
            // offsetYは変わらず

            // コンテンツが取り得る幅を、大元の幅 - 現在のオフセットから計算。
            childView.viewWidth = viewWidth - childView.offsetX;
        
            // offsetYは変わらず、高さに関しては特に厳密な計算をしない。
            childView.viewHeight = 0;
            return childView;
        }

        /**
            起点はそのまま、コンテンツのサイズがない = 0にしたカーソルを返す。
         */
        public static ViewCursor ZeroSizeCursor (ViewCursor baseCursor) {
            baseCursor.viewWidth = 0;
            baseCursor.viewHeight = 0;
            return baseCursor;
        }

        public static ViewCursor NextLeftTopView (TagTree baseTree, float viewWidth) {
            var nextLeftTopCursor = new ViewCursor(baseTree.offsetX + baseTree.viewWidth, baseTree.offsetY, viewWidth, 0);
            return nextLeftTopCursor;
        }

        override public string ToString () {
            return "offsetX:" + offsetX + " offsetY:" + offsetY + " viewWidth:" + viewWidth + " viewHeight:" + viewHeight;
        }   
    }
}