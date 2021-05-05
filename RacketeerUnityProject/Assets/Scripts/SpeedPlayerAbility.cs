public class SpeedPlayerAbility : PlayerAbility
{
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || GameManager.Instance.syncVar_RewindingActive) return false;
        activatingPlayerAbility = true;
        if (TimeController.Instance.IsRewinding) return false;

        //increase player speed

        SoundManager.PlaySound(abilitySoundName, abilitySoundVolume, 1, false);
        
        return true;
    }
}