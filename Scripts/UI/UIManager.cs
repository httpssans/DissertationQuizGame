/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * UIManager class handles all UI-related functionality for the quiz game.
 * It manages answer options, resolution screens, difficulty screens, and score displays.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#region UI Parameter Structures
[Serializable()]
public struct UIManagerParameters
{
    [Header("Answers Options")]
    [SerializeField] float margins;
    public float Margins { get { return margins; } }

    [Header("Resolution Screen Options")]
    [SerializeField] Color correctBGColor;
    public Color CorrectBGColor { get { return correctBGColor; } }
    [SerializeField] Color incorrectBGColor;
    public Color IncorrectBGColor { get { return incorrectBGColor; } }
    [SerializeField] Color finalBGColor;
    public Color FinalBGColor { get { return finalBGColor; } }
    
    [Header("Difficulty Resolution Screen Options")]
    [SerializeField] Color easyScreenColor;
    public Color EasyScreenColor { get { return easyScreenColor; } }
    [SerializeField] Color mediumScreenColor;
    public Color MediumScreenColor { get { return mediumScreenColor; } }
    [SerializeField] Color hardScreenColor;
    public Color HardScreenColor { get { return hardScreenColor; } }
    [SerializeField] Color veryHardScreenColor;
    public Color VeryHardScreenColor { get { return veryHardScreenColor; } }
    
    [Header("Overlay Options")]
    [SerializeField] Color overlayColor;
    public Color OverlayColor { get { return overlayColor; } }
    [SerializeField] float overlayAlpha;
    public float OverlayAlpha { get { return overlayAlpha; } }
    
    // Constructor to initialize default values
    public UIManagerParameters(bool init)
    {
        margins = 0f;
        correctBGColor = Color.green;
        incorrectBGColor = Color.red;
        finalBGColor = Color.blue;
        easyScreenColor = Color.cyan;
        mediumScreenColor = Color.yellow;
        hardScreenColor = Color.magenta;
        veryHardScreenColor = Color.red;
        overlayColor = Color.black;
        overlayAlpha = 0.9f;
    }
}

[Serializable()]
public struct UIElements
{
    public RectTransform answersContentArea;      // Container for answer options
    public TextMeshProUGUI questionInfoTextObject; // Displays the current question
    public TextMeshProUGUI scoreText;             // Displays the current score
    public Animator resolutionScreenAnimator;     // Controls resolution screen animations
    public Image resolutionBG;                    // Background image for resolution screen
    public Image overlayImage;                    // Black overlay for transitions
    public TextMeshProUGUI resolutionStateInfoText; // Shows correct/incorrect/finish status
    public TextMeshProUGUI resolutionScoreText;    // Shows score on resolution screen
    public TextMeshProUGUI highScoreText;         // Shows high score on finish screen
    public CanvasGroup mainCanvasGroup;           // Main canvas for enabling/disabling interaction
    public RectTransform finishUIElements;        // Container for end game UI elements
    public Button playAgainButton;                // Button to restart the game
    public Button quitButton;                     // Button to quit the game
    public Button nextButton;                     // Button to proceed to next question
}
#endregion

public class UIManager : MonoBehaviour
{
    // Resolution screen types for different game states
    public enum ResolutionScreenType { Correct, Incorrect, Finish, Easy, Medium, Hard, VeryHard }

    #region Variables and References
    [Header("References")]
    [SerializeField] GameEvents events = null;    // Reference to game events system

    [Header("UI Elements (Prefabs)")]
    [SerializeField] GameObject answerPrefab = null; // Prefab for answer options

    [SerializeField] UIElements uIElements = new UIElements();
    [SerializeField] ToggleGroup toggleGroup;     // Toggle group for answer selection

    [Space]
    [SerializeField] UIManagerParameters parameters = new UIManagerParameters();

    private List<AnswerData> currentAnswers = new List<AnswerData>(); // Tracks current answer objects
    private int resStateParaHash = 0;             // Hash for animator parameter

    private IEnumerator IE_DisplayTimedResolution = null;
    private bool isGameFinished = false;          // Flag to track game completion

    // Time to display difficulty transition screens
    private const float DifficultyResolutionDelayTime = 5f;
    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        // Cache the animator parameter hash for better performance
        resStateParaHash = Animator.StringToHash("ScreenState");
        ValidateReferences();
        SetupOverlay();
    }

    void OnEnable()
    {
        SubscribeToEvents();
        SetupButtons();
    }

    void OnDisable()
    {
        CleanupCoroutines();
        UnsubscribeFromEvents();
        CleanupButtons();
    }

    void Start()
    {
        UpdateScoreUI();
        
        // Initialize UI elements
        if (uIElements.finishUIElements != null)
        {
            uIElements.finishUIElements.gameObject.SetActive(false);
        }
        
        if (uIElements.highScoreText != null)
        {
            uIElements.highScoreText.gameObject.SetActive(false);
        }
        
        if (uIElements.nextButton != null)
        {
            uIElements.nextButton.gameObject.SetActive(false);
        }
        
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(false);
        }
        
        isGameFinished = false;
    }
    #endregion

    #region Initialization and Setup
    private void ValidateReferences()
    {
        // Validate all required references are assigned - helps with debugging
        if (events == null)
        {
            Debug.LogError("GameEvents reference is null in UIManager! Please assign it in the inspector.");
        }
        
        if (answerPrefab == null)
        {
            Debug.LogError("Answer prefab is null in UIManager! Please assign it in the inspector.");
        }
        
        if (uIElements.answersContentArea == null)
        {
            Debug.LogError("AnswersContentArea is null in UIManager! Please assign it in the inspector.");
        }
        
        if (uIElements.questionInfoTextObject == null)
        {
            Debug.LogError("QuestionInfoTextObject is null in UIManager! Please assign it in the inspector.");
        }
        
        if (uIElements.resolutionScreenAnimator == null)
        {
            Debug.LogError("ResolutionScreenAnimator is null in UIManager! Please assign it in the inspector.");
        }
        
        if (uIElements.mainCanvasGroup == null)
        {
            Debug.LogError("MainCanvasGroup is null in UIManager! Please assign it in the inspector.");
        }
        
        if (uIElements.overlayImage == null)
        {
            Debug.LogWarning("OverlayImage is null in UIManager! The black background overlay won't work.");
        }
    }
    
    // Setup the black overlay for transitions between screens
    private void SetupOverlay()
    {
        if (uIElements.overlayImage != null)
        {
            // Set the color of the overlay image to black with the specified alpha
            Color overlayColor = parameters.OverlayColor;
            overlayColor.a = parameters.OverlayAlpha;
            uIElements.overlayImage.color = overlayColor;
            
            // Make sure the overlay is initially hidden
            uIElements.overlayImage.gameObject.SetActive(false);
            
            Debug.Log("Black overlay setup complete");
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to all necessary game events
        if (events != null)
        {
            events.UpdateQuestionUI += UpdateQuestionUI;
            events.DisplayResolutionScreen += DisplayResolution;
            events.ScoreUpdated += UpdateScoreUI;
            events.DisplayDifficultyIntro += HandleDifficultyIntro;
            events.HideDifficultyIntro += HideDifficultyIntro;
            Debug.Log("UIManager subscribed to GameEvents");
        }
        else
        {
            Debug.LogError("GameEvents reference is null in UIManager! Cannot subscribe to events.");
        }
    }

    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from events to prevent memory leaks
        if (events != null)
        {
            events.UpdateQuestionUI -= UpdateQuestionUI;
            events.DisplayResolutionScreen -= DisplayResolution;
            events.ScoreUpdated -= UpdateScoreUI;
            events.DisplayDifficultyIntro -= HandleDifficultyIntro;
            events.HideDifficultyIntro -= HideDifficultyIntro;
            Debug.Log("UIManager unsubscribed from GameEvents");
        }
    }

    private void CleanupCoroutines()
    {
        // Stop any running coroutines to prevent errors
        if (IE_DisplayTimedResolution != null)
        {
            StopCoroutine(IE_DisplayTimedResolution);
            IE_DisplayTimedResolution = null;
        }
    }

    private void SetupButtons()
    {
        // Setup button click listeners
        if (uIElements.playAgainButton != null)
        {
            uIElements.playAgainButton.onClick.RemoveAllListeners();
            uIElements.playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            Debug.Log("PlayAgain button set up");
        }
        
        if (uIElements.quitButton != null)
        {
            uIElements.quitButton.onClick.RemoveAllListeners();
            uIElements.quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("Quit button set up");
        }
        
        if (uIElements.nextButton != null)
        {
            uIElements.nextButton.onClick.RemoveAllListeners();
            uIElements.nextButton.onClick.AddListener(OnNextClicked);
            Debug.Log("Next button set up");
        }
    }
    
    private void CleanupButtons()
    {
        // Remove button listeners to prevent memory leaks
        if (uIElements.playAgainButton != null)
        {
            uIElements.playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
        
        if (uIElements.quitButton != null)
        {
            uIElements.quitButton.onClick.RemoveListener(OnQuitClicked);
        }
        
        if (uIElements.nextButton != null)
        {
            uIElements.nextButton.onClick.RemoveListener(OnNextClicked);
        }
    }
    #endregion

    #region Button Event Handlers
    void OnPlayAgainClicked()
    {
        Debug.Log("Play Again clicked");
        isGameFinished = false;
        
        if (uIElements.resolutionScreenAnimator != null)
        {
            uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 1);
        }
        
        if (uIElements.finishUIElements != null)
        {
            uIElements.finishUIElements.gameObject.SetActive(false);
        }
        
        if (uIElements.highScoreText != null)
        {
            uIElements.highScoreText.gameObject.SetActive(false);
        }
        
        // Hide the black overlay
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(false);
        }
        
        // Restart the quiz using the SceneLoader singleton
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.RestartQuiz();
        }
        else
        {
            Debug.LogError("SceneLoader instance not found!");
        }
    }
    
    void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        if (events != null && events.QuitGame != null)
        {
            events.QuitGame.Invoke();
        }
        else
        {
            // Fallback quit handling if event is not available
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }

    void OnNextClicked()
    {
        Debug.Log("Next button clicked");
        if (events != null && events.NextQuestion != null)
        {
            events.NextQuestion.Invoke();
            if (uIElements.nextButton != null)
            {
                uIElements.nextButton.gameObject.SetActive(false);
            }
            
            // Hide the black overlay when moving to next question
            if (uIElements.overlayImage != null)
            {
                uIElements.overlayImage.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("NextQuestion event is not assigned or has no listeners!");
        }
    }
    #endregion

    #region UI Update Methods
    // Updates the question display and creates answer options
    void UpdateQuestionUI(QuestionData question)
    {
        Debug.Log($"UpdateQuestionUI called. Question: {(question != null ? question.question : "null")}");

        if (isGameFinished || question == null)
        {
            Debug.LogWarning($"Cannot update UI. isGameFinished: {isGameFinished}, question: {(question == null ? "null" : "valid")}");
            return;
        }

        if (uIElements.questionInfoTextObject != null)
        {
            uIElements.questionInfoTextObject.text = question.question;
            Debug.Log($"Set question text to: {question.question}");
        }
        else
        {
            Debug.LogError("questionInfoTextObject is null!");
            return;
        }

        CreateAnswers(question);
    }

    // Displays various resolution screens (correct/incorrect/finish/difficulty)
    void DisplayResolution(ResolutionScreenType type, int score)
    {
        Debug.Log($"Displaying resolution screen: {type}, Score: {score}");
        if (uIElements.resolutionScreenAnimator == null || uIElements.mainCanvasGroup == null)
        {
            Debug.LogError("Required UI elements missing for resolution display!");
            return;
        }

        // Show the black overlay
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(true);
        }

        UpdateResUI(type, score);
        uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 2);
        uIElements.mainCanvasGroup.blocksRaycasts = false;

        CleanupCoroutines();

        if (type == ResolutionScreenType.Finish)
        {
            // Game is finished - show end game UI
            isGameFinished = true;
            uIElements.mainCanvasGroup.blocksRaycasts = true;
            
            if (uIElements.finishUIElements != null)
            {
                uIElements.finishUIElements.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Finish UI elements not found at game end!");
            }
            
            if (uIElements.playAgainButton != null)
            {
                uIElements.playAgainButton.interactable = true;
            }
            
            if (uIElements.quitButton != null)
            {
                uIElements.quitButton.interactable = true;
            }
        }
        else if (type == ResolutionScreenType.Easy || type == ResolutionScreenType.Medium || 
                 type == ResolutionScreenType.Hard || type == ResolutionScreenType.VeryHard)
        {
            // Displaying difficulty transition screen
            IE_DisplayTimedResolution = DisplayDifficultyTimedResolution();
            StartCoroutine(IE_DisplayTimedResolution);
        }
        else
        {
            // Displaying regular answer resolution screen
            IE_DisplayTimedResolution = DisplayTimedResolution();
            StartCoroutine(IE_DisplayTimedResolution);
            if (uIElements.nextButton != null)
            {
                uIElements.nextButton.gameObject.SetActive(true);
                uIElements.nextButton.interactable = true;
                Debug.Log("Next button activated");
            }
        }
    }

    // Coroutine to display resolution screen for a timed duration
    IEnumerator DisplayTimedResolution()
    {
        yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
        
        if (uIElements.resolutionScreenAnimator != null)
        {
            uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 1);
        }
        
        // Hide the black overlay after resolution display time
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(false);
        }
        
        if (uIElements.mainCanvasGroup != null)
        {
            uIElements.mainCanvasGroup.blocksRaycasts = true;
        }
    }

    // Coroutine to display difficulty intro screen for a timed duration
    IEnumerator DisplayDifficultyTimedResolution()
    {
        yield return new WaitForSeconds(DifficultyResolutionDelayTime);
        
        if (uIElements.resolutionScreenAnimator != null)
        {
            uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 1);
        }
        
        // Hide the black overlay after difficulty resolution display time
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(false);
        }
        
        if (uIElements.mainCanvasGroup != null)
        {
            uIElements.mainCanvasGroup.blocksRaycasts = true;
        }
    }

    // Handle difficulty level introduction screens
    void HandleDifficultyIntro(string difficultyText, Color difficultyColor)
    {
        Debug.Log($"Handling difficulty intro: {difficultyText}");
        
        CleanupCoroutines();
        
        ResolutionScreenType screenType = ResolutionScreenType.Easy;
        
        // Determine which difficulty screen to show
        if (difficultyText.Contains("EASY"))
        {
            screenType = ResolutionScreenType.Easy;
        }
        else if (difficultyText.Contains("MEDIUM"))
        {
            screenType = ResolutionScreenType.Medium;
        }
        else if (difficultyText.Contains("HARD") && !difficultyText.Contains("VERY"))
        {
            screenType = ResolutionScreenType.Hard;
        }
        else if (difficultyText.Contains("VERY HARD"))
        {
            screenType = ResolutionScreenType.VeryHard;
        }
        
        if (uIElements.mainCanvasGroup != null)
        {
            uIElements.mainCanvasGroup.alpha = 1f;
            uIElements.mainCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("MainCanvasGroup is null!");
        }
        
        // Show the black overlay
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(true);
        }
        
        UpdateResUI(screenType, 0);
        
        if (uIElements.resolutionScreenAnimator != null)
        {
            uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 2);
        }
        else
        {
            Debug.LogError("ResolutionScreenAnimator is null!");
        }
    }

    // Hide the difficulty intro screen
    public void HideDifficultyIntro()
    {
        Debug.Log("Hiding difficulty intro");
        if (uIElements.resolutionScreenAnimator != null)
        {
            uIElements.resolutionScreenAnimator.SetInteger(resStateParaHash, 1);
        }
        
        // Hide the black overlay
        if (uIElements.overlayImage != null)
        {
            uIElements.overlayImage.gameObject.SetActive(false);
        }
        
        if (uIElements.mainCanvasGroup != null)
        {
            uIElements.mainCanvasGroup.blocksRaycasts = true;
        }
    }

    // Update resolution UI based on the screen type
    void UpdateResUI(ResolutionScreenType type, int score)
    {
        if (uIElements.resolutionBG == null || uIElements.resolutionStateInfoText == null || 
            uIElements.resolutionScoreText == null)
        {
            Debug.LogError("Resolution UI elements missing!");
            return;
        }

        var highscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey);

        switch (type)
        {
            case ResolutionScreenType.Correct:
                uIElements.resolutionBG.color = parameters.CorrectBGColor;
                uIElements.resolutionStateInfoText.text = "CORRECT!";
                uIElements.resolutionScoreText.text = "+" + score;
                break;
            case ResolutionScreenType.Incorrect:
                uIElements.resolutionBG.color = parameters.IncorrectBGColor;
                uIElements.resolutionStateInfoText.text = "INCORRECT";
                uIElements.resolutionScoreText.text = ":(";
                break;
            case ResolutionScreenType.Easy:
                uIElements.resolutionBG.color = parameters.EasyScreenColor;
                uIElements.resolutionStateInfoText.text = "EASY MODE";
                uIElements.resolutionScoreText.text = "Round 1";
                break;
            case ResolutionScreenType.Medium:
                uIElements.resolutionBG.color = parameters.MediumScreenColor;
                uIElements.resolutionStateInfoText.text = "MEDIUM MODE";
                uIElements.resolutionScoreText.text = "Round 2";
                break;
            case ResolutionScreenType.Hard:
                uIElements.resolutionBG.color = parameters.HardScreenColor;
                uIElements.resolutionStateInfoText.text = "HARD MODE";
                uIElements.resolutionScoreText.text = "Round 3";
                break;
            case ResolutionScreenType.VeryHard:
                uIElements.resolutionBG.color = parameters.VeryHardScreenColor;
                uIElements.resolutionStateInfoText.text = "VERY HARD MODE";
                uIElements.resolutionScoreText.text = "Final Round";
                break;
            case ResolutionScreenType.Finish:
                uIElements.resolutionBG.color = parameters.FinalBGColor;
                uIElements.resolutionStateInfoText.text = "FINAL SCORE";
                StartCoroutine(CalculateScore());
                
                if (uIElements.finishUIElements != null)
                {
                    uIElements.finishUIElements.gameObject.SetActive(true);
                }
                
                if (uIElements.highScoreText != null)
                {
                    uIElements.highScoreText.gameObject.SetActive(true);
                    string newHighScorePrefix = (highscore > events.StartupHighscore) ? "<color=yellow>NEW </color>" : "";
                    uIElements.highScoreText.text = newHighScorePrefix + "HIGHSCORE: " + highscore;
                }
                break;
        }
    }

    // Animate the score counting up for visual effect
    IEnumerator CalculateScore()
    {
        if (uIElements.resolutionScoreText == null || events == null)
        {
            yield break;
        }

        var scoreValue = 0;
        while (scoreValue < events.CurrentFinalScore)
        {
            scoreValue++;
            uIElements.resolutionScoreText.text = scoreValue.ToString();
            yield return null;
        }
    }

    // Update the score display
    void UpdateScoreUI()
    {
        if (uIElements.scoreText == null)
        {
            Debug.LogWarning("Cannot update score UI: scoreText is null");
            return;
        }
        
        if (events == null)
        {
            Debug.LogWarning("Cannot update score UI: events is null");
            return;
        }

        uIElements.scoreText.text = "Score: " + events.CurrentFinalScore;
        Debug.Log($"Updated score UI to: {events.CurrentFinalScore}");
    }
    #endregion

    #region Answer Management
    // Create answer options for a question
    void CreateAnswers(QuestionData question)
    {
        Debug.Log($"Creating answers for question: {question.question}");
        EraseAnswers();

        if (answerPrefab == null)
        {
            Debug.LogError("answerPrefab is null!");
            return;
        }

        if (uIElements.answersContentArea == null)
        {
            Debug.LogError("answersContentArea is null!");
            return;
        }

        float offset = 0 - parameters.Margins;
        for (int i = 0; i < question.options.Length; i++)
        {
            Debug.Log($"Creating answer {i}: {question.options[i]}");
            GameObject answerInstance = Instantiate(answerPrefab, uIElements.answersContentArea);
            if (answerInstance == null)
            {
                Debug.LogError($"Failed to instantiate answer {i}");
                continue;
            }
            
            AnswerData newAnswer = answerInstance.GetComponent<AnswerData>();
            if (newAnswer != null)
            {
                newAnswer.UpdateData(question.options[i], i);
                newAnswer.Rect.anchoredPosition = new Vector2(0, offset);
                offset -= (newAnswer.Rect.sizeDelta.y + parameters.Margins);
                uIElements.answersContentArea.sizeDelta = new Vector2(uIElements.answersContentArea.sizeDelta.x, offset * -1);
                
                Toggle toggle = newAnswer.GetComponent<Toggle>();
                if (toggleGroup != null && toggle != null)
                {
                    toggle.group = toggleGroup;
                    Debug.Log($"Assigned toggle group to answer {i}");
                }
                
                currentAnswers.Add(newAnswer);
            }
            else
            {
                Debug.LogError("AnswerData component missing on answer instance!");
                Destroy(answerInstance);
            }
        }
    }

    // Remove all existing answer options
    void EraseAnswers()
    {
        Debug.Log($"Erasing {currentAnswers.Count} existing answers");
        foreach (var answer in currentAnswers)
        {
            if (answer != null)
            {
                Destroy(answer.gameObject);
            }
        }
        currentAnswers.Clear();
    }
    #endregion

    // Utility method to check if required references are set
    public bool CheckRequiredReferences()
    {
        return uIElements.mainCanvasGroup != null && uIElements.resolutionScreenAnimator != null;
    }
}