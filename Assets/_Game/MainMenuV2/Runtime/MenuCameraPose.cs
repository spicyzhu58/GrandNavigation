using UnityEngine;
namespace GrandNavigation.MainMenuV2
{
    public sealed class MenuCameraPose : MonoBehaviour
    {
        public Transform lookTarget;
        [Range(15,90)] public float fieldOfView=38;
        public Quaternion LookRotation => Quaternion.LookRotation(lookTarget.position-transform.position,Vector3.up);
    }
}
