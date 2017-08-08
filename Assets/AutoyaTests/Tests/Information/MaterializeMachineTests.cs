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
public class MaterializeMachineTests : MiyamasuTestRunner {
    private HTMLParser parser;

    private InformationResourceLoader loader;

    private ViewBox viewBox;
    private GameObject canvas;

    private void ShowLayoutRecursive (ParsedTree tree) {
        Debug.Log("tree:" + loader.GetTagFromIndex(tree.parsedTag) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren()) {
            ShowLayoutRecursive(child);
        }
    }

	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};

        loader = new InformationResourceLoader(Autoya.Mainthread_Commit, null, null);
        parser = new HTMLParser(loader);
        viewBox = new ViewBox(100,100,0);

        RunOnMainThread(
            () => {
                canvas = GameObject.Find("Canvas/MaterializeTestPlace");
            }
        );
	}

    private ParsedTree CreateLayoutedTree (string sampleHtml) {
        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });

        Autoya.Mainthread_Commit(cor);
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        

        ParsedTree layouted = null;
        var layoutMachine = new LayoutMachine(
            loader, 
            viewBox
        );

        var cor2 = layoutMachine.Layout(
            parsedRoot, 
            layoutedTree => {
                layouted = layoutedTree;
            }
        );

        Autoya.Mainthread_Commit(cor2);

        WaitUntil(
            () => layouted != null, 5, "timeout."
        );

        return layouted;
    }

    private int index;
    private void Show (ParsedTree tree) {
        var materializer = new MaterializeMachine(loader);
        
        GameObject rootObj = null;
        RectTransform rectTrans = null;

        // このへんでreloadManagerみたいなのを考える必要が出てくる。
        // 現在はまだ適当。
        RunOnMainThread(
            () => {
                rootObj = new GameObject();
                rootObj.transform.SetParent(canvas.transform, false);
            }
        );

        var done = false;
        var cor = materializer.Materialize(rootObj, tree, 0, progress => {}, () => {
            done = true;
        });

        
        Autoya.Mainthread_Commit(cor);
        
        WaitUntil(
            () => done, 5, "not yet."
        );
        
        RunOnMainThread(
            () => {
                rectTrans = rootObj.GetComponent<RectTransform>();
                
                // move to indexed pos.
                rectTrans.anchoredPosition += new Vector2(100 * index, 0);
                index++;
            }
        );
    }

    [MTest] public void MaterializeHTML () {
        var sample = @"
<body>something</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLHasValidView () {
        var sample = @"
<body>something</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithSmallTextHasValidView () {
        var sample = @"
<body>over 100px string should be multi lined text with good separation. need some length.</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithImage () {
        var sample = @"
<body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithSmallImage () {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithSmallImageAndText () {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>text</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithSmallImageAndSmallText () {
        var sample = @"
<body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithWideImageAndText () {
        var sample = @"
<body><img src='https://dummyimage.com/97x10/000/fff'/>something</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }
    [MTest] public void MaterializeHTMLWithTextAndWideImage () {
        var sample = @"
<body>something<img src='https://dummyimage.com/100x10/000/fff'/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }


    [MTest] public void MaterializeHTMLWithTextAndWideImageAndText () {
        var sample = @"
<body>something<img src='https://dummyimage.com/100x10/000/fff'/>else</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithTextAndWideImageAndTextAndWideImageAndText () {
        var sample = @"
<body>something<img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithWideImageAndTextAndWideImageAndText () {
        var sample = @"
<body><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }


    [MTest] public void MaterializeHTMLWithTextAndSmallImage () {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/></body>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }


    [MTest] public void MaterializeHTMLWithTextAndSmallImageAndText () {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithTextAndSmallImageAndTextAndWideImageAndText () {
        var sample = @"
<body>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithSmallImageAndTextAndSmallImageAndText () {
        var sample = @"
<body><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }


    [MTest] public void LoadHTMLWithCustomTagLink () {
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithCustomTag () {
        /*
            ・そもそもレイヤーにboxが存在する場合、指定の位置に出す。
            ・レイヤーにboxが存在していてその存在を無視した別のtagが来た場合、エラー。
            ・レイヤーにboxが存在しない場合、左上から出す。

         */
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
<body>
<customtag><custombg><textbg><customtext>something</customtext></textbg></custombg></customtag>
else
</body>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithCustomTagSmallText () {
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
<body>
<customtag><custombg><textbg><customtext>
something you need is not time, money, but do things fast.</customtext></textbg></custombg></customtag>
else
</body>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithCustomTagLargeText () {
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
<body>
<customtag><custombg><textbg><customtext>
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
</customtext></textbg></custombg></customtag>
else
</body>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }

    
}