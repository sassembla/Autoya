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

            var constraints = infoResLoader.CustomTagList().layerConstraints;

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
            // Debug.LogError("TraverseTagRecursive tree:" + infoResLoader.GetTagFromIndex(tree.parsedTag) + " treeType:" + tree.treeType);
            
            switch (tree.treeType) {
                case TreeType.CustomLayer: {
                    ExpandCustomTagToLayer(tree);
                    break;
                }
                case TreeType.CustomEmptyLayer: {
                    ExpandCustomTagToEmptyLayer(tree);
                    break;
                }
                default: {
                    foreach (var child in tree.GetChildren()) {
                        TraverseTagRecursive(child);
                    }
                    break;
                }
            }
        }

        /**
            boxありのlayerを作成する。
            ・childを入れるboxを生成
            ・boxにchildを入れる
            ・layerにboxを入れる
         */
        private void ExpandCustomTagToLayer (ParsedTree tree) {
            var adoptedConstaints = GetConstraints(tree.parsedTag);
            
            var children = tree.GetChildren();

            var box = new List<ParsedTree>();
            Debug.LogWarning("子供順ではなく、boxの出現順をもとに子供コンテンツを制御する必要がある。そのほうが描きやすいかもね。");

            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                
                var newBoxName = GetLayerBoxName(tree.parsedTag, child.parsedTag);
                
                var matchedBoxes = adoptedConstaints.Where(c => c.boxName == newBoxName).ToArray();
                if (!matchedBoxes.Any()) {
                    throw new Exception("no target tag found:" + infoResLoader.GetTagFromIndex(child.parsedTag) + " in this layer:" + infoResLoader.GetTagFromIndex(tree.parsedTag));
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
                    // 新規に中間treeを作成する。
                    var newBoxTreeAttr = new AttributeKVs(){
                        {Attribute._BOX, matchedBoxes[0].rect}
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

        private void ExpandCustomTagToEmptyLayer (ParsedTree tree) {
            // Debug.LogError("専用で一つ、なんでも入るboxを作り出す。範囲は全体。");

            var newBoxName = GetLayerBoxName(tree.parsedTag, tree.parsedTag);

            Debug.LogWarning("名前は仮で newBoxName:" + newBoxName + " このときのtreeType:" + tree.treeType);

            var newTagId = infoResLoader.FindOrCreateTag(newBoxName);
            var boxTree = new ParsedTree(newTagId, new AttributeKVs(), TreeType.CustomBox);
            
            var children = tree.GetChildren();
            for (var i = 0; i < children.Count; i++) {
                var child = children[i];

                // 現在参加しているtreeから離脱
                tree.RemoveChild(child);

                // boxTreeにchildを追加
                boxTree.AddChild(child);

                // 子の階層の処理を続ける。
                TraverseTagRecursive(child);
            }

            tree.AddChild(boxTree);
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