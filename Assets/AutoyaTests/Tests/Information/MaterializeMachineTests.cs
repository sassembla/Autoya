using Miyamasu;
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

    private ResourceLoader loader;

    private GameObject canvas;

    
    GameObject rootObj;
    UUebView executor;
    private void ShowLayoutRecursive (TagTree tree) {
        Debug.Log("tree:" + loader.GetTagFromValue(tree.tagValue) + " offsetX:" + tree.offsetX + " offsetY:" + tree.offsetY + " width:" + tree.viewWidth + " height:" + tree.viewHeight);
        foreach (var child in tree.GetChildren()) {
            ShowLayoutRecursive(child);
        }
    }


	[MSetup] public void Setup () {

		// GetTexture(url) runs only Play mode.
		if (!IsTestRunningInPlayingMode()) {
			SkipCurrentTest("Information feature should run on MainThread.");
		};

        RunOnMainThread(
            () => {
                rootObj = new GameObject();
                executor = rootObj.AddComponent<UUebView>();
                
                canvas = GameObject.Find("Canvas/MaterializeTestPlace");
                loader = new ResourceLoader(executor.CoroutineExecutor);
            }
        );

        
        parser = new HTMLParser(loader);
	}

    private TagTree CreateLayoutedTree (string sampleHtml) {
        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(
            sampleHtml, 
            parsed => {
                parsedRoot = parsed;
            }
        );

        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        

        TagTree layouted = null;
        var layoutMachine = new LayoutMachine(
            loader
        );

        var cor2 = layoutMachine.Layout(
            parsedRoot, 
            new Vector2(100,100),
            layoutedTree => {
                layouted = layoutedTree;
            }
        );

        RunOnMainThread(() => executor.CoroutineExecutor(cor2));

        WaitUntil(
            () => layouted != null, 5, "timeout."
        );

        return layouted;
    }

    private int index;
    private void Show (TagTree tree) {
        var materializeMachine = new MaterializeMachine(loader);

        RectTransform rectTrans = null;

        // このへんでreloadみたいなのを考える必要が出てくる。
        // 現在はまだ適当。
        RunOnMainThread(
            () => {
                rootObj.transform.SetParent(canvas.transform, false);
                rectTrans = rootObj.AddComponent<RectTransform>();
            }
        );

        var done = false;
        
        RunOnMainThread(
            () => {
                var cor = materializeMachine.Materialize(rootObj, new UUebViewCore(rootObj.GetComponent<UUebView>()), tree, 0, () => {
                    done = true;
                });
                executor.CoroutineExecutor(cor);
            }
        );
        
        WaitUntil(
            () => done, 5, "not yet."
        );
        
        RunOnMainThread(
            () => {
                // move to indexed pos.
                rectTrans.anchoredPosition = new Vector2(100 * index, 100);
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

    [MTest] public void MaterializeHTMLWithLink () {
        var sample = @"
<body><a href='https://dummyimage.com/100.png/09f/fff'>link!</a></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithLinkWithId () {
        var sample = @"
<body><a href='https://dummyimage.com/100.png/09f/fff' id='linkId'>link!</a></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithImage () {
        var sample = @"
<body><img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithImageAsButton () {
        var sample = @"
<body><img src='https://dummyimage.com/100.png/09f/fff' button='true''/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithImageAsButtonWithId () {
        var sample = @"
<body><img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/></body>";
        var tree = CreateLayoutedTree(sample);
        
        Show(tree);
    }

    [MTest] public void MaterializeHTMLWithImageAsButtonWithIdMakeChanges () {
        var sample = @"
<body>
<p listen='imageId' hidden='true'>something</p>
<img src='https://dummyimage.com/100.png/09f/fff' button='true' id='imageId'/>
</body>";
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
<customimg src='https://dummyimage.com/10x20/000/fff'/>
</body>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }

    [MTest] public void MultipleBoxConstraints () {
        var sample = @"
<!--depth asset list url(resources://Views/MultipleBoxConstraints/DepthAssetList)-->
<itemlayout>
<topleft>
    <img src='https://dummyimage.com/100.png/09f/fff'/>
</topleft>
<topright>
    <img src='https://dummyimage.com/100.png/08f/fff'/>
</topright>
<content><p>something! need more lines for test. get wild and tough is really good song. really really good song. forever. long lives get wild and tough!</p></content>
<bottom>
    <img src='https://dummyimage.com/100.png/07f/fff'/>
</bottom>
</itemlayout>";
        var tree = CreateLayoutedTree(sample);

        Show(tree);
    }
}