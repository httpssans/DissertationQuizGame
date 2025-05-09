// Author: Sanskar Bikram Kunwar, 2025
using System;
using System.Collections.Generic;
using UnityEngine;

// Serializable struct to store answer information
[Serializable()]
public struct Answer
{
    // The text content of the answer
    [SerializeField] private string _info;
    public string Info { get { return _info; } }

    // Boolean indicating if this answer is correct
    [SerializeField] private bool _isCorrect;
    public bool IsCorrect { get { return _isCorrect; } }
}

// ScriptableObject for creating question assets in the Unity editor
[CreateAssetMenu(fileName = "New Question", menuName = "Quiz/new Question")]
public class Question : ScriptableObject {

    // Enum to define question answer types (multiple correct answers or single correct answer)
    public enum                 AnswerType                  { Multi, Single }

    // The question text
    [SerializeField] private    String      _info           = String.Empty;
    public                      String      Info            { get { return _info; } }

    // Array of possible answers for this question
    [SerializeField]            Answer[]    _answers        = null;
    public                      Answer[]    Answers         { get { return _answers; } }

    //Parameters

    // Whether this question uses a timer
    [SerializeField] private    bool        _useTimer       = false;
    public                      bool        UseTimer        { get { return _useTimer; } }

    // The time limit for this question in seconds (only used if useTimer is true)
    [SerializeField] private    int         _timer          = 60;
    public                      int         Timer           { get { return _timer; } }

    // The answer type for this question (Single or Multi)
    [SerializeField] private    AnswerType  _answerType     = AnswerType.Multi;
    public                      AnswerType  GetAnswerType   { get { return _answerType; } }

    // The score value for this question
    [SerializeField] private    int         _addScore       = 10;
    public                      int         AddScore        { get { return _addScore; } }

    /// <summary>
    /// Function that is called to collect and return correct answers indexes.
    /// </summary>
    /// <returns>A list of indices that correspond to correct answers</returns>
    public List<int> GetCorrectAnswers ()
    {
        List<int> CorrectAnswers = new List<int>();
        for (int i = 0; i < Answers.Length; i++)
        {
            if (Answers[i].IsCorrect)
            {
                CorrectAnswers.Add(i);
            }
        }
        return CorrectAnswers;
    }
}