
using Mirror;

public class RewindPlayerAbility : PlayerAbility
{
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || TimeController.Instance.IsRewinding) return false;
        activatingPlayerAbility = true;
        
        TimeController.Instance.RewindTime(transform.parent.gameObject.GetComponent<NetworkBehaviour>().netId);
        
        return true;
    }
}
