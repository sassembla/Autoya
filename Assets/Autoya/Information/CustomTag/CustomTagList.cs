using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoyaFramework.Information {
    [Serializable] public class CustomTagList {
        [SerializeField] public string viewName;
        [SerializeField] public ContentInfo[] contents;
        [SerializeField] public LayerInfo[] layerConstraints;
        
        public CustomTagList (string viewName, ContentInfo[] contents, LayerInfo[] constraints) {
            this.viewName = viewName;
            this.contents = contents;
            this.layerConstraints = constraints;
        }

        public Dictionary<string, TreeType> GetTagTypeDict () {
            var tagNames = new Dictionary<string, TreeType>();
            foreach (var content in contents) {
                tagNames[content.contentName] = content.type;
            }
            foreach (var constraint in layerConstraints) {
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
        [SerializeField] public BoxConstraint[] boxes;
        [SerializeField] public BoxCollisionGroup[] collisions;
        [SerializeField] public string loadPath;
        public LayerInfo (string layerName, BoxConstraint[] boxes, string loadPath) {
            this.layerName = layerName;
            this.boxes = boxes;
            this.loadPath = loadPath;
        }
    }

    [Serializable] public class BoxConstraint {
        [SerializeField] public string boxName;
        [SerializeField] public BoxPos rect;

        public BoxConstraint (string boxName, BoxPos rect) {
            this.boxName = boxName.ToLower();
            this.rect = rect;
        }
    }

    [Serializable] public class BoxPos {
        [SerializeField] public Vector2 anchoredPosition;
        [SerializeField] public Vector2 sizeDelta;
        [SerializeField] public Vector2 offsetMin;
        [SerializeField] public Vector2 offsetMax;
        [SerializeField] public Vector2 pivot;

        [SerializeField] public Vector2 anchorMin;
        [SerializeField] public Vector2 anchorMax;

        public BoxPos (RectTransform rect) {
            this.anchoredPosition = rect.anchoredPosition;
            this.sizeDelta = rect.sizeDelta;
            this.offsetMin = rect.offsetMin;
            this.offsetMax = rect.offsetMax;
            this.pivot = rect.pivot;
            this.anchorMin = rect.anchorMin;
            this.anchorMax = rect.anchorMax;
        }
        
        override public string ToString () {
            return "anchoredPosition:" + this.anchoredPosition + " sizeDelta:" + this.sizeDelta + " offsetMin:" + this.offsetMin + " offsetMax:" + this.offsetMax + " pivot:" +this.pivot + " anchorMin:" + this.anchorMin + " anchorMax:" + this.anchorMax;
        }
    }

    [Serializable] public class BoxCollisionGroup {
        [SerializeField] public string[] boxNames;
        public BoxCollisionGroup (string[] boxNames) {
            this.boxNames = boxNames;
        }
    }
}