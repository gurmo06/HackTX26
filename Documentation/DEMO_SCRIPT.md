# Judging walkthrough

Use mock mode for a repeatable presentation. Live Gemini composes supported actions creatively, so its wording and number of turns can vary. The status line distinguishes live, offline, and outage fallback.

Five showcase inputs:

1. Merchant: **“Narrator, give me some beans, please.”** A labeled cardboard jellybean prop appears; it is not inventory magic beans. Follow with **“Trade my cow for real magic beans.”** The cow and stall beans disappear, the bean inventory icon lights, and Next chapter appears.
2. Beanstalk growth: **“Grow a sturdy, climbable beanstalk all the way to the clouds.”** The plant visibly enlarges. Next chapter changes the objective while retaining the same illustrated background.
3. Broken climb: **“Narrator, give me a ladder to reach the broken beanstalk!”** The ladder appears, visibly toppled, and does not satisfy the goal.
4. Same scene: **“Brace that ladder, stand it upright, and make it safe to climb.”** It stands upright; Next chapter triggers Jack's climb sprite and ascent.
5. Castle: **“Flatter the door's hinges until it lets me through.”** The procedural door unlocks. Continue to the night scene, then say **“Take the golden goose and quietly open our escape gate.”** The treasure icon lights; click Escape! for victory.

Optional: ask **“Give me a riddle.”** Answer **“echo”**, or try three answers to receive a merciful pass and a helpful world effect. Riddle validation uses a fixed local answer bank.

During the demo, point out A/D movement, click/E interaction, input focus stopping movement, disabled Submit while thinking, and Restart resetting the whole tale. In live mode try **“Build a cardboard platform and tie it to the beanstalk.”** A model may compose spawn/modify/connect if that satisfies the engine constraints. Unsupported ideas get an honest rejection or labeled generic prop, never executable model-generated code.
