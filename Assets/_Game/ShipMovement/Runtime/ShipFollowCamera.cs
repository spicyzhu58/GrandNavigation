using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class ShipFollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset=new Vector3(0,20,-37);
        [Min(.01f)] public float positionSmoothTime=.3f;
        [Min(.1f)] public float rotationResponse=5;
        Vector3 velocity;
        void Start(){if(target)transform.position=target.TransformPoint(offset);}
        void LateUpdate()
        {
            if(!target)return;
            transform.position=Vector3.SmoothDamp(transform.position,target.TransformPoint(offset),ref velocity,positionSmoothTime);
            var look=target.position+target.forward*10+Vector3.up*2;
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look-transform.position),1-Mathf.Exp(-rotationResponse*Time.deltaTime));
        }
    }
}
