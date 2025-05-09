/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * GameManager.cs
 * This script manages the game flow, including question selection, timer management,
 * scoring, and difficulty progression. It serves as the core controller for the quiz game.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Variables and Serialized Fields
    [Header("Core References")]
    [SerializeField] private GameEvents events = null;
    [SerializeField] private Animator timerAnimator = null;
    [SerializeField] private TMPro.TextMeshProUGUI timerText = null;
    
    [Header("Timer Settings")]
    [SerializeField] private Color timerHalfWayOutColor = Color.yellow;
    [SerializeField] private Color timerAlmostOutColor = Color.red;
    [SerializeField] private float postQuestionDelay = 1.5f;
    [SerializeField] private float difficultyIntroDelay = 2.0f;
    
    [Header("Audio Settings")]
    [SerializeField, Tooltip("Name of the music to play during the quiz")]
    private string quizMusicName = "GameMusic";
    [SerializeField, Tooltip("Name of the music to play during minigames")]
    private string minigameMusicName = "MinigameMusic";
    
    [Header("Difficulty Settings")]
    [SerializeField] private Difficulty difficulty = Difficulty.Easy;
    [SerializeField] private int questionsPerDifficulty = 25;
    
    [Header("Minigame Settings")]
    [SerializeField] private float minigameTimeLimit = 30f;
    
    public enum Difficulty { Easy, Medium, Hard, VeryHard }

    private Color timerDefaultColor = Color.white;
    private List<QuestionData> currentQuestions = null;
    private QuestionData currentQuestion = null;
    private float timeLimit = 60f;
    private float timer = 0f;
    private bool isFinished = false;
    private bool isTimerActive = false;
    private bool isLoadingQuestions = false;
    private int timerStateParamHash = 0;
    private IEnumerator timerCoroutine = null;
    private string currentSceneName => SceneManager.GetActiveScene().name;
    private bool isMinigameScene = false;
    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        // Ensure this GameManager is not persisted between scenes
        // Each scene should have its own GameManager instance
        InitializeGameManager();
    }
    
    private void Start()
    {
        LogDebug($"GameManager started in {currentSceneName}");
        LogGameEventsStatus();
        
        // Check if current scene is a minigame
        CheckIfMinigameScene();
        
        // Play the appropriate music for this scene
        PlaySceneMusic();
    }
    private void OnEnable()
    {
        RegisterEventHandlers();
    }

    private void OnDisable()
    {
        UnregisterEventHandlers();
    }
    
    private void OnDestroy()
    {
        LogDebug($"GameManager destroyed in {currentSceneName}");
    }

    private void Update()
    {
        UpdateTimer();
    }

    #endregion

    #region Initialization

    private void InitializeGameManager()
    {
        // Verify we're not in DontDestroyOnLoad
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            LogError("GameManager should not be in DontDestroyOnLoad! This is an implementation error.");
        }
        
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
        InitializeTimerComponents();
        timerStateParamHash = Animator.StringToHash("TimerState");
        
        LogDebug($"GameManager Awake in {currentSceneName}, difficulty: {difficulty}");
        LogComponentReferences();

        if (events != null)
        {
            events.CurrentFinalScore = GameStateManager.Instance.Score;
            events.StartupHighscore = GameStateManager.Instance.StartupHighscore;
        }
        
        StartCoroutine(LoadQuestions());
    }

    private void InitializeTimerComponents()
    {
        if (timerText != null)
        {
            timerDefaultColor = timerText.color;
        }
        else
        {
            LogWarning("timerText is not assigned in Inspector!");
        }
    }

    private void LogComponentReferences()
    {
        LogDebug($"Initial timerAnimator: {(timerAnimator != null ? timerAnimator.name : "null")}");
        LogDebug($"Initial timerText: {(timerText != null ? timerText.name : "null")}");
    }

    private void LogGameEventsStatus()
    {
        if (events != null)
        {
            LogDebug($"GameEvents reference is valid. UpdateQuestionUI has listeners: {events.UpdateQuestionUI != null}");
        }
        else
        {
            LogError("GameEvents reference is NULL!");
        }
    }

    private void RegisterEventHandlers()
    {
        if (events == null) return;
        
        events.UpdateQuestionAnswer += UpdateAnswers;
        events.RestartGame += ResetGame;
        events.QuitGame += QuitGame;
        events.NextQuestion += OnNextQuestion;
        LogDebug($"GameManager OnEnable in {currentSceneName}");
    }

    private void UnregisterEventHandlers()
    {
        if (events == null) return;
        
        events.UpdateQuestionAnswer -= UpdateAnswers;
        events.RestartGame -= ResetGame;
        events.QuitGame -= QuitGame;
        events.NextQuestion -= OnNextQuestion;
        LogDebug($"GameManager OnDisable in {currentSceneName}");
    }

    private void CheckIfMinigameScene()
    {
        // Check if the current scene is a minigame scene
        isMinigameScene = currentSceneName.StartsWith("Game", StringComparison.OrdinalIgnoreCase);
        LogDebug($"Current scene '{currentSceneName}' is minigame: {isMinigameScene}");
    }

    public void PlaySceneMusic()
    {
        if (AudioManager.Instance == null)
        {
            LogWarning("AudioManager instance is null!");
            return;
        }

        // Stop both music tracks first to ensure no overlap
        AudioManager.Instance.StopSound(quizMusicName);
        AudioManager.Instance.StopSound(minigameMusicName);
        
        // Play the appropriate music for this scene type
        if (isMinigameScene)
        {
            LogDebug($"Playing minigame music in {currentSceneName}");
            AudioManager.Instance.PlaySound(minigameMusicName);
        }
        else
        {
            LogDebug($"Playing quiz music in {currentSceneName}");
            AudioManager.Instance.PlaySound(quizMusicName);
        }
    }


    #endregion

    #region Question Loading and Management

    private IEnumerator LoadQuestions()
    {
        isLoadingQuestions = true;
        LogDebug($"Loading {difficulty} questions in {currentSceneName}...");

        if (QuestionGenerator.Instance == null)
        {
            LogError("QuestionGenerator instance is missing! Loading MainMenu.");
            LoadMainMenu();
            yield break;
        }

        QuestionSet questionSet = GetQuestionSetForDifficulty();

        if (IsQuestionSetEmpty(questionSet))
        {
            LogError($"No {difficulty} questions available in {currentSceneName}! Loading MainMenu.");
            LoadMainMenu();
            yield break;
        }

        currentQuestions = new List<QuestionData>();
        AddQuestions(questionSet, currentQuestions);

        LogDebug($"Loaded {difficulty} questions: {currentQuestions.Count} in {currentSceneName}");
        isLoadingQuestions = false;

        yield return new WaitForSeconds(0.5f);
        ShowDifficultyIntro();
    }

    private QuestionSet GetQuestionSetForDifficulty()
    {
        if (QuestionGenerator.Instance == null)
            return null;

        QuestionSet questionSet = null;
        switch (difficulty)
        {
            case Difficulty.Easy:
                questionSet = QuestionGenerator.Instance.easyQuestions;
                LogDebug($"Easy questions count: {questionSet?.questions?.Length ?? 0}");
                break;
            case Difficulty.Medium:
                questionSet = QuestionGenerator.Instance.mediumQuestions;
                LogDebug($"Medium questions count: {questionSet?.questions?.Length ?? 0}");
                break;
            case Difficulty.Hard:
                questionSet = QuestionGenerator.Instance.hardQuestions;
                LogDebug($"Hard questions count: {questionSet?.questions?.Length ?? 0}");
                break;
            case Difficulty.VeryHard:
                questionSet = QuestionGenerator.Instance.veryHardQuestions;
                LogDebug($"VeryHard questions count: {questionSet?.questions?.Length ?? 0}");
                break;
        }

        return questionSet;
    }

    private bool IsQuestionSetEmpty(QuestionSet questionSet)
    {
        return questionSet == null || questionSet.questions == null || questionSet.questions.Length == 0;
    }

    private void AddQuestions(QuestionSet questionSet, List<QuestionData> targetList)
    {
        if (questionSet == null || questionSet.questions == null)
        {
            LogWarning("Question set is null or empty!");
            return;
        }

        List<QuestionData> allQuestions = new List<QuestionData>(questionSet.questions);
        ShuffleQuestions(allQuestions);
        
        targetList.Clear();
        
        int questionsToAdd = Mathf.Min(questionsPerDifficulty, allQuestions.Count);
        for (int i = 0; i < questionsToAdd; i++)
        {
            targetList.Add(allQuestions[i]);
        }
        
        LogDebug($"Added {questionsToAdd} questions to list");
        
        EnsureEnoughQuestions(targetList);
    }

    public void EnsureCorrectMusicIsPlaying()
    {

        CheckIfMinigameScene();
        PlaySceneMusic();
    }
    private void EnsureEnoughQuestions(List<QuestionData> targetList)
    {
        if (targetList.Count < questionsPerDifficulty && targetList.Count > 0)
        {
            LogWarning($"Only {targetList.Count} questions available. Expected {questionsPerDifficulty}.");
            int originalCount = targetList.Count;
            
            while (targetList.Count < questionsPerDifficulty)
            {
                int indexToCopy = UnityEngine.Random.Range(0, originalCount);
                targetList.Add(targetList[indexToCopy]);
            }
            LogDebug($"Duplicated questions to reach {questionsPerDifficulty} total questions");
        }
    }

    private void ShuffleQuestions(List<QuestionData> questions)
    {
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            QuestionData temp = questions[i];
            questions[i] = questions[j];
            questions[j] = temp;
        }
    }

    private void SelectNewQuestion()
    {
        LogDebug($"Selecting new question. CurrentQuestions: {(currentQuestions != null ? currentQuestions.Count : 0)}, Index: {GameStateManager.Instance.CurrentQuestionIndex} in {currentSceneName}");

        if (currentQuestions == null || currentQuestions.Count == 0)
        {
            LogError($"No questions available to select in {currentSceneName}! Loading MainMenu.");
            LoadMainMenu();
            return;
        }

        EnsureValidQuestionIndex();

        currentQuestion = currentQuestions[GameStateManager.Instance.CurrentQuestionIndex];
        LogDebug($"Selected question: {(currentQuestion != null ? currentQuestion.question : "null")} in {currentSceneName}");

        if (currentQuestion == null)
        {
            LogError($"Current question is null in {currentSceneName}! Loading MainMenu.");
            LoadMainMenu();
            return;
        }

        ResetPickedAnswers();
        UpdateUI();
        SetupTimer();
    }

    private void EnsureValidQuestionIndex()
    {
        if (GameStateManager.Instance.CurrentQuestionIndex >= currentQuestions.Count)
        {
            LogWarning($"Question index out of range! Index: {GameStateManager.Instance.CurrentQuestionIndex}, Count: {currentQuestions.Count} in {currentSceneName}");
            GameStateManager.Instance.CurrentQuestionIndex = 0;
        }
    }

    private void ResetPickedAnswers()
    {
        GameStateManager.Instance.PickedAnswers.Clear();
    }

    private void UpdateUI()
    {
        if (events.UpdateQuestionUI != null)
        {
            LogDebug($"Invoking UpdateQuestionUI event in {currentSceneName}");
            events.UpdateQuestionUI.Invoke(currentQuestion);
        }
        else
        {
            LogError($"UpdateQuestionUI event is not assigned or has no listeners in {currentSceneName}!");
        }
    }

    private void UpdateAnswers(PickedAnswerData newAnswer)
    {
        LogDebug($"Received answer index: {newAnswer.AnswerIndex} in {currentSceneName}");
        
        if (!GameStateManager.Instance.PickedAnswers.Exists(x => x.AnswerIndex == newAnswer.AnswerIndex))
        {
            GameStateManager.Instance.PickedAnswers.Add(newAnswer);
        }
        else
        {
            GameStateManager.Instance.PickedAnswers.RemoveAll(x => x.AnswerIndex == newAnswer.AnswerIndex);
        }
    }

    private void OnNextQuestion()
    {
        if (!isFinished && GameStateManager.Instance.QuestionsAnsweredInCurrentDifficulty < questionsPerDifficulty)
        {
            LogDebug($"Next question requested in {currentSceneName}");
            GameStateManager.Instance.CurrentQuestionIndex++;
            SelectNewQuestion();
        }
    }

    #endregion

    #region Timer Management

    private void SetupTimer()
    {
        if (currentQuestion.useTimer)
        {
            StopTimerCoroutine();
            timeLimit = currentQuestion.timer;
            timer = timeLimit;
            timerCoroutine = StartTimer();
            StartCoroutine(timerCoroutine);
            isTimerActive = true;
            UpdateTimerDisplay();
            
            if (timerAnimator != null)
            {
                timerAnimator.SetInteger(timerStateParamHash, 2);
            }
        }
        else
        {
            isTimerActive = false;
            if (timerText != null)
            {
                timerText.text = "";
            }
            
            if (timerAnimator != null)
            {
                timerAnimator.SetInteger(timerStateParamHash, 0);
            }
        }
    }

    private void UpdateTimer()
    {
        if (isTimerActive)
        {
            timer -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timer <= 0f)
            {
                isTimerActive = false;
                Accept();
            }
        }
    }

    private void StopTimerCoroutine()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator StartTimer()
    {
        if (timerText != null)
        {
            timerText.color = timerDefaultColor;
        }
        
        int lastSecond = Mathf.CeilToInt(timer);
        
        while (timer > 0)
        {
            yield return null;
            int currentSecond = Mathf.CeilToInt(timer);
            
            if (currentSecond != lastSecond)
            {
                lastSecond = currentSecond;
                PlayCountdownSoundIfNeeded(currentSecond);
            }
        }
    }

    private void PlayCountdownSoundIfNeeded(int currentSecond)
    {
        if (AudioManager.Instance != null && currentSecond <= 5)
        {
            AudioManager.Instance.PlaySound("CountdownSFX");
        }
    }

    private void UpdateTimerDisplay()
    {
        if (!isTimerActive || timerText == null)
        {
            return;
        }

        float timeRemaining = Mathf.Max(0, timer);
        timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

        UpdateTimerColor(timeRemaining);
    }

    private void UpdateTimerColor(float timeRemaining)
    {
        if (timerText == null) return;
        
        if (timeRemaining <= timeLimit / 4)
        {
            timerText.color = timerAlmostOutColor;
        }
        else if (timeRemaining <= timeLimit / 2)
        {
            timerText.color = timerHalfWayOutColor;
        }
        else
        {
            timerText.color = timerDefaultColor;
        }
    }

    #endregion

    #region Answer Checking and Scoring

    public void Accept()
    {
        if (isTimerActive)
        {
            isTimerActive = false;
            StopTimerCoroutine();
            if (timerAnimator != null)
            {
                timerAnimator.SetInteger(timerStateParamHash, 1);
            }
        }
        CheckAnswers();
    }

    private void CheckAnswers()
    {
        if (!isFinished && currentQuestion != null)
        {
            LogDebug($"Checking answers, Correct: {string.Join(", ", currentQuestion.GetCorrectAnswers())}, Picked: {string.Join(", ", GameStateManager.Instance.PickedAnswers.ConvertAll(x => x.AnswerIndex))} in {currentSceneName}");
            bool isCorrect = IsAnswerCorrect(currentQuestion.GetCorrectAnswers(), GameStateManager.Instance.PickedAnswers);
    
            UIManager.ResolutionScreenType type = isCorrect ? UIManager.ResolutionScreenType.Correct : UIManager.ResolutionScreenType.Incorrect;
            int scoreChange = isCorrect ? currentQuestion.addScore : -currentQuestion.addScore;

            PlayAnswerSound(isCorrect);
            UpdateScore(scoreChange);
            ShowResolutionScreen(type, scoreChange);

            GameStateManager.Instance.QuestionsAnsweredInCurrentDifficulty++;
            
            HandleQuestionProgress();
        }
        else if (isFinished)
        {
            LogDebug($"Game is already finished in {currentSceneName}.");
        }
        else
        {
            LogError($"Current question is null in {currentSceneName}!");
        }
    }
    
    private bool IsAnswerCorrect(List<int> correctAnswers, List<PickedAnswerData> pickedAnswers)
    {
        if (correctAnswers.Count != pickedAnswers.Count)
        {
            return false;
        }
        
        HashSet<int> pickedIndices = new HashSet<int>();
        foreach (var answer in pickedAnswers)
        {
            pickedIndices.Add(answer.AnswerIndex);
        }
        
        foreach (int correctIndex in correctAnswers)
        {
            if (!pickedIndices.Contains(correctIndex))
            {
                return false;
            }
        }
        
        return true;
    }

    private void PlayAnswerSound(bool isCorrect)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(isCorrect ? "CorrectSFX" : "IncorrectSFX");
        }
        else
        {
            LogWarning("AudioManager instance is null.");
        }
    }

    private void ShowResolutionScreen(UIManager.ResolutionScreenType type, int scoreChange)
    {
        if (events.DisplayResolutionScreen != null)
        {
            LogDebug($"Triggering DisplayResolutionScreen: {type}, ScoreChange: {Mathf.Abs(scoreChange)} in {currentSceneName}");
            events.DisplayResolutionScreen(type, Mathf.Abs(scoreChange));
        }
    }

    private void HandleQuestionProgress()
    {
        if (GameStateManager.Instance.QuestionsAnsweredInCurrentDifficulty >= questionsPerDifficulty)
        {
            LogDebug($"All questions completed for {difficulty} difficulty in {currentSceneName}.");
            Invoke(nameof(OnDifficultyComplete), postQuestionDelay);
        }
        else
        {
            GameStateManager.Instance.CurrentQuestionIndex++;
            Invoke(nameof(SelectNewQuestion), postQuestionDelay);
        }
    }

    private void UpdateScore(int scoreChange)
    {
        GameStateManager.Instance.Score += scoreChange;
        GameStateManager.Instance.Score = Mathf.Max(0, GameStateManager.Instance.Score);
        
        if (events != null)
        {
            events.CurrentFinalScore = GameStateManager.Instance.Score;
            if (events.ScoreUpdated != null)
            {
                LogDebug($"Invoking ScoreUpdated event in {currentSceneName}");
                events.ScoreUpdated.Invoke();
            }
        }
    }

    #endregion

    #region Difficulty Management

    private void ShowDifficultyIntro()
    {
        if (isLoadingQuestions)
        {
            LogWarning($"Attempted to show difficulty intro while still loading questions in {currentSceneName}!");
            return;
        }

        LogDebug($"Showing {difficulty} difficulty intro in {currentSceneName}");
        
        DifficultySettings settings = GetDifficultySettings();
        
        SetupGameStateForDifficulty();
        ResetTimerDisplay();

        DisplayDifficultyIntro(settings);
        StartCoroutine(StartQuestionsAfterIntro());
    }

    private DifficultySettings GetDifficultySettings()
    {
        DifficultySettings settings = new DifficultySettings();
        
        switch (difficulty)
        {
            case Difficulty.Easy:
                settings.screenType = UIManager.ResolutionScreenType.Easy;
                settings.text = "EASY MODE";
                settings.color = Color.green;
                break;
            case Difficulty.Medium:
                settings.screenType = UIManager.ResolutionScreenType.Medium;
                settings.text = "MEDIUM MODE";
                settings.color = Color.yellow;
                break;
            case Difficulty.Hard:
                settings.screenType = UIManager.ResolutionScreenType.Hard;
                settings.text = "HARD MODE";
                settings.color = Color.red;
                break;
            case Difficulty.VeryHard:
                settings.screenType = UIManager.ResolutionScreenType.VeryHard;
                settings.text = "VERY HARD MODE";
                settings.color = new Color(0.5f, 0f, 0.5f);
                break;
        }

        return settings;
    }

    private void SetupGameStateForDifficulty()
    {
        GameStateManager.Instance.CurrentDifficulty = difficulty;
        GameStateManager.Instance.CurrentQuestionIndex = 0;
        GameStateManager.Instance.QuestionsAnsweredInCurrentDifficulty = 0;
    }

    private void ResetTimerDisplay()
    {
        isTimerActive = false;
        StopTimerCoroutine();
        if (timerAnimator != null)
        {
            timerAnimator.SetInteger(timerStateParamHash, 0);
        }
    }

    private void DisplayDifficultyIntro(DifficultySettings settings)
    {
        if (events.DisplayDifficultyIntro != null)
        {
            LogDebug($"Triggering DisplayDifficultyIntro event with text: {settings.text} in {currentSceneName}");
            events.DisplayDifficultyIntro(settings.text, settings.color);
        }
        else
        {
            LogWarning("DisplayDifficultyIntro event is not set up!");
            FallbackToResolutionScreen(settings.screenType);
        }
    }

    private void FallbackToResolutionScreen(UIManager.ResolutionScreenType screenType)
    {
        if (events.DisplayResolutionScreen != null)
        {
            LogDebug($"Falling back to DisplayResolutionScreen event with type: {screenType} in {currentSceneName}");
            events.DisplayResolutionScreen(screenType, 0);
        }
        else
        {
            LogError("Both DisplayDifficultyIntro and DisplayResolutionScreen events are not set up!");
            SelectNewQuestion();
        }
    }

    private IEnumerator StartQuestionsAfterIntro()
    {
        yield return new WaitForSeconds(difficultyIntroDelay);
        
        if (events.HideDifficultyIntro != null)
        {
            LogDebug($"Triggering HideDifficultyIntro event in {currentSceneName}");
            events.HideDifficultyIntro();
        }
        else
        {
            LogWarning("HideDifficultyIntro event is not set up!");
        }
        
        LogDebug($"Starting new question selection in {currentSceneName}");
        SelectNewQuestion();
    }

    private void OnDifficultyComplete()
    {
        LogDebug($"{difficulty} difficulty completed in {currentSceneName}. Notifying SceneLoader...");
        SceneLoader sceneLoader = FindFirstObjectByType<SceneLoader>();
        
        if (difficulty == Difficulty.VeryHard) // This is stage4
        {
            // Show end game resolution screen instead of loading the next scene
            EndGame();
        }
        else if (sceneLoader != null)
        {
            sceneLoader.OnDifficultyCompleted(difficulty.ToString().ToLower());
        }
        else
        {
            LogError("SceneLoader not found!");
            LoadMainMenu();
        }
    }

    #endregion

    #region Game Flow

    private void EndGame()
    {
        isFinished = true;
        isTimerActive = false;
        StopTimerCoroutine();

        PlayGameOverSound();
        UpdateHighscoreAndScore();
        ShowGameOverScreen();
    }

    private void PlayGameOverSound()
    {
        if (AudioManager.Instance != null)
        {
            // Stop all game music before playing game over sound
            AudioManager.Instance.StopSound(quizMusicName);
            AudioManager.Instance.StopSound(minigameMusicName);
            AudioManager.Instance.PlaySound("GameOverSFX");
        }
        else
        {
            LogWarning("AudioManager instance not found!");
        }
    }

    private void UpdateHighscoreAndScore()
    {
        if (events != null)
        {
            events.CurrentFinalScore = GameStateManager.Instance.Score;
        }
        GameStateManager.Instance.UpdateHighscore();
    }

    private void ShowGameOverScreen()
    {
        if (events != null && events.DisplayResolutionScreen != null)
        {
            LogDebug($"Triggering DisplayResolutionScreen: Finish, Score: {GameStateManager.Instance.Score} in {currentSceneName}");
            events.DisplayResolutionScreen(UIManager.ResolutionScreenType.Finish, GameStateManager.Instance.Score);
        }
    }

    public void ReturnToMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            // Stop all music when returning to main menu
            AudioManager.Instance.StopSound(quizMusicName);
            AudioManager.Instance.StopSound(minigameMusicName);
        }
    }

    public void ResetGame()
    {
        LogDebug($"Resetting game in {currentSceneName}...");
        
        isFinished = false;
        GameStateManager.Instance.ResetGameState();
        
        if (events != null)
        {
            events.CurrentFinalScore = GameStateManager.Instance.Score;
        }

        isTimerActive = false;
        if (timerAnimator != null)
        {
            timerAnimator.SetInteger(timerStateParamHash, 0);
        }

        // Make sure appropriate music is playing for this scene type
        PlaySceneMusic();

        StartCoroutine(LoadQuestions());
    }

    public void QuitGame()
    {
        LogDebug($"Quitting game in {currentSceneName}...");
        
        if (AudioManager.Instance != null)
        {
            // Stop all music when quitting
            AudioManager.Instance.StopSound(quizMusicName);
            AudioManager.Instance.StopSound(minigameMusicName);
        }
        
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public float GetSceneTimeLimit()
    {
        return minigameTimeLimit;
    }

    private void LoadMainMenu()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
        }
        else
        {
            LogError("SceneLoader instance is null!");
        }
    }

    #endregion

    #region Utility Classes and Methods

    private class DifficultySettings
    {
        public UIManager.ResolutionScreenType screenType;
        public string text;
        public Color color;
    }

    private void LogDebug(string message)
    {
        Debug.Log(message);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    #endregion
}