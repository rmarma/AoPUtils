using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AoP.Editor
{
    [CustomEditor(typeof(FileTgaTxImporter))]
    public sealed class FileTgaTxImporterEditor : ScriptedImporterEditor
    {

        private SerializedProperty isReadWriteProperty;
        private readonly GUIContent isReadWriteGUIContent = EditorGUIUtility.TrTextContent(
            text: "Read/Write",
            tooltip: "Enable to be able to access the raw pixel data from code.");

        private SerializedProperty isSRGBTextureProperty;
        private readonly GUIContent isSRGBTextureGUIContent = EditorGUIUtility.TrTextContent(
            text: "sRGB (Color Texture)",
            tooltip: "Texture content is stored in gamma space. Non-HDR color textures should enable this flag (except if used for IMGUI).");

        private SerializedProperty wrapModeProperty;
        private SerializedProperty wrapModeUProperty;
        private SerializedProperty wrapModeVProperty;
        private readonly GUIContent wrapModeGUIContent = EditorGUIUtility.TrTextContent("Wrap Mode");
        private readonly GUIContent wrapModeUGUIContent = EditorGUIUtility.TrTextContent("U axis");
        private readonly GUIContent wrapModeVGUIContent = EditorGUIUtility.TrTextContent("V axis");

        private readonly GUIContent[] wrapModeContents =
        {
            EditorGUIUtility.TrTextContent("Repeat"),
            EditorGUIUtility.TrTextContent("Clamp"),
            EditorGUIUtility.TrTextContent("Mirror"),
            EditorGUIUtility.TrTextContent("Mirror Once"),
            EditorGUIUtility.TrTextContent("Per-axis")
        };
        private readonly int[] wrapModeValues =
        {
            (int)TextureWrapMode.Repeat,
            (int)TextureWrapMode.Clamp,
            (int)TextureWrapMode.Mirror,
            (int)TextureWrapMode.MirrorOnce,
            -1
        };

        private SerializedProperty filterModeProperty;
        private readonly GUIContent filterModeGUIContent = EditorGUIUtility.TrTextContent("Filter Mode");
        private readonly int[] filterModeValues =
        {
            (int)FilterMode.Point,
            (int)FilterMode.Bilinear,
            (int)FilterMode.Trilinear
        };
        private readonly GUIContent[] filterModeOptions =
        {
            EditorGUIUtility.TrTextContent("Point (no filter)"),
            EditorGUIUtility.TrTextContent("Bilinear"),
            EditorGUIUtility.TrTextContent("Trilinear")
        };

        private SerializedProperty anisoLevelProperty;

        private SerializedProperty isFlipVerticallyProperty;
        private readonly GUIContent isFlipVerticallyGUIContent = EditorGUIUtility.TrTextContent(
            text: "Flip Vertically",
            tooltip: "Flips the image vertically.");


        public override void OnEnable()
        {
            base.OnEnable();

            isReadWriteProperty = serializedObject.FindProperty("isReadWrite");
            isSRGBTextureProperty = serializedObject.FindProperty("isSRGBTexture");
            wrapModeProperty = serializedObject.FindProperty("wrapMode");
            wrapModeUProperty = serializedObject.FindProperty("wrapModeU");
            wrapModeVProperty = serializedObject.FindProperty("wrapModeV");
            filterModeProperty = serializedObject.FindProperty("filterMode");
            anisoLevelProperty = serializedObject.FindProperty("anisoLevel");
            isFlipVerticallyProperty = serializedObject.FindProperty("isFlipVertically");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(isReadWriteProperty, isReadWriteGUIContent);
            EditorGUILayout.PropertyField(isSRGBTextureProperty, isSRGBTextureGUIContent);
            OnInspectorWrapModeGUI();
            OnInspectorFilterModeGUI();
            OnInspectorAnisoLevelGUI();
            // TODO flip vertically
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(isFlipVerticallyProperty, isFlipVerticallyGUIContent);
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        private void OnInspectorWrapModeGUI()
        {
            int wrapMode = wrapModeProperty.intValue;
            EditorGUI.BeginChangeCheck();
            wrapMode = EditorGUILayout.IntPopup(wrapModeGUIContent, wrapMode, wrapModeContents, wrapModeValues);
            if (EditorGUI.EndChangeCheck())
            {
                wrapModeProperty.intValue = wrapMode;
            }
            if (wrapMode == -1)
            {
                EditorGUI.indentLevel++;
                // Wrap U
                TextureWrapMode wrapModeU = (TextureWrapMode)Mathf.Max(wrapModeUProperty.intValue, 0);
                Rect rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginProperty(rect, wrapModeUGUIContent, wrapModeUProperty);
                wrapModeU = (TextureWrapMode)EditorGUI.EnumPopup(rect, wrapModeUGUIContent, wrapModeU);
                EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                {
                    wrapModeUProperty.intValue = (int)wrapModeU;
                }
                // Wrap V
                TextureWrapMode wrapModeV = (TextureWrapMode)Mathf.Max(wrapModeVProperty.intValue, 0);
                rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginProperty(rect, wrapModeVGUIContent, wrapModeVProperty);
                wrapModeV = (TextureWrapMode)EditorGUI.EnumPopup(rect, wrapModeVGUIContent, wrapModeV);
                EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                {
                    wrapModeVProperty.intValue = (int)wrapModeV;
                }
                EditorGUI.indentLevel--;
            }
        }

        private void OnInspectorFilterModeGUI()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, filterModeGUIContent, filterModeProperty);
            EditorGUI.BeginChangeCheck();
            FilterMode filter = (FilterMode)EditorGUI.IntPopup(rect, filterModeGUIContent, filterModeProperty.intValue, filterModeOptions, filterModeValues);
            if (EditorGUI.EndChangeCheck())
            {
                filterModeProperty.intValue = (int)filter;
            }
            EditorGUI.EndProperty();
        }

        private void OnInspectorAnisoLevelGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = anisoLevelProperty.hasMultipleDifferentValues;
            int anisoLevel = anisoLevelProperty.intValue;
            anisoLevel = EditorGUILayout.IntSlider("Aniso Level", anisoLevel, 0, 16);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                anisoLevelProperty.intValue = anisoLevel;
            }
            if (anisoLevel > 1)
            {
                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
                {
                    EditorGUILayout.HelpBox("Anisotropic filtering is disabled for all textures in Quality Settings.", MessageType.Info);
                }
                else if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable)
                {
                    EditorGUILayout.HelpBox("Anisotropic filtering is enabled for all textures in Quality Settings.", MessageType.Info);
                }
            }
        }
    }
}
