using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace GrandNavigation.Gameplay.Battle
{
    public sealed class ResultUI : MonoBehaviour
    {
        public GameObject panel;
        public Text title;
        public Button retryButton, mainMenuButton;
        bool loading, shown;
        public int ShowCount { get; private set; }
        void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            mainMenuButton.interactable = false;
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
            loading = true; retryButton.interactable = false;
            Time.timeScale = 1;
            SceneManager.LoadScene(scene.path);
        }
        // Integration point only. No guessed scene name or navigation until menu integration.
        public void ReturnToMainMenu() { }
        void OnDestroy() { if (retryButton) retryButton.onClick.RemoveListener(Retry); }
    }
}
