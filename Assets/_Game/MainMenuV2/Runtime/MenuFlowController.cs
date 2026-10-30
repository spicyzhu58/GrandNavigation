using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace GrandNavigation.MainMenuV2
{
    public sealed class MenuFlowController : MonoBehaviour
    {
        public enum MenuState { MainMenu,TransitionToPreparation,ShipPreparation,TransitionToMenu }
        public MenuState State { get; private set; }
        public MenuCameraRig cameraRig;
        public ShipPreparationController preparation;
        public GameObject menuPanel,preparationPanel;
        public Button startButton,exitButton,goButton,backButton;
        public Text message;
        public string gameplaySceneName="Gameplay";
        void Awake()
        {
            Application.runInBackground=true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            startButton.onClick.AddListener(StartPreparation);backButton.onClick.AddListener(Back);
            goButton.onClick.AddListener(Go);exitButton.onClick.AddListener(Exit);
            cameraRig.Initialize();SetState(MenuState.MainMenu);
        }
        void SetState(MenuState state)
        {
            State=state;
            menuPanel.SetActive(state==MenuState.MainMenu);
            preparationPanel.SetActive(state==MenuState.ShipPreparation);
            startButton.interactable=exitButton.interactable=state==MenuState.MainMenu;
            goButton.interactable=backButton.interactable=state==MenuState.ShipPreparation;
        }
        public void StartPreparation()
        {
            if(State!=MenuState.MainMenu)return;
            StartCoroutine(Transition(true));
        }
        public void Back()
        {
            if(State!=MenuState.ShipPreparation)return;
            StartCoroutine(Transition(false));
        }
        IEnumerator Transition(bool forward)
        {
            message.text="";
            if(!forward)preparation.SetActive(false);
            SetState(forward?MenuState.TransitionToPreparation:MenuState.TransitionToMenu);
            yield return cameraRig.Move(forward?cameraRig.menuPose:cameraRig.preparationPose,forward?cameraRig.preparationPose:cameraRig.menuPose);
            SetState(forward?MenuState.ShipPreparation:MenuState.MainMenu);
            if(forward)preparation.SetActive(true);
        }
        public void Go()
        {
            if(State!=MenuState.ShipPreparation)return;
            // Navigation boundary: a future gameplay implementation can use gameplaySceneName here.
            message.text="Gameplay scene not implemented yet.";
        }
        public void Exit()
        {
            if(State!=MenuState.MainMenu)return;
#if UNITY_EDITOR
            message.text="Exit is disabled in the Unity Editor.";
#else
            Application.Quit();
#endif
        }
    }
}

