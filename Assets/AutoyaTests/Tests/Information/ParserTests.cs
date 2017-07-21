using Miyamasu;
using MarkdownSharp;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using AutoyaFramework.Information;
using System.Collections;
using AutoyaFramework;

/**
	test for markdown based view.
 */
public class ParserTests : MiyamasuTestRunner {
    private HTMLParser parser;

    private InformationResourceLoader loader;

	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};

        parser = new HTMLParser();
        loader = new InformationResourceLoader(Autoya.Mainthread_Commit, null, null);
	}

    [MTest] public void ParseSimpleHTML () {
        var sampleHtml = @"
<body>something</body>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });
        Autoya.Mainthread_Commit(cor);
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        
        var children = parsedRoot.GetChildlen();

        Assert(children.Count == 1, "not match. children.Count:" + children.Count);
    }

    [MTest] public void LoadDepthAssetListIsDone () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<body>something</body>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });
        Autoya.Mainthread_Commit(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(!loader.IsLoadingDepthAssetList, "still loading.");
    }    
}