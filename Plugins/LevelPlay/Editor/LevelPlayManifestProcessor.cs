#if UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Unity.Services.LevelPlay.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class LevelPlayManifestProcessor : IPreprocessBuildWithReport
{
    const string META_APPLICATION_ID = "com.google.android.gms.ads.APPLICATION_ID";
    const string AD_ID_PERMISSION_ATTR = "com.google.android.gms.permission.AD_ID";
    const string MANIFEST_PERMISSION = "uses-permission";
    const string MANIFEST_META_DATA = "meta-data";
    string m_AndroidManifestPath { get; set; } = EnvironmentVariables.androidManifestPath;
    XNamespace ns = "http://schemas.android.com/apk/res/android";

    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (File.Exists(LevelPlayMediationNetworkSettings.MEDIATION_SETTINGS_ASSET_PATH) || File.Exists(LevelPlayMediationSettings.s_LevelPlaySettingsAssetPath))
        {
            XElement elemManifest = ValidateAndroidManifest();

            XElement elemApplication = elemManifest.Element("application");

            if (File.Exists(LevelPlayMediationNetworkSettings.MEDIATION_SETTINGS_ASSET_PATH))
            {
                string appId = LevelPlayMediationNetworkSettingsInspector.LevelPlayMediationNetworkSettings.AdmobAndroidAppId;

                IEnumerable<XElement> metas = elemApplication.Descendants()
                    .Where(elem => elem.Name.LocalName.Equals(MANIFEST_META_DATA));

                if (LevelPlayMediationNetworkSettingsInspector.LevelPlayMediationNetworkSettings.EnableAdmob)
                {
                    XElement elemAdMobEnabled = GetMetaElement(metas, META_APPLICATION_ID);

                    if (appId.Length == 0)
                    {
                        StopBuildWithMessage(
                            "Android AdMob app ID is empty. Please enter your app ID to run ads properly");
                    }
                    else if (!Regex.IsMatch(appId, "^[a-zA-Z0-9-~]*$"))
                    {
                        StopBuildWithMessage(
                            "Android AdMob app ID is not valid. Please enter a valid app ID to run ads properly");
                    }
                    else if (elemAdMobEnabled == null)
                    {
                        elemApplication.Add(CreateMetaElement(META_APPLICATION_ID, appId));
                    }
                    else
                    {
                        elemAdMobEnabled.SetAttributeValue(ns + "value", appId);
                    }
                }
                else if (GetPermissionElement(metas, META_APPLICATION_ID) != null)
                {
                    //remove admob app id in case flag is off
                    GetPermissionElement(metas, META_APPLICATION_ID).Remove();
                }
            }

            if (File.Exists(LevelPlayMediationSettings.s_LevelPlaySettingsAssetPath))
            {
                IEnumerable<XElement> permissons = elemManifest.Descendants().Where(elem => elem.Name.LocalName.Equals(MANIFEST_PERMISSION));

                if (LevelPlayMediationSettingsInspector.LevelPlayMediationSettings.DeclareAD_IDPermission && GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR) == null)
                {
                    elemManifest.Add(CreatePermissionElement(AD_ID_PERMISSION_ATTR));
                }
                else if (GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR) != null && !LevelPlayMediationSettingsInspector.LevelPlayMediationSettings.DeclareAD_IDPermission)
                {
                    //remove the permission if flag is false
                    GetPermissionElement(permissons, AD_ID_PERMISSION_ATTR).Remove();
                }
            }

            if (m_AndroidManifestPath == null)
            {
                m_AndroidManifestPath = BuildManifest();
            }

            elemManifest.Save(m_AndroidManifestPath);
        }
    }

    XElement CreateMetaElement(string name, object value)
    {
        return new XElement(MANIFEST_META_DATA,
            new XAttribute(ns + "name", name), new XAttribute(ns + "value", value));
    }

    XElement CreatePermissionElement(string name)
    {
        return new XElement(MANIFEST_PERMISSION,
            new XAttribute(ns + "name", name));
    }

    XElement GetMetaElement(IEnumerable<XElement> metas, string metaName)
    {
        foreach (XElement elem in metas)
        {
            IEnumerable<XAttribute> attrs = elem.Attributes();
            foreach (XAttribute attr in attrs)
            {
                if (attr.Name.Namespace.Equals(ns)
                    && attr.Name.LocalName.Equals("name") && attr.Value.Equals(metaName))
                {
                    return elem;
                }
            }
        }
        return null;
    }

    XElement GetPermissionElement(IEnumerable<XElement> manifest, string permissionName)
    {
        foreach (XElement elem in manifest)
        {
            IEnumerable<XAttribute> attrs = elem.Attributes();
            foreach (XAttribute attr in attrs)
            {
                if (attr.Name.Namespace.Equals(ns)
                    && attr.Name.LocalName.Equals("name") && attr.Value.Equals(permissionName))
                {
                    return elem;
                }
            }
        }
        return null;
    }

    void StopBuildWithMessage(string message)
    {
        string prefix = "[IronSourceApplicationSettings] ";

        EditorUtility.DisplayDialog(
            "IronSource Developer Settings", "Error: " + message, "", "");
        throw new System.OperationCanceledException(prefix + message);
    }

    XElement ValidateAndroidManifest()
    {
        XDocument manifest = null;
        try
        {
            if (m_AndroidManifestPath == null)
            {
                m_AndroidManifestPath = BuildManifest();
            }

            manifest = XDocument.Load(m_AndroidManifestPath);
        }
        catch (IOException e)
        {
            StopBuildWithMessage("AndroidManifest.xml is missing. Try re-importing the plugin." + e.Message);
        }

        XElement elemManifest = manifest.Element("manifest");
        if (elemManifest == null)
        {
            StopBuildWithMessage("AndroidManifest.xml is not valid. Try re-importing the plugin.");
        }

        XElement elemApplication = elemManifest.Element("application");
        if (elemApplication == null)
        {
            StopBuildWithMessage("AndroidManifest.xml is not valid. Try re-importing the plugin.");
        }

        return elemManifest;
    }

    // Fallback method for path

    string BuildManifest()
    {
        const string k_AndroidLibPath = "Runtime/Plugins/Android/AndroidManifest.xml";
        var k_UpmManifestPath = Path.Combine("Packages/com.unity.services.levelplay", k_AndroidLibPath);
        var k_DotUnityPackageManifestPath = Path.Combine("LevelPlay", k_AndroidLibPath);
        var k_path = Path.GetFullPath(k_UpmManifestPath);

        return File.Exists(k_path) ? k_path : Path.Combine(Application.dataPath, k_DotUnityPackageManifestPath);
    }
}
#endif
