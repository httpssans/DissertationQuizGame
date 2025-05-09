// Author: Sanskar Bikram Kunwar, 2025
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class representing an individual answer UI component in the quiz game
public class Answer : MonoBehaviour
{
    // Reference to the text component that displays the answer text
    public TextMeshProUGUI infoTextObject;
    
    // Reference to the image component that displays the toggle state (checked/unchecked)
    public Image toggle;
    
    // Sprite references for the two toggle states
    public Sprite uncheckedToggle;
    public Sprite checkedToggle;
    
    // Property to store the answer text
    public string answerText { get; private set; }
    
    // Internal state tracking whether this answer is currently selected
    private bool isSelected = false;

    /// <summary>
    /// Initializes the answer component with text and callback function
    /// </summary>
    /// <param name="text">The answer text to display</param>
    /// <param name="onSelected">Callback function triggered when this answer is selected/deselected</param>
    public void Initialize(string text, System.Action<string> onSelected)
    {
        // Store and display the answer text
        answerText = text;
        infoTextObject.text = text;
        
        // Set the initial toggle state to unchecked
        toggle.sprite = uncheckedToggle;
        isSelected = false;

        // Remove any existing click listeners and add a new one
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            // Toggle the selection state when clicked
            isSelected = !isSelected;
            
            // Update the toggle sprite based on selection state
            toggle.sprite = isSelected ? checkedToggle : uncheckedToggle;
            
            // Invoke the callback with the answer text if selected, or null if deselected
            onSelected?.Invoke(isSelected ? answerText : null);
        });
    }
}