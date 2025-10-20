using Newtonsoft.Json.Linq;
using System; // Added for TimeoutException
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Thirdweb;
using Thirdweb.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;




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
    public string BuyTokenContractAddress { get; set; }
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
    //[field: SerializeField]
    public string BuyNftAddress { get; set; }
    [field: SerializeField]
    public string BuyNftBrownAddress { get; set; }
    [field: SerializeField]
    public string BuyNftGhostAddress { get; set; }
    [field: SerializeField]
    public string BuyNftHealthAddress { get; set; }

    [field: SerializeField, Header("UI Elements")]
    public GameManager GameManager { get; set; }
    [field: SerializeField]
    public Button ClaimButton { get; set; }
    //[field: SerializeField]
    public Button BuyButton { get; set; }
    //[field: SerializeField]
    public Button UseButton { get; set; }
    [field: SerializeField]
    public Button BuyButton1 { get; set; }
    [field: SerializeField]
    public Button UseButton1 { get; set; }
    [field: SerializeField]
    public Button BuyButton2 { get; set; }
    [field: SerializeField]
    public Button UseButton2 { get; set; }
    [field: SerializeField]
    public Button BuyButtonH { get; set; }
    [field: SerializeField]
    public Button UseButtonH { get; set; }
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
    //[field: SerializeField]
    public TextMeshProUGUI BuyTokenBalanceText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI BuyTokenBalanceText1 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI BuyTokenBalanceText2 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI BuyTokenBalanceTextH { get; set; }
    //[field: SerializeField]
    public TextMeshProUGUI UseTokenText { get; set; }
    //public TextMeshProUGUI UseTokenBalanceText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenText1 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenText2 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenTextH { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI TokenBalanceText { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenBalanceText1 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenBalanceText2 { get; set; }
    [field: SerializeField]
    public TextMeshProUGUI UseTokenBalanceTextH { get; set; }

    [field: SerializeField, Header("NFT Display Canvas")]
    public Canvas NftDisplayCanvas { get; set; }
    [field: SerializeField]
    public GameObject NftDisplayPrefab { get; set; }
    [field: SerializeField]
    public Transform NftDisplayParent { get; set; }

    [field: SerializeField] 
    public BigInteger BrownPrice { get; set; } = 100;
    [field: SerializeField] 
    public BigInteger GhostPrice { get; set; } = 200;

    private BigInteger colorPrice;
    

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
                provider: WalletProvider.ReownWallet,
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

    //public async void SendEth()
    //{
    //    if (thirdwebManager == null || wallet == null)
    //    {
    //        Debug.LogError("Cannot send ETH: ThirdwebManager or wallet not initialized.");
    //        return;
    //    }

    //    if (string.IsNullOrEmpty(ToAddress) || !ToAddress.StartsWith("0x") || ToAddress.Length != 42)
    //    {
    //        Debug.LogError("Invalid recipient address.");
    //        return;
    //    }

    //    if (string.IsNullOrEmpty(Amount) || !float.TryParse(Amount, out float ethAmount) || ethAmount <= 0)
    //    {
    //        Debug.LogError("Invalid ETH amount.");
    //        return;
    //    }

    //    try
    //    {
    //        Debug.Log($"Sending {Amount} ETH to {ToAddress}...");
    //        if (wallet is WalletConnectWallet walletConnect)
    //        {
    //            await walletConnect.EnsureCorrectNetwork(ActiveChainId);
    //        }
    //        await Task.Delay(5000);
    //        string weiAmountString = Utils.ToWei(Amount);
    //        BigInteger weiAmount = BigInteger.Parse(weiAmountString);
    //        var transactionResult = await wallet.Transfer(ActiveChainId, ToAddress, weiAmount);
    //        Debug.Log($"ETH sent! Transaction Hash: {transactionResult.TransactionHash}");

    //        var balance = await wallet.GetBalance(chainId: ActiveChainId);
    //        var balanceEth = Utils.ToEth(wei: balance.ToString(), decimalsToDisplay: 2, addCommas: true);
    //        if (EthBalanceText != null)
    //        {
    //            EthBalanceText.gameObject.SetActive(true);
    //            EthBalanceText.text = $"ETH: {balanceEth}";
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        Debug.LogError($"Failed to send ETH: {ex.Message}");
    //    }
    //}

    //public async void SendCustomToken()
    //{
    //    if (thirdwebManager == null || wallet == null)
    //    {
    //        Debug.LogError("Cannot send token: ThirdwebManager or wallet not initialized.");
    //        return;
    //    }

    //    if (string.IsNullOrEmpty(TokenContractAddress) || string.IsNullOrEmpty(TokenRecipientAddress))
    //    {
    //        Debug.LogError("Invalid token contract or recipient address.");
    //        return;
    //    }

    //    if (string.IsNullOrEmpty(TokenAmount) || !float.TryParse(TokenAmount, out float tokenAmount) || tokenAmount <= 0)
    //    {
    //        Debug.LogError("Invalid token amount.");
    //        return;
    //    }

    //    try
    //    {
    //        Debug.Log($"Sending {TokenAmount} {TokenName} to {TokenRecipientAddress}...");
    //        if (wallet is WalletConnectWallet walletConnect)
    //        {
    //            await walletConnect.EnsureCorrectNetwork(ActiveChainId);
    //        }
    //        await Task.Delay(5000);
    //        var contract = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
    //        var decimals = 2;
    //        string tokenAmountInWei = Utils.ToWei(TokenAmount);
    //        BigInteger tokenAmountBigInt = BigInteger.Parse(tokenAmountInWei);
    //        var transactionResult = await contract.ERC20_Transfer(wallet, TokenRecipientAddress, tokenAmountBigInt);
    //        Debug.Log($"Token sent! Transaction Hash: {transactionResult.TransactionHash}");

    //        var tokenBalance = await contract.ERC20_BalanceOf(walletAddress);
    //        var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
    //        if (CustomTokenBalanceText != null)
    //        {
    //            CustomTokenBalanceText.gameObject.SetActive(true);
    //            CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
    //        }
    //    }
    //    catch (System.Exception ex)
    //    {
    //        Debug.LogError($"Failed to send {TokenName}: {ex.Message}");
    //    }
    //}
    [Obsolete]
    public async void ClaimToken()
    {

        try
        {
            if (ClaimButton != null) ClaimButton.interactable = false;
            Debug.Log(wallet);
            Debug.Log(walletAddress);

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

            //if (wallet is WalletConnectWallet walletConnect)
            //{
            //    await walletConnect.EnsureCorrectNetwork(ActiveChainId);
            //}
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

    //[Obsolete]
    public async void Buy(int colorIndex)
    {
        try
        {
            SetColorUIElements(colorIndex);
            if (BuyButton != null) BuyButton.interactable = false;

            if (BuyTokenBalanceText != null)
            {
                BuyTokenBalanceText.gameObject.SetActive(true);
                BuyTokenBalanceText.text = "Buying...";
            }

            BuyNftAddress = GetColorContractAddress(colorIndex);

            if (!string.IsNullOrEmpty(BuyTokenContractAddress))
            {
                var tokencontract = await ThirdwebManager.Instance.GetContract(BuyTokenContractAddress, ActiveChainId);
                var decimals = 2;
                var tokenBalance = await tokencontract.ERC20_BalanceOf(walletAddress);
                Debug.Log($"Balance: {tokenBalance} tokens");
                var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
                if (tokenBalance < colorPrice) {
                    if (BuyTokenBalanceText != null)
                    {
                        BuyTokenBalanceText.gameObject.SetActive(true);
                        BuyTokenBalanceText.text = $"Need {colorPrice} color...";
                    }
                }
                else
                {                    
                    var nftcontract = await ThirdwebManager.Instance.GetContract(BuyNftAddress, ActiveChainId);
                    Debug.Log("Approving token spend...");
                    await tokencontract.ERC20_Approve(wallet, BuyNftAddress, tokenBalance);
                    Debug.Log("Token spend approved.");
                    BigInteger nftBalance = await nftcontract.ERC721_BalanceOf(walletAddress);
                    //Debug.Log("Waiting here.

                    Debug.Log($"Balance: {nftBalance} NFTs");
                    var transactionResult = await nftcontract.DropERC721_Claim(wallet, walletAddress, 1);
                    Debug.Log($"NFTs claimed successfully! Transaction Hash: {transactionResult.TransactionHash}");
                    var finalnftBalance = await nftcontract.ERC721_BalanceOf(walletAddress);
                    Debug.Log($"Balance: {finalnftBalance} NFTs");
                    if (finalnftBalance == nftBalance)
                    {
                        Debug.LogError($"No NFTs owned by {walletAddress} after claim. Check contract logic or transaction.");
                        if (BuyTokenBalanceText != null)
                        {
                            BuyTokenBalanceText.text = $"No NFTs owned. Tx Hash: {transactionResult.TransactionHash}";
                        }
                        SetNftBalances();
                        return;
                    }

                    if (finalnftBalance < 1)
                    {
                        //Disable buy button
                        if (UseButton != null)
                        {
                            UseButton.interactable = false;
                        }
                    }
                    else {
                        //Enable use button
                        if (UseButton != null) {
                            UseButton.interactable = true;
                        }
                    }

                    if (BuyTokenBalanceText != null)
                    {
                        //ClaimedNFTText.text = $"Claimed! Tx Hash: {transactionResult.TransactionHash}\nBalance: {tokenBalance}";
                        BuyTokenBalanceText.text = $"Claimed!";
                        //ClaimedNft.SetActive(true);
                        SetNftBalances();

                    }

                    if (BuyButton != null)
                    {
                        //GameManager.LevelClaimed();
                        BuyButton.interactable = true;
                    }
                }
                //Debug.Log($"Custom token balance for {walletAddress}: {tokenBalanceFormatted}");
                //if (CustomTokenBalanceText != null)
                //{
                //    CustomTokenBalanceText.gameObject.SetActive(true);
                //    CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
                //}
            }
        }
        catch (System.Exception ex)
        {
            if (BuyButton != null) BuyButton.interactable = true;
            Debug.LogError($"Failed to open URL: {ex.Message}");

        }
    }

    
    public void SetNft()
    {
        //if (isBrown)
        //{
        //    //audioManager.PlayPortalAnimationSound();
        //    BrownNft.SetActive(true);
        //}
        //if (isWhite)
        //{
        //    //audioManager.PlayPortalAnimationSound();
        //    WhiteNft.SetActive(true);
        //}

        
        Debug.Log($"Bought");
    }

    //[Obsolete]
    public async void UseColor(int colorIndex)
    {
        try {
            SetColorUIElements(colorIndex);

            if (UseButton != null) UseButton.interactable = false;

            if (UseTokenText != null)
            {
                UseTokenText.gameObject.SetActive(true);
                UseTokenText.text = "Preparing to use...";
            }
            if (colorIndex == 2)
            {
                if (GameManager.GetHealth() == 3)
                {
                    UseTokenText.text = "Health is full!";
                    if (UseButton != null) UseButton.interactable = true;
                    return;
                }
            }
            BuyNftAddress = GetColorContractAddress(colorIndex);

            if (!string.IsNullOrEmpty(BuyNftAddress))
            {
                var nftcontract = await ThirdwebManager.Instance.GetContract(BuyNftAddress, ActiveChainId);
                var initialnftBalance = await nftcontract.ERC721_BalanceOf(walletAddress);
                Debug.Log($"Balance: {initialnftBalance} tokens");
               
                if (initialnftBalance < 1)
                {
                    if (UseTokenText != null)
                    {
                        UseTokenText.gameObject.SetActive(true);
                        UseTokenText.text = "No sprays buy more in store...";
                    }
                }
                else
                {
                    //BigInteger nftBalance = await nftcontract.ERC721_BalanceOf(walletAddress);
                    //Debug.Log("Waiting here.");

                    BigInteger tokenId = 0;
                    var ownedNfts = await nftcontract.ERC721_GetOwnedNFTs(walletAddress);
                    //Debug.Log("Passed here.");
                    if (ownedNfts.Count > 0)
                    {
                        tokenId = ownedNfts.Select(nft => BigInteger.Parse(nft.Metadata.Id)).Min();
                        Debug.Log($"Small tokenId:{tokenId}");
                    }
                    else
                    {
                        UseTokenText.text = "No sprays buy more in store...";
                    }

                    Debug.Log($"Smallest Token ID: {tokenId}");
                    //Debug.Log($"Balance: {nftBalance} NFTs");
                    var transactionResult = await nftcontract.DropER721_Burn(wallet, tokenId);
                    Debug.Log($"NFTs burned successfully! Transaction Hash: {transactionResult.TransactionHash}");
                    var finalnftBalance = await nftcontract.ERC721_BalanceOf(walletAddress);
                    Debug.Log($"Balance: {finalnftBalance} NFTs");

                    if (finalnftBalance == initialnftBalance)
                    {
                        Debug.LogError($"No NFTs owned by {walletAddress} after claim. Check contract logic or transaction.");
                        if (UseTokenText != null)
                        {
                            UseTokenText.text = $"No NFTs owned. Tx Hash: {transactionResult.TransactionHash}";
                        }
                        return;
                    }
                    if (colorIndex == 2)
                    {
                        if (GameManager.GetHealth() < 3)
                            GameManager.Heal();
                        else if (GameManager.GetHealth() == 3)
                        {
                            UseTokenText.text = "Health is full!";
                            return;
                        }
                    }
                    else
                        GameManager.UseColor(colorIndex);

                    if (finalnftBalance < 1)
                    {
                        //Disable use button
                        if (UseButton != null)
                        {
                            UseButton.interactable = false;
                        }

                        if (UseTokenText != null)
                        {
                            //ClaimedNFTText.text = $"Claimed! Tx Hash: {transactionResult.TransactionHash}\nBalance: {tokenBalance}";
                            UseTokenText.text = $"Now You can use it in the game!";
                            //ClaimedNft.SetActive(true);
                            SetNftBalances();

                        }

                    }

                    if (UseTokenText != null)
                    {
                        //ClaimedNFTText.text = $"Claimed! Tx Hash: {transactionResult.TransactionHash}\nBalance: {tokenBalance}";
                        UseTokenText.text = $"Now You can use it in the game!";
                        //ClaimedNft.SetActive(true);
                        //SetNft();

                    }

                    if (UseButton != null)
                    {
                        //GameManager.LevelClaimed();
                        UseButton.interactable = true;
                    }

                    //SetNftBalances();
                }
                //Debug.Log($"Custom token balance for {walletAddress}: {tokenBalanceFormatted}");
                //if (CustomTokenBalanceText != null)
                //{
                //    CustomTokenBalanceText.gameObject.SetActive(true);
                //    CustomTokenBalanceText.text = $"{TokenName}: {tokenBalanceFormatted}";
                //}
            }
            else
            {
                Debug.LogError("BuyNftAddress is null or empty.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to use color: {ex.Message}");
            if (UseButton != null) UseButton.interactable = true;
        }
    }

    private IEnumerator DelaySetBalanceCoroutine()
    {
        var delay = 10f; // seconds
        yield return new WaitForSeconds(delay); // uses Time.timeScale
        SetNftBalances();
    }
    public void SetColorUIElements(int colorIndex)
    { 
        if (colorIndex == 0)
        {
            //Brown
            if (UseButton1 != null)
            {
                UseButton = UseButton1;
            }

            if (BuyButton1 != null)
            {
                BuyButton = BuyButton1;
            }

            if(UseTokenText1 != null)
            {
                UseTokenText = UseTokenText1;
            }

            if(BuyTokenBalanceText1 != null)
            {
                BuyTokenBalanceText = BuyTokenBalanceText1;
            }
            colorPrice = 100;
        }
        else if (colorIndex == 1)
        {
            //Ghost
            if (UseButton2 != null)
            {
                UseButton = UseButton2;
            }

            if (BuyButton2 != null)
            {
                BuyButton = BuyButton2;
            }

            if (UseTokenText2 != null)
            {
                UseTokenText = UseTokenText2;
            }

            if (BuyTokenBalanceText2 != null)
            {
                BuyTokenBalanceText = BuyTokenBalanceText2;
            }
            colorPrice = 200;
        }
        else if (colorIndex == 2)
        {
            //Ghost
            if (UseButtonH != null)
            {
                UseButton = UseButtonH;
            }

            if (BuyButtonH != null)
            {
                BuyButton = BuyButtonH;
            }

            if (UseTokenTextH != null)
            {
                UseTokenText = UseTokenTextH;
            }

            if (BuyTokenBalanceTextH != null)
            {
                BuyTokenBalanceText = BuyTokenBalanceTextH;
            }
            colorPrice = 50;
        }

    }
    public string GetColorContractAddress(int colorIndex)
    {
        if (colorIndex == 0)
        {
            BuyNftAddress = BuyNftBrownAddress;
        }
        else if (colorIndex == 1)
        {
            BuyNftAddress = BuyNftGhostAddress;
        }
        else if (colorIndex == 2)
        {
            BuyNftAddress = BuyNftHealthAddress;
        }
        return BuyNftAddress;
    }

    public async void SetNftBalances()
    {
        TokenBalanceText.text = $"Loading balances...";
        var tokens = await ThirdwebManager.Instance.GetContract(TokenContractAddress, ActiveChainId);
        var tokenBalance = await tokens.ERC20_BalanceOf(walletAddress);
        var decimals = 2;
        var tokenBalanceFormatted = Utils.ToEth(tokenBalance.ToString(), decimals, addCommas: true);
        TokenBalanceText.text = $"Owned: {tokenBalanceFormatted}";

        UseTokenBalanceText1.text = $"Loading balances...";
        var brownnft = await ThirdwebManager.Instance.GetContract(BuyNftBrownAddress, ActiveChainId);
        var brownnftBalance = await brownnft.ERC721_BalanceOf(walletAddress);
        UseTokenBalanceText1.text = $"Owned: {brownnftBalance}";

        UseTokenBalanceText2.text = $"Loading balances...";
        var ghostnft = await ThirdwebManager.Instance.GetContract(BuyNftGhostAddress, ActiveChainId);
        var ghostnftBalance = await ghostnft.ERC721_BalanceOf(walletAddress);
        UseTokenBalanceText2.text = $"Owned: {ghostnftBalance}";

        UseTokenBalanceTextH.text = $"Loading balances...";
        var health = await ThirdwebManager.Instance.GetContract(BuyNftHealthAddress, ActiveChainId);
        var healthnftBalance = await health.ERC721_BalanceOf(walletAddress);
        UseTokenBalanceTextH.text = $"Owned: {healthnftBalance}";

        

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
       

        AuthProvider provider = AuthProvider.Farcaster;
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
            case "farcaster":
                provider = AuthProvider.Farcaster;
                break;
        }
        Debug.Log($"Wallet provider: {authProvider}");
        // Initialize client with clientId (not secretKey)
        //var client = ThirdwebClient.Create(clientId: "your_client_id");

        // Use this client in your ThirdwebManager or wallet connection logic
        var connection = new WalletOptions(
            provider: WalletProvider.InAppWallet,
            chainId: 84532,
            inAppWalletOptions: new InAppWalletOptions(authprovider: provider),
            smartWalletOptions: new SmartWalletOptions(sponsorGas: true)
        );
        Debug.Log($"Wallet chainid: {ActiveChainId}");
        wallet = await ThirdwebManager.Instance.ConnectWallet(connection);
        walletAddress = await wallet.GetAddress();
        Debug.Log($"Wallet: {wallet}");
        Debug.Log($"Wallet add: {walletAddress}");
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

    public async void LoginWithReown()
    {
        if (ConnectedText != null)
        {
            ConnectedText.gameObject.SetActive(true);
            ConnectedText.text = "Connecting...";
        }
        var reownOptions = new ReownOptions(
            projectId: "1c367b6687847515ab0a7b9b2f32cb59",
            name: "Paint Your World",
            description: "thirdweb powered experience",
            url: "https://rasta669.github.io/PaintYourWorld/",
            iconUrl: "https://mygame.example/icon.png",
            includedWalletIds: new[] { "eip155:1:metamask" },
            excludedWalletIds: null
        );

        var walletOptions = new WalletOptions(
            provider: WalletProvider.ReownWallet,
            chainId: 84532,
            reownOptions: reownOptions
        );

        wallet = await ThirdwebManager.Instance.ConnectWallet(walletOptions);
        walletAddress = await wallet.GetAddress();
        Debug.Log($"Wallet: {wallet}");
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



