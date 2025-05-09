/*
 * Author: Sanskar Bikram Kunwar, 2025
 * 
 * QuestionGenerator.cs
 * 
 * This script is responsible for loading and generating trivia questions for the quiz game.
 * It can either load questions from a local JSON file or generate new questions using the OpenAI API.
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class QuestionGenerator : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private bool loadFromAPI = false;                   // Toggle to use OpenAI API instead of local JSON
    [SerializeField] private string openAiApiKey = "YOUR_API_KEY_HERE";  // API key for OpenAI
    [SerializeField] private bool fallbackToLocalOnApiFailure = true;    // Fallback to local JSON if API fails
    [SerializeField] private int minQuestionsPerDifficulty = 25;         // Minimum required questions per difficulty level
    #endregion

    #region Public Properties
    public static QuestionGenerator Instance;  // Singleton instance
    public QuestionSet easyQuestions;          // Collection of easy difficulty questions
    public QuestionSet mediumQuestions;        // Collection of medium difficulty questions
    public QuestionSet hardQuestions;          // Collection of hard difficulty questions
    public QuestionSet veryHardQuestions;      // Collection of very hard difficulty questions
    #endregion

    #region Initialization
    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("QuestionGenerator instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("Duplicate QuestionGenerator found, destroying this instance");
            Destroy(gameObject);
            return;
        }

        // Load default questions on startup
        GenerateQuestionsFromJSON("default", null);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Main entry point for generating questions based on a topic
    /// </summary>
    /// <param name="topic">The topic for questions</param>
    /// <param name="onComplete">Callback when generation is complete</param>
    public void GenerateQuestions(string topic, Action onComplete)
    {
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogError("Topic cannot be empty!");
            onComplete?.Invoke();
            return;
        }

        Debug.Log($"Generating questions for topic: {topic}");
        if (loadFromAPI)
        {
            StartCoroutine(GenerateQuestionsFromAPICoroutine(topic, onComplete));
        }
        else
        {
            GenerateQuestionsFromJSON(topic, onComplete);
        }
    }

    /// <summary>
    /// Check if sufficient questions are loaded for all difficulty levels
    /// </summary>
    /// <returns>True if all question categories meet minimum requirements</returns>
    public bool AreQuestionsLoaded()
    {
        return easyQuestions != null && easyQuestions.questions.Length >= minQuestionsPerDifficulty &&
               mediumQuestions != null && mediumQuestions.questions.Length >= minQuestionsPerDifficulty &&
               hardQuestions != null && hardQuestions.questions.Length >= minQuestionsPerDifficulty &&
               veryHardQuestions != null && veryHardQuestions.questions.Length >= minQuestionsPerDifficulty;
    }
    #endregion

    #region Local JSON Processing
    /// <summary>
    /// Load questions from a local JSON file in Resources folder
    /// </summary>
    /// <param name="topic">Topic for questions (not used in local JSON loading)</param>
    /// <param name="onComplete">Callback when loading is complete</param>
    private void GenerateQuestionsFromJSON(string topic, Action onComplete)
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Questions");
        if (jsonText == null)
        {
            Debug.LogError("Questions.json not found in Assets/Resources!");
            onComplete?.Invoke();
            return;
        }

        try
        {
            Debug.Log($"Loaded Questions.json (first 200 chars): {jsonText.text.Substring(0, Mathf.Min(200, jsonText.text.Length))}...");
            JArray questions = JArray.Parse(jsonText.text);
            var easy = new List<QuestionData>();
            var medium = new List<QuestionData>();
            var hard = new List<QuestionData>();
            var veryHard = new List<QuestionData>();

            foreach (JObject q in questions)
            {
                string[] options = q["options"]?.ToObject<string[]>();
                string correctAnswer = q["correctAnswer"]?.ToString();
                string questionText = q["question"]?.ToString();

                if (options == null || options.Length < 4)
                {
                    Debug.LogWarning($"Invalid or insufficient options for question: {questionText ?? "null"}");
                    continue;
                }
                if (string.IsNullOrEmpty(correctAnswer))
                {
                    Debug.LogWarning($"Missing correctAnswer for question: {questionText ?? "null"}");
                    continue;
                }
                if (string.IsNullOrEmpty(questionText))
                {
                    Debug.LogWarning($"Missing question text for question: {q.ToString()}");
                    continue;
                }

                int correctIndex = Array.IndexOf(options, correctAnswer);
                if (correctIndex < 0)
                {
                    Debug.LogWarning($"Correct answer '{correctAnswer}' not found in options for question: {questionText}");
                    continue;
                }

                string[] shuffledOptions = ShuffleArray(options);
                int newCorrectIndex = Array.IndexOf(shuffledOptions, correctAnswer);

                QuestionData question = new QuestionData
                {
                    question = questionText,
                    options = shuffledOptions,
                    correctAnswer = correctAnswer,
                    difficulty = q["difficulty"]?.ToString() ?? "Easy",
                    answerType = q["answerType"]?.ToString() == "Multi" ? QuestionData.AnswerType.Multi : QuestionData.AnswerType.Single,
                    useTimer = q["useTimer"]?.ToObject<bool>() ?? true,
                    timer = q["timer"]?.ToObject<int>() ?? 30,
                    addScore = q["addScore"]?.ToObject<int>() ?? 10
                };

                Debug.Log($"Parsed question: {questionText}, Options: [{string.Join(", ", shuffledOptions)}], Correct: {correctAnswer}");

                switch (question.difficulty)
                {
                    case "Easy": easy.Add(question); break;
                    case "Medium": medium.Add(question); break;
                    case "Hard": hard.Add(question); break;
                    case "VeryHard": veryHard.Add(question); break;
                    default:
                        Debug.LogWarning($"Unknown difficulty '{question.difficulty}' for question: {questionText}");
                        break;
                }
            }

            easyQuestions = new QuestionSet { questions = easy.ToArray() };
            mediumQuestions = new QuestionSet { questions = medium.ToArray() };
            hardQuestions = new QuestionSet { questions = hard.ToArray() };
            veryHardQuestions = new QuestionSet { questions = veryHard.ToArray() };

            Debug.Log($"Loaded {easy.Count} Easy, {medium.Count} Medium, {hard.Count} Hard, {veryHard.Count} VeryHard questions from JSON.");

            // Validate question counts
            if (easy.Count < minQuestionsPerDifficulty || medium.Count < minQuestionsPerDifficulty ||
                hard.Count < minQuestionsPerDifficulty || veryHard.Count < minQuestionsPerDifficulty)
            {
                Debug.LogWarning($"Insufficient questions: Easy={easy.Count}, Medium={medium.Count}, Hard={hard.Count}, VeryHard={veryHard.Count}. Minimum required: {minQuestionsPerDifficulty} per difficulty.");
            }

            onComplete?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Parsing Error: {e.Message}");
            easyQuestions = mediumQuestions = hardQuestions = veryHardQuestions = new QuestionSet { questions = new QuestionData[0] };
            onComplete?.Invoke();
        }
    }
    #endregion

    #region Utility Functions
    /// <summary>
    /// Shuffle array elements using Fisher-Yates shuffle algorithm
    /// </summary>
    /// <typeparam name="T">Type of array elements</typeparam>
    /// <param name="array">Array to shuffle</param>
    /// <returns>A new shuffled array</returns>
    private T[] ShuffleArray<T>(T[] array)
    {
        T[] newArray = (T[])array.Clone();
        int n = newArray.Length;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T temp = newArray[k];
            newArray[k] = newArray[n];
            newArray[n] = temp;
        }
        return newArray;
    }

    /// <summary>
    /// Extract JSON content from a string that might contain markdown code blocks
    /// </summary>
    /// <param name="response">Response text potentially containing JSON in markdown blocks</param>
    /// <returns>Extracted JSON content</returns>
    private string ExtractJsonFromResponse(string response)
    {
        if (response.Contains("```json"))
        {
            int startIndex = response.IndexOf("```json") + 7;
            int endIndex = response.IndexOf("```", startIndex);
            if (endIndex > startIndex)
            {
                return response.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        else if (response.Contains("```"))
        {
            int startIndex = response.IndexOf("```") + 3;
            int endIndex = response.IndexOf("```", startIndex);
            if (endIndex > startIndex)
            {
                return response.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        
        return response.Trim();
    }
    #endregion

    #region OpenAI API Integration
    /// <summary>
    /// Generate questions by calling OpenAI's API with the specified topic
    /// </summary>
    /// <param name="topic">Topic for question generation</param>
    /// <param name="onComplete">Callback when generation is complete</param>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator GenerateQuestionsFromAPICoroutine(string topic, Action onComplete)
    {
        // Validate API key
        if (string.IsNullOrEmpty(openAiApiKey) || openAiApiKey == "YOUR_API_KEY_HERE")
        {
            Debug.LogError("OpenAI API key is invalid or unset. Falling back to JSON.");
            if (fallbackToLocalOnApiFailure)
            {
                GenerateQuestionsFromJSON(topic, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
            yield break;
        }

        Debug.Log($"Generating questions from OpenAI API for topic: {topic}");

        // Create prompt for OpenAI API
        string prompt = $@"Generate 100 unique multiple-choice questions on the topic: {topic}. 
                        Split them into 4 difficulty levels:
                        - 25 Easy
                        - 25 Medium
                        - 25 Hard
                        - 25 VeryHard

                        Each question must be a unique object with these fields:
                        - question (string)
                        - options (array of 4 distinct answer choices)
                        - correctAnswer (string, must match one of the options)
                        - difficulty (string: Easy, Medium, Hard, or VeryHard)
                        - answerType (string: Single)
                        - useTimer (boolean: always true)
                        - timer (integer: 60)
                        - addScore (integer: 10 for Easy, 15 for Medium, 20 for Hard, 25 for VeryHard)

                        Format the output as a single JSON array of 100 objects with exactly 25 questions of each difficulty level.
                        Ensure valid JSON and no duplicate questions.";

        // Prepare API request body
        JObject requestBody = new JObject
        {
            { "model", "gpt-4o" },
            { "messages", new JArray(
                new JObject { { "role", "user" }, { "content", prompt } }
            )},
            { "max_tokens", 10000 }
        };

        // Send API request
        using (UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {openAiApiKey}");
            request.SetRequestHeader("Content-Type", "application/json");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            // Check for API errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API Error: {request.error}, Response: {(request.downloadHandler?.text ?? "No response")}");
                if (fallbackToLocalOnApiFailure)
                {
                    Debug.Log("API request failed. Falling back to JSON.");
                    GenerateQuestionsFromJSON(topic, onComplete);
                }
                else
                {
                    onComplete?.Invoke();
                }
                yield break;
            }

            // Check for empty response
            if (string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                Debug.LogError("API returned empty response.");
                if (fallbackToLocalOnApiFailure)
                {
                    Debug.Log("Empty API response. Falling back to JSON.");
                    GenerateQuestionsFromJSON(topic, onComplete);
                }
                else
                {
                    onComplete?.Invoke();
                }
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log($"API Response (first 100 chars): {responseText.Substring(0, Mathf.Min(100, responseText.Length))}...");

            try
            {
                // Parse API response
                JObject response = JObject.Parse(responseText);
                if (response["choices"] == null || !response["choices"].HasValues)
                {
                    Debug.LogError("API response missing 'choices' array.");
                    if (fallbackToLocalOnApiFailure)
                    {
                        Debug.Log("Invalid API response. Falling back to JSON.");
                        GenerateQuestionsFromJSON(topic, onComplete);
                    }
                    else
                    {
                        onComplete?.Invoke();
                    }
                    yield break;
                }

                string questionsJson = response["choices"][0]["message"]["content"]?.ToString();
                if (string.IsNullOrEmpty(questionsJson))
                {
                    Debug.LogError("API response missing content.");
                    if (fallbackToLocalOnApiFailure)
                    {
                        Debug.Log("Empty questions JSON. Falling back to JSON.");
                        GenerateQuestionsFromJSON(topic, onComplete);
                    }
                    else
                    {
                        onComplete?.Invoke();
                    }
                    yield break;
                }

                // Extract JSON content from response that might contain markdown
                questionsJson = ExtractJsonFromResponse(questionsJson);
                JArray questions = JArray.Parse(questionsJson);

                var easy = new List<QuestionData>();
                var medium = new List<QuestionData>();
                var hard = new List<QuestionData>();
                var veryHard = new List<QuestionData>();

                // Process each question from API response
                foreach (JObject q in questions)
                {
                    string[] options = q["options"]?.ToObject<string[]>();
                    string correctAnswer = q["correctAnswer"]?.ToString();
                    string questionText = q["question"]?.ToString();

                    // Validate question data
                    if (options == null || options.Length < 4)
                    {
                        Debug.LogWarning($"Invalid or insufficient options for question: {questionText ?? "null"}");
                        continue;
                    }
                    if (string.IsNullOrEmpty(correctAnswer))
                    {
                        Debug.LogWarning($"Missing correctAnswer for question: {questionText ?? "null"}");
                        continue;
                    }
                    if (string.IsNullOrEmpty(questionText))
                    {
                        Debug.LogWarning($"Missing question text for question: {q.ToString()}");
                        continue;
                    }

                    int correctIndex = Array.IndexOf(options, correctAnswer);
                    if (correctIndex < 0)
                    {
                        Debug.LogWarning($"Correct answer '{correctAnswer}' not found in options for question: {questionText}");
                        continue;
                    }

                    // Shuffle options to randomize answer positions
                    string[] shuffledOptions = ShuffleArray(options);
                    int newCorrectIndex = Array.IndexOf(shuffledOptions, correctAnswer);

                    // Create QuestionData object
                    QuestionData question = new QuestionData
                    {
                        question = questionText,
                        options = shuffledOptions,
                        correctAnswer = correctAnswer,
                        difficulty = q["difficulty"]?.ToString() ?? "Easy",
                        answerType = q["answerType"]?.ToString() == "Multi" ? QuestionData.AnswerType.Multi : QuestionData.AnswerType.Single,
                        useTimer = q["useTimer"]?.ToObject<bool>() ?? true,
                        timer = q["timer"]?.ToObject<int>() ?? 30,
                        addScore = q["addScore"]?.ToObject<int>() ?? 10
                    };

                    Debug.Log($"Parsed question: {questionText}, Options: [{string.Join(", ", shuffledOptions)}], Correct: {correctAnswer}");

                    // Categorize by difficulty
                    switch (question.difficulty)
                    {
                        case "Easy": easy.Add(question); break;
                        case "Medium": medium.Add(question); break;
                        case "Hard": hard.Add(question); break;
                        case "VeryHard": veryHard.Add(question); break;
                        default:
                            Debug.LogWarning($"Unknown difficulty '{question.difficulty}' for question: {questionText}");
                            break;
                    }
                }

                // Update question sets
                easyQuestions = new QuestionSet { questions = easy.ToArray() };
                mediumQuestions = new QuestionSet { questions = medium.ToArray() };
                hardQuestions = new QuestionSet { questions = hard.ToArray() };
                veryHardQuestions = new QuestionSet { questions = veryHard.ToArray() };

                Debug.Log($"Loaded {easy.Count} Easy, {medium.Count} Medium, {hard.Count} Hard, {veryHard.Count} VeryHard questions from API.");

                // Validate question counts
                if (easy.Count < minQuestionsPerDifficulty || medium.Count < minQuestionsPerDifficulty ||
                    hard.Count < minQuestionsPerDifficulty || veryHard.Count < minQuestionsPerDifficulty)
                {
                    Debug.LogWarning($"Insufficient questions: Easy={easy.Count}, Medium={medium.Count}, Hard={hard.Count}, VeryHard={veryHard.Count}. Minimum required: {minQuestionsPerDifficulty} per difficulty.");
                }

                onComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JSON Parsing Error: {e.Message}");
                if (fallbackToLocalOnApiFailure)
                {
                    Debug.Log("Failed to parse API response. Falling back to JSON.");
                    GenerateQuestionsFromJSON(topic, onComplete);
                }
                else
                {
                    onComplete?.Invoke();
                }
                yield break;
            }
        }
    }
    #endregion
}