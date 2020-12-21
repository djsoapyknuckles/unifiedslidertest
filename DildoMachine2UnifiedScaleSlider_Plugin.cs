using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using System.IO;

public class DildoMachine2UnifiedScaleSlider : MVRScript
{
    // My JSONStorables
    public JSONStorableBool animOn_Off;
    public JSONStorableFloat SCALEsliderFloat;
    public JSONStorableFloat SCALEdefaultdildo;
    public JSONStorableFloat SCALEchanceunflareddildo;
    public JSONStorableFloat SCALErexdildo;
    public JSONStorableFloat SCALEtuckerdildo;
    public JSONStorableFloat ROTATIONsliderFloat;
    public JSONStorableFloat ANIMSPEEDsliderFloat;
    public JSONStorableStringChooser dildoChoice;

    // My misc variables to find and edit the prefab made in Unity
    private string objectNameSearchString = "dildoMachine";
    public Transform ObjectRoot;
    private Transform machineTR;
    private Transform defaultDildo;
    private Transform chanceUnflaredDildo;
    private Transform rexDildo;
    private Transform tuckerDildo;
    private Transform[] dildoArray;
    private Animator _animator;
    private Collider[] colliderArray;
    private Transform[] children;
    private Vector3[] _connectedAnchor;
    private Vector3[] _anchor;
    private Vector3 newScale;

    //UI variables
    protected UIDynamicSlider SCALEslider;
    protected UIDynamicSlider ROTATIONslider;
    protected UIDynamicPopup CHOOSERudp;

    // Security and initialization
    private bool editorInitialized = false;
    private int initLoops = 0;
    private int maxInitLoops = 3600;
    private Transform selectedDildo = null;
    public override void Init()
    {
        try
        {
            // We only authorize the script to be used by a CUA
           if (containingAtom.type != "CustomUnityAsset")
                {
                   SuperController.LogError("Dildo Machine Controller - Please add this script to dildoMachine Custom Unity Asset ( script disabled ).");
                    return;
                }
                //**************UI CODE GOES HERE*********************
                //animation toggle
                animOn_Off = new JSONStorableBool("animation on/off", false, animOn_OffCallback);
                animOn_Off.storeType = JSONStorableParam.StoreType.Full;
                CreateToggle(animOn_Off, false);
                RegisterBool(animOn_Off);

                // JSONStorableFloat Rotation Slider
                 ROTATIONsliderFloat = new JSONStorableFloat("Rotation", 0f, RotationFloatCallback, 0f, 90f, true);
                 ROTATIONsliderFloat.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(ROTATIONsliderFloat);
                 ROTATIONslider = CreateSlider(ROTATIONsliderFloat, false);

                 // JSONStorableFloat Animation Speed Slider
                 ANIMSPEEDsliderFloat = new JSONStorableFloat("Animation Speed", 1f, AnimSpeedFloatCallback, 0.5f, 5f, true);
                 ANIMSPEEDsliderFloat.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(ANIMSPEEDsliderFloat);
                 ROTATIONslider = CreateSlider(ANIMSPEEDsliderFloat, false);

                 //JSONStorableChooser
                 List<string> choices = new List<string>();
                 //choices.Add("None");
                 choices.Add("Default");
                 choices.Add("Chance Unflared");
                 choices.Add("Rex");
                 choices.Add("Tucker");
                 //choices.Add("Choice3");
                 dildoChoice = new JSONStorableStringChooser("Chooser", choices, "Default", "Choose Dildo", DildoChooserCallback);
                 dildoChoice.storeType = JSONStorableParam.StoreType.Full;
                 RegisterStringChooser(dildoChoice);
                 UIDynamicPopup udp = CreatePopup(dildoChoice, false);
                 //CreateScrollablePopup(jchooser, true);
                 udp.labelWidth = 100f; 

                //Register SCALE StorableFloats for each dildo
                 SCALEdefaultdildo = new JSONStorableFloat("scaledefaultdildo", 1.0f, IntermediateScaleHolder, 0.5f, 1.5f, true);
                 SCALEdefaultdildo.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(SCALEdefaultdildo);

                 SCALEchanceunflareddildo = new JSONStorableFloat("scalechanceunflareddildo", 1.0f, IntermediateScaleHolder, 0.5f, 1.5f, true);
                 SCALEchanceunflareddildo.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(SCALEchanceunflareddildo);

                 SCALErexdildo = new JSONStorableFloat("scalerexddildo", 1.0f, IntermediateScaleHolder, 0.5f, 1.5f, true);
                 SCALErexdildo.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(SCALErexdildo);

                 SCALEtuckerdildo = new JSONStorableFloat("scaletuckerdildo", 1.0f, IntermediateScaleHolder, 0.5f, 1.5f, true);
                 SCALEtuckerdildo.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(SCALEtuckerdildo); 

                 //JSONStorableFloat Scale Slider
                 SCALEsliderFloat = new JSONStorableFloat("Scale Selected Dildo", 1.0f, IntermediateScaleHolder, 0.5f, 1.5f, true); 
                 SCALEsliderFloat.storeType = JSONStorableParam.StoreType.Full;
                 RegisterFloat(SCALEsliderFloat);
                 SCALEslider = CreateSlider(SCALEsliderFloat, false);
            

                // help
                JSONStorableString helpText = new JSONStorableString("Help",
                    "<b>Dildo Machine Controller</b> lets you control customizable attributes of the Dildo Machine 2 asset.\n\n" +
                    "Change the options on the left to configure the selected attachment \n\n" + "Save your scene with this plugin loaded if you want your configuration values restored when loading saved scene \n\n" +
                    "Reload the plugin to restore the <i>default</i> values");
                UIDynamic helpTextfield = CreateTextField(helpText, true);
                helpTextfield.height = 1100.0f;
            
        }
        catch (Exception e)
        {
            SuperController.LogError("Dildo Machine Controller: Exception caught: " + e);
        }
    }

    // Used for the initialization of the editor.
    // When it is initialized, there's nothing done every frame
    void Update()
    {
        // If the editor is not initialized, we're gonna try to do it
        // I'm putting 3600 tries ( at 60fps it is 1min )
        if (initLoops < maxInitLoops && editorInitialized == false)
        {
            initLoops++;
            InitEditor();
        }

        // Triggering an error if we have reached our limit of tries
        if (initLoops == maxInitLoops)
        {
            SuperController.LogError("Dildo Machine Controller: This CUA could not be initialized properly after 3600 tries. Please select a compatible object and reload the script.");
            initLoops++; // To avoid spamming the log with the error
        }
    }

    private bool InitEditor()
    {

        if (editorInitialized == true) return true;


        ObjectRoot = getObjectRoot(containingAtom.reParentObject);
        if (ObjectRoot == null)
        {
            return false;
        }

        // Find first
        machineTR = ObjectRoot.Find("mechanical");

        defaultDildo = ObjectRoot.Find("dildo");

        chanceUnflaredDildo = ObjectRoot.Find("chanceUnflared_37k_rigged");

        rexDildo = ObjectRoot.Find("rex_10k_rigged");

        tuckerDildo = ObjectRoot.Find("tucker_23k_rigged");

        dildoArray = new Transform[] { defaultDildo, chanceUnflaredDildo, rexDildo, tuckerDildo };

        // found my transforms, now the rest
        if (machineTR && defaultDildo && chanceUnflaredDildo && rexDildo && tuckerDildo)
        {
            // Finding Animator component
            _animator = machineTR.GetComponentInChildren<Animator>();


            // Found Animator...
            if (_animator)
            {
                // Initialized
                editorInitialized = true;

                // Initialize script based on the information in my JSONStorables

                //these are the values stored on save
                animOn_OffCallback(animOn_Off.val);
                // SuperController.LogMessage("anim_on_OffCallback called from update InitEditor");
                RotationFloatCallback(ROTATIONsliderFloat);
                // SuperController.LogMessage("RotationFloatCallback called from update InitEditor");
                 AnimSpeedFloatCallback(ANIMSPEEDsliderFloat);
                // SuperController.LogMessage("AnimSpeedFloatCallback called from update InitEditor");

                IntermediateScaleHolder(SCALEsliderFloat);
               
                DildoChooserCallback(dildoChoice.val);



                SuperController.LogMessage("Dildo Machine Controller: script initialized properly");
                return true;
            }
            else
            {
                SuperController.LogError("Dildo Machine Controller : transform found, but does not have Animator attached.");
                initLoops = maxInitLoops; // We don't have what we need, we consider the initialization failed
                return false;
            }
        }
        else
        {
            SuperController.LogError("Dildo Machine Controller : missing mandatory transform object");
            initLoops = maxInitLoops; // We don't have what we need, we consider the initialization failed
            return false;
        }
    }


    /*
     * *************Callbacks*********************************
    */
    protected void animOn_OffCallback(bool on_off)
    {

        if (machineTR)
        {
            if (on_off)
            {
                _animator.enabled = true;
            }
            else
            {
                _animator.enabled = false;
            }

        }

    }

    protected void RotationFloatCallback(JSONStorableFloat rotation)
    {
        if (machineTR)
        {
            //SuperController.LogMessage("Float param " + jf.name + " set to " + jf.val);
            machineTR.transform.localEulerAngles = new Vector3(rotation.val, 0.0f, 0.0f);
        }
    }
    protected void AnimSpeedFloatCallback(JSONStorableFloat animSpeed)
    {
        if (machineTR)
        {

            //SuperController.LogMessage("Float param " + animSpeed.name + " set to " + animSpeed.val);
            _animator.speed = animSpeed.val;
        }
    }


    protected void DildoChooserCallback(string choice)
    {

     if (machineTR != null)
     {
        if (choice == "Default")
        { selectedDildo = defaultDildo;
          //SCALEslider.slider.value = 1.5f;
          SCALEslider.slider.value = SCALEdefaultdildo.val;
          ScaleFloatCallback(SCALEdefaultdildo);
         // ScaleFloatCallback(SCALEdefaultdildo);
        }
        else
        if (choice == "Chance Unflared")
        { selectedDildo = chanceUnflaredDildo;
            SCALEslider.slider.value = SCALEchanceunflareddildo.val;
            ScaleFloatCallback(SCALEchanceunflareddildo);
            // ScaleFloatCallback(SCALEchanceunflareddildo);
        }
        else
        if (choice == "Rex")
        { selectedDildo = rexDildo;
            SCALEslider.slider.value = SCALErexdildo.val;
            ScaleFloatCallback(SCALErexdildo);
         // ScaleFloatCallback(SCALErexdildo);
        }
        else
        if (choice == "Tucker")
        { selectedDildo = tuckerDildo;
            SCALEslider.slider.value = SCALEtuckerdildo.val;
            ScaleFloatCallback(SCALEtuckerdildo);
         // ScaleFloatCallback(SCALEtuckerdildo);
        }

        //set scale for selected dildo

        for (int i = 0; i < dildoArray.Length; i++)
        {
            if (selectedDildo != null)
            {
                if (dildoArray[i] == selectedDildo)
                {
                    EnableMesh(selectedDildo);
                    DoTurnCollidersforChildrenON(selectedDildo);
                }
                else
                {
                    DisableMesh(dildoArray[i]);
                    DoTurnCollidersforChildrenOFF(dildoArray[i]);
                }
            }
        }
     }
    }
    protected void ScaleFloatCallback(JSONStorableFloat scale)
    {
       if (machineTR != null)
       {
        for (int i = 0; i < dildoArray.Length; i++)
        {
            if (selectedDildo != null)
            {
                if (dildoArray[i] == selectedDildo)
                {
                    DoGetConnectedAnchorandAnchorVals(selectedDildo);
                    DoTurnCollidersforChildrenOFF(selectedDildo);
                    DoScaleTransform(selectedDildo, scale.val);
                    DoApplyConnectedAnchorandAnchorVals(selectedDildo);
                    DoTurnCollidersforChildrenON(selectedDildo);
                }
            }

        }
       }
    }

    /*
    *********************UTILITY METHODS**********************
    */

private void IntermediateScaleHolder(JSONStorableFloat s)
    {
     if (machineTR != null)
    {
        if (selectedDildo == defaultDildo)
        {
           SCALEdefaultdildo.val = s.val;
        }
        else if (selectedDildo == chanceUnflaredDildo)
        {
            SCALEchanceunflareddildo.val = s.val;
        }
        else if (selectedDildo == rexDildo )
        {
            SCALErexdildo.val = s.val;
        }
        else if (selectedDildo == tuckerDildo)
        {
           SCALEtuckerdildo.val = s.val;
        }
        ScaleFloatCallback(s);
     }
    }

    private Transform getObjectRoot(Transform parent)
    // Recursive function searching for a transform containing the string set in objectNameSearchString
    // It is used the first time for the initialization of the script
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(objectNameSearchString))
            {
                return child;
            }
            else
            {
                Transform childSearch = getObjectRoot(child);
                if (childSearch != null) return childSearch;
            }
        }

        return null;
    }

    protected void DoScaleTransform(Transform t, float s)
    {
        newScale = new Vector3(s, s, s);
        t.transform.localScale = newScale;
    }

    protected void DoGetConnectedAnchorandAnchorVals(Transform t)
    {
        children = t.transform.GetComponentsInChildren<Transform>();
        _connectedAnchor = new Vector3[children.Length];
        _anchor = new Vector3[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].GetComponent<CharacterJoint>() != null)
            {
                _connectedAnchor[i] = children[i].GetComponent<CharacterJoint>().connectedAnchor;
                _anchor[i] = children[i].GetComponent<CharacterJoint>().anchor;
            }
        }
    }
    protected void DoApplyConnectedAnchorandAnchorVals(Transform t)
    {
        children = t.transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].GetComponent<CharacterJoint>() != null)
            {
                children[i].GetComponent<CharacterJoint>().connectedAnchor = _connectedAnchor[i];
                children[i].GetComponent<CharacterJoint>().anchor = _anchor[i];
            }
        }
    }
    protected void EnableMesh(Transform t)
    {
        Renderer Mesh = t.GetComponentInChildren<Renderer>();
        Mesh.enabled = true;
    }

    protected void DisableMesh(Transform t)
    {
        Renderer Mesh = t.GetComponentInChildren<Renderer>();
        Mesh.enabled = false;
    }
    protected void DoTurnCollidersforChildrenON(Transform t)
    {
        colliderArray = t.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliderArray.Length; i++)
        {
            if (colliderArray[i].GetComponent<Collider>() != null)
            {
                colliderArray[i].GetComponent<Collider>().enabled = true;
            }
        }
    }

    protected void DoTurnCollidersforChildrenOFF(Transform t)
    {
        colliderArray = t.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliderArray.Length; i++)
        {
            if (colliderArray[i].GetComponent<Collider>() != null)
            {
                colliderArray[i].GetComponent<Collider>().enabled = false;
            }
        }
    }
}



