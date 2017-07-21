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
    public struct ContentAndWidthAndHeight {
		public string content;
        public float width;
        public int height;
        public ContentAndWidthAndHeight (string content, float width, int totalHeight) {
			this.content = content;
            this.width = width;
            this.height = totalHeight;
        }
    }

    /**
        パーサ。
        stringからParsedTreeを生成する。
		depthAssetListが発見されたら、DLを開始する。
     */
    public class HTMLParser {
		private InformationResourceLoader infoResLoader;

        public IEnumerator ParseRoot (string source, InformationResourceLoader infoResLoader, Action<ParsedTree> parsed) {
			this.infoResLoader = infoResLoader;

			var lines = source.Split('\n');
			for (var i = 0; i < lines.Length; i++) {
				lines[i] = lines[i].TrimStart();
			}

            var root = new ParsedTree();
            return Parse(root, string.Join(string.Empty, lines), parsed);
        }


        /**
			与えられたstringから情報を抜き出し、パーツの親子構造を規定する。
			ParsedTreeを返してくる。

			そのうち単一のArrayとしてindexのみで処理するように書き換えると楽。
		 */
        private IEnumerator Parse (ParsedTree parentTree, string data, Action<ParsedTree> parsed) {
			// Debug.LogError("data:" + data + " parentTree:" + parentTree.parsedTag);
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

					switch (foundTag) {
						// get depthAssetList from commented url.
						case (int)HtmlTag._COMMENT: {
							// <!--SOMETHING-->
							var endPos = -1;
							var contentStr = GetContentOfCommentTag(data, charIndex, out endPos);
							
							var cor = ParseAsComment(parentTree, contentStr);
							while (cor.MoveNext()) {
								yield return null;
							}
							
							charIndex = endPos;
							readPoint = charIndex;
							continue;
						}

						// ignore !SOMETHING tag.
						case (int)HtmlTag._IGNORED_EXCLAMATION_TAG: {
							charIndex = GetClosePointOfIgnoredTag(data, charIndex);
							readPoint = charIndex;
							continue;
						}


						case (int)HtmlTag._NO_TAG_FOUND: {
							// no tag found. go to next char.
							charIndex++;
							continue;
						}
						
						
						// html tag will be parsed without creating html tag.
						case (int)HtmlTag.HTML: {
							var endTagStartPos = GetStartPointOfCloseTag(data, charIndex, foundTag);

							// only content string should be parse.
							var contentStr = GetTagContent(data, charIndex, foundTag, endTagStartPos);

							var cor = Parse(parentTree, contentStr, parsedTree => {});
							while (cor.MoveNext()) {
								yield return null;
							}

							charIndex = endTagStartPos;
							readPoint = charIndex;
							continue;
						}

						// ignore these tags.
						case (int)HtmlTag.HEAD:
						case (int)HtmlTag.TITLE:{
							charIndex = GetClosePointOfTag(data, charIndex, foundTag);
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

					var rawTagName = GetTagFromIndex(foundTag);
					
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
							var kv = new AttributeKVs();
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
									
									kv = GetAttr(tag, attrStr);
									
									// tag closed point is tagEndIndex. next point is tagEndIndex + 1.
									tempCharIndex = startTagEndIndex + 1;
									tempReadPoint = tempCharIndex;

									// Debug.LogError("data[tempCharIndex]:" + data[tempCharIndex]);

									/*
										single close tag found.
										this tag content is just closed.
									 */
									if (data[startTagEndIndex - 1] == '/') {// <tag [attr]/>
										// Debug.LogError("-1 is / @tag:" + tag);

										// add content before tag.
										if (0 < readingPointLength) {
											var str = data.Substring(readingPointStartIndex, readingPointLength);
								
											// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

											if (!string.IsNullOrEmpty(str)) {
												var contentTagPoint = new ParsedTree(
													(int)HtmlTag._TEXT_CONTENT, 
													parentTree,
													new AttributeKVs(){
														{Attribute._CONTENT, str}
													}, 
													string.Empty
												);
												contentTagPoint.SetParent(parentTree);
											}
										}

										var tagPoint2 = new ParsedTree(
											tag, 
											parentTree,
											kv, 
											rawTagName
										);
										tagPoint2.SetParent(parentTree);

										charIndex = tempCharIndex;
										readPoint = tempReadPoint;
										continue;
									}

									// Debug.LogError("not closed tag:" + tag + " in data:" + data);

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();
									
									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);
									
									// Debug.LogError("endTagIndex:" + endTagIndex);

									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
										// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

										if (!string.IsNullOrEmpty(str)) {
											var contentTagPoint = new ParsedTree(
												(int)HtmlTag._TEXT_CONTENT, 
												parentTree,
												new AttributeKVs(){
													{Attribute._CONTENT, str}
												}, 
												string.Empty
											);
											contentTagPoint.SetParent(parentTree);
										}
									}

									var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
												
									var tagPoint = new ParsedTree(
										tag, 
										parentTree,
										kv, 
										rawTagName
									);
									tagPoint.SetParent(parentTree);
									
									// Debug.LogError("contents1:" + contents);
									var cor = Parse(tagPoint, contents, parsedTree => {});
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
									continue;;
								}
								case '>': {// <tag> start tag is closed.
									// set to next char.
									tempCharIndex = tempCharIndex + 1;

									// Debug.LogError("> found at tag:" + tag + " cont:" + data.Substring(tempCharIndex) + "___ finding end tag of tag:" + tag);

									/*
										finding end-tag of this tag.
									*/
									var endTag = "</" + rawTagName.ToLower() + ">";
									var cascadedStartTagHead = "<" + rawTagName.ToLower();

									var endTagIndex = FindEndTag(endTag, cascadedStartTagHead, data, tempCharIndex);

									// add content before tag.
									if (0 < readingPointLength) {
										var str = data.Substring(readingPointStartIndex, readingPointLength);
							
										// Debug.LogError("1 str:" + str + " parentTagPoint:" + parentTagPoint.tag + " current tag:" + foundTag);

										if (!string.IsNullOrEmpty(str)) {
											var contentTagPoint = new ParsedTree(
												(int)HtmlTag._TEXT_CONTENT, 
												parentTree,
												new AttributeKVs(){
													{Attribute._CONTENT, str}
												}, 
												string.Empty
											);
											contentTagPoint.SetParent(parentTree);
										}
									}

									var contents = data.Substring(tempCharIndex, endTagIndex - tempCharIndex);
									
									var tree = new ParsedTree(
										tag, 
										parentTree,
										kv, 
										rawTagName
									);
									tree.SetParent(parentTree);
									
									// Debug.LogError("contents2:" + contents);
									var cor = Parse(tree, contents, parsedTree => {});
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
									throw new Exception("parse error. unknown keyword found:" + data[charIndex] + " at tag:" + tag);
								}
							}
						}
					}
				}
				charIndex++;
            }

			if (readPoint < data.Length) { 
				var restStr = data.Substring(readPoint);
				// Debug.LogError("2 restStr:" + restStr + " parentTagPoint:" + parentTagPoint.tag);
				if (!string.IsNullOrEmpty(restStr)) {
					var contentTree = new ParsedTree(
						(int)HtmlTag._TEXT_CONTENT, 
						parentTree, 
						new AttributeKVs(){
							{Attribute._CONTENT, restStr}
						}, 
						string.Empty
					);
					contentTree.SetParent(parentTree);
				}
			}

            parsed(parentTree);
        }

		/**
			parse comment as specific parameters for Information feature.
			get depthAssetList url if exists.
		 */
		private IEnumerator ParseAsComment (ParsedTree parent, string data) {
			if (parent.parsedTag != (int)HtmlTag._ROOT) {
				// ignored.
				yield break;
			}

			// parse as params only root/comment tag with specific format.
			if (data.StartsWith(InformationConstSettings.DEPTH_ASSETLIST_URL_START) && data.EndsWith(InformationConstSettings.DEPTH_ASSETLIST_URL_END)) {
				var keywordLen = InformationConstSettings.DEPTH_ASSETLIST_URL_START.Length;
				var depthAssetListUrl = data.Substring(keywordLen, data.Length - keywordLen - InformationConstSettings.DEPTH_ASSETLIST_URL_END.Length);
				
				try {
					var uri = new Uri(depthAssetListUrl);
				} catch (Exception e) {
					throw new Exception("failed to get uri from depth asset list url. error:" + e);
				}
				
				/*
					start loading of depthAssetList.
				 */
				var cor = infoResLoader.LoadDepthAssetList(depthAssetListUrl);
				while (cor.MoveNext()) {
					yield return null;
				}

				Debug.LogWarning("_DEPTH_ASSET_LIST_INFO そのうち定義自体を消せそう。");
				// var depthAssetListUrlTree = new ParsedTree(
				// 	(int)HtmlTag._DEPTH_ASSET_LIST_INFO,
				// 	parent,
				// 	new AttributeKVs{
				// 		{Attribute.SRC, depthAssetListUrl}
				// 	},
				// 	HtmlTag._DEPTH_ASSET_LIST_INFO.ToString()
				// );
				// depthAssetListUrlTree.SetParent(parent);
			}
		}

		private string GetContentOfCommentTag (string data, int offset, out int tagEndPos) {
			var startPos = data.IndexOf("<!--", offset);
			if (startPos == -1) {
				throw new Exception("failed to parse comment tag. Information feature uses specific formatted comment tag.");
			}

			var commentContentStartPos = startPos + "<!--".Length;

			var commentEndPos = data.IndexOf("-->", commentContentStartPos);
			if (commentEndPos == -1) {
				throw new Exception("failed to find end of comment tag. Information feature uses specific formatted comment tag.");
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
			var foundTagStr = GetTagFromIndex(foundTagIndex);
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
				throw new Exception("failed to parse data. tag like <!~ is not closed properly.");
			}
			return nearestCloseTagIndex + 1;
		}

		/**
			<(startPoint)/SOMETHING>
			return startPoint.
		 */
		private int GetStartPointOfCloseTag (string data, int offset, int foundTagIndex) {
			var foundTagStr = GetTagFromIndex(foundTagIndex);
			var closeTagStr = "</"+ foundTagStr.ToLower() + ">";
			var nearestHeaderCloseTagIndex = data.IndexOf(closeTagStr, offset);
			if (nearestHeaderCloseTagIndex == -1) {
				throw new Exception("failed to parse data. tag '" + foundTagStr + "' is not closed properly.");
			}
			return nearestHeaderCloseTagIndex;
		}

		/**
			</SOMETHING>(closePoint)
			return closePoint.
		 */
		private int GetClosePointOfTag (string data, int offset, int foundTagIndex) {
			var foundTagStr = GetTagFromIndex(foundTagIndex);
			var closeTagStr = "</"+ foundTagStr.ToLower() + ">";
			return GetStartPointOfCloseTag(data, offset, foundTagIndex) + closeTagStr.Length;
		}

		
		private int FindEndTag (string endTagStr, string startTagStr, string data, int offset) {
			var cascadedStartTagIndexies = GetIndexiesOf(startTagStr, data, offset);
			var endTagCandidateIndexies = GetIndexiesOf(endTagStr, data, offset);

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
			
			throw new Exception("parse error. failed to find end tag:" + endTagStr + " after charIndex:" + offset);
		}
        
		private int[] GetIndexiesOf (string tagStr, string data, int offset) {
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
				Attribute keyEnum = Attribute._UNKNOWN;
				try {
					keyEnum = (Attribute)Enum.Parse(typeof(Attribute), keyStr, true);
				} catch (Exception e) {
					throw new Exception("at tag:" + GetTagFromIndex(tagIndex) + ", found attribute:" + keyStr + " is not supported yet, e:" + e);
				}
				
				var valStartIndex = eqIndex + 1;
				
				var delim = source[valStartIndex];
				var valEndIndex = source.IndexOf(delim, valStartIndex + 1);
				if (valEndIndex == -1) {
					// no delim end found.
					throw new Exception("attribute at tag:" + GetTagFromIndex(tagIndex) + " contains illigal description. source:" + originalAttrSource);
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

		private string GetTagFromIndex (int index) {
			if (index < (int)HtmlTag._END) {
				return ((HtmlTag)Enum.ToObject(typeof(HtmlTag), index)).ToString();
			}

			if (undefinedTagDict.ContainsValue(index)) {
				return undefinedTagDict.FirstOrDefault(x => x.Value == index).Key;
			}
			
			throw new Exception("failed to get tag from index. index:" + index);
		}

		private Dictionary<string, int> undefinedTagDict = new Dictionary<string, int>();

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
			var allowedMaxTagLength = InformationConstSettings.TAG_MAX_LEN;
			if (data.Length - tagStartPos < allowedMaxTagLength) {
				allowedMaxTagLength = data.Length - tagStartPos;
			}

			// get sampling str.
			var tagFindingSampleStr = data.Substring(tagStartPos, allowedMaxTagLength).ToUpper();
			
			if (tagStartPos < data.Length && data[tagStartPos] == '!') {
				if (data[index + 2] == '-') {
					return (int)HtmlTag._COMMENT;
				}

				// not comment.
				return (int)HtmlTag._IGNORED_EXCLAMATION_TAG;
			}

			var closeTagIndex = tagFindingSampleStr.IndexOfAny(new char[]{' ', '>'});
			if (closeTagIndex == -1) {
				return (int)HtmlTag._NO_TAG_FOUND; 
			}

			var tagCandidateStr = tagFindingSampleStr.Substring(0, closeTagIndex);
			
			try {
				// try-catchだと重いかもしれないので、なんか長さとかで足切りを考えるか。
				var tag = (HtmlTag)Enum.Parse(typeof(HtmlTag), tagCandidateStr);
				return (int)tag;
			} catch {
				// collect undefined tag.

				if (undefinedTagDict.ContainsKey(tagCandidateStr)) {
					return undefinedTagDict[tagCandidateStr];
				}

				var count = (int)HtmlTag._END + undefinedTagDict.Count + 1;
				undefinedTagDict[tagCandidateStr] = count;
				return count;
			}
		}
    }
}