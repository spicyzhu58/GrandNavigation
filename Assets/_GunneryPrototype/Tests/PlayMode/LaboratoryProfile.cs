using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    // Existing V0.1/V0.2 regression tests intentionally use exact laboratory observations.
    // CombatCalibrationTests separately exercise the actual saved, delayed optical profile.
    static class LaboratoryProfile
    {
        public static void Apply(PrototypeSession s)
        {
            s.prediction.useOpticalObservations=false; s.prediction.observationLatency=0;
            s.prediction.fireControlUpdateInterval=2.5f;
            s.prediction.baseLongitudinalRadius=8; s.prediction.baseLateralRadius=5;
            s.prediction.minRadius=3; s.prediction.maxRadius=18;
            s.target.spawnPosition=new Vector3(0,0,160);
            s.target.straightDuration=5; s.target.turnDuration=1.5f; s.target.turnSequence=new[]{-15f,20f};
            if(s.targets!=null) foreach(var t in s.targets) if(t!=s.target) t.gameObject.SetActive(false);
            s.prediction.ClearSamples();
        }
    }
}
