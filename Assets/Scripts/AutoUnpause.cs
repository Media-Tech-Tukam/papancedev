using UnityEngine;

public class AutoUnpause : MonoBehaviour
{
    void Start()
    {
        // Forzar que el juego esté activo al iniciar
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = false;
        #endif
        
        // Asegurar que timeScale sea 1
        Time.timeScale = 1f;
        
        Debug.Log("Auto-unpause applied!");
    }
    
    void Update()
    {
        // Unpause automático si detecta pausa al inicio
        #if UNITY_EDITOR
        if (Time.timeSinceLevelLoad < 1f && UnityEditor.EditorApplication.isPaused)
        {
            UnityEditor.EditorApplication.isPaused = false;
            Debug.Log("Auto-unpaused game on startup!");
        }
        #endif
    }
}