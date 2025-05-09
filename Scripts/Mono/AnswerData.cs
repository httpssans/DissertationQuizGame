/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * AnswerData manages the UI and state for individual answer options in quiz questions.
 * It handles toggling between selected and unselected states and ensures only one answer
 * can be selected at a time (radio button behavior).
 */

using UnityEngine;
using TMPro;
using System;

public class AnswerData : MonoBehaviour
{
    #region UI References
    [SerializeField] private TextMeshProUGUI infoTextObject = null;
    [SerializeField] private UnityEngine.UI.Image toggle = null;
    [SerializeField] private Sprite uncheckedToggle = null;
    [SerializeField] private Sprite checkedToggle = null;
    [SerializeField] private GameEvents events = null;
    #endregion

    // Static reference to track the currently selected answer
    private static AnswerData currentlySelectedAnswer = null;

    #region Property Caching
    private RectTransform _rect = null;
    public RectTransform Rect
    {
        get
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
            }
            return _rect;
        }
    }

    private int _answerIndex = -1;
    public int AnswerIndex { get { return _answerIndex; } }

    private bool Checked = false;
    #endregion

    #region Public Methods
    // Update the answer text and index
    public void UpdateData(string text, int index)
    {
        if (infoTextObject != null)
        {
            infoTextObject.text = text;
        }
        else
        {
            Debug.LogWarning("InfoTextObject is not assigned in AnswerData!");
        }
        _answerIndex = index;

        // Reset checked state when updating data
        Checked = false;
        UpdateVisual();
    }

    // Toggle the selection state of this answer
    public void SwitchState()
    {
        // If this answer is already checked, do nothing (can't uncheck the only selected option)
        if (Checked)
            return;

        // Uncheck the previously selected answer if it exists
        if (currentlySelectedAnswer != null && currentlySelectedAnswer != this)
        {
            currentlySelectedAnswer.Checked = false;
            currentlySelectedAnswer.UpdateVisual();
        }

        // Set this as the new checked answer
        Checked = true;
        currentlySelectedAnswer = this;
        UpdateVisual();

        // Invoke the event to update the selected answer
        if (events != null)
        {
            events.UpdateQuestionAnswer?.Invoke(new PickedAnswerData { AnswerIndex = _answerIndex });
        }
    }

    // Reset this individual answer's state
    public void Reset()
    {
        Checked = false;
        UpdateVisual();
        
        // Only clear the static reference if this is the currently selected answer
        if (currentlySelectedAnswer == this)
        {
            currentlySelectedAnswer = null;
        }
    }
    #endregion

    #region Static Methods
    // This method resets the static tracking of selected answers when new questions are loaded
    public static void ResetSelections()
    {
        if (currentlySelectedAnswer != null)
        {
            currentlySelectedAnswer.Checked = false;
            currentlySelectedAnswer.UpdateVisual();
            currentlySelectedAnswer = null;
        }
    }
    #endregion

    #region Private Methods
    // Update the visual appearance based on checked state
    private void UpdateVisual()
    {
        if (toggle != null)
        {
            toggle.sprite = Checked ? checkedToggle : uncheckedToggle;
        }
        else
        {
            Debug.LogWarning("Toggle Image is not assigned in AnswerData!");
        }
    }
    #endregion

    #region Lifecycle Methods
    private void OnDestroy()
    {
        // Clear the static reference if this object is being destroyed
        if (currentlySelectedAnswer == this)
        {
            currentlySelectedAnswer = null;
        }
    }
    #endregion
}