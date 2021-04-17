using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetVSyncAndFrameRate : MonoBehaviour
{
    public int SetVSyncCount;
    public int SetTargetFrameRate;
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = SetVSyncCount;
        Application.targetFrameRate = SetTargetFrameRate;
    }

}
