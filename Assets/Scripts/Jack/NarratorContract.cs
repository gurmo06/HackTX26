using System;

namespace JackGame
{
    [Serializable] public class WorldSnapshot
    {
        public string id = "", archetype = "", state = "";
        public float x, y, size = 1, stability = 1;
        public bool climbable, locked, visible = true;
    }
    [Serializable] public class HistoryEntry
    {
        public string role = "", text = "";
        public HistoryEntry(string role, string text) { this.role = role; this.text = text; }
    }
    [Serializable] public class NarratorRequest
    {
        public string sessionId = "", levelId = "", playerText = "", activeChallengeId = "";
        public int turnNumber, consecutiveSetbacks;
        public bool challengeSolved;
        public string[] inventory = Array.Empty<string>();
        public string[] storyMilestones = Array.Empty<string>();
        public WorldSnapshot[] worldEntities = Array.Empty<WorldSnapshot>();
        public HistoryEntry[] recentHistory = Array.Empty<HistoryEntry>();
    }
    [Serializable] public class GameAction
    {
        public string type = "", targetId = "", archetype = "", property = "", value = "", placement = "";
        public float numberValue;
        public static GameAction Make(string type, string target = "", string archetype = "", string property = "", string value = "", float number = 0, string placement = "")
        { return new GameAction { type = type, targetId = target, archetype = archetype, property = property, value = value, numberValue = number, placement = placement }; }
    }
    [Serializable] public class NarratorResponse
    {
        public string narration = "", mood = "neutral", twist = "none", challengeId = "", explanation = "";
        public GameAction[] actions = Array.Empty<GameAction>();
        public static NarratorResponse Say(string text, params GameAction[] actions)
        { return new NarratorResponse { narration = text, actions = actions, twist = Array.Exists(actions,a=>a!=null && a.type=="advance") ? "helpful" : "none" }; }
    }
}
