// /*-------------------------------------------
// ---------------------------------------------
// Creation Date: 06/02/17
// Author: Ben MacKinnon
// Description: Allows for multiple transition effects on a UI.Selectable component
// Soluis Technolgies ltd.
// ---------------------------------------------
// -------------------------------------------*/

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
public class SelectableTransition : UIBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
{

    // Toggle component.
    [SerializeField]
    private Toggle m_Toggle;

    // Highlighting state
    public enum Transition
    {
        None,
        ColorTint,
        SpriteSwap
    }

    // Type of the transition that occurs when the button state changes.
    [FormerlySerializedAs("transition")]
    [SerializeField]
    private Transition m_Transition = Transition.ColorTint;

    // Colors used for a color tint-based transition.
    [FormerlySerializedAs("colors")]
    [SerializeField]
    private ColorBlock m_Colors = ColorBlock.defaultColorBlock;

    // Sprites used for a Image swap-based transition.
    [FormerlySerializedAs("spriteState")]
    [SerializeField]
    private SpriteState m_SpriteState;

    // Graphic that will be colored.
    [FormerlySerializedAs("highlightGraphic")]
    [FormerlySerializedAs("m_HighlightGraphic")]
    [SerializeField]
    private Graphic m_TargetGraphic;

    // Convenience function that converts the Graphic to a Image, if possible
    public Image image
    {
        get { return m_TargetGraphic as Image; }
        set { m_TargetGraphic = value; }
    }

    private bool m_GroupsAllowInteraction = true;

    private SelectionState m_CurrentSelectionState;

    protected enum SelectionState
    {
        Normal,
        Highlighted,
        Pressed,
        Disabled
    }


    //   public Transition transition { get { return m_Transition; } set { if (SetPropertyUtility.SetStruct(ref m_Transition, value)) OnSetProperty(); } }
    //   public ColorBlock colors { get { return m_Colors; } set { if (SetPropertyUtility.SetStruct(ref m_Colors, value)) OnSetProperty(); } }
    //   public SpriteState spriteState { get { return m_SpriteState; } set { if (SetPropertyUtility.SetStruct(ref m_SpriteState, value)) OnSetProperty(); } }
    ////   public AnimationTriggers animationTriggers { get { return m_AnimationTriggers; } set { if (SetPropertyUtility.SetClass(ref m_AnimationTriggers, value)) OnSetProperty(); } }
    //   public Graphic targetGraphic { get { return m_TargetGraphic; } set { if (SetPropertyUtility.SetClass(ref m_TargetGraphic, value)) OnSetProperty(); } }
       public bool interactable { get { return m_Interactable; } }

    private bool isPointerInside { get; set; }
    private bool isPointerDown { get; set; }
    private bool hasSelection { get; set; }


    protected override void Awake()
    {
        if (m_TargetGraphic == null)
            m_TargetGraphic = GetComponent<Graphic>();
        m_Toggle = GetComponent<Toggle>();
        if (m_Toggle != null)
        {
            Toggle(m_Toggle.isOn);
            m_Toggle.onValueChanged.Invoke(m_Toggle.isOn);
        }
    }

    //private readonly List<CanvasGroup> m_CanvasGroupCache = new List<CanvasGroup>();

    private void OnSetProperty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            InternalEvaluateAndTransitionToSelectionState(true);
        else
#endif
            InternalEvaluateAndTransitionToSelectionState(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        var state = SelectionState.Normal;

        if (hasSelection)
            state = SelectionState.Highlighted;

        m_CurrentSelectionState = state;
        InternalEvaluateAndTransitionToSelectionState(true);
    }

    // Remove from the list.
    protected override void OnDisable()
    {
        InstantClearState();
        base.OnDisable();
    }

    private void InstantClearState()
    {
        isPointerInside = false;
        isPointerDown = false;
        hasSelection = false;

        switch (m_Transition)
        {
            case Transition.ColorTint:
                StartColorTween(Color.white, true);
                break;
            case Transition.SpriteSwap:
                DoSpriteSwap(null);
                break;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        m_Colors.fadeDuration = Mathf.Max(m_Colors.fadeDuration, 0.0f);

        // OnValidate can be called before OnEnable, this makes it unsafe to access other components
        // since they might not have been initialized yet.
        // OnSetProperty potentially access Animator or Graphics. (case 618186)
        if (isActiveAndEnabled)
        {
            // Need to clear out the override image on the target...
            DoSpriteSwap(null);

            // If the transition mode got changed, we need to clear all the transitions, since we don't know what the old transition mode was.
            StartColorTween(Color.white, true);

            // And now go to the right state.
            InternalEvaluateAndTransitionToSelectionState(true);
        }
    }

    protected override void Reset()
    {
        m_TargetGraphic = GetComponent<Graphic>();
    }

#endif // if UNITY_EDITOR


    protected virtual void DoStateTransition(SelectionState state, bool instant)
    {
        Color tintColor;
        Sprite transitionSprite;

        switch (state)
        {
            case SelectionState.Normal:
                tintColor = m_Colors.normalColor;
                transitionSprite = null;
                break;
            case SelectionState.Highlighted:
                tintColor = m_Colors.highlightedColor;
                transitionSprite = m_SpriteState.highlightedSprite;
                break;
            case SelectionState.Pressed:
                tintColor = m_Colors.pressedColor;
                transitionSprite = m_SpriteState.pressedSprite;
                break;
            case SelectionState.Disabled:
                tintColor = m_Colors.disabledColor;
                transitionSprite = m_SpriteState.disabledSprite;
                break;
            default:
                tintColor = Color.black;
                transitionSprite = null;
                break;
        }

        if (gameObject.activeInHierarchy)
        {
            switch (m_Transition)
            {
                case Transition.ColorTint:
                    StartColorTween(tintColor * m_Colors.colorMultiplier, instant);
                    break;
                case Transition.SpriteSwap:
                    DoSpriteSwap(transitionSprite);
                    break;
            }
        }
    }


    void StartColorTween(Color targetColor, bool instant)
    {
        if (m_TargetGraphic == null)
        {
            return;
        }

        if (m_TargetGraphic is Text)
        {
            Text text = m_TargetGraphic as Text;
            text.color = targetColor;
            //text.CrossFadeColor(targetColor, instant ? 0f : m_Colors.fadeDuration, true, true);
            text.SetAllDirty();
        }
        else
        {
            m_TargetGraphic.CrossFadeColor(targetColor, instant ? 0f : m_Colors.fadeDuration, true, true);
        }
    }

    void DoSpriteSwap(Sprite newSprite)
    {
        if (image == null)
            return;

        image.overrideSprite = newSprite;
    }

    //[Tooltip("Can the Selectable be interacted with?")]
    [SerializeField]
    private bool m_Interactable = true;

    public virtual bool IsInteractable()
    {
        return m_GroupsAllowInteraction && m_Interactable;
    }

    // Whether the control should be 'selected'.
    protected bool IsHighlighted(BaseEventData eventData)
    {
        if (!isActiveAndEnabled)
            return false;

        if (IsPressed())
            return false;

        bool selected = hasSelection;
        if (eventData is PointerEventData)
        {
            var pointerData = eventData as PointerEventData;
            selected |=
                (isPointerDown && !isPointerInside && pointerData.pointerPress == gameObject) // This object pressed, but pointer moved off
                || (!isPointerDown && isPointerInside && pointerData.pointerPress == gameObject) // This object pressed, but pointer released over (PointerUp event)
                || (!isPointerDown && isPointerInside && pointerData.pointerPress == null); // Nothing pressed, but pointer is over
        }
        else
        {
            selected |= isPointerInside;
        }
        return selected;
    }

    //[Obsolete("Is Pressed no longer requires eventData", false)]
    protected bool IsPressed(BaseEventData eventData)
    {
        return IsPressed();
    }

    // Whether the control should be pressed.
    protected bool IsPressed()
    {
        if (!isActiveAndEnabled)
            return false;

        return isPointerInside && isPointerDown;
    }

    // The current visual state of the control.
    protected void UpdateSelectionState(BaseEventData eventData)
    {
        if (IsPressed())
        {
            m_CurrentSelectionState = SelectionState.Pressed;
            return;
        }
        
        if (IsHighlighted(eventData))
        {
            m_CurrentSelectionState = SelectionState.Highlighted;
            return;
        }

        m_CurrentSelectionState = SelectionState.Normal;
    }

    // Change the button to the correct state
    private void EvaluateAndTransitionToSelectionState(BaseEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        UpdateSelectionState(eventData);
        InternalEvaluateAndTransitionToSelectionState(false);
    }

    private void InternalEvaluateAndTransitionToSelectionState(bool instant)
    {
        var transitionState = m_CurrentSelectionState;
        if (isActiveAndEnabled && !IsInteractable())
            transitionState = SelectionState.Disabled;
        DoStateTransition(transitionState, instant);
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Selection tracking
        if (IsInteractable())
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

        isPointerDown = true;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPointerDown = false;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void OnSelect(BaseEventData eventData)
    {
        //special case for Toggles
        if (m_Toggle != null)
        {
            return;
        }
        hasSelection = true;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void OnDeselect(BaseEventData eventData)
    {
        //special case for Toggles
        if (m_Toggle != null)
        {
            return;
        }
        hasSelection = false;
        EvaluateAndTransitionToSelectionState(eventData);
    }

    public virtual void Select()
    {
        if (EventSystem.current.alreadySelecting)
            return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }


    public void Toggle(bool isOn)
    {
        if (isOn)
        {
            hasSelection = true;
            m_CurrentSelectionState = SelectionState.Pressed;
            InternalEvaluateAndTransitionToSelectionState(true);
        }
        else
        {
            hasSelection = false;
            m_CurrentSelectionState = SelectionState.Normal;
            InternalEvaluateAndTransitionToSelectionState(true);
        }
    }
}
