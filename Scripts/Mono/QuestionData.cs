using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestionData
{
    public string question;
    public string[] options;
    public string correctAnswer;
    public string difficulty; // Easy, Medium, Hard, VeryHard
    public AnswerType answerType = AnswerType.Single; // Single or Multi
    public bool useTimer = true;
    public int timer = 60; // Seconds
    public int addScore = 10; // Score per question

    public enum AnswerType
    {
        Single,
        Multi
    }

    // Mimic Question ScriptableObject methods
    public List<int> GetCorrectAnswers()
    {
        List<int> correctIndices = new List<int>();
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == correctAnswer)
            {
                correctIndices.Add(i);
            }
        }
        return correctIndices;
    }

    public AnswerType GetAnswerType => answerType;
}

[Serializable]
public class QuestionSet
{
    public QuestionData[] questions;
}