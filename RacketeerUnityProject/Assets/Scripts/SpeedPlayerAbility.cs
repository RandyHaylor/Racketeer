public class SpeedPlayerAbility : PlayerAbility
{
    public override bool ActivatePlayerAbility()
    {
        if (activatingPlayerAbility || TimeController.Instance.IsRewinding) return false;

        activatingPlayerAbility = true;
        //increase player speed


        return true;
    }
}