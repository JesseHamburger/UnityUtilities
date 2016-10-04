using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(SomeClass))]
public class SomeClassEditor : Editor {

    private ReorderableList whateverList;


    private void OnEnable() {

        //DrawReorderabltBarKitList();
        whateverList = CreateList(serializedObject, serializedObject.FindProperty("barDisplayKits"), "Bar Display Kits", "numberOfBarsInKit", 5);

    }

    public override void OnInspectorGUI() {
        serializedObject.Update(); // Needed for free good editor functionality

        whateverList.DoLayoutList();

        serializedObject.ApplyModifiedProperties(); // Needed for zero-hassle good editor functionality
    }



    // Originally from MALQUA
    // https://feedback.unity3d.com/suggestions/custom-element-size-in-reorderable-list
    // http://i.imgur.com/fIbBorr.gifv
    // and GitHub user Socapex
    // https://gist.github.com/Socapex/1d9b45507464681d530b
    // Modified by Jesse Hamburger

    /// <summary>
    /// Creates a reorderable list for a property in the inspector for this type where each element expands when selected.
    /// </summary>
    /// <param name="obj">The Serialized Object for which to return a reorderable list. Required by ReorderableList constructor.</param>
    /// <param name="prop">The Serialized Property that this list will read/write user input data from/to.</param>
    /// <param name="label">The header label for this list.</param>
    /// <param name="elementLabelPropertyName">The property name to use as a label for each element in the list.</param>
    /// <param name="expandedLines">Number of lines of the element when expanded. Wish this could be handled automatically, but I can't figure out how to make it do so.</param>
    /// <returns>null</returns>
    ReorderableList CreateList(
        SerializedObject obj,
        SerializedProperty prop,
        string label,
        string elementLabelPropertyName,
        int expandedLines
    ) {
        ReorderableList list = new ReorderableList(obj, prop, true, true, true, true);

        list.drawHeaderCallback = rect => {
            EditorGUI.LabelField(rect, label);
        };

        // Initialize a temporary list of element heights to be used later on in the draw function
        List<float> heights = new List<float>(prop.arraySize);

        // Main draw callback for the reorderable list
        list.drawElementCallback = ( rect, index, active, focused ) => {
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            //            Sprite s = (element.objectReferenceValue as GameObject);

            // Manage the height of this element
            bool foldout = active;
            float height = EditorGUIUtility.singleLineHeight * 1.25f; // multiply by 1.25 to give each property a little breathing room
            if ( foldout ) {
                height = EditorGUIUtility.singleLineHeight * expandedLines + 2; // +2 is to give each element a bit of padding on the bottom
            }

            // Manage heights of each element
            /// TODO: heights should really based on the GetPropertyHeight of property type, rather
            /// than some random function parameter that we input, but I can't get GetPropertyHeight
            /// to be properly here... at least for custom property drawers.
            try {
                heights[index] = height;
            } catch ( ArgumentOutOfRangeException e ) {
                Debug.LogWarning(e.Message);
            } finally {
                float[] floats = heights.ToArray();
                Array.Resize(ref floats, prop.arraySize);
                heights = floats.ToList();
            }

            // Add a bit of padding to the top of each element
            rect.y += 2;

            // If we have our element selected, show our property
            if ( foldout ) {
                EditorGUI.PropertyField(rect, element);
            } else {
                string valueType = element.FindPropertyRelative(elementLabelPropertyName).type;
                string stringValue = "";
                /// TODO: This should really be split out into a separate parsing function that
                /// returns the appropriate value, but for my purposes this is fine. Will do later!
                if ( valueType == "string" ) {
                    stringValue = element.FindPropertyRelative(elementLabelPropertyName).stringValue;
                } else if ( valueType == "int" ) {
                    stringValue = element.FindPropertyRelative(elementLabelPropertyName).intValue.ToString();
                }
                string closedLabel = element.FindPropertyRelative(elementLabelPropertyName).displayName + "" + stringValue;
                EditorGUI.LabelField(rect, closedLabel);
            }
        };

        // Adjust heights based on whether or not an element is selected.
        list.elementHeightCallback = ( index ) => {
            Repaint();
            float height = 0;

            try {
                height = heights[index];
            } catch ( ArgumentOutOfRangeException e ) {
                Debug.LogWarning(e.Message);
            } finally {
                float[] floats = heights.ToArray();
                Array.Resize(ref floats, prop.arraySize);
                heights = floats.ToList();
            }

            return height;
        };

        list.drawElementBackgroundCallback = ( rect, index, active, focused ) => {
            rect.height = heights[index];
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.1f, 0.33f, 1f, 0.33f));
            tex.Apply();
            if ( active )
                GUI.DrawTexture(rect, tex as Texture);
        };

/* 
/// Uncomment this section if you want a little dropdown list for the 
/// "add element to list" button.
        list.onAddDropdownCallback = ( rect, li ) => {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Element"), false, () => {
                serializedObject.Update();
                li.serializedProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            });

            menu.ShowAsContext();

            float[] floats = heights.ToArray();
            Array.Resize(ref floats, prop.arraySize);
            heights = floats.ToList();
        };
*/
        return list;
    }

}