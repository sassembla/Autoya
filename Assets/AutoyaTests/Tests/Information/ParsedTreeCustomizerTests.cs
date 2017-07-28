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
	test for customizer.
 */
public class ParsedTreeCustomizerTests : MiyamasuTestRunner {
    private HTMLParser parser;

    private InformationResourceLoader loader;
    private ParsedTreeCustomizer customizer;

    public static void ShowRecursive (ParsedTree tree, InformationResourceLoader loader) {
        Debug.Log("parsedTag:" + loader.GetTagFromIndex(tree.parsedTag));
        foreach (var child in tree.GetChildren()) {
            ShowRecursive(child, loader);
        }
    }

    private int CountContentsRecursive (ParsedTree tree) {
        var children = tree.GetChildren();
        var count = 0;
        foreach (var child in children) {
            count += CountContentsRecursive(child);
        }
        return count + 1;// add this content count.
    }

	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};

        parser = new HTMLParser();
        loader = new InformationResourceLoader(Autoya.Mainthread_Commit, null, null);
	}

    [MTest] public void ParseDefaultTag () {
        var sampleHtml = @"
<body>something</body>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        
        // parsedRootを与えて、custimizedRootを返してくる
        // treeの内容が変わらないはず
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 3/*root + body + content*/, "not match. contentsCount:" + contentsCount);

        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(newContentsCount == 3, "not match. newContentsCount:" + newContentsCount);
    }

    [MTest] public void WithCustamTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithCustamTag/DepthAssetList)-->
<customtag>something</customtag>
<p>else</p>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 5, "not match.");

        // カスタマイズタグを変形させて中身を伸長する
        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        // 階層が増えてるはず
        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(contentsCount < newContentsCount, "actual:" + newContentsCount);

        // 増えてる階層に関してのチェックを行う
        Assert(contentsCount +1 == newContentsCount, "actual:" + newContentsCount);
    }

    [MTest] public void WithDeepCustamTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithDeepCustamTag/DepthAssetList)-->
<customtag>something<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
<p>else</p>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 7, "not match. contentsCount:" + contentsCount);

        // カスタマイズタグを変形させて中身を伸長する
        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        // 階層が増えてるはず
        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(contentsCount < newContentsCount, "less. newContentsCount:" + newContentsCount);

        // 増えてる階層に関してのチェックを行う。2種のcustomTagがあるので2つ増える。
        Assert(contentsCount +2 == newContentsCount, "not match. newContentsCount:" + newContentsCount);

        ShowRecursive(customizedTree, loader);
    }
    
}