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
    test for html parser.
 */
public class HTMLParserTests : MiyamasuTestRunner {
    private HTMLParser parser;

    private ResourceLoader loader;

    private UUebView executor;

    [MSetup] public void Setup () {

        // GetTexture(url) runs only Play mode.
        if (!IsTestRunningInPlayingMode()) {
            SkipCurrentTest("Information feature should run on MainThread.");
        };

        
        RunOnMainThread(
            () => {
                executor = new GameObject("htmlParserTest").AddComponent<UUebView>();
            }
        );
        
        loader = new ResourceLoader(null, null);
        parser = new HTMLParser(loader);
    }

    public static void ShowRecursive (TagTree tree, ResourceLoader loader) {
        Debug.Log("parsedTag:" + loader.GetTagFromValue(tree.tagValue) + " type:" + tree.treeType);
        foreach (var child in tree.GetChildren()) {
            ShowRecursive(child, loader);
        }
    }

    private int CountContentsRecursive (TagTree tree) {
        var children = tree.GetChildren();
        var count = 0;
        foreach (var child in children) {
            count += CountContentsRecursive(child);
        }
        return count + 1;// add this content count.
    }

    [MTest] public void LoadSimpleHTML () {
        var sampleHtml = @"
<body>something</body>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        
        var children = parsedRoot.GetChildren();

        Assert(children.Count == 1, "not match. children.Count:" + children.Count);
    }

    [MTest] public void LoadDepthAssetListIsDone () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<body>something</body>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(!loader.IsLoadingDepthAssetList, "still loading.");
    }

    [MTest] public void LoadDepthAssetListWithCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<customtag><customtagtext>something</customtagtext></customtag>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(!loader.IsLoadingDepthAssetList, "still loading.");
    }


    // 解析した階層が想定通りかどうか

    [MTest] public void ParseSimpleHTML () {
        var sampleHtml = @"
<body>something</body>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );
        
        var children = parsedRoot.GetChildren();

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].tagValue == (int)HTMLTag.body, "not match.");
        
    }

    [MTest] public void ParseCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<customtag><customtagtext>something</customtagtext></customtag>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        // loader contains 3 additional custom tags.
        var count = loader.GetAdditionalTagCount();
        Assert(count == 3, "not match. count:" + count);

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].tagValue == 33, "not match. actual:" + parsedRoot.GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].GetChildren()[0].tagValue == ((int)HTMLTag._END) + 3, "not match +3. actual:" + parsedRoot.GetChildren()[0].GetChildren()[0].tagValue);
    }

    [MTest] public void ParseCustomTagMoreDeep () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<customtag><customtagtext>
    <customtag2><customtagtext2>something</customtagtext2></customtag2>
</customtagtext></customtag>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        // loader contains 4 additional custom tags.
        var count = loader.GetAdditionalTagCount();
        Assert(count == 6, "not match. count:" + count);

        Assert(parsedRoot.GetChildren().Count == 1, "not match a.");
        Assert(parsedRoot.GetChildren()[0].tagValue == 33, "not match b. actual:" + parsedRoot.GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].GetChildren()[0].tagValue == 38, "not match c. actual:" + parsedRoot.GetChildren()[0].GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].GetChildren()[0].GetChildren()[0].tagValue == 34, "not match d. actual:" + parsedRoot.GetChildren()[0].GetChildren()[0].GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].tagValue == 35, "not match e. actual:" + parsedRoot.GetChildren()[0].GetChildren()[0].GetChildren()[0].GetChildren()[0].tagValue);
    }


    [MTest] public void ParseCustomTagRecursive () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTest/DepthAssetList)-->
<customtag><customtagtext>
    something<customtag><customtagtext>else</customtagtext></customtag>other
</customtagtext></customtag>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        // loader contains 2 additional custom tags.
        var count = loader.GetAdditionalTagCount();
        Assert(count == 3, "not match. count:" + count);

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].tagValue == 33, "not match a. actual:" + parsedRoot.GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].GetChildren()[0].tagValue == ((int)HTMLTag._END) + 3, "not match b. actual:" + parsedRoot.GetChildren()[0].GetChildren()[0].tagValue);
    }

    [MTest] public void ParseImageAsImgContent () {
        var sampleHtml = @"
<img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' />";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].tagValue == (int)HTMLTag.img, "not match 1. actual:" + parsedRoot.GetChildren()[0].tagValue);
        Assert(parsedRoot.GetChildren()[0].treeType == TreeType.Content_Img, "not match.");
    }

    [MTest] public void ParseCustomImgAsImgContent () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTestImgView/DepthAssetList)-->
<myimg src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' />";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].treeType == TreeType.Content_Img, "not match. expected:" + TreeType.Content_Img + " actual:" + parsedRoot.GetChildren()[0].treeType);
    }

    [MTest] public void ParseCustomTextAsTextContent () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTestTextView/DepthAssetList)-->
<mytext>text</mytext>";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(parsedRoot.GetChildren().Count == 1, "not match.");
        Assert(parsedRoot.GetChildren()[0].treeType == TreeType.Container, "not match. expected:" + TreeType.Container + " actual:" + parsedRoot.GetChildren()[0].treeType);
    }

    [MTest] public void ParserTestCustomLayerAndCustomContentCombination () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/ParserTestCombination/DepthAssetList)-->
<customtag><customtagtext><customtext>text</customtext></customtagtext></customtag>
<customtext>text</customtext>";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(parsedRoot.GetChildren().Count == 2, "not match.");
        Assert(parsedRoot.GetChildren()[0].treeType == TreeType.CustomLayer, "not match. expected:" + TreeType.CustomLayer + " actual:" + parsedRoot.GetChildren()[0].treeType);
        Assert(parsedRoot.GetChildren()[1].treeType == TreeType.Container, "not match. expected:" + TreeType.Container + " actual:" + parsedRoot.GetChildren()[0].treeType);
    }
    
    [MTest] public void Revert () {
        var sampleHtml = @"
<body>something</body>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
        });
        RunOnMainThread(() => executor.CoroutineExecutor(cor));
        
        WaitUntil(
            () => parsedRoot != null, 1, "too late."
        );

        {
            var bodyContainer = parsedRoot.GetChildren()[0];
            
            var textChildren = bodyContainer.GetChildren();

            Assert(textChildren.Count == 1, "not match a. actual:" + textChildren.Count);
            
            var textChildrenTree = textChildren[0];
            var textPart = textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string;
            var frontHalf = textPart.Substring(0,4);
            var backHalf = textPart.Substring(4);

            textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] = frontHalf;

            var insertionTree = new InsertedTree(textChildrenTree, backHalf, textChildrenTree.tagValue);
            insertionTree.SetParent(bodyContainer);

            // 増えてるはず
            Assert(bodyContainer.GetChildren().Count == 2, "not match b. actual:" + bodyContainer.GetChildren().Count);
        }

        TagTree.RevertInsertedTree(parsedRoot);

        {
            var bodyContainer = parsedRoot.GetChildren()[0];
            
            var textChildren = bodyContainer.GetChildren();
            var textChildrenTree = textChildren[0];

            Assert(textChildren.Count == 1, "not match c. actual:" + textChildren.Count);
            Assert(textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string == "something", "actual:" + textChildrenTree.keyValueStore[HTMLAttribute._CONTENT] as string);
        }
    }

    [MTest] public void ParseDefaultTag () {
        var sampleHtml = @"
<body>something</body>
        ";

        TagTree parsedRoot = null;
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

        var newContentsCount = CountContentsRecursive(parsedRoot);
        Assert(newContentsCount == 3, "not match. newContentsCount:" + newContentsCount);
    }

    [MTest] public void WithCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithCustomTag/DepthAssetList)-->
<customtag><customtext>something</customtext></customtag>
<p>else</p>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        Assert(contentsCount == 7, "not match. contentsCount:" + contentsCount);
    }

    [MTest] public void WithDeepCustomTag () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithDeepCustomTag/DepthAssetList)-->
<customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
<p>else</p>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
        var contentsCount = CountContentsRecursive(parsedRoot);
        
        // 増えてる階層に関してのチェックを行う。1種のcustomTagがあるので1つ増える。
        Assert(contentsCount == 7, "not match. contentsCount:" + contentsCount);

        // ShowRecursive(customizedTree, loader);
    }

    [MTest] public void WithDeepCustomTagBoxHasBoxAttr () {
        var sampleHtml = @"
<!--depth asset list url(resources://Views/WithDeepCustomTag/DepthAssetList)-->
<customtag><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /><img src='https://github.com/sassembla/Autoya/blob/master/doc/scr.png?raw=true2' /></customtag>
<p>else</p>
        ";

        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );
        
       foreach (var s in parsedRoot.GetChildren()[0].GetChildren()) {
            Assert(s.treeType == TreeType.CustomBox, "not match, s.treeType:" + s.treeType);
            Assert(s.keyValueStore.ContainsKey(AutoyaFramework.Information.HTMLAttribute._BOX), "box does not have pos kv.");
        }

        // ShowRecursive(customizedTree, loader);
    }

    [MTest] public void Order () {
        var sampleHtml = @"
<body>something1.<img src='https://dummyimage.com/100.png/09f/fff'/></body>";
        TagTree parsedRoot = null;
        var cor = parser.ParseRoot(sampleHtml, parsed => {
            parsedRoot = parsed;
            // ShowRecursive(parsedRoot, loader);
        });
        RunEnumeratorOnMainThread(cor);
        
        WaitUntil(
            () => parsedRoot != null, 5, "too late."
        );

        Assert(
            parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType == TreeType.Content_Text, "not match, type:" + parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[0]/*text of body*/.treeType
        );

        Assert(
            parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType == TreeType.Content_Img, "not match, type:" + parsedRoot/*root*/.GetChildren()[0]/*body*/.GetChildren()[1]/*img*/.treeType
        );

    }
}