﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity;
using UnityEngine.Events;
using UnityEditor;

public enum listFunction { showDetails, Form,CustomPlusDetails };

public interface I_ItemMenu
{
    void SetSelected(GameObject obj);
    GameObject GetSelected();
}

[System.Serializable]
public class OnSelectListEvent : UnityEvent<selectListItem>
{
}


public class selectListItem
{
    public FillFromSource fill;//reference to selected obj
    public string index;//index of selected item
}

// Custom serializable class
[Serializable]
public class ListProps
{
    public string label = "";
    public listFunction onSelect = listFunction.Form;
    public Color32 color = Color.grey;
    public UnityEvent ev;
    public OnSelectListEvent ev1;
    
    public DataSource data;
    public string field;
    //public string option;
    public listObjProps displayObj;
    public int fieldIdx;
}


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ListProps))]
public class ListDrawer : PropertyDrawer
{
    
    private float xOffset = 0;
    private float yHeight = 32;
    private float expandedHeight = 50;//extra space for event control +/- buttons
    // Draw the property inside the given rect
    int _choiceIndex;

    List<string> allFields = new List<string>();
    string currentData = "null";
    // Have we loaded the prefs yet
    private static bool prefsLoaded = false;

    // The Preferences
    public static bool boolPreference = false;

    // Add preferences section named "My Preferences" to the Preferences window
    [PreferenceItem("My Preferences")]
    public static void PreferencesGUI()
    {
        // Load the preferences
        if (!prefsLoaded)
        {
            boolPreference = EditorPrefs.GetBool("BoolPreferenceKey", false);
            prefsLoaded = true;
        }

        // Preferences GUI
        boolPreference = EditorGUILayout.Toggle("Bool Preference", boolPreference);

        // Save the preferences
        if (GUI.changed)
            EditorPrefs.SetBool("BoolPreferenceKey", boolPreference);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty p = property.FindPropertyRelative("onSelect");
        
        if (p.intValue == 2)//custom prop
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("ev1")) + expandedHeight ;
        }
        else
        {
            return yHeight;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        
        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
       // EditorGUIUtility.labelWidth = 14f;
       // position.width /= 2f;
        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        xOffset = position.x;
        // Calculate rects
        var labelRect = getRect(50,position);
        var amountRect = getRect(100, position);
        var unitRect = getRect(100, position);
        var colorRect = getRect(100, position);
        var nameRect = getRect(100, position);
        var acRect = getRect(100, position);

        DataSource dataProp = (DataSource)property.FindPropertyRelative("data").objectReferenceValue;
        if(dataProp != null && dataProp.name != currentData)
        {
            allFields = dataProp.getFieldSimple();
            currentData = dataProp.name;
        }
        else if(dataProp == null)
        {
            allFields = new List<string>();
            currentData = "mull";
        }
        
        //dataProp.GetType()

        SerializedProperty userIndexProperty = property.FindPropertyRelative("fieldIdx");
        SerializedProperty fieldSelection = property.FindPropertyRelative("field");

        GUIutil.doPrefixLabel(ref labelRect, "Label");
        EditorGUI.PropertyField(labelRect, property.FindPropertyRelative("label"), GUIContent.none);

        EditorGUI.BeginChangeCheck();
        GUIutil.doPrefixLabel(ref unitRect, "Field");
        _choiceIndex = EditorGUI.Popup(unitRect, userIndexProperty.intValue, allFields.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            userIndexProperty.intValue = _choiceIndex;
            fieldSelection.stringValue = allFields[_choiceIndex];
        }



        //when changing fields, field is first set to null.
        //keep a list of choices, when field is null, reset choices. if list is null, get choices.
        SerializedProperty p = property.FindPropertyRelative("onSelect");

        if (p.intValue == 2)//custom prop
        {
            var eventRect = new Rect(position.x, position.y + yHeight, position.width, position.height - yHeight);
            position.height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("ev1"));


            var lblRect = new Rect(position.x, position.y + yHeight, position.width, position.height - yHeight);
            EditorGUI.PrefixLabel(lblRect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("OnPress : "));

            eventRect.y += 16;
            EditorGUI.PropertyField(eventRect, property.FindPropertyRelative("ev1"), GUIContent.none);

        }


        if (p.intValue == 0 || p.intValue == 2)
        {
            GUIutil.doPrefixLabel(ref nameRect, "Display Object");
            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("displayObj"), GUIContent.none);
        }

        GUIutil.doPrefixLabel(ref amountRect, "Data Source");
        EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("data"), GUIContent.none);

        GUIutil.doPrefixLabel(ref colorRect, "DropDown Type");
        EditorGUI.PropertyField(colorRect, property.FindPropertyRelative("onSelect"), GUIContent.none);
        
        
        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public Rect getRect(int size, Rect position)
    {
        Rect newR = new Rect(xOffset, position.y, size, yHeight);
        xOffset += size;
        return newR;
    }
}
#endif

public class populateList : MonoBehaviour, I_ItemMenu
{

    public ListProps props;
    public GameObject layoutGroup; // where to place generated buttons
    public GameObject prefab;//button prefab
    public bool populateOnStart = true;
    public bool autoRefresh = false;
    public float refreshRate = 0.5f;//change to event
    public bool alwaysVisible = false;//for individual item - should infocard pop up, or should data be placed onto an always visible ui element.

    public string filterVar;//Show ONLY items where filterVar == filterValue
    public string filterValue;
    public bool showItemsNOTMatchingFilter = false;
    [TextArea(3, 10)]
    public string displayCode;

    [HideInInspector]
    public GameObject selectedItem;

    void Reset()
    {
        //props = new List<DropDownProps>();
        //props.Add(new DropDownProps());
        //props[0].label= "";
        //props[0].color = Color.grey;
        layoutGroup = gameObject;
        projectHandler.init();
        prefab = projectHandler.pData.defaultButton;
    }
    // Start is called before the first frame update
    void Start()
    {
        if(populateOnStart)
        {
            Populate();
        }
        if(autoRefresh)
        {
            InvokeRepeating("Refresh", refreshRate, refreshRate);
        }
        
    }

    private void OnEnable()
    {
        if(populateOnStart)
        {
            Populate();
        }
    }

    public void Populate()
    {
        if(!props.data.isDataReady())
        {
            return;
        }
        bool selected = false;

        Clear();

        List<string> keys = props.data.getFieldFromAllItems(props.data.primaryKey);

        foreach(string key in keys)
        {
            if (filterVar != "")
            {
                string value = props.data.getFieldFromItemID(key, filterVar);
                if((filterValue != value && !showItemsNOTMatchingFilter) || (showItemsNOTMatchingFilter && filterValue == value))
                {
                    continue;
                }
            }

            GameObject obj = Instantiate(prefab, layoutGroup.transform);
            obj.transform.parent = layoutGroup.transform;
            obj.name = key;
            FillFromSource fill = obj.GetComponent<FillFromSource>();
            fill.data = props.data;
            fill.index = key;
            displayObjDetails display = obj.GetComponent<displayObjDetails>();
            display.alwaysVisible = alwaysVisible;
            display.followCursor = !alwaysVisible;
            display.uiObject = props.displayObj;
            SelectableItem select = obj.GetComponent<SelectableItem>();

            
            if(select != null)
            {
                select.parentList = this;
            }
            foreach (DataFieldMap m in fill.FieldMapping)
            {
                m.data = props.data;
                SelectableItem item = obj.GetComponent<SelectableItem>();
                switch (props.onSelect)
                {
                    case listFunction.showDetails:

                        item.e_pointerDown.AddListener(delegate {; });
                        break;

                    case listFunction.CustomPlusDetails:

                        item.e_pointerDown.AddListener(delegate {; });

                        selectListItem s = new selectListItem();
                        s.fill = fill;
                        s.index = key;

                        item = obj.GetComponent<SelectableItem>();
                        item.e_pointerDown.AddListener(delegate { props.ev1.Invoke(s); });


                        break;

                }
               
            }
            fill.fill(displayCode);
        }

    }

    public void Refresh()
    {
        Invoke("Populate",refreshRate);
    }

    public void setData(DataSource d)
    {
        props.data = d;
        Clear();
        Invoke("Populate", 0.2f);
    }

    public void Clear()
    {
        GUIutil.clearChildren(layoutGroup.transform);
        //props.Clear();
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSelected(GameObject obj)
    {
        selectedItem = obj;
    }

    public GameObject GetSelected()
    {
        return selectedItem;
    }
}