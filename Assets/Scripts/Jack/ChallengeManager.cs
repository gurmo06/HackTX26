using System;
using UnityEngine;

namespace JackGame
{
    public class ChallengeManager : MonoBehaviour
    {
        public string ActiveId { get; private set; } = "";
        public int Attempts { get; private set; }
        public bool Solved { get; private set; }
        public static readonly string[] Ids = { "echo", "towel", "piano" };
        public static string Question(string id) => id=="echo" ? "I repeat your words but have no mouth. What am I?" : id=="towel" ? "What gets wetter as it dries you?" : "What has keys but opens no locks?";
        public bool Begin(string id) { if (Array.IndexOf(Ids,id)<0 || ActiveId!="" || Solved) return false; ActiveId=id; Attempts=0; return true; }
        public bool Answer(string text)
        {
            if (ActiveId=="") return false;
            Attempts++;
            var words = text.ToLowerInvariant().Split(new[]{' ','.',',','!','?',';',':','\'','"'},StringSplitOptions.RemoveEmptyEntries);
            var answer = ActiveId=="echo" ? new[]{"echo","echoes"} : ActiveId=="towel" ? new[]{"towel","towels"} : new[]{"piano","keyboard"};
            foreach(var word in words) if(Array.IndexOf(answer,word)>=0) { Solved=true; ActiveId=""; return true; }
            // Three genuine attempts unlock a merciful pass; the model never judges factual correctness.
            if(Attempts>=3) { Solved=true; ActiveId=""; return true; }
            return false;
        }
        public void ResetChallenge() { ActiveId=""; Attempts=0; Solved=false; }
    }
}
