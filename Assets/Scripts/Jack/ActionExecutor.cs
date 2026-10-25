using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JackGame
{
    public class ActionExecutor : MonoBehaviour
    {
        public WorldRegistry world;
        public LevelController levels;
        public Inventory inventory;
        public ChallengeManager challenges;
        static bool ValidId(string id) => id != null && Regex.IsMatch(id,"^[a-z][a-z0-9_]{0,31}$");
        static string Label(string text) => string.IsNullOrWhiteSpace(text) ? "Cardboard curiosity" : text.Substring(0,Math.Min(48,text.Length)).Replace("<","").Replace(">","");
        public bool Apply(GameAction a)
        {
            if(levels.CinematicCamera || !levels.story || !levels.story.CanEnter(levels.Beat)) return false;
            if(a==null || string.IsNullOrEmpty(a.type) || float.IsNaN(a.numberValue) || float.IsInfinity(a.numberValue)) return false;
            // Story inventory cannot be conjured early, even as a transformed world prop.
            if((a.type=="spawn" || a.type=="transform") && (a.archetype=="beans" || a.archetype=="goose")) return false;
            var e=world.Find(a.targetId);
            switch(a.type) {
                case "spawn":
                    if(!ValidId(a.targetId) || e || Array.IndexOf(WorldRegistry.SpawnArchetypes,a.archetype)<0 || (a.placement!="near_player" && a.placement!="center" && a.placement!="right")) return false;
                    var pos=a.placement=="near_player" ? (Vector2)levels.jack.transform.position+Vector2.right*1.5f : new Vector2(a.placement=="right" ? 4 : 0,.2f);
                    pos.x=Mathf.Clamp(pos.x,-6,6); pos.y=Mathf.Clamp(pos.y,-1,2);
                    e=world.Spawn(a.targetId,a.archetype,Label(a.value),pos);
                    if(!e) return false;
                    e.size=Mathf.Clamp(a.numberValue==0 ? 1 : a.numberValue,.5f,2.5f);
                    e.climbable=a.archetype=="ladder" || a.archetype=="rope" || a.archetype=="platform";
                    e.Refresh(); return true;
                case "modify":
                    if(!e) return false;
                    switch(a.property) {
                        case "size": var size=Mathf.Clamp(a.numberValue,.5f,2.5f); if(Mathf.Approximately(e.size,size)) return true; e.size=size; break;
                        case "stability": var stable=Mathf.Clamp01(a.numberValue); if(Mathf.Approximately(e.stability,stable)) return true; e.stability=stable; break;
                        case "climbable": if(a.value!="true" && a.value!="false") return false; if(e.climbable==(a.value=="true")) return true; e.climbable=a.value=="true"; break;
                        case "locked": if((e.archetype!="door" && e.archetype!="goose") || (a.value!="true" && a.value!="false")) return false; e.locked=a.value=="true"; e.displayName=e.id=="exit" ? "Route home" : e.id=="goose" ? "Golden goose" : "Castle door"; break;
                        case "visible": if(e.essential || (a.value!="true" && a.value!="false")) return false; e.visible=a.value=="true"; break;
                        case "label": var label=Label(a.value); if(e.displayName==label) return true; e.displayName=label; break;
                        default: return false;
                    }
                    e.Refresh(); return true;
                case "transform":
                    if(!e || e.essential || Array.IndexOf(WorldRegistry.SpawnArchetypes,a.archetype)<0 ) return false;
                    // Replace owned generated objects; mandatory story objects retain their identity.
                    var position=e.transform.position; var id=e.id;
                    world.Remove(e); e=world.Spawn(id,a.archetype,Label(a.value),position);
                    if(!e) return false; e.climbable=a.archetype=="ladder" || a.archetype=="rope" || a.archetype=="platform"; e.Refresh(); return true;
                case "move":
                    if(!e || (a.property!="x" && a.property!="y")) return false;
                    var p=e.transform.position;
                    if(a.property=="x") p.x=Mathf.Clamp(a.numberValue,-6,6); else p.y=Mathf.Clamp(a.numberValue,-1,2.5f);
                    if(p==e.transform.position) return true; e.transform.position=p; return true;
                case "connect":
                    var other=world.Find(a.value);
                    if(!e || !other || e==other ) return false;
                    e.Connect(other,world.art.worldMaterial); e.stability=1; e.Refresh(); return true;
                case "destroy": return world.Remove(e);
                case "grant_item":
                    if(a.value=="beans") return false; // The prologue alone receives and plants beans.
                    if(a.value=="goose") {
                        var goose=world.Find("goose"); var giant=world.Find("giant");
                        if(levels.Current.id!="treasure" || !levels.story.Has("castle_entered") || !goose || goose.locked || !goose.visible || !giant || giant.state!="sleeping") return false;
                        if(!levels.story.TakeGoose()) return false;
                    }
                    if(a.value!="beans" && a.value!="goose" && a.value!="rope" && a.value!="key") return false;
                    if(!inventory.Grant(a.value)) return true;
                    if(a.value=="goose") Hide("goose");
                    return true;
                case "challenge": return challenges.Begin(a.value);
                case "resolve_challenge": return challenges.Solved; // Answers are checked locally before any model call.
                case "advance": return levels.GoalMet; // GameManager transitions after applying the full response.
                default: return false;
            }
        }
        void Hide(string id) { var e=world.Find(id); if(e) { e.visible=false; e.Refresh(); } }
    }
}
