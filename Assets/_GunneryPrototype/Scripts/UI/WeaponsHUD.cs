using UnityEngine;
namespace GrandNavigation.Gunnery
{
    public class WeaponsHUD : MonoBehaviour
    {
        public WeaponManager weapons;
        public PrototypeSession session;
        GUIStyle style,title;
        void OnGUI()
        {
            if(style==null)
            {
                style=new GUIStyle(GUI.skin.label) { fontSize=16,wordWrap=true }; style.normal.textColor=Color.white;
                title=new GUIStyle(style) { fontSize=19,fontStyle=FontStyle.Bold };
            }
            float x=Screen.width-320;
            GUI.Box(new Rect(x,12,308,245),"");
            GUI.Label(new Rect(x+12,22,286,28),"MAIN BATTERY | 3 x 2",title);
            for(int i=0;i<weapons.turrets.Length;i++)
            {
                var turret=weapons.turrets[i]; GUI.color=turret.CanFire?Color.green:Color.white;
                GUI.Label(new Rect(x+12,57+i*29,280,27),"Turret "+(i+1)+"  "+turret.State,style);
            }
            GUI.color=Color.white;
            GUI.Label(new Rect(x+12,146,280,48),weapons.Status,style);
            var hp=session.target.GetComponent<Health>();
            GUI.Label(new Rect(x+12,194,280,24),"Target HP: "+hp.CurrentHP.ToString("F0")+" / "+hp.maxHP.ToString("F0"),style);
            GUI.Label(new Rect(x+12,221,280,28),"Hits "+ProjectileController.HitCount+"  |  Splashes "+ProjectileController.SplashCount,style);
            if(!hp.IsAlive) GUI.Label(new Rect(Screen.width/2-170,80,360,50),"TARGET SUNK - press R to reset",title);
        }
    }
}