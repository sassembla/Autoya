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
    private ParsedTreeCustomizer customizer;

    private ViewBox viewBox;
    private GameObject viewGameObj;

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
        
        customizer = new ParsedTreeCustomizer(loader);
        var tree = customizer.Customize(parsedRoot);

        ParsedTree layouted = null;
        var layoutMachine = new LayoutMachine(
            loader, 
            viewBox
        );

        var cor2 = layoutMachine.Layout(
            tree, 
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

    [MTest] public void MaterializeHTML () {
        var sample = @"
<body>something</body>";
        var tree = CreateLayoutedTree(sample);
        
        var materializer = new MaterializeMachine(loader);
        
        GameObject rootObj = null;

        // このへんでreloadManagerみたいなのを考える必要が出てくる。
        // 現在はまだ適当。
        RunOnMainThread(
            () => {
                rootObj = new GameObject();
                var canvas = GameObject.Find("Canvas");
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
    }

//     [MTest] public void MaterializeHTMLHasValidView () {
//         var sample = @"
// <body>something</body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );

//     }

//     [MTest] public void MaterializeHTMLWithSmallTextHasValidView () {
//         var sample = @"
// <body>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 112, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
        
//     }

//     [MTest] public void MaterializeHTMLWithImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 100, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithSmallImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 10, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );

//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>text</body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndSmallText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 112, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/97x10/000/fff'/>something</body>";
//         var tree = CreateLayoutedTree(sample);
        
        

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 26, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }
//     [MTest] public void MaterializeHTMLWithTextAndWideImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }


//     [MTest] public void MaterializeHTMLWithTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16+16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithTextAndWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16+16+16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 10+16+16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }


//     [MTest] public void MaterializeHTMLWithTextAndSmallImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);

//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }


//     [MTest] public void MaterializeHTMLWithTextAndSmallImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithTextAndSmallImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 16+16+16, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndTextAndSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         var done = false;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 ShowLayoutRecursive(layoutedTree);
//                 done = true;
//                 Assert(layoutedTree.viewHeight == 20, "not match.");
//             }
//         );

//         WaitUntil(
//             () => done, 5, "timeout."
//         );
//     }


//     [MTest] public void LoadHTMLWithCustomTagLink () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
//         ";
//         var tree = CreateLayoutedTree(sample);

//         ParsedTree layouted = null;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 layouted = layoutedTree;
//             }
//         );

//         WaitUntil(
//             () => layouted != null, 5, "timeout."
//         );
//     }

//     [MTest] public void MaterializeHTMLWithCustomTag () {
//         /*
//             textbgの上にカスタムタグが乗ってほしいが、そういうの想定してなくて辛い。
//             boxが存在しないレイヤーにコンテンツを足すにはどうすればいいか、とかその辺。
//             ・そもそもレイヤーにboxが存在する場合、指定の位置に出す。
//             ・レイヤーにboxが存在していて無視したtagが来た場合、エラー。
//             ・レイヤーにboxが存在しない場合、左上から出す。

//             とかか。
//          */
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag><textbg><customtext>something</customtext></textbg></customtag>
// else
// </body>
//         ";
//         var tree = CreateLayoutedTree(sample);

//         ParsedTree layouted = null;
//         var layoutMachine = new LayoutMachine(
//             tree, 
//             loader, 
//             new ViewBox(100,100,0), 
//             Autoya.Mainthread_Commit, 
//             layoutedTree => {
//                 layouted = layoutedTree;
//             }
//         );

//         WaitUntil(
//             () => layouted != null, 5, "timeout."
//         );

//         ParsedTreeCustomizerTests.ShowRecursive(layouted, loader);
//     }

    
}