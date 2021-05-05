using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour
{
    //server-located player movement variables for central & live adjustment or future loading from ini file
    public float velocityBoostMultiplier;
    public float rotationalBoostMultiplier;
    public float playerSpeedLimit = 5f;
    public float playerBaseMovementForce = 2;
    public float playerBaseAngularMovementForce = 2;
    public float speedLimitBoostMultiplier;
    public float angularSpeedLimitBoost = 10f;
    public float angularSpeedLimitNormal = 4f;

    public List<Color> playerColors;

    [SyncVar] public bool syncVar_RewindingActive;
    public AudioClip initialCountdownSound; 
    public float wallTurningRate;
    private Quaternion _previousRotation = Quaternion.identity;
    private Quaternion _targetRotation = Quaternion.identity;
    private float turnStartTime = 0f;


    public bool randomizeGravity = true;
    public float randomizeGravityMinSeconds;
    public float randomizeGravityMaxSeconds;
    public float gravityStrength = 10;
    public Transform wallsTransform;
    public int defaultRoundTime = 30;
    public int defaultCountdownTime = 3;
    [HideInInspector]
    public int playerNumberOwningBall = -1;

    public List<GameObject> playerExplosions;

    public List<int> playerScores;

    public TMP_Text PlayerScoreOutput;

    public GameObject centerCountdownTextGameObject;
    public TMP_Text CenterCountdownText;


    public TMP_InputField RoundTimeInputField;
    public TMP_Text RoundTimeTextField;
    public Button RoundStartBtn;
    bool roundActive = false;
    bool startingNewRound = false;

    public Button LevelStartBtn;
    public LevelManager levelManager;
    bool levelActive = false;

    private static GameManager _instance;
    public NetworkManager networkManager;


    public GameObject coinPrefab;
    GameObject newCoin;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.Log("GameManager instance is null, GameManager object/component is probably missing from the scene...");

            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
        networkManager = FindObjectOfType<NetworkManager>();
    }

    [Server]
    void Start()
    {
        playerScores = new List<int>();
        for (int i = 0; i < NetworkManager.singleton.maxConnections; i++)
        {
            playerScores.Add(0);
        }
        // -- THIS WAS GOOD, BUT ENABLED VSYNC AND TIME-CORRECTED INPUT INSTEAD OF BELOW SETTINGS DUE TO TEARING
        //QualitySettings.vSyncCount = 0; // VSync must be disabled.
        //Application.targetFrameRate = 60;
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(StartNewRound);
        Debug.Log("Called Server Start()");
        StartCoroutine(RandomizeGravity(randomizeGravityMinSeconds, randomizeGravityMaxSeconds));
    }

    /*
    IEnumerator ReportInfo()
    {
        while (true)
        {
            Debug.Log("Application.isFocused: " + Application.isFocused);
            Debug.Log("timeScale: " + Time.timeScale);
            Debug.Log("captureFramerate: " + Time.captureFramerate);
            Debug.Log("deltaTime: " + Time.deltaTime);
            Debug.Log("fixedDeltaTime: " + Time.fixedDeltaTime);
            Debug.Log("Time.time: " + Time.time);
            Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

            yield return new WaitForSeconds(1);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Debug.Log("Gained focus");
        }
        else
        {
            Debug.Log("Lost focus");
        }
    }
    */

    IEnumerator RandomizeGravity(float minTime, float maxTime)
    {
        Debug.Log("Started RandomizeGravity()");
        bool gravityOn = false;
        while(true)
        {            
            yield return new WaitForSeconds(Random.Range(minTime, gravityOn?minTime:maxTime));
            if (randomizeGravity)
            {
                if (!gravityOn)
                {
                    switch (Random.Range(0, 3))
                    {
                        case 0:
                            Physics.gravity = gravityStrength * Vector3.up;
                            SetBlendedEulerAngles(new Vector3(-3, 0, 0));
                            break;
                        case 1:
                            Physics.gravity = gravityStrength * Vector3.down;
                            SetBlendedEulerAngles(new Vector3(3, 0, 0));
                            break;
                        case 2:
                            Physics.gravity = gravityStrength * Vector3.left;
                            SetBlendedEulerAngles(new Vector3(0, -3, 0));
                            break;
                        case 3:
                            Physics.gravity = gravityStrength * Vector3.right;
                            SetBlendedEulerAngles(new Vector3(0, 3, 0));
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    Physics.gravity = Vector3.zero;
                    SetBlendedEulerAngles(new Vector3(0, 0, 0));
                }
                gravityOn = !gravityOn;
                Debug.Log("New Gravity: " + Physics.gravity.ToString());
            }                
        }
    }

    private void OnConnectedToServer()
    {
        Debug.Log("Connected to server...");
        SetRoundTimeTextToDefault();
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(StartNewRound);
        centerCountdownTextGameObject.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
    {
        /*  Caused issues going full screen - basically broke game...
        if (Input.GetKeyDown(KeyCode.Escape) && !Application.isEditor)
        {
            if (Screen.fullScreen)
            {
                Screen.SetResolution(1280, 720, false);
            }
            else
            {
                Screen.SetResolution(1920, 1080, true);
            }
        }
        */

        /*
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
        */
        /*
        if (Input.GetKeyDown(KeyCode.R))
        {      
            Debug.Log("Rotating...");
            SetBlendedEulerAngles(new Vector3(3, 0, 0));
        }
        */

        // Turn towards our target rotation.
        wallsTransform.rotation = Quaternion.Slerp(_previousRotation, _targetRotation, wallTurningRate * (Time.time - turnStartTime));
    }


    private void SetBlendedEulerAngles(Vector3 angles)
    {
        RpcSetBlendedEulerAngles(angles);
        turnStartTime = Time.time;
        _previousRotation = wallsTransform.localRotation;
        _targetRotation = Quaternion.Euler(angles);

    }
    [ClientRpc]
    private void RpcSetBlendedEulerAngles(Vector3 angles)
    {
        turnStartTime = Time.time;
        _previousRotation = wallsTransform.localRotation;
        _targetRotation = Quaternion.Euler(angles);
    }


    public void StartLevel()
    {
        CmdStartLevel();
    }

    [Command(requiresAuthority = false)]
    private void CmdStartLevel()
    {
        if (levelActive || roundActive)
            return;

        levelActive = true;

        var coins = GameObject.FindGameObjectsWithTag("Coin");
        ResetPlayerScores();
        foreach (var coin in coins) Destroy(coin);

        levelManager.StartLevel();

        CameraFollowsPlayer(true);

        SoundManager.ServerPlayMusic("LevelMusic");
    }

    public static void EndLevel()
    {
        _instance.EndLevelInt();
    }
    private void EndLevelInt()
    {
        SoundManager.StopMusic();
        levelActive = false;
        levelManager.ResetLevel();
        MovePlayersToSpawnPoints();
        LoadRoundCoins();
        MoveSphereToCenter();
        ResetCamera();
    }
    private void ResetCamera()
    {
        Camera.main.GetComponent<SmoothCamera2D>().target = null;
        RpcResetCamera();
    }
    [ClientRpc] void RpcResetCamera() => Camera.main.GetComponent<SmoothCamera2D>().target = null;
    
    void MoveSphereToCenter()
    {
        GameObject.FindGameObjectWithTag("Ball").GetComponent<Rigidbody>().velocity = Vector3.zero;
        GameObject.FindGameObjectWithTag("Ball").transform.position = Vector3.zero;
    }

    void LoadRoundCoins()
    {
        var roundCoinSpawns = GameObject.FindGameObjectsWithTag("RoundCoinSpawn");
        foreach (var coinSpawnPoint in roundCoinSpawns)
        {
                newCoin = GameObject.Instantiate(coinPrefab, coinSpawnPoint.transform.position, coinSpawnPoint.transform.rotation);
                newCoin.GetComponent<DissapearOnBallCollide>().spanwNewItemCollected = true;
                NetworkServer.Spawn(newCoin);
        }
    }

    private void MovePlayersToSpawnPoints()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        var respawns = GameObject.FindGameObjectsWithTag("Respawn");
        for (int i = 0; i < players.Length; i++)
        {
            players[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
            players[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            players[i].transform.position = respawns[i].transform.position;
        }
    }

    [ClientRpc]
    private void CameraFollowsPlayer(bool state)
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            if (player.GetComponent<Player>().isLocalPlayer)
                Camera.main.GetComponent<SmoothCamera2D>().target = player.transform;
        }
    }


    public void StartNewRound()
    {
        Debug.Log("Called Start new round");
        CmdStartNewRound(defaultRoundTime);
    }

    private void SetRoundTimeTextToDefault()
    {
        Debug.Log("setting round time text to default: " + defaultRoundTime + " old value was: " + RoundTimeTextField.text);
        if (isClient)
            CmdSetRoundTimeTextToDefault();
        else
            RpcSetRoundTimeTextToDefault();
    }

    [Command(requiresAuthority = false)] private void CmdSetRoundTimeTextToDefault() => RoundTimeTextField.text = defaultRoundTime.ToString();
    [ClientRpc] private void RpcSetRoundTimeTextToDefault() => RoundTimeTextField.text = defaultRoundTime.ToString();


    [Command(requiresAuthority = false)]
    private void CmdStartNewRound(int roundTimeFromClient)
    {
        if (!isServer) return;
        Debug.Log("Called CmdStartNewRound(" + roundTimeFromClient + ")");
        if (startingNewRound)
        {
            Debug.Log("Round start already in progress, aborting additional attempt to start round");
            return;
        }

        startingNewRound = true;

        if (roundActive || levelActive)
        {
            Debug.Log("calling CmdStartNewRound() when roundActive or levelActive is true...");
            return;
        }


        //update server button text and listener
        RoundStartBtn.GetComponentInChildren<TMP_Text>().text = "Abort Round";
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(CmdEndRound);

        //update client button text and listener
        RpcUpdateRoundStartBtnText("Abort Round");
        RpcSetupButtonForAbortRound();

        StartCoroutine(RoundHandler(roundTimeFromClient));
    }
    [ClientRpc]
    private void RpcSetupButtonForAbortRound()
    {        
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(ClientEndRound);
    }

    [ClientRpc] private void RpcUpdateRoundStartBtnText(string newText) => RoundStartBtn.GetComponentInChildren<TMP_Text>().text = newText;
    [ClientRpc] private void RpcUpdateRoundTimeText(string newText) => RoundTimeTextField.text = newText;
    [ClientRpc] private void RpcUpdateCountdownText(string newText) => CenterCountdownText.text = newText;
    [ClientRpc] private void RpcEnableCenterCountdownTimer(bool enableBool) => centerCountdownTextGameObject.SetActive(enableBool);

    private IEnumerator RoundHandler(int newRoundTime) //only called from server CmdStartNewRound
    {
        Debug.Log("Called RoundHandler(" + newRoundTime + ")");
        int currentRoundTime = newRoundTime;
        int currentCountdownTime = defaultCountdownTime;
        centerCountdownTextGameObject.SetActive(true);
        RpcEnableCenterCountdownTimer(true);
        for (int i = currentCountdownTime; i >= 0; i--)
        {
            SoundManager.PlaySound("CountdownSound", 0.6f, 1, false);
            CenterCountdownText.text = i.ToString();
            RpcUpdateCountdownText(i.ToString());
            yield return new WaitForSeconds(1);
            Debug.Log("waited 1 second in countdown...");
        }
        //disable center countdown timer on server and clients
        centerCountdownTextGameObject.SetActive(false); 
        RpcEnableCenterCountdownTimer(false);

        //start round music on server and clients
        Debug.Log("GameManager Calling PlayMusic for round");
        SoundManager.ServerPlayMusic("RoundMusic");


        roundActive = true;
        startingNewRound = false;

        ResetPlayerScores();

        for (int i = currentRoundTime; i >= 0; i--)
        {
            if (!roundActive) continue;
            Debug.Log("Current Round Time Remaining: " + i);
            RoundTimeTextField.text = i.ToString();
            RpcUpdateRoundTimeText(i.ToString());
            yield return new WaitForSeconds(1);
        }

        CmdEndRound();

    }

    [Server] 
    private void ResetPlayerScores()
    {
        Debug.Log("Called [Server] ResetPlayerScores()");
        for (int i = 0; i < playerScores.Count; i++)
        {
            playerScores[i] = 0;
        }
        UpdatePlayerScores(); //refreshes player score ui on server & clients
    }

    [Command(requiresAuthority = false)]
    private void CmdEndRound()
    {
        Debug.Log("Called [Server] EndRound()");
        if (!roundActive)
        {
            Debug.Log("Calling EndRound() but roundActive is false...");
            return;
        }

        SoundManager.StopMusic();

        roundActive = false;
        RoundStartBtn.GetComponentInChildren<TMP_Text>().text = "Start Round";
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(StartNewRound);
        SetRoundTimeTextToDefault();
        RpcEndRound();
    }

    [Client]
    private void ClientEndRound()
    {
        CmdEndRound();
    }

    [ClientRpc]
    private void RpcEndRound()
    {
        RoundStartBtn.GetComponentInChildren<TMP_Text>().text = "Start Round";
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(StartNewRound);
        RoundTimeTextField.text = defaultRoundTime.ToString();
    }



    public static void AddPointForOwningPlayer()
    {
        _instance._AddPointForOwningPlayer();
    }

    private void _AddPointForOwningPlayer()
    {
        if (playerNumberOwningBall > -1 && (roundActive || levelActive))
        {
            playerScores[playerNumberOwningBall] += 1;
            UpdatePlayerScores();
            if (levelActive)
                levelManager.CoinCollected();
        }

    }
    [Server]
    private void UpdatePlayerScores()
    {
        string playerScoreString = "";
        for (int i = 0; i < NetworkManager.singleton.maxConnections; i++)
        {
            playerScoreString += "Player " + (i+1).ToString() + ": " + playerScores[i]+"   ";
        }

        playerScoreString.TrimEnd(' ');

        PlayerScoreOutput.text = playerScoreString;
        UpdateClientScoreText(playerScoreString);
    }
    [ClientRpc]
    private void UpdateClientScoreText(string playerScoreString)
    {
        PlayerScoreOutput.text = playerScoreString;
    }


        //if I switch to key frame updates vs a flow of updates, I might get away with multiple balls, but it might break sync
        //If players each have their own ball and are trapped in their own area on a level, I could switch it to local physics
    /* spawning multiple spheres wasn't a great idea, but fun to play with. Commenting out code for now
public GameObject spherePrefab;
float sphereSpawnCooldown = 1f;
bool sphereSpawnCooldownActive = false;
int maxSpheres = 20;
int sphereCount = 1;
////to call this func from sphere: GameManager.Instance.NewSphere(gameObject.transform.position, Quaternion.identity);
public void NewSphere(Vector3 location, Quaternion rotation)
{
    if (!sphereSpawnCooldownActive & maxSpheres > sphereCount)
    {
        sphereSpawnCooldownActive = true;
        StartCoroutine(sphereSpawnCooldownTimer(sphereSpawnCooldown));
        Instantiate(spherePrefab, location, rotation);
    }
}

IEnumerator sphereSpawnCooldownTimer(float cooldownTimeSeconds)
{
    yield return new WaitForSeconds(cooldownTimeSeconds);
    sphereSpawnCooldownActive = false;
}
*/
}
