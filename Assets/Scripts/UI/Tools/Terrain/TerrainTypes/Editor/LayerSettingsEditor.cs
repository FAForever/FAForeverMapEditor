using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EditMap.TerrainTypes.Editor
{
    [CustomEditor(typeof(LayersSettings))]
    public class LayerSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty LayersList;
        private ReorderableList reorderableList;

        private float leftSpaceWidth = 10f;
        private float nameWidth = 100f;
        private float indexWidth = 50f;
        private float colorWidth = 100f;
        private float blockingWidth = 60f;
        private float recommendedWidth = 100f;
        private float styleWidth = 110f;
        private float fieldHeight = 18f;

        private static float defaultElementHeight = 45f;


        private void OnEnable()
        {
            LayersList = serializedObject.FindProperty("layersSettings");
            reorderableList = new ReorderableList(serializedObject, LayersList, true, true, true, true);
            reorderableList.drawElementCallback += DrawElement;
            reorderableList.onChangedCallback += list => { serializedObject.ApplyModifiedProperties(); };
            reorderableList.elementHeightCallback += GetElementHeight;
            reorderableList.drawHeaderCallback += DrawHeader;
//            reorderableList.onAddCallback += OnAddElement;
//            reorderableList.onAddDropdownCallback+= (rect, list) =>
//            {
//                GUI.W
//            }
//            reorderableList.

            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            serializedObject.Update();
        }

        public override void OnInspectorGUI()
        {
            reorderableList.DoLayoutList();
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            var property = LayersList.GetArrayElementAtIndex(index);
            var nameProperty = property.FindPropertyRelative("name"); //name
            var indexProperty = property.FindPropertyRelative("index"); //index
            var colorProperty = property.FindPropertyRelative("color"); //color
            var blockingProperty = property.FindPropertyRelative("blocking"); //blocking
            var recommendedProperty = property.FindPropertyRelative("recommended");
            var styleProperty = property.FindPropertyRelative("style"); //style
            var descriptionProperty = property.FindPropertyRelative("description"); //description

            rect.yMin += 3;
            rect.yMax -= 3;
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
               
                    float shift = rect.x;
                    EditorGUI.PropertyField(
                        new Rect(shift, rect.y, nameWidth, fieldHeight), nameProperty, GUIContent.none);
                    shift += nameWidth + 2;
                    EditorGUI.PropertyField(
                        new Rect(shift, rect.y, indexWidth, fieldHeight), indexProperty, GUIContent.none);
                    shift += indexWidth + 2;
                    EditorGUI.PropertyField(
                        new Rect(shift, rect.y, blockingWidth, fieldHeight), blockingProperty, GUIContent.none);
                    shift += blockingWidth + 2;
                    EditorGUI.PropertyField(
                            new Rect(shift, rect.y, recommendedWidth, fieldHeight), recommendedProperty, GUIContent.none);
                    shift += recommendedWidth;
                    EditorGUI.PropertyField(
                        new Rect(shift, rect.y, styleWidth, fieldHeight), styleProperty, GUIContent.none);
                    shift += styleWidth;
                    EditorGUI.PropertyField(
                        new Rect(rect.xMax - colorWidth, rect.y, colorWidth, fieldHeight), colorProperty, GUIContent.none);
                    shift += colorWidth;

                    descriptionProperty.stringValue = EditorGUI.TextArea(
                        new Rect(rect.x + 20, rect.y + fieldHeight + 3, rect.width - 20, fieldHeight), descriptionProperty.stringValue);
                

                if (scope.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private float GetElementHeight(int index)
        {
            return defaultElementHeight;
        }

        private void DrawHeader(Rect rect)
        {
            float shift = rect.x + leftSpaceWidth;
            EditorGUI.LabelField(new Rect(shift, rect.y, rect.width, rect.height), "name");
            shift += nameWidth + 2;
            EditorGUI.LabelField(new Rect(shift, rect.y, rect.width, rect.height), "index");
            shift += indexWidth + 2;
            EditorGUI.LabelField(new Rect(shift, rect.y, rect.width, rect.height), "blocking");
            shift += blockingWidth + 2;
            EditorGUI.LabelField(new Rect(shift, rect.y, rect.width, rect.height), "recommended");
            shift += recommendedWidth;
            EditorGUI.LabelField(new Rect(shift, rect.y, rect.width, rect.height), "style");
            shift += styleWidth;
            EditorGUI.LabelField(new Rect(rect.xMax - colorWidth, rect.y, colorWidth, rect.height), "color");
            shift += colorWidth;
        }

        private void OnAddElement(ReorderableList list)
        {
        }

        private void DrawElementDescription(Rect rect, int index, bool active, bool focused)
        {
        }
    }
}