﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class TectonicsEditor : Editor {

    Planet planet;
    Editor planetEditor;

    public override void OnInspectorGUI ( ) {

        using (var check = new EditorGUI.ChangeCheckScope()) {
            base.OnInspectorGUI();

            if (check.changed)
            {
                this.planet.GeneratePlanet();
            }
        }

        if (GUILayout.Button("Generate Tectonics Planet")) {
            this.planet.GeneratePlanet();
        }

        this.DrawSettingsEditor(this.planet.planetSettings, this.planet.OnPlanetSettingsUpdated, ref this.planet.planetSettingsFoldout, ref this.planetEditor);
        //this.DrawSettingsEditor(this.planet.colorSettings, this.planet.OnColorSettingsUpdated, ref this.planet.colorSettingsFoldout, ref this.colorEditor);
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
        this.planet = (Planet) this.target;
    }
}
