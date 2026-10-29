using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GrandNavigation.Menu
{
    public sealed class MainMenuController : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] UnityEditor.SceneAsset startScene;
#endif
        [SerializeField, HideInInspector] string startScenePath;
        [SerializeField] Button startButton;
        bool loading;

#if UNITY_EDITOR
        void OnValidate()
        {
            startScenePath=startScene ? UnityEditor.AssetDatabase.GetAssetPath(startScene) : "";
        }
        public void Configure(UnityEditor.SceneAsset scene, Button button)
        {
            startScene=scene; startButton=button; OnValidate();
        }
#endif
        void Awake()
        {
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
            if(startButton) startButton.onClick.AddListener(StartGame);
        }
        public void StartGame()
        {
            if(loading) return;
            if(string.IsNullOrEmpty(startScenePath) || !Application.CanStreamedLevelBeLoaded(startScenePath))
            {
                Debug.LogError("Main menu target scene is missing or disabled in Build Profiles.",this);
                return;
            }
            loading=true;
            if(startButton) startButton.interactable=false;
            SceneManager.LoadSceneAsync(startScenePath,LoadSceneMode.Single);
        }
        void OnDestroy()
        {
            if(startButton) startButton.onClick.RemoveListener(StartGame);
        }
    }
}
