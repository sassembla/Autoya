using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
    public class ParsedTreeCustomizer {
        private readonly InformationResourceLoader infoResLoader;

		public ParsedTreeCustomizer (InformationResourceLoader infoResLoader) {
            this.infoResLoader = infoResLoader;
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
            var adoptedConstaints = infoResLoader.GetConstraints(tree.parsedTag);
            var children = tree.GetChildren();

            /*
                これで、
                layer/child
                    ->
                layer/box/child x N
                になる。boxの数だけ増える。
            */
            foreach (var box in adoptedConstaints) {
                var boxName = box.boxName;

                var boxingChildren = children.Where(c => infoResLoader.GetLayerBoxName(tree.parsedTag, c.parsedTag) == boxName).ToArray();
                if (boxingChildren.Any()) {
                    var boxTag = infoResLoader.FindOrCreateTag(boxName);
                    
                    // 新規に中間box treeを作成する。
                    var newBoxTreeAttr = new AttributeKVs(){
                        {Attribute._BOX, box.rect}
                    };
                    var boxTree = new ParsedTree(boxTag, newBoxTreeAttr, TreeType.CustomBox);
                    
                    // すでに入っているchildrenを取り除いて、boxを投入
                    tree.ReplaceChildrenToBox(boxingChildren, boxTree);
                    
                    // boxTreeにchildを追加
                    boxTree.AddChildren(boxingChildren);

                    foreach (var boxingChild in boxingChildren) {
                        TraverseTagRecursive(boxingChild);
                    }
                }
            }

            var errorTrees = tree.GetChildren().Where(c => c.treeType != TreeType.CustomBox);
            if (errorTrees.Any()) {
                throw new Exception("unexpected tag:" + string.Join(", ", errorTrees.Select(t => infoResLoader.GetTagFromIndex(t.parsedTag)).ToArray()) + " found at customLayer:" + infoResLoader.GetTagFromIndex(tree.parsedTag) + ". please exclude not defined tags in this layer, or define it on this layer.");
            }
        }
    }
}