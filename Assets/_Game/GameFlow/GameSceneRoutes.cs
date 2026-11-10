using UnityEngine;
namespace GrandNavigation.GameFlow
{
    [CreateAssetMenu(menuName="Grand Navigation/Game Scene Routes")]
    public sealed class GameSceneRoutes : ScriptableObject
    {
        [Tooltip("Full scene asset path included in Scenes In Build.")] public string mainMenuScene;
        [Tooltip("Full scene asset path included in Scenes In Build.")] public string gameplayScene;
    }
}
