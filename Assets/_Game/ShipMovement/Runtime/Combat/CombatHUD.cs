using UnityEngine;
namespace GrandNavigation.Gameplay
{
    public sealed class CombatHUD : MonoBehaviour
    {
        public PlayerShip player;public CombatFireControl fireControl;public CombatAimController aim;public Camera view;
        void OnGUI()
        {
            if(!player)return;GUI.color=Color.white;GUI.Box(new Rect(12,12,440,235),"");
            var w=player.Weapon;string reload="";for(int i=0;i<w.turrets.Length;i++){var t=w.turrets[i];reload+=$"T{i+1}: {(t.ReloadRemaining>0?t.ReloadRemaining.ToString("F1")+"s":t.Aligned?"READY / ON TARGET":"READY / TRAVERSING")}  ";}
            GUI.Label(new Rect(24,24,420,220),$"PLAYER COMBAT V0.1    HP {player.Health.CurrentHealth}/{player.Health.maximumHealth}\nSpeed {player.Movement.ActualSpeed:F1}  Target {player.Movement.TargetSpeed:F1} m/s\nRudder {player.Movement.RudderAngle:F0} deg\n"+
                (fireControl.Target?$"LOCK: {fireControl.Target.name}  HP {fireControl.Target.Health.CurrentHealth}/{fireControl.Target.Health.maximumHealth}\n":"NO TARGET\n")+
                (fireControl.Valid?$"Observation {fireControl.SampleAge:F2}s / {fireControl.observationInterval:F1}s\n":"NO VALID PREDICTION / OUT OF RANGE\n")+reload+"\n"+w.Status+"\nWASD drive | RMB lock at centre reticle | Hold Shift aim\nMouse adjust aim / orbit | Wheel zoom | LMB salvo | Esc unlock\nR reset orbit | Yellow: prediction area | Cyan: YOUR impact point");
            if(!aim.IsAiming){GUI.color=Color.white;GUI.Label(new Rect(Screen.width/2-5,Screen.height/2-10,20,20),"+");}
            DrawAim(aim.PlayerAimPoint,Color.cyan);
            if(fireControl.Target)
            {
                var p=view.WorldToScreenPoint(fireControl.Target.WorldPosition+Vector3.up*4);
                if(p.z>0){GUI.color=Color.red;GUI.Box(new Rect(p.x-35,Screen.height-p.y-10,70,20),fireControl.Target.Health.CurrentHealth.ToString());}
            }
            GUI.color=Color.white;
        }
        void DrawAim(Vector3 point,Color color)
        {
            var p=view.WorldToScreenPoint(point);if(p.z<=0||p.x<0||p.x>Screen.width||p.y<0||p.y>Screen.height)return;
            GUI.color=color;float x=p.x,y=Screen.height-p.y;
            GUI.DrawTexture(new Rect(x-9,y-1,18,2),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x-1,y-9,2,18),Texture2D.whiteTexture);
        }
    }
}
