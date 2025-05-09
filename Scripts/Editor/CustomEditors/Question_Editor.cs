// Author: Sanskar Bikram Kunwar, 2025
using UnityEditor;
using UnityEngine;

// Custom editor class for the Question class, provides a better UI in the inspector
[CustomEditor(typeof(Question))]
public class Question_Editor : Editor {

    #region Variables

    #region Serialized Properties
    // References to serialized properties in the Question class
    SerializedProperty  questionInfoProp        = null;
    SerializedProperty  answersProp             = null;
    SerializedProperty  useTimerProp            = null;
    SerializedProperty  timerProp               = null;
    SerializedProperty  answerTypeProp          = null;
    SerializedProperty  addScoreProp            = null;

    // Property to get the size of the answers array
    SerializedProperty  arraySizeProp
    {
        get
        {
            return answersProp.FindPropertyRelative("Array.size");
        }
    }
    #endregion

    // Flag to show/hide parameters section in the editor
    private bool        showParameters          = false;

    #endregion

    #region Default Unity methods

    // Called when the editor becomes enabled
    void OnEnable ()
    {
        #region Fetch Properties
        // Initialize all serialized properties by finding them in the target object
        questionInfoProp    = serializedObject.FindProperty("_info");
        answersProp         = serializedObject.FindProperty("_answers");
        useTimerProp        = serializedObject.FindProperty("_useTimer");
        timerProp           = serializedObject.FindProperty("_timer");
        answerTypeProp      = serializedObject.FindProperty("_answerType");
        addScoreProp        = serializedObject.FindProperty("_addScore");
        #endregion

        #region Get Values
        // Load the state of the parameters foldout from EditorPrefs
        showParameters = EditorPrefs.GetBool("Question_showParameters_State");
        #endregion
    }

    // Overriding the default inspector GUI
    public override void OnInspectorGUI ()
    {
        // Show "Question" label
        GUILayout.Label("Question", EditorStyles.miniLabel);
        
        // Create a custom text area style for the question input
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 15,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleLeft
        };
        
        // Display the question text field
        questionInfoProp.stringValue = EditorGUILayout.TextArea(questionInfoProp.stringValue, textAreaStyle);
        GUILayout.Space(7.5f);

        // Create a custom foldout style
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 10
        };
        
        // Handle the foldout for the parameters section
        EditorGUI.BeginChangeCheck();
        showParameters = EditorGUILayout.Foldout(showParameters, "Parameters", foldoutStyle);
        if (EditorGUI.EndChangeCheck())
        {
            // Save the state of the foldout to EditorPrefs
            EditorPrefs.SetBool("Question_showParameters_State", showParameters);
        }
        
        // Display parameters if the foldout is open
        if (showParameters)
        {
            // Timer options
            EditorGUILayout.PropertyField(useTimerProp, new GUIContent("Use Timer", "Should this question have a timer?"));
            if (useTimerProp.boolValue)
            {
                timerProp.intValue = EditorGUILayout.IntSlider(new GUIContent("Time"), timerProp.intValue, 1, 120);
            }
            GUILayout.Space(2);
            
            // Answer type selection (Single or Multi)
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(answerTypeProp, new GUIContent("Answer Type", "Specify this question answer type."));
            if (EditorGUI.EndChangeCheck())
            {
                // If changing to Single answer type but multiple correct answers exist
                if (answerTypeProp.enumValueIndex == (int)Question.AnswerType.Single)
                {
                    if (GetCorrectAnswersCount() > 1)
                    {
                        // Uncheck all correct answers to maintain single answer type integrity
                        UncheckCorrectAnswers();
                    }
                }
            }
            
            // Score value for the question
            addScoreProp.intValue = EditorGUILayout.IntSlider(new GUIContent("Add Score"), addScoreProp.intValue, 0, 100);
        }
        GUILayout.Space(7.5f);
        
        // Display answers section
        GUILayout.Label("Answers", EditorStyles.miniLabel);
        DrawAnswers();

        // Apply any modified properties
        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    // Method to draw the answers list in the inspector
    void DrawAnswers ()
    {
        EditorGUILayout.BeginVertical();

        // Array size property allows adding/removing answers
        EditorGUILayout.PropertyField(arraySizeProp);
        GUILayout.Space(5);

        // Increase indent for the answers list
        EditorGUI.indentLevel++;
        for (int i = 0; i < arraySizeProp.intValue; i++)
        {
            // Check for changes to each answer
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(answersProp.GetArrayElementAtIndex(i));
            if (EditorGUI.EndChangeCheck())
            {
                // Handle single answer type constraints
                if (answerTypeProp.enumValueIndex == (int)Question.AnswerType.Single)
                {
                    SerializedProperty isCorrectProp = answersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_isCorrect");

                    if (isCorrectProp.boolValue)
                    {
                        // Uncheck all other answers first
                        UncheckCorrectAnswers();
                        // Then set this one to correct
                        answersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_isCorrect").boolValue = true;

                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            GUILayout.Space(5);
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    // Helper method to uncheck all correct answers
    void UncheckCorrectAnswers ()
    {
        for (int i = 0; i < arraySizeProp.intValue; i++)
        {
            answersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_isCorrect").boolValue = false;
        }
    }

    // Helper method to count how many answers are marked as correct
    int GetCorrectAnswersCount ()
    {
        int count = 0;
        for (int i = 0; i < arraySizeProp.intValue; i++)
        {
            if (answersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_isCorrect").boolValue)
            {
                count++;
            }
        }
        return count;
    }
}