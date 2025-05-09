/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * MainMenuController.cs
 * 
 * This script manages the main menu UI and navigation for the quiz game,
 * including panel transitions, button events, and initiating the quiz generation.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;

public class MainMenuController : MonoBehaviour
{
    #region UI Elements
    [Header("UI Panels")]
    public GameObject menuPanel;          // Main menu panel
    public GameObject topicInputPanel;    // Panel for entering quiz topic
    public GameObject aboutPanel;         // Information about the game
    public GameObject loadingPanel;       // Loading indicator while generating questions
    
    [Header("UI Buttons")]
    public Button playButton;             // Button to start a new game
    public Button aboutButton;            // Button to show information about the game
    public Button exitButton;             // Button to quit the application
    public Button startQuizButton;        // Button to begin quiz with entered topic
    public Button backButton;             // Button to return to main menu
    
    [Header("Input Fields")]
    public TMP_InputField topicInput;     // Field for entering quiz topic
    #endregion
    
    #region Components
    [Header("Components")]
    public QuestionGenerator questionGenerator;    // Reference to question generator
    #endregion
    
    #region Audio
    [Header("Audio")]
    [SerializeField] private string mainMenuMusicName = "MainMenuMusic";    // Name of background music track
    #endregion

    #region Initialization
    private void Awake()
    {
        ValidateReferences();
    }

    /// <summary>
    /// Validates that all required components are properly assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (menuPanel == null) Debug.LogError("MenuPanel is not assigned!");
        if (topicInputPanel == null) Debug.LogError("TopicInputPanel is not assigned!");
        if (aboutPanel == null) Debug.LogError("AboutPanel is not assigned!");
        if (loadingPanel == null) Debug.LogError("LoadingPanel is not assigned!");
        if (playButton == null) Debug.LogError("PlayButton is not assigned!");
        if (aboutButton == null) Debug.LogError("AboutButton is not assigned!");
        if (exitButton == null) Debug.LogError("ExitButton is not assigned!");
        if (startQuizButton == null) Debug.LogError("StartQuizButton is not assigned!");
        if (backButton == null) Debug.LogError("BackButton is not assigned!");
        if (topicInput == null) Debug.LogError("TopicInput is not assigned!");
        if (questionGenerator == null) Debug.LogError("QuestionGenerator is not assigned!");
    }

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        SetupInputFieldListener();
        PlayMainMenuMusic();
    }

    private void OnDestroy()
    {
        // Clean up event listeners to prevent memory leaks
        CleanupButtonListeners();
        CleanupInputFieldListener();
    }

    /// <summary>
    /// Set initial UI panel states
    /// </summary>
    private void InitializeUI()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (topicInputPanel != null) topicInputPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    #endregion

    #region Event Listeners
    /// <summary>
    /// Set up button click event listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(ShowTopicInput);
        }
        
        if (aboutButton != null)
        {
            aboutButton.onClick.RemoveAllListeners();
            aboutButton.onClick.AddListener(ShowAbout);
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
        
        if (startQuizButton != null)
        {
            startQuizButton.onClick.RemoveAllListeners();
            startQuizButton.onClick.AddListener(StartQuiz);
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowMenu);
        }
    }
    
    /// <summary>
    /// Set up input field submit event listener
    /// </summary>
    private void SetupInputFieldListener()
    {
        if (topicInput != null)
        {
            topicInput.onSubmit.RemoveAllListeners();
            topicInput.onSubmit.AddListener(delegate { StartQuiz(); });
        }
    }
    
    /// <summary>
    /// Remove all button event listeners
    /// </summary>
    private void CleanupButtonListeners()
    {
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (aboutButton != null) aboutButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        if (startQuizButton != null) startQuizButton.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveAllListeners();
    }
    
    /// <summary>
    /// Remove all input field event listeners
    /// </summary>
    private void CleanupInputFieldListener()
    {
        if (topicInput != null) topicInput.onSubmit.RemoveAllListeners();
    }
    #endregion

    #region Audio
    /// <summary>
    /// Play background music for the main menu
    /// </summary>
    private void PlayMainMenuMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(mainMenuMusicName);
        }
        else
        {
            Debug.LogError("AudioManager instance not found! Unable to play main menu music.");
        }
    }
    #endregion

    #region UI Navigation
    /// <summary>
    /// Show the topic input panel
    /// </summary>
    void ShowTopicInput()
    {
        if (menuPanel != null && topicInputPanel != null)
        {
            menuPanel.SetActive(false);
            topicInputPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("MenuPanel or TopicInputPanel is not assigned!");
        }
    }

    /// <summary>
    /// Show the about panel
    /// </summary>
    void ShowAbout()
    {
        if (menuPanel != null && aboutPanel != null)
        {
            menuPanel.SetActive(false);
            aboutPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("MenuPanel or AboutPanel is not assigned!");
        }
    }

    /// <summary>
    /// Return to the main menu panel
    /// </summary>
    void ShowMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            
            if (aboutPanel != null)
            {
                aboutPanel.SetActive(false);
            }
            
            if (topicInputPanel != null)
            {
                topicInputPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("MenuPanel is not assigned!");
        }
    }
    #endregion

    #region Quiz Management
    /// <summary>
    /// Start the quiz with the entered topic
    /// </summary>
    void StartQuiz()
    {
        if (topicInput == null)
        {
            Debug.LogError("TopicInput is not assigned!");
            return;
        }
        
        string topic = topicInput.text;
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogWarning("Please enter a topic!");
            return;
        }
        
        if (topicInputPanel == null || loadingPanel == null)
        {
            Debug.LogError("TopicInputPanel or LoadingPanel is not assigned!");
            return;
        }
        
        // Stop menu music when starting the quiz
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSound(mainMenuMusicName);
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found! Unable to stop main menu music.");
        }
        
        // Show loading panel while generating questions
        topicInputPanel.SetActive(false);
        loadingPanel.SetActive(true);
        
        if (questionGenerator == null)
        {
            Debug.LogError("QuestionGenerator is not assigned!");
            loadingPanel.SetActive(false);
            topicInputPanel.SetActive(true);
            return;
        }
        
        // Generate questions based on the entered topic
        questionGenerator.GenerateQuestions(topic, OnQuestionsGenerated);
    }
    
    /// <summary>
    /// Callback when questions have been generated
    /// </summary>
    private void OnQuestionsGenerated()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        // Load the game scene
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("Stage1");
        }
        else
        {
            Debug.LogError("SceneLoader instance not found! Loading Stage1 directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Stage1");
        }
    }
    #endregion

    #region Application Control
    /// <summary>
    /// Exit the application
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSound(mainMenuMusicName);
        }
        
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    #endregion
}