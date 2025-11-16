using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

// ============================================
// FIREBASE RANKING MANAGER - Sistema completo con Firebase
// ============================================

public class FirebaseRankingManager : RankingManagerBase
{
    [Header("Firebase Integration")]
    public FirebaseRankingSync firebaseSync;
    public bool useFirebase = true;
    public bool syncOnRegister = true;
    public bool syncOnScoreAdd = true;
    public float syncTimeout = 10f;
    
    [Header("WebGL Storage")]
    public bool usePlayerPrefs = true; // Para WebGL en lugar de JSON
    
    // Estado de sincronización
    private bool isSyncing = false;
    private Queue<System.Action> pendingSyncActions = new Queue<System.Action>();
    
    // Nuevos eventos específicos de Firebase
    public System.Action<bool> OnFirebaseSyncCompleted;
    public System.Action<string> OnFirebaseSyncError;
    
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Setup file path
        filePath = Path.Combine(Application.persistentDataPath, "ranking_data.json");
        
        Debug.Log($"🏆 FirebaseRankingManager initialized. File path: {filePath}");
        
        // Configurar Firebase
        if (firebaseSync == null)
            firebaseSync = GetComponent<FirebaseRankingSync>();
        
        if (firebaseSync == null)
        {
            Debug.LogWarning("⚠️ FirebaseRankingSync not found. Adding component automatically.");
            firebaseSync = gameObject.AddComponent<FirebaseRankingSync>();
        }
        
        SetupFirebaseEvents();
    }
    
    void Start()
    {
        // En WebGL, cargar desde PlayerPrefs en lugar de archivo
        if (Application.platform == RuntimePlatform.WebGLPlayer && usePlayerPrefs)
        {
            LoadFromPlayerPrefs();
        }
        else
        {
            LoadRankingData();
        }
        
        LoadCurrentPlayer();
        
        // Sincronizar con Firebase al inicio
        if (useFirebase)
        {
            StartCoroutine(InitialFirebaseSync());
        }
    }
    
    public void AddPlayerToCache(PlayerData player)
{
    if (player == null) return;
    
    // Verificar si ya existe en la lista
    var existingPlayer = rankingData.players.Find(p => p.id == player.id);
    if (existingPlayer != null)
    {
        // Actualizar datos existentes
        int index = rankingData.players.IndexOf(existingPlayer);
        rankingData.players[index] = player;
        Debug.Log($"🔄 Updated player in cache: {player.name}");
    }
    else
    {
        // Agregar nuevo jugador
        rankingData.players.Add(player);
        Debug.Log($"➕ Added new player to cache: {player.name}");
    }
    
    // Guardar datos después de actualizar
    SaveRankingData();
    
    // Disparar evento de ranking actualizado
    OnRankingUpdated?.Invoke(GetTopPlayers(10));
    
    Debug.Log($"📊 Total players in cache: {rankingData.players.Count}");
}

    void SetupFirebaseEvents()
    {
        if (firebaseSync != null)
        {
            firebaseSync.OnPlayerSaved += OnFirebasePlayerSaved;
            firebaseSync.OnTopPlayersLoaded += OnFirebaseTopPlayersLoaded;
            firebaseSync.OnError += OnFirebaseError;
        }
    }
    
    // ============================================
    // MÉTODOS DE CARGA Y GUARDADO
    // ============================================
    
    void LoadRankingData()
    {
        try
        {
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                rankingData = JsonUtility.FromJson<RankingData>(jsonData);
                
                if (rankingData == null)
                    rankingData = new RankingData();
                
                Debug.Log($"✅ Ranking data loaded. Players: {rankingData.players.Count}");
            }
            else
            {
                rankingData = new RankingData();
                Debug.Log("📄 New ranking file created.");
            }
            
            isDataLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading ranking data: {e.Message}");
            rankingData = new RankingData();
            isDataLoaded = true;
        }
    }
    
    void LoadFromPlayerPrefs()
    {
        try
        {
            string rankingJson = PlayerPrefs.GetString("RankingData", "");
            
            if (!string.IsNullOrEmpty(rankingJson))
            {
                rankingData = JsonUtility.FromJson<RankingData>(rankingJson);
                if (rankingData == null)
                    rankingData = new RankingData();
                
                Debug.Log($"✅ Ranking data loaded from PlayerPrefs. Players: {rankingData.players.Count}");
            }
            else
            {
                rankingData = new RankingData();
                Debug.Log("📄 New ranking data created for WebGL.");
            }
            
            isDataLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading from PlayerPrefs: {e.Message}");
            rankingData = new RankingData();
            isDataLoaded = true;
        }
    }
    
    void SaveRankingData()
    {
        if (!isDataLoaded) return;
        
        if (Application.platform == RuntimePlatform.WebGLPlayer && usePlayerPrefs)
        {
            SaveToPlayerPrefs();
        }
        else
        {
            SaveToFile();
        }
    }
    
    void SaveToPlayerPrefs()
    {
        try
        {
            rankingData.lastUpdate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string jsonData = JsonUtility.ToJson(rankingData, true);
            PlayerPrefs.SetString("RankingData", jsonData);
            PlayerPrefs.Save();
            
            Debug.Log("💾 Ranking data saved to PlayerPrefs successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error saving to PlayerPrefs: {e.Message}");
        }
    }
    
    void SaveToFile()
    {
        try
        {
            rankingData.lastUpdate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string jsonData = JsonUtility.ToJson(rankingData, true);
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log("💾 Ranking data saved successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error saving ranking data: {e.Message}");
        }
    }
    
    void LoadCurrentPlayer()
    {
        string playerId = PlayerPrefs.GetString("CurrentPlayerId", "");
        
        if (!string.IsNullOrEmpty(playerId))
        {
            currentPlayer = GetPlayerById(playerId);
            
            if (currentPlayer != null)
            {
                Debug.Log($"👤 Current player loaded: {currentPlayer.name}");
            }
            else
            {
                PlayerPrefs.DeleteKey("CurrentPlayerId");
                Debug.LogWarning("⚠️ Current player ID found but player data missing. Cleared.");
            }
        }
    }
    
    // ============================================
    // IMPLEMENTACIÓN DE MÉTODOS ABSTRACTOS
    // ============================================
    
    public override bool RegisterPlayer(string name, string email, string phone)
    {
        // Validación básica
        if (string.IsNullOrWhiteSpace(name) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(phone))
        {
            Debug.LogWarning("⚠️ Registration failed: Empty fields");
            return false;
        }
        
        if (!IsValidEmail(email))
        {
            Debug.LogWarning("⚠️ Registration failed: Invalid email");
            return false;
        }
        
        if (!IsValidPhone(phone))
        {
            Debug.LogWarning("⚠️ Registration failed: Invalid phone");
            return false;
        }
        
        if (GetPlayerByEmail(email) != null)
        {
            Debug.LogWarning("⚠️ Registration failed: Email already registered");
            return false;
        }
        
        // Crear nuevo jugador
        currentPlayer = new PlayerData(name.Trim(), email.Trim().ToLower(), phone.Trim());
        
        // Agregar al ranking
        rankingData.players.Add(currentPlayer);
        
        // Guardar datos
        SaveCurrentPlayer();
        SaveRankingData();
        
        Debug.Log($"🎉 Player registered successfully: {currentPlayer.name} ({currentPlayer.email})");
        
        // Disparar evento
        OnPlayerRegistered?.Invoke(currentPlayer);
        
        // Sincronizar con Firebase si está habilitado
        if (useFirebase && syncOnRegister)
        {
            SyncPlayerToFirebase(currentPlayer);
        }
        
        return true;
    }
    
    public override void AddScore(int score, float distance, int coins, int gems, int powerCoins, 
                                 string gameOverReason, float playTime)
    {
        if (currentPlayer == null)
        {
            Debug.LogWarning("⚠️ Cannot add score: No current player");
            return;
        }
        
        // Crear nueva puntuación
        PlayerScore newScore = new PlayerScore(score, distance, coins, gems, powerCoins, gameOverReason, playTime);
        
        // Agregar a la lista del jugador
        currentPlayer.scores.Add(newScore);
        
        // Actualizar estadísticas del jugador
        UpdatePlayerStats(newScore);
        
        // Mantener solo las mejores puntuaciones
        if (currentPlayer.scores.Count > maxScoresPerPlayer)
        {
            currentPlayer.scores = currentPlayer.scores
                .OrderByDescending(s => s.score)
                .Take(maxScoresPerPlayer)
                .ToList();
        }
        
        // Guardar datos
        SaveRankingData();
        
        // Calcular nueva posición en ranking
        int newRank = GetPlayerRank(currentPlayer.id);
        
        Debug.Log($"🎯 Score added for {currentPlayer.name}: {score} points (Rank: #{newRank})");
        
        // Disparar eventos
        OnPlayerRankChanged?.Invoke(newRank);
        OnRankingUpdated?.Invoke(GetTopPlayers(10));
        
        // Sincronizar con Firebase si está habilitado
        if (useFirebase && syncOnScoreAdd)
        {
            SyncPlayerToFirebase(currentPlayer);
        }
    }
    
    void UpdatePlayerStats(PlayerScore newScore)
    {
        currentPlayer.totalGames++;
        currentPlayer.totalCoins += newScore.coins;
        currentPlayer.totalGems += newScore.gems;
        currentPlayer.lastPlayDate = newScore.timestamp;
        
        if (newScore.score > currentPlayer.bestScore)
        {
            currentPlayer.bestScore = newScore.score;
            Debug.Log($"🏆 NEW PERSONAL BEST for {currentPlayer.name}: {newScore.score}!");
        }
        
        if (newScore.distance > currentPlayer.bestDistance)
        {
            currentPlayer.bestDistance = newScore.distance;
        }
    }
    
    public override List<PlayerData> GetTopPlayers(int count = 10)
    {
        if (!isDataLoaded) return new List<PlayerData>();
        
        return rankingData.players
            .Where(p => p.bestScore > 0)
            .OrderByDescending(p => p.bestScore)
            .ThenByDescending(p => p.bestDistance)
            .Take(count)
            .ToList();
    }
    
    public override int GetPlayerRank(string playerId)
    {
        var allPlayers = rankingData.players
            .OrderByDescending(p => p.bestScore)
            .ThenByDescending(p => p.bestDistance)
            .ToList();
        
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].id == playerId)
                return i + 1;
        }
        
        return -1;
    }
    
    public override PlayerData GetPlayerById(string id)
    {
        if (!isDataLoaded) return null;
        
        return rankingData.players.FirstOrDefault(p => p.id == id);
    }
    
    public override PlayerData GetPlayerByEmail(string email)
    {
        if (!isDataLoaded) return null;
        
        return rankingData.players.FirstOrDefault(p => p.email.ToLower() == email.ToLower());
    }
    
    public override bool IsEmailRegistered(string email)
    {
        return GetPlayerByEmail(email) != null;
    }
    
    // ============================================
    // MÉTODOS DE LOGIN
    // ============================================
    
    public bool LoginPlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("⚠️ Login failed: Empty player ID");
            return false;
        }
        
        PlayerData player = GetPlayerById(playerId);
        
        if (player == null)
        {
            Debug.LogWarning($"⚠️ Login failed: Player with ID {playerId} not found");
            return false;
        }
        
        currentPlayer = player;
        currentPlayer.lastPlayDate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        
        SaveCurrentPlayer();
        SaveRankingData();
        
        Debug.Log($"✅ Player logged in successfully: {currentPlayer.name} ({currentPlayer.email})");
        
        OnPlayerRegistered?.Invoke(currentPlayer);
        
        return true;
    }
    
    public bool LoginPlayerByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogWarning("⚠️ Login failed: Empty email");
            return false;
        }
        
        PlayerData player = GetPlayerByEmail(email);
        
        if (player == null)
        {
            Debug.LogWarning($"⚠️ Login failed: No player found with email {email}");
            return false;
        }
        
        return LoginPlayer(player.id);
    }
    
    // ============================================
    // MÉTODOS DE FIREBASE
    // ============================================
    
    IEnumerator InitialFirebaseSync()
    {
        Debug.Log("🔄 Starting initial Firebase sync...");
        yield return new WaitForSeconds(1f);
        
        if (firebaseSync != null)
        {
            firebaseSync.LoadTopPlayers(50);
        }
    }
    
    void SyncPlayerToFirebase(PlayerData player)
    {
        if (firebaseSync == null || isSyncing) 
        {
            pendingSyncActions.Enqueue(() => SyncPlayerToFirebase(player));
            return;
        }
        
        Debug.Log($"🔄 Syncing player to Firebase: {player.name}");
        
        isSyncing = true;
        firebaseSync.SavePlayer(player);
        
        StartCoroutine(SyncTimeoutCoroutine());
    }
    
    IEnumerator SyncTimeoutCoroutine()
    {
        yield return new WaitForSeconds(syncTimeout);
        
        if (isSyncing)
        {
            Debug.LogWarning("⚠️ Firebase sync timeout");
            isSyncing = false;
            ProcessPendingSyncActions();
        }
    }
    
    void ProcessPendingSyncActions()
    {
        if (pendingSyncActions.Count > 0)
        {
            var action = pendingSyncActions.Dequeue();
            action?.Invoke();
        }
    }
    
    void OnFirebasePlayerSaved(FirebasePlayerData firebasePlayer)
    {
        Debug.Log($"✅ Player synced to Firebase: {firebasePlayer.name}");
        
        isSyncing = false;
        OnFirebaseSyncCompleted?.Invoke(true);
        
        ProcessPendingSyncActions();
    }
    
    void OnFirebaseTopPlayersLoaded(List<FirebasePlayerData> firebasePlayers)
    {
        Debug.Log($"📥 Received {firebasePlayers.Count} players from Firebase");
        
        MergeFirebaseData(firebasePlayers);
        OnRankingUpdated?.Invoke(GetTopPlayers(10));
    }
    
    void OnFirebaseError(string error)
    {
        Debug.LogError($"❌ Firebase sync error: {error}");
        
        isSyncing = false;
        OnFirebaseSyncError?.Invoke(error);
        
        ProcessPendingSyncActions();
    }
    
    void MergeFirebaseData(List<FirebasePlayerData> firebasePlayers)
    {
        if (rankingData?.players == null) return;
        
        int merged = 0;
        int added = 0;
        
        foreach (var firebasePlayer in firebasePlayers)
        {
            var existingPlayer = rankingData.players.FirstOrDefault(p => p.id == firebasePlayer.id);
            
            if (existingPlayer != null)
            {
                if (firebasePlayer.bestScore > existingPlayer.bestScore ||
                    firebasePlayer.totalGames > existingPlayer.totalGames)
                {
                    UpdatePlayerFromFirebase(existingPlayer, firebasePlayer);
                    merged++;
                }
            }
            else
            {
                PlayerData newPlayer = ConvertFromFirebaseData(firebasePlayer);
                rankingData.players.Add(newPlayer);
                added++;
            }
        }
        
        Debug.Log($"🔄 Firebase merge complete: {merged} updated, {added} added");
        SaveRankingData();
    }
    
    void UpdatePlayerFromFirebase(PlayerData local, FirebasePlayerData firebase)
    {
        if (firebase.bestScore > local.bestScore)
            local.bestScore = firebase.bestScore;
        
        if (firebase.bestDistance > local.bestDistance)
            local.bestDistance = firebase.bestDistance;
        
        if (firebase.totalGames > local.totalGames)
            local.totalGames = firebase.totalGames;
        
        if (firebase.totalCoins > local.totalCoins)
            local.totalCoins = firebase.totalCoins;
        
        if (firebase.totalGems > local.totalGems)
            local.totalGems = firebase.totalGems;
        
        if (firebase.scores != null && firebase.scores.Count > 0)
        {
            var allScores = new List<PlayerScore>(local.scores);
            allScores.AddRange(firebase.scores);
            
            local.scores = allScores
                .GroupBy(s => s.score)
                .Select(g => g.First())
                .OrderByDescending(s => s.score)
                .Take(maxScoresPerPlayer)
                .ToList();
        }
        
        if (!string.IsNullOrEmpty(firebase.lastPlayDate))
            local.lastPlayDate = firebase.lastPlayDate;
    }
    
    PlayerData ConvertFromFirebaseData(FirebasePlayerData firebase)
    {
        PlayerData player = new PlayerData(firebase.name, firebase.email, firebase.phone);
        player.id = firebase.id;
        player.bestScore = firebase.bestScore;
        player.bestDistance = firebase.bestDistance;
        player.totalGames = firebase.totalGames;
        player.totalCoins = firebase.totalCoins;
        player.totalGems = firebase.totalGems;
        player.firstPlayDate = firebase.firstPlayDate;
        player.lastPlayDate = firebase.lastPlayDate;
        
        if (firebase.scores != null)
            player.scores = new List<PlayerScore>(firebase.scores);
        
        return player;
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS ADICIONALES
    // ============================================
    
    public void ForceFirebaseSync()
    {
        if (useFirebase && currentPlayer != null)
        {
            SyncPlayerToFirebase(currentPlayer);
        }
    }
    
    public void LoadFirebaseRanking()
    {
        if (useFirebase && firebaseSync != null)
        {
            firebaseSync.LoadTopPlayers(20);
        }
    }
    
    public bool IsFirebaseEnabled()
    {
        return useFirebase && firebaseSync != null;
    }
    
    // ============================================
    // EVENTOS DE APLICACIÓN
    // ============================================
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveRankingData();
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveRankingData();
    }
    
    void OnDestroy()
    {
        SaveRankingData();
        
        if (firebaseSync != null)
        {
            firebaseSync.OnPlayerSaved -= OnFirebasePlayerSaved;
            firebaseSync.OnTopPlayersLoaded -= OnFirebaseTopPlayersLoaded;
            firebaseSync.OnError -= OnFirebaseError;
        }
    }
    
    // ============================================
    // DEBUG EN EDITOR
    // ============================================
    
    #if UNITY_EDITOR
    [ContextMenu("Force Sync Current Player")]
    void DebugSyncCurrentPlayer()
    {
        if (currentPlayer != null)
        {
            SyncPlayerToFirebase(currentPlayer);
        }
        else
        {
            Debug.LogWarning("No current player to sync");
        }
    }
    
    [ContextMenu("Load Firebase Ranking")]
    void DebugLoadFirebaseRanking()
    {
        LoadFirebaseRanking();
    }
    
    [ContextMenu("Clear PlayerPrefs")]
    void DebugClearPlayerPrefs()
    {
        PlayerPrefs.DeleteKey("RankingData");
        PlayerPrefs.DeleteKey("CurrentPlayerId");
        PlayerPrefs.Save();
        Debug.Log("🗑️ PlayerPrefs cleared");
    }
    [ContextMenu("Debug Player Cache")]
void DebugPlayerCache()
{
    Debug.Log("=== PLAYER CACHE DEBUG ===");
    Debug.Log($"Total players: {rankingData.players.Count}");
    
    for (int i = 0; i < rankingData.players.Count; i++)
    {
        var player = rankingData.players[i];
        Debug.Log($"{i+1}. {player.name} ({player.email}) - Best: {player.bestScore}");
    }
    
    Debug.Log($"Current player: {(currentPlayer != null ? currentPlayer.name : "None")}");
}

[ContextMenu("Clear All Cache")]
void DebugClearCache()
{
    rankingData.players.Clear();
    currentPlayer = null;
    PlayerPrefs.DeleteKey("CurrentPlayerId");
    SaveRankingData();
    Debug.Log("🗑️ All cache cleared");
}
    #endif
}