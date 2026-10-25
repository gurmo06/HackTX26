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
            new LevelDefinition { id="merchant", environment=0, objective="1 / 5 · Negotiate for magic beans", introduction="Once upon a time, Jack decided to trade his cow. A terrible business decision, if you ask me.", actors=new[] {
                new ActorDefinition("stall","stall","market_stall","Market stall",3.8f,-0.2f,2.4f),
                new ActorDefinition("merchant","npc","merchant","Merchant · click or E",2.3f,-0.6f,2.2f),
                new ActorDefinition("cow","cow","cow","Your bargaining chip",-4.4f,-0.6f,1.7f),
                new ActorDefinition("beans","beans","BeanIcon","Magic beans",4.1f,0.5f,0.65f) } },
            new LevelDefinition { id="grow", environment=1, objective="2 / 5 · Grow a sturdy, climbable beanstalk", introduction="The beans are planted. Now convince this botanical underachiever to grow.", actors=new[] {
                new ActorDefinition("beanstalk","beanstalk","beanstalk_segment","Planted beanstalk",1,-0.5f,1.5f),
                new ActorDefinition("cloud","platform","cloud_platform","Cloud landing",3,2.5f,0.7f) } },
            new LevelDefinition { id="climb", environment=1, objective="3 / 5 · Bridge the gap with a stable climbing aid", introduction="Splendid growth. Shame about the missing middle. Ask for a ladder, rope, or your own absurd solution.", actors=new[] {
                new ActorDefinition("beanstalk","beanstalk","beanstalk_segment","Broken beanstalk",1,0,3),
                new ActorDefinition("cloud","platform","cloud_platform","Cloud landing",3,2.5f,0.7f) } },
            new LevelDefinition { id="castle", environment=2, objective="4 / 5 · Unlock the door or build a way around it", introduction="A giant door. A giant ego. Surely one of them can be persuaded to open.", actors=new[] {
                new ActorDefinition("giant","giant","giant","Giant · click or E",4.3f,0,3.6f),
                new ActorDefinition("door","door","","Castle door · locked",1.3f,-0.2f,2.6f),
                new ActorDefinition("castle","castle","cloud_castle","Cloud castle",-4,1,1.8f) } },
            new LevelDefinition { id="escape", environment=3, objective="5 / 5 · Take the goose and open an escape route", introduction="One golden goose, one sleeping giant, and absolutely no sensible decisions. Bring the goose home.", actors=new[] {
                new ActorDefinition("giant","giant","giant","Sleeping giant",4.3f,0,3.4f),
                new ActorDefinition("goose","goose","golden_goose","Golden goose · click or E",1,-0.6f,1.3f),
                new ActorDefinition("egg","egg","golden_egg","Golden egg",2,-1,0.6f),
                new ActorDefinition("exit","door","","Escape gate · locked",-4,-0.1f,2.4f) } }
        };
    }
}
