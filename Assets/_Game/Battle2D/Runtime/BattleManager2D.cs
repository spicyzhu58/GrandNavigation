using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.Battle;
namespace GrandNavigation.Battle2D
{
    [DefaultExecutionOrder(10000)]
    public sealed class BattleManager2D : MonoBehaviour
    {
        public ShipActor2D player;
        public ShipActor2D[] enemies;
        public GameObject startPanel,resultPanel;
        public Button startButton,retryButton,quitButton;
        public Text title;
        public BattleState State {get;private set;}=BattleState.Playing;
        public bool Started {get;private set;}
        public int Remaining=>roster.Count-dead.Count;
        public int EndCount {get;private set;}
        readonly Dictionary<HealthSystem,Action> roster=new();readonly HashSet<HealthSystem> dead=new();
        ShipActor2D[] ships;bool loading,ownsPause;
        void Awake()
        {
            ships=FindObjectsByType<ShipActor2D>(FindObjectsSortMode.None);
            foreach(var ship in ships)
            {
                ship.GetComponent<ShipMovement2D>().enabled=false;ship.GetComponent<Weapon2D>().enabled=false;
                var input=ship.GetComponent<PlayerInput2D>();if(input)input.enabled=false;
                var ai=ship.GetComponent<EnemyAI2D>();if(ai)ai.enabled=false;
                ship.GetComponent<HealthSystem>().DamageBlocked=true;
            }
        }
        void Start()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            foreach(var ship in ships)ship.Movement.Body.simulated=false;
            foreach(var enemy in enemies)
            {
                if(!enemy||enemy.team==player.team||roster.ContainsKey(enemy.Health))continue;
                var health=enemy.Health;Action handler=()=>dead.Add(health);roster.Add(health,handler);health.Died+=handler;
            }
            if(roster.Count==0)Debug.LogWarning("Battle2D has no participating enemies; automatic victory disabled.",this);
            startPanel.SetActive(true);resultPanel.SetActive(false);startButton.onClick.AddListener(Begin);retryButton.onClick.AddListener(Retry);
            if(quitButton)quitButton.onClick.AddListener(Quit);
        }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        public void Begin()
        {
            if(Started)return;Started=true;startPanel.SetActive(false);
            foreach(var ship in ships)
            {
                ship.Health.DamageBlocked=false;ship.Movement.Body.simulated=true;ship.Movement.enabled=true;ship.GetComponent<Weapon2D>().enabled=true;
                var input=ship.GetComponent<PlayerInput2D>();if(input)input.enabled=true;
                var ai=ship.GetComponent<EnemyAI2D>();if(ai)ai.enabled=true;
            }
        }
        void LateUpdate()
        {
            if(!Started||State!=BattleState.Playing)return;
            if(!player||!player.Alive)Finish(BattleState.Defeat);
            else if(roster.Count>0&&dead.Count==roster.Count)Finish(BattleState.Victory);
        }
        void Finish(BattleState outcome)
        {
            State=outcome;EndCount++;
            foreach(var ship in ships)if(ship)
            {
                ship.Health.DamageBlocked=true;ship.Movement.Stop();ship.Movement.enabled=false;ship.Movement.Body.simulated=false;ship.GetComponent<Weapon2D>().enabled=false;
                var input=ship.GetComponent<PlayerInput2D>();if(input)input.enabled=false;
                var ai=ship.GetComponent<EnemyAI2D>();if(ai)ai.enabled=false;
            }
            foreach(var shell in FindObjectsByType<Projectile2D>(FindObjectsSortMode.None)){shell.enabled=false;shell.GetComponent<Rigidbody2D>().simulated=false;}
            title.text=State==BattleState.Victory?"VICTORY":"DEFEAT";resultPanel.SetActive(true);Time.timeScale=0;ownsPause=true;
        }
        public void Retry()
        {
            if(State==BattleState.Playing||loading)return;
            loading=true;retryButton.interactable=false;Time.timeScale=1;SceneManager.LoadSceneAsync(gameObject.scene.path);
        }
        void OnGUI()
        {
            if(!Started||State!=BattleState.Playing||!player)return;
            var input=player.GetComponent<PlayerInput2D>();var weapon=player.GetComponent<Weapon2D>();
            GUI.Box(new Rect(10,10,350,125),$"NAVAL BATTLE 2D   HP {player.Health.CurrentHealth}/{player.Health.maximumHealth}\nSpeed {player.Movement.Speed:F1} / Target {player.Movement.TargetSpeed:F1}\nEnemies {Remaining}   Reload {weapon.ReloadRemaining:F1}s\nW/S throttle | A/D rudder | Wheel zoom\nRMB lock | LMB salvo toward mouse | Esc unlock\n{(input.Target?$"{input.Target.name} HP {input.Target.Health.CurrentHealth}":"No target")}");
            if(input.Target&&input.Target.Alive)
            {
                var p=Camera.main.WorldToScreenPoint(input.Target.transform.position);GUI.color=Color.green;GUI.Box(new Rect(p.x-25,Screen.height-p.y-25,50,50),"");GUI.color=Color.white;
            }
        }
        void OnDestroy()
        {
            foreach(var pair in roster)if(pair.Key)pair.Key.Died-=pair.Value;
            if(ownsPause)Time.timeScale=1;
        }
    }
}
