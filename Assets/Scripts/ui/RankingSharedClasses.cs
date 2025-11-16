using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// CLASES COMPARTIDAS PARA RANKING SYSTEM
// ============================================

[System.Serializable]
public class PlayerScore
{
    public int score;
    public float distance;
    public int coins;
    public int gems;
    public int powerCoins;
    public string gameOverReason;
    public string timestamp;
    public float playTime; // Duración de la partida en segundos
    
    public PlayerScore(int score, float distance, int coins, int gems, int powerCoins, string gameOverReason, float playTime)
    {
        this.score = score;
        this.distance = distance;
        this.coins = coins;
        this.gems = gems;
        this.powerCoins = powerCoins;
        this.gameOverReason = gameOverReason;
        this.playTime = playTime;
        this.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}

[System.Serializable]
public class PlayerData
{
    public string id;
    public string name;
    public string email;
    public string phone;
    public List<PlayerScore> scores;
    public int bestScore;
    public float bestDistance;
    public int totalGames;
    public int totalCoins;
    public int totalGems;
    public string firstPlayDate;
    public string lastPlayDate;
    
    public PlayerData(string name, string email, string phone)
    {
        this.id = System.Guid.NewGuid().ToString();
        this.name = name;
        this.email = email;
        this.phone = phone;
        this.scores = new List<PlayerScore>();
        this.bestScore = 0;
        this.bestDistance = 0f;
        this.totalGames = 0;
        this.totalCoins = 0;
        this.totalGems = 0;
        this.firstPlayDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        this.lastPlayDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}

[System.Serializable]
public class RankingData
{
    public List<PlayerData> players;
    public string lastUpdate;
    public int version;
    
    public RankingData()
    {
        players = new List<PlayerData>();
        lastUpdate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
        version = 1;
    }
}

[System.Serializable]
public class PlayerLoginInfo
{
    public string name;
    public int totalGames;
    public int bestScore;
    public string lastPlayDate;
}

// ============================================
// CLASE BASE RANKING MANAGER (ABSTRACTA)
// ============================================

public abstract class RankingManagerBase : MonoBehaviour
{
    [Header("Configuration")]
    public int maxScoresPerPlayer = 10;
    public int maxRankingEntries = 100;
    
    // Eventos
    public System.Action<PlayerData> OnPlayerRegistered;
    public System.Action<List<PlayerData>> OnRankingUpdated;
    public System.Action<int> OnPlayerRankChanged;
    
    // Variables protegidas
    protected RankingData rankingData;
    protected PlayerData currentPlayer;
    protected string filePath;
    protected bool isDataLoaded = false;
    
    // Singleton pattern
    public static RankingManagerBase Instance { get; protected set; }
    
    // Métodos abstractos que deben implementar las clases derivadas
    public abstract bool RegisterPlayer(string name, string email, string phone);
    public abstract void AddScore(int score, float distance, int coins, int gems, int powerCoins, string gameOverReason, float playTime);
    public abstract List<PlayerData> GetTopPlayers(int count = 10);
    public abstract int GetPlayerRank(string playerId);
    public abstract PlayerData GetPlayerById(string id);
    public abstract PlayerData GetPlayerByEmail(string email);
    public abstract bool IsEmailRegistered(string email);
    
    // Métodos comunes
    public virtual bool HasCurrentPlayer()
    {
        return currentPlayer != null;
    }
    
    public virtual PlayerData GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    public virtual int GetCurrentPlayerRank()
    {
        if (currentPlayer == null) return -1;
        return GetPlayerRank(currentPlayer.id);
    }
    
    public virtual int GetTotalPlayers()
    {
        return rankingData?.players?.Count ?? 0;
    }
    
    public virtual int GetTotalGames()
    {
        if (!isDataLoaded) return 0;
        int total = 0;
        foreach (var player in rankingData.players)
        {
            total += player.totalGames;
        }
        return total;
    }
    
    public virtual int GetHighestScore()
    {
        if (!isDataLoaded) return 0;
        int highest = 0;
        foreach (var player in rankingData.players)
        {
            if (player.bestScore > highest)
                highest = player.bestScore;
        }
        return highest;
    }
    
    public virtual PlayerData GetTopPlayer()
    {
        var topPlayers = GetTopPlayers(1);
        return topPlayers.Count > 0 ? topPlayers[0] : null;
    }
    
    // Métodos de utilidad
    protected virtual bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email && email.Contains("@") && email.Contains(".");
        }
        catch
        {
            return false;
        }
    }
    
    protected virtual bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        
        string cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        if (cleanPhone.StartsWith("+"))
            cleanPhone = cleanPhone.Substring(1);
        
        return cleanPhone.Length >= 7 && cleanPhone.Length <= 15 && System.Linq.Enumerable.All(cleanPhone, char.IsDigit);
    }
    
    protected virtual void SaveCurrentPlayer()
    {
        if (currentPlayer != null)
        {
            PlayerPrefs.SetString("CurrentPlayerId", currentPlayer.id);
            PlayerPrefs.Save();
        }
    }
    
    public virtual void LogoutCurrentPlayer()
    {
        if (currentPlayer != null)
        {
            Debug.Log($"👋 Player {currentPlayer.name} logged out.");
            currentPlayer = null;
        }
        
        PlayerPrefs.DeleteKey("CurrentPlayerId");
        PlayerPrefs.Save();
    }
}