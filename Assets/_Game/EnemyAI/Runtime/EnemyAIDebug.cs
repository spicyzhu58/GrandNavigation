using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace GrandNavigation.Gameplay.AI
{
    public sealed class EnemyAIDebug : MonoBehaviour
    {
        [System.Serializable] public sealed class SpawnSlot
        { public GameObject prefab; public EnemyShipAI instance; public Vector3 position; public Vector3 euler; public Transform[] route; }
        public SpawnSlot[] slots;
        public Transform enemiesRoot;
        public bool showHUD=true;
        public void ResetEnemy(int index)
        {
            var slot=slots[index];
            if(slot.instance){slot.instance.gameObject.SetActive(false);Destroy(slot.instance.gameObject);}
            var go=Instantiate(slot.prefab,slot.position,Quaternion.Euler(slot.euler),enemiesRoot);
            slot.instance=go.GetComponent<EnemyShipAI>();slot.instance.waypoints=slot.route;go.name="NPC_"+(index+1)+"_"+slot.prefab.name;
        }
        void Update()
        {
            if(Keyboard.current==null)return;
            if(Keyboard.current.f6Key.wasPressedThisFrame)for(int i=0;i<slots.Length;i++)ResetEnemy(i);
            if(Keyboard.current.f7Key.wasPressedThisFrame)showHUD=!showHUD;
        }
        void OnGUI()
        {
            if(!showHUD)return;
            GUILayout.BeginArea(new Rect(Screen.width-420,10,410,Screen.height-20),GUI.skin.box);
            GUILayout.Label("ENEMY AI | F6 reset NPCs | F7 hide debug");
            if(GUILayout.Button("Reset entire test scene (including player)"))SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            for(int i=0;i<slots.Length;i++)
            {
                var ai=slots[i].instance;
                if(ai && ai.Ship)
                {
                    var s=ai.Ship;var n=ai.Navigation;
                    GUILayout.Label($"{s.name}: {ai.State}  HP {s.Health.CurrentHealth}/{s.Health.maximumHealth}\nTarget {(ai.Target?ai.Target.name:"--")} Visible {ai.Visible} Distance {(ai.Target?EnemyAILogic.Distance(s.WorldPosition,ai.Target.WorldPosition):0):F0}\nSpeed {s.Movement.ActualSpeed:F1} -> {s.Movement.TargetSpeed:F1} Heading {s.transform.eulerAngles.y:F0} -> {Mathf.Atan2(n.DesiredDirection.x,n.DesiredDirection.z)*Mathf.Rad2Deg:F0}\nSide {ai.Broadside} Avoid {n.Avoiding} Stuck {n.StuckTime:F1}s\nState {ai.StateAge:F1}s Memory {ai.MemoryAge:F1}s Ally {(ai.RetreatAlly?ai.RetreatAlly.name:"--")}\n{ai.TransitionReason}\n{n.Status} | {ai.FireStatus}");
                }
                if(GUILayout.Button("Reset NPC "+(i+1)))ResetEnemy(i);
            }
            GUILayout.EndArea();
        }
    }
}
