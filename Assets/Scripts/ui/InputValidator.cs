using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

// ============================================
// INPUT VALIDATOR - Validador de campos de entrada
// ============================================
public class InputValidator : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TMP_InputField phoneInput;
    
    [Header("Validation Settings")]
    public int minNameLength = 2;
    public int maxNameLength = 30;
    public int minPhoneLength = 7;
    public int maxPhoneLength = 15;
    
    [Header("Visual Feedback")]
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    public Color neutralColor = Color.white;
    
    // Eventos de validación
    public System.Action<bool> OnValidationChanged;
    
    // Estado de validación
    private bool isNameValid = false;
    private bool isEmailValid = false;
    private bool isPhoneValid = false;
    
    void Start()
    {
        SetupValidation();
    }
    
    void SetupValidation()
    {
        // Configurar listeners para validación en tiempo real
        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(ValidateName);
            nameInput.onEndEdit.AddListener(ValidateName);
        }
        
        if (emailInput != null)
        {
            emailInput.onValueChanged.AddListener(ValidateEmail);
            emailInput.onEndEdit.AddListener(ValidateEmail);
        }
        
        if (phoneInput != null)
        {
            phoneInput.onValueChanged.AddListener(ValidatePhone);
            phoneInput.onEndEdit.AddListener(ValidatePhone);
        }
        
        // Validación inicial
        UpdateValidationStatus();
    }
    
    void ValidateName(string name)
    {
        isNameValid = IsValidName(name);
        UpdateFieldColor(nameInput, isNameValid);
        UpdateValidationStatus();
    }
    
    void ValidateEmail(string email)
    {
        isEmailValid = IsValidEmail(email);
        UpdateFieldColor(emailInput, isEmailValid);
        UpdateValidationStatus();
    }
    
    void ValidatePhone(string phone)
    {
        isPhoneValid = IsValidPhone(phone);
        UpdateFieldColor(phoneInput, isPhoneValid);
        UpdateValidationStatus();
    }
    
    bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        name = name.Trim();
        
        // Verificar longitud
        if (name.Length < minNameLength || name.Length > maxNameLength)
            return false;
        
        // Verificar que solo contenga letras, espacios y algunos caracteres especiales
        return Regex.IsMatch(name, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-\.]+$");
    }
    
    bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        email = email.Trim();
        
        // Patrón básico de email
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        
        return Regex.IsMatch(email, pattern) && email.Length <= 254; // RFC 5321 limit
    }
    
    bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;
        
        // Limpiar el teléfono (remover espacios, guiones, paréntesis)
        string cleanPhone = Regex.Replace(phone, @"[\s\-\(\)\.]", "");
        
        // Permitir + al inicio para código de país
        if (cleanPhone.StartsWith("+"))
            cleanPhone = cleanPhone.Substring(1);
        
        // Verificar longitud y que solo contenga números
        return cleanPhone.Length >= minPhoneLength && 
               cleanPhone.Length <= maxPhoneLength && 
               Regex.IsMatch(cleanPhone, @"^\d+$");
    }
    
    void UpdateFieldColor(TMP_InputField field, bool isValid)
    {
        if (field == null) return;
        
        Color targetColor;
        
        if (string.IsNullOrWhiteSpace(field.text))
        {
            targetColor = neutralColor;
        }
        else
        {
            targetColor = isValid ? validColor : invalidColor;
        }
        
        // Aplicar color al texto
        field.textComponent.color = targetColor;
        
        // Opcional: cambiar color del borde/placeholder
        if (field.placeholder != null)
        {
            field.placeholder.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f);
        }
    }
    
    void UpdateValidationStatus()
    {
        bool allValid = IsAllFieldsValid();
        OnValidationChanged?.Invoke(allValid);
    }
    
    public bool IsAllFieldsValid()
    {
        // Verificar si hay contenido en los campos
        bool hasName = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text);
        bool hasEmail = emailInput != null && !string.IsNullOrWhiteSpace(emailInput.text);
        bool hasPhone = phoneInput != null && !string.IsNullOrWhiteSpace(phoneInput.text);
        
        // Todos los campos deben tener contenido Y ser válidos
        return hasName && hasEmail && hasPhone && isNameValid && isEmailValid && isPhoneValid;
    }
    
    public bool IsNameValid()
    {
        return isNameValid;
    }
    
    public bool IsEmailValid()
    {
        return isEmailValid;
    }
    
    public bool IsPhoneValid()
    {
        return isPhoneValid;
    }
    
    // Métodos para obtener los valores limpios
    public string GetCleanName()
    {
        return nameInput?.text?.Trim() ?? "";
    }
    
    public string GetCleanEmail()
    {
        return emailInput?.text?.Trim().ToLower() ?? "";
    }
    
    public string GetCleanPhone()
    {
        if (phoneInput == null || string.IsNullOrWhiteSpace(phoneInput.text))
            return "";
        
        // Limpiar pero mantener el formato básico
        string phone = phoneInput.text.Trim();
        
        // Si no empieza con +, agregar +57 por defecto (Colombia)
        if (!phone.StartsWith("+"))
        {
            // Remover caracteres especiales para verificar si es un número local
            string cleanPhone = Regex.Replace(phone, @"[\s\-\(\)\.]", "");
            
            if (cleanPhone.Length == 10 && cleanPhone.StartsWith("3"))
            {
                // Número móvil colombiano típico
                phone = "+57" + cleanPhone;
            }
            else if (cleanPhone.Length >= 7 && cleanPhone.Length <= 10)
            {
                // Otro número, agregar código de país por defecto
                phone = "+57" + cleanPhone;
            }
        }
        
        return phone;
    }
    
    // Método para limpiar todos los campos
    public void ClearFields()
    {
        if (nameInput != null)
        {
            nameInput.text = "";
            nameInput.textComponent.color = neutralColor;
        }
        
        if (emailInput != null)
        {
            emailInput.text = "";
            emailInput.textComponent.color = neutralColor;
        }
        
        if (phoneInput != null)
        {
            phoneInput.text = "";
            phoneInput.textComponent.color = neutralColor;
        }
        
        isNameValid = false;
        isEmailValid = false;
        isPhoneValid = false;
        
        UpdateValidationStatus();
    }
    
    // Método para establecer valores (útil para editar perfil)
    public void SetValues(string name, string email, string phone)
    {
        if (nameInput != null)
        {
            nameInput.text = name;
            ValidateName(name);
        }
        
        if (emailInput != null)
        {
            emailInput.text = email;
            ValidateEmail(email);
        }
        
        if (phoneInput != null)
        {
            phoneInput.text = phone;
            ValidatePhone(phone);
        }
    }
    
    // Método para mostrar mensajes de error específicos
    public string GetValidationMessage()
    {
        if (!isNameValid && !string.IsNullOrWhiteSpace(nameInput?.text))
        {
            return $"Name must be {minNameLength}-{maxNameLength} characters and contain only letters";
        }
        
        if (!isEmailValid && !string.IsNullOrWhiteSpace(emailInput?.text))
        {
            return "Please enter a valid email address";
        }
        
        if (!isPhoneValid && !string.IsNullOrWhiteSpace(phoneInput?.text))
        {
            return $"Phone must be {minPhoneLength}-{maxPhoneLength} digits";
        }
        
        if (!IsAllFieldsValid())
        {
            return "Please fill all fields correctly";
        }
        
        return ""; // Todo válido
    }
    
    // Método para enfocar el primer campo inválido
    public void FocusFirstInvalidField()
    {
        if (nameInput != null && !isNameValid)
        {
            nameInput.Select();
            nameInput.ActivateInputField();
        }
        else if (emailInput != null && !isEmailValid)
        {
            emailInput.Select();
            emailInput.ActivateInputField();
        }
        else if (phoneInput != null && !isPhoneValid)
        {
            phoneInput.Select();
            phoneInput.ActivateInputField();
        }
    }
    
    // Animación visual para campos inválidos
    public void HighlightInvalidFields()
    {
        if (!isNameValid && nameInput != null)
        {
            StartCoroutine(ShakeField(nameInput.transform));
        }
        
        if (!isEmailValid && emailInput != null)
        {
            StartCoroutine(ShakeField(emailInput.transform));
        }
        
        if (!isPhoneValid && phoneInput != null)
        {
            StartCoroutine(ShakeField(phoneInput.transform));
        }
    }
    
    System.Collections.IEnumerator ShakeField(Transform fieldTransform)
    {
        Vector3 originalPosition = fieldTransform.localPosition;
        float shakeAmount = 5f;
        float shakeDuration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            float x = originalPosition.x + Random.Range(-shakeAmount, shakeAmount);
            fieldTransform.localPosition = new Vector3(x, originalPosition.y, originalPosition.z);
            
            yield return null;
        }
        
        fieldTransform.localPosition = originalPosition;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Test Validation")]
    void TestValidation()
    {
        Debug.Log($"Name Valid: {isNameValid}");
        Debug.Log($"Email Valid: {isEmailValid}");
        Debug.Log($"Phone Valid: {isPhoneValid}");
        Debug.Log($"All Valid: {IsAllFieldsValid()}");
        Debug.Log($"Validation Message: {GetValidationMessage()}");
    }
    
    [ContextMenu("Fill Test Data")]
    void FillTestData()
    {
        SetValues("Juan Pérez", "juan.perez@email.com", "+573001234567");
    }
    
    [ContextMenu("Fill Invalid Data")]
    void FillInvalidData()
    {
        SetValues("A", "invalid-email", "123");
    }
    #endif
}