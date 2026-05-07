using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class SceneShortcut : MonoBehaviour
{
    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        if (kb == null) return;
        bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        if (!shift) return;

        if (kb.digit1Key.wasPressedThisFrame) LoadScene("StarsScene");
        if (kb.digit2Key.wasPressedThisFrame) LoadScene("FirstLevel");
        if (kb.digit3Key.wasPressedThisFrame) LoadScene("SecondLevel");
        if (kb.digit4Key.wasPressedThisFrame) LoadScene("ThirdLevel");
#else
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) LoadScene("StarsScene");
            if (Input.GetKeyDown(KeyCode.Alpha2)) LoadScene("FirstLevel");
            if (Input.GetKeyDown(KeyCode.Alpha3)) LoadScene("SecondLevel");
            if (Input.GetKeyDown(KeyCode.Alpha4)) LoadScene("ThirdLevel");
        }
#endif
    }

    void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene '" + sceneName + "' not in Build Settings or cannot be loaded.");
        }
    }
}
