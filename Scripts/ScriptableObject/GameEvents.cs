
// Author: Sanskar Bikram Kunwar, 2025
using UnityEngine;

// ScriptableObject that serves as a centralized event system for the quiz game
// This allows decoupling of systems while maintaining communication between them
[CreateAssetMenu(fileName = "GameEvents", menuName = "Quiz/new GameEvents")]
public class GameEvents : ScriptableObject
{
    // Delegate and event for updating the UI with a new question
    public delegate void UpdateQuestionUICallback(QuestionData question);
    public UpdateQuestionUICallback UpdateQuestionUI = null;

    // Delegate and event for handling when a player picks an answer
    public delegate void UpdateQuestionAnswerCallback(PickedAnswerData pickedAnswer);
    public UpdateQuestionAnswerCallback UpdateQuestionAnswer = null;

    // Delegate and event for showing the resolution screen (success/failure)
    public delegate void DisplayResolutionScreenCallback(UIManager.ResolutionScreenType type, int score);
    public DisplayResolutionScreenCallback DisplayResolutionScreen = null;

    // Delegate and event for updating the score display
    public delegate void ScoreUpdatedCallback();
    public ScoreUpdatedCallback ScoreUpdated = null;
    
    // Delegate and event for restarting the game
    public delegate void RestartGameCallback();
    public RestartGameCallback RestartGame = null;
    
    // Delegate and event for quitting the game
    public delegate void QuitGameCallback();
    public QuitGameCallback QuitGame = null;
    
    // Delegate and event for displaying difficulty introduction screens
    public delegate void DisplayDifficultyIntroCallback(string difficultyText, Color difficultyColor);
    public DisplayDifficultyIntroCallback DisplayDifficultyIntro = null;
    
    // Delegate and event for hiding the difficulty intro
    public delegate void HideDifficultyIntroCallback();
    public HideDifficultyIntroCallback HideDifficultyIntro = null;
    
    // Delegate and event for proceeding to the next question
    public delegate void NextQuestionCallback();
    public NextQuestionCallback NextQuestion = null;

    // Fields to track the current score and startup highscore
    [HideInInspector]
    public int CurrentFinalScore = 0;
    [HideInInspector]
    public int StartupHighscore = 0;
}