// using Miyamasu;
// using UnityEngine;
// using System.Collections.Generic;
// using System;
// using System.Linq;
// using UnityEngine.UI;
// using UnityEngine.Events;
// using AutoyaFramework.Information;
// using System.Collections;
// using AutoyaFramework;

// /**
// 	test for customizer.
//  */
// public class MaterializeMachineTests : MiyamasuTestRunner {
//     private HTMLParser parser;

//     private GameObject canvas;

    
//     GameObject rootObj;
//     UUebView view;

//     UUebViewCore core;

//     private void ShowLayoutRecursive (TagTree tree) {
//         Debug.Log("tree:" + core.resLoader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
//         foreach (var child in tree.GetChildren()) {
//             ShowLayoutRecursive(child);
//         }
//     }


// 	[MSetup] public void Setup () {

// 		// GetTexture(url) runs only Play mode.
// 		if (!IsTestRunningInPlayingMode()) {
// 			SkipCurrentTest("Information feature should run on MainThread.");
// 		};

//         RunOnMainThread(
//             () => {
//                 rootObj = new GameObject();
//                 var rectTrans = rootObj.AddComponent<RectTransform>();
//                 rectTrans.anchorMin = new Vector2(0,1);
//                 rectTrans.anchorMax = new Vector2(0,1);
//                 rectTrans.pivot = new Vector2(0,1);

//                 view = rootObj.AddComponent<UUebView>();
//                 core = new UUebViewCore(view);
                
//                 var canvas = GameObject.Find("Canvas/MaterializeTestPlace");
//                 rootObj.transform.SetParent(canvas.transform, false);

//                 rectTrans.anchoredPosition = new Vector2(100 * index, 0);
//                 index++;
//             }
//         );

        
//         parser = new HTMLParser(core.resLoader);
// 	}

//     private TagTree CreateLayoutedTree (string sampleHtml, float width=100) {
//         ParsedTree parsedRoot = null;
//         var cor = parser.ParseRoot(
//             sampleHtml, 
//             parsed => {
//                 parsedRoot = parsed;
//             }
//         );
        
//         RunOnMainThread(() => view.Internal_CoroutineExecutor(cor));
        
//         WaitUntil(
//             () => parsedRoot != null, 1, "too late."
//         );
        
//         if (parsedRoot.errors.Any()) {
//             foreach (var error in parsedRoot.errors) {
//                 Debug.LogError("error:" + error.code + " reason:" + error.reason);
//             }
//             throw new Exception("failed to create layouted tree.");
//         }

//         TagTree layouted = null;
//         var layoutMachine = new LayoutMachine(core.resLoader);

//         var cor2 = layoutMachine.Layout(
//             parsedRoot, 
//             new Vector2(width,100),
//             layoutedTree => {
//                 layouted = layoutedTree;
//             }
//         );

//         RunOnMainThread(() => view.Internal_CoroutineExecutor(cor2));

//         WaitUntil(
//             () => layouted != null, 5, "layout timeout."
//         );

//         return layouted;
//     }

//     private int index;
//     private void Show (TagTree tree) {
//         var materializeMachine = new MaterializeMachine(core.resLoader);

//         var materializeDone = false;
//         RunOnMainThread(
//             () => {
//                 var cor = materializeMachine.Materialize(rootObj, core, tree, 0, () => {
//                     materializeDone = true;
//                 });
//                 view.Internal_CoroutineExecutor(cor);
//             }
//         );
        
//         WaitUntil(
//             () => materializeDone && !view.IsLoading(), 5, "slow materialize. materializeDone:" + materializeDone + " view.IsLoading():" + view.IsLoading()
//         );
//     }

//     [MTest] public void MaterializeHTML () {
//         var sample = @"
// <body>something</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLHasValidView () {
//         var sample = @"
// <body>something</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithSmallTextHasValidView () {
//         var sample = @"
// <body>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithLink () {
//         var sample = @"
// <body><a href='https://dummyimage.com/100.png/09f/fff'>link!</a></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithLinkWithId () {
//         var sample = @"
// <body><a href='https://dummyimage.com/100.png/09f/fff' id='linkId'>link!</a></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithImageAsButton () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100.png/09f/fff' button='true''/></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithImageAsButtonWithId () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithImageAsButtonWithIdMakeChanges () {
//         Debug.LogWarning("保留。");
//         return;
//         var sample = @"
// <body>
// <p listen='imageId' hidden='true'>something</p>
// <img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/>
// </body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithDoubleBoxedLayer () {
//         var sample = @"
// <!--depth asset list url(resources://Views/MyInfoView/DepthAssetList)-->
// <textbox>
//     <p>fmmm???</p>
//     <updatetext>something.</updatetext>
//     <updatetext>omake!</updatetext>
// </textbox>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithSmallImage () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>text</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndSmallText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10.png/09f/fff'/>over 100px string should be multi lined text with good separation. need some length.</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/97x10/000/fff'/>something</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }
//     [MTest] public void MaterializeHTMLWithTextAndWideImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }


//     [MTest] public void MaterializeHTMLWithTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithTextAndWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithWideImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/100x10/000/fff'/>else<img src='https://dummyimage.com/100x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }


//     [MTest] public void MaterializeHTMLWithTextAndSmallImage () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/></body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }


//     [MTest] public void MaterializeHTMLWithTextAndSmallImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>b!</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithTextAndSmallImageAndTextAndWideImageAndText () {
//         var sample = @"
// <body>something<img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/100x10/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithSmallImageAndTextAndSmallImageAndText () {
//         var sample = @"
// <body><img src='https://dummyimage.com/10x10/000/fff'/>else<img src='https://dummyimage.com/10x20/000/fff'/>other</body>";
//         var tree = CreateLayoutedTree(sample);
        
//         Show(tree);
//     }


//     [MTest] public void LoadHTMLWithCustomTagLink () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithCustomTag () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag><custombg><textbg><customtext>something</customtext></textbg></custombg></customtag>
// else

// </body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithCustomTagSmallText () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag><custombg><textbg><customtext>
// something you need is not time, money, but do things fast.</customtext></textbg></custombg></customtag>
// else
// </body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithCustomTagLargeText () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag><custombg><textbg><customtext>
// Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
// </customtext></textbg></custombg></customtag>
// else
// <customimg src='https://dummyimage.com/10x20/000/fff'/>
// </body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MultipleBoxConstraints () {
        
//         var sample = @"
// <!--depth asset list url(resources://Views/MultipleBoxConstraints/DepthAssetList)-->
// <itemlayout>
// <topleft>
//     <img src='https://dummyimage.com/100.png/09f/fff'/>
// </topleft>
// <topright>
//     <img src='https://dummyimage.com/100.png/08f/fff'/>
// </topright>
// <content><p>something! need more lines for test. get wild and tough is really good song. really really good song. forever. long lives get wild and tough!</p></content>
// <bottom>
//     <img src='https://dummyimage.com/100.png/07f/fff'/>
// </bottom>
// </itemlayout>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithCustomTagMultiple () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag><custombg><textbg><customtext>something1</customtext></textbg></custombg></customtag>
// <customtag><custombg><textbg><customtext>something2</customtext></textbg></custombg></customtag>
// else
// </body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithCustomTagMultipleByInnerContent () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <body>
// <customtag>
//     <custombg><textbg><customtext>something1</customtext></textbg></custombg>
//     <custombg><textbg><customtext>something2</customtext></textbg></custombg>
// </customtag>
// else
// </body>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void LayoutHTMLWithCustomTagMultipleByInnerContentWithParentLayer () {
//         var sample = @"
// <!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
// <customtag>
//     <custombg><textbg><customtext>something1</customtext></textbg></custombg>
//     <custombg><textbg><customtext>something2</customtext></textbg></custombg>
// </customtag>";
//         var tree = CreateLayoutedTree(sample);

//         Show(tree);
//     }

//     [MTest] public void MaterializeHTMLWithDoubleBoxedLayerNeverOverLayout () {
//         var sample = @"
// <!--depth asset list url(resources://Views/MyInfoView/DepthAssetList)-->
// <body>
//     <bg>
//     	<textbg>
//     		<textbox>
// 	    		<p>koko ni nihongo ga iikanji ni hairu. <br> 2line content! 2line content! 2line content!2 line content! a good thing.<a href='somewhere'>link</a>a long text will make large window. something like this.</p>
// 	    		<updatetext>omake! abc d</updatetext>
//                 <p>ef ghijklm</p>
//                 <updatetext>aaaaaaaaaaaaabcdefghijk</updatetext>
// 	    	</textbox>
// 	    </textbg>
//     </bg>
// </body>";
//         var tree = CreateLayoutedTree(sample, 300);

//         Show(tree);
//     }

//     [MTest] public void MaterializeSampleView2_HiddenBreakView () {
//         var sampleHtml = @"
// <!--depth asset list url(resources://Views/MyInfoView/DepthAssetList)-->
// <body>
//     <bg>
//     	<titlebox>
//     		<titletext>レモン一個ぶんのビタミンC</titletext>
//     	</titlebox>
//     	<newbadge></newbadge>
//     	<textbg>
//     		<textbox>
// 	    		<updatetext>koko ni nihongo ga iikanji ni hairu. good thing. long text will make large window. like this.</updatetext>
// 	    		<!-- hiddenがあると、コンテンツが出ないみたいなのがある。連続するのがいけないのかな。 -->
// 	    		<updatetext hidden='true' listen='readmore'>omake!<img src='https://dummyimage.com/100.png/07f/fff'/></updatetext>
//                 <img src='https://dummyimage.com/100.png/09f/fff' button='true' id='readmore'/>
// 	    	</textbox>
// 	    </textbg>
//     </bg>
// </body>";
//         var tree = CreateLayoutedTree(sampleHtml, 300);

//         Show(tree);
//     }

//     [MTest] public void LayoutSampleView2_HiddenBreakView () {
//         var sampleHtml = @"
// <!--depth asset list url(resources://Views/MyInfoView/DepthAssetList)-->
// <body>
//     <bg>
//     	<textbg>
//     		<textbox>
// 	    		<updatetext>koko ni nihongo ga iikanji ni hairu. good thing. long text will make large window. like this.</updatetext>
// 	    		<updatetext hidden='true' listen='readmore'>omake!</updatetext>
// 	    	</textbox>
// 	    </textbg>
//     </bg>
// </body>";
//         var tree = CreateLayoutedTree(sampleHtml, 300);
//         Show(tree);
//     }

//     [MTest] public void PSupport () {
//         var sampleHtml = @"
// <p>
//     p1<a href=''>a1</a>p2
// </p>";
//         var tree = CreateLayoutedTree(sampleHtml);
//         Show(tree);
//     }

//     [MTest] public void PSupport2 () {
//         Debug.LogWarning("保留");
//         return;
//         var sampleHtml = @"
// <p>
//     p1<a href=''>a1</a>p2
// </p><p>
//     p3
// </p>";
//         var tree = CreateLayoutedTree(sampleHtml);
//         Show(tree);
//     }
// }