using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JackGame
{
    public static class NarratorMock
    {
        static GameAction A(string type,string id="",string arch="",string prop="",string value="",float number=0,string placement="") => GameAction.Make(type,id,arch,prop,value,number,placement);
        // Deliberately narrow: compound/creative requests remain the model's job.
        public const string BareTraversalPattern=@"^(?:narrator[,\s]+)?(?:please\s+)?(?:(?:let|allow|help)\s+me\s+|(?:can|may|could)\s+i\s+|i\s+(?:(?:want|try|attempt)\s+to\s+|will\s+)?|(?:try|attempt)\s+to\s+)?(?:climb|ascend|cross|use)(?:\s+(?:up|on|onto|across))?(?:\s+(?:(?:the|that|this|your|wobbly|slippery|unstable)\s+)*(?:it|ladder|rope|platform|bridge|beanstalk))?(?:\s+(?:now|anyway|please))?[.!?]*$";
        public static NarratorResponse UnsafeTraversal(NarratorRequest r)
        {
            if(r.activeChallengeId!="" || (r.levelId!="sabotaged" && r.levelId!="climb")) return null;
            var text=(r.playerText ?? "").Trim().ToLowerInvariant();
            if(!Regex.IsMatch(text,BareTraversalPattern)) return null;
            var named=new[]{"ladder","rope","platform","beanstalk"}.FirstOrDefault(word=>Regex.IsMatch(text,@"\b"+word+@"\b"));
            var target=r.worldEntities.LastOrDefault(e=>e.visible && (named!=null ? e.archetype==named : e.climbable || e.archetype=="beanstalk"));
            if(target==null || target.stability>=.7f) return null;
            var reply=NarratorResponse.Say("Jack tries to climb and slips back down. The route is still unsafe. Asking to climb it did not include a repair, Jack.",A("modify",target.id,prop:"stability",number:Math.Min(target.stability,.15f)));
            reply.twist="ironic_side_effect"; reply.explanation="unsafe_traversal"; return reply;
        }
        public static NarratorResponse Respond(NarratorRequest r,bool rescue=false)
        {
            var failedAttempt=UnsafeTraversal(r); if(failedAttempt!=null) return failedAttempt;
            var text=r.playerText.ToLowerInvariant();
            bool Has(params string[] words) => words.Any(text.Contains);
            WorldSnapshot Find(string id) => Array.Find(r.worldEntities,e=>e.id==id);
            if(r.activeChallengeId!="") return NarratorResponse.Say(ChallengeManager.Question(r.activeChallengeId)+" Try an answer; mercy arrives after three attempts.");
            if(!rescue && !r.challengeSolved && Has("riddle","trivia")) return NarratorResponse.Say("A toll payable in brain cells.",A("challenge",value:"echo"));
            switch(r.levelId) {
                case "sabotaged":
                case "climb":
                    var ladder=Find("ladder") ?? r.worldEntities.LastOrDefault(e=>e.archetype=="ladder" && e.visible);
                    if(ladder==null) { var created=NarratorResponse.Say(rescue ? "A sturdy ladder. I have reluctantly included the useful part." : "One ladder, delivered horizontally. Gravity insists this is a feature.",A("spawn","ladder","ladder",value:"Questionable ladder",number:1.7f,placement:"center"),A("modify","ladder",prop:"stability",number:rescue ? 1 : .15f));
                        if(rescue) { created.actions=created.actions.Concat(new[]{A("advance")}).ToArray(); created.twist="helpful"; }
                        return created;
                    }
                    bool needsRepair=ladder.stability<.7f || !ladder.climbable || ladder.size<1.5f || !ladder.visible;
                    if(needsRepair && !rescue && !Regex.IsMatch(text,@"\b(brace|stabili[sz]e|secure|anchor|bolt|tie|repair|fix|reinforce|steady|grip|safe|safely|sturdy|support|finish)\b"))
                        return NarratorResponse.Say("The ladder remains exactly as questionable as before. Repeating your destination is not a construction plan.");
                    var repairs=new List<GameAction>();
                    if(ladder.stability<.7f) repairs.Add(A("modify",ladder.id,prop:"stability",number:1));
                    if(!ladder.climbable) repairs.Add(A("modify",ladder.id,prop:"climbable",value:"true"));
                    if(ladder.size<1.5f) repairs.Add(A("modify",ladder.id,prop:"size",number:1.7f));
                    if(!ladder.visible) repairs.Add(A("modify",ladder.id,prop:"visible",value:"true"));
                    if(repairs.Count>2) return NarratorResponse.Say("Repairs are underway. The ladder is not ready yet.",repairs.Take(2).ToArray());
                    repairs.Add(A("advance"));
                    return NarratorResponse.Say("Braced and upright. You have defeated gravity with the radical concept of asking twice.",repairs.ToArray());
                case "castle": return NarratorResponse.Say("The magical seal yields to a compliment about its calligraphy. The door opens. Please wipe your feet.",A("modify","door",prop:"locked",value:"false"),A("advance"));
                case "treasure":
                    if(Find("goose")?.locked==true) return NarratorResponse.Say("I silence the goose's alarm ward. The giant continues snoring. We have used up our luck.",A("modify","goose",prop:"locked",value:"false"));
                    return NarratorResponse.Say("The golden goose is in your arms. You managed the quiet part. I make no promises about the goose.",A("grant_item",value:"goose"),A("advance"));
                case "escape": return NarratorResponse.Say("The gate opens. Run home with your extremely loud retirement plan, Jack!",A("modify","exit",prop:"locked",value:"false"),A("advance"));
                default: return NarratorResponse.Say("That page belongs to the storybook. Let the cutscene finish before asking for help.");
            }
        }
    }
}
