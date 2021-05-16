using Mirror;
using UnityEngine;

public class SpeedPlayerAbility : PlayerAbility
{
    public GameObject SpeedUpParticleEffectOnPlayer;
    public float SpeedUpDuration = 1;
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || TimeController.Instance.IsRewinding) return false;

        activatingPlayerAbility = true;
        //increase player speed
        transform.parent.gameObject.GetComponent<Player>().GrantBoostForSeconds(SpeedUpDuration);
        if (transform.parent.gameObject.GetComponent<NetworkBehaviour>().isServer)
        {
            GameObject particleEffect = GameObject.Instantiate(SpeedUpParticleEffectOnPlayer, transform.parent.gameObject.transform.position, SpeedUpParticleEffectOnPlayer.transform.rotation, transform.parent);
            NetworkServer.Spawn(particleEffect);
        }
        return true;
    }
}