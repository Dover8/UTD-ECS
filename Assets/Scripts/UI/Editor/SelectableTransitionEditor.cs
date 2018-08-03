// /*-------------------------------------------
// ---------------------------------------------
// Creation Date: 06/02/17
// Author: Ben MacKinnon
// Description: Editor script for Selectable Transitions
// Soluis Technolgies ltd.
// ---------------------------------------------
// -------------------------------------------*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(SelectableTransition), true)]
public class SelectableTransitionEditor : Editor {

    SerializedProperty m_Script;
    SerializedProperty m_InteractableProperty;
    SerializedProperty m_TargetGraphicProperty;
    SerializedProperty m_TransitionProperty;
    SerializedProperty m_ColorBlockProperty;
    SerializedProperty m_SpriteStateProperty;

    AnimBool m_ShowColorTint = new AnimBool();
    AnimBool m_ShowSpriteTrasition = new AnimBool();

    private static List<SelectableTransitionEditor> s_Editors = new List<SelectableTransitionEditor>();

    // Whenever adding new SerializedProperties to the Selectable and SelectableEditor
    // Also update this guy in OnEnable. This makes the inherited classes from Selectable not require a CustomEditor.
    private string[] m_PropertyPathToExcludeForChildClasses;

    protected virtual void OnEnable()
    {
        m_Script = serializedObject.FindProperty("m_Script");
        m_InteractableProperty = serializedObject.FindProperty("m_Interactable");
        m_TargetGraphicProperty = serializedObject.FindProperty("m_TargetGraphic");
        m_TransitionProperty = serializedObject.FindProperty("m_Transition");
        m_ColorBlockProperty = serializedObject.FindProperty("m_Colors");
        m_SpriteStateProperty = serializedObject.FindProperty("m_SpriteState");

        m_PropertyPathToExcludeForChildClasses = new[]
        {
                m_Script.propertyPath,
                m_TransitionProperty.propertyPath,
                m_ColorBlockProperty.propertyPath,
                m_SpriteStateProperty.propertyPath,
                m_InteractableProperty.propertyPath,
                m_TargetGraphicProperty.propertyPath,
            };

        var trans = GetTransition(m_TransitionProperty);
        m_ShowColorTint.value = (trans == SelectableTransition.Transition.ColorTint);
        m_ShowSpriteTrasition.value = (trans == SelectableTransition.Transition.SpriteSwap);

        m_ShowColorTint.valueChanged.AddListener(Repaint);
        m_ShowSpriteTrasition.valueChanged.AddListener(Repaint);

        s_Editors.Add(this);
        //RegisterStaticOnSceneGUI();
    }


    protected virtual void OnDisable()
    {
        m_ShowColorTint.valueChanged.RemoveListener(Repaint);
        m_ShowSpriteTrasition.valueChanged.RemoveListener(Repaint);

        s_Editors.Remove(this);
        //RegisterStaticOnSceneGUI();
    }

    //private void RegisterStaticOnSceneGUI()
    //{
    //    SceneView.onSceneGUIDelegate -= StaticOnSceneGUI;
    //    if (s_Editors.Count > 0)
    //        SceneView.onSceneGUIDelegate += StaticOnSceneGUI;
    //}

    static SelectableTransition.Transition GetTransition(SerializedProperty transition)
    {
        return (SelectableTransition.Transition)transition.enumValueIndex;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (!IsDerivedSelectableTransitionEditor())
            EditorGUILayout.PropertyField(m_Script);

        EditorGUILayout.PropertyField(m_InteractableProperty);

        var trans = GetTransition(m_TransitionProperty);

        var graphic = m_TargetGraphicProperty.objectReferenceValue as Graphic;
        if (graphic == null)
            graphic = (target as SelectableTransition).GetComponent<Graphic>();

        m_ShowColorTint.target = (!m_TransitionProperty.hasMultipleDifferentValues && trans == (SelectableTransition.Transition)Button.Transition.ColorTint);
        m_ShowSpriteTrasition.target = (!m_TransitionProperty.hasMultipleDifferentValues && trans == (SelectableTransition.Transition)Button.Transition.SpriteSwap);

        EditorGUILayout.PropertyField(m_TransitionProperty);

        ++EditorGUI.indentLevel;
        {
            if (trans == SelectableTransition.Transition.ColorTint || trans == SelectableTransition.Transition.SpriteSwap)
            {
                EditorGUILayout.PropertyField(m_TargetGraphicProperty);
            }

            switch (trans)
            {
                case SelectableTransition.Transition.ColorTint:
                    if (graphic == null)
                        EditorGUILayout.HelpBox("You must have a Graphic target in order to use a color transition.", MessageType.Warning);
                    break;

                case SelectableTransition.Transition.SpriteSwap:
                    if (graphic as Image == null)
                        EditorGUILayout.HelpBox("You must have a Image target in order to use a sprite swap transition.", MessageType.Warning);
                    break;
            }

            if (EditorGUILayout.BeginFadeGroup(m_ShowColorTint.faded))
            {
                EditorGUILayout.PropertyField(m_ColorBlockProperty);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowSpriteTrasition.faded))
            {
                EditorGUILayout.PropertyField(m_SpriteStateProperty);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            
            EditorGUILayout.EndFadeGroup();
        }
        --EditorGUI.indentLevel;

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        Rect toggleRect = EditorGUILayout.GetControlRect();
        toggleRect.xMin += EditorGUIUtility.labelWidth;
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        // We do this here to avoid requiring the user to also write a Editor for their Selectable-derived classes.
        // This way if we are on a derived class we dont draw anything else, otherwise draw the remaining properties.
        ChildClassPropertiesGUI();

        serializedObject.ApplyModifiedProperties();
    }

    // Draw the extra SerializedProperties of the child class.
    // We need to make sure that m_PropertyPathToExcludeForChildClasses has all the Selectable properties and in the correct order.
    // TODO: find a nicer way of doing this. (creating a InheritedEditor class that automagically does this)
    private void ChildClassPropertiesGUI()
    {
        if (IsDerivedSelectableTransitionEditor())
            return;

        DrawPropertiesExcluding(serializedObject, m_PropertyPathToExcludeForChildClasses);
    }

    private bool IsDerivedSelectableTransitionEditor()
    {
        return GetType() != typeof(SelectableTransitionEditor);
    }

   

    private static string GetSaveControllerPath(SelectableTransition target)
    {
        var defaultName = target.gameObject.name;
        var message = string.Format("Create a new animator for the game object '{0}':", defaultName);
        return EditorUtility.SaveFilePanelInProject("New Animation Contoller", defaultName, "controller", message);
    }

    //private static void StaticOnSceneGUI(SceneView view)
    //{
    //    for (int i = 0; i < Selectable.allSelectables.Count; i++)
    //    {
    //        DrawNavigationForSelectable(Selectable.allSelectables[i]);
    //    }
    //}

    //private static void DrawNavigationForSelectable(UnityEngine.UI.Selectable sel)
    //{
    //    if (sel == null)
    //        return;

    //    Transform transform = sel.transform;
    //    bool active = Selection.transforms.Any(e => e == transform);
    //    Handles.color = new Color(1.0f, 0.9f, 0.1f, active ? 1.0f : 0.4f);
    //    DrawNavigationArrow(-Vector2.right, sel, sel.FindSelectableOnLeft());
    //    DrawNavigationArrow(Vector2.right, sel, sel.FindSelectableOnRight());
    //    DrawNavigationArrow(Vector2.up, sel, sel.FindSelectableOnUp());
    //    DrawNavigationArrow(-Vector2.up, sel, sel.FindSelectableOnDown());
    //}

    //const float kArrowThickness = 2.5f;
    //const float kArrowHeadSize = 1.2f;

    //private static void DrawNavigationArrow(Vector2 direction, UnityEngine.UI.Selectable fromObj, UnityEngine.UI.Selectable toObj)
    //{
    //    if (fromObj == null || toObj == null)
    //        return;
    //    Transform fromTransform = fromObj.transform;
    //    Transform toTransform = toObj.transform;

    //    Vector2 sideDir = new Vector2(direction.y, -direction.x);
    //    Vector3 fromPoint = fromTransform.TransformPoint(GetPointOnRectEdge(fromTransform as RectTransform, direction));
    //    Vector3 toPoint = toTransform.TransformPoint(GetPointOnRectEdge(toTransform as RectTransform, -direction));
    //    float fromSize = HandleUtility.GetHandleSize(fromPoint) * 0.05f;
    //    float toSize = HandleUtility.GetHandleSize(toPoint) * 0.05f;
    //    fromPoint += fromTransform.TransformDirection(sideDir) * fromSize;
    //    toPoint += toTransform.TransformDirection(sideDir) * toSize;
    //    float length = Vector3.Distance(fromPoint, toPoint);
    //    Vector3 fromTangent = fromTransform.rotation * direction * length * 0.3f;
    //    Vector3 toTangent = toTransform.rotation * -direction * length * 0.3f;

    //    Handles.DrawBezier(fromPoint, toPoint, fromPoint + fromTangent, toPoint + toTangent, Handles.color, null, kArrowThickness);
    //    Handles.DrawAAPolyLine(kArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction - sideDir) * toSize * kArrowHeadSize);
    //    Handles.DrawAAPolyLine(kArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction + sideDir) * toSize * kArrowHeadSize);
    //}

    private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
    {
        if (rect == null)
            return Vector3.zero;
        if (dir != Vector2.zero)
            dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
        dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
        return dir;
    }
}

