using UnityEngine;
using Thirdweb;
using System.Threading.Tasks;
using Thirdweb.Unity;
using System.Numerics;
using TMPro;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System; // Added for TimeoutException
using UnityEngine.Events;
using System.Linq;
using UnityEditor;



public class WalletConnectManager : MonoBehaviour
{
    public static WalletConnectManager Instance { get; private set; }

    public UnityEvent<string> OnLoggedIn;
    private ThirdwebManager thirdwebManager;
    private IThirdwebWallet wallet;
    private string walletAddress;
    [field: SerializeField, Header("Wallet Options")]
    private ulong ActiveChainId = 84532;

    [field: SerializeField, Header("Send ETH amount")]
    public string Amount { get; set; }
    [field: SerializeField, Header("Send ETH address")]
    public string ToAddress { get; set; }

    [field: SerializeField, Header("Send Custom Token Options")]
    public string TokenName { get; set; }
    [field: SerializeField]
    public string TokenContractAddress { get; set; }
    [field: SerializeField]
    public string TokenAmount { get; set; }
    [field: SerializeField]
    public string TokenRecipientAddress { get; set; }

    [field: SerializeField, Header("Claim Token Options")]
    public string ClaimTokenContractAddress { get; set; }
    [field: SerializeField]
    public string ClaimTokenAmount { get; set; }

    [field: SerializeField, Header("Claim Nft Options")]
    public string ClaimNftContractAddress { get; set; }
    [field: SerializeField]
    public string ClaimNftAmount { get; set; }

    [field: SerializeField, Header("UI Elements")]
    public GameManager GameManager { get; set; }
    [field: SerializeField]
    public Button ClaimButton { get; set; }
    [field: SerializeField]
    public GameObject ConnectButton { get; set; }
    [field: SerializeField]
    public GameObject DisconnectButton { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI ConnectedText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI ClaimedNFTText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI AddressText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI EthBalanceText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI CustomTokenBalanceText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI ClaimedTokenBalanceText { get; set; }

    [field: SerializeField, Header("NFT Display Canvas")]
    public Canvas NftDisplayCanvas { get; set; }
    [field: SerializeField]
    public GameObject NftDisplayPrefab { get; set; }
    [field: SerializeField]
    public Transform NftDisplayParent { get; set; }

    private List<GameObject> instantiatedNfts = new List<GameObject>();
    private float lastFeedbackUpdateTime;
    private int feedbackDotCount;
    public uint readScore;
    public string readAddress;
    public string readName;
    //public uint readTimestamp;
    public string Gamename;
    public uint scorers;
    public uint LeaderboardLength;
    // Fields with corrected ScrollRect type
    [field: SerializeField, Header("Leaderboard Contract")]
    public string LeaderboardContractAddress { get; set; }

    private List<uint> scoreList = new List<uint>();
    private List<string> nameList = new List<string>();

    // PlayerScore struct matching contract's Score struct
    //[System.Serializable]
    //public struct PlayerScore
    //{
    //    public string player;
    //    public string score; // Stored as string due to BigInteger
    //    public string timestamp; // Unix timestamp as string
    //}

    // ABI for the Leaderboard contract, formatted as a C# string
    //public const string ContractABI = "[{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"}],\"name\":\"ScoreRemoved\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":false,\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"ScoreSubmitted\",\"type\":\"event\"},{\"inputs\":[],\"name\":\"MAX_SCORES\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"}],\"name\":\"getPlayerScore\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"getTopScores\",\"outputs\":[{\"components\":[{\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"timestamp\",\"type\":\"uint256\"}],\"internalType\":\"struct Leaderboard.Score[]\",\"name\":\"\",\"type\":\"tuple[]\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"name\":\"playerScores\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"scores\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"player\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"timestamp\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"score\",\"type\":\"uint256\"}],\"name\":\"submitScore\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

    void Awake()
    {
        //if (Instance == null)
        //{
        //    Instance = this;
        //}
        //else
        //{
        //    Destroy(gameObject);
        //    return;
        //}
        //thirdwebManager = FindObjectOfType<ThirdwebManager>();
        //if (thirdwebManager == null)
        //{
        //    Debug.LogError("ThirdwebManager not found in the scene! Please add the ThirdwebManager prefab.");
        //}

        if (FindObjectsOfType<WalletConnectManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
 thirdwebManager = FindObjectOfType<ThirdwebManager>();
        if (thirdwebManager == null)
        {
            Debug.LogError("ThirdwebManager not found in the scene! Please add the ThirdwebManager prefab.");
        }
        DontDestroyOnLoad(gameObject);

       


        // Initialize GameManager
        if (GameManager == null)
        {
            GameManager = GameManager.Instance;
            if (GameManager == null)
            {
                Debug.LogError("GameManager not found in the scene!.");
            }
        }

        // Restore UI state if wallet is already connected
        if (wallet != null && !string.IsNullOrEmpty(walletAddress))
        {
            Debug.Log($"Restoring wallet connection: {walletAddress}");
            if (ConnectButton != null) ConnectButton.SetActive(false);
            if (DisconnectButton != null)
            {
                DisconnectButton.SetActive(true);
                var buttonComponent = DisconnectButton.GetComponent<UnityEngine.UI.Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = true;
                }
            }
            if (ConnectedText != null)
            {
                ConnectedText.gameObject.SetActive(true);
                ConnectedText.text = "Connected";
            }
            if (AddressText != null)
            {
                AddressText.gameObject.SetActive(true);
                string shortAddress = $"{walletAddress.Substring(0, 5)}...{walletAddress.Substring(walletAddress.Length - 5)}";
                AddressText.text = shortAddress;
            }
           
        }
        else
        {
            // Standard UI initialization for disconnected state
            if (ConnectButton != null) ConnectButton.SetActive(true);
            if (DisconnectButton != null)
            {
                DisconnectButton.SetActive(false);
                var buttonComponent = DisconnectButton.GetComponent<UnityEngine.UI.Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = true;
                }
                else
                {
                    Debug.LogError("DisconnectButton does not have a Button component!");
                }
            }
            if (ClaimButton != null)
            {
                ClaimButton.interactable = true;
            }
            if (ConnectedText != null) ConnectedText.gameObject.SetActive(false);
            if (AddressText != null) AddressText.gameObject.SetActive(false);
            if (EthBalanceText != null) EthBalanceText.gameObject.SetActive(false);
            if (CustomTokenBalanceText != null) CustomTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedTokenBalanceText != null) ClaimedTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedNFTText != null) ClaimedNFTText.gameObject.SetActive(false);
            if (NftDisplayCanvas != null) NftDisplayCanvas.gameObject.SetActive(false);
        }
    }

  
    private void Update()
    {
        // Update UI feedback animation (e.g., "Processing...")
        if (Time.time - lastFeedbackUpdateTime > 0.5f)
        {
            feedbackDotCount = (feedbackDotCount + 1) % 4;
            string dots = new string('.', feedbackDotCount);
            if (ConnectedText != null && ConnectedText.text.StartsWith("Connecting"))
            {
                ConnectedText.text = $"Connecting{dots}";
            }
            if (ClaimedTokenBalanceText != null && ClaimedTokenBalanceText.text.StartsWith("Claiming"))
            {
                ClaimedTokenBalanceText.text = $"Claiming{dots}";
            }
            if (ClaimedNFTText != null && ClaimedNFTText.text.StartsWith("Claiming"))
            {
                ClaimedNFTText.text = $"Claiming{dots}";
            }
            lastFeedbackUpdateTime = Time.time;
        }
    }

    public async void Connect()
    {
        if (thirdwebManager == null)
        {
            Debug.LogError("Cannot connect: ThirdwebManager is not initialized.");
            if (ConnectedText != null)
            {
                ConnectedText.gameObject.SetActive(true);
                ConnectedText.text = "Error: ThirdwebManager missing";
            }
            return;
        }

        try
        {
            if (ConnectedText != null)
            {
                ConnectedText.gameObject.SetActive(true);
                ConnectedText.text = "Connecting...";
            }
            if (DisconnectButton != null)
            {
                var disconnectButton = DisconnectButton.GetComponent<Button>();
                if (disconnectButton != null)
                {
                    disconnectButton.interactable = false;
                }
            }

            // Disconnect existing wallet if connected
            if (wallet != null)
            {
                await wallet.Disconnect();
                wallet = null;
                walletAddress = null;
                Debug.Log("Disconnected existing wallet to start new connection.");
            }

            var options = new WalletOptions(
                provider: WalletProvider.WalletConnectWallet,
                chainId: 84532
            );

            Debug.Log("WebGL: Initiating WalletConnect connection...");
#if UNITY_WEBGL
            Debug.Log("WebGL: Ensure browser supports WebSockets and localhost allows outbound connections.");
#endif

            // Add timeout for WalletConnect connection
            async Task<IThirdwebWallet> ConnectWithTimeout(WalletOptions opts, int timeoutMs)
            {
                var connectTask = ThirdwebManager.Instance.ConnectWallet(opts);
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("WalletConnect connection timed out after " + timeoutMs + "ms");
                }
                return await connectTask;
            }

            // Attempt connection with retry logic
            int maxRetries = 2;
            int retryCount = 0;
            bool connected = false;
            while (retryCount <= maxRetries && !connected)
            {
                try
                {
                    wallet = await ConnectWithTimeout(options, 30000); // 30s timeout
                    walletAddress = await wallet.GetAddress();
                    connected = true;
                    Debug.Log($"Wallet connected successfully! Address: {walletAddress}");
                }
                catch (System.Exception ex)
                {
                    retryCount++;
                    string errorMsg = $"Connection attempt {retryCount}/{maxRetries} failed: {ex.Message}";
                    Debug.LogWarning(errorMsg);
                    if (retryCount > maxRetries)
                    {
                        throw new System.Exception(errorMsg);
                    }
                    await Task.Delay(2000); // Wait before retrying
                    Debug.Log("Retrying WalletConnect connection...");
                }
            }

            var balance = await wallet.GetBalance(chainId: ActiveChainId);
            var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);
            Debug.Log($"Wallet balance: {balanceEth}");
            if (EthBalanceText != null)
            {
                EthBalanceText.gameObject.SetActive(true);
                EthBalanceText.text = $"ETH: {balanceEth}";
            }

            if (!string.IsNullOrEmpty(TokenContractAddress))
            {
                var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
                var decimals = 2;
                var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
                var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
                Debug.Log($"Custom token balance for {walletAddress}: {tokenBalanceFormatted}");
                if (CustomTokenBalanceText != null)
                {
                    CustomTokenBalanceText.gameObject.SetActive(true);
                    CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
                }
            }

            if (ConnectButton != null)
            {
                ConnectButton.SetActive(false);
                var connectButton = ConnectButton.GetComponent<Button>();
                if (connectButton != null)
                {
                    connectButton.interactable = true;
                }
            }
            if (DisconnectButton != null)
            {
                DisconnectButton.SetActive(true);
                var buttonComponent = DisconnectButton.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = true;
                }
            }
            if (ConnectedText != null)
            {
                ConnectedText.text = "Connected";
            }
            if (AddressText != null && !string.IsNullOrEmpty(walletAddress))
            {
                AddressText.gameObject.SetActive(true);
                string shortAddress = $"{walletAddress.Substring(0, 3)}...{walletAddress.Substring(walletAddress.Length - 3)}";
                AddressText.text = shortAddress;
            }
        }
        catch (TimeoutException ex)
        {
            Debug.LogWarning($"Wallet connection timed out: {ex.Message}");
            if (ConnectedText != null)
            {
                ConnectedText.text = "Connection Timeout: Check wallet app or network";
            }
            ResetUIAfterFailure();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Wallet connection failed: {ex.Message}");
            if (ConnectedText != null)
            {
                ConnectedText.text = $"Connection Failed: {ex.Message}";
            }
            ResetUIAfterFailure();
        }
    }

    private void ResetUIAfterFailure()
    {
        wallet = null;
        walletAddress = null;

        if (ConnectButton != null)
        {
            ConnectButton.SetActive(true);
            var connectButton = ConnectButton.GetComponent<Button>();
            if (connectButton != null)
            {
                connectButton.interactable = true;
            }
        }
        if (DisconnectButton != null)
        {
            DisconnectButton.SetActive(false);
            var disconnectButton = DisconnectButton.GetComponent<Button>();
            if (disconnectButton != null)
            {
                disconnectButton.interactable = false;
            }
        }
        if (AddressText != null) AddressText.gameObject.SetActive(false);
        if (EthBalanceText != null) EthBalanceText.gameObject.SetActive(false);
        if (CustomTokenBalanceText != null) CustomTokenBalanceText.gameObject.SetActive(false);
        if (ClaimedTokenBalanceText != null) ClaimedTokenBalanceText.gameObject.SetActive(false);
        if (ClaimedNFTText != null) ClaimedNFTText.gameObject.SetActive(false);
        if (NftDisplayCanvas != null) NftDisplayCanvas.gameObject.SetActive(false);
    }

    public async void Disconnect()
    {
        if (wallet == null)
        {
            Debug.LogWarning("No wallet to disconnect.");
            return;
        }

        try
        {
            Debug.Log("Disconnecting wallet...");
            await wallet.Disconnect();
            wallet = null;
            walletAddress = null;

            if (ConnectButton != null)
            {
                ConnectButton.SetActive(true);
                var connectButton = ConnectButton.GetComponent<Button>();
                if (connectButton != null) connectButton.interactable = true;
            }
            if (DisconnectButton != null) DisconnectButton.SetActive(false);
            if (ClaimButton != null) ClaimButton.interactable = true;
            if (ConnectedText != null) ConnectedText.gameObject.SetActive(false);
            if (AddressText != null) AddressText.gameObject.SetActive(false);
            if (EthBalanceText != null) EthBalanceText.gameObject.SetActive(false);
            if (CustomTokenBalanceText != null) CustomTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedTokenBalanceText != null) ClaimedTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedNFTText != null) ClaimedNFTText.gameObject.SetActive(false);
            if (NftDisplayCanvas != null) NftDisplayCanvas.gameObject.SetActive(false);
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to disconnect wallet: {ex.Message}");
        }
    }

    public async void SendEth()
    {
        if (thirdwebManager == null || wallet == null)
        {
            Debug.LogError("Cannot send ETH: ThirdwebManager or wallet not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(ToAddress) || !ToAddress.StartsWith("0x") || ToAddress.Length != 42)
        {
            Debug.LogError("Invalid recipient address.");
            return;
        }

        if (string.IsNullOrEmpty(Amount) || !float.TryParse(Amount, out float ethAmount) || ethAmount <= 0)
        {
            Debug.LogError("Invalid ETH amount.");
            return;
        }

        try
        {
            Debug.Log($"Sending {Amount} ETH to {ToAddress}...");
            if (wallet is WalletConnectWallet walletConnect)
            {
                await walletConnect.EnsureCorrectNetwork(ActiveChainId);
            }
            await Task.Delay(5000);
            string weiAmountString = Utils.ToWei(Amount);
            BigInteger weiAmount = BigInteger.Parse(weiAmountString);
            var transactionResult = await wallet.Transfer(ActiveChainId, ToAddress, weiAmount);
            Debug.Log($"ETH sent! Transaction Hash: {transactionResult.TransactionHash}");

            var balance = await wallet.GetBalance(chainId: ActiveChainId);
            var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);
            if (EthBalanceText != null)
            {
                EthBalanceText.gameObject.SetActive(true);
                EthBalanceText.text = $"ETH: {balanceEth}";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to send ETH: {ex.Message}");
        }
    }

    public async void SendCustomToken()
    {
        if (thirdwebManager == null || wallet == null)
        {
            Debug.LogError("Cannot send token: ThirdwebManager or wallet not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(TokenContractAddress) || string.IsNullOrEmpty(TokenRecipientAddress))
        {
            Debug.LogError("Invalid token contract or recipient address.");
            return;
        }

        if (string.IsNullOrEmpty(TokenAmount) || !float.TryParse(TokenAmount, out float tokenAmount) || tokenAmount <= 0)
        {
            Debug.LogError("Invalid token amount.");
            return;
        }

        try
        {
            Debug.Log($"Sending {TokenAmount} {TokenName} to {TokenRecipientAddress}...");
            if (wallet is WalletConnectWallet walletConnect)
            {
                await walletConnect.EnsureCorrectNetwork(ActiveChainId);
            }
            await Task.Delay(5000);
            var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
            var decimals = 2;
            string tokenAmountInWei = Utils.ToWei(TokenAmount);
            BigInteger tokenAmountBigInt = BigInteger.Parse(tokenAmountInWei);
            var transactionResult = await contract.ERC20_Transfer(wallet, TokenRecipientAddress, tokenAmountBigInt);
            Debug.Log($"Token sent! Transaction Hash: {transactionResult.TransactionHash}");

            var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
            var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
            if (CustomTokenBalanceText != null)
            {
                CustomTokenBalanceText.gameObject.SetActive(true);
                CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to send {TokenName}: {ex.Message}");
        }
    }
    [Obsolete]
    public async void ClaimToken()
    {
        
        try
        {
            if (ClaimButton != null) ClaimButton.interactable = false;
            if (ClaimedTokenBalanceText != null)
            {
                ClaimedTokenBalanceText.gameObject.SetActive(true);
                ClaimedTokenBalanceText.text = "Claiming...";
            }

            float totalXP = GameManager.GetTotalXP();
            decimal tokenAmount = (decimal)totalXP;
            ClaimTokenAmount = tokenAmount.ToString();
            var contract = await ThirdwebManager.Instance.GetContract(ClaimTokenContractAddress, ActiveChainId);
            var decimals = 2;
            string claimAmountInWei = Utils.ToWei(tokenAmount.ToString());
            Debug.Log($"Claiming {tokenAmount} tokens ({claimAmountInWei} wei) based on {totalXP} XP");

            if (wallet is WalletConnectWallet walletConnect)
            {
                await walletConnect.EnsureCorrectNetwork(ActiveChainId);
            }
            //await Task.Delay(5000);

            var transactionResult = await contract.DropERC20_Claim(wallet, walletAddress, ClaimTokenAmount);
            //var transactionResult = await contract.TokenERC20_MintTo(wallet, walletAddress, ClaimTokenAmount);
           
            //var transactionResult = await contract.Write(wallet, "claim", 0, ClaimTokenAmount);

            Debug.Log($"Tokens claimed successfully! Transaction Hash: {transactionResult.TransactionHash}");
            //await Task.Delay(5000);

            var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
            var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
            Debug.Log($"Updated token balance for {walletAddress}: {tokenBalanceFormatted}");
            if (ClaimedTokenBalanceText != null)
            {
                ClaimedTokenBalanceText.text = $"Claimed: {totalXP} Color";
                ClaimedTokenBalanceText.text = $"Color: {tokenBalanceFormatted}";
            }
            if (ClaimButton != null) ClaimButton.interactable = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to claim tokens: {ex.Message}");
            if (ClaimedTokenBalanceText != null)
            {
                ClaimedTokenBalanceText.text = $"Claim Failed: {ex.Message}";
            }
            if (ClaimButton != null) ClaimButton.interactable = true;
        }
    }

    public async void ConnectWithEcosystem()
    {
        if (thirdwebManager == null)
        {
            Debug.LogError("Cannot connect: ThirdwebManager is not initialized.");
            return;
        }

        try
        {
            if (ConnectedText != null)
            {
                ConnectedText.gameObject.SetActive(true);
                ConnectedText.text = "Connecting...";
            }
            if (DisconnectButton != null)
            {
                var disconnectButton = DisconnectButton.GetComponent<Button>();
                if (disconnectButton != null)
                {
                    disconnectButton.interactable = false;
                }
            }

            if (wallet != null)
            {
                await wallet.Disconnect();
                wallet = null;
                walletAddress = null;
                Debug.Log("Disconnected existing wallet to start new connection.");
            }

            var ecosystemWalletOptions = new EcosystemWalletOptions(ecosystemId: "ecosystem.your-ecosystem", email: "myepicemail@domain.id");
            var options = new WalletOptions(
                provider: WalletProvider.EcosystemWallet,
                chainId: 84532,
                ecosystemWalletOptions: ecosystemWalletOptions
            );
            Debug.Log("Initiating ecosystem wallet connection...");
            wallet = await ThirdwebManager.Instance.ConnectWallet(options);
            walletAddress = await wallet.GetAddress();
            Debug.Log($"Wallet connected successfully! Address: {walletAddress}");

            var balance = await wallet.GetBalance(chainId: ActiveChainId);
            var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);
            Debug.Log($"Wallet balance: {balanceEth}");
            if (EthBalanceText != null)
            {
                EthBalanceText.gameObject.SetActive(true);
                EthBalanceText.text = $"ETH: {balanceEth}";
            }

            if (!string.IsNullOrEmpty(TokenContractAddress))
            {
                var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
                var decimals = 2;
                var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
                var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
                Debug.Log($"Custom token balance for {walletAddress}: {tokenBalanceFormatted}");
                if (CustomTokenBalanceText != null)
                {
                    CustomTokenBalanceText.gameObject.SetActive(true);
                    CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
                }
            }

            if (ConnectButton != null)
            {
                ConnectButton.SetActive(true);
                var connectButton = ConnectButton.GetComponent<Button>();
                if (connectButton != null)
                {
                    connectButton.interactable = true;
                }
            }
            if (DisconnectButton != null)
            {
                DisconnectButton.SetActive(true);
                var buttonComponent = DisconnectButton.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.interactable = true;
                }
            }
            if (ConnectedText != null)
            {
                ConnectedText.text = "Connected";
            }
            if (AddressText != null && !string.IsNullOrEmpty(walletAddress))
            {
                AddressText.gameObject.SetActive(true);
                string shortAddress = $"{walletAddress.Substring(0, 3)}...{walletAddress.Substring(walletAddress.Length - 3)}";
                AddressText.text = shortAddress;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Wallet connection failed or canceled: {ex.Message}");
            if (ConnectedText != null)
            {
                ConnectedText.text = $"Connection Failed: {ex.Message}";
            }
            wallet = null;
            walletAddress = null;

            if (ConnectButton != null)
            {
                ConnectButton.SetActive(true);
                var connectButton = ConnectButton.GetComponent<Button>();
                if (connectButton != null)
                {
                    connectButton.interactable = true;
                }
            }
            if (DisconnectButton != null)
            {
                DisconnectButton.SetActive(false);
                var disconnectButton = DisconnectButton.GetComponent<Button>();
                if (disconnectButton != null)
                {
                    disconnectButton.interactable = false;
                }
            }
            if (ConnectedText != null)
            {
                ConnectedText.gameObject.SetActive(false);
            }
            if (AddressText != null) AddressText.gameObject.SetActive(false);
            if (EthBalanceText != null) EthBalanceText.gameObject.SetActive(false);
            if (CustomTokenBalanceText != null) CustomTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedTokenBalanceText != null) ClaimedTokenBalanceText.gameObject.SetActive(false);
            if (ClaimedNFTText != null) ClaimedNFTText.gameObject.SetActive(false);
            if (NftDisplayCanvas != null) NftDisplayCanvas.gameObject.SetActive(false);
        }
    }


    public async void Login(string authProvider)
    {

        if (ConnectedText != null)
        {
            ConnectedText.gameObject.SetActive(true);
            ConnectedText.text = "Connecting...";
        }
        if (DisconnectButton != null)
        {
            var disconnectButton = DisconnectButton.GetComponent<Button>();
            if (disconnectButton != null)
            {
                disconnectButton.interactable = false;
            }
        }

        AuthProvider provider = AuthProvider.Google;
        switch (authProvider)
        {
            case "google":
                provider = AuthProvider.Google;
                break;
            case "apple":
                provider = AuthProvider.Apple;
                break;
            case "facebook":
                provider = AuthProvider.Facebook;
                break;
        }

        var connection = new WalletOptions(
            provider: WalletProvider.InAppWallet,
            chainId: 84532,
            inAppWalletOptions: new InAppWalletOptions(authprovider: provider),
            smartWalletOptions: new SmartWalletOptions(sponsorGas: true)
        );

        wallet = await ThirdwebManager.Instance.ConnectWallet(connection);
        walletAddress = await wallet.GetAddress();

        OnLoggedIn?.Invoke(walletAddress);

        var balance = await wallet.GetBalance(chainId: ActiveChainId);
        var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);
        //Debug.Log($"Wallet balance: {balanceEth}");
        if (EthBalanceText != null)
        {
            EthBalanceText.gameObject.SetActive(true);
            EthBalanceText.text = $"ETH: {balanceEth}";
        }

        if (!string.IsNullOrEmpty(TokenContractAddress))
        {
            var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
            var decimals = 2;
            var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
            var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
            //Debug.Log($"Custom token balance for {walletAddress}: {tokenBalanceFormatted}");
            if (CustomTokenBalanceText != null)
            {
                CustomTokenBalanceText.gameObject.SetActive(true);
                CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
            }
        }

        
        
        if (ConnectedText != null)
        {
            ConnectedText.text = "Connected";
        }
        if (AddressText != null && !string.IsNullOrEmpty(walletAddress))
        {
            AddressText.gameObject.SetActive(true);
            string shortAddress = $"{walletAddress.Substring(0, 3)}...{walletAddress.Substring(walletAddress.Length - 3)}";
            AddressText.text = shortAddress;
        }

        GameManager.OnWalletLoggedIn();
    }

    public async void GetCustomTokenBalanceAsync()
    {
        if (thirdwebManager == null)
        {
            Debug.LogError("Cannot fetch token balance: ThirdwebManager is not initialized.");
           
        }

        if (wallet == null || string.IsNullOrEmpty(walletAddress))
        {
            Debug.LogWarning("No wallet connected to fetch token balance.");
          
        }

        if (string.IsNullOrEmpty(TokenContractAddress))
        {
            Debug.LogWarning("Token contract address is not set.");
            
        }

        try
        {
            var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
            var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
            var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), 2, addCommas: true);
            Debug.Log($"Fetched live custom token balance for {walletAddress}: {tokenBalanceFormatted}");
            if (CustomTokenBalanceText != null)
            {
                CustomTokenBalanceText.gameObject.SetActive(true);
                CustomTokenBalanceText.text = $"Color: {tokenBalanceFormatted}";
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch custom token balance: {ex.Message}");
            
        }
    }

    public string GetConnectedWallet()
    {
        if (wallet != null && !string.IsNullOrEmpty(walletAddress))
        {
            //Debug.Log($"Retrieved connected wallet address: {walletAddress}");
            return walletAddress;
        }
        else
        {
            Debug.LogWarning("No wallet is currently connected.");
            return null;
        }
    }

    internal async Task SubmitScore(float score)
    {
        //Debug.Log($"Submitting score of {score} to blockchain for address {walletAddress}");
        var contract = await ThirdwebManager.Instance.GetContract(
            LeaderboardContractAddress,
            84532
        );
        await contract.Write(wallet, "submitScore", 0, (int)score);
    }

    


    public async Task ReadScore(int position)
    {
        if (string.IsNullOrEmpty(LeaderboardContractAddress))
        {
            Debug.LogError("LeaderboardContractAddress is not set in the Inspector!");
            return;
        }

        try
        {
            Debug.Log("fetching...");
            // Get the contract instance
            var contract = await ThirdwebManager.Instance.GetContract(
                LeaderboardContractAddress,
                ActiveChainId
                
            );
            //Debug.Log(" Starting to fetch");

            // Read the 0th score from the scores
            // Read the top scores from the contract
            uint topScore = await contract.Read<uint>("getScoreByPosition",position);
            readScore = topScore;
            scoreList.Add(readScore);
            //Debug.Log($"{position +1 }th Score: {readScore}");
            string PlayerName = await contract.Read<string>("getPlayerNameByPosition", position);
            readName = PlayerName;
            nameList.Add(readName);
            //Debug.Log($"{position + 1}th name : {PlayerName}");  // Logs 1, which is correct
            //uint timestamp = await contract.Read<uint>("getTimestampByPosition", position);
            //readTimestamp = timestamp;
            //Debug.Log($"{position + 1}th timestamp : {timestamp}");  // Logs 1, which is correct
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to read first score: {ex.Message}");
        }
    }

    

    public async Task RegisterLeaderboardName(string name)
    {
        //Debug.Log($"Registering {name} to blockchain for address {walletAddress}");
        var contract = await ThirdwebManager.Instance.GetContract(
            LeaderboardContractAddress,
            84532
        );
        //Debug.Log("stage1");
        await contract.Write(wallet, "setPlayerName", 0, name);
        //Debug.Log("Registered");
        await ReadName(0);
    }

    public async Task ReadName(uint position)
    {
        //Debug.Log($"Reading name to blockchain for position {position}");
        var contract = await ThirdwebManager.Instance.GetContract(
            LeaderboardContractAddress,
            84532
        );
        string gamename = await contract.Read<string>("getPlayerNameByPosition", position);
        Gamename = gamename;
        //Debug.Log(Gamename);
    }

    public async Task GetTotalScorers()
    {
        //bug.Log($"Reading name to blockchain for position {walletAddress}");
        var contract = await ThirdwebManager.Instance.GetContract(
            LeaderboardContractAddress,
            84532
        );
        scorers = await contract.Read<uint>("getTotalScores");
        //Debug.Log(scorers);
    }

    public uint TotalScorers()
    {
        return scorers;
    }

    public string GameName()
    {
        return Gamename;
    }

    public uint GetScore()
    {
        return readScore;
    }


    public uint GetLeaderboardLength()
    {
        return LeaderboardLength;
    }

    public async Task GetScoreList()
    {
        for(int i = 0; i < LeaderboardLength; i++)
        {
            await ReadScore(i);
        }
    }

    public List<uint> ScoreList()
    {
        return scoreList;
    }
    
    public List<string> NameList()
    {
        return nameList;
    }
}



