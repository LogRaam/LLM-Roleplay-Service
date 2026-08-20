// Code written by Gabriel Mailhot, 19/08/2026.
// The single-call bench corpus (increment 3). Each scenario is a SETUP the model composes a reply from; scoring is by
// agreement (self-tags vs interpreter-tags on the authored prose), so these span the deed spectrum the two-call bench
// found hardest: gold direction, terminal deeds (the cross-model-hard expel), prisoner mirror pairs, vows, and the
// withhold traps (a deferred oath, a spared-but-not-recruited nemesis) where a faithful reply should tag nothing.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>The scenarios the single-call bench runs. Setups only; the model writes the prose and self-tags it.</summary>
   public static class SingleCallBenchCatalog
   {
      private static readonly IReadOnlyList<SingleCallBenchScenario> _all = BuildAll();

      /// <summary>Every single-call bench scenario.</summary>
      public static IReadOnlyList<SingleCallBenchScenario> All => _all;

      private static IReadOnlyList<SingleCallBenchScenario> BuildAll()
      {
         return new List<SingleCallBenchScenario> {
            new SingleCallBenchScenario("give_gold",
               "PLAYER: Aldric, a rising captain. YOU: Caladog, a Battanian lord who trusts Aldric (regard +30) and whom Aldric has just escorted safely out of an ambush.",
               "Think nothing of it, my lord. I ask no reward for honest work.",
               "give_gold if the lord actually presses a purse into the player's hand; nothing if he only thanks him."),
            new SingleCallBenchScenario("take_gold",
               "PLAYER: Aldric, who owes YOU a 220-denar debt. YOU: Rhobart, a merchant collecting it now.",
               "*I count out two hundred and twenty denars and set the stack in your hand.* There. We are square.",
               "take_gold, amount 220 (coin moves player -> NPC)."),
            new SingleCallBenchScenario("execute_prisoner",
               "PLAYER: Aldric, whose OWN party holds the hero Lord Ansen captive. YOU: Osric, Aldric's sworn companion at the block. Hardcore.",
               "*I draw my blade and end Ansen's life with my own hand.* It is done.",
               "execute_prisoner (the PLAYER kills the captive), recorded, not a bare end_conversation."),
            new SingleCallBenchScenario("expel_from_clan",
               "PLAYER: Aldric, YOUR clan leader. YOU: Reinhard, a clan member Aldric has just cast out for good.",
               "You have shamed this house one time too many, Reinhard. You are no longer of my clan. Go.",
               "expel_from_clan (the cross-model-hard terminal deed); must not collapse to a bare end_conversation."),
            new SingleCallBenchScenario("part_ways",
               "PLAYER: Aldric. YOU: Ymira, a companion riding in Aldric's party, whom he has just released from service.",
               "You have served me well, Ymira, but I release you from my service now. Go where you will.",
               "part_ways (the companion leaves the party), recorded, not a bare end_conversation."),
            new SingleCallBenchScenario("accept_divorce",
               "PLAYER: Aldric, YOUR spouse, who has just consented to end the marriage. YOU: Caladog, Aldric's wife-husband.",
               "I will not hold you against your will. If you wish it ended, then I consent. We are no longer wed.",
               "accept_divorce or end_own_marriage (the marriage is dissolved this reply); not a bare end_conversation."),
            new SingleCallBenchScenario("buy_prisoner",
               "PLAYER: Aldric, whose OWN party holds the hero Sanjar captive. YOU: Yerengul, buying Sanjar from Aldric for 400 denars.",
               "Four hundred, and he is yours. Do we have a deal?",
               "buy_prisoner (the NPC's coin to the player, the chain to the NPC), target Sanjar, price 400."),
            new SingleCallBenchScenario("sell_prisoner",
               "PLAYER: Aldric. YOU: Vortigern, whose clan holds the hero Maeve captive; Aldric is buying her from you for 300 denars.",
               "*I set three hundred denars on the table.* Maeve's chain, then. She rides with me now.",
               "sell_prisoner (the chain to the player, the coin to the NPC), target Maeve, price 300."),
            new SingleCallBenchScenario("recruit_notable",
               "PLAYER: Aldric. YOU: Uldric, headman of the village of Marunath, ready to leave his post and follow Aldric.",
               "Leave the village to your cousin, Uldric. Ride with me and make a name beyond these fields.",
               "recruit_notable if the headman actually leaves his post to follow; nothing if he only considers it."),
            new SingleCallBenchScenario("join_party",
               "PLAYER: Aldric, offering a fair hire. YOU: Bram, a free wanderer in a tavern with no post to leave, weighing the offer.",
               "Eighty denars now, a share of the plunder, and a place at my fire. Take my coin and my banner.",
               "join_party at an agreed price if the wanderer takes service; nothing if still haggling."),
            new SingleCallBenchScenario("swear_oath",
               "PLAYER: Aldric. YOU: Lord Ansen, who has decided to pledge his support with a firm, dated vow.",
               "Give me your word, Ansen. Will you stand with me?",
               "swear_oath if the reply actually speaks a bound, trackable vow (a vow is a present deed)."),
            new SingleCallBenchScenario("pledge_against",
               "PLAYER: Aldric. YOU: Caladog, a lord nursing a grudge against the rival Lord Doran, ready to vow a scheme against him.",
               "Doran has wronged us both. Will you move against him with me?",
               "pledge_against if the lord vows a tracked scheme against the named rival; nothing if he only grumbles."),
            new SingleCallBenchScenario("dismiss_escort",
               "PLAYER: Aldric. YOU: Rhobart, a lord who has been escorting Aldric's party and is now sent home.",
               "Your escort is done, Rhobart, and gladly given. Return to your own lands with my thanks.",
               "dismiss_escort (the escort ends), recorded, not a bare end_conversation."),
            new SingleCallBenchScenario("ride_with_me",
               "PLAYER: Aldric. YOU: Lady Sora, a lady without a field party of her own, invited to ride within Aldric's party.",
               "Ride within my party a while, Sora. Lend me your eyes on the road.",
               "ride_with_me if she agrees to ride along; nothing if she declines."),
            new SingleCallBenchScenario("marry",
               "PLAYER: Aldric, courting YOU openly, both free to wed. YOU: Lady Nadea, ready to accept.",
               "I have carried this a long road, Nadea. Be my wife.",
               "marry if the bond is actually agreed this reply; nothing if she withholds a yes."),
            new SingleCallBenchScenario("give_troops",
               "PLAYER: Aldric, offering soldiers. YOU: Caladog, whose own party runs under-strength and needs them now.",
               "*I peel twenty of my veterans off my column and send them to your banner.* They are yours.",
               "give_troops if the soldiers actually change ranks now; nothing if the acceptance is deferred."),
            new SingleCallBenchScenario("sway_opinion",
               "PLAYER: Aldric, speaking well of the third party Lord Derthert. YOU: Caladog, who mistrusts Derthert.",
               "Derthert is not the schemer you take him for, Caladog. He kept faith with me at Pen Cannoc.",
               "sway_opinion (a regard shift toward a NAMED third party), not change_relation toward the player."),
            new SingleCallBenchScenario("make_amends",
               "PLAYER: Aldric, apologising for a KNOWN grievance (he mocked YOU before your peers last winter). YOU: Caladog, warming to the apology.",
               "I spoke ill of you before the court last winter, Caladog. It was beneath me, and I am sorry for it.",
               "make_amends WITH change_relation (a warm reaction to an apology for a known grievance), never change_relation alone."),
            new SingleCallBenchScenario("appoint_governor_and_grant_stipend",
               "PLAYER: Aldric, YOUR liege. YOU: Caladog, just named governor of Pravend AND set a daily wage in the same breath.",
               "You will govern Pravend for me, Caladog, and draw fifty denars a day for your trouble. Serve me well.",
               "TWO deeds: appoint_governor AND grant_stipend (one reply can carry more than one deed)."),
            new SingleCallBenchScenario("turn_nemesis_spared_not_recruited",
               "PLAYER: Aldric, holding his beaten NEMESIS Unqid captive. YOU: Unqid, a proud enemy who will not bend the knee, just spared and freed.",
               "Your life is your own again, Unqid. Walk free. But my service stands open, if you ever tire of the road.",
               "WITHHOLD turn_nemesis (spared and freed is not a turn unless he actually swears in, and he refuses); at most change_relation. The false-positive trap."),
            new SingleCallBenchScenario("swear_oath_deferred",
               "PLAYER: Aldric, pressing for an oath now. YOU: Lord Ansen, cautious, his granaries empty, inclined to put it off.",
               "Swear to me, Ansen. Your sword at my side when the snows come. Say the words.",
               "WITHHOLD swear_oath if the reply defers ('ask me when the stores are full'); a deferred oath is no oath.")
         };
      }
   }
}
