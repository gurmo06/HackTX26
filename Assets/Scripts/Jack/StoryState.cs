using System;
using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    public enum StoryCutscene { Prologue, BeginClimb, CloudKingdom, TreasureRoom, Awakening, Ending }

    // Only deterministic story sequences and validated inventory grants write milestones.
    // Neither narration nor a model's 'advance' action can write these flags.
    public class StoryState : MonoBehaviour
    {
        readonly HashSet<string> milestones = new HashSet<string>();
        public static readonly string[] Order = { "received_beans", "planted_beans", "beanstalk_grown", "route_secured", "gap_crossed", "castle_entered", "goose_taken", "giant_awake", "escaped", "home" };
        public bool Has(string milestone) => milestones.Contains(milestone);
        public string[] Snapshot() => Array.FindAll(Order, Has);
        public void ResetStory() => milestones.Clear();
        public bool CanEnter(int beat) => beat>=0 && beat<5 && Has(new[]{"beanstalk_grown","route_secured","gap_crossed","castle_entered","giant_awake"}[beat]);
        public void ReceiveBeans(Inventory inventory)
        {
            if(Has("received_beans")) return;
            inventory.Remove("cow"); inventory.Grant("beans"); milestones.Add("received_beans");
        }
        public void PlantBeans(Inventory inventory)
        {
            ReceiveBeans(inventory);
            inventory.Remove("beans"); milestones.Add("planted_beans");
        }
        public void GrowBeanstalk(Inventory inventory) { PlantBeans(inventory); milestones.Add("beanstalk_grown"); }
        public bool TakeGoose()
        {
            if(!Has("castle_entered")) return false;
            milestones.Add("goose_taken"); return true;
        }
        public void Complete(StoryCutscene scene, Inventory inventory)
        {
            switch(scene) {
                case StoryCutscene.Prologue: GrowBeanstalk(inventory); break;
                case StoryCutscene.BeginClimb: Require("beanstalk_grown"); milestones.Add("route_secured"); break;
                case StoryCutscene.CloudKingdom: Require("route_secured"); milestones.Add("gap_crossed"); break;
                case StoryCutscene.TreasureRoom: Require("gap_crossed"); milestones.Add("castle_entered"); break;
                case StoryCutscene.Awakening: Require("goose_taken"); milestones.Add("giant_awake"); break;
                case StoryCutscene.Ending: Require("giant_awake"); if(!inventory.Has("goose")) throw new InvalidOperationException("Jack must bring the goose home."); milestones.Add("escaped"); milestones.Add("home"); break;
            }
        }
        void Require(string milestone) { if(!Has(milestone)) throw new InvalidOperationException("Missing story milestone: "+milestone); }
    }
}
