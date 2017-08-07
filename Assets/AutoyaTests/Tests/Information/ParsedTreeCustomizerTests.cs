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
        Debug.Log("parsedTag:" + loader.GetTagFromIndex(tree.parsedTag) + " type:" + tree.treeType);
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

        loader = new InformationResourceLoader(Autoya.Mainthread_Commit, null, null);
        parser = new HTMLParser(loader);
	}

    [MTest] public void ParseDefaultTag () {
        var sampleHtml = @"
<body>something</body>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
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

    [MTest] public void WithCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithCustomTag/DepthAssetList)-->
<customtag><customtext>something</customtext></customtag>
<p>else</p>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 6, "not match. contentsCount:" + contentsCount);

        // カスタマイズタグを変形させて中身を伸長する
        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        // 階層が増えてるはず
        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(contentsCount < newContentsCount, "actual:" + newContentsCount);

        // 増えてる階層に関してのチェックを行う customtagで+1, customtextで+1。
        Assert(contentsCount +2 == newContentsCount, "actual:" + newContentsCount);
    }

    [MTest] public void WithDeepCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithDeepCustomTag/DepthAssetList)-->
<customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
<p>else</p>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 6, "not match. contentsCount:" + contentsCount);

        // カスタマイズタグを変形させて中身を伸長する
        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        // 階層が増えてるはず
        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(contentsCount < newContentsCount, "less. newContentsCount:" + newContentsCount);

        // 増えてる階層に関してのチェックを行う。1種のcustomTagがあるので1つ増える。
        Assert(contentsCount +1 == newContentsCount, "not match. newContentsCount:" + newContentsCount);

        // ShowRecursive(customizedTree, loader);
    }

    [MTest] public void WithDeepCustomTagBoxHasBoxAttr () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithDeepCustomTag/DepthAssetList)-->
<customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
<p>else</p>
        ";

        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 6, "not match. contentsCount:" + contentsCount);

        // カスタマイズタグを変形させて中身を伸長する
        customizer = new ParsedTreeCustomizer(loader);
        var customizedTree = customizer.Customize(parsedRoot);

        // 階層が増えてるはず
        var newContentsCount = CountContentsRecursive(customizedTree);
        Assert(contentsCount < newContentsCount, "less. newContentsCount:" + newContentsCount);

        // 増えてる階層に関してのチェックを行う。1種のcustomTagがあるので1つ増える。
        Assert(contentsCount +1 == newContentsCount, "not match. newContentsCount:" + newContentsCount);

        foreach (var s in customizedTree.GetChildren()[0].GetChildren()) {
            Assert(s.treeType == TreeType.CustomBox, "not match, s.treeType:" + s.treeType);
            Assert(s.keyValueStore.ContainsKey(AutoyaFramework.Information.Attribute._BOX), "box does not have pos kv.");
        }

        // ShowRecursive(customizedTree, loader);
    }
    
}