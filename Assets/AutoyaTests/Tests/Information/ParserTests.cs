// using Miyamasu;
// using MarkdownSharp;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine.UI;
// using UnityEngine.Events;
// using AutoyaFramework.Information;
// using System.Collections;

// /**
// 	test for markdown based view.
//  */
// public class ParserTests : MiyamasuTestRunner {
//     private HTMLParser parser;

// 	[MSetup] public void Setup () {

// 		// GetTexture(url) runs only Play mode.
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Information feature should run on MainThread.");
// 		};

//         parser = new HTMLParser();
// 	}

//     [MTest] public void ParseSimpleHTML () {
//         var sampleHtml = @"
// <body>something</body>
//         ";

//         var parsedRoot = parser.ParseRoot(sampleHtml);
//         var children = parsedRoot.GetChildlen();

//         Assert(children.Count == 1, "not match. children.Count:" + children.Count);
//     }

//     // 速度の向上とかはこの辺で計測できればいいや。
// }