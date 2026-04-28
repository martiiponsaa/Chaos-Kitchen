using UnityEngine;

public class GeneralSettingsApp : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 90; ; // o el refresh del HMD o Screen.currentResolution.refreshRate
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
