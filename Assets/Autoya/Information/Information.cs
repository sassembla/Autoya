using System;
using System.Collections;
using MarkdownSharp;
using UnityEngine;

namespace AutoyaFramework.Information {
	public class InformationView {
		private readonly Markdown mark;
		
		public InformationView () {
			mark = new Markdown();
		}

		public GameObject Show (Action<IEnumerator> executor, string data, View view) {
			var html = mark.Transform(data);
			var tokenizer = new Tokenizer(html);
			var root = tokenizer.Materialize(
				"test",
				executor,
				view,
				(tag, depth, padding, kv) => {
					
				},
				(go, tag, depth, kv) => {

				}
			);
			
			return root;
		}
	}
}