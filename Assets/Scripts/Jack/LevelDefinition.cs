namespace JackGame
{
    public sealed class EnvironmentDefinition
    {
        public readonly string id, background;
        public EnvironmentDefinition(string id, string background) { this.id = id; this.background = background; }
    }
    public sealed class ActorDefinition
    {
        public string id, archetype, sprite, label;
        public float x, y, height;
        public ActorDefinition(string id, string archetype, string sprite, string label, float x, float y, float height)
        { this.id = id; this.archetype = archetype; this.sprite = sprite; this.label = label; this.x = x; this.y = y; this.height = height; }
    }
    public sealed class LevelDefinition
    {
        public string id, objective, introduction;
        public int environment;
        public ActorDefinition[] actors;
        public static readonly EnvironmentDefinition[] Environments = {
            new EnvironmentDefinition("village", "01_Merchant_Village_Post_Beanstalk"),
            new EnvironmentDefinition("beanstalk", "02_Beanstalk_Climbing"),
            new EnvironmentDefinition("castle", "03_Giant_Castle"),
            new EnvironmentDefinition("night", "04_NightEscape")
        };
        public static readonly LevelDefinition[] Beats = {
            new LevelDefinition { id="sabotaged", environment=0, objective="1 / 5 · The Sabotaged Beanstalk — secure a climbing route", introduction="The beanstalk has grown. I have made it somewhat less attached to the ground. Ask for a safe way up.", actors=new[] {
                new ActorDefinition("beanstalk","beanstalk","beanstalk_segment","Unstable beanstalk",1,0,3.2f),
                new ActorDefinition("cottage","castle","cottage","Home",-4,.1f,2),
                new ActorDefinition("stone","stone","stone_block","Anchor stone",3,-.5f,.7f) } },
            new LevelDefinition { id="climb", environment=1, objective="2 / 5 · The Impossible Climb — bridge the missing section", introduction="Halfway to the clouds, Jack discovered my architectural innovation: a gap. Build a stable way across.", actors=new[] {
                new ActorDefinition("beanstalk","beanstalk","beanstalk_segment","Broken beanstalk",-1,0,3),
                new ActorDefinition("cloud","platform","cloud_platform","Far side of the gap",3,1.6f,.7f) } },
            new LevelDefinition { id="castle", environment=2, objective="3 / 5 · The Castle Entrance — break the magical seal", introduction="The giant's entrance is magically sealed. Compliments, clever contraptions, and loopholes are all acceptable forms of entry.", actors=new[] {
                new ActorDefinition("door","door","","Sealed castle door",1.3f,-.2f,2.6f),
                new ActorDefinition("castle","castle","cloud_castle","The giant's castle",-4,1,1.8f),
                new ActorDefinition("crate","crate","crate","Abandoned crate",4,-.5f,.9f) } },
            new LevelDefinition { id="treasure", environment=2, objective="4 / 5 · The Golden Goose — silence its ward and take it quietly", introduction="The giant sleeps beside the golden goose. Silence the goose's alarm ward before taking it. I suggest avoiding a brass band.", actors=new[] {
                new ActorDefinition("giant","giant","giant","Sleeping giant",4.4f,.2f,3.4f),
                new ActorDefinition("goose","goose","golden_goose","Goose · alarm ward",.5f,-.5f,1.2f),
                new ActorDefinition("treasure","egg","golden_egg_sack","Giant's treasure",-2,-.5f,1),
                new ActorDefinition("crate","crate","crate","Soft landing crate",-4,-.5f,.8f) } },
            new LevelDefinition { id="escape", environment=3, objective="5 / 5 · The Great Escape — open the route home", introduction="Goose secured. Giant furious. The final gate is stuck. Open it, or build a stable crossing before he reaches us.", actors=new[] {
                new ActorDefinition("giant","giant","giant","The pursuing giant",5,.1f,3.4f),
                new ActorDefinition("exit","door","","Blocked route home",-4,-.1f,2.4f),
                new ActorDefinition("stone","stone","stone_block","Fallen stone",1,-.5f,.8f) } }
        };
    }
}
