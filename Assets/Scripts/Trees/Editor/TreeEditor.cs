using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreeGenerator))]
public class TreeEditor : Editor {

    TreeGenerator tree;
    Editor shapeEditor;
    Editor colorEditor;

    public override void OnInspectorGUI ( ) {

        using (var check = new EditorGUI.ChangeCheckScope()) {
            base.OnInspectorGUI();

            if (check.changed) {
                this.tree.GenerateTree();
            }
        }

        if (GUILayout.Button("Generate Tree")) {
            this.tree.GenerateTree();
        }

        this.DrawSettingsEditor(this.tree.treeSettings, this.tree.OnTreeSettingsUpdated, ref this.tree.treeSettingsFoldout, ref this.shapeEditor);
    }

    private void DrawSettingsEditor ( Object _settings, System.Action _onSettingsUpdated, ref bool _foldout, ref Editor _editor ) {

        if (_settings != null) {
            _foldout = EditorGUILayout.InspectorTitlebar(_foldout, _settings);

            using (var check = new EditorGUI.ChangeCheckScope()) {

                if (_foldout) {
                    CreateCachedEditor(_settings, null, ref _editor);
                    _editor.OnInspectorGUI();

                    if (check.changed) {
                        if (_onSettingsUpdated != null) {
                            _onSettingsUpdated();
                        }
                    }
                }
            }
        }
    }

    private void OnEnable ( ) {
        this.tree = (TreeGenerator) this.target;
    }
}
