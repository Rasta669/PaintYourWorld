using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Net.Http;
using System;
using UnityEngine.Events;
using System.Threading.Tasks;
using static WalletConnectManager;
using System.Collections.Generic;
//using Nethereum.HdWallet; // Added for UnityEvent handling

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Canvas mainMenuCanvas;     // Reference to your main menu canvas
    [SerializeField] private Canvas loginCanvas;
    [SerializeField] private Canvas HTPCanvas;
    [SerializeField] private Canvas leaderboardCanvas;
    [SerializeField] private Canvas LoadingCanvas;
    [SerializeField] private Button startButton;         // Reference to your start button
    [SerializeField] private Button pauseButton;         // Reference to your pause button
    [SerializeField] private Button resumeButton;        // Reference to your resume button
    [SerializeField] private Button restartButton;       // Reference to your restart button
    [SerializeField] private Button BMMutton;            // Reference to your back to main menu button
    [SerializeField] private Button RBMMutton;           // Reference to your back to main menu button
    // Color selection buttons in pause menu
    [SerializeField] private Button purpleButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button yellowButton;
    [SerializeField] private Button redButton;
    [SerializeField] private Button ghostButton;
    [SerializeField] private Button brownButton;
    public bool isGameStarted = false; // Tracks if game has started
    public bool isGameLoggedIn = false; // Tracks if game has been logged in
    public bool hasDied = false; // Tracks if game has started
    [SerializeField] private Canvas pauseMenu;
    [SerializeField] private Canvas resumeMenu;
    [SerializeField] private Canvas gameOverMenu;
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private GameObject player;          // Reference to your player GameObject
    [SerializeField] private Camera main;          // Reference to your main camera
    [SerializeField] private PaintManager paintManager;  // Reference to your PaintManager
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI walletAddressText; // New: Displays wallet address in pause menu
    [SerializeField] private TextMeshProUGUI claimedTokenText; // New: Displays claimed token balance in pause menu
    [SerializeField] private TextMeshProUGUI GOclaimedTokenText; // New: Displays claimed token balance in pause menu
    [SerializeField] private List<Image> heartImages;
    private TextMeshProUGUI _rankText;
    private Vector3 initialPlayerPosition; // Store player's starting position
    //private static bool shouldStartImmediately = false; // Flag for immediate start after restart
    private int totalXP = 0;        // Total XP earned
    private int obstacleBonus = 10; // XP bonus per obstacle passed
    private WalletConnectManager walletConnectManager; // Reference to WalletConnectManager

    // Instruction UI fields
    public Canvas instructionCanvas;    // Reference to instruction canvas
    public Image instructionImage;      // Reference to the UI Image for displaying sprites
    public Button nextButton;           // Reference to the Next button
    public Button skipButton;           // Reference to the Skip button
    public Sprite[] instructionSprites; // Array of instruction sprites
    private int currentInstructionIndex = 0; // Track current instruction sprite
    private bool showingInstructions = false; // Track if instructions are being shown

    [SerializeField] private Canvas characterSelectCanvas;  // New: character select screen
    [SerializeField] private Button character1Button;       // Assign in Inspector
    [SerializeField] private Button character2Button;       // Assign in Inspector
    [SerializeField] private GameObject character1;         // Reference to Player 1 prefab
    [SerializeField] private GameObject character2;         // Reference to Player 2 prefab

    private bool hasSelectedCharacter = false;              // Ensures only first-time character selection
    private bool hasSeenInstructions = false;               // Ensures instructions shown only once


    // Camera follow settings
    public float smoothSpeed = 0.125f; // How smooth the camera follows (lower = smoother but slower)
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f); // Offset from player (Z=-10 for 2D)

    public TextMeshProUGUI leaderboardText; // New field for displaying leaderboard in game over UI

    public TMP_InputField inputField; // Assign via Inspector
    public CanvasTransition transition;
    

    public static string capturedText = "";

    [SerializeField] private List<TextMeshProUGUI> scoreFields = new List<TextMeshProUGUI>(); // List of TextMeshProUGUI fields
    [SerializeField] private List<TextMeshProUGUI> nameFields = new List<TextMeshProUGUI>(); // List of TextMeshProUGUI fields
    [SerializeField] private List<string> nameList = new List<string>(); // List of strings
    [SerializeField] private List<uint> scoreList = new List<uint>(); // List of string
    //public LoadingDots loadingDots;
    private Stack<GameObject> canvasStack = new Stack<GameObject>();

    private int selectedCharacterIndex = 0;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager
            //if (character1 != null) DontDestroyOnLoad(character1);
            //if (character2 != null) DontDestroyOnLoad(character2);
            if (transitionCanvasGroup != null)
                DontDestroyOnLoad(transitionCanvasGroup.gameObject); // Keep canvas group alive
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reassign CanvasGroup if it's null or destroyed
        if (transitionCanvasGroup == null)
        {
            transitionCanvasGroup = GameObject.Find("TransitionCanvasGroup")?.GetComponent<CanvasGroup>();
            if (transitionCanvasGroup == null)
            {
                Debug.LogError("TransitionCanvasGroup not found in scene after reload!");
            }
        }

        if (paintManager == null)
        {
            paintManager = FindObjectOfType<PaintManager>();
            if (paintManager != null)
                Debug.Log("✅ PaintManager re-linked after restart.");
            else
                Debug.LogWarning("⚠️ PaintManager not found after scene load!");
        }

        //// Also reassign player, main camera, and canvases
        //player = GameObject.FindWithTag("Player");
        main = Camera.main;
    }




    void Start()
    {
        // Find WalletConnectManager in the scene
        walletConnectManager = FindObjectOfType<WalletConnectManager>();
        //loadingDots = loadingDots.GetComponent<LoadingDots>();
        if (walletConnectManager == null)
        {
            Debug.LogError("WalletConnectManager not found in the scene!");
        }
        paintManager = FindObjectOfType<PaintManager>();
        //loadingDots = loadingDots.GetComponent<LoadingDots>();
        if (paintManager == null)
        {
            Debug.LogError("WalletConnectManager not found in the scene!");
        }
        
        //if (loadingDots == null)
        //{
        //    Debug.LogError("Loading dots not found in the scene!");
        //}

        // Store initial player position
        if (player != null)
        {
            initialPlayerPosition = player.transform.position;
        }

        // Ensure correct UI state at start
        if (mainMenuCanvas != null && loginCanvas != null && pauseMenu != null && resumeMenu != null && gameOverMenu != null)
        {

            AudioManager.Instance.PlayMenuMusic();
            if (isGameLoggedIn)
            {
                if (loginCanvas != null)
                {
                    loginCanvas.gameObject.SetActive(false);
                }
                mainMenuCanvas.gameObject.SetActive(true);
                pauseMenu.gameObject.SetActive(false);
                resumeMenu.gameObject.SetActive(false);
                gameOverMenu.gameObject.SetActive(false);
                HTPCanvas.gameObject.SetActive(false);
                LoadingCanvas.gameObject.SetActive(false);
            }
            else
            {
                loginCanvas.gameObject.SetActive(true);
                mainMenuCanvas.gameObject.SetActive(false);
                pauseMenu.gameObject.SetActive(false);
                resumeMenu.gameObject.SetActive(false);
                gameOverMenu.gameObject.SetActive(false);
                HTPCanvas.gameObject.SetActive(false);
                LoadingCanvas.gameObject.SetActive(false);
            }
            if (instructionCanvas != null)
            {
                instructionCanvas.gameObject.SetActive(false);
            }

            

            if (characterSelectCanvas != null)
                characterSelectCanvas.gameObject.SetActive(false);

            Time.timeScale = 0f;
            hasDied = false;

            // Add initial canvas to stack
        //canvasStack.Clear(); // Clear stack to avoid duplicates
        //canvasStack.Push(mainMenuCanvas.gameObject); // Push loginCanvas to stack
        }
        else
        {
            Debug.LogWarning("One or more Canvas references (including LoginCanvas) are not set in GameManager!");
        }

        // Initialize XP text
        UpdateXPText();

        // Initialize pause menu wallet and token displays
        UpdatePauseMenuWalletInfo();


        // Add listeners to buttons (excluding login buttons as they are set in Inspector)
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (character1Button != null)
            character1Button.onClick.AddListener(() => OnCharacterSelected(character1));

        if (character2Button != null)
            character2Button.onClick.AddListener(() => OnCharacterSelected(character2));

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        if (BMMutton != null)
        {
            BMMutton.onClick.AddListener(ReturnToMainMenu);
        }
        if (RBMMutton != null)
        {
            RBMMutton.onClick.AddListener(OnBackToMainMenuButtonClicked);
        }

        // Add listeners for color selection buttons
        if (purpleButton != null)
        {
            purpleButton.onClick.AddListener(() => OnColorButtonClicked("purple"));
        }
        if (blueButton != null)
        {
            blueButton.onClick.AddListener(() => OnColorButtonClicked("blue"));
        }
        if (yellowButton != null)
        {
            yellowButton.onClick.AddListener(() => OnColorButtonClicked("yellow"));
        }
        if (redButton != null)
        {
            redButton.onClick.AddListener(() => OnColorButtonClicked("red"));
        }
        if (ghostButton != null)
        {
            ghostButton.onClick.AddListener(() => OnColorButtonClicked("ghost"));
        }
        if (brownButton != null)
        {
            brownButton.onClick.AddListener(() => OnColorButtonClicked("brown"));
        }

        // Add listeners for instruction buttons
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextInstructionClicked);
        }
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipInstructionsClicked);
        }

        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnTextEntered);
        }

        
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    void Update()
    {
        // Only follow player when game is started
        if (isGameStarted && player != null && main != null)
        {
            FollowPlayer();
        }
    }

    void FollowPlayer()
    {
        // Target position: follow player X, keep camera Y fixed, maintain Z offset

        Vector3 desiredPosition = new Vector3(
            player.transform.position.x + cameraOffset.x,
            main.transform.position.y, // Keep Y fixed
            cameraOffset.z
        );

        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(
            main.transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        main.transform.position = smoothedPosition;
    }

    public void OnWalletLoggedIn()
    {
        // Update pause menu wallet info when login completes
        //UpdatePauseMenuWalletInfo();
        Register(capturedText);
        //ReadName();
        // Transition to main menu after login
        if (loginCanvas != null && mainMenuCanvas != null)
        {
            loginCanvas.gameObject.SetActive(false);
            mainMenuCanvas.gameObject.SetActive(true);
            isGameLoggedIn = true;
        }
    }

    

    void OnStartButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();

        if (!hasSelectedCharacter)
        {
            // Show character select the first time only
            ShowCharacterSelect();
        }
        else if (!hasSeenInstructions)
        {
            // Skip directly to instructions after character already chosen
            StartInstructions();
        }
        else
        {
            // If both character and instructions were already seen, just start game directly
            StartGame();
        }
    }
    void ShowCharacterSelect()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);

        if (characterSelectCanvas != null)
            characterSelectCanvas.gameObject.SetActive(true);
    }




    void OnCharacterSelected(GameObject selectedPrefab)
    {
        AudioManager.Instance.PlayClickSound();

        if (selectedPrefab == null)
        {
            Debug.LogError("Selected character prefab is null!");
            return;
        }

        selectedCharacterIndex = selectedPrefab == character2 ? 2 : 1;
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedCharacterIndex);
        PlayerPrefs.Save();
        // Instantiate the selected character prefab at initial position
        player = Instantiate(selectedPrefab, initialPlayerPosition, Quaternion.identity);

        // Assign player to other scripts
        RewardSpawner spawner = FindObjectOfType<RewardSpawner>();
        if (spawner != null) spawner.SetPlayer(player);

        ObstacleSpawner obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
        if (obstacleSpawner != null) obstacleSpawner.SetPlayer(player);

        FixedXObstacleSpawner2D fixedSpawner = FindObjectOfType<FixedXObstacleSpawner2D>();
        if (fixedSpawner != null) fixedSpawner.SetPlayer(player.transform);

        PaintManager PM = FindObjectOfType<PaintManager>();
        if (PM != null) PM.SetPlayer(player);

        hasSelectedCharacter = true;

        // Hide character select canvas
        if (characterSelectCanvas != null)
            characterSelectCanvas.gameObject.SetActive(false);

        // Continue to instructions
        StartInstructions();
    }



    

    void StartGame()
    {
        // Hide instruction canvas and reset player position
        if (instructionCanvas != null)
        {
            instructionCanvas.gameObject.SetActive(false);
        }
        if (mainMenuCanvas != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
            mainMenuCanvas.gameObject.SetActive(false);
            if (pauseMenu != null)
            {
                pauseMenu.gameObject.SetActive(true);
            }
        }

        if (player != null)
        {
            player.transform.position = initialPlayerPosition;
        }

        totalXP = 0; // Reset XP on new game
        UpdateXPText();
        UpdatePauseMenuWalletInfo();

        // Start the game
        Time.timeScale = 1f;
        isGameStarted = true;
        //shouldStartImmediately = false; // Reset the flag
    }
    public async Task GameOver()
    {
        
        
        isGameStarted = false;
        hasDied = true;
        gameOverMenu.gameObject.SetActive(true);
        AudioManager.Instance.PlayGameOverMusic();
        pauseMenu.gameObject.SetActive(false);
        Time.timeScale = 0f;
        UpdateXPText(); // Update XP display on game over
        UpdateGameOverText();
        UpdateGOMenuWalletInfo();
        Debug.Log("Game Over: Updating wallet info");
        Debug.Log("Submitting score...");
        await SubmitScore();
        Debug.Log("Score submitted successfully");
        Debug.Log("Fetching top players...");
        
    }

    void OnRestartButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        RestartGame();
    }

    public void ReturnToMainMenu()
    {
        ResetGameState();
        StartCoroutine(ReturnToMainMenuWithTransition(SceneManager.GetActiveScene().name));
    }

    //private IEnumerator ReturnToMainMenuWithTransition(string sceneName)
    //{
    //    // fade in transition
    //    AudioManager.Instance.PlayMenuMusic();
    //    transitionCanvasGroup.gameObject.SetActive(true);
    //    float elapsedTime = 0f;
    //    while (elapsedTime < transitionDuration)
    //    {
    //        elapsedTime += Time.unscaledDeltaTime;
    //        transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / transitionDuration);
    //        yield return null;
    //    }
    //    transitionCanvasGroup.alpha = 1f;

    //    // load the scene
    //    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
    //    asyncLoad.allowSceneActivation = false;

    //    while (asyncLoad.progress < 0.9f)
    //    {
    //        yield return null;
    //    }
    //    asyncLoad.allowSceneActivation = true;
    //    while (!asyncLoad.isDone)
    //    {
    //        yield return null;
    //    }

    //    // fade out transition
    //    elapsedTime = 0f;
    //    while (elapsedTime < transitionDuration)
    //    {
    //        elapsedTime += Time.unscaledDeltaTime;
    //        transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / transitionDuration);
    //        yield return null;
    //    }
    //    transitionCanvasGroup.alpha = 0f;
    //    transitionCanvasGroup.gameObject.SetActive(false);

    //    // show the main menu instead of gameplay
    //    loginCanvas.gameObject.SetActive(false);            // show login first (so user can log in again if needed)
    //    mainMenuCanvas.gameObject.SetActive(true);         // show main menu
    //    //gameModeMenuCanvas.SetActive(false);
    //    leaderboardCanvas.gameObject.gameObject.SetActive(false);
    //    resumeMenu.gameObject.SetActive(false);
    //    pauseMenu.gameObject.SetActive(false);
    //    gameOverMenu.gameObject.SetActive(false);
    //    //gameWinCanvas.SetActive(false);
    //    Time.timeScale = 0f;                    // keep the game paused until user starts
    //    UpdateXPText();
    //}

    private IEnumerator ReturnToMainMenuWithTransition(string sceneName)
    {
        // fade in transition
        AudioManager.Instance.PlayMenuMusic();
        gameOverMenu.gameObject.SetActive(false);
        transitionCanvasGroup.gameObject.SetActive(true);
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / transitionDuration);
            yield return null;
        }
        transitionCanvasGroup.alpha = 1f;

        // load the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // fade out transition
        elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / transitionDuration);
            yield return null;
        }
        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.gameObject.SetActive(false);

        // show the main menu instead of gameplay
        loginCanvas.gameObject.SetActive(false);
        mainMenuCanvas.gameObject.SetActive(true);
        leaderboardCanvas.gameObject.SetActive(false);
        resumeMenu.gameObject.SetActive(false);
        pauseMenu.gameObject.SetActive(false);
        gameOverMenu.gameObject.SetActive(false);
        Time.timeScale = 0f;
        UpdateXPText();

        // ✅ Recreate player (so it exists again after RMM)
        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 1);
        GameObject selectedPrefab = selectedCharacterIndex == 2 ? character2 : character1;

        if (selectedPrefab != null)
        {
            player = Instantiate(selectedPrefab, initialPlayerPosition, Quaternion.identity);

            // reconnect all systems
            var spawner = FindObjectOfType<RewardSpawner>();
            if (spawner != null) spawner.SetPlayer(player);

            var obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
            if (obstacleSpawner != null) obstacleSpawner.SetPlayer(player);

            var fixedSpawner = FindObjectOfType<FixedXObstacleSpawner2D>();
            if (fixedSpawner != null) fixedSpawner.SetPlayer(player.transform);

            var pm = FindObjectOfType<PaintManager>();
            if (pm != null) pm.SetPlayer(player);
        }
        else
        {
            Debug.LogError("❌ No player prefab found for ReturnToMainMenu.");
        }
    }


    private void ResetGameState()
    {
        AudioManager.Instance.StopAllMusic(); 
        ResetPlayerHealth();
        totalXP = 0;
        isGameStarted = false;
        hasDied = false;
        isGameLoggedIn  = true; // Keep logged in when returning to main menu
    }

    public void RestartGame()
    {
        ResetGameState();
        StartCoroutine(RestartLevelDirectly(SceneManager.GetActiveScene().name));
    }

    private IEnumerator RestartLevelDirectly(string sceneName)
    {
        // show loading/transition effect if you like
        AudioManager.Instance.PlayGameplayMusic();
        gameOverMenu.gameObject.SetActive(false);
        transitionCanvasGroup.gameObject.SetActive(true);
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / transitionDuration);
            yield return null;
        }
        transitionCanvasGroup.alpha = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // fade out transition
        elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / transitionDuration);
            yield return null;
        }
        transitionCanvasGroup.alpha = 0f;
        transitionCanvasGroup.gameObject.SetActive(false);

        // directly enter gameplay, skipping login/menu canvases
        loginCanvas.gameObject.SetActive(false);
        mainMenuCanvas.gameObject.SetActive(false);
        //gameModeMenuCanvas.SetActive(false);
        pauseMenu.gameObject.SetActive(true);
        leaderboardCanvas.gameObject.SetActive(false);
        gameOverMenu.gameObject.SetActive(false);
        HTPCanvas.gameObject.SetActive(false);
        // Recreate the selected player prefab after scene reload
        if (player == null)
        {
            selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 1);
            GameObject selectedPrefab = selectedCharacterIndex == 2 ? character2 : character1;
            player = Instantiate(selectedPrefab, initialPlayerPosition, Quaternion.identity);


            // Reconnect player references in other scripts
            var spawner = FindObjectOfType<RewardSpawner>();
            if (spawner != null) spawner.SetPlayer(player);

            var obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
            if (obstacleSpawner != null) obstacleSpawner.SetPlayer(player);

            var fixedSpawner = FindObjectOfType<FixedXObstacleSpawner2D>();
            if (fixedSpawner != null) fixedSpawner.SetPlayer(player.transform);

            var pm = FindObjectOfType<PaintManager>();
            if (pm != null) pm.SetPlayer(player);

            Debug.Log("✅ Player re-instantiated after restart.");
        }

        Time.timeScale = 1f;
        isGameStarted = true;
        UpdateXPText();
    }

    void StartInstructions()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.gameObject.SetActive(false);
        }
        if (instructionCanvas != null)
        {
            showingInstructions = true;
            instructionCanvas.gameObject.SetActive(true);
            currentInstructionIndex = 0;
            ShowInstruction();
        }
        else
        {
            Debug.LogWarning("InstructionCanvas reference not set in GameManager! Starting game directly.");
            StartGame();
        }
    }
    public void OnBackButtonClickedHTP()
    {

        HTPCanvas.gameObject.SetActive(false);
        mainMenuCanvas.gameObject.SetActive(true);
    }

    public void OnBackButtonClickedLB()
    {
        if (hasDied)
        {
            leaderboardCanvas.gameObject.SetActive(false);
            gameOverMenu.gameObject.SetActive(true);
        }
        else
        {
            leaderboardCanvas.gameObject.SetActive(false);
            mainMenuCanvas.gameObject.SetActive(true);
        }
    }

    void OnPauseButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        PauseGame();
    }

    void PauseGame()
    {
        AudioManager.Instance.PlayPauseMusic();
        Time.timeScale = 0f;
        pauseMenu.gameObject.SetActive(false);
        resumeMenu.gameObject.SetActive(true);
        UpdateXPText(); // Update XP display when pausing
        //UpdatePauseMenuWalletInfo(); // Update wallet and token info in pause menu
    }

    void OnResumeButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        ResumeGame();
    }

    void ResumeGame()
    {
        AudioManager.Instance.PlayGameplayMusic();
        Time.timeScale = 1f;
        resumeMenu.gameObject.SetActive(false);
        pauseMenu.gameObject.SetActive(true);
    }
   

    public void OnBackToMainMenuButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        BackToMainMenu();
    }

    public void BackToMainMenu()
    {
        // Fully reload the scene to reset everything
        //shouldStartImmediately = false; // Ensure we go to main menu
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnColorButtonClicked(string colorName)
    {
        AudioManager.Instance.PlayClickSound();
        SetPaintColor(colorName);
    }

    // Method to set paint color via PaintManager
    void SetPaintColor(string colorName)
    {
        if (paintManager != null)
        {
            paintManager.SetPaintColor(colorName);
        }
        else
        {
            Debug.LogError("PaintManager reference is not set in GameManager!");
        }
    }

    public void AddXP(int amount)
    {
        AudioManager.Instance.PlayKeyAcquiredSound();
        totalXP += amount;
        UpdateXPText();
    }

    public void AddObstacleBonus()
    {
        totalXP += obstacleBonus;
        UpdateXPText();
    }

    void UpdateXPText()
    {
        if (xpText != null)
        {
            xpText.text = $"XP: {totalXP}";
            //b3.SetScore(totalXP);
        }
        else
        {
            Debug.LogWarning("xpText reference not set in GameManager!");
        }
    }

    public float GetTotalXP()
    {
        return totalXP;
    }

    void UpdateGameOverText()
    {
        if (gameOverText != null)
        {
            gameOverText.text = $"Final XP: {totalXP}";
        }
        else
        {
            Debug.LogWarning("gameOverText reference not set in GameManager!");
        }
    }

    void UpdatePauseMenuWalletInfo()
    {
        if (walletConnectManager != null)
        {
            // Update wallet address display
            if (walletAddressText != null)
            {
                if (walletConnectManager.AddressText != null && walletConnectManager.AddressText.gameObject.activeSelf)
                {
                    walletAddressText.text = walletConnectManager.AddressText.text;
                    walletAddressText.gameObject.SetActive(true);
                }
                else
                {
                    walletAddressText.text = "Wallet: Not Connected";
                    walletAddressText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("walletAddressText reference not set in GameManager!");
            }

            // Update claimed token balance display
            if (claimedTokenText != null)
            {
                if (walletConnectManager.CustomTokenBalanceText != null)
                {
                    claimedTokenText.text = walletConnectManager.CustomTokenBalanceText.text;
                    claimedTokenText.gameObject.SetActive(true);
                }
                else
                {
                    claimedTokenText.text = "Couldnt fetch";
                    claimedTokenText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("claimedTokenText reference not set in GameManager!");
            }
        }
        else
        {
            if (walletAddressText != null)
            {
                walletAddressText.text = "Wallet: Not Connected";
                walletAddressText.gameObject.SetActive(true);
            }
            if (claimedTokenText != null)
            {
                claimedTokenText.text = "Claimed: 0 Color";
                claimedTokenText.gameObject.SetActive(true);
            }
        }
    }
    void UpdateGOMenuWalletInfo()
    {
        if (walletConnectManager != null)
        {
            // Update wallet address display
            if (walletAddressText != null)
            {
                if (walletConnectManager.AddressText != null && walletConnectManager.AddressText.gameObject.activeSelf)
                {
                    walletAddressText.text = walletConnectManager.AddressText.text;
                    walletAddressText.gameObject.SetActive(true);
                    //Debug.Log("wallet set");
                }
                else
                {
                    walletAddressText.text = "Wallet: Not Connected";
                    walletAddressText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("walletAddressText reference not set in GameManager!");
            }

            // Update claimed token balance display
            if (claimedTokenText != null)
            {
                if (walletConnectManager.CustomTokenBalanceText != null && walletConnectManager.CustomTokenBalanceText.gameObject.activeSelf)
                {
                    GOclaimedTokenText.text = walletConnectManager.CustomTokenBalanceText.text;
                    GOclaimedTokenText.gameObject.SetActive(true);
                }
                else
                {
                    GOclaimedTokenText.text = "Claimed: 0 Color";
                    GOclaimedTokenText.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("claimedTokenText reference not set in GameManager!");
            }
        }
        else
        {
            if (walletAddressText != null)
            {
                walletAddressText.text = "Wallet: Not Connected";
                walletAddressText.gameObject.SetActive(true);
            }
            if (GOclaimedTokenText != null)
            {
                GOclaimedTokenText.text = "Claimed: 0 Color";
                GOclaimedTokenText.gameObject.SetActive(true);
            }
        }

        //SubmitScore();
    }

    // Instruction handling methods
    void ShowInstruction()
    {
        if (instructionSprites == null || instructionSprites.Length == 0)
        {
            Debug.LogWarning("No instruction sprites assigned in GameManager!");
            EndInstructions();
            return;
        }

        if (currentInstructionIndex >= instructionSprites.Length)
        {
            EndInstructions();
            return;
        }

        if (instructionImage != null)
        {
            instructionImage.sprite = instructionSprites[currentInstructionIndex];
            // Enable Next button only if there are more instructions
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(currentInstructionIndex < instructionSprites.Length - 1);
            }
            // Optional: Uncomment to enable auto-advance
            // StartCoroutine(AutoAdvanceInstruction(5f)); // Auto-advance after 5 seconds
        }
        else
        {
            Debug.LogWarning("InstructionImage reference not set in GameManager!");
            EndInstructions();
        }
    }

    void OnNextInstructionClicked()
    {
        AudioManager.Instance.PlayClickSound();
        currentInstructionIndex++;
        ShowInstruction();
    
    
    }
    
    void OnLeaderboardClicked()
    {
        AudioManager.Instance.PlayClickSound();
        GetLeaderboard();
    }

    void OnSkipInstructionsClicked()
    {
        AudioManager.Instance.PlayClickSound();
        EndInstructions();
    }

    //void EndInstructions()
    //{
    //    showingInstructions = false;
    //    if (instructionCanvas != null)
    //    {
    //        instructionCanvas.gameObject.SetActive(false);
    //    }
    //    StartGame(); // Proceed to start the game
    //}

    void EndInstructions()
    {
        showingInstructions = false;
        hasSeenInstructions = true; // mark instructions as seen
        if (instructionCanvas != null)
        {
            instructionCanvas.gameObject.SetActive(false);
        }
        StartGame(); // Proceed to start the game
    }


    // Optional: Auto-advance coroutine
    IEnumerator AutoAdvanceInstruction(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Use real-time for paused game
        if (showingInstructions) // Only advance if still showing instructions
        {
            currentInstructionIndex++;
            ShowInstruction();
        }
    }


    public void ShowCanvas(GameObject newCanvas)
    {
        if (canvasStack.Count > 0)
        {
            GameObject currentCanvas = canvasStack.Peek();
            currentCanvas.SetActive(false);
        }

        canvasStack.Push(newCanvas);
        newCanvas.SetActive(true);
    }

    public void GoBack()
    {
        if (canvasStack.Count > 1)
        {
            GameObject topCanvas = canvasStack.Pop();
            topCanvas.SetActive(false);

            GameObject previousCanvas = canvasStack.Peek();
            previousCanvas.SetActive(true);
        }
        else
        {
            Debug.Log("No previous canvas to return to.");
        }
    }



    public async Task SubmitScore()
    {
        Debug.Log($"Your rank....");
        //_rankText.text = $"Global Rank: ...";
        Debug.Log($"..");
        await walletConnectManager.SubmitScore(totalXP);
        Debug.Log($"submitted score....");
        //int rank = await WalletConnectManager.Instance.GetRank();
        //Debug.Log($"Extracted rank.");
        //if (_rankText != null)
        //{
        //    _rankText.text = $"$\"Global Rank: {{rank}}";
        //}
        
        //Debug.Log($"Your rank : {rank}");    
    }

    public void ReadScore(int position)
    {
        walletConnectManager.ReadScore(position);
    }

    public void Register(string name)
    {
        walletConnectManager.RegisterLeaderboardName(name);
        
    }

    public void ReadName(uint position) { 
        walletConnectManager.ReadName(position);
    }

    public void ReadnRegister(string name) {
        Register(name); 
       //eadName(position);
    }

    void OnTextEntered(string text)
    {
        capturedText = text;
        Debug.Log("Captured: " + capturedText);
    }

    public async void GetLeaderboard()
    {
        // List of all possible canvases
        Canvas[] allCanvases = { mainMenuCanvas, gameOverMenu  };

        // Find the currently active canvas
        Canvas currentCanvas = null;
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != null && canvas.gameObject.activeSelf)
            {
                currentCanvas = canvas;
                break;
            }
        }

        // If no active canvas is found, log a warning and return
        if (currentCanvas == null)
        {
            Debug.LogWarning("No active canvas found for transition!");
            return;
        }
 
        // Perform the transition using the detected current canvas
        currentCanvas.gameObject.SetActive(false);
        LoadingCanvas.gameObject.SetActive(true);
        Debug.Log("Dots loading");
        //loadingDots.StartLoading();
        Debug.Log("Reading blockchain data");
        //await WalletConnectManager.Instance.GetScoreList();
        for (int i = 0; i < 5; i++)
        {
            await walletConnectManager.ReadScore(i);
        }
        //UpdateNameFields();
        UpdateScoreFields();
        UpdateNameFields();
        Debug.Log("done Reading");
        LoadingCanvas.gameObject.SetActive(false);
        leaderboardCanvas.gameObject.SetActive(true);
        ////Read data
        //Debug.Log("Reading blockchain data");
        //WalletConnectManager.Instance.ReadName(0);
        //Debug.Log("done Reading");
    }


    public async Task UpdateTextFields()

    {
        await WalletConnectManager.Instance.GetScoreList();
        //UpdateNameFields();
        UpdateScoreFields();
    }

    void UpdateNameFields()
    {
        nameList = walletConnectManager.NameList();

        //for (int i = 0; i < 5; i++)
        //{

        //    nameFields[i].text = nameList[i];
        //}      

        for (int i = 0; i < 5; i++)
        {
            string name = nameList[i];
            if (name.Length > 8)
                name = name.Substring(0, 10);

            nameFields[i].text = name;
        }
    }

    void UpdateScoreFields()
    {
        scoreList = walletConnectManager.ScoreList();

        for (int i = 0; i < 5; i++)
        {
            scoreFields[i].text = scoreList[i].ToString();
        }
    }

    public void OnHTPClicked()
    {
        AudioManager.Instance.PlayClickSound();
        mainMenuCanvas.gameObject.SetActive(false);
        HTPCanvas.gameObject.SetActive(true);
    }

    public void UpdatePlayerHealth(int health)
    {
        if (heartImages != null && heartImages.Count >= 3)
        {
            for (int i = 0; i < heartImages.Count; i++)
            {
                heartImages[i].enabled = i < health;
            }
        }
    }

    private void ResetPlayerHealth()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResetHealth();
        }
    }
}