using UnityEngine;
namespace GrandNavigation.MainMenuV2
{
    public sealed class ShipIdleMotion : MonoBehaviour
    {
        Vector3 origin;Quaternion rotation;
        void Awake(){origin=transform.localPosition;rotation=transform.localRotation;}
        void Update()
        {
            float t=Time.time;
            transform.localPosition=origin+Vector3.up*(Mathf.Sin(t*.65f)*.06f);
            transform.localRotation=rotation*Quaternion.Euler(Mathf.Sin(t*.4f)*.15f,0,Mathf.Sin(t*.53f)*.3f);
        }
    }
}
