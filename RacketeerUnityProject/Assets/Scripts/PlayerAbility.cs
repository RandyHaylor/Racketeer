using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class PlayerAbility : MonoBehaviour
{
    public bool activatingPlayerAbility { get; set; }

    private void Awake()
    {
        activatingPlayerAbility = false;
    }

    public abstract bool ActivatePlayerAbility();
    

    public virtual void RemovePlayerAbility()
    {
        Destroy(gameObject);
    }
}
