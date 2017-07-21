using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
	public class LayoutedTree : ParsedTree {
		public readonly float totalHeight;
		public LayoutedTree(
			ParsedTree source
		) : base(
			source.parsedTag, 
			source,
			source.keyValueStore, 
			source.rawTagName
		) {
			var childlen = source.GetChildren();

			this.SetChildren(childlen);

			// get total height of layouted tree.
			this.totalHeight = TotalHeight(source);
		}

		private float TotalHeight (ParsedTree tree) {
			var childlen = tree.GetChildren();
			if (childlen.Count == 0) {
				return tree.anchoredPosition.y + tree.sizeDelta.y + tree.padding.PadHeight();
			}

			var last = childlen[childlen.Count - 1];
			return last.anchoredPosition.y + Mathf.Abs(last.sizeDelta.y);
		}
    }
}