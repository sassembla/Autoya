using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;


/*
// 全てのAssetを個別にグループにする。
 */
[CustomNode("Custom/Flatten", 1000)]
public class Flatten : Node {

	[SerializeField] private SerializableMultiTargetString m_myValue;

	public override string ActiveStyle {
		get {
			return "node 8 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 8";
		}
	}

	public override string Category {
		get {
			return "Custom";
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_myValue = new SerializableMultiTargetString();
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new Flatten();
		newNode.m_myValue = new SerializableMultiTargetString(m_myValue);
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("My Custom Node: Implement your own Inspector.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		//Show target configuration tab
		editor.DrawPlatformSelector(node);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
			// Draw Platform selector tab. 
			var disabledScope = editor.DrawOverrideTargetToggle(node, m_myValue.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Platform Settings", node, true)) {
					if(b) {
						m_myValue[editor.CurrentEditingGroup] = m_myValue.DefaultValue;
					} else {
						m_myValue.Remove(editor.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			// Draw tab contents
			using (disabledScope) {
				var val = m_myValue[editor.CurrentEditingGroup];

				var newValue = EditorGUILayout.TextField("My Value:", val);
				if (newValue != val) {
					using(new RecordUndoScope("My Value Changed", node, true)){
						m_myValue[editor.CurrentEditingGroup] = newValue;
						onValueChanged();
					}
				}
			}
		}
	}

	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
		// Pass incoming assets straight to Output
		if(Output != null) {
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();

			if(incoming != null) {
				var newAssetGroups = new Dictionary<string, List<AssetReference>>();

				foreach(var ag in incoming) {
					// ここで、ag.assetGroups には、グループとそこに含まれるAssetReferenceが入っている。
					// で、今回はflatにしたいので、グループ名を書き換える。
					foreach (var key in ag.assetGroups.Keys.ToArray()) {
						var assetReferences = ag.assetGroups[key];
						foreach (var assetReference in assetReferences) {
							newAssetGroups[assetReference.fileName] = new List<AssetReference>{assetReference};
						}
					}
				}

				Output(destination, newAssetGroups);
			} else {
				// Overwrite output with empty Dictionary when no there is incoming asset
				Output(destination, new Dictionary<string, List<AssetReference>>());
			}
		}
	}

	/**
	 * Build is called when Unity builds assets with AssetBundle Graph. 
	 */ 
	public override void Build (BuildTarget target, 
		Model.NodeData nodeData, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output outputFunc,
		Action<Model.NodeData, string, float> progressFunc)
	{
		// Do nothing 呼ばれるんだけどなんもせんでいいんだろうか。
	}
}
