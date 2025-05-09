/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * SceneLoader handles scene transitions throughout the quiz game.
 * It manages the game flow from main menu through different difficulty stages and minigames.
 * This class uses the singleton pattern to maintain a single instance across scenes.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Singleton instance
    private static SceneLoader instance;
    
    #region Scene Configuration
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string stage1SceneName = "Stage1";
    [SerializeField] private string stage2SceneName = "Stage2";
    [SerializeField] private string stage3SceneName = "Stage3";
    [SerializeField] private string stage4SceneName = "Stage4";
    [SerializeField] private string minigame1SceneName = "Game 1";
    [SerializeField] private string minigame2SceneName = "Game 2";
    [SerializeField] private string minigame3SceneName = "Game 3";
    
    public string GetMainMenuSceneName() { return mainMenuSceneName; }
    
    [Header("Minigame Settings")]
    [SerializeField] private float minigame1TimeLimit = 30f;
    [SerializeField] private float minigame2TimeLimit = 30f;
    [SerializeField] private float minigame3TimeLimit = 30f;
    #endregion
    
    #region State Management
    // Enum representing the current game state
    private enum QuizState
    {
        MainMenu,
        Stage1,
        Minigame1,
        Stage2,
        Minigame2,
        Stage3,
        Minigame3,
        Stage4,
        Finished
    }
    
    private QuizState currentState = QuizState.MainMenu;
    #endregion
    
    #region Lifecycle Methods
    // Singleton pattern implementation
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneLoader instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("Duplicate SceneLoader found, destroying this instance");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log($"SceneLoader started. Current state: {currentState}");
        ValidateSceneNames();
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("SceneLoader destroyed");
    }
    #endregion
    
    #region Scene Validation
    // Verify all scene names exist in build settings
    private void ValidateSceneNames()
    {
        string[] sceneNames = { mainMenuSceneName, stage1SceneName, stage2SceneName, stage3SceneName,
                               stage4SceneName, minigame1SceneName, minigame2SceneName, minigame3SceneName };
        foreach (string sceneName in sceneNames)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"Scene name is null or empty!");
                continue;
            }
            
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    sceneExists = true;
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings or is invalid!");
            }
        }
    }
    #endregion
    
    #region Scene Loading Events
    // Handle actions needed when a scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene '{scene.name}' loaded. Current state: {currentState}");
        
        // Configure minigame time limits when loading minigame scenes
        if (scene.name == minigame1SceneName || scene.name == minigame2SceneName || scene.name == minigame3SceneName)
        {
            TransitionSceneManager transitionManager = Object.FindFirstObjectByType<TransitionSceneManager>();
            if (transitionManager != null)
            {
                float timeLimit = minigame1TimeLimit;
                if (scene.name == minigame2SceneName)
                {
                    timeLimit = minigame2TimeLimit;
                }
                else if (scene.name == minigame3SceneName)
                {
                    timeLimit = minigame3TimeLimit;
                }
                
                Debug.Log($"Setting minigame time limit to {timeLimit} seconds for scene '{scene.name}'");
                transitionManager.SetMinigameTimeLimit(timeLimit);
            }
            else
            {
                Debug.LogWarning($"TransitionSceneManager not found in minigame scene '{scene.name}'! Using default time limit.");
                StartCoroutine(FallbackMinigameCompletion(30f));
            }
        }
    }
    
    // Fallback mechanism if TransitionSceneManager is not found
    private IEnumerator FallbackMinigameCompletion(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Fallback minigame completion triggered.");
        OnMinigameCompleted();
    }
    #endregion
    
    #region Game Flow Navigation
    // Called when a difficulty level is completed
    public void OnDifficultyCompleted(string difficulty)
    {
        Debug.Log($"Difficulty '{difficulty}' completed. Current state: {currentState}");
        
        switch (difficulty.ToLower())
        {
            case "easy":
                currentState = QuizState.Minigame1;
                Debug.Log($"Loading {minigame1SceneName} scene");
                LoadScene(minigame1SceneName);
                break;
                
            case "medium":
                currentState = QuizState.Minigame2;
                Debug.Log($"Loading {minigame2SceneName} scene");
                LoadScene(minigame2SceneName);
                break;
                
            case "hard":
                currentState = QuizState.Minigame3;
                Debug.Log($"Loading {minigame3SceneName} scene");
                LoadScene(minigame3SceneName);
                break;
                
            case "veryhard":
                currentState = QuizState.Finished;
                Debug.Log("Very Hard difficulty completed, showing end resolution");
                break;
                
            default:
                Debug.LogError($"Unknown difficulty level: {difficulty}");
                LoadMainMenu();
                break;
        }
    }
    
    // Called when a minigame is completed
    public void OnMinigameCompleted()
    {
        Debug.Log($"Minigame completed. Current state: {currentState}");
        switch (currentState)
        {
            case QuizState.Minigame1:
                currentState = QuizState.Stage2;
                GameStateManager.Instance.CurrentDifficulty = GameManager.Difficulty.Medium;
                Debug.Log($"Loading {stage2SceneName} scene");
                LoadScene(stage2SceneName);
                break;
            case QuizState.Minigame2:
                currentState = QuizState.Stage3;
                GameStateManager.Instance.CurrentDifficulty = GameManager.Difficulty.Hard;
                Debug.Log($"Loading {stage3SceneName} scene");
                LoadScene(stage3SceneName);
                break;
            case QuizState.Minigame3:
                currentState = QuizState.Stage4;
                GameStateManager.Instance.CurrentDifficulty = GameManager.Difficulty.VeryHard;
                Debug.Log($"Loading {stage4SceneName} scene");
                LoadScene(stage4SceneName);
                break;
            default:
                Debug.LogError($"Unexpected state during minigame completion: {currentState}");
                LoadMainMenu();
                break;
        }
    }
    
    // Reset and restart the entire quiz
    public void RestartQuiz()
    {
        currentState = QuizState.Stage1;
        GameStateManager.Instance.ResetGameState();
        GameStateManager.Instance.CurrentDifficulty = GameManager.Difficulty.Easy;
        Debug.Log($"Restarting quiz, loading {stage1SceneName}");
        LoadScene(stage1SceneName);
    }
    
    // Start the quiz from the beginning
    public void StartQuiz()
    {
        currentState = QuizState.Stage1;
        GameStateManager.Instance.CurrentDifficulty = GameManager.Difficulty.Easy;
        Debug.Log($"Starting quiz, loading {stage1SceneName}");
        LoadScene(stage1SceneName);
    }
    
    // Return to the main menu
    public void LoadMainMenu()
    {
        currentState = QuizState.MainMenu;
        Debug.Log($"Loading {mainMenuSceneName} scene");
        LoadScene(mainMenuSceneName);
    }
    #endregion
    
    #region Scene Loading Utilities
    // Load a scene by name with validation
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Cannot load scene: Scene name is null or empty!");
            return;
        }
        
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
            {
                sceneExists = true;
                break;
            }
        }
        
        if (!sceneExists)
        {
            Debug.LogError($"Cannot load scene '{sceneName}': Not found in Build Settings!");
            return;
        }
        
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    #endregion
    
    // Public access to singleton instance
    public static SceneLoader Instance
    {
        get { return instance; }
    }
}