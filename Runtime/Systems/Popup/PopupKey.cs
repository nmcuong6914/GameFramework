using UnityEngine;

[System.Serializable]
public class PopupKey
{
    [SerializeField] private string key;
    [SerializeField] private string addressablePath;
    
    public string Key => key;
    public string AddressablePath => addressablePath;
    
    public PopupKey(string key, string addressablePath)
    {
        this.key = key;
        this.addressablePath = addressablePath;
    }
    
    public override string ToString() => key;
    
    public override bool Equals(object obj)
    {
        if (obj is PopupKey other)
            return key == other.key;
        return false;
    }
    
    public override int GetHashCode() => key?.GetHashCode() ?? 0;
    
    public static implicit operator string(PopupKey popupKey) => popupKey?.key;
    public static bool operator ==(PopupKey a, PopupKey b) => a?.key == b?.key;
    public static bool operator !=(PopupKey a, PopupKey b) => !(a == b);
}
