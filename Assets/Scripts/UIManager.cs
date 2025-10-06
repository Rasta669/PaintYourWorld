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
    //public static GameManager Instance { get; private set; }

    public Canvas mainMenuCanvas;     // Reference to your main menu canvas
    public Canvas loginCanvas;
    public Canvas HTPCanvas;
    public Canvas leaderboardCanvas;
    public Canvas LoadingCanvas;
    public Button startButton;         // Reference to your start button
    public Button pauseButton;         // Reference to your pause button
    public Button resumeButton;        // Reference to your resume button
    public Button restartButton;       // Reference to your restart button
    public Button BMMutton;            // Reference to your back to main menu button
    public Button RBMMutton;           // Reference to your back to main menu button
    // Color selection buttons in pause menu
    public Button purpleButton;
    public Button blueButton;
    public Button yellowButton;
    public Button redButton;
    public bool isGameStarted = false; // Tracks if game has started
    public bool hasDied = false; // Tracks if game has started
    public Canvas pauseMenu;
    public Canvas resumeMenu;
    public Canvas gameOverMenu;
    public GameObject player;          // Reference to your player GameObject
    public Camera main;          // Reference to your main camera
    public PaintManager paintManager;  // Reference to your PaintManager
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI walletAddressText; // New: Displays wallet address in pause menu
    public TextMeshProUGUI claimedTokenText; // New: Displays claimed token balance in pause menu
    public TextMeshProUGUI GOclaimedTokenText; // New: Displays claimed token balance in pause menu
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

    //void Awake()
    //{
    //    if (Instance == null)
    //    {
    //        Instance = this;
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }
    //}

    void Start()
    {
        // Find WalletConnectManager in the scene
        walletConnectManager = FindObjectOfType<WalletConnectManager>();
        //loadingDots = loadingDots.GetComponent<LoadingDots>();
        if (walletConnectManager == null)
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
            
            // Normal start: show login canvas only
            loginCanvas.gameObject.SetActive(true);
            mainMenuCanvas.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            resumeMenu.gameObject.SetActive(false);
            gameOverMenu.gameObject.SetActive(false);
            HTPCanvas.gameObject.SetActive(false);
            LoadingCanvas.gameObject.SetActive(false);
            if (instructionCanvas != null)
            {
                instructionCanvas.gameObject.SetActive(false);
            }
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
            BMMutton.onClick.AddListener(OnBackToMainMenuButtonClicked);
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

        // Subscribe to WalletConnectManager's OnLoggedIn event
        //if (walletConnectManager != null)
        //{
        //    walletConnectManager.OnLoggedIn.AddListener(OnWalletLoggedIn);
        //}
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

    void OnLoginButtonClicked(string authProvider)
    {
        AudioManager.Instance.PlayClickSound();
        if (walletConnectManager != null)
        {
            walletConnectManager.Login(authProvider);
        }
        else
        {
            Debug.LogError("WalletConnectManager is not set, cannot perform login!");
        }
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
        }
    }

    void OnStartButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        // Show instructions instead of starting game directly
        StartInstructions();
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

    //public async Task GameOver()
    //{
    //    AudioManager.Instance.PlayGameOverMusic();
    //    isGameStarted = false;
    //    gameOverMenu.gameObject.SetActive(true);
    //    pauseMenu.gameObject.SetActive(false);
    //    Time.timeScale = 0f;
    //    UpdateXPText(); // Update XP display on game over
    //    UpdateGameOverText();
    //    Debug.Log("updating wallet info");
    //    UpdateGOMenuWalletInfo();
    //    Debug.Log("submitting");
    //    await SubmitScore();
    //    Debug.Log("submitted");
    //    //SetScore();
    //}

    // ... (other fields and methods remain unchanged)

   

    public async Task GameOver()
    {
        AudioManager.Instance.PlayGameOverMusic();
        isGameStarted = false;
        hasDied = true;
        gameOverMenu.gameObject.SetActive(true);
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

    public async void SetScore()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.basement.fun/launcher/");
        request.Headers.Add("X-Service-Method", "setUserScore");
        var content = new StringContent("{\r\n    \"launcherJwt\": {{JWT}},\r\n    \"nonce\": \"nonce=50zpzg\",\r\n    \"score\": 155\r\n}", null, "text/plain");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    void OnRestartButtonClicked()
    {
        AudioManager.Instance.PlayClickSound();
        Restart();
    }

    void Restart()
    {
        // Set flag to start immediately after reload
        //shouldStartImmediately = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    void EndInstructions()
    {
        showingInstructions = false;
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
        await WalletConnectManager.Instance.SubmitScore(totalXP);
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
        WalletConnectManager.Instance.ReadScore(position);
    }

    public void Register(string name)
    {
        WalletConnectManager.Instance.RegisterLeaderboardName(name);
        
    }

    public void ReadName(uint position) { 
        WalletConnectManager.Instance.ReadName(position);
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
            await WalletConnectManager.Instance.ReadScore(i);
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
        nameList = WalletConnectManager.Instance.NameList();

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
        scoreList = WalletConnectManager.Instance.ScoreList();

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
}