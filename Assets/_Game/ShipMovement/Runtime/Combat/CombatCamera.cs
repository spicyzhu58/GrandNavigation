using UnityEngine;
namespace GrandNavigation.Gameplay
{
    [DefaultExecutionOrder(50)]
    public sealed class CombatCamera : MonoBehaviour
    {
        public Camera view;public ShipFollowCamera orbit;public CombatAimController aim;public CombatFireControl fireControl;
        [Range(10,70)] public float aimingFov=32;
        [Min(.01f)] public float smoothTime=.18f;
        [Min(10)] public float aimingDistance=65;
        [Min(3)] public float aimingHeight=24;
        [Min(0)] public float framingMargin=12;
        bool aiming,returning;float normalFov,returnAge,returnFov;Vector3 velocity,returnPosition;Quaternion returnRotation;
        void Awake()=>normalFov=view.fieldOfView;
        public void SetAiming(bool value)
        {
            if(aiming==value)return;aiming=value;orbit.SetControlActive(!value);velocity=Vector3.zero;
            if(!value){returning=true;returnAge=0;returnPosition=view.transform.position;returnRotation=view.transform.rotation;returnFov=view.fieldOfView;}
            else {returning=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
        void LateUpdate()
        {
            if(!aiming)
            {
                if(returning){returnAge+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(returnAge/Mathf.Max(.01f,smoothTime*4)));view.transform.position=Vector3.Lerp(returnPosition,view.transform.position,t);view.transform.rotation=Quaternion.Slerp(returnRotation,view.transform.rotation,t);view.fieldOfView=Mathf.Lerp(returnFov,normalFov,t);if(t>=1)returning=false;}
                return;
            }
            var bounds=new Bounds(aim.PlayerAimPoint,Vector3.zero);
            if(fireControl.Target)bounds.Encapsulate(fireControl.Target.WorldPosition);
            if(fireControl.Valid)bounds.Encapsulate(fireControl.PredictedIntercept);
            Vector3 centre=bounds.center;
            float fitDistance=(bounds.extents.magnitude+framingMargin)/Mathf.Sin(aimingFov*.5f*Mathf.Deg2Rad);
            float distance=Mathf.Max(aimingDistance,fitDistance);
            Vector3 forward=Vector3.ProjectOnPlane(centre-fireControl.player.WorldPosition,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            Vector3 desired=centre-forward*distance+Vector3.up*(aimingHeight*distance/Mathf.Max(1,aimingDistance));
            view.transform.position=Vector3.SmoothDamp(view.transform.position,desired,ref velocity,smoothTime);
            view.transform.position=new Vector3(view.transform.position.x,Mathf.Max(2,view.transform.position.y),view.transform.position.z);
            var rotation=Quaternion.LookRotation(centre-view.transform.position,Vector3.up);
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime/smoothTime));
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,aimingFov,1-Mathf.Exp(-Time.deltaTime/smoothTime));
        }
        void OnDisable(){if(orbit)orbit.SetControlActive(true);if(view)view.fieldOfView=normalFov;}
    }
}
