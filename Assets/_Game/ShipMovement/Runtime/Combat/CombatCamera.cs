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
        [Header("Aim entry visibility")]
        [SerializeField,Range(.1f,1),Tooltip("Keep the existing world aim inside this central fraction of the destination camera frame. Outside points are recentered once on entry.")] float entrySafeViewportFraction=.8f;
        [SerializeField,Min(1),Tooltip("Maximum centre-ray distance to the sea. Invalid / near-horizontal rays fall back to the observed target sea position, not the predicted intercept.")] float maximumEntryRayDistance=5000;
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
                GetAimDestination(out var position,out var rotation);
                if(!InsideEntryFrame(aim.PlayerAimPoint,position,rotation,aimingFov,view.aspect,entrySafeViewportFraction))
                    aim.SetPoint(EntrySeaPoint(position,rotation,fireControl.Target.WorldPosition,maximumEntryRayDistance));
                returning=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            }
        }
        void GetAimDestination(out Vector3 position,out Quaternion rotation)
        {
            Vector3 centre=fireControl.Target.WorldPosition+Vector3.up*targetFocusHeight;
            position=centre+Vector3.up*aimingHeight-ViewingDirection*aimingDistance;
            position.y=Mathf.Max(2,position.y);
            rotation=Quaternion.LookRotation(centre-position,Vector3.up);
        }
        // Evaluate the final pose, not the still-blending observation camera.
        public static bool InsideEntryFrame(Vector3 point,Vector3 position,Quaternion rotation,float fov,float aspect,float safeFraction)
        {
            Vector3 local=Quaternion.Inverse(rotation)*(point-position);
            if(!CombatBallistics.Finite(local)||local.z<=.001f||!float.IsFinite(aspect)||aspect<=0)return false;
            float halfHeight=local.z*Mathf.Tan(Mathf.Clamp(fov,1,179)*.5f*Mathf.Deg2Rad)*Mathf.Clamp(safeFraction,.1f,1);
            return Mathf.Abs(local.y)<=halfHeight && Mathf.Abs(local.x)<=halfHeight*aspect;
        }
        public static Vector3 EntrySeaPoint(Vector3 position,Quaternion rotation,Vector3 fallback,float maximumDistance)
        {
            var ray=new Ray(position,rotation*Vector3.forward);
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance) && float.IsFinite(distance) && distance<=maximumDistance)
            {
                Vector3 point=ray.GetPoint(distance);
                if(CombatBallistics.Finite(point)){point.y=0;return point;}
            }
            return new Vector3(fallback.x,0,fallback.z);
        }
        void OnValidate(){entrySafeViewportFraction=Mathf.Clamp(entrySafeViewportFraction,.1f,1);maximumEntryRayDistance=Mathf.Max(1,maximumEntryRayDistance);}
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
