using System;
using UnityEngine;
using TMPro;

namespace JackGame
{
    [Serializable] public class ArtEntry { public string key; public Sprite sprite; }
    public class ArtCatalog : ScriptableObject
    {
        public ArtEntry[] entries;
        public TMP_FontAsset bodyFont, headingFont;
        public Material worldMaterial;
        public Sprite Get(string key)
        {
            foreach (var entry in entries) if (entry.key == key) return entry.sprite;
            return null;
        }
    }
}
