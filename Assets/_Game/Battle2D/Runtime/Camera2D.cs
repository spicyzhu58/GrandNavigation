using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Battle2D
{
    public sealed class Camera2D : MonoBehaviour
    {
        public Transform target;
        public float minimumZoom=55,maximumZoom=220,zoomSensitivity=12,smoothTime=.15f;
        Camera view;float zoom,zoomVelocity;Vector3 velocity;
        void Awake(){view=GetComponent<Camera>();zoom=view.orthographicSize;}
        void LateUpdate()
        {
            if(!target)return;
            if(Mouse.current!=null)zoom=Mathf.Clamp(zoom-Mouse.current.scroll.ReadValue().y*zoomSensitivity,minimumZoom,maximumZoom);
            view.orthographicSize=Mathf.SmoothDamp(view.orthographicSize,zoom,ref zoomVelocity,smoothTime);
            transform.position=Vector3.SmoothDamp(transform.position,new Vector3(target.position.x,target.position.y,-10),ref velocity,smoothTime);
        }
    }
}
