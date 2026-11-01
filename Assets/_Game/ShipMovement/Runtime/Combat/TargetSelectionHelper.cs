using System.Collections.Generic;
using UnityEngine;
namespace GrandNavigation.Gameplay
{
    // Called only on selection input. No target tracking or aim-point writes.
    public static class TargetSelectionHelper
    {
        public static bool Eligible(Ship target,Ship player,float range,bool enemiesOnly)
        {
            return target&&player&&target!=player&&target.isActiveAndEnabled&&target.IsAlive
                &&(!enemiesOnly||target.team!=player.team)
                &&Vector3.ProjectOnPlane(target.WorldPosition-player.WorldPosition,Vector3.up).sqrMagnitude<=range*range;
        }
        static bool LayerAllowed(Collider c,int mask)=>(mask&(1<<c.gameObject.layer))!=0;
        static bool Own(Collider c,Ship player)=>c.transform==player.transform||c.transform.IsChildOf(player.transform);
        public static Ship Select(Camera camera,Vector2 pointer,Ship player,float range,float rayDistance,float radius,
            bool assist,bool occlusion,bool enemiesOnly,int targetMask,int obstructionMask)
        {
            if(!camera||!player||!player.IsAlive||!camera.pixelRect.Contains(pointer))return null;
            var ray=camera.ScreenPointToRay(pointer);
            // Camera offset must not shorten the player-centred horizontal lock range.
            float length=Mathf.Max(rayDistance,range+Vector3.Distance(camera.transform.position,player.WorldPosition)+100);
            var hits=Physics.RaycastAll(ray,length,targetMask|obstructionMask,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(Own(hit.collider,player))continue;
                var ship=hit.collider.GetComponentInParent<Ship>();
                if(LayerAllowed(hit.collider,targetMask)&&Eligible(ship,player,range,enemiesOnly))return ship;
                if(occlusion&&LayerAllowed(hit.collider,obstructionMask))break;
            }
            if(!assist)return null;
            Ship best=null;float bestDistance=float.PositiveInfinity,bestCentre=float.PositiveInfinity,bestRange=float.PositiveInfinity;
            foreach(var ship in Object.FindObjectsByType<Ship>(FindObjectsSortMode.InstanceID))
            {
                if(!Eligible(ship,player,range,enemiesOnly))continue;
                float distance=float.PositiveInfinity;
                foreach(var collider in ship.GetComponentsInChildren<Collider>())
                {
                    if(!collider.enabled||collider.isTrigger||!LayerAllowed(collider,targetMask))continue;
                    float d=ScreenDistance(camera,pointer,collider.bounds);
                    if(d>radius||d>=distance)continue;
                    if(occlusion&&!Visible(camera.transform.position,collider,ship,player,obstructionMask))continue;
                    distance=d;
                }
                if(distance>radius)continue;
                Vector3 screen=camera.WorldToScreenPoint(ship.WorldPosition);
                float centre=((Vector2)screen-pointer).sqrMagnitude;
                float horizontal=Vector3.ProjectOnPlane(ship.WorldPosition-player.WorldPosition,Vector3.up).sqrMagnitude;
                if(distance<bestDistance-.001f||(Mathf.Abs(distance-bestDistance)<=.001f
                    &&(centre<bestCentre-.001f||(Mathf.Abs(centre-bestCentre)<=.001f&&horizontal<bestRange))))
                {best=ship;bestDistance=distance;bestCentre=centre;bestRange=horizontal;}
            }
            return best;
        }
        public static float ScreenDistance(Camera camera,Vector2 pointer,Bounds bounds)
        {
            var corners=new Vector3[8];var points=new List<Vector2>(20);
            for(int i=0;i<8;i++)
            {
                corners[i]=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 p=camera.WorldToScreenPoint(corners[i]);
                if(p.z>=camera.nearClipPlane&&p.z<=camera.farClipPlane)points.Add(p);
            }
            // Clip box edges at the near plane instead of projecting behind-camera vertices.
            for(int i=0;i<8;i++)for(int axis=1;axis<=4;axis*=2)
            {
                int j=i^axis;if(j<i)continue;
                float a=camera.WorldToScreenPoint(corners[i]).z,b=camera.WorldToScreenPoint(corners[j]).z,n=camera.nearClipPlane;
                if((a<n&&b>=n)||(b<n&&a>=n))
                    points.Add(camera.WorldToScreenPoint(Vector3.Lerp(corners[i],corners[j],(n-a)/(b-a))));
            }
            if(points.Count<3)return float.PositiveInfinity;
            points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            var hull=new List<Vector2>();
            foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-1]-hull[hull.Count-2],p-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;
            for(int i=points.Count-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-1]-hull[hull.Count-2],p-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            if(hull.Count<4)return float.PositiveInfinity;
            hull.RemoveAt(hull.Count-1);
            bool inside=true;float distance=float.PositiveInfinity;
            for(int i=0;i<hull.Count;i++)
            {
                Vector2 a=hull[i],b=hull[(i+1)%hull.Count],edge=b-a;
                if(Cross(edge,pointer-a)<0)inside=false;
                float t=edge.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(pointer-a,edge)/edge.sqrMagnitude):0;
                distance=Mathf.Min(distance,Vector2.Distance(pointer,a+edge*t));
            }
            // The projected hull must overlap the current viewport.
            float minX=float.PositiveInfinity,minY=minX,maxX=float.NegativeInfinity,maxY=maxX;
            foreach(var p in hull){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
            if(!camera.pixelRect.Overlaps(Rect.MinMaxRect(minX,minY,maxX,maxY)))return float.PositiveInfinity;
            return inside?0:distance;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static bool Visible(Vector3 origin,Collider collider,Ship ship,Ship player,int mask)
        {
            Bounds b=collider.bounds;
            // Centre plus face/edge/corner samples; closest-point keeps samples on real geometry.
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
            {
                Vector3 point=collider.ClosestPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)*.9f));
                Vector3 delta=point-origin;float length=delta.magnitude;if(length<.001f)return true;
                bool blocked=false;
                foreach(var hit in Physics.RaycastAll(origin,delta/length,length,mask,QueryTriggerInteraction.Ignore))
                {
                    if(Own(hit.collider,player)||hit.collider.GetComponentInParent<Ship>()==ship)continue;
                    blocked=true;break;
                }
                if(!blocked)return true;
            }
            return false;
        }
    }
}