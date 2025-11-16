using UnityEngine;
using System.Collections;

// ============================================
// AUDIO MANAGER - Sistema de audio global CON DEBUG COMPLETO
// ============================================
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    
    [Header("Music")]
    public AudioClip gameplayMusic;
    public AudioClip menuMusic;
    public AudioClip gameOverMusic;
    public bool loopMusic = true;
    
    [Header("SFX")]
    public AudioClip[] collectSounds; // [0]Coin, [1]Gem, [2]PowerCoin, [3]Bonus
    public AudioClip[] powerUpSounds; // [0]SpeedBoost, [1]Magnet, [2]DoublePoints, [3]Shield
    public AudioClip[] uiSounds; // [0]Click, [1]Hover, [2]Error, [3]Success
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float uiVolume = 0.9f;
    
    [Header("Fade Settings")]
    public float musicFadeDuration = 1f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private Coroutine musicFadeCoroutine;
    
    void Awake()
    {
        Debug.Log("🔊 AudioManager Awake() called");
        
        // Singleton pattern
        if (Instance == null)
        {
            Debug.Log("🔊 Creating AudioManager singleton instance");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Debug.Log("🔊 AudioManager instance already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("🔊 AudioManager Start() called");
        
        // Cargar configuraciones guardadas
        Debug.Log("🔊 Loading audio settings...");
        LoadAudioSettings();
        
        // Iniciar música de menú
        Debug.Log("🎵 About to call PlayMenuMusic()");
        PlayMenuMusic();
        Debug.Log("🎶 PlayMenuMusic() call completed");
    }
    
    void InitializeAudioManager()
    {
        Debug.Log("🔊 InitializeAudioManager() called");
        
        // Crear AudioSources si no existen
        if (musicSource == null)
        {
            Debug.Log("🔊 Creating Music Source");
            GameObject musicGO = new GameObject("Music Source");
            musicGO.transform.parent = transform;
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        else
        {
            Debug.Log("🔊 Music Source already assigned");
        }
        
        if (sfxSource == null)
        {
            Debug.Log("🔊 Creating SFX Source");
            GameObject sfxGO = new GameObject("SFX Source");
            sfxGO.transform.parent = transform;
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        else
        {
            Debug.Log("🔊 SFX Source already assigned");
        }
        
        if (uiSource == null)
        {
            Debug.Log("🔊 Creating UI Source");
            GameObject uiGO = new GameObject("UI Source");
            uiGO.transform.parent = transform;
            uiSource = uiGO.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
        }
        else
        {
            Debug.Log("🔊 UI Source already assigned");
        }
        
        Debug.Log("🔊 AudioManager initialized successfully!");
    }
    
    void LoadAudioSettings()
    {
        Debug.Log("🔊 LoadAudioSettings() called");
        
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        float savedUIVolume = PlayerPrefs.GetFloat("UIVolume", 0.9f);
        
        Debug.Log($"🔊 Loaded volumes - Music: {savedMusicVolume}, SFX: {savedSFXVolume}, UI: {savedUIVolume}");
        
        musicVolume = savedMusicVolume;
        sfxVolume = savedSFXVolume;
        uiVolume = savedUIVolume;
        
        ApplyVolumeSettings();
    }
    
    void ApplyVolumeSettings()
    {
        Debug.Log($"🔊 ApplyVolumeSettings() - Master: {masterVolume}, Music: {musicVolume}, SFX: {sfxVolume}, UI: {uiVolume}");
        
        if (musicSource != null)
        {
            float calculatedMusicVolume = musicVolume * masterVolume;
            musicSource.volume = calculatedMusicVolume;
            Debug.Log($"🎵 Music source volume set to: {calculatedMusicVolume}");
        }
        else
        {
            Debug.LogWarning("⚠️ Music source is null in ApplyVolumeSettings");
        }
        
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
            Debug.Log($"🔊 SFX source volume set to: {sfxVolume * masterVolume}");
        }
        
        if (uiSource != null)
        {
            uiSource.volume = uiVolume * masterVolume;
            Debug.Log($"🖱️ UI source volume set to: {uiVolume * masterVolume}");
        }
    }
    
    // ============================================
    // MÉTODOS DE MÚSICA CON DEBUG
    // ============================================
    
    public void PlayMenuMusic()
    {
        Debug.Log("🎵 PlayMenuMusic() called");
        Debug.Log($"🎵 Menu music assigned: {menuMusic != null}");
        
        if (menuMusic != null)
        {
            Debug.Log($"🎶 Menu music found: {menuMusic.name}, calling PlayMusic()");
            PlayMusic(menuMusic);
        }
        else
        {
            Debug.LogWarning("⚠️ No menu music assigned!");
        }
    }
    
    public void PlayGameplayMusic()
    {
        Debug.Log("🎮 PlayGameplayMusic() called");
        Debug.Log($"🎮 Gameplay music assigned: {gameplayMusic != null}");
        
        if (gameplayMusic != null)
        {
            Debug.Log($"🎶 Gameplay music found: {gameplayMusic.name}, calling PlayMusic()");
            PlayMusic(gameplayMusic);
        }
        else
        {
            Debug.LogError("❌ No gameplay music assigned!");
        }
    }
    
    public void PlayGameOverMusic()
    {
        Debug.Log("💀 PlayGameOverMusic() called");
        Debug.Log($"💀 Game over music assigned: {gameOverMusic != null}");
        
        if (gameOverMusic != null)
        {
            Debug.Log($"🎶 Game over music found: {gameOverMusic.name}, calling PlayMusic()");
            PlayMusic(gameOverMusic);
        }
        else
        {
            Debug.LogWarning("⚠️ No game over music assigned!");
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        Debug.Log($"🎵 PlayMusic() called with clip: {(clip != null ? clip.name : "NULL")}");
        Debug.Log($"🎵 Music source exists: {musicSource != null}");
        
        if (musicSource == null || clip == null) 
        {
            Debug.LogError("❌ PlayMusic() - Music source or clip is null!");
            return;
        }
        
        if (musicSource.clip == clip && musicSource.isPlaying) 
        {
            Debug.Log("🎵 Same clip already playing, skipping");
            return;
        }
        
        Debug.Log("🎵 Starting fade to new music");
        
        if (musicFadeCoroutine != null)
        {
            Debug.Log("🎵 Stopping previous fade coroutine");
            StopCoroutine(musicFadeCoroutine);
        }
        
        musicFadeCoroutine = StartCoroutine(FadeToNewMusic(clip));
    }
    
    IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        Debug.Log($"🎵 FadeToNewMusic started with: {newClip.name}");
        Debug.Log($"🎵 Current music playing: {musicSource.isPlaying}");
        Debug.Log($"🎵 Current volume: {musicSource.volume}");
        Debug.Log($"🎵 Target volume will be: {musicVolume * masterVolume}");
        
        // Fade out música actual
        if (musicSource.isPlaying)
        {
            Debug.Log("🎵 Fading out current music");
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            
            while (elapsed < musicFadeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / (musicFadeDuration * 0.5f);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, fadeCurve.Evaluate(progress));
                yield return null;
            }
            
            musicSource.Stop();
            Debug.Log("🎵 Fade out completed, music stopped");
        }
        else
        {
            Debug.Log("🎵 No music currently playing, skipping fade out");
        }
        
        // Cambiar clip y hacer fade in
        Debug.Log("🎵 Setting new clip and starting fade in");
        musicSource.clip = newClip;
        musicSource.loop = loopMusic;
        
        // VERIFICACIÓN CRÍTICA ANTES DE PLAY
        Debug.Log($"🎵 About to call musicSource.Play()");
        Debug.Log($"🎵 Music source enabled: {musicSource.enabled}");
        Debug.Log($"🎵 Music source gameObject active: {musicSource.gameObject.activeInHierarchy}");
        Debug.Log($"🎵 AudioListener volume: {AudioListener.volume}");
        Debug.Log($"🎵 AudioListeners in scene: {FindObjectsOfType<AudioListener>().Length}");
        
        musicSource.Play();
        
        Debug.Log($"🎵 musicSource.Play() called");
        Debug.Log($"🎵 Music source playing: {musicSource.isPlaying}");
        Debug.Log($"🎵 Music source time: {musicSource.time}");
        Debug.Log($"🎵 Music source volume: {musicSource.volume}");
        Debug.Log($"🎵 Clip length: {newClip.length} seconds");
        Debug.Log($"🎵 Clip frequency: {newClip.frequency}Hz");
        Debug.Log($"🎵 Clip channels: {newClip.channels}");
        
        float targetVolume = musicVolume * masterVolume;
        float elapsed2 = 0f;
        
        Debug.Log($"🎵 Starting fade in to volume: {targetVolume}");
        
        while (elapsed2 < musicFadeDuration * 0.5f)
        {
            elapsed2 += Time.unscaledDeltaTime;
            float progress = elapsed2 / (musicFadeDuration * 0.5f);
            float newVolume = Mathf.Lerp(0f, targetVolume, fadeCurve.Evaluate(progress));
            musicSource.volume = newVolume;
            
            // Log cada segundo durante el fade
            if (Mathf.FloorToInt(elapsed2) != Mathf.FloorToInt(elapsed2 - Time.unscaledDeltaTime))
            {
                Debug.Log($"🎵 Fade in progress: {progress:F2}, Volume: {newVolume:F3}");
            }
            
            yield return null;
        }
        
        musicSource.volume = targetVolume;
        Debug.Log($"🎵 Fade in completed! Final volume: {musicSource.volume}");
        Debug.Log($"🎵 Music is playing: {musicSource.isPlaying}");
        Debug.Log($"🎵 Music time: {musicSource.time}");
    }
    
    public void StopMusic(bool immediate = false)
    {
        Debug.Log($"🎵 StopMusic() called, immediate: {immediate}");
        
        if (musicSource == null) 
        {
            Debug.LogWarning("⚠️ Music source is null in StopMusic");
            return;
        }
        
        if (immediate)
        {
            Debug.Log("🎵 Stopping music immediately");
            musicSource.Stop();
        }
        else
        {
            Debug.Log("🎵 Starting fade out");
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }
    }
    
    IEnumerator FadeOutMusic()
    {
        Debug.Log("🎵 FadeOutMusic() started");
        
        if (!musicSource.isPlaying) 
        {
            Debug.Log("🎵 No music playing, fade out cancelled");
            yield break;
        }
        
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        Debug.Log($"🎵 Fading out from volume: {startVolume}");
        
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / musicFadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, fadeCurve.Evaluate(progress));
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = startVolume;
        Debug.Log("🎵 Fade out completed, music stopped");
    }
    
    // ============================================
    // MÉTODOS DE SFX CON DEBUG
    // ============================================
    
    public void PlayCollectSound(CollectibleCollision.CollectibleType collectibleType)
    {
        Debug.Log($"🪙 PlayCollectSound() called with type: {collectibleType}");
        
        AudioClip soundToPlay = null;
        
        switch (collectibleType)
        {
            case CollectibleCollision.CollectibleType.Coin:
                soundToPlay = GetSafeAudioClip(collectSounds, 0);
                break;
            case CollectibleCollision.CollectibleType.Gem:
                soundToPlay = GetSafeAudioClip(collectSounds, 1);
                break;
            case CollectibleCollision.CollectibleType.PowerCoin:
                soundToPlay = GetSafeAudioClip(collectSounds, 2);
                break;
            case CollectibleCollision.CollectibleType.BonusItem:
                soundToPlay = GetSafeAudioClip(collectSounds, 3);
                break;
        }
        
        if (soundToPlay != null)
        {
            Debug.Log($"🪙 Playing collect sound: {soundToPlay.name}");
            PlaySFX(soundToPlay);
        }
        else
        {
            Debug.LogWarning($"⚠️ No sound found for collectible type: {collectibleType}");
        }
    }
    
    public void PlayPowerUpSound(CollectibleCollision.PowerUpType powerUpType)
    {
        Debug.Log($"⚡ PlayPowerUpSound() called with type: {powerUpType}");
        
        AudioClip soundToPlay = null;
        
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                soundToPlay = GetSafeAudioClip(powerUpSounds, 0);
                break;
            case CollectibleCollision.PowerUpType.Magnet:
                soundToPlay = GetSafeAudioClip(powerUpSounds, 1);
                break;
            case CollectibleCollision.PowerUpType.DoublePoints:
                soundToPlay = GetSafeAudioClip(powerUpSounds, 2);
                break;
            case CollectibleCollision.PowerUpType.Shield:
                soundToPlay = GetSafeAudioClip(powerUpSounds, 3);
                break;
        }
        
        if (soundToPlay != null)
        {
            Debug.Log($"⚡ Playing power-up sound: {soundToPlay.name}");
            PlaySFX(soundToPlay);
        }
        else
        {
            Debug.LogWarning($"⚠️ No sound found for power-up type: {powerUpType}");
        }
    }
    
    public void PlayUISound(int soundIndex)
    {
        Debug.Log($"🖱️ PlayUISound() called with index: {soundIndex}");
        
        AudioClip soundToPlay = GetSafeAudioClip(uiSounds, soundIndex);
        if (soundToPlay != null)
        {
            Debug.Log($"🖱️ Playing UI sound: {soundToPlay.name}");
            PlayUIAudio(soundToPlay);
        }
        else
        {
            Debug.LogWarning($"⚠️ No UI sound found at index: {soundIndex}");
        }
    }
    
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        Debug.Log($"🔊 PlaySFX() called with clip: {(clip != null ? clip.name : "NULL")}, volumeScale: {volumeScale}");
        
        if (sfxSource != null && clip != null)
        {
            Debug.Log($"🔊 Playing SFX at volume: {sfxSource.volume * volumeScale}");
            sfxSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning("⚠️ SFX source or clip is null");
        }
    }
    
    public void PlayUIAudio(AudioClip clip, float volumeScale = 1f)
    {
        Debug.Log($"🖱️ PlayUIAudio() called with clip: {(clip != null ? clip.name : "NULL")}, volumeScale: {volumeScale}");
        
        if (uiSource != null && clip != null)
        {
            Debug.Log($"🖱️ Playing UI audio at volume: {uiSource.volume * volumeScale}");
            uiSource.PlayOneShot(clip, volumeScale);
        }
        else
        {
            Debug.LogWarning("⚠️ UI source or clip is null");
        }
    }
    
    AudioClip GetSafeAudioClip(AudioClip[] array, int index)
    {
        if (array != null && index >= 0 && index < array.Length)
        {
            Debug.Log($"🎵 GetSafeAudioClip() returning: {array[index]?.name ?? "NULL"}");
            return array[index];
        }
        
        Debug.LogWarning($"⚠️ GetSafeAudioClip() - Invalid array or index. Array length: {array?.Length ?? 0}, Index: {index}");
        return null;
    }
    
    // ============================================
    // CONFIGURACIÓN DE VOLUMEN CON DEBUG
    // ============================================
    
    public void SetMasterVolume(float volume)
    {
        Debug.Log($"🎚️ SetMasterVolume() called with value: {volume}");
        
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        
        Debug.Log($"🎚️ Master volume set to: {masterVolume}");
    }
    
    public void SetMusicVolume(float volume)
    {
        Debug.Log($"🎵 SetMusicVolume() called with value: {volume}");
        
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            float newVolume = musicVolume * masterVolume;
            musicSource.volume = newVolume;
            Debug.Log($"🎵 Music volume updated to: {newVolume} (music: {musicVolume} * master: {masterVolume})");
        }
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }
    
    public void SetSFXVolume(float volume)
    {
        Debug.Log($"🔊 SetSFXVolume() called with value: {volume}");
        
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            float newVolume = sfxVolume * masterVolume;
            sfxSource.volume = newVolume;
            Debug.Log($"🔊 SFX volume updated to: {newVolume}");
        }
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }
    
    public void SetUIVolume(float volume)
    {
        Debug.Log($"🖱️ SetUIVolume() called with value: {volume}");
        
        uiVolume = Mathf.Clamp01(volume);
        if (uiSource != null)
        {
            float newVolume = uiVolume * masterVolume;
            uiSource.volume = newVolume;
            Debug.Log($"🖱️ UI volume updated to: {newVolume}");
        }
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS DE CONSULTA CON DEBUG
    // ============================================
    
    public bool IsMusicPlaying()
    {
        bool isPlaying = musicSource != null && musicSource.isPlaying;
        Debug.Log($"🎵 IsMusicPlaying() returning: {isPlaying}");
        return isPlaying;
    }
    
    public float GetMusicVolume()
    {
        Debug.Log($"🎵 GetMusicVolume() returning: {musicVolume}");
        return musicVolume;
    }
    
    public float GetSFXVolume()
    {
        Debug.Log($"🔊 GetSFXVolume() returning: {sfxVolume}");
        return sfxVolume;
    }
    
    public float GetUIVolume()
    {
        Debug.Log($"🖱️ GetUIVolume() returning: {uiVolume}");
        return uiVolume;
    }
    
    // ============================================
    // MÉTODOS DE DEBUG COMPLETOS
    // ============================================
    
    [ContextMenu("Debug Audio Status")]
    void DebugAudioStatus()
    {
        Debug.Log("=== AUDIO MANAGER DEBUG STATUS ===");
        Debug.Log($"Instance exists: {Instance != null}");
        Debug.Log($"GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"Component enabled: {enabled}");
        
        Debug.Log($"Music Source exists: {musicSource != null}");
        Debug.Log($"SFX Source exists: {sfxSource != null}");
        Debug.Log($"UI Source exists: {uiSource != null}");
        
        if (musicSource != null)
        {
            Debug.Log($"Music Source - Volume: {musicSource.volume}");
            Debug.Log($"Music Source - Is Playing: {musicSource.isPlaying}");
            Debug.Log($"Music Source - Current Clip: {(musicSource.clip != null ? musicSource.clip.name : "NULL")}");
            Debug.Log($"Music Source - Audio enabled: {musicSource.enabled}");
            Debug.Log($"Music Source - Mute: {musicSource.mute}");
            Debug.Log($"Music Source - GameObject active: {musicSource.gameObject.activeInHierarchy}");
            Debug.Log($"Music Source - Time: {musicSource.time}");
            Debug.Log($"Music Source - Loop: {musicSource.loop}");
        }
        
        Debug.Log($"Master Volume: {masterVolume}");
        Debug.Log($"Music Volume: {musicVolume}");
        Debug.Log($"SFX Volume: {sfxVolume}");
        Debug.Log($"UI Volume: {uiVolume}");
        
        Debug.Log($"Gameplay Music assigned: {gameplayMusic != null}");
        Debug.Log($"Menu Music assigned: {menuMusic != null}");
        Debug.Log($"Game Over Music assigned: {gameOverMusic != null}");
        
        if (gameplayMusic != null)
        {
            Debug.Log($"Gameplay Music name: {gameplayMusic.name}");
            Debug.Log($"Gameplay Music length: {gameplayMusic.length} seconds");
            Debug.Log($"Gameplay Music state: {gameplayMusic.loadState}");
        }
        
        if (menuMusic != null)
        {
            Debug.Log($"Menu Music name: {menuMusic.name}");
            Debug.Log($"Menu Music length: {menuMusic.length} seconds");
            Debug.Log($"Menu Music state: {menuMusic.loadState}");
        }
        
        // Verificar sistema de audio
        Debug.Log($"AudioListener volume: {AudioListener.volume}");
        Debug.Log($"AudioListeners in scene: {FindObjectsOfType<AudioListener>().Length}");
        Debug.Log($"Audio sample rate: {AudioSettings.outputSampleRate}Hz");
        Debug.Log($"Audio speaker mode: {AudioSettings.speakerMode}");
        
        Debug.Log("=== END AUDIO DEBUG ===");
    }
    
    [ContextMenu("Force Play Gameplay Music")]
    void ForcePlayGameplayMusic()
    {
        Debug.Log("🎵 FORCE PLAY GAMEPLAY MUSIC - Manual Test");
        
        if (gameplayMusic == null)
        {
            Debug.LogError("❌ No gameplay music assigned!");
            return;
        }
        
        if (musicSource == null)
        {
            Debug.LogError("❌ No music source found!");
            return;
        }
        
        // Forzar reproducción directa (sin fade)
        Debug.Log("🎵 Stopping current music");
        musicSource.Stop();
        
        Debug.Log("🎵 Setting gameplay music clip");
        musicSource.clip = gameplayMusic;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.loop = loopMusic;
        
        Debug.Log($"🎵 About to play - Volume: {musicSource.volume}, Loop: {musicSource.loop}");
        musicSource.Play();
        
        Debug.Log($"🎶 FORCE PLAY RESULT:");
        Debug.Log($"  - Playing: {musicSource.isPlaying}");
        Debug.Log($"  - Volume: {musicSource.volume}");
        Debug.Log($"  - Time: {musicSource.time}");
        Debug.Log($"  - Clip: {musicSource.clip.name}");
    }
    
    [ContextMenu("Force Play Menu Music")]
    void ForcePlayMenuMusic()
    {
        Debug.Log("🎵 FORCE PLAY MENU MUSIC - Manual Test");
        
        if (menuMusic == null)
        {
            Debug.LogError("❌ No menu music assigned!");
            return;
        }
        
        if (musicSource == null)
        {
            Debug.LogError("❌ No music source found!");
            return;
        }
        
        // Forzar reproducción directa
        musicSource.Stop();
        musicSource.clip = menuMusic;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.loop = loopMusic;
        musicSource.Play();
        
        Debug.Log($"🎶 FORCE MENU MUSIC RESULT: Playing: {musicSource.isPlaying}, Volume: {musicSource.volume}");
    }
    
    [ContextMenu("Test All Volume Settings")]
    void TestAllVolumeSettings()
    {
        StartCoroutine(TestVolumeSequence());
    }
    
    System.Collections.IEnumerator TestVolumeSequence()
    {
        Debug.Log("🎚️ STARTING VOLUME TEST SEQUENCE");
        
        // Guardar valores originales
        float originalMaster = masterVolume;
        float originalMusic = musicVolume;
        
        // Test 1: Volumen máximo
        Debug.Log("Test 1: Maximum volume");
        SetMasterVolume(1f);
        SetMusicVolume(1f);
        PlayGameplayMusic();
        yield return new WaitForSeconds(2f);
        
        // Test 2: Volumen medio
        Debug.Log("Test 2: Medium volume");
        SetMasterVolume(0.5f);
        SetMusicVolume(0.5f);
        yield return new WaitForSeconds(2f);
        
        // Test 3: Volumen bajo
        Debug.Log("Test 3: Low volume");
        SetMasterVolume(0.1f);
        SetMusicVolume(0.1f);
        yield return new WaitForSeconds(2f);
        
        // Restaurar valores originales
        Debug.Log("Restoring original volumes");
        SetMasterVolume(originalMaster);
        SetMusicVolume(originalMusic);
        
        Debug.Log("🎵 VOLUME TEST COMPLETED!");
    }
    
    [ContextMenu("Check Audio Configuration")]
    void CheckAudioConfiguration()
    {
        Debug.Log("=== AUDIO CONFIGURATION CHECK ===");
        
        // Verificar configuración del dispositivo
        Debug.Log($"Audio Device Sample Rate: {AudioSettings.outputSampleRate}Hz");
        Debug.Log($"Audio Driver Capabilities: {AudioSettings.driverCapabilities}");
        Debug.Log($"Speaker Mode: {AudioSettings.speakerMode}");
        Debug.Log($"DSP Buffer Size: {AudioSettings.GetConfiguration().dspBufferSize}");
        Debug.Log($"Sample Rate: {AudioSettings.GetConfiguration().sampleRate}");
        
        // Verificar configuración del proyecto
        Debug.Log($"Global Volume: {AudioListener.volume}");
        var audioListeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"Audio Listeners in scene: {audioListeners.Length}");
        
        for (int i = 0; i < audioListeners.Length; i++)
        {
            Debug.Log($"  AudioListener {i}: {audioListeners[i].gameObject.name}, Active: {audioListeners[i].gameObject.activeInHierarchy}");
        }
        
        // Verificar PlayerPrefs
        Debug.Log($"Saved Music Volume: {PlayerPrefs.GetFloat("MusicVolume", -1)}");
        Debug.Log($"Saved SFX Volume: {PlayerPrefs.GetFloat("SFXVolume", -1)}");
        Debug.Log($"Saved UI Volume: {PlayerPrefs.GetFloat("UIVolume", -1)}");
        Debug.Log($"Saved Master Volume: {PlayerPrefs.GetFloat("MasterVolume", -1)}");
        
        // Verificar arrays de sonidos
        Debug.Log($"Collect Sounds Array: {(collectSounds != null ? collectSounds.Length : 0)} elements");
        Debug.Log($"PowerUp Sounds Array: {(powerUpSounds != null ? powerUpSounds.Length : 0)} elements");
        Debug.Log($"UI Sounds Array: {(uiSounds != null ? uiSounds.Length : 0)} elements");
        
        Debug.Log("=== END CONFIGURATION CHECK ===");
    }
    
    [ContextMenu("Test Collect Sounds")]
    void TestCollectSounds()
    {
        Debug.Log("🪙 TESTING COLLECT SOUNDS");
        StartCoroutine(TestCollectSoundsCoroutine());
    }
    
    IEnumerator TestCollectSoundsCoroutine()
    {
        Debug.Log("🪙 Testing Coin sound");
        PlayCollectSound(CollectibleCollision.CollectibleType.Coin);
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("💎 Testing Gem sound");
        PlayCollectSound(CollectibleCollision.CollectibleType.Gem);
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("🪙 Testing PowerCoin sound");
        PlayCollectSound(CollectibleCollision.CollectibleType.PowerCoin);
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("🎁 Testing BonusItem sound");
        PlayCollectSound(CollectibleCollision.CollectibleType.BonusItem);
        
        Debug.Log("🪙 Collect sounds test completed");
    }
    
    [ContextMenu("Test Power-Up Sounds")]
    void TestPowerUpSounds()
    {
        Debug.Log("⚡ TESTING POWER-UP SOUNDS");
        StartCoroutine(TestPowerUpSoundsCoroutine());
    }
    
    IEnumerator TestPowerUpSoundsCoroutine()
    {
        Debug.Log("🚀 Testing SpeedBoost sound");
        PlayPowerUpSound(CollectibleCollision.PowerUpType.SpeedBoost);
        yield return new WaitForSeconds(0.8f);
        
        Debug.Log("🧲 Testing Magnet sound");
        PlayPowerUpSound(CollectibleCollision.PowerUpType.Magnet);
        yield return new WaitForSeconds(0.8f);
        
        Debug.Log("✨ Testing DoublePoints sound");
        PlayPowerUpSound(CollectibleCollision.PowerUpType.DoublePoints);
        yield return new WaitForSeconds(0.8f);
        
        Debug.Log("🛡️ Testing Shield sound");
        PlayPowerUpSound(CollectibleCollision.PowerUpType.Shield);
        
        Debug.Log("⚡ Power-up sounds test completed");
    }
    
    [ContextMenu("Test UI Sounds")]
    void TestUISounds()
    {
        Debug.Log("🖱️ TESTING UI SOUNDS");
        StartCoroutine(TestUISoundsCoroutine());
    }
    
    IEnumerator TestUISoundsCoroutine()
    {
        for (int i = 0; i < (uiSounds?.Length ?? 0); i++)
        {
            Debug.Log($"🖱️ Testing UI sound {i}");
            PlayUISound(i);
            yield return new WaitForSeconds(0.3f);
        }
        
        Debug.Log("🖱️ UI sounds test completed");
    }
    
    [ContextMenu("Emergency Audio Reset")]
    void EmergencyAudioReset()
    {
        Debug.Log("🚨 EMERGENCY AUDIO RESET");
        
        // Detener toda la música
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0.7f;
        }
        
        // Detener corrutinas
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }
        
        // Resetear volúmenes
        masterVolume = 1f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        uiVolume = 0.9f;
        
        // Aplicar configuración
        ApplyVolumeSettings();
        
        Debug.Log("🚨 Emergency reset completed");
    }
    
    [ContextMenu("Test Audio File Loading")]
    void TestAudioFileLoading()
    {
        Debug.Log("📁 TESTING AUDIO FILE LOADING");
        
        AudioClip[] allClips = { gameplayMusic, menuMusic, gameOverMusic };
        string[] clipNames = { "Gameplay Music", "Menu Music", "Game Over Music" };
        
        for (int i = 0; i < allClips.Length; i++)
        {
            if (allClips[i] != null)
            {
                Debug.Log($"✅ {clipNames[i]}: {allClips[i].name}");
                Debug.Log($"   Length: {allClips[i].length:F2}s");
                Debug.Log($"   Frequency: {allClips[i].frequency}Hz");
                Debug.Log($"   Channels: {allClips[i].channels}");
                Debug.Log($"   Load State: {allClips[i].loadState}");
                Debug.Log($"   Load Type: {allClips[i].loadType}");
                // Debug.Log($"   Compression Format: {allClips[i].compressionFormat}"); // No disponible en todas las versiones
            }
            else
            {
                Debug.LogError($"❌ {clipNames[i]}: NOT ASSIGNED");
            }
        }
        
        // Test arrays
        TestAudioArray(collectSounds, "Collect Sounds");
        TestAudioArray(powerUpSounds, "Power-Up Sounds");
        TestAudioArray(uiSounds, "UI Sounds");
    }
    
    void TestAudioArray(AudioClip[] array, string arrayName)
    {
        Debug.Log($"📁 Testing {arrayName} array:");
        
        if (array == null)
        {
            Debug.LogError($"❌ {arrayName} array is NULL");
            return;
        }
        
        if (array.Length == 0)
        {
            Debug.LogWarning($"⚠️ {arrayName} array is EMPTY");
            return;
        }
        
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null)
            {
                Debug.Log($"  ✅ [{i}]: {array[i].name} ({array[i].length:F2}s)");
            }
            else
            {
                Debug.LogError($"  ❌ [{i}]: NULL");
            }
        }
    }
    
    // ============================================
    // UTILIDADES DE DEBUG ADICIONALES
    // ============================================
    
    void Update()
    {
        // Solo en modo debug, presiona teclas para testing rápido
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("🎹 F1 pressed - Testing gameplay music");
            PlayGameplayMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("🎹 F2 pressed - Testing menu music");
            PlayMenuMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("🎹 F3 pressed - Debug audio status");
            DebugAudioStatus();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("🎹 F4 pressed - Stop all music");
            StopMusic(true);
        }
        #endif
    }
    
    void OnDestroy()
    {
        Debug.Log("🔊 AudioManager OnDestroy() called");
        
        // Cleanup
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"🔊 AudioManager - Application pause: {pauseStatus}");
        
        if (pauseStatus)
        {
            // Pausar audio cuando la aplicación se pausa
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
                Debug.Log("🎵 Music paused due to application pause");
            }
        }
        else
        {
            // Reanudar audio cuando la aplicación se reanuda
            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.UnPause();
                Debug.Log("🎵 Music resumed from application pause");
            }
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"🔊 AudioManager - Application focus: {hasFocus}");
        
        // Similar behavior to pause
        if (!hasFocus)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
                Debug.Log("🎵 Music paused due to focus lost");
            }
        }
        else
        {
            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.UnPause();
                Debug.Log("🎵 Music resumed from focus gained");
            }
        }
    }
}