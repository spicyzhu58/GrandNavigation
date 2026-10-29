using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class PrototypeCameraController : MonoBehaviour
    {
        public UnityEngine.Camera view;
        public Transform player;
        public TargetSelectionController selection;
        public ManualAimController aim;
        public float normalZoom=1, aimFov=36;
        public Vector3 aimOffset=new Vector3(0,35,-65);
        [Range(0,.5f)] public float impactFraming=0;
        float shake; Vector3 smoothPosition;
        void Start() { smoothPosition=view.transform.position; }
        public void Zoom(float scroll)
        {
            if(aim.IsAiming) aimFov=Mathf.Clamp(aimFov-scroll*.012f,18,55);
            else normalZoom=Mathf.Clamp(normalZoom-scroll*.001f,.65f,1.5f);
        }
        public void Shake(float strength=.35f) => shake=Mathf.Max(shake,strength);
        void LateUpdate()
        {
            bool aiming=aim.IsAiming && selection.HasTarget;
            Vector3 focus=aiming ? selection.TargetPosition+Vector3.up*1.5f : player.position+Vector3.forward*65;
            if(aiming) focus+=Vector3.ClampMagnitude(aim.PlayerAimPoint-selection.TargetPosition,60)*impactFraming;
            Vector3 wanted=aiming ? focus+aimOffset :
                player.position+new Vector3(65,130,-85)*normalZoom;
            float blend=1-Mathf.Exp(-Time.deltaTime*5);
            smoothPosition=Vector3.Lerp(smoothPosition,wanted,blend);
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime);
            view.transform.position=smoothPosition+Random.insideUnitSphere*shake;
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,Quaternion.LookRotation(focus-smoothPosition),blend);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,aiming?aimFov:55,blend);
        }
    }
}
