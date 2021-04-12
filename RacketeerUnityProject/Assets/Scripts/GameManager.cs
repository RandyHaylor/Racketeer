using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public int playerNumberOwningBall = -1;

    public List<int> playerScores;

    public TMP_Text PlayerScoreOutput;

    private static GameManager _instance;

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

    void Start()
    {
        playerScores = new List<int>();
        for (int i = 0; i < NetworkManager.singleton.maxConnections; i++)
        {
            playerScores.Add(0);
        }
        //QualitySettings.vSyncCount = 0; // VSync must be disabled.
        //Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
            #endif
            Application.Quit();
        }
    }

    public static void AddPointForOwningPlayer()
    {
        _instance._AddPointForOwningPlayer();
    }

    private void _AddPointForOwningPlayer()
    {
        if (playerNumberOwningBall < 0) return;
        playerScores[playerNumberOwningBall] += 1;
        UpdatePlayerScores();
    }

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
}
