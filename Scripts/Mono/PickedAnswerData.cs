using UnityEngine;

[System.Serializable]
public class PickedAnswerData
{
    public int AnswerIndex;

    public void Reset()
    {
        AnswerIndex = -1;
    }
}