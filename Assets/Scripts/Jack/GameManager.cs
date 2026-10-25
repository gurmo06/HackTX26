using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JackGame
{
    public class GameManager : MonoBehaviour
    {
        public LevelController levels;
        public NarratorClient client;
        public NarratorUI ui;
        public ActionExecutor executor;
        public Inventory inventory;
        public WorldRegistry world;
        public ChallengeManager challenges;
        public bool Busy { get; private set; }
        public bool Won { get; private set; }
        public int Turn { get; private set; }
        public int Setbacks { get; private set; }
        readonly List<HistoryEntry> history=new List<HistoryEntry>();
        string session;
        int generation;
        void Start() => Restart();
        public void Restart()
        {
            generation++; StopAllCoroutines(); client.Cancel(); Busy=false; Won=false; Turn=0; Setbacks=0;
            history.Clear(); session="demo-"+Guid.NewGuid().ToString("N"); inventory.ResetItems(); challenges.ResetChallenge(); levels.Load(0);
            ui.input.text=""; ui.lastRequest.text="A/D or arrows · click a character, or approach and press E";
            ui.Say(levels.Current.introduction); ui.Refresh(client.UseMockNarrator ? "Offline narrator · no key needed" : "Live narrator · offline fallback available");
        }
        public void Interact(WorldEntity entity)
        {
            if(Busy || Won) return;
            string prompt=entity.id=="merchant" ? "Narrator, help me negotiate a fair trade for the magic beans." : entity.id=="cow" ? "Narrator, what could I trade my cow for?" : "Narrator, help me use the "+entity.displayName+" to complete my goal.";
            ui.input.text=prompt;
            ui.Say("Ask about "+entity.displayName+". Edit the request below, then press Enter or Submit.");
            ui.input.ActivateInputField();
        }
        NarratorRequest Snapshot(string text) => new NarratorRequest { sessionId=session,levelId=levels.Current.id,playerText=text,turnNumber=Turn,consecutiveSetbacks=Setbacks,inventory=inventory.Snapshot(),worldEntities=world.Snapshot(),recentHistory=history.ToArray(),activeChallengeId=challenges.ActiveId,challengeSolved=challenges.Solved };
        void Remember(string role,string text) { history.Add(new HistoryEntry(role,text.Substring(0,Math.Min(350,text.Length)))); while(history.Count>6) history.RemoveAt(0); }
        public void Submit(string text)
        {
            if(Busy || Won || string.IsNullOrWhiteSpace(text)) return;
            text=text.Trim(); if(text.Length>400) text=text.Substring(0,400);
            if(Turn>=200) { ui.Say("This edition has reached its 200-turn limit. Restart for a fresh tale."); return; }
            Turn++; ui.lastRequest.text="You: "+text;
            if(challenges.ActiveId!="") {
                bool correct=challenges.Answer(text);
                Remember("player",text);
                if(!correct) { ui.Say(ChallengeManager.Question(challenges.ActiveId)+" Try again; mercy arrives after three attempts."); ui.Refresh("Riddle attempt "+challenges.Attempts+" / 3"); return; }
                var reward=NarratorMock.Respond(Snapshot(text),true); ApplyResponse(reward,"Riddle cleared · helpful effect unlocked"); return;
            }
            var request=Snapshot(text); Remember("player",text);
            Busy=true; ui.Refresh(); int revision=levels.Revision, stamp=generation;
            StartCoroutine(client.Request(request,(response,status)=> {
                if(stamp!=generation || revision!=levels.Revision) return;
                Busy=false; ApplyResponse(response,status);
            }));
        }
        void ApplyResponse(NarratorResponse response,string status)
        {
            string before=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            int rejected=0;
            if(response.actions!=null) foreach(var action in response.actions) if(!executor.Apply(action)) {
                // Setting an already-satisfied property is harmless, but suppress unverified claims conservatively.
                rejected++;
            }
            if(!string.IsNullOrEmpty(response.challengeId) && challenges.ActiveId=="") challenges.Begin(response.challengeId);
            string after=JsonUtility.ToJson(new NarratorRequest { inventory=inventory.Snapshot(),worldEntities=world.Snapshot() });
            Setbacks=before!=after ? 0 : Setbacks+1;
            string narration=rejected>0 ? "The stage crew refused part of that request. Only the changes you can see took effect. Try a different approach." : response.narration;
            if(challenges.ActiveId!="") narration+="\n"+ChallengeManager.Question(challenges.ActiveId);
            if(Setbacks>=3 && challenges.ActiveId=="" && !levels.GoalMet) {
                if(world.entities.Count>=WorldRegistry.MaxEntities) { var removable=world.entities.Find(e=>e && !e.essential && e.id!="ladder"); if(removable) world.Remove(removable); }
                var help=NarratorMock.Respond(Snapshot("Please help me progress"),true);
                int applied=0; foreach(var action in help.actions) if(executor.Apply(action)) applied++;
                if(applied>0) { narration=help.narration; Setbacks=0; status+=" · mercy rule applied"; }
            } else if(Setbacks>=2) narration+="\nHint: "+Hint();
            ui.Say(narration,response.mood); Remember("narrator",narration); ui.Refresh(status);
        }
        string Hint() { switch(levels.Current.id) { case "merchant": return "Offer the cow for real magic beans."; case "grow": return "Ask for a large, sturdy, climbable beanstalk."; case "climb": return "Ask for a ladder, then brace it."; case "castle": return "Ask to unlock the castle door."; default: return "Ask to take the goose and unlock the escape gate."; } }
        public void Next()
        {
            if(Busy || Won || !levels.GoalMet) return;
            if(levels.Beat==4) { Won=true; ui.Refresh("The end"); return; }
            StartCoroutine(Transition());
        }
        IEnumerator Transition()
        {
            Busy=true; ui.Refresh();
            if(levels.Current.id=="climb") { levels.jack.BeginAscent(); yield return new WaitForSecondsRealtime(1.2f); }
            levels.Load(levels.Beat+1); challenges.ResetChallenge(); Setbacks=0; Busy=false;
            ui.Say(levels.Current.introduction); ui.Refresh("New chapter · your satchel and story memory travel with you");
        }
    }
}
