using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class PlayerAbility : MonoBehaviour
{
    public string abilitySoundName;
    [Range(0, 1)]
    public float abilitySoundVolume = 1;

    [HideInInspector]
    public bool activatingPlayerAbility { get; set; }

    private void Awake()
    {
        activatingPlayerAbility = false;
    }

    public abstract bool ActivatePlayerAbility();

    public void PlayAbilitySoundLocalThenEveryoneElse()
    {
        SoundManager.PlaySound(abilitySoundName, transform.position, SoundManager.UsersToPlayFor.SelfLocallyThenEveryone);
    }   

    public virtual void RemovePlayerAbility()
    {
        Destroy(gameObject);
    }
}
