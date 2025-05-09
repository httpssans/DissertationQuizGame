using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimpleMinigame : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int targetHits = 5;
    [SerializeField] private float spawnInterval = 0.8f;
    [SerializeField] private float targetLifetime = 1.5f;
    [SerializeField] private int pointsPerHit = 10;
    [SerializeField] private float gameTimeLimit = 30f; // Time limit can be set here
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetsLeftText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private RectTransform spawnArea;
    
    [Header("Effects")]
    [SerializeField] private GameObject hitParticlePrefab;
    
    // Delegates for communication with parent game
    public delegate void MinigameCompleted(int score);
    public event MinigameCompleted OnMinigameCompleted;
    
    // Private variables
    private int currentScore = 0;
    private int hitsRemaining;
    private bool gameRunning = false;
    private float gameTimer = 0f;
    private List<GameObject> activeTargets = new List<GameObject>();
    private TransitionSceneManager transitionManager;
    
    private void Awake()
    {
        // Try to find TransitionSceneManager
        transitionManager = Object.FindFirstObjectByType<TransitionSceneManager>();
        
        // If TransitionSceneManager exists, inform it of our time limit
        if (transitionManager != null)
        {
            transitionManager.SetMinigameTimeLimit(gameTimeLimit);
        }
    }
    
    private void Start()
    {
        StartGame();
        
        // Find the GameManager instance and ensure correct music is playing
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.EnsureCorrectMusicIsPlaying();
        }
        else
        {
            Debug.LogWarning("GameManager not found in scene!");
        }
    }
    
    public void StartGame()
    {
        // Initialize game state
        hitsRemaining = targetHits;
        currentScore = 0;
        gameTimer = gameTimeLimit;
        gameRunning = true;
        
        // Clear any existing targets
        foreach (GameObject target in activeTargets)
        {
            Destroy(target);
        }
        activeTargets.Clear();
        
        UpdateUI();
        
        // Start spawning targets
        StartCoroutine(SpawnTargets());
    }
    
    private void Update()
    {
        if (gameRunning)
        {
            // Update timer
            gameTimer -= Time.deltaTime;
            UpdateTimerDisplay();
            
            // Check if time's up
            if (gameTimer <= 0f)
            {
                gameTimer = 0f;
                EndGame(false);
            }
        }
    }
    
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
        
        if (targetsLeftText != null)
        {
            targetsLeftText.text = "Targets: " + hitsRemaining;
        }
        
        UpdateTimerDisplay();
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(gameTimer);
            timerText.text = "Time: " + seconds.ToString();
            
            // Change timer color based on remaining time
            if (gameTimer <= gameTimeLimit * 0.25f)
            {
                timerText.color = Color.red;
            }
            else if (gameTimer <= gameTimeLimit * 0.5f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    
    private IEnumerator SpawnTargets()
    {
        while (gameRunning && hitsRemaining > 0)
        {
            SpawnTarget();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    private void SpawnTarget()
    {
        if (targetPrefab != null && spawnArea != null)
        {
            // Calculate random position within spawn area
            float randomX = Random.Range(-spawnArea.rect.width/2, spawnArea.rect.width/2);
            float randomY = Random.Range(-spawnArea.rect.height/2, spawnArea.rect.height/2);
            
            // Create the target
            GameObject target = Instantiate(targetPrefab, spawnArea);
            RectTransform targetRect = target.GetComponent<RectTransform>();
            
            if (targetRect != null)
            {
                targetRect.anchoredPosition = new Vector2(randomX, randomY);
                
                // Add click handler
                Button targetButton = target.GetComponent<Button>();
                if (targetButton != null)
                {
                    targetButton.onClick.AddListener(() => OnTargetHit(target));
                }
                
                // Add to active targets list
                activeTargets.Add(target);
                
                // Start destroy timer
                StartCoroutine(DestroyTargetAfterTime(target));
            }
        }
    }
    
    private IEnumerator DestroyTargetAfterTime(GameObject target)
    {
        yield return new WaitForSeconds(targetLifetime);
        
        if (target != null && activeTargets.Contains(target))
        {
            activeTargets.Remove(target);
            Destroy(target);
        }
    }
    
    private void OnTargetHit(GameObject target)
    {
        if (!gameRunning) return;

        // Create hit effect that inherits properties from the target
        if (hitParticlePrefab != null)
        {
            // Convert target position from UI space to world space
            Vector3 worldPos = target.transform.position;
            
            // Create particle effect
            GameObject particles = Instantiate(hitParticlePrefab, worldPos, Quaternion.identity);
            
            ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
            }
            
            // Destroy particles after animation completes
            float destroyTime = particles.GetComponent<ParticleSystem>()?.main.duration ?? 1f;
            Destroy(particles, destroyTime + 0.5f); // Add a small buffer
        }

        // Play hit sound using AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("TargetHitSFX");
        }
        
        // Add points
        currentScore += pointsPerHit;
        
        // Reduce remaining targets
        hitsRemaining--;
        
        // Clean up target
        activeTargets.Remove(target);
        Destroy(target);
        
        // Update UI
        UpdateUI();
        
        // Check for game completion
        if (hitsRemaining <= 0)
        {
            EndGame(true);
        }
    }
    
    private void EndGame(bool completed)
    {
        if (!gameRunning) return; // Prevent multiple calls
        gameRunning = false;
        // Clean up any remaining targets
        foreach (GameObject target in activeTargets)
        {
            Destroy(target);
        }
        activeTargets.Clear();
        
        // Notify any listeners that the minigame is completed
        OnMinigameCompleted?.Invoke(currentScore);
        
        // If TransitionSceneManager exists, inform it that we're done
        if (transitionManager != null)
        {
            transitionManager.CompleteMinigame();
        }
    }
    
    // For target prefab creation in editor
    public GameObject CreateTargetPrefab()
    {
        // This method can be called from Editor scripts to create a target prefab
        GameObject targetObj = new GameObject("Target");
        
        // Add required components
        Image image = targetObj.AddComponent<Image>();
        image.color = new Color(1f, 0.5f, 0.5f);
        
        Button button = targetObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.8f, 0.8f);
        colors.pressedColor = new Color(0.8f, 0.3f, 0.3f);
        button.colors = colors;
        
        // Set size
        RectTransform rect = targetObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 80);
        
        return targetObj;
    }
    
    // Helper method to create a particle prefab
    public GameObject CreateHitParticlePrefab()
    {
        GameObject particleObj = new GameObject("TargetHitParticle");
        
        // Add particle system
        ParticleSystem particleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // Configure basic particle system settings
        var main = particleSystem.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.5f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Configure emission
        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0f, 20) 
        });
        
        // Configure shape
        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        return particleObj;
    }
}