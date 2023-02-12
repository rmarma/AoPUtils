using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AoP.Editor
{
    [CustomEditor(typeof(FileGmImporter))]
    public sealed class FileGmImporterEditor : ScriptedImporterEditor
    {
        private readonly GUIContent meshLabel = EditorGUIUtility.TrTextContent("Meshes", "Global settings for generated meshes");
        private readonly GUIContent meshIsReadableGUIConten = EditorGUIUtility.TrTextContent("Read/Write", "Allow vertices and indices to be accessed from script.");
        private readonly GUIContent flipUvVerticalGUIContent = EditorGUIUtility.TrTextContent(
            text: "Flip UV-vertical",
            tooltip: "Flip UV-map vertically.");
        private SerializedProperty isMeshReadWriteProperty;
        private SerializedProperty flipUvVerticalProperty;
        private SerializedProperty hasAnimatedMeshProperty;

        private readonly GUIContent materialsLabel = EditorGUIUtility.TrTextContent("Materials");
        private readonly GUIContent extractEmbeddedMaterialsButton = EditorGUIUtility.TrTextContent("Extract Materials...", "Click on this button to extract the embedded materials.");
        private SerializedProperty materialsExtractPathProperty;

        private readonly GUIContent animationsLabel = EditorGUIUtility.TrTextContent("Animations");
        private readonly GUIContent selectAnFileButton = EditorGUIUtility.TrTextContent("Select .an file...", "Click on this button to select .an file.");
        private SerializedProperty animationFilePathProperty;

        private readonly GUIContent locatorsLabel = EditorGUIUtility.TrTextContent(
            text: "Locators",
            tooltip: "Settings for locators");
        private readonly GUIContent locatorsCreateConstraintGUIContent = EditorGUIUtility.TrTextContent(
            text: "Create Constraint",
            tooltip: "Create a constraint to link locators to bones.");
        private SerializedProperty hasLocatorsProperty;
        private SerializedProperty createLocatorConstraintProperty;

        public override void OnEnable()
        {
            base.OnEnable();

            isMeshReadWriteProperty = serializedObject.FindProperty("isMeshReadWrite");
            flipUvVerticalProperty = serializedObject.FindProperty("flipUvVertical");
            hasAnimatedMeshProperty = serializedObject.FindProperty("hasAnimatedMesh");
            materialsExtractPathProperty = serializedObject.FindProperty("materialsExtractPath");
            animationFilePathProperty = serializedObject.FindProperty("animationFilePath");
            hasLocatorsProperty = serializedObject.FindProperty("hasLocators");
            createLocatorConstraintProperty = serializedObject.FindProperty("createLocatorConstraint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnInspectorMeshesGUI();
            OnInspectorAnimationsGUI();
            OnInspectorMaterialsGUI();
            OnInspectorLocatorsGUI();

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        private void OnInspectorMeshesGUI()
        {
            EditorGUILayout.LabelField(meshLabel, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isMeshReadWriteProperty, meshIsReadableGUIConten);
            EditorGUILayout.PropertyField(flipUvVerticalProperty, flipUvVerticalGUIContent);
        }

        private void OnInspectorMaterialsGUI()
        {
            AssetImporter assetImporter = target as AssetImporter;
            EditorGUILayout.LabelField(materialsLabel, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(GetMaterialsFromAsset(assetImporter.assetPath).Length <= 0))
            {
                if (GUILayout.Button(extractEmbeddedMaterialsButton))
                {
                    string destinationPath = assetImporter.assetPath;
                    destinationPath = EditorUtility.SaveFolderPanel("Select Materials Folder", Path.GetDirectoryName(destinationPath), "");
                    if (string.IsNullOrEmpty(destinationPath))
                    {
                        // cancel the extraction if the user did not select a folder
                        return;
                    }
                    string assetPath = FileUtil.GetProjectRelativePath(destinationPath);
                    try
                    {
                        // batch the extraction of the textures
                        AssetDatabase.StartAssetEditing();
                        ExtractMaterialsFromAsset(targets, assetPath);
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                    }
                }
            }
        }

        private void OnInspectorAnimationsGUI()
        {
            if (hasAnimatedMeshProperty.boolValue)
            {
                EditorGUILayout.LabelField(animationsLabel, EditorStyles.boldLabel);
                string animationFilePath = animationFilePathProperty.stringValue;
                string fileName = Path.GetFileName(animationFilePath);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "Not selected";
                }
                GUILayout.Label(EditorGUIUtility.TrTextContent(fileName, animationFilePath));
                if (GUILayout.Button(selectAnFileButton))
                {
                    string destinationPath = animationFilePath;
                    destinationPath = EditorUtility.OpenFilePanel("Select .an file", destinationPath, "an");
                    if (!string.IsNullOrEmpty(destinationPath))
                    {
                        destinationPath = FileUtil.GetProjectRelativePath(destinationPath);
                    }
                    animationFilePathProperty.stringValue = destinationPath;
                }
            }
        }

        private void OnInspectorLocatorsGUI()
        {
            if (hasLocatorsProperty.boolValue && hasAnimatedMeshProperty.boolValue)
            {
                EditorGUILayout.LabelField(locatorsLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(createLocatorConstraintProperty, locatorsCreateConstraintGUIContent);
            }
        }

        private void ExtractMaterialsFromAsset(Object[] targets, string destinationPath)
        {
            var assetsToReload = new HashSet<string>();
            bool hasError = false;
            foreach (var t in targets)
            {
                var importer = t as AssetImporter;
                var materials = GetMaterialsFromAsset(importer.assetPath);
                foreach (var material in materials)
                {
                    string newAssetPath = Path.Combine(destinationPath, material.name) + ".mat";
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                    string error = AssetDatabase.ExtractAsset(material, newAssetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        assetsToReload.Add(importer.assetPath);
                    }
                    else
                    {
                        hasError = true;
                        Debug.LogWarning($"Failed to extract asset: error='{error}'.");
                    }
                }
            }
            if (!hasError)
            {
                materialsExtractPathProperty.stringValue = destinationPath;
            }
            foreach (var path in assetsToReload)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private Object[] GetMaterialsFromAsset(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Material)).ToArray();
        }
    }
}
