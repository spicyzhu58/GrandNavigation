using NUnit.Framework;
using UnityEngine.InputSystem;
namespace GrandNavigation.Gameplay.Tests
{
    // CLI tests inject virtual devices; editor window focus must not swallow those events.
    // Changes are in-memory only and restored even when a test assertion fails.
    [SetUpFixture]
    public sealed class InputTestFocusScope
    {
        InputSettings settings;
        InputSettings.BackgroundBehavior background;
        InputSettings.EditorInputBehaviorInPlayMode routing;
        [OneTimeSetUp]public void Begin()
        {
            settings=InputSystem.settings;background=settings.backgroundBehavior;routing=settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }
        [OneTimeTearDown]public void End()
        {
            if(!settings)return;
            settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=routing;
        }
    }
}
