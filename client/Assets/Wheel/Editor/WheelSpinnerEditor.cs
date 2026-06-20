using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WheelSpinner))]
public class WheelSpinnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Wheel Visuals"))
        {
            ((WheelSpinner)target).UpdateWheelVisuals();
        }
    }
}
