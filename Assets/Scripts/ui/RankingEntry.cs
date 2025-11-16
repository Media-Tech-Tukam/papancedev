using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ============================================
// RANKING ENTRY - Componente para entradas del ranking
// ============================================
public class RankingEntry : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gamesText;
    public TextMeshProUGUI distanceText;
    public Image backgroundImage;
    public Image rankIcon; // Para mostrar iconos especiales (corona, medallas, etc.)
    
    [Header("Visual Settings")]
    public Color normalBackgroundColor = new Color(1f, 1f, 1f, 0.1f);
    public Color currentPlayerColor = new Color(1f, 1f, 0f, 0.3f);
    public Color goldColor = new Color(1f, 0.84f, 0f); // #FFD700
    public Color silverColor = new Color(0.75f, 0.75f, 0.75f); // #C0C0C0
    public Color bronzeColor = new Color(0.8f, 0.5f, 0.2f); // #CD7F32
    
    [Header("Animation")]
    public float animationDelay = 0f;
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Icons")]
    public Sprite crownIcon; // Para el #1
    public Sprite medalIcon; // Para #2 y #3
    public Sprite playerIcon; // Para el jugador actual
    
    private PlayerData playerData;
    private int rank;
    private bool isCurrentPlayer;
    
    void Start()
    {
        // Configurar animación de entrada con delay
        if (animationDelay > 0f)
        {
            StartCoroutine(DelayedEntryAnimation());
        }
        else
        {
            PlayEntryAnimation();
        }
    }
    
    public void SetupEntry(PlayerData player, int playerRank, bool isCurrent = false)
    {
        playerData = player;
        rank = playerRank;
        isCurrentPlayer = isCurrent;
        
        UpdateDisplay();
        SetupVisuals();
    }
    
    void UpdateDisplay()
    {
        if (playerData == null) return;
        
        // Configurar textos
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            
            // Aplicar colores especiales para top 3
            switch (rank)
            {
                case 1:
                    rankText.color = goldColor;
                    break;
                case 2:
                    rankText.color = silverColor;
                    break;
                case 3:
                    rankText.color = bronzeColor;
                    break;
                default:
                    rankText.color = Color.white;
                    break;
            }
        }
        
        if (nameText != null)
        {
            nameText.text = playerData.name;
            
            // Resaltar nombre del jugador actual
            if (isCurrentPlayer)
            {
                nameText.color = Color.yellow;
                nameText.fontStyle = FontStyles.Bold;
            }
            else
            {
                nameText.color = Color.white;
                nameText.fontStyle = FontStyles.Normal;
            }
        }
        
        if (scoreText != null)
        {
            scoreText.text = FormatScore(playerData.bestScore);
        }
        
        if (gamesText != null)
        {
            gamesText.text = $"{playerData.totalGames}";
        }
        
        if (distanceText != null)
        {
            distanceText.text = $"{playerData.bestDistance:F0}m";
        }
    }
    
    void SetupVisuals()
    {
        // Configurar fondo
        if (backgroundImage != null)
        {
            backgroundImage.color = isCurrentPlayer ? currentPlayerColor : normalBackgroundColor;
        }
        
        // Configurar icono especial
        if (rankIcon != null)
        {
            switch (rank)
            {
                case 1:
                    if (crownIcon != null)
                    {
                        rankIcon.sprite = crownIcon;
                        rankIcon.color = goldColor;
                        rankIcon.gameObject.SetActive(true);
                    }
                    break;
                    
                case 2:
                case 3:
                    if (medalIcon != null)
                    {
                        rankIcon.sprite = medalIcon;
                        rankIcon.color = rank == 2 ? silverColor : bronzeColor;
                        rankIcon.gameObject.SetActive(true);
                    }
                    break;
                    
                default:
                    if (isCurrentPlayer && playerIcon != null)
                    {
                        rankIcon.sprite = playerIcon;
                        rankIcon.color = Color.yellow;
                        rankIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        rankIcon.gameObject.SetActive(false);
                    }
                    break;
            }
        }
    }
    
    string FormatScore(int score)
    {
        // Formatear puntuación con separadores de miles
        if (score >= 1000000)
        {
            return $"{score / 1000000f:F1}M";
        }
        else if (score >= 1000)
        {
            return $"{score / 1000f:F1}K";
        }
        else
        {
            return score.ToString();
        }
    }
    
    IEnumerator DelayedEntryAnimation()
    {
        // Configurar estado inicial (invisible)
        SetAlpha(0f);
        transform.localScale = Vector3.zero;
        
        // Esperar el delay
        yield return new WaitForSecondsRealtime(animationDelay);
        
        // Ejecutar animación
        PlayEntryAnimation();
    }
    
    void PlayEntryAnimation()
    {
        StartCoroutine(EntryAnimationCoroutine());
    }
    
    IEnumerator EntryAnimationCoroutine()
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;
        Vector3 startScale = Vector3.zero;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = animationCurve.Evaluate(elapsed / animationDuration);
            
            // Animar escala
            transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            // Animar alpha
            SetAlpha(progress);
            
            yield return null;
        }
        
        // Asegurar valores finales
        transform.localScale = targetScale;
        SetAlpha(1f);
    }
    
    void SetAlpha(float alpha)
    {
        // Aplicar alpha a todos los componentes gráficos
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = alpha * (isCurrentPlayer ? 0.3f : 0.1f);
            backgroundImage.color = bgColor;
        }
        
        if (rankText != null)
        {
            Color color = rankText.color;
            color.a = alpha;
            rankText.color = color;
        }
        
        if (nameText != null)
        {
            Color color = nameText.color;
            color.a = alpha;
            nameText.color = color;
        }
        
        if (scoreText != null)
        {
            Color color = scoreText.color;
            color.a = alpha;
            scoreText.color = color;
        }
        
        if (gamesText != null)
        {
            Color color = gamesText.color;
            color.a = alpha;
            gamesText.color = color;
        }
        
        if (distanceText != null)
        {
            Color color = distanceText.color;
            color.a = alpha;
            distanceText.color = color;
        }
        
        if (rankIcon != null)
        {
            Color color = rankIcon.color;
            color.a = alpha;
            rankIcon.color = color;
        }
    }
    
    // Método para actualizar la entrada sin recrearla
    public void UpdateEntry(PlayerData player, int playerRank, bool isCurrent = false)
    {
        playerData = player;
        rank = playerRank;
        isCurrentPlayer = isCurrent;
        
        UpdateDisplay();
        SetupVisuals();
    }
    
    // Animación de highlight para cuando el jugador sube de posición
    public void PlayHighlightAnimation()
    {
        StartCoroutine(HighlightAnimationCoroutine());
    }
    
    IEnumerator HighlightAnimationCoroutine()
    {
        Color originalColor = backgroundImage != null ? backgroundImage.color : Color.white;
        Color highlightColor = Color.green;
        highlightColor.a = 0.5f;
        
        // Pulso de color verde
        for (int i = 0; i < 3; i++)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlightColor;
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
            
            if (backgroundImage != null)
            {
                backgroundImage.color = originalColor;
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
    
    // Método para configurar delay de animación (útil para crear efectos escalonados)
    public void SetAnimationDelay(float delay)
    {
        animationDelay = delay;
    }
    
    // Método para obtener información del jugador
    public PlayerData GetPlayerData()
    {
        return playerData;
    }
    
    public int GetRank()
    {
        return rank;
    }
    
    public bool IsCurrentPlayer()
    {
        return isCurrentPlayer;
    }
    
    // Método para efectos de hover (opcional)
    public void OnPointerEnter()
    {
        if (!isCurrentPlayer && backgroundImage != null)
        {
            Color hoverColor = normalBackgroundColor;
            hoverColor.a = 0.2f;
            backgroundImage.color = hoverColor;
        }
    }
    
    public void OnPointerExit()
    {
        if (!isCurrentPlayer && backgroundImage != null)
        {
            backgroundImage.color = normalBackgroundColor;
        }
    }
}