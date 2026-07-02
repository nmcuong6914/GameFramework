using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "PopupConfig", menuName = "UI/Popup Configuration")]
public class PopupConfig : ScriptableObject
{
    [System.Serializable]
    public class PopupEntry
    {
        public PopupType key;
        public AssetReference assetReference;
    }
    
    [Header("Popup Entries")]
    [SerializeField] private List<PopupEntry> popupEntries = new List<PopupEntry>();
    
    [Header("Global Animation Settings")]
    [Tooltip("Duration for popup fade-in animation")]
    [SerializeField] private float fadeInDuration = 0.25f;
    
    [Tooltip("Duration for popup fade-out animation")]
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    [Tooltip("Delay between each animated element")]
    [SerializeField] private float elementAnimationDelay = 0.1f;
    
    [Tooltip("Duration for each element animation")]
    [SerializeField] private float elementAnimationDuration = 0.3f;
    
    [Tooltip("Starting scale for animated elements (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float elementStartScale = 0.5f;
    
    private Dictionary<PopupType, PopupEntry> popupLookup;
    
    // Public properties for animation settings
    public float FadeInDuration => fadeInDuration;
    public float FadeOutDuration => fadeOutDuration;
    public float ElementAnimationDelay => elementAnimationDelay;
    public float ElementAnimationDuration => elementAnimationDuration;
    public float ElementStartScale => elementStartScale;
    
    void OnEnable()
    {
        BuildLookup();
    }
    
    void OnValidate()
    {
        BuildLookup();
    }
    
    private void BuildLookup()
    {
        popupLookup = popupEntries?.ToDictionary(entry => entry.key, entry => entry) ?? new Dictionary<PopupType, PopupEntry>();
    }
    
    public bool TryGetPopupEntry(PopupType key, out PopupEntry entry)
    {
        if (popupLookup == null) BuildLookup();
        return popupLookup.TryGetValue(key, out entry);
    }
    
    public AssetReference GetPopupAssetReference(PopupType key)
    {
        if (TryGetPopupEntry(key, out var entry))
            return entry.assetReference;
        return null;
    }
    
    public IEnumerable<PopupType> GetAllPopupTypes()
    {
        if (popupLookup == null) BuildLookup();
        return popupLookup.Keys;
    }
}
