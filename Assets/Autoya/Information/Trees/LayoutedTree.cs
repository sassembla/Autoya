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
			var childlen = source.GetChildlen();

			this.SetChildlen(childlen);

			// get total height of layouted tree.
			this.totalHeight = TotalHeight(source);
		}

		private float TotalHeight (ParsedTree tree) {
			var childlen = tree.GetChildlen();
			if (childlen.Count == 0) {
				return tree.anchoredPosition.y + tree.sizeDelta.y + tree.padding.PadHeight();
			}

			var last = childlen[childlen.Count - 1];
			return TotalHeight(last);
		}
    }
}