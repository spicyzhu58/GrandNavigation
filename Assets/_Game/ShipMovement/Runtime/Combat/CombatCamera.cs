using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(50)]
    public sealed class CombatCamera : MonoBehaviour
    {
        public Camera view;public ShipFollowCamera orbit;public CombatAimController aim;public CombatFireControl fireControl;
        [Range(10,70)] public float aimingFov=36;
        [Min(.01f)] public float smoothTime=.18f;
        [Min(10)] public float aimingDistance=65;
        [Min(3)] public float aimingHeight=35;
        [Tooltip("Observation centre above the target hull.")] public float targetFocusHeight=1.5f;
        public Vector3 ViewingDirection {get;private set;}=Vector3.forward;
        [Range(1,10)] public float zoomDegreesPerNotch=2;
        [Range(10,55)] public float minimumAimFov=18,maximumAimFov=55;
        [Min(.01f)] public float aimResponse=5;
        public void Zoom(float scroll){if(aiming)aimingFov=Mathf.Clamp(aimingFov-scroll*zoomDegreesPerNotch,minimumAimFov,maximumAimFov);}
        bool aiming,returning;float normalFov,returnAge,returnFov;Vector3 returnPosition;Quaternion returnRotation;
        void Awake()=>normalFov=view.fieldOfView;
        public void SetAiming(bool value)
        {
            value=value&&fireControl&&fireControl.Target&&fireControl.Target.IsAlive&&aim&&aim.player&&aim.player.IsAlive;
            if(aiming==value)return;aiming=value;orbit.SetControlActive(!value);
            if(!value){returning=true;returnAge=0;returnPosition=view.transform.position;returnRotation=view.transform.rotation;returnFov=view.fieldOfView;}
            else {
                ViewingDirection=Vector3.ProjectOnPlane(fireControl.Target.WorldPosition-aim.player.WorldPosition,Vector3.up).normalized;
                if(ViewingDirection.sqrMagnitude<.01f)ViewingDirection=aim.player.WorldForward;
                aim.SetViewBasis(ViewingDirection);
                returning=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            }
        }
        void LateUpdate()
        {
            if(aiming&&(!fireControl.Target||!fireControl.Target.IsAlive)){aim.SetAiming(false);SetAiming(false);}
            if(!aiming)
            {
                if(returning){returnAge+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(returnAge/Mathf.Max(.01f,smoothTime*4)));view.transform.position=Vector3.Lerp(returnPosition,view.transform.position,t);view.transform.rotation=Quaternion.Slerp(returnRotation,view.transform.rotation,t);view.fieldOfView=Mathf.Lerp(returnFov,normalFov,t);if(t>=1)returning=false;}
                return;
            }
            // Freeze the engagement bearing on entry; track only the target position.
            // The manual impact point never affects the zoom camera's framing or distance.
            Vector3 centre=fireControl.Target.WorldPosition+Vector3.up*targetFocusHeight;
            Vector3 desired=centre+Vector3.up*aimingHeight-ViewingDirection*aimingDistance;
            float blend=1-Mathf.Exp(-Time.deltaTime*aimResponse);
            view.transform.position=Vector3.Lerp(view.transform.position,desired,blend);
            view.transform.position=new Vector3(view.transform.position.x,Mathf.Max(2,view.transform.position.y),view.transform.position.z);
            var rotation=Quaternion.LookRotation(centre-view.transform.position,Vector3.up);
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,rotation,blend);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,aimingFov,blend);
        }
        void OnDisable(){if(orbit)orbit.SetControlActive(true);if(view)view.fieldOfView=normalFov;}
    }
}
