using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AutoyaFramework.Information {

	public enum ParseErrors {
		NOT_RESERVED_TAG_IN_LAYER,
        FAILED_TO_PARSE_LIST_URI,
        CLOSETAG_NOT_FOUND,
        FAILED_TO_PARSE_COMMENT,
        UNSUPPORTED_ATTR_FOUND,
        UNEXPECTED_ATTR_DESCRIPTION,
        CANNOT_CONTAIN_TEXT_IN_BOX_DIRECTLY,
        UNDEFINED_TAG
    }

    /**
        パーサ。
        stringからTagTreeを生成する。
		customTagListをロードするコメント記述が発見されたら、DLを開始する。
     */
    public class HTMLParser {
		private readonly ResourceLoader resLoader;

		public HTMLParser (ResourceLoader resLoader) {
			this.resLoader = resLoader;
		}

		private Action<int, string> parseFailed;

        public IEnumerator ParseRoot (string source, Action<ParsedTree> parsed) {
			var lines = source.Split('\n');
			for (var i = 0; i < lines.Length; i++) {
				lines[i] = lines[i].TrimStart();
			}


            var root = new ParsedTree();
			parseFailed = (code, reason) => {
				root.errors.Add(new ParseError(code, reason));
			};

			var cor = Parse(root, string.Join(string.Empty, lines));

			while (cor.MoveNext()) {
				yield return null;
			}
			
			parsed(root);
        }

        /**
			与えられたstringから情報を抜き出し、パーツの親子構造を規定する。
			ParsedTreeを返してくる。

			そのうち単一のArrayとしてindexのみで処理するように書き換えると、文字のコピーが減って楽。
		 */
        private IEnumerator Parse (TagTree parentTree, string data) {
			// Debug.LogError("data:" + data + " parentTree:" + resLoader.GetTagFromIndex(parentTree.parsedTag));
			var charIndex = 0;
			var readPoint = 0;
			
            while (true) {
				// consumed.
				if (data.Length <= charIndex) {
					break;
				}

				var chr = data[charIndex];
				// Debug.LogError("chr:" + chr);
				
				if (chr == '<') {
					var foundTag = IsTag(data, charIndex);
					// Debug.LogError("foundTag:" + resLoader.GetTagFromIndex(foundTag));

					switch (foundTag) {
						// get depthAssetList from commented url.
						case (int)HTMLTag._COMMENT: {
							// <!--SOMETHING-->
							var endPos = -1;
							var contentStr = GetContentOfCommentTag(data, charIndex, out endPos);
							if (endPos == -1) {
								yield break;
							}

							var cor = ParseAsComment(parentTree, contentStr);
							while (cor.MoveNext()) {
								yield return null;
							}
							
							charIndex = endPos;
							readPoint = charIndex;
							continue;
						}

						// ignore !SOMETHING tag.
						case (int)HTMLTag._IGNORED_EXCLAMATION_TAG: {
							charIndex = GetClosePointOfIgnoredTag(data, charIndex);
							if (charIndex == -1) {
								yield break;
							}

							readPoint = charIndex;
							continue;
						}


						case (int)HTMLTag._NO_TAG_FOUND: {
							// no tag found. go to next char.
							charIndex++;
							continue;
						}
						
						
						// html tag will be parsed without creating html tag.
						case (int)HTMLTag.html: {
							var endTagStartPos = GetStartPointOfCloseTag(data, charIndex, foundTag);
							if (endTagStartPos == -1) {
								yield break;
							}

							// only content string should be parse.
							var contentStr = GetTagContent(data, charIndex, foundTag, endTagStartPos);

							var cor = Parse(parentTree, contentStr);
							while (cor.MoveNext()) {
								yield return null;
							}

							charIndex = endTagStartPos;
							readPoint = charIndex;
							continue;
						}

						// ignore these tags.
						case (int)HTMLTag.head:
						case (int)HTMLTag.title:{
							charIndex = GetClosePointOfTag(data, charIndex, foundTag);
							if (charIndex == -1) {
								yield break;
							}

							readPoint = charIndex;
							continue;
						}
						default: {
							// pass.
							break;
						}
					}

					// Debug.LogError("foundTag:" + foundTag + " cont:" + data.Substring(charIndex));

					var readingPointStartIndex = 0;
					var readingPointLength = 0;

					if (readPoint < charIndex) {
						// Debug.LogError("readPoint:" + readPoint + " vs charIndex:" + charIndex);
						var length = charIndex - readPoint;

						// reserve index and length.
						readingPointStartIndex = readPoint;
						readingPointLength = length;
					}

					var rawTagName = resLoader.GetTagFromValue(foundTag);
					
					// set tag.
					var tag = foundTag;
					// Debug.LogError("rawTagName:" + rawTagName);

					{
						// set to next char index. after '<tag'
						var tempCharIndex = charIndex + ("<" + rawTagName).Length;
						var tempReadPoint = readPoint;

						/*
							collect attr and find start-tag end.
						*/
						{
							switch (data[tempCharIndex]) {
								case ' ': {// <tag [attr]/> or <tag [attr]>
									var startTagEndIndex = data.IndexOf(">", tempCharIndex);
									// Debug.LogError("startTagEndIndex:" + startTagEndIndex);
									if (startTagEndIndex == -1) {
										// start tag never close.
										charIndex++;
										continue;
									}

									// Debug.LogError("' ' found at tag:" + tag + " startTagEndIndex:" + startTagEndIndex);
									var attrStr = data.Substring(tempCharIndex + 1, startTagEndIndex - tempCharIndex - 1);
									
									var kv = GetAttr(tag, attrStr);
									if (kv == null) {
										yield break;
									}
									
									// tag closed point is tagEndIndex. next point is tagEndIndex + 1.
									tempCharIndex = startTagEndIndex + 1;
									tempReadPoint = tempCharIndex;

									// Debug.LogError("data[tempCharIndex]:" + data[tempCharIndex]);

									/*
										single close tag found.
										this tag content is just closed.
									 */
									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
										// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

										if (!string.IsNullOrEmpty(str)) {
											var contentTagPoint = new TagTree(
												str,
												parentTree.tagValue
											);
											contentTagPoint.SetParent(parentTree);
										}
									}
									
									if (data[startTagEndIndex - 1] == '/') {// <tag [attr]/>
										// Debug.LogError("-1 is / @tag:" + tag);

										{
											var treeType = resLoader.GetTreeType(tag);
											if (treeType == TreeType.NotFound) {
												parseFailed((int)ParseErrors.UNDEFINED_TAG, "the tag:" + resLoader.GetTagFromValue(tag) + " is not defined in both customTagList and default tags.");
												yield break;
											}

											var tagPoint2 = new TagTree(
												tag, 
												kv,
												treeType
											);
											tagPoint2.SetParent(parentTree);

											charIndex = tempCharIndex;
											readPoint = tempReadPoint;
											continue;
										}
									}

									// Debug.LogError("not closed tag:" + tag + " in data:" + data);

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();
									
									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);
									if (endTagIndex == -1) {
										yield break;
									}

									// Debug.LogError("endTagIndex:" + endTagIndex);

									{
										var treeType = resLoader.GetTreeType(tag);

										if (treeType == TreeType.NotFound) {
											parseFailed((int)ParseErrors.UNDEFINED_TAG, "the tag:" + resLoader.GetTagFromValue(tag) + " is not defined in both customTagList and default tags.");
											yield break;
										}

										var tagPoint = new TagTree(
											tag, 
											kv,
											treeType
										);
										tagPoint.SetParent(parentTree);

										var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
										
										// Debug.LogError("contents1:" + contents);
										var cor = Parse(tagPoint, contents);
										while (cor.MoveNext()) {
											yield return null;
										}

										// one tag start & end is detected.
										
										tempCharIndex = endTagIndex + endTag.Length;
										// Debug.LogError("tempCharIndex:" + tempCharIndex + " data:" + data[tempCharIndex]);

										tempReadPoint = tempCharIndex;

										/*
											<T [ATTR]>V</T><SOMETHING...
										*/
										if (tempCharIndex < data.Length && data[tempCharIndex] == '<') {
											charIndex = tempCharIndex;
											readPoint = tempReadPoint;
											continue;
										}
										
										tempCharIndex++;

										charIndex = tempCharIndex;
										readPoint = tempReadPoint;
										continue;
									}
								}
								case '>': {// <tag> start tag is closed.

									// Debug.LogError("> found at tag:" + tag + " cont:" + data.Substring(tempCharIndex) + "___ finding end tag of tag:" + tag);

									// set to next char.
									tempCharIndex = tempCharIndex + 1;


									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
										// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

										if (!string.IsNullOrEmpty(str)) {
											var contentTagPoint = new TagTree(
												str,
												parentTree.tagValue
											);
											contentTagPoint.SetParent(parentTree);
										}
									}

									if (tag == (int)HTMLTag.br) {
										var brTree = new TagTree(tag);
										brTree.SetParent(parentTree);

										charIndex = tempCharIndex;
										readPoint = charIndex;
										continue;
									}

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();

									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);
									if (endTagIndex == -1) {
										yield break;
									}

									// treat tag contained contents.
									var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
									
									var treeType = resLoader.GetTreeType(tag);
									if (treeType == TreeType.NotFound) {
										parseFailed((int)ParseErrors.UNDEFINED_TAG, "the tag:" + resLoader.GetTagFromValue(tag) + " is not defined in both customTagList and default tags.");
										yield break;
									}

									var tree = new TagTree(
										tag,
										new AttributeKVs(),
										treeType
									);
									tree.SetParent(parentTree);
									
									// Debug.LogError("contents2:" + contents);
									var cor = Parse(tree, contents);
									while (cor.MoveNext()) {
										yield return null;
									}

									tempCharIndex = endTagIndex + endTag.Length;
									tempReadPoint = tempCharIndex;
									
									charIndex = tempCharIndex;
									readPoint = tempReadPoint;
									continue;
								}
								default: {
									parseFailed(-1, "parse error. unknown keyword found:" + data[charIndex] + " at tag:" + tag);
									yield break;
								}
							}
						}
					}
				}
				charIndex++;
            }

			if (readPoint < data.Length) { 
				var restStr = data.Substring(readPoint);
				// Debug.LogError("restStr:" + restStr);
				if (!string.IsNullOrEmpty(restStr)) {
					var contentTree = new TagTree(
						restStr,
						parentTree.tagValue
					);
					contentTree.SetParent(parentTree);
				}
			}

			/*
				expand customLayer to layer + box + children.
			 */
			switch (parentTree.treeType) {
				case TreeType.CustomLayer: {
					ExpandCustomTagToLayer(parentTree);
					break;
				}
			}
        }

		private void ExpandCustomTagToLayer (TagTree layerBaseTree) {
			var adoptedConstaints = resLoader.GetConstraints(layerBaseTree.tagValue);
			var children = layerBaseTree.GetChildren();

			/*
				これで、
				layer/child
					->
				layer/box/child x N
				になる。boxの数だけ増える。
			*/
			foreach (var box in adoptedConstaints) {
				var boxName = box.boxName;

				var boxingChildren = children.Where(c => resLoader.GetLayerBoxName(layerBaseTree.tagValue, c.tagValue) == boxName).ToArray();
				
				foreach (var boxingChild in boxingChildren) {
					var boxingChildChildren = boxingChild.GetChildren();
					foreach (var boxingChildChild in boxingChildChildren) {
						if (!resLoader.IsDefaultTag(boxingChildChild.tagValue)) {
							switch (boxingChildChild.treeType) {
								case TreeType.Content_CRLF:
								case TreeType.Content_Text: {
									parseFailed((int)ParseErrors.CANNOT_CONTAIN_TEXT_IN_BOX_DIRECTLY, "tag:" + resLoader.GetTagFromValue(boxingChildChild.tagValue) + " could not contain text value directly. please wrap text content with some tag.");
									return;
								}
							}
						}
					}
				}

				if (boxingChildren.Any()) {
					var boxTag = resLoader.FindOrCreateTag(boxName);

					// 新規に中間box treeを作成する。
					var newBoxTreeAttr = new AttributeKVs(){
						{HTMLAttribute._BOX, box.rect},
						{HTMLAttribute._COLLISION, box.collisionGroupId}
					};
					var boxTree = new TagTree(boxTag, newBoxTreeAttr, TreeType.CustomBox);
					
					// すでに入っているchildrenを取り除いて、boxを投入
					layerBaseTree.ReplaceChildrenToBox(boxingChildren, boxTree);
					
					// boxTreeにchildを追加
					boxTree.AddChildren(boxingChildren);

					// boxingChildがlayerな場合、parentがboxであるというマークをつける。
					foreach (var child in boxingChildren) {
						switch (child.treeType) {
							case TreeType.CustomLayer:
							case TreeType.CustomBox: {
								child.keyValueStore[HTMLAttribute.LAYER_PARENT_TYPE] = "box";
								break;
							}
						}
					}
				}
			}

			var errorTrees = layerBaseTree.GetChildren().Where(c => c.treeType != TreeType.CustomBox);
			if (errorTrees.Any()) {
				parseFailed((int)ParseErrors.NOT_RESERVED_TAG_IN_LAYER, "unexpected tag:" + string.Join(", ", errorTrees.Select(t => resLoader.GetTagFromValue(t.tagValue)).ToArray()) + " found at customLayer:" + resLoader.GetTagFromValue(layerBaseTree.tagValue) + ". please exclude not defined tags in this layer, or define it on this layer.");
			}
		}
		
		/**
			parse comment as specific parameters for Information feature.
			get depthAssetList url if exists.
		 */
		private IEnumerator ParseAsComment (TagTree parent, string data) {
			if (parent.tagValue != (int)HTMLTag._ROOT) {
				// ignored.
				yield break;
			}

			// parse as params only root/comment tag with specific format.
			if (data.StartsWith(ConstSettings.DEPTH_ASSETLIST_URL_START) && data.EndsWith(ConstSettings.DEPTH_ASSETLIST_URL_END)) {
				var keywordLen = ConstSettings.DEPTH_ASSETLIST_URL_START.Length;
				var depthAssetListUrl = data.Substring(keywordLen, data.Length - keywordLen - ConstSettings.DEPTH_ASSETLIST_URL_END.Length);
				
				try {
					var uri = new Uri(depthAssetListUrl);
				} catch (Exception e) {
					parseFailed((int)ParseErrors.FAILED_TO_PARSE_LIST_URI, "failed to get uri from depth asset list url. error:" + e);
					yield break;
				}
				
				/*
					start loading of depthAssetList.
				 */
				var cor = resLoader.LoadCustomTagList(depthAssetListUrl);

				while (cor.MoveNext()) {
					yield return null;
				}
				
				// loaded.
			}
		}

		private string GetContentOfCommentTag (string data, int offset, out int tagEndPos) {
			var startPos = data.IndexOf("<!--", offset);
			if (startPos == -1) {
				parseFailed((int)ParseErrors.FAILED_TO_PARSE_COMMENT, "failed to parse comment tag. Information feature uses specific formatted comment tag.");
				tagEndPos = -1;
				return string.Empty;
			}

			var commentContentStartPos = startPos + "<!--".Length;

			var commentEndPos = data.IndexOf("-->", commentContentStartPos);
			if (commentEndPos == -1) {
				parseFailed((int)ParseErrors.FAILED_TO_PARSE_COMMENT, "failed to find end of comment tag. Information feature uses specific formatted comment tag.");
				tagEndPos = -1;
				return string.Empty;
			}

			// set tag end pos. <!--SOMETHING-->(here)
			tagEndPos = commentEndPos + "-->".Length;
			
			return data.Substring(commentContentStartPos, commentEndPos - commentContentStartPos);
		}

		/**
			<SOMETHING>(startPos)X(endPos)</SOMETHING>
			return X.
		 */
		private string GetTagContent (string data, int offset, int foundTagIndex, int endPos) {
			var foundTagStr = resLoader.GetTagFromValue(foundTagIndex);
			var foundTagLength = ("<" + foundTagStr.ToLower() + ">").Length;
			var startPos = offset + foundTagLength;
			var contentStr = data.Substring(startPos, endPos - startPos);
			return contentStr;
		}

		/**
			<SOMETHING>(closePoint)
			return closePoint.
		 */
		private int GetClosePointOfIgnoredTag (string data, int offset) {
			var nearestCloseTagIndex = data.IndexOf('>', offset);
			if (nearestCloseTagIndex == -1) {
				parseFailed((int)ParseErrors.CLOSETAG_NOT_FOUND, "failed to parse data. tag like <!~ is not closed properly.");
				return -1;
			}
			return nearestCloseTagIndex + 1;
		}

		/**
			<(startPoint)/SOMETHING>
			return startPoint.
		 */
		private int GetStartPointOfCloseTag (string data, int offset, int foundTagIndex) {
			var foundTagStr = resLoader.GetTagFromValue(foundTagIndex);
			var closeTagStr = "</"+ foundTagStr.ToLower() + ">";
			var nearestHeaderCloseTagIndex = data.IndexOf(closeTagStr, offset);
			if (nearestHeaderCloseTagIndex == -1) {
				parseFailed((int)ParseErrors.CLOSETAG_NOT_FOUND, "failed to parse data. tag '" + foundTagStr + "' is not closed properly.");
				return -1;
			}
			return nearestHeaderCloseTagIndex;
		}

		/**
			</SOMETHING>(closePoint)
			return closePoint.
		 */
		private int GetClosePointOfTag (string data, int offset, int foundTagIndex) {
			var foundTagStr = resLoader.GetTagFromValue(foundTagIndex);
			var closeTagStr = "</"+ foundTagStr.ToLower() + ">";
			return GetStartPointOfCloseTag(data, offset, foundTagIndex) + closeTagStr.Length;
		}

		
		private int FindEndTag (string endTagStr, string startTagStr, string data, int offset) {
			// Debug.LogError("endTagStr:" + endTagStr + " startTagStr:" + startTagStr);
			var cascadedStartTagIndexies = GetStartTagIndexiesOf(startTagStr, data, offset);
			var endTagCandidateIndexies = GetEndTagIndexiesOf(endTagStr, data, offset);

			// finding pair of start-end tags.
			for (var i = 0; i < endTagCandidateIndexies.Length; i++) {
				var endIndex = endTagCandidateIndexies[i];

				// if start tag exist, this endTag is possible pair.
				if (i < cascadedStartTagIndexies.Length) {
					// start tag exists, 
					var startIndex = cascadedStartTagIndexies[i];

					// endIndex appears faster than startIndex.
					// endIndex is that we expected.
					if (endIndex < startIndex) {
						return endIndex;
					} else {
						// startIndex appears faster than endIndex. maybe they are pair.
						// continue to find.
						continue;
					}
				} else {
					// startIndex is exhausted, found endInex is the result.
					return endIndex;
				}
			}

			parseFailed((int)ParseErrors.CLOSETAG_NOT_FOUND, "parse error. failed to find end tag:" + endTagStr + " after charIndex:" + offset + " data:" + data);
			return -1;
		}
        
		private int[] GetStartTagIndexiesOf (string tagStr, string data, int offset) {
			var resultList = new List<int>();
			var result = -1;
			while (true) {
				result = data.IndexOf(tagStr, offset);
				if (result == -1) {
					break;
				}

				if (data[result + tagStr.Length] == ' ' || data[result + tagStr.Length] == '>') {
					resultList.Add(result);
				}

				offset = result + 1;
			}
			return resultList.ToArray();
		}

		private int[] GetEndTagIndexiesOf (string tagStr, string data, int offset) {
			var resultList = new List<int>();
			var result = -1;
			while (true) {
				result = data.IndexOf(tagStr, offset);
				if (result == -1) {
					break;
				}

				resultList.Add(result);
				offset = result + 1;
			}
			return resultList.ToArray();
		}

		private AttributeKVs GetAttr (int tagIndex, string originalAttrSource) {
			// [src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' width='100' height='200' /]
			var source = originalAttrSource.TrimEnd('/');
			// Debug.LogError("source:" + source);

			var kvDict = new AttributeKVs();
			
			// k1="v1" k2='v2'
			// k1="v1%" k2='v2%'
			var index = 0;
			while (true) {
				if (source.Length <= index) {
					break;
				}
				
				var eqIndex = source.IndexOf('=', index);
				if (eqIndex == -1) {
					// no "=" found.
					break;
				}

				// = is found.

				var keyStr = source.Substring(index, eqIndex - index);
				HTMLAttribute keyEnum = HTMLAttribute._UNKNOWN;
				try {
					keyEnum = (HTMLAttribute)Enum.Parse(typeof(HTMLAttribute), keyStr, true);
				} catch (Exception e) {
					parseFailed((int)ParseErrors.UNSUPPORTED_ATTR_FOUND, "at tag:" + resLoader.GetTagFromValue(tagIndex) + ", found attribute:" + keyStr + " is not supported yet, e:" + e);
					return null;
				}
				
				var valStartIndex = eqIndex + 1;
				
				var delim = source[valStartIndex];
				var valEndIndex = source.IndexOf(delim, valStartIndex + 1);
				if (valEndIndex == -1) {
					// no delim end found.
					parseFailed((int)ParseErrors.UNEXPECTED_ATTR_DESCRIPTION, "attribute at tag:" + resLoader.GetTagFromValue(tagIndex) + " contains illigal description. source:" + originalAttrSource);
					return null;
				}

				var val = source.Substring(valStartIndex + 1, valEndIndex - (valStartIndex + 1));

				kvDict[keyEnum] = val;

				var spaceIndex = source.IndexOf(" ", valEndIndex);
				if (spaceIndex == -1) {
					break;
				}

				index = spaceIndex + 1;
			}
			
			// foreach (var dict in kvDict) {
			// 	Debug.LogError("kv:" + dict.Key + " val:" + dict.Value);
			// }

			return kvDict;
		}

		private int IsTag (string data, int index) {
			var tagStartPos = index + 1/* "<" */;
			/*
				get max length of tag.
				finding Tag is the way for finding "<" and some "tag" char in this feature.
				
				like <SOMETHING....

				and this feature has limit of len of tag. is defined at InformationConstSettings.TAG_MAX_LEN.

				get TAG_MAX_LEN char for finding tag.
				if the len of data is less than this 12 char, len is become that data's len itself.
			 */
			var allowedMaxTagLength = ConstSettings.TAG_MAX_LEN;
			if (data.Length - tagStartPos < allowedMaxTagLength) {
				allowedMaxTagLength = data.Length - tagStartPos;
			}

			// get sampling str.
			var tagFindingSampleStr = data.Substring(tagStartPos, allowedMaxTagLength).ToLower();
			// Debug.LogError("tagFindingSampleStr:" + tagFindingSampleStr);
			if (tagStartPos < data.Length && data[tagStartPos] == '!') {
				if (data[index + 2] == '-') {
					return (int)HTMLTag._COMMENT;
				}

				// not comment.
				return (int)HTMLTag._IGNORED_EXCLAMATION_TAG;
			}

			var closeTagIndex = tagFindingSampleStr.IndexOfAny(new char[]{' ', '>'});
			if (closeTagIndex == -1) {
				return (int)HTMLTag._NO_TAG_FOUND; 
			}

			var tagCandidateStr = tagFindingSampleStr.Substring(0, closeTagIndex);
			return resLoader.FindOrCreateTag(tagCandidateStr);
		}
    }
}