using UnityEngine;
namespace GrandNavigation.Gameplay
{
    // Extends the prototype's fixed-speed model. The first positive time root is the low trajectory.
    public static class CombatBallistics
    {
        public static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
        public static Vector3 Position(Vector3 o,Vector3 v,float g,float t)=>o+v*t+Vector3.down*(.5f*g*t*t);
        public static bool Solve(Vector3 origin,Vector3 point,Vector3 platform,float speed,float gravity,out Vector3 relative,out float time)
        {
            relative=Vector3.zero;time=0;
            if(!Finite(origin)||!Finite(point)||!Finite(platform)||!Finite(speed)||!Finite(gravity)||speed<=0||gravity<=0)return false;
            Vector3 d=point-origin;if(d.sqrMagnitude<.0001f)return false;
            double Error(double t){double x=d.x/t-platform.x,y=d.y/t-platform.y+.5*gravity*t,z=d.z/t-platform.z;return x*x+y*y+z*z-speed*speed;}
            double lo=.00001,hi=lo;
            // A bounded search followed by bisection; time is solved, never prescribed.
            for(int i=1;i<=2400;i++)
            {
                hi=i*.05;
                if(Error(hi)<=0)
                {
                    for(int j=0;j<48;j++){double mid=(lo+hi)*.5;if(Error(mid)>0)lo=mid;else hi=mid;}
                    time=(float)((lo+hi)*.5);relative=d/time-platform+Vector3.up*(.5f*gravity*time);
                    return Finite(relative)&&Mathf.Abs(relative.magnitude-speed)<.01f;
                }
                lo=hi;
            }
            return false;
        }
        public static bool Intercept(Vector3 origin,Vector3 target,Vector3 velocity,Vector3 platform,float speed,float gravity,float range,out Vector3 point,out float time,bool horizontalRange=false)
        {
            point=new Vector3(target.x,0,target.z);time=0;
            if(!Finite(velocity)||!Finite(range)||range<=0)return false;
            for(int i=0;i<48;i++)
            {
                if(!Solve(origin,point,platform,speed,gravity,out _,out float next))return false;
                Vector3 p=target+velocity*next;p.y=0;
                if(Vector3.Distance(p,point)<.002f){point=p;time=next;return (horizontalRange?Vector3.ProjectOnPlane(point-origin,Vector3.up).magnitude:Vector3.Distance(origin,point))<=range;}
                point=p;
            }
            return false;
        }
    }
    public static class GunneryMath
    {
        public static bool InArc(float localYaw,float centre,float arc)=>Mathf.Abs(Mathf.DeltaAngle(centre,localYaw))<=Mathf.Clamp(arc,1,359)*.5f+.001f;
        public static Vector2 Gaussian(System.Random random,float sigma)
        {
            // Rejection truncates the radial offset at 3 sigma without piling samples on the edge.
            for(int i=0;i<64;i++)
            {
                float r=Mathf.Sqrt(-2*Mathf.Log(Mathf.Max(.000001f,(float)random.NextDouble())));
                if(r>3)continue;float a=(float)random.NextDouble()*Mathf.PI*2;
                return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*Mathf.Max(0,sigma);
            }
            return Vector2.zero;
        }
    }
}
