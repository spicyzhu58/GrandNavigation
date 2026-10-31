using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
namespace GrandNavigation.Gameplay
{
    public sealed class ShipFollowCamera : MonoBehaviour
    {
        [Header("Follow target")]
        [FormerlySerializedAs("target"),SerializeField,Tooltip("Physics root, never the decorative model.")] Transform followTarget;
        [SerializeField,Tooltip("Independent scene observation pivot.")] Transform cameraPivot;
        [SerializeField,Range(0,15),Tooltip("Pivot height above the ship root, in metres.")] float cameraPivotHeight=3;
        [Header("Mouse orbit")]
        [SerializeField,Range(.01f,2),Tooltip("Horizontal degrees per mouse pixel.")] float mouseSensitivityX=.15f;
        [SerializeField,Range(.01f,2),Tooltip("Vertical degrees per mouse pixel.")] float mouseSensitivityY=.12f;
        [SerializeField,Tooltip("Reverse vertical mouse input.")] bool invertY;
        [SerializeField,Range(-180,180),Tooltip("Reset yaw relative to the ship heading; zero is behind the stern.")] float defaultYaw;
        [SerializeField,Range(0,89),Tooltip("Default downward viewing angle.")] float defaultPitch=30;
        [SerializeField,Range(0,89),Tooltip("Lowest orbit pitch.")] float minPitch=10;
        [SerializeField,Range(0,89),Tooltip("Highest orbit pitch.")] float maxPitch=75;
        [Header("Zoom")]
        [SerializeField,Range(1,100),Tooltip("Reset orbit distance in metres.")] float defaultDistance=35;
        [SerializeField,Range(1,100),Tooltip("Closest requested orbit distance. Collision safety may override it.")] float minDistance=15;
        [SerializeField,Range(1,150),Tooltip("Farthest requested orbit distance.")] float maxDistance=65;
        [SerializeField,Range(.1f,10f),Tooltip("Metres per normalized scroll unit (one wheel notch with Uniform Across All Platforms input). Positive scroll zooms in.")] float zoomSensitivity=4f;
        [Header("Smoothing")]
        [SerializeField,Range(.01f,2),Tooltip("Yaw and pitch damping time in seconds.")] float rotationSmoothTime=.12f;
        [SerializeField,Range(.01f,2),Tooltip("World pivot follow damping in seconds.")] float followSmoothTime=.2f;
        [SerializeField,Range(.01f,2),Tooltip("Wheel distance damping in seconds.")] float zoomSmoothTime=.2f;
        [Header("Camera safety")]
        [SerializeField,Range(.05f,3),Tooltip("Sphere radius used for camera obstruction checks.")] float cameraCollisionRadius=.45f;
        [SerializeField,Range(0,2),Tooltip("Extra clearance from hull and obstacles.")] float cameraCollisionPadding=.25f;
        [SerializeField,Tooltip("Solid obstacles that can obstruct the camera.")] LayerMask collisionMask=~0;
        [SerializeField,Tooltip("Flat sea reference height.")] float seaLevel;
        [SerializeField,Range(.2f,5),Tooltip("Minimum camera centre clearance above the sea.")] float minimumSeaClearance=1;
        [SerializeField,Tooltip("Lock cursor for free mouse observation during play.")] bool captureCursor=true;
        [SerializeField,Tooltip("Disable when a combat input controller owns Esc.")] bool allowCursorRelease=true;

        public Transform target { get=>followTarget; set { followTarget=value;initialized=false; } }
        // Legacy scene builder compatibility; no second camera system.
        public Vector3 offset => Vector3.up*cameraPivotHeight+Quaternion.Euler(defaultPitch,defaultYaw,0)*Vector3.back*defaultDistance;
        public Vector3 PivotPosition => pivotPosition;
        public float RequestedYaw => requestedYaw;
        public float RequestedPitch => requestedPitch;
        public float RequestedDistance => requestedDistance;
        public float CurrentDistance => Vector3.Distance(transform.position,pivotPosition);
        public bool ControlActive { get; private set; }=true;
        float requestedYaw,requestedPitch,requestedDistance,yaw,pitch,distance,yawVelocity,pitchVelocity,zoomVelocity;
        float collisionDistance,collisionVelocity;
        Vector3 pivotPosition,pivotVelocity; bool initialized,cursorReleased;
        Renderer[] shipRenderers;
        void OnValidate()
        {
            minPitch=Mathf.Clamp(minPitch,0,89);maxPitch=Mathf.Clamp(maxPitch,minPitch,89);
            defaultPitch=Mathf.Clamp(defaultPitch,minPitch,maxPitch);
            minDistance=Mathf.Max(1,minDistance);maxDistance=Mathf.Max(minDistance,maxDistance);
            defaultDistance=Mathf.Clamp(defaultDistance,minDistance,maxDistance);
            mouseSensitivityX=Mathf.Max(.001f,mouseSensitivityX);mouseSensitivityY=Mathf.Max(.001f,mouseSensitivityY);
            zoomSensitivity=Mathf.Max(.001f,zoomSensitivity);
            rotationSmoothTime=Mathf.Max(.01f,rotationSmoothTime);followSmoothTime=Mathf.Max(.01f,followSmoothTime);zoomSmoothTime=Mathf.Max(.01f,zoomSmoothTime);
            cameraPivotHeight=Mathf.Max(0,cameraPivotHeight);cameraCollisionRadius=Mathf.Max(.05f,cameraCollisionRadius);
            cameraCollisionPadding=Mathf.Max(0,cameraCollisionPadding);minimumSeaClearance=Mathf.Max(cameraCollisionRadius,minimumSeaClearance);
        }
        void Start(){Initialize();UpdateCursor();}
        void Initialize()
        {
            if(!followTarget)return;
            shipRenderers=followTarget.GetComponentsInChildren<Renderer>();
            RequestReset();yaw=requestedYaw;pitch=requestedPitch;distance=requestedDistance;
            pivotPosition=followTarget.position+Vector3.up*cameraPivotHeight;
            pivotVelocity=Vector3.zero;yawVelocity=pitchVelocity=zoomVelocity=0;collisionDistance=distance;collisionVelocity=0;initialized=true;
        }
        public void RequestReset()
        {
            if(!followTarget)return;
            requestedYaw=followTarget.eulerAngles.y+defaultYaw;
            requestedPitch=defaultPitch;requestedDistance=defaultDistance;
        }
        public void SetPivot(Transform pivot){cameraPivot=pivot;}
        // Future fire-control view can take camera ownership without touching ship controls.
        public void SetControlActive(bool active){ControlActive=active;UpdateCursor();}
        void UpdateCursor()
        {
            Cursor.lockState=ControlActive && captureCursor && !cursorReleased?CursorLockMode.Locked:CursorLockMode.None;
            Cursor.visible=Cursor.lockState!=CursorLockMode.Locked;
        }
        void Update()
        {
            if(!ControlActive || !followTarget)return;
            if(!initialized)Initialize();
            if(allowCursorRelease && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)cursorReleased=!cursorReleased;
            var mouse=Mouse.current;
            if(mouse!=null && (!captureCursor || !cursorReleased))
            {
                Vector2 delta=mouse.delta.ReadValue();
                requestedYaw=Mathf.Repeat(requestedYaw+delta.x*mouseSensitivityX,360);
                requestedPitch=Mathf.Clamp(requestedPitch-delta.y*mouseSensitivityY*(invertY?-1:1),minPitch,maxPitch);
                requestedDistance=Mathf.Clamp(requestedDistance-mouse.scroll.ReadValue().y*zoomSensitivity,minDistance,maxDistance);
            }
            if(Keyboard.current!=null && Keyboard.current.rKey.wasPressedThisFrame)RequestReset();
            UpdateCursor();
        }
        void LateUpdate()
        {
            if(!ControlActive || !followTarget)return;
            if(!initialized)Initialize();
            requestedPitch=Mathf.Clamp(requestedPitch,minPitch,maxPitch);
            requestedDistance=Mathf.Clamp(requestedDistance,minDistance,maxDistance);
            Vector3 wantedPivot=followTarget.position+Vector3.up*cameraPivotHeight;
            if(cameraPivot)cameraPivot.position=wantedPivot;
            pivotPosition=Vector3.SmoothDamp(pivotPosition,wantedPivot,ref pivotVelocity,followSmoothTime);
            yaw=Mathf.SmoothDampAngle(yaw,requestedYaw,ref yawVelocity,rotationSmoothTime);
            pitch=Mathf.SmoothDamp(pitch,requestedPitch,ref pitchVelocity,rotationSmoothTime);
            distance=Mathf.SmoothDamp(distance,requestedDistance,ref zoomVelocity,zoomSmoothTime);
            Vector3 direction=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
            Vector3 position=ResolveCollision(pivotPosition,direction,distance);
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(pivotPosition-position,Vector3.up));
        }
        Vector3 ResolveCollision(Vector3 origin,Vector3 direction,float requested)
        {
            float allowed=requested;
            foreach(var hit in Physics.SphereCastAll(origin,cameraCollisionRadius,direction,requested,collisionMask,QueryTriggerInteraction.Ignore))
            {
                if(hit.transform.IsChildOf(followTarget))continue;
                allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-cameraCollisionPadding));
            }
            if(allowed<collisionDistance){collisionDistance=allowed;collisionVelocity=0;}
            else collisionDistance=Mathf.SmoothDamp(collisionDistance,allowed,ref collisionVelocity,zoomSmoothTime);
            Vector3 position=origin+direction*collisionDistance;
            // A close obstruction must not compress the camera into the ship's visual superstructure.
            bool found=false;Bounds hull=default;
            if(shipRenderers!=null)foreach(var renderer in shipRenderers)if(renderer)
            {
                if(!found){hull=renderer.bounds;found=true;}else hull.Encapsulate(renderer.bounds);
            }
            if(found)
            {
                hull.Expand((cameraCollisionRadius+cameraCollisionPadding)*2);
                if(hull.Contains(position))position.y=hull.max.y;
            }
            position.y=Mathf.Max(position.y,seaLevel+minimumSeaClearance);
            // Resolve an overlapping start or constrained corner by lifting above the solid object.
            foreach(var collider in Physics.OverlapSphere(position,cameraCollisionRadius,collisionMask,QueryTriggerInteraction.Ignore))
                if(!collider.transform.IsChildOf(followTarget))position.y=Mathf.Max(position.y,collider.bounds.max.y+cameraCollisionRadius+cameraCollisionPadding);
            return position;
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}



