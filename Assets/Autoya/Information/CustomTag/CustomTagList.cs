using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
    [Serializable] public class UUebTags {
        [SerializeField] public string viewName;
        [SerializeField] public ContentInfo[] contents;
        [SerializeField] public LayerInfo[] layerInfos;
        
        public UUebTags (string viewName, ContentInfo[] contents, LayerInfo[] constraints) {
            this.viewName = viewName;
            this.contents = contents;
            this.layerInfos = constraints;
        }

        public Dictionary<string, TreeType> GetTagTypeDict () {
            var tagNames = new Dictionary<string, TreeType>();
            foreach (var content in contents) {
                tagNames[content.contentName] = content.type;
            }
            foreach (var constraint in layerInfos) {
                if (constraint.boxes.Any()) {
                    tagNames[constraint.layerName] = TreeType.CustomLayer;
                } else {
                    tagNames[constraint.layerName] = TreeType.CustomEmptyLayer;
                }
            }
            
            return tagNames;
        }
    }

    [Serializable] public class ContentInfo {
        [SerializeField] public string contentName;
        [SerializeField] public TreeType type;
        [SerializeField] public string loadPath;

        public ContentInfo (string contentName, TreeType type, string loadPath) {
            this.contentName = contentName;
            this.type = type;
            this.loadPath = loadPath;
        }
    }

    /**
        エディタ上でのみ使用する型コンテナ。collisionBaseSizeを保持しておき、collisionGroupを生成し、layerInfoにgroupIdとして出力する。
        ゲーム中で使用されるのはlayerInfo。
     */
    [Serializable] public class LayerInfoOnEditor {
        [SerializeField] public LayerInfo layerInfo;
        [SerializeField] public Vector2 collisionBaseSize;

        public LayerInfoOnEditor (LayerInfo layerInfo, Vector2 collisionBaseSize) {
            this.layerInfo = layerInfo;
            this.collisionBaseSize = collisionBaseSize;
        }
    }

    [Serializable] public class LayerInfo {
        [SerializeField] public string layerName;
        [SerializeField] public BoxPos unboxedLayerSize;
        [SerializeField] public BoxConstraint[] boxes;
        [SerializeField] public string loadPath;
        public LayerInfo (string layerName, BoxPos unboxedLayerSize, BoxConstraint[] boxes, string loadPath) {
            this.layerName = layerName;
            this.unboxedLayerSize = unboxedLayerSize;
            this.boxes = boxes;
            this.loadPath = loadPath;
        }
    }

    /**
        レイヤーを格納するboxの情報。
        親レイヤー内でのboxの位置を保持している。
     */
    [Serializable] public class BoxConstraint {
        [SerializeField] public string boxName;
        [SerializeField] public BoxPos rect;
        [SerializeField] public int collisionGroupId;
        
        public BoxConstraint (string boxName, BoxPos rect) {
            this.boxName = boxName.ToLower();
            this.rect = rect;
        }
    }

    [Serializable] public class BoxPos {
        [SerializeField] public Vector2 offsetMin;
        [SerializeField] public Vector2 offsetMax;
        [SerializeField] public Vector2 pivot;

        [SerializeField] public Vector2 anchorMin;
        [SerializeField] public Vector2 anchorMax;

        [SerializeField] public float originalHeight;

        public BoxPos (RectTransform rect, float originalHeight) {
            this.offsetMin = rect.offsetMin;
            this.offsetMax = rect.offsetMax;
            this.pivot = rect.pivot;
            this.anchorMin = rect.anchorMin;
            this.anchorMax = rect.anchorMax;
            this.originalHeight = originalHeight;
        }
        
        override public string ToString () {
            return "offsetMin:" + this.offsetMin + " offsetMax:" + this.offsetMax + " pivot:" +this.pivot + " anchorMin:" + this.anchorMin + " anchorMax:" + this.anchorMax;
        }
    }
}