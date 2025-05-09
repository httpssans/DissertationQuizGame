/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * TransitionSceneManager controls the minigame scenes between quiz stages.
 * It manages the UI elements, countdown timer, and transitions to the next level.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TransitionSceneManager : MonoBehaviour
{
    #region UI References
    [Header("UI References")]
    [SerializeField] private GameObject quizUIElements; // Reference to quiz UI elements to hide
    [SerializeField] private GameObject minigameContent; // Reference to the minigame content to show
    [SerializeField] private TextMeshProUGUI sceneTitle; // Display current scene/minigame name
    [SerializeField] private TextMeshProUGUI countdownText; // Display countdown to next level
    [SerializeField] private Image countdownFill; // Optional: visual countdown indicator
    #endregion
    
    #region Display Settings
    [Header("Display Settings")]
    [SerializeField] private string customTitle = ""; // Override default scene name if desired
    [SerializeField] private bool showCountdown = true; // Whether to show the countdown timer
    [SerializeField] private float defaultTimeLimit = 30f; // Default time limit if not set by minigame
    #endregion
    
    #region Private Members
    private float timeLimit;
    private float timeRemaining;
    private bool isCompleted = false;
    private SceneLoader sceneLoader;
    #endregion
    
    #region Initialization
    private void Awake()
    {
        // Try to find SceneLoader in the scene or get the singleton instance
        sceneLoader = Object.FindFirstObjectByType<SceneLoader>();
        if (sceneLoader == null)
        {
            sceneLoader = SceneLoader.Instance;
            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader not found in scene or via Instance!");
            }
        }
        
        // Initialize timer values
        timeLimit = defaultTimeLimit;
        timeRemaining = timeLimit;
        
        Debug.Log($"TransitionSceneManager initialized in {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} with time limit: {timeLimit}");
    }
    
    private void Start()
    {
        // Set up the UI elements
        if (quizUIElements != null)
        {
            quizUIElements.SetActive(false);
        }
        else
        {
            Debug.LogWarning("quizUIElements not assigned! Quiz UI will not be hidden.");
        }
        
        if (minigameContent != null)
        {
            minigameContent.SetActive(true);
        }
        else
        {
            Debug.LogWarning("minigameContent not assigned! Minigame content will not be shown.");
        }
        
        // Set the scene title
        if (sceneTitle != null)
        {
            sceneTitle.text = string.IsNullOrEmpty(customTitle) ? 
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : 
                customTitle;
        }
        
        // Start the countdown if enabled
        if (showCountdown)
        {
            StartCoroutine(UpdateCountdown());
        }
        
        // Play minigame music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("MinigameMusic");
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found!");
        }
        
        Debug.Log($"TransitionSceneManager started in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }
    #endregion
    
    #region Timer Management
    private void Update()
    {
        // Update the countdown timer
        if (showCountdown && !isCompleted)
        {
            timeRemaining -= Time.deltaTime;
            
            // Update visual fill indicator
            if (countdownFill != null)
            {
                countdownFill.fillAmount = Mathf.Clamp01(timeRemaining / timeLimit);
            }
            
            // Check if time is up
            if (timeRemaining <= 0)
            {
                CompleteMinigame();
            }
        }

        // Debug feature: Press Space to complete minigame immediately
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Debug: Spacebar pressed, completing minigame");
            CompleteMinigame();
        }
    }
    
    private IEnumerator UpdateCountdown()
    {
        // Continuously update the countdown text
        while (timeRemaining > 0 && !isCompleted)
        {
            if (countdownText != null)
            {
                countdownText.text = "Next Level: " + Mathf.CeilToInt(timeRemaining).ToString();
                
                // Turn text red when time is almost up
                if (timeRemaining <= 3f)
                {
                    countdownText.color = Color.red;
                }
            }
            
            yield return null;
        }
        
        // Show transition message when countdown finishes
        if (countdownText != null)
        {
            countdownText.text = "Transitioning...";
        }
    }
    #endregion
    
    #region Public Methods
    // Set a custom time limit for the minigame
    public void SetMinigameTimeLimit(float customTimeLimit)
    {
        if (customTimeLimit > 0)
        {
            timeLimit = customTimeLimit;
            timeRemaining = customTimeLimit;
            
            Debug.Log($"Minigame time limit set to: {timeLimit} seconds");
            
            // Update UI elements with new time
            if (countdownText != null)
            {
                countdownText.text = "Next Level: " + Mathf.CeilToInt(timeRemaining).ToString();
            }
            
            if (countdownFill != null)
            {
                countdownFill.fillAmount = 1f;
            }
        }
    }
    
    // Get the current time limit
    public float GetTimeLimit()
    {
        return timeLimit;
    }
    
    // Handle minigame completion and transition to next level
    public void CompleteMinigame()
    {
        // Prevent duplicate calls
        if (isCompleted)
        {
            Debug.Log("Minigame already completed. Ignoring duplicate call.");
            return;
        }
            
        isCompleted = true;
        timeRemaining = 0;
        
        Debug.Log($"Minigame completed in {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}! Transitioning to next level...");
        
        // Stop minigame music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSound("MinigameMusic");
        }
        
        // Notify SceneLoader to transition to the next level
        if (sceneLoader != null)
        {
            Debug.Log("Notifying SceneLoader of minigame completion");
            sceneLoader.OnMinigameCompleted();
        }
        else
        {
            Debug.LogError("SceneLoader not found! Attempting to use SceneLoader.Instance.");
            sceneLoader = SceneLoader.Instance;
            if (sceneLoader != null)
            {
                sceneLoader.OnMinigameCompleted();
            }
            else
            {
                Debug.LogError("SceneLoader.Instance also null! Cannot complete minigame.");
            }
        }
    }
    #endregion
}