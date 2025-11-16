using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// ============================================
// FIREBASE RANKING SYNC - Helper para WebGL
// ============================================

[System.Serializable]
public class FirebasePlayerData
{
    public string id;
    public string name;
    public string email;
    public string phone;
    public int bestScore;
    public float bestDistance;
    public int totalGames;
    public int totalCoins;
    public int totalGems;
    public string firstPlayDate;
    public string lastPlayDate;
    public List<PlayerScore> scores;
    
    public FirebasePlayerData()
    {
        scores = new List<PlayerScore>();
    }
}

public class FirebaseRankingSync : MonoBehaviour
{
    [Header("Firebase Configuration")]
    public string firebaseURL = "https://endless-runner-bo-default-rtdb.firebaseio.com";
    public string apiKey = "AIzaSyAVPqsueuYclg8-We-UXFni9R58eKN7NQ4";
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Eventos
    public System.Action<bool> OnConnectionTested;
    public System.Action<FirebasePlayerData> OnPlayerSaved;
    public System.Action<List<FirebasePlayerData>> OnTopPlayersLoaded;
    public System.Action<string> OnError;
    public System.Action OnFirebaseReady; // ✨ NUEVO: Notifica cuando Firebase está listo
    
    // Estado
    private bool isConnected = false;
    private bool isConnecting = false; // ✨ NUEVO: Evita múltiples intentos de conexión simultáneos
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("🔥 FirebaseRankingSync initialized");
            
        // ✨ NUEVO: Establecer conexión automáticamente al iniciar
        TestConnection();
    }
    
    // ============================================
    // MÉTODOS DE TESTING
    // ============================================
    
    [ContextMenu("Test Firebase Connection")]
    public void TestConnection()
    {
        if (isConnecting)
        {
            LogDebug("🔄 Already attempting to connect to Firebase...");
            return;
        }
        
        StartCoroutine(TestConnectionCoroutine());
    }
    
    IEnumerator TestConnectionCoroutine()
    {
        isConnecting = true; // ✨ NUEVO: Marcar que estamos conectando
        string url = $"{firebaseURL}/test.json";
        
        LogDebug("🔄 Attempting to connect to Firebase..."); // ✨ MEJORADO: Log más informativo
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                LogDebug("✅ Firebase connection successful!");
                OnConnectionTested?.Invoke(true);
                OnFirebaseReady?.Invoke(); // ✨ NUEVO: Notificar que Firebase está listo
            }
            else
            {
                isConnected = false;
                LogError($"❌ Firebase connection failed: {request.error}");
                OnConnectionTested?.Invoke(false);
                OnError?.Invoke(request.error);
            }
        }
        
        isConnecting = false; // ✨ NUEVO: Marcar que ya terminamos de conectar
    }
    
    // ============================================
    // MÉTODOS PRINCIPALES
    // ============================================
    
    public void SavePlayer(PlayerData playerData)
    {
        if (!isConnected)
        {
            LogError("❌ Cannot save player: Firebase not connected. Try again in a moment.");
            OnError?.Invoke("Firebase not connected");
            return;
        }
        
        StartCoroutine(SavePlayerCoroutine(playerData));
    }
    
    IEnumerator SavePlayerCoroutine(PlayerData playerData)
    {
        // Convertir PlayerData a FirebasePlayerData
        FirebasePlayerData firebaseData = ConvertToFirebaseData(playerData);
        
        // Serializar a JSON
        string jsonData = JsonUtility.ToJson(firebaseData);
        
        // URL del endpoint
        string url = $"{firebaseURL}/players/{playerData.id}.json";
        
        LogDebug($"🔄 Saving player to Firebase: {playerData.name}");
        LogDebug($"🔄 URL: {url}");
        LogDebug($"🔄 JSON: {jsonData}");
        
        using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug($"✅ Player saved successfully: {playerData.name}");
                LogDebug($"✅ Response: {request.downloadHandler.text}");
                OnPlayerSaved?.Invoke(firebaseData);
            }
            else
            {
                LogError($"❌ Error saving player: {request.error}");
                LogError($"❌ Response Code: {request.responseCode}");
                LogError($"❌ Response: {request.downloadHandler.text}");
                OnError?.Invoke(request.error);
            }
        }
    }
    
    public void LoadPlayerByEmail(string email)
    {
        if (!isConnected)
        {
            LogError("❌ Cannot load player: Firebase not connected. Try again in a moment.");
            OnError?.Invoke("Firebase not connected");
            return;
        }
        
        StartCoroutine(LoadPlayerByEmailCoroutine(email));
    }
    
    IEnumerator LoadPlayerByEmailCoroutine(string email)
    {
        // Consulta optimizada por email usando indexación
        string url = $"{firebaseURL}/players.json?orderBy=\"email\"&equalTo=\"{email}\"";
        
        LogDebug($"🔄 Loading player by email: {email}");
        LogDebug($"🔄 URL: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                LogDebug($"📥 Player search response: {response}");
                
                List<FirebasePlayerData> players = ParsePlayersResponse(response);
                
                if (players.Count > 0)
                {
                    LogDebug($"✅ Found player: {players[0].name}");
                    OnPlayerSaved?.Invoke(players[0]); // Reutilizamos este evento
                }
                else
                {
                    LogDebug("❌ No player found with that email");
                    OnError?.Invoke("Player not found");
                }
            }
            else
            {
                LogError($"❌ Error searching player: {request.error}");
                OnError?.Invoke(request.error);
            }
        }
    }
    
    public void LoadTopPlayers(int count = 10)
    {
        if (!isConnected)
        {
            LogError("❌ Cannot load top players: Firebase not connected. Try again in a moment.");
            OnError?.Invoke("Firebase not connected");
            return;
        }
        
        StartCoroutine(LoadTopPlayersCoroutine(count));
    }
    
    IEnumerator LoadTopPlayersCoroutine(int count)
    {
        // Ahora podemos usar consultas optimizadas con indexación
        string url = $"{firebaseURL}/players.json?orderBy=\"bestScore\"&limitToLast={count}";
        
        LogDebug($"🔄 Loading top {count} players from Firebase (optimized query)");
        LogDebug($"🔄 URL: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                LogDebug($"📥 Firebase response received, length: {response.Length}");
                LogDebug($"📥 Raw response: {response}");
                
                List<FirebasePlayerData> players = ParsePlayersResponse(response);
                
                // Ordenar descendente (Firebase devuelve ascendente)
                players.Sort((a, b) => b.bestScore.CompareTo(a.bestScore));
                
                LogDebug($"✅ Loaded {players.Count} players from Firebase");
                OnTopPlayersLoaded?.Invoke(players);
            }
            else
            {
                LogError($"❌ Error loading players: {request.error}");
                LogError($"❌ Response Code: {request.responseCode}");
                OnError?.Invoke(request.error);
            }
        }
    }
    
    // ============================================
    // MÉTODOS DE UTILIDAD
    // ============================================
    
    FirebasePlayerData ConvertToFirebaseData(PlayerData original)
    {
        FirebasePlayerData firebaseData = new FirebasePlayerData();
        firebaseData.id = original.id;
        firebaseData.name = original.name;
        firebaseData.email = original.email;
        firebaseData.phone = original.phone;
        firebaseData.bestScore = original.bestScore;
        firebaseData.bestDistance = original.bestDistance;
        firebaseData.totalGames = original.totalGames;
        firebaseData.totalCoins = original.totalCoins;
        firebaseData.totalGems = original.totalGems;
        firebaseData.firstPlayDate = original.firstPlayDate;
        firebaseData.lastPlayDate = original.lastPlayDate;
        
        // Copiar scores
        if (original.scores != null)
        {
            firebaseData.scores = new List<PlayerScore>(original.scores);
        }
        
        return firebaseData;
    }
    
    List<FirebasePlayerData> ParsePlayersResponse(string jsonResponse)
    {
        List<FirebasePlayerData> players = new List<FirebasePlayerData>();
        
        try
        {
            if (jsonResponse == "null" || string.IsNullOrEmpty(jsonResponse))
            {
                LogDebug("📊 No players found in Firebase");
                return players;
            }
            
            LogDebug("📊 Parsing Firebase response...");
            
            // Firebase devuelve: {"playerId1": {playerData}, "playerId2": {playerData}}
            // Necesitamos extraer cada playerData
            
            // Remover llaves externas
            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("{") && jsonResponse.EndsWith("}"))
            {
                jsonResponse = jsonResponse.Substring(1, jsonResponse.Length - 2);
            }
            
            // Si está vacío después de remover llaves, no hay datos
            if (string.IsNullOrEmpty(jsonResponse.Trim()))
            {
                LogDebug("📊 Empty response after parsing");
                return players;
            }
            
            // Split por jugadores usando método simple
            string[] playerEntries = SplitPlayerEntries(jsonResponse);
            
            LogDebug($"🔍 Found {playerEntries.Length} potential player entries");
            
            foreach (string entry in playerEntries)
            {
                if (string.IsNullOrEmpty(entry.Trim())) continue;
                
                string entryPreview = entry.Length > 50 ? entry.Substring(0, 50) + "..." : entry;
                LogDebug($"🔍 Processing entry: {entryPreview}");
                
                // Extraer solo el JSON del jugador (después de los dos puntos)
                int colonIndex = entry.IndexOf(":");
                if (colonIndex > 0 && colonIndex < entry.Length - 1)
                {
                    string playerJson = entry.Substring(colonIndex + 1).Trim();
                    
                    try
                    {
                        FirebasePlayerData player = JsonUtility.FromJson<FirebasePlayerData>(playerJson);
                        if (player != null && !string.IsNullOrEmpty(player.id))
                        {
                            players.Add(player);
                            LogDebug($"✅ Parsed player: {player.name} ({player.bestScore} pts)");
                        }
                        else
                        {
                            LogDebug("⚠️ Player data incomplete or invalid");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        LogError($"❌ Error parsing individual player: {parseEx.Message}");
                        LogDebug($"🔍 Problematic JSON: {playerJson}");
                    }
                }
                else
                {
                    LogDebug("⚠️ No colon found in entry");
                }
            }
            
            LogDebug($"📊 Successfully parsed {players.Count} players");
        }
        catch (Exception e)
        {
            LogError($"❌ Error parsing Firebase response: {e.Message}");
        }
        
        return players;
    }
    
    string[] SplitPlayerEntries(string jsonContent)
    {
        List<string> entries = new List<string>();
        
        int braceCount = 0;
        int startIndex = 0;
        bool inString = false;
        
        for (int i = 0; i < jsonContent.Length; i++)
        {
            char c = jsonContent[i];
            
            if (c == '"' && (i == 0 || jsonContent[i - 1] != '\\'))
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == '{')
                {
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                    
                    if (braceCount == 0)
                    {
                        // Found complete entry
                        string entry = jsonContent.Substring(startIndex, i - startIndex + 1);
                        entries.Add(entry);
                        
                        // Skip comma and whitespace
                        while (i + 1 < jsonContent.Length && 
                               (jsonContent[i + 1] == ',' || char.IsWhiteSpace(jsonContent[i + 1])))
                        {
                            i++;
                        }
                        
                        startIndex = i + 1;
                    }
                }
            }
        }
        
        return entries.ToArray();
    }
    
    // ============================================
    // DEBUG Y LOGGING
    // ============================================
    
    void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[FirebaseSync] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[FirebaseSync] {message}");
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS DE ESTADO
    // ============================================
    
    public bool IsConnected()
    {
        return isConnected;
    }
    
    public bool IsConnecting() // ✨ NUEVO: Permite saber si está en proceso de conexión
    {
        return isConnecting;
    }
    
    // ============================================
    // TESTING EN EDITOR
    // ============================================
    
    #if UNITY_EDITOR
    [ContextMenu("Test Save Dummy Player")]
    void TestSaveDummyPlayer()
    {
        LogDebug("🧪 Starting dummy player test...");
        
        PlayerData dummyPlayer = new PlayerData("Test Player Firebase", "test@firebase.com", "+573001234567");
        dummyPlayer.bestScore = 15000;
        dummyPlayer.totalGames = 5;
        dummyPlayer.totalCoins = 200;
        dummyPlayer.totalGems = 15;
        
        LogDebug($"🧪 Created dummy player: {dummyPlayer.name} with ID: {dummyPlayer.id}");
        
        SavePlayer(dummyPlayer);
    }
    
    [ContextMenu("Test Load Top Players")]
    void TestLoadTopPlayers()
    {
        LogDebug("🧪 Starting load top players test...");
        LoadTopPlayers(5);
    }
    
    [ContextMenu("Test Search Player by Email")]
    void TestSearchPlayerByEmail()
    {
        LogDebug("🧪 Testing search by email...");
        LoadPlayerByEmail("test@firebase.com");
    }
    #endif
}