using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{

    public bool autoUpdate = true;

    [HideInInspector]
    public bool treeSettingsFoldout = true;

    public TreeSettings treeSettings;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateTree () {

    }

    public void OnTreeSettingsUpdated ( ) {
        if (this.autoUpdate) {
        }
    }
}
