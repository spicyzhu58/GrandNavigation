using UnityEngine;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay.Levels
{
    public sealed class LevelDebug : MonoBehaviour
    {
        public PlayerShip player;
        public CombatFireControl fire;
        public CombatAimController aim;
        public CombatCamera cameraRig;
        public Transform playerSpawn,targetsRoot;
        public GameObject targetPrefab;
        public Transform[] targetSpawns;
        public Ship[] targets;
        public int targetHealth=1000;
        public bool showHUD=true;
        public static void ConfigureTarget(Ship target,int hp)
        {
            target.Health.maximumHealth=hp;target.Health.Restore();
            target.Movement.SetTargetSpeed(0);target.Movement.SetSteeringInput(0);target.Movement.enabled=false;
            var motion=target.GetComponent<TestTargetMotion>();if(motion)motion.enabled=false;
            target.Weapon.enabled=false;
            var body=target.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeAll;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        }
        public void ResetPlayer()
        {
            fire.Lock(null);aim.SetAiming(false);cameraRig.SetAiming(false);
            player.Movement.SetTargetSpeed(0);player.Movement.SetSteeringInput(0);
            var body=player.GetComponent<Rigidbody>();body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            body.position=playerSpawn.position;body.rotation=playerSpawn.rotation;
            aim.SetPoint(playerSpawn.position+playerSpawn.forward*400);player.Health.Restore();
            cameraRig.orbit.RequestReset();
        }
        public void ResetTargets()
        {
            fire.Lock(null);
            for(int i=0;i<targets.Length;i++)
            {
                if(targets[i]){targets[i].gameObject.SetActive(false);Destroy(targets[i].gameObject);}
                var go=Instantiate(targetPrefab,targetSpawns[i].position+Vector3.up,targetSpawns[i].rotation,targetsRoot);
                go.name="Target_0"+(i+1);targets[i]=go.GetComponent<Ship>();ConfigureTarget(targets[i],targetHealth);
            }
        }
        void Start(){foreach(var target in targets)if(target)ConfigureTarget(target,targetHealth);}
        void Update()
        {
            var key=Keyboard.current;if(key==null)return;
            if(key.f5Key.wasPressedThisFrame)ResetPlayer();
            if(key.f6Key.wasPressedThisFrame)ResetTargets();
            if(key.f7Key.wasPressedThisFrame)showHUD=!showHUD;
        }
        void OnGUI()
        {
            if(!showHUD||!player)return;
            Vector3 p=player.WorldPosition;
            string distance=fire.Target?Vector3.ProjectOnPlane(fire.Target.WorldPosition-p,Vector3.up).magnitude.ToString("F0")+" UU":"--";
            GUI.Box(new Rect(12,Screen.height-105,500,93),"LEVEL GREYBOX  |  2560 x 2560  |  Core +/-600");
            GUI.Label(new Rect(24,Screen.height-82,475,65),
                $"Speed {player.Movement.ActualSpeed:F2} UU/s  Heading {player.transform.eulerAngles.y:F0} deg\nPosition ({p.x:F1}, {p.y:F1}, {p.z:F1})  Target {distance}\nF5: reset player   F6: reset targets   F7: debug HUD");
        }
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(Vector3.zero,new Vector3(1200,1,1200));}
    }
}