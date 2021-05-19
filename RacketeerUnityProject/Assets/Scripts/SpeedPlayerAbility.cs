using Mirror;
using UnityEngine;

public class SpeedPlayerAbility : PlayerAbility
{
    public float SpeedUpDuration = 1;
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || TimeController.Instance.IsRewinding) return false;

        activatingPlayerAbility = true;
        //increase player speed
        transform.parent.gameObject.GetComponent<Player>().GrantBoostForSeconds(SpeedUpDuration);
        return true;
    }
}