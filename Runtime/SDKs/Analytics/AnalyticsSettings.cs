using UnityEngine;

namespace Analytics
{
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "Analytics/Settings")]
    public class AnalyticsSettings : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private bool enableInEditor = false;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Service Settings")]
        [SerializeField] private bool enableAmplitude = true;
        [SerializeField] private bool enableFirebase = true;
        [SerializeField] private bool enableFacebook = true;
        
        [Header("Amplitude Settings")]
        [SerializeField] private string amplitudeApiKey = "";
        
        [Header("Firebase Settings")]
        [SerializeField] private bool firebaseAutoInit = true;
        
        private static AnalyticsSettings instance;
        
        public static AnalyticsSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AnalyticsSettings>("AnalyticsSettings");
                    if (instance == null)
                    {
                        Debug.LogWarning("AnalyticsSettings not found in Resources folder. Creating default settings.");
                        instance = CreateInstance<AnalyticsSettings>();
                    }
                }
                return instance;
            }
        }
        
        public bool EnableInEditor => enableInEditor;
        public bool EnableDebugLogs => enableDebugLogs;
        public bool EnableAmplitude => enableAmplitude;
        public bool EnableFirebase => enableFirebase;
        public bool EnableFacebook => enableFacebook;
        public string AmplitudeApiKey => amplitudeApiKey;
        public bool FirebaseAutoInit => firebaseAutoInit;
        
        [ContextMenu("Create Settings Asset")]
        private void CreateSettingsAsset()
        {
#if UNITY_EDITOR
            var resourcesPath = "Assets/Resources";
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            var assetPath = $"{resourcesPath}/AnalyticsSettings.asset";
            UnityEditor.AssetDatabase.CreateAsset(this, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"AnalyticsSettings created at {assetPath}");
#endif
        }
    }
}
