using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    public class Inventory : MonoBehaviour
    {
        readonly HashSet<string> items = new HashSet<string>();
        public bool Has(string item) => items.Contains(item);
        public bool Grant(string item) => items.Add(item);
        public void Remove(string item) => items.Remove(item);
        public string[] Snapshot() { var result = new List<string>(items); result.Sort(); return result.ToArray(); }
        public void ResetItems() { items.Clear(); items.Add("cow"); }
    }
}
