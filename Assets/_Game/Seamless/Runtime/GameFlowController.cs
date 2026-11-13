using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GrandNavigation.Gameplay;
using GrandNavigation.Gameplay.Battle;
using GrandNavigation.MainMenuV2;
namespace GrandNavigation.Seamless
{
    public enum GameStage { MainMenu, Preparation, Transition, Playing, Result }
    [DefaultExecutionOrder(-10000)]
    public sealed class GameFlowController : MonoBehaviour
    {
        public BattleManager battle;
        public ShipFollowCamera orbit;
        public MenuCameraRig rig;
        public MenuCameraPose battlePose;
        public ShipPreparationController preparation;
        public GameObject menuPanel,preparationPanel;
        public Button startButton,goButton,backButton,exitButton;
        public Text message;
        [Min(.1f),Tooltip("Preparation to stern orbit duration.")] public float goDuration=2.5f;
        public GameStage Stage {get;private set;}
        readonly List<MonoBehaviour> paused=new();
        readonly Dictionary<Rigidbody,bool> bodies=new();
        float battleFov;bool direct;
        static string pendingPath;
        static bool pendingDirect;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLaunch(){pendingPath=null;pendingDirect=false;SceneManager.sceneLoaded-=ClearLaunch;}
        static void ClearLaunch(Scene scene,LoadSceneMode mode){pendingPath=null;pendingDirect=false;SceneManager.sceneLoaded-=ClearLaunch;}
        void Awake()
        {
            Time.timeScale=1;
            direct=pendingPath==gameObject.scene.path&&pendingDirect;
            pendingPath=null;pendingDirect=false;
            battleFov=rig.view.fieldOfView;
            foreach(var c in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if(c.gameObject.scene!=gameObject.scene||!c.enabled)continue;
                string ns=c.GetType().Namespace;
                if(ns!=null&&ns.StartsWith("GrandNavigation.Gameplay")&&!ns.StartsWith("GrandNavigation.Gameplay.Water")&&!(c is ResultUI)) {paused.Add(c);c.enabled=false;}
            }
            startButton.onClick.AddListener(StartPreparation);goButton.onClick.AddListener(Go);backButton.onClick.AddListener(Back);exitButton.onClick.AddListener(Exit);
            battle.Ended+=OnResult;
        }
        void Start()
        {
            // All ShipMovement Awake calls have now initialized their rigidbodies.
            foreach(var ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
            {
                if(ship.gameObject.scene!=gameObject.scene)continue;
                ship.Health.DamageBlocked=true;
                var body=ship.GetComponent<Rigidbody>();if(body){bodies.Add(body,body.isKinematic);body.isKinematic=true;}
            }
            battle.resultUI.ReloadRequested=Reload;
            battle.resultUI.mainMenuButton.interactable=true;
            orbit.GetDefaultView(out var position,out var rotation);
            battlePose.transform.SetPositionAndRotation(position,rotation);battlePose.fieldOfView=battleFov;
            if(direct){rig.Apply(battlePose,battlePose,1);EnterPlaying();}
            else {rig.Initialize();SetStage(GameStage.MainMenu);}
        }
        void SetStage(GameStage stage)
        {
            Stage=stage;menuPanel.SetActive(stage==GameStage.MainMenu);preparationPanel.SetActive(stage==GameStage.Preparation);
            if(stage!=GameStage.Playing){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        public void StartPreparation(){if(Stage==GameStage.MainMenu)StartCoroutine(Move(rig.menuPose,rig.preparationPose,GameStage.Preparation,rig.duration));}
        public void Back(){if(Stage==GameStage.Preparation){preparation.SetActive(false);StartCoroutine(Move(rig.preparationPose,rig.menuPose,GameStage.MainMenu,rig.duration));}}
        public void Go(){if(Stage==GameStage.Preparation)StartCoroutine(Move(rig.preparationPose,battlePose,GameStage.Playing,goDuration));}
        IEnumerator Move(MenuCameraPose from,MenuCameraPose to,GameStage destination,float seconds)
        {
            SetStage(GameStage.Transition);message.text="";
            float old=rig.duration;rig.duration=Mathf.Max(.1f,seconds);
            yield return rig.Move(from,to);rig.duration=old;
            if(destination==GameStage.Playing)EnterPlaying();
            else {SetStage(destination);preparation.SetActive(destination==GameStage.Preparation);}
        }
        void EnterPlaying()
        {
            orbit.AdoptCurrentView();
            foreach(var item in bodies)if(item.Key)item.Key.isKinematic=item.Value;
            foreach(var ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))if(ship.gameObject.scene==gameObject.scene)ship.Health.DamageBlocked=false;
            foreach(var c in paused)if(c)c.enabled=true;
            SetStage(GameStage.Playing);
        }
        void OnResult(BattleState result){SetStage(GameStage.Result);}
        void Reload(bool battleOnly)
        {
            Time.timeScale=1;pendingPath=gameObject.scene.path;pendingDirect=battleOnly;
            SceneManager.sceneLoaded-=ClearLaunch;SceneManager.sceneLoaded+=ClearLaunch;
            try{SceneManager.LoadSceneAsync(pendingPath,LoadSceneMode.Single);}catch{ClearLaunch(default,LoadSceneMode.Single);throw;}
        }
        void Exit()
        {
            if(Stage!=GameStage.MainMenu)return;
#if UNITY_EDITOR
            message.text="Exit is disabled in the Unity Editor.";
#else
            Application.Quit();
#endif
        }
        void OnDestroy(){if(battle){battle.Ended-=OnResult;if(battle.resultUI)battle.resultUI.ReloadRequested=null;}}
        void OnValidate(){goDuration=Mathf.Max(.1f,goDuration);}
    }
}
