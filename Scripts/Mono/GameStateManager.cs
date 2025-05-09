using UnityEngine;
using System.Collections.Generic;

public class GameStateManager : MonoBehaviour
{
    private int score = 0;
    private int currentQuestionIndex = 0;
    private int questionsAnsweredInCurrentDifficulty = 0;
    private GameManager.Difficulty currentDifficulty = GameManager.Difficulty.Easy;
    private List<PickedAnswerData> pickedAnswers = new List<PickedAnswerData>();
    private int startupHighscore = 0;
    private static GameStateManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameStateManager instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("Duplicate GameStateManager found, destroying this instance");
            Destroy(gameObject);
            return; // Added return to ensure code execution stops if this is a duplicate
        }

        startupHighscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey, 0);
        Debug.Log($"Initialized with StartupHighscore: {startupHighscore}");
    }

    public static GameStateManager Instance
    {
        get { return instance; }
    }

    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            Debug.Log($"Score updated to: {score}");
        }
    }

    public int CurrentQuestionIndex
    {
        get { return currentQuestionIndex; }
        set
        {
            currentQuestionIndex = value;
            Debug.Log($"CurrentQuestionIndex updated to: {currentQuestionIndex}");
        }
    }

    public int QuestionsAnsweredInCurrentDifficulty
    {
        get { return questionsAnsweredInCurrentDifficulty; }
        set
        {
            questionsAnsweredInCurrentDifficulty = value;
            Debug.Log($"QuestionsAnsweredInCurrentDifficulty updated to: {questionsAnsweredInCurrentDifficulty}");
        }
    }

    public GameManager.Difficulty CurrentDifficulty
    {
        get { return currentDifficulty; }
        set
        {
            currentDifficulty = value;
            Debug.Log($"CurrentDifficulty updated to: {currentDifficulty}");
        }
    }

    public List<PickedAnswerData> PickedAnswers
    {
        get { return pickedAnswers; }
    }

    public int StartupHighscore
    {
        get { return startupHighscore; }
        set
        {
            startupHighscore = value;
            Debug.Log($"StartupHighscore updated to: {startupHighscore}");
        }
    }

    public void ResetGameState()
    {
        score = 0;
        currentQuestionIndex = 0;
        questionsAnsweredInCurrentDifficulty = 0;
        currentDifficulty = GameManager.Difficulty.Easy;
        pickedAnswers.Clear();
        startupHighscore = PlayerPrefs.GetInt(GameUtility.SavePrefKey, 0);
        Debug.Log("Game state reset");
    }

    public void UpdateHighscore()
    {
        int highScore = PlayerPrefs.GetInt(GameUtility.SavePrefKey, 0);
        if (score > highScore)
        {
            PlayerPrefs.SetInt(GameUtility.SavePrefKey, score);
            PlayerPrefs.Save();
            startupHighscore = score;
            Debug.Log($"New highscore set: {score}");
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to MainMenu");
        if (SceneLoader.Instance != null)
        {
            // Fixed: Use the constant from SceneLoader instead of hardcoded string
            // This will make sure we're using the same scene name that's defined in SceneLoader
            SceneLoader.Instance.LoadScene(SceneLoader.Instance.GetMainMenuSceneName());
        }
        else
        {
            Debug.LogError("SceneLoader not found! Loading MainMenu directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}