
using Mirror;
using UnityEngine;

public class RewindPlayerAbility : PlayerAbility
{
    public GameObject RewindParticleEffectOnPlayer;
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || TimeController.Instance.IsRewinding) return false;
        activatingPlayerAbility = true;
        
        
        TimeController.Instance.RewindTime(transform.parent.gameObject.GetComponent<NetworkBehaviour>().netId);
        if (transform.parent.gameObject.GetComponent<NetworkBehaviour>().isServer)
        {
            GameObject particleEffect = GameObject.Instantiate(RewindParticleEffectOnPlayer, transform.parent.gameObject.transform.position, RewindParticleEffectOnPlayer.transform.rotation);
            NetworkServer.Spawn(particleEffect);
        }
        return true;
    }
}
