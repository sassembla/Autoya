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
public class LayoutMachineTests : MiyamasuTestRunner {
    private HTMLParser parser;

    private InformationResourceLoader loader;
    private ParsedTreeCustomizer customizer;

    private void ShowLayoutRecursive (ParsedTree tree) {
        Debug.Log("tree:" + tree.prefabName + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren()) {
            ShowLayoutRecursive(child);
        }
    }

	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};

        parser = new HTMLParser();
        loader = new InformationResourceLoader(Autoya.Mainthread_Commit, null, null);
	}

    private ParsedTree CreateCustomizedTree (string sampleHtml) {
        ParsedTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, loader, parsed => {
            parsedRoot = parsed;
        });

        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        
        customizer = new ParsedTreeCustomizer(loader);
        return customizer.Customize(parsedRoot);
    }

//     [MTest] public void LayoutHTML () {
//         var sample = @"
// <body>something</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLHasValidView () {
//         var sample = @"
// <body>something</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithSmallTextHasValidView () {
//         var sample = @"
// <body>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithSmallImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/></body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>text</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithSmallImageAndSmallText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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

//     [MTest] public void LayoutHTMLWithWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/97x10/000/fff'/>something</body>";
//         var tree = CreateCustomizedTree(sample);
        
        

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
//     [MTest] public void LayoutHTMLWithTextAndWideImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/></body>";
//         var tree = CreateCustomizedTree(sample);

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


//     [MTest] public void LayoutHTMLWithTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else</body>";
//         var tree = CreateCustomizedTree(sample);
        
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

//     [MTest] public void LayoutHTMLWithTextAndWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateCustomizedTree(sample);
        
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

//     [MTest] public void LayoutHTMLWithWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateCustomizedTree(sample);
        
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


//     [MTest] public void LayoutHTMLWithTextAndSmallImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/></body>";
//         var tree = CreateCustomizedTree(sample);

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


//     [MTest] public void LayoutHTMLWithTextAndSmallImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</body>";
//         var tree = CreateCustomizedTree(sample);
        
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

//     [MTest] public void LayoutHTMLWithTextAndSmallImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</body>";
//         var tree = CreateCustomizedTree(sample);
        
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

//     [MTest] public void LayoutHTMLWithSmallImageAndTextAndSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</body>";
//         var tree = CreateCustomizedTree(sample);
        
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
//         var tree = CreateCustomizedTree(sample);

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

    [MTest] public void LayoutHTMLWithCustomTag () {
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
<body>
<customtag>something</customtag>
else
</body>
        ";
        var tree = CreateCustomizedTree(sample);

        ParsedTree layouted = null;
        var layoutMachine = new LayoutMachine(
            tree, 
            loader, 
            new ViewBox(100,100,0), 
            Autoya.Mainthread_Commit, 
            layoutedTree => {
                layouted = layoutedTree;
            }
        );

        WaitUntil(
            () => layouted != null, 5, "timeout."
        );

        ParsedTreeCustomizerTests.ShowRecursive(layouted, loader);
    }

    
}