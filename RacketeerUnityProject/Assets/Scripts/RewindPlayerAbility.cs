using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RewindPlayerAbility : PlayerAbility
{
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility) return false;
        activatingPlayerAbility = true;
        if (TimeController.Instance.IsRewinding) return false;
        TimeController.Instance.RewindTime();
        return true;
    }
}
