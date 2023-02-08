using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AoP.Editor
{
    [CustomEditor(typeof(FileAnImporter))]
    public sealed class FileAnImporterEditor : ScriptedImporterEditor
    {

        private SerializedProperty fileAniPathProperty;
        private readonly GUIContent animationsInfoLabel = EditorGUIUtility.TrTextContent("Animations Info");
        private readonly GUIContent selectFileAniButton = EditorGUIUtility.TrTextContent(
            text: "Select .ani file...",
            tooltip: "Click on this button to select .ani file with information about animation clips.");

        private SerializedProperty eventFunctionNameProperty;
        private readonly GUIContent eventFunctionNameGUIContent = EditorGUIUtility.TrTextContent(
            text: "Event Function",
            tooltip: "The name of the function that will be called by an Animation Event.");

        public override void OnEnable()
        {
            base.OnEnable();

            fileAniPathProperty = serializedObject.FindProperty("fileAniPath");
            eventFunctionNameProperty = serializedObject.FindProperty("eventFunctionName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnInspectorAnimationsInfoGUI();
            EditorGUILayout.PropertyField(eventFunctionNameProperty, eventFunctionNameGUIContent);

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        private void OnInspectorAnimationsInfoGUI()
        {
            EditorGUILayout.LabelField(animationsInfoLabel, EditorStyles.boldLabel);
            string fileAniPath = fileAniPathProperty.stringValue;
            string fileName = Path.GetFileName(fileAniPath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "Not selected";
            }
            GUILayout.Label(EditorGUIUtility.TrTextContent(fileName, fileAniPath));
            if (GUILayout.Button(selectFileAniButton))
            {
                string destinationPath = fileAniPath;
                destinationPath = EditorUtility.OpenFilePanel("Select .ani file", destinationPath, "ani");
                if (!string.IsNullOrEmpty(destinationPath))
                {
                    destinationPath = FileUtil.GetProjectRelativePath(destinationPath);
                }
                fileAniPathProperty.stringValue = destinationPath;
            }
        }
    }
}
