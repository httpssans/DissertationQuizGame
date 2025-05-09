// Author: Sanskar Bikram Kunwar, 2025
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Class that handles the main menu functionality
public class MainMenu : MonoBehaviour
{
    // Reference to the input field for entering quiz topic
    [SerializeField] private TMP_InputField topicInputField = null;
    
    // Reference to the start button
    [SerializeField] private Button startButton = null;
    
    // Reference to the QuestionGenerator prefab to instantiate if needed
    [SerializeField] private GameObject questionGeneratorPrefab = null;

    // Called when the script instance is being loaded
    void Awake()
    {
        // Ensure QuestionGenerator exists - if not, instantiate it from the prefab
        if (QuestionGenerator.Instance == null && questionGeneratorPrefab != null)
        {
            Debug.Log("Instantiating QuestionGenerator prefab...");
            Instantiate(questionGeneratorPrefab);
        }
    }

    // Called before the first frame update
    void Start()
    {
        // Set up click listener for the start button
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartQuiz);
        }
        else
        {
            Debug.LogError("StartButton is not assigned in MainMenu!");
        }
    }

    // Method called when the start button is clicked
    void StartQuiz()
    {
        // Get the topic from the input field, or use "default" if empty
        string topic = topicInputField != null && !string.IsNullOrEmpty(topicInputField.text) ? topicInputField.text : "default";
        Debug.Log($"Starting quiz with topic: {topic}");

        // Use the QuestionGenerator to create questions based on the topic
        if (QuestionGenerator.Instance != null)
        {
            Debug.Log("Calling QuestionGenerator.GenerateQuestions...");
            QuestionGenerator.Instance.GenerateQuestions(topic, () =>
            {
                // Once questions are generated, load the Game scene
                Debug.Log("Questions generated, loading Game scene...");
                SceneManager.LoadScene("Game");
            });
        }
        else
        {
            Debug.LogError("QuestionGenerator instance is missing! Cannot start quiz.");
            // Optionally, display UI error message instead of loading scene
        }
    }
}