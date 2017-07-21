using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
    public class ParsedTreeCustomizer {
        private Dictionary<string, BoxConstraint[]> constraintsDict;
		public ParsedTreeCustomizer (BoxConstraints[] constraints) {
            this.constraintsDict = new Dictionary<string, BoxConstraint[]>();
            foreach (var constraint in constraints) {
                constraintsDict[constraint.layerName.ToLower()] = constraint.constraints;
            }
        }

        public ParsedTree Customize (ParsedTree parsedTreeRoot) {
            // ルートの子からカスタムタグな可能性がある。

            // 現在のレイヤー : 中身のパーツ名 : パーツ位置 というツリーになってるので、まず現在のパーツ名を見つけて、それと一致するなら子供を移住
            TraverseTagRecursive(parsedTreeRoot);
            
            return parsedTreeRoot;
        }


        private void TraverseTagRecursive (ParsedTree tree) {
            foreach (var child in tree.GetChildren()) {
                if (IsCustomTag(child.prefabName)) {
                    // Debug.LogError("child prefab:" + child.prefabName + "　こいつはカスタムタグ。");
                    // 構造に足す。
                    ExpandCustomTag(child);
                } else {
                    // Debug.LogError("child prefab:" + child.prefabName + "　こいつはカスタムタグではない");
                    TraverseTagRecursive(child);
                }
            }
        }

        private void ExpandCustomTag (ParsedTree tree) {
            var adoptedConstaints = GetConstraints(tree.prefabName);

            // このtree自体がカスタムタグなので、存在する子供に対してboxConstraintをチェックしていく。
            var children = tree.GetChildren();

            var box = new List<ParsedTree>();
            /*
                あ、種類の数を調べて、みたいな感じか。
                まずは重たいかもしれないけど一致を見る
             */
            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                var constraintName = GetLayerBoxName(tree.prefabName, child.prefabName);
                Debug.LogError("layerBoxName:" + constraintName);

                // whereでの名前一致が辛い。まあでもいいか。
                var matched = adoptedConstaints.Where(c => c.boxName == constraintName).ToArray();
                if (!matched.Any()) {
                    throw new Exception("該当するboxが見つからない、誤ったタグ。:" + child.prefabName);
                }

                // pass.

                // このchildは、boxの内部へと移動される。どうやるのがいいのかな〜〜 入れ替え？ layout時に必要なデータをもたせといた方がいいのか。is でチェックできると楽か。 それとも単に名前と内容だけを扱うか。

                // 単に名前とかだけを使おう。まずはここで、このchildをこいつの子にして、
                var boxTree = new ParsedTree();
                tree.ReplaceChildren(child, boxTree);

                // 子の親をboxにする
                boxTree.AddChildren(child);

                /*
                    これで、
                    layer/child
                        ->
                    layer/box/child
                    になる。たぶん。要素の数だけ階層が増える。
                 */

                // で、ここまでで処理は終わり、子の階層の処理を続ける。
                TraverseTagRecursive(child);
            }

            foreach (var adoptedConstaint in adoptedConstaints) {
                Debug.LogError("adoptedConstaint:" + adoptedConstaint.boxName);
            }
        }



        private bool IsCustomTag (string prefabName) {
            var key = prefabName.ToLower();
            if (constraintsDict.ContainsKey(key)) {
                var constraints = constraintsDict[key];
                for (var i = 0; i < constraints.Length; i++) {
                    constraints[i].boxName = constraints[i].boxName.ToLower();
                }
                return true;
            }

            return false;
        }

        
        private string GetLayerBoxName (string layerPrefabName, string boxingPrefabName) {
            return layerPrefabName.ToLower() + "_" + boxingPrefabName.ToLower();
        }

        private BoxConstraint[] GetConstraints (string prefabName) {
            return constraintsDict[prefabName.ToLower()];
        }
    }
}