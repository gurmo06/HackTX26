using System;
using System.Linq;

namespace JackGame
{
    public static class NarratorMock
    {
        static GameAction A(string type,string id="",string arch="",string prop="",string value="",float number=0,string placement="") => GameAction.Make(type,id,arch,prop,value,number,placement);
        public static NarratorResponse Respond(NarratorRequest r, bool rescue=false)
        {
            var text=r.playerText.ToLowerInvariant();
            bool Has(params string[] words) => words.Any(text.Contains);
            WorldSnapshot Find(string id) => Array.Find(r.worldEntities,e=>e.id==id);
            if(r.activeChallengeId!="") return NarratorResponse.Say(ChallengeManager.Question(r.activeChallengeId)+" Try an answer; after three attempts, mercy prevails.");
            if(!rescue && Has("riddle","trivia")) return NarratorResponse.Say("A toll payable in brain cells.", A("challenge",value:"echo"));
            switch(r.levelId) {
                case "merchant":
                    if(rescue || Has("trade","cow","negotiate","barter","real","magic","stabil") || Find("jellybeans")!=null)
                        return NarratorResponse.Say("The merchant accepts your cow for magic beans. An economically catastrophic success, Jack.", A("grant_item",value:"beans"));
                    return NarratorResponse.Say("Beans, as requested. Jellybeans. Try offering the cow for the actual magic variety.", A("spawn","jellybeans","generic",value:"Jellybeans (not magic)",number:.6f,placement:"right"));
                case "grow": return NarratorResponse.Say("Up it grows! Your beans now violate several building regulations. Fortunately, they are sturdy enough to climb.", A("modify","beanstalk",prop:"size",number:2), A("modify","beanstalk",prop:"climbable",value:"true"), A("modify","beanstalk",prop:"stability",number:1));
                case "climb":
                    var ladder=Find("ladder");
                    if(ladder==null) return NarratorResponse.Say(rescue ? "A sturdy ladder. I have reluctantly included the useful part." : "One ladder, delivered horizontally. Gravity insists this is a feature. Ask me to brace it.", A("spawn","ladder","ladder",value:"Questionable ladder",number:1.7f,placement:"center"), A("modify","ladder",prop:"stability",number:rescue ? 1 : .15f));
                    return NarratorResponse.Say("Braced and upright. You have defeated gravity with the radical concept of asking twice.", A("modify","ladder",prop:"stability",number:1), A("modify","ladder",prop:"climbable",value:"true"), A("modify","ladder",prop:"size",number:1.7f));
                case "castle": return NarratorResponse.Say("The door opens after you compliment its hinges. Giants never remember to praise the staff.", A("modify","door",prop:"locked",value:"false"));
                case "escape": return NarratorResponse.Say("The goose joins your inventory, the gate opens, and the giant sleeps through the paperwork. Make your escape!", A("grant_item",value:"goose"), A("modify","exit",prop:"locked",value:"false"));
                default: return NarratorResponse.Say("The story has misplaced its page. Restart to find it.");
            }
        }
    }
}
