// Author: Sanskar Bikram Kunwar, 2025
using UnityEngine;

// Simple component to create a continuous rotating animation
// Typically used for loading indicators or spinners
public class SpinnerAnimation : MonoBehaviour
{
    void Update()
    {
        // Rotate the object counterclockwise at a speed of 360 degrees per second
        transform.Rotate(0, 0, -360 * Time.deltaTime); // Rotate 360 degrees per second
    }
}