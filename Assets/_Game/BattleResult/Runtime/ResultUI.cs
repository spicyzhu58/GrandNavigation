using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GrandNavigation.GameFlow;
namespace GrandNavigation.Gameplay.Battle
{
    public sealed class ResultUI : MonoBehaviour
    {
        public GameObject panel;
        public Text title;
        public Button retryButton, mainMenuButton;
        public GameSceneRoutes sceneRoutes;
        public System.Action<bool> ReloadRequested;
        bool loading, shown;
        public int ShowCount { get; private set; }
        void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            mainMenuButton.interactable = sceneRoutes && !string.IsNullOrEmpty(sceneRoutes.mainMenuScene) && Application.CanStreamedLevelBeLoaded(sceneRoutes.mainMenuScene);
        }
        public void Show(BattleState state)
        {
            if (shown || state == BattleState.Playing) return;
            shown = true; ShowCount++;
            title.text = state == BattleState.Victory ? "VICTORY" : "DEFEAT";
            panel.SetActive(true);
        }
        public void Retry()
        {
            if (!shown || loading) return;
            var scene = gameObject.scene;
            if (!Application.CanStreamedLevelBeLoaded(scene.path)) { Debug.LogError("Battle scene is missing from Scenes In Build.", this); return; }
            loading = true; retryButton.interactable = mainMenuButton.interactable = false;
            Time.timeScale = 1;
            if(ReloadRequested!=null){ReloadRequested(true);return;}
            SceneManager.LoadSceneAsync(scene.path,LoadSceneMode.Single);
        }
        public void ReturnToMainMenu()
        {
            if (!shown || loading || !sceneRoutes || string.IsNullOrEmpty(sceneRoutes.mainMenuScene) || !Application.CanStreamedLevelBeLoaded(sceneRoutes.mainMenuScene)) return;
            loading = true; retryButton.interactable = mainMenuButton.interactable = false;
            Time.timeScale = 1;
            if(ReloadRequested!=null){ReloadRequested(false);return;}
            SceneManager.LoadSceneAsync(sceneRoutes.mainMenuScene,LoadSceneMode.Single);
        }
        void OnDestroy() { if (retryButton) retryButton.onClick.RemoveListener(Retry); if(mainMenuButton) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu); }
    }
}
