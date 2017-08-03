using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
    public class ParsedTreeCustomizer {
        private Dictionary<string, BoxConstraint[]> constraintsDict;
        private readonly InformationResourceLoader infoResLoader;
		public ParsedTreeCustomizer (InformationResourceLoader infoResLoader) {
            this.infoResLoader = infoResLoader;
            this.constraintsDict = new Dictionary<string, BoxConstraint[]>();

            var constraints = infoResLoader.DepthAssetList().layerConstraints;

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
                if (IsCustomTag(child.parsedTag)) {
                    // Debug.LogError("child prefab:" + child.prefabName + "　こいつはカスタムタグ。");
                    ExpandCustomTag(child);
                } else {
                    // Debug.LogError("child prefab:" + child.prefabName + "　こいつはカスタムタグではない");
                    TraverseTagRecursive(child);
                }
            }
        }

        /**
            カスタムタグの内容を分解し、存在するchildの代わりにboxを挿入する。
         */
        private void ExpandCustomTag (ParsedTree tree) {
            var adoptedConstaints = GetConstraints(tree.parsedTag);
            // foreach (var s in adoptedConstaints) {
            //     Debug.LogError("s:" + s.boxName);
            // }
            
            // このtree自体がカスタムタグなので、存在する子供に対してboxConstraintをチェックしていく。
            var children = tree.GetChildren();

            var box = new List<ParsedTree>();
            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                
                var newBoxName = GetLayerBoxName(tree.parsedTag, child.parsedTag);
                // Debug.LogError("newBoxName:" + newBoxName);
                
                // whereでの名前一致が辛い。まあでもいいか。
                var matchedBoxies = adoptedConstaints.Where(c => c.boxName == newBoxName).ToArray();
                if (!matchedBoxies.Any()) {
                    throw new Exception("該当するboxが見つからない、行き先のないhtmlタグを発見した:" + infoResLoader.GetTagFromIndex(tree.parsedTag) + " newBoxName:" + newBoxName);
                }

                // pass.

                var newTagId = infoResLoader.FindOrCreateTag(newBoxName);
                
                // もしすでにtreeに同名の子供がいたら、そいつにこの子も追加する話になる。
                if (tree.ContainsChild(newTagId)) {
                    // すでにchildが存在してるので、このchildはそこに追加する。
                    
                    // 現在参加しているtreeから離脱
                    tree.RemoveChild(child);

                    var boxTree = tree.GetChildOfTag(newTagId);

                    // boxTreeにchildを追加
                    boxTree.AddChild(child);
                } else {
                    Debug.LogError("add box.");
                    // 新規に中間treeを作成する。
                    var newBoxTreeAttr = new AttributeKVs(){
                        {Attribute._BOX, matchedBoxies[0].rect}
                    };
                    var boxTree = new ParsedTree(newTagId, newBoxTreeAttr, TreeType.CustomBox);
                    
                    // すでに入っているchildとboxTreeを交換
                    tree.ReplaceChildren(child, boxTree);
                    
                    // boxTreeにchildを追加
                    boxTree.AddChild(child);
                }
                
                /*
                    これで、
                    layer/child
                        ->
                    layer/box/child x N
                    になる。boxの数だけ増える。
                 */

                // 子の階層の処理を続ける。
                TraverseTagRecursive(child);
            }
        }



        private bool IsCustomTag (int parsedTag) {
            var key = infoResLoader.GetTagFromIndex(parsedTag);
            if (constraintsDict.ContainsKey(key)) {
                var constraints = constraintsDict[key];
                for (var i = 0; i < constraints.Length; i++) {
                    constraints[i].boxName = constraints[i].boxName.ToLower();
                }
                return true;
            }

            return false;
        }

        
        private string GetLayerBoxName (int layerTag, int boxTag) {
            return infoResLoader.GetTagFromIndex(layerTag) + "_" + infoResLoader.GetTagFromIndex(boxTag);
        }

        private BoxConstraint[] GetConstraints (int parsedTag) {
            var key = infoResLoader.GetTagFromIndex(parsedTag);
            return constraintsDict[key];
        }
    }
}