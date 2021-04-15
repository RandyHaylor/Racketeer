using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : NetworkBehaviour
{
    //server-located player movement variables for central & live adjustment or future loading from ini file
    public float VelocityBoostMultiplier;
    public float RotationalBoostMultiplier;
    public float playerSpeedLimit = 5f;
    public float speedLimitBoostMultiplier;
    public float angularSpeedLimitBoost = 10f;
    public float angularSpeedLimitNormal = 4f;

    public AudioClip InitialCountdownSound; 
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

    public GameObject CenterCountdownTextGameObject;
    public TMP_Text CenterCountdownText;


    public TMP_InputField RoundTimeInputField;
    public TMP_Text RoundTimeTextField;
    public Button RoundStartBtn;
    bool roundActive = false;

    private static GameManager _instance;



    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.Log("GameMangager instance is null, GameManager object/component is probably missing from the scene...");

            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
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
        CenterCountdownTextGameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
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


    void SetBlendedEulerAngles(Vector3 angles)
    {
        RpcSetBlendedEulerAngles(angles);
        turnStartTime = Time.time;
        _previousRotation = wallsTransform.localRotation;
        _targetRotation = Quaternion.Euler(angles);

    }
    [ClientRpc]
    void RpcSetBlendedEulerAngles(Vector3 angles)
    {
        turnStartTime = Time.time;
        _previousRotation = wallsTransform.localRotation;
        _targetRotation = Quaternion.Euler(angles);
    }
    
    public void StartNewRound()
    {
        CmdStartNewRound(defaultRoundTime);
    }

    void SetRoundTimeTextToDefault()
    {
        Debug.Log("setting round time text to default: " + defaultRoundTime + " old value was: " + RoundTimeTextField.text);
        if (isClient)
            CmdSetRoundTimeTextToDefault();
        else
            RpcSetRoundTimeTextToDefault();
    }

    [Command(requiresAuthority = false)] void CmdSetRoundTimeTextToDefault() => RoundTimeTextField.text = defaultRoundTime.ToString();
    [ClientRpc] void RpcSetRoundTimeTextToDefault() => RoundTimeTextField.text = defaultRoundTime.ToString();


    [Command(requiresAuthority = false)]
    void CmdStartNewRound(int roundTimeFromClient)
    {
        Debug.Log("Called CmdStartnewRound(" + roundTimeFromClient + ")");
        if (roundActive)
        {
            Debug.Log("calling CmdStartNewRound() when roundActive is true...");
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
    void RpcSetupButtonForAbortRound()
    {        
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(ClientEndRound);
    }

    [ClientRpc] void RpcUpdateRoundStartBtnText(string newText) => RoundStartBtn.GetComponentInChildren<TMP_Text>().text = newText;
    [ClientRpc] void RpcUpdateRoundTimeText(string newText) => RoundTimeTextField.text = newText;
    [ClientRpc] void RpcUpdateCountdownText(string newText) => CenterCountdownText.text = newText;
    [ClientRpc] void RpcEnableCenterCountdownTimer(bool enableBool) => CenterCountdownTextGameObject.SetActive(enableBool);

    IEnumerator RoundHandler(int newRoundTime) //only called from server CmdStartNewRound
    {
        Debug.Log("Called RoundHandler(" + newRoundTime + ")");
        int currentRoundTime = newRoundTime;
        int currentCountdownTime = defaultCountdownTime;
        CenterCountdownTextGameObject.SetActive(true);
        RpcEnableCenterCountdownTimer(true);
        for (int i = currentCountdownTime; i >= 0; i--)
        {
            PlayCountdownSound();
            CenterCountdownText.text = i.ToString();
            RpcUpdateCountdownText(i.ToString());
            yield return new WaitForSeconds(1);
            Debug.Log("waited 1 second in countdown...");
        }
        //disable center countdown timer on server and clients
        CenterCountdownTextGameObject.SetActive(false); 
        RpcEnableCenterCountdownTimer(false);

        //start round music on server and clients
        SoundManager.PlayRoundMusic();


        roundActive = true;
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
    private void PlayCountdownSound()
    {
        // SoundManager.PlaySound(InitialCountdownSound);
        CmdPlayCountdownSound();
    }
    [ClientRpc]
    private void CmdPlayCountdownSound()
    {
        SoundManager.PlaySound(InitialCountdownSound);
    }

    [Server] 
    void ResetPlayerScores()
    {
        Debug.Log("Called [Server] ResetPlayerScores()");
        for (int i = 0; i < playerScores.Count; i++)
        {
            playerScores[i] = 0;
        }
        UpdatePlayerScores(); //refreshes player score ui on server & clients
    }

    [Command(requiresAuthority = false)]
    void CmdEndRound()
    {
        Debug.Log("Called [Server] EndRound()");
        if (!roundActive)
        {
            Debug.Log("Calling EndRound() but roundActive is false...");
            return;
        }

        SoundManager.StopRoundMusic();

        roundActive = false;
        RoundStartBtn.GetComponentInChildren<TMP_Text>().text = "Start Round";
        RoundStartBtn.onClick.RemoveAllListeners();
        RoundStartBtn.onClick.AddListener(StartNewRound);
        SetRoundTimeTextToDefault();
        RpcEndRound();
    }

    [Client]
    void ClientEndRound()
    {
        CmdEndRound();
    }

    [ClientRpc]
    void RpcEndRound()
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
        if (playerNumberOwningBall < 0 || !roundActive) return;
        playerScores[playerNumberOwningBall] += 1;
        UpdatePlayerScores();
    }
    [Server]
    void UpdatePlayerScores()
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
    void UpdateClientScoreText(string playerScoreString)
    {
        PlayerScoreOutput.text = playerScoreString;
    }



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
