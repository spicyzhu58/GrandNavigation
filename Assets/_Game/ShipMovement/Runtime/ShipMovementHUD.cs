using UnityEngine;
using UnityEngine.UI;
namespace GrandNavigation.Gameplay
{
    public sealed class ShipMovementHUD : MonoBehaviour
    {
        public ShipMovement movement;
        public Text readout;
        void Update()
        {
            if(!movement || !readout)return;
            readout.text=$"SHIP MOVEMENT V0.1\nSpeed  {movement.ActualSpeed,6:F1} m/s\nTarget {movement.TargetSpeed,6:F1} m/s\nRudder {movement.RudderAngle,6:F1} deg\nHeading {movement.transform.eulerAngles.y:F0} deg\n\nHold W / S: set speed\nRelease: keep target speed\nA / D: steer | release: centre";
            readout.text=readout.text.Replace("\\n","\n");
        }
    }
}
