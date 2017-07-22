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

    [MTest] public void LayoutHTML () {
        var sample = @"
<body>something</body>
        ";
        var tree = CreateCustomizedTree(sample);

        LayoutedTree layouted = null;
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

    [MTest] public void LayoutHTMLWithCustomTag () {
        var sample = @"
<!--depth asset list url(resources://Views/LayoutHTMLWithCustomTag/DepthAssetList)-->
<customtag>something</customtag>
<p>else</p>
        ";
        var tree = CreateCustomizedTree(sample);

        LayoutedTree layouted = null;
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