using System;
using System.Linq;

namespace JackGame
{
    public static class NarratorMock
    {
        static GameAction A(string type,string id="",string arch="",string prop="",string value="",float number=0,string placement="") => GameAction.Make(type,id,arch,prop,value,number,placement);
        public static NarratorResponse Respond(NarratorRequest r,bool rescue=false)
        {
            var text=r.playerText.ToLowerInvariant();
            bool Has(params string[] words) => words.Any(text.Contains);
            WorldSnapshot Find(string id) => Array.Find(r.worldEntities,e=>e.id==id);
            if(r.activeChallengeId!="") return NarratorResponse.Say(ChallengeManager.Question(r.activeChallengeId)+" Try an answer; mercy arrives after three attempts.");
            if(!rescue && !r.challengeSolved && Has("riddle","trivia")) return NarratorResponse.Say("A toll payable in brain cells.",A("challenge",value:"echo"));
            switch(r.levelId) {
                case "sabotaged":
                case "climb":
                    var ladder=Find("ladder");
                    if(ladder==null) return NarratorResponse.Say(rescue ? "A sturdy ladder. I have reluctantly included the useful part." : "One ladder, delivered horizontally. Gravity insists this is a feature. Ask me to brace it.",A("spawn","ladder","ladder",value:"Questionable ladder",number:1.7f,placement:"center"),A("modify","ladder",prop:"stability",number:rescue ? 1 : .15f));
                    return NarratorResponse.Say("Braced and upright. You have defeated gravity with the radical concept of asking twice.",A("modify","ladder",prop:"stability",number:1),A("modify","ladder",prop:"climbable",value:"true"),A("modify","ladder",prop:"size",number:1.7f));
                case "castle": return NarratorResponse.Say("The magical seal yields to a compliment about its calligraphy. The door opens. Please wipe your feet.",A("modify","door",prop:"locked",value:"false"));
                case "treasure":
                    if(Find("goose")?.locked==true) return NarratorResponse.Say("I silence the goose's alarm ward. The giant continues snoring. Now take the goose quietly; we have used up our luck.",A("modify","goose",prop:"locked",value:"false"));
                    return NarratorResponse.Say("The golden goose is in your arms. You managed the quiet part. I make no promises about the goose.",A("grant_item",value:"goose"));
                case "escape": return NarratorResponse.Say("The gate opens. Run home with your extremely loud retirement plan, Jack!",A("modify","exit",prop:"locked",value:"false"));
                default: return NarratorResponse.Say("That page belongs to the storybook. Let the cutscene finish before asking for help.");
            }
        }
    }
}
