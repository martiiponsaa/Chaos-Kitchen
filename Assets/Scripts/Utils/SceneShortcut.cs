using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class SceneShortcut : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var systems = FindObjectsOfType<EventSystem>();
        if (systems == null || systems.Length <= 1) return;

        // Keep the first EventSystem and remove any extras to avoid duplicate warnings
        for (int i = 1; i < systems.Length; i++)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(systems[i].gameObject);
            else
                DestroyImmediate(systems[i].gameObject);
#else
            Destroy(systems[i].gameObject);
#endif
        }
    }
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
#if UNITY_EDITOR
        // In the Editor try to find the scene asset by name and open it.
        // Use different APIs depending on Editor play state so we don't call OpenScene during play mode.
        string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (EditorApplication.isPlaying)
            {
                // Load scene during play mode using the Editor helper that accepts a scene asset path
                EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            }
            else
            {
                // Not playing: open the scene in the Editor
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            return;
        }
#endif

        // At runtime (or if not found as an asset in the Editor) fall back to loading by name via Build Settings
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
