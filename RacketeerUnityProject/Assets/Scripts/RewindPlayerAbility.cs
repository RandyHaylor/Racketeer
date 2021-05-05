
public class RewindPlayerAbility : PlayerAbility
{
    public string AbilitySoundName;
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || GameManager.Instance.syncVar_RewindingActive) return false;
        activatingPlayerAbility = true;
        if (TimeController.Instance.IsRewinding) return false;
        
        TimeController.Instance.RewindTime(transform.parent.gameObject);

        SoundManager.PlaySound(AbilitySoundName);
        return true;
    }
}
