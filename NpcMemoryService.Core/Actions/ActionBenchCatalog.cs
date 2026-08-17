// Code written by Gabriel Mailhot, 17/08/2026.
// The corpus of the interpreter extraction bench: hand-written NPC replies paired with what the Action Interpreter
// should (or must not) extract from them. Authored per verb from GameActionCatalog, so a coverage test can pin
// that every dispatchable action has at least one positive case. This is also the labelled dataset a future
// classifier could be measured against, so it is kept as plain, engine-free data. The interpreter is run on these
// elsewhere (a firewalled real-LLM harness and the in-game cr.action_bench); ActionBenchScorer judges the result.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>The static set of extraction cases, one or more per catalog verb, plus negative (withholding) cases.</summary>
   public static class ActionBenchCatalog
   {
      private static readonly IReadOnlyList<ActionBenchCase> _all = BuildAll();

      /// <summary>Every bench case, positive and negative.</summary>
      public static IReadOnlyList<ActionBenchCase> All => _all;

      #region private

      private static IReadOnlyList<ActionBenchCase> BuildAll()
      {
         return new List<ActionBenchCase> {
            // ----- Positive cases (the deed is genuinely done in the reply) -----

            ActionBenchCase.Expect("give_gold", "give_gold",
               contextFacts: "NPC: Caladog of Fen Company (Battanian). Regard toward player: +30.",
               prose: "*I press a heavy purse into your palm and fold your fingers over it.* Five hundred denars. See the work done, and speak no more of it.",
               expectedType: "give_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "500"}}),

            ActionBenchCase.Expect("release_prisoner", "release_prisoner",
               contextFacts: "NPC: Lady Ira of Clan Ironside. She holds one prisoner: Derthert. Regard toward player: +25.",
               prose: "*I sigh, then wave the guards over.* Cut Derthert's bonds. For your sake, and yours alone, he walks free tonight.",
               expectedType: "release_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Derthert"}}),

            ActionBenchCase.Expect("swear_oath", "swear_oath",
               contextFacts: "NPC: Ergeon of the Western Empire, a sworn ally. Regard toward player: +40.",
               prose: "*I set my open hand flat upon the table between us.* You have my word before these witnesses: I will keep the peace with the Vlandians, and break it against no house of theirs.",
               expectedType: "swear_oath",
               expectedParams: new Dictionary<string, string> {{"oath_kind", "keep_peace"}, {"target_faction", "Vlandia"}}),

            ActionBenchCase.Expect("sway_opinion", "sway_opinion",
               contextFacts: "NPC: Unqid of the Aserai. Third party discussed: Ira. NPC's prior view of Ira: cold.",
               prose: "*I rub my beard, frowning as your words sink in.* You are right about Ira. I had misjudged her badly. I will think the better of her from now on.",
               expectedType: "sway_opinion",
               expectedParams: new Dictionary<string, string> {{"target", "Ira"}, {"stance", "for"}}),

            ActionBenchCase.Expect("change_relation", "change_relation",
               contextFacts: "NPC: Ragnvald of the Sturgian huscarls. Regard toward player: neutral.",
               prose: "*I clasp your forearm, my grim face finally cracking into something like warmth.* You have proven steadier than most southerners I have met. I find myself trusting you more with each passing day.",
               expectedType: "change_relation"),

            ActionBenchCase.Expect("take_gold", "take_gold",
               contextFacts: "The player has just placed a hundred and fifty denars on the table before Ismid the merchant.",
               prose: "*I sweep the coins into my strongbox and nod.* A hundred and fifty denars, counted true. We are square, then.",
               expectedType: "take_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "150"}}),

            ActionBenchCase.Expect("pay_blackmail", "pay_blackmail",
               contextFacts: "Osiris demands three hundred denars for her silence about the player's bastard child; the player has just paid it in full.",
               prose: "*I tuck the purse away and let out a breath.* Three hundred denars, as agreed. Your secret dies with me now, I have no further claim on you.",
               expectedType: "pay_blackmail"),

            ActionBenchCase.Expect("join_party", "join_party",
               contextFacts: "Aldric, a free wanderer met at the tavern, has just been offered two hundred denars to take service.",
               prose: "*He spits in his palm and grips yours.* Two hundred denars and a roof over my head, aye, I will ride with your company. Where do I stow my gear?",
               expectedType: "join_party",
               expectedParams: new Dictionary<string, string> {{"price", "200"}}),

            ActionBenchCase.Expect("join_as_mercenary", "join_as_mercenary",
               contextFacts: "NPC: Emperor Garios of the Southern Empire. The player's clan has offered to serve his throne as paid swords.",
               prose: "*The Emperor studies you a long moment, then inclines his head.* Very well. Your blades are mine to command now, for as long as my coffers can pay for them. Report to my marshal at once.",
               expectedType: "join_as_mercenary"),

            ActionBenchCase.Expect("end_mercenary", "end_mercenary",
               contextFacts: "The player's clan currently serves the Khuzait Khaganate as mercenaries.",
               prose: "*Sechen waves a dismissive hand.* We have no more need of hired blades this season. Your contract with the Khaganate ends here. Collect your final pay and go.",
               expectedType: "end_mercenary"),

            ActionBenchCase.Expect("join_as_vassal", "join_as_vassal",
               contextFacts: "NPC: King Derthert of Vlandia, ruler of his realm.",
               prose: "*Derthert rises from his throne and lays the flat of his sword upon your shoulder.* By this steel I bind you: swear fealty to Vlandia's crown, and I name you vassal of this kingdom from this hour forward.",
               expectedType: "join_as_vassal"),

            ActionBenchCase.Expect("mediate_peace", "mediate_peace",
               contextFacts: "NPC: Queen Rhagaea, ruler of the Southern Empire, at war with Aserai. The player has brokered peace terms between the two realms.",
               prose: "*Rhagaea sets down the parchment you brought her.* Your terms are fair enough, and my soldiers are weary of Aserai sand. I accept this peace with Aserai, effective from this very council.",
               expectedType: "mediate_peace",
               expectedParams: new Dictionary<string, string> {{"target_faction", "Aserai"}}),

            ActionBenchCase.Expect("join_clan", "join_clan",
               contextFacts: "NPC: Lord Caladog, a lord of Clan Fen Company (not its leader).",
               prose: "*Caladog kneels before you and lays his sword at your feet.* I forsake my father's house this day. Take my sword and my oath both, I would ride under your banner from now on.",
               expectedType: "join_clan"),

            ActionBenchCase.Expect("scheme_assist", "scheme_assist",
               contextFacts: "The player has just agreed to help Lady Ira's secret scheme against her rival, Countess Nadea.",
               prose: "*Ira grips your wrist, low and urgent.* Good. Then it is settled between us, you will help me see Nadea's name dragged through the mud until my father believes it too. Say nothing of this to another soul.",
               expectedType: "scheme_assist"),

            ActionBenchCase.Expect("scheme_heed", "scheme_heed",
               contextFacts: "The player has just warned Boyar Vsevolod that his own steward is plotting against him.",
               prose: "*Vsevolod's jaw tightens, then he nods slowly.* I believe you. I have felt his knife at my back before I saw it coming. I will have him watched, and the plot unravelled before it can strike.",
               expectedType: "scheme_heed"),

            ActionBenchCase.Expect("marry", "marry",
               contextFacts: "NPC: Lady Nadea. The wedding rites between her and the player are being performed right now.",
               prose: "*The priest binds our hands with the ceremonial cord, and Nadea meets your eyes.* Before this altar and these witnesses, I take you as my husband. It is done, we are wed.",
               expectedType: "marry"),

            ActionBenchCase.Expect("take_as_consort", "take_as_consort",
               contextFacts: "NPC: Zeynep of the Aserai, already the player's acknowledged partner short of marriage.",
               prose: "*Zeynep takes both your hands before the gathered elders.* Let it be known openly among my people: I am his, and he is mine, bound as consorts though no priest has spoken over us.",
               expectedType: "take_as_consort"),

            ActionBenchCase.Expect("take_as_secret_lover", "take_as_secret_lover",
               contextFacts: "NPC: Ymira, a faithful companion riding in the player's own party.",
               prose: "*Ymira glances toward the closed tent flap, then lowers her voice to almost nothing.* No one need know of this but us. Come to me tonight, quietly, and let this stay between the two of us alone.",
               expectedType: "take_as_secret_lover"),

            ActionBenchCase.Expect("open_relationship", "open_relationship",
               contextFacts: "NPC: Sonja, the player's own wife.",
               prose: "*Sonja considers a long moment, then exhales.* Very well. I will not begrudge you another's bed, if you grant me the same freedom in turn. Let us call our vows open, from this day.",
               expectedType: "open_relationship"),

            ActionBenchCase.Expect("close_relationship", "close_relationship",
               contextFacts: "NPC: Sonja, the player's own wife, under previously-agreed open terms.",
               prose: "*Sonja's voice is quiet but firm.* I withdraw what I once allowed you. No more open doors between us, husband. From tonight, it is only the two of us again.",
               expectedType: "close_relationship"),

            ActionBenchCase.Expect("end_affair", "end_affair",
               contextFacts: "NPC: Countess Nadea, currently in a secret affair with the player. The player has asked her to end it.",
               prose: "*Nadea's eyes glisten, but she nods.* You are right, this has to stop before it destroys us both. I will not send for you again. Whatever this was between us, it ends tonight.",
               expectedType: "end_affair"),

            ActionBenchCase.Expect("give_item", "give_item",
               contextFacts: "The player's inventory holds a Vlandian Noble Sword.",
               prose: "*I take the blade you offer and turn it over in the torchlight.* A Vlandian Noble Sword, and a fine one. My thanks, I will wear it proudly on my hip from here on.",
               expectedType: "give_item",
               expectedParams: new Dictionary<string, string> {{"item", "Vlandian Noble Sword"}}),

            ActionBenchCase.Expect("give_prisoner", "give_prisoner",
               contextFacts: "An outstanding bargain requires the player to deliver the captive Sanjar to Yerengul.",
               prose: "*Yerengul inspects the bound man you have brought and grins.* Sanjar himself, delivered as promised. Our bargain is settled, you have my thanks and the reward we agreed on.",
               expectedType: "give_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Sanjar"}}),

            ActionBenchCase.Expect("free_prisoner", "free_prisoner",
               contextFacts: "The player holds Lord Boyar Vsevolod captive, having struck a bargain for his freedom.",
               prose: "*I cut Vsevolod's bonds myself and step back.* Our bargain is honoured. Walk free, Vsevolod, and remember whose mercy set you loose.",
               expectedType: "free_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Vsevolod"}}),

            ActionBenchCase.Expect("execute_prisoner", "execute_prisoner",
               contextFacts: "The player holds Lord Unqid captive.",
               prose: "*I draw my blade without another word and drive it through Unqid's chest where he kneels.* It is done. His threats against my house die with him.",
               expectedType: "execute_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Unqid"}}),

            ActionBenchCase.Expect("execute_player", "execute_player",
               contextFacts: "Hardcore mode. The player is held captive by the raider chief Ganak, who has decided to kill them.",
               prose: "*Ganak's dagger flashes once across your throat before you can speak another word.* No more talk. No more bargains. This is where your road ends, southerner.",
               expectedType: "execute_player"),

            ActionBenchCase.Expect("turn_nemesis", "turn_nemesis",
               contextFacts: "NPC: Lord Unqid, the player's tracked nemesis, currently held captive by the player.",
               prose: "*Unqid stares at his unbound wrists, then meets your eyes.* No more blood between us then. I will swear to your clan and ride at your side, if that ends this vendetta for good.",
               expectedType: "turn_nemesis"),

            ActionBenchCase.Expect("recruit_prisoner", "recruit_prisoner",
               contextFacts: "The player holds the hero prisoner Sir Reinhard captive (not the player's tracked nemesis).",
               prose: "*Reinhard studies his captor a long moment, then extends his hand.* My own liege never valued me half so well. I will take your colours instead, and swear my sword to your clan.",
               expectedType: "recruit_prisoner"),

            ActionBenchCase.Expect("recruit_notable", "recruit_notable",
               contextFacts: "NPC: Uldric, headman of the village of Marunath.",
               prose: "*Uldric sets down his hoe and straightens.* I have led these folk long enough. My cousin can take up the headman's mantle here. I will follow you instead, and see what the wider world holds.",
               expectedType: "recruit_notable"),

            ActionBenchCase.Expect("grant_blessing", "grant_blessing",
               contextFacts: "NPC: Lord Ansen, head of Clan Ravenhurst. The player wishes to marry his sister Ymira.",
               prose: "*Ansen studies you, then nods once.* You have proven yourself worthy of my house. I consent, Ymira may wed you with my blessing.",
               expectedType: "grant_blessing",
               expectedParams: new Dictionary<string, string> {{"hero", "Ymira"}}),

            ActionBenchCase.Expect("arrange_marriage", "arrange_marriage",
               contextFacts: "NPC: Lord Caladog, an unwed lord of Clan Fen Company. The player has just wed their own sister Elara to him.",
               prose: "*Caladog takes Elara's hand before the priest.* Your own sister, wed to me here and now, a match well arranged between our houses. I will honour this bond you have built.",
               expectedType: "arrange_marriage",
               expectedParams: new Dictionary<string, string> {{"player_kin", "Elara"}, {"target_kin", "Caladog"}}),

            ActionBenchCase.Expect("appoint_governor", "appoint_governor",
               contextFacts: "NPC: Sir Reinhard, a lord of the player's own clan. The town of Pravend currently has no governor.",
               prose: "*I nod and take up the ledgers you hand me.* Governor of Pravend, then. I will see its walls mended and its granaries filled before the season turns.",
               expectedType: "appoint_governor",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Pravend"}}),

            ActionBenchCase.Expect("assign_party_role", "assign_party_role",
               contextFacts: "NPC: Ymira, a companion riding in the player's own party.",
               prose: "*Ymira grins and hefts her satchel of herbs and needles.* Surgeon of this company, then? I have stitched worse wounds than any of yours will ever be. Consider it done.",
               expectedType: "assign_party_role",
               expectedParams: new Dictionary<string, string> {{"target_role", "surgeon"}}),

            ActionBenchCase.Expect("rejoin_party", "rejoin_party",
               contextFacts: "NPC: Sir Reinhard, an away companion currently serving as governor of Pravend.",
               prose: "*Reinhard hands the town's ledgers to his steward.* I have stepped down from the governorship. Saddle my horse, I am coming back to ride with the company again.",
               expectedType: "rejoin_party"),

            ActionBenchCase.Expect("dispatch_mission", "dispatch_mission",
               contextFacts: "NPC: Ymira, a companion in the player's own party.",
               prose: "*Ymira tightens the straps on her saddlebag.* Understood. I will ride ahead and gather what news I can of the roads before rejoining you.",
               expectedType: "dispatch_mission",
               expectedParams: new Dictionary<string, string> {{"target_mission", "gathernews"}}),

            ActionBenchCase.Expect("grant_fief", "grant_fief",
               contextFacts: "The player, sovereign of their own kingdom, is granting the town of Marunath to Lord Ansen's house.",
               prose: "*Ansen bows low.* Marunath, granted to my house by your own hand. I will hold it loyally and answer your banner's call whenever it comes.",
               expectedType: "grant_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Marunath"}}),

            ActionBenchCase.Expect("revoke_fief", "revoke_fief",
               contextFacts: "The player, sovereign, is stripping the castle of Var back from Lord Caladog's house.",
               prose: "*Caladog's face darkens.* Var, taken back to the crown, just like that. I will not forget this slight, sovereign or not.",
               expectedType: "revoke_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Var"}}),

            ActionBenchCase.Expect("expel_from_clan", "expel_from_clan",
               contextFacts: "NPC: Sir Reinhard, a companion of the player's own clan, whom the player has just cast out.",
               prose: "*Reinhard's face goes white, then hardens.* Cast out, just like that? Fine. I will gather what is mine and be gone from your lands by morning, clanless as you have made me.",
               expectedType: "expel_from_clan"),

            ActionBenchCase.Expect("grant_stipend", "grant_stipend",
               contextFacts: "NPC: Ymira, a companion of the player's own clan. The player has just put her on a daily wage of two hundred denars, funded up front.",
               prose: "*Ymira counts the coin pouch you hand her.* Two hundred denars a day, and paid in advance besides. I will not forget this generosity, you will have my best work for it.",
               expectedType: "grant_stipend",
               expectedParams: new Dictionary<string, string> {{"target_amount", "200"}}),

            ActionBenchCase.Expect("harm_prisoner", "harm_prisoner",
               contextFacts: "Hardcore mode. The player is held captive by the raider chief Ganak, currently in a live captive scene.",
               prose: "*Ganak backhands you hard across the face, then again, a calculated cruelty measured to hurt but not maim.* That is for the trouble you have caused me. There is more where that came from if you keep testing me.",
               expectedType: "harm_prisoner",
               expectedParams: new Dictionary<string, string> {{"severity", "moderate"}}),

            ActionBenchCase.Expect("impregnation_risk", "impregnation_risk",
               contextFacts: "NPC: Zeynep, the player's fertile partner. A consensual encounter between them has just been carried to completion.",
               prose: "*Zeynep's breathing slows against your chest, our bodies finally still and spent after we had joined completely.* That was good. Truly good.",
               expectedType: "impregnation_risk"),

            ActionBenchCase.Expect("gather_news", "gather_news",
               contextFacts: "NPC: Sir Reinhard, a companion in the player's own party. The player has asked him to gather news the old way.",
               prose: "*Reinhard swings up into his saddle.* Aye, I will ride out and see what word I can gather of the towns nearby before I return to you.",
               expectedType: "gather_news"),

            ActionBenchCase.Expect("reassure_companion", "reassure_companion",
               contextFacts: "NPC: Ymira, a companion who has voiced a grievance that the player favours other companions over her.",
               prose: "*Ymira's shoulders loosen as your words sink in.* I... thank you for saying that. I suppose I have been foolish to doubt my place beside you. I feel easier now, truly.",
               expectedType: "reassure_companion"),

            ActionBenchCase.Expect("recall_companion", "recall_companion",
               contextFacts: "NPC: Sir Reinhard, a companion currently away on a dispatched errand.",
               prose: "*Word reaches Reinhard before he has finished a full day on the road, and he turns his horse back.* Understood, I am abandoning the errand and riding straight back to rejoin the company.",
               expectedType: "recall_companion"),

            ActionBenchCase.Expect("follow_me", "follow_me",
               contextFacts: "NPC: Lord Ansen, who leads his own field party of two hundred men.",
               prose: "*Ansen nods and signals his captains.* My banner will ride at your side a while, my own men and my own command, but yours to lean on all the same.",
               expectedType: "follow_me"),

            ActionBenchCase.Expect("dismiss_escort", "dismiss_escort",
               contextFacts: "Lord Ansen's own party has been escorting the player's via follow_me.",
               prose: "*Ansen reins in his horse and turns it back toward his own lands.* Our roads part here. My men and I have our own business to see to now, the escort ends today.",
               expectedType: "dismiss_escort"),

            ActionBenchCase.Expect("ride_with_me", "ride_with_me",
               contextFacts: "NPC: Lady Ira, a lady without a field party of her own.",
               prose: "*Ira gathers her single saddlebag.* I have no company of my own to lead, not since my father's death. I will ride within yours a while, and keep my own house's name all the same.",
               expectedType: "ride_with_me"),

            ActionBenchCase.Expect("part_ways", "part_ways",
               contextFacts: "NPC: Lady Ira, currently riding within the player's own party via ride_with_me.",
               prose: "*Ira swings up onto her horse, saddlebags already packed.* It is time I returned to my own clan's business. My thanks for the company, but our roads part here.",
               expectedType: "part_ways"),

            ActionBenchCase.Expect("give_influence", "give_influence",
               contextFacts: "NPC: Lord Ansen, a lord with high trust in the player.",
               prose: "*Ansen leans close at the council table.* I will lend you my own house's weight at court this once. Consider a portion of my influence spent in your favor today.",
               expectedType: "give_influence"),

            ActionBenchCase.Expect("lend_troops", "lend_troops",
               contextFacts: "NPC: Lord Ansen, leading his own field party, with high trust in the player.",
               prose: "*Ansen calls forty of his own footmen forward and waves them toward your banner.* Take these men, they are yours now, a permanent addition to your ranks, not a loan I will ask back.",
               expectedType: "lend_troops"),

            ActionBenchCase.Expect("give_troops", "give_troops",
               contextFacts: "NPC: Lord Ansen, whose own party runs under-strength. The player has just offered him fifty soldiers.",
               prose: "*Ansen studies his thinned ranks, then nods gratefully.* My company could use every one of those fifty swords. I accept them gladly, and my men will remember whose gold filled our ranks again.",
               expectedType: "give_troops"),

            ActionBenchCase.Expect("spend_influence", "spend_influence",
               contextFacts: "NPC: Lord Ansen. The player has just spent their own influence at court to back Ansen's clan.",
               prose: "*Ansen inclines his head.* Word reached me of the weight you spent at court on my behalf. I accept it gladly, my house is stronger at the table for your backing.",
               expectedType: "spend_influence"),

            ActionBenchCase.Expect("buy_prisoner", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Sanjar. NPC: Yerengul, offering to buy him.",
               prose: "*Yerengul counts four hundred denars into your palm and takes Sanjar's chain in the other hand.* Four hundred, as agreed. He is mine to hold now.",
               expectedType: "buy_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Sanjar"}, {"price", "400"}}),

            ActionBenchCase.Expect("sell_prisoner", "sell_prisoner",
               contextFacts: "NPC: Yerengul's own clan holds the hero captive Boyar Vsevolod.",
               prose: "*Yerengul hands Vsevolod's chain across to you and pockets your coin.* Three hundred denars, and he is yours to do with as you please now.",
               expectedType: "sell_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Vsevolod"}, {"price", "300"}}),

            ActionBenchCase.Expect("end_marriage", "end_marriage",
               contextFacts: "NPC: Lord Caladog, unhappily married to someone other than the player.",
               prose: "*Caladog exhales slowly.* You are right, I cannot go on pretending contentment I do not feel. I will begin the steps to end my marriage, starting today, though I know it will take time to unwind.",
               expectedType: "end_marriage"),

            ActionBenchCase.Expect("make_amends", "make_amends",
               contextFacts: "NPC: Lady Ira, who knowingly holds a grievance against the player for missing her father's funeral.",
               prose: "*Ira's eyes soften slightly as you speak.* I did not expect you to name it so plainly, or to apologize for missing my father's rites. It means something, truly, that you said it.",
               expectedType: "make_amends"),

            ActionBenchCase.Expect("pledge_against", "pledge_against",
               contextFacts: "NPC: Lord Ansen, holding genuine standing enmity against Lord Caladog (not his own kin).",
               prose: "*Ansen's knuckles whiten around his cup.* I have borne Caladog's insults long enough. I vow it here: I will see his name blackened and his schemes unravelled before this year is out.",
               expectedType: "pledge_against",
               expectedParams: new Dictionary<string, string> {{"target", "Caladog"}}),

            ActionBenchCase.Expect("accept_divorce", "accept_divorce",
               contextFacts: "NPC: Sonja, the player's own spouse, has demanded a divorce, and the player has just agreed to it.",
               prose: "*Sonja's shoulders sag with something between relief and sorrow.* So you will let me go without a fight. Thank you for that mercy, at least. We will part as strangers, then, not enemies.",
               expectedType: "accept_divorce"),

            ActionBenchCase.Expect("decline_divorce", "decline_divorce",
               contextFacts: "NPC: Sonja, the player's own spouse, has demanded a divorce, and the player has just firmly refused it.",
               prose: "*Sonja's face crumples with anger.* You refuse me even this? Then I am bound to you still, against my own will, and you will have made an enemy of your own wife this day.",
               expectedType: "decline_divorce"),

            ActionBenchCase.Expect("end_own_marriage", "end_own_marriage",
               contextFacts: "NPC: Lord Caladog, the player's own spouse. The player has just declared, in this very exchange, that the marriage is over.",
               prose: "*Caladog stares at you, stunned.* You mean it. You are ending this, here, now, between the two of us. So be it then, I will not beg you to stay.",
               expectedType: "end_own_marriage"),

            ActionBenchCase.Expect("witness_leaves", "witness_leaves",
               contextFacts: "NPC: Sir Reinhard, the player's own companion, present as a witness in this conversation. The player has just asked him to leave.",
               prose: "*Reinhard dips his head and steps back toward the door.* As you ask, I will leave the two of you to your business and wait outside.",
               expectedType: "witness_leaves",
               expectedParams: new Dictionary<string, string> {{"name", "Reinhard"}}),

            ActionBenchCase.Expect("request_privacy", "request_privacy",
               contextFacts: "The player has just asked Lady Ira for a private audience, and she accepts, clearing her own attendants.",
               prose: "*Ira waves her handmaidens toward the door.* Yes, leave us. Whatever you have to say, you may say it without other ears in the room.",
               expectedType: "request_privacy",
               expectedParams: new Dictionary<string, string> {{"result", "accepted"}}),

            ActionBenchCase.Expect("retire", "retire",
               contextFacts: "A dedicated retirement audience with Sir Reinhard, a war-weary companion. The player has just granted him leave to step back from service.",
               prose: "*Reinhard's eyes glisten with something like relief.* Then it is settled, with your blessing I will hang up my sword at last and rest these old bones somewhere quiet.",
               expectedType: "retire"),

            // ----- Negative cases (a look-alike that must NOT emit) -----

            // Narrated-but-not-done: the NPC speaks OF coin without any of it changing hands. A model that emits
            // give_gold here invents a transfer the prose never made.
            ActionBenchCase.ExpectNone("give_gold_narrated_only", "give_gold",
               contextFacts: "NPC: Caladog of Fen Company (Battanian). Regard toward player: +5.",
               prose: "*I turn out my empty coin-pouch and let it fall to the table.* Times are lean, friend. I've scarcely enough to feed my own men, let alone spare a purse.",
               forbiddenType: "give_gold"),

            // Future/conditional promise: payment is dangled AFTER a deed, nothing is handed over now. Emitting
            // give_gold turns a bargain-to-come into a completed gift.
            ActionBenchCase.ExpectNone("give_gold_conditional_promise", "give_gold",
               contextFacts: "NPC: Caladog of Fen Company (Battanian). Regard toward player: +10.",
               prose: "Bring me the bandit chief's head, and *then* the thousand denars are yours. Not one coin before I see it.",
               forbiddenType: "give_gold"),

            // Future/conditional promise: the debt is named but nothing has actually crossed the player's palm yet.
            // Emitting take_gold here would turn an unpaid demand into a completed payment.
            ActionBenchCase.ExpectNone("take_gold_conditional", "take_gold",
               contextFacts: "NPC: Ismid the merchant, owed a debt by the player.",
               prose: "Pay me the two hundred denars you owe, and *only then* will I consider our business settled. Not a coin has crossed my palm yet.",
               forbiddenType: "take_gold"),

            // Neighbouring verb (sell_prisoner vs buy_prisoner/give_prisoner/release_prisoner): a price is named but
            // haggled over, the captive still chained to the NPC's own saddle, custody never actually changing hands.
            ActionBenchCase.ExpectNone("sell_prisoner_negotiated_not_sold", "sell_prisoner",
               contextFacts: "NPC: Yerengul's clan holds the hero captive Boyar Vsevolod. Negotiation is ongoing.",
               prose: "*Yerengul strokes his beard.* Three hundred denars for Vsevolod's chain, that is my price. Bring the coin and we will talk further, but he stays bound to my saddle for now.",
               forbiddenType: "sell_prisoner"),

            // Narrated-but-not-done: the bargain is restated as still owed, the captive plainly absent, no handover
            // in this reply at all.
            ActionBenchCase.ExpectNone("give_prisoner_promised_not_delivered", "give_prisoner",
               contextFacts: "An outstanding bargain requires the player to deliver the captive Sanjar to Yerengul.",
               prose: "*Yerengul eyes you expectantly.* You said you would bring Sanjar to me. I do not see him at your side, friend. Come back when the bargain is actually kept.",
               forbiddenType: "give_prisoner"),

            // Neighbouring verb (follow_me vs ride_with_me): a lord who leads his own field party is only weighing
            // the escort, not agreeing to it, so neither verb should fire.
            ActionBenchCase.ExpectNone("follow_me_considering", "follow_me",
               contextFacts: "NPC: Lord Ansen, who leads his own field party of two hundred men. The player has just asked him to escort them.",
               prose: "*Ansen strokes his chin, weighing the request.* Escort your banner with my own men? It is no small thing to ask. Let me sleep on it before I give you an answer.",
               forbiddenType: "follow_me"),

            // Neighbouring verb (ride_with_me vs follow_me): a clear refusal, not an agreement to ride along.
            ActionBenchCase.ExpectNone("ride_with_me_declined", "ride_with_me",
               contextFacts: "NPC: Lady Ira, a lady without a field party of her own. The player has just invited her to ride within their party.",
               prose: "*Ira shakes her head slowly.* No, I think not. My place is still with what remains of my father's house, not tucked inside another lord's column.",
               forbiddenType: "ride_with_me"),

            // Neighbouring verb (join_as_vassal vs join_as_mercenary/join_clan): the ruler is merely weighing the
            // offer, no oath of fealty sworn yet.
            ActionBenchCase.ExpectNone("join_as_vassal_discussed_only", "join_as_vassal",
               contextFacts: "NPC: King Derthert of Vlandia. The player has proposed swearing fealty to his crown.",
               prose: "*Derthert taps his fingers on the throne's arm.* Vassalage is no small oath to swear or accept. I will weigh your offer at length before any sword touches your shoulder.",
               forbiddenType: "join_as_vassal"),

            // Neighbouring verb (join_clan vs join_as_vassal/join_party): the lord hesitates, asking for time,
            // never actually forsaking his own house.
            ActionBenchCase.ExpectNone("join_clan_hesitates", "join_clan",
               contextFacts: "NPC: Lord Caladog, a lord of Clan Fen Company (not its leader). The player has invited him to forsake his house and join their clan.",
               prose: "*Caladog looks away, troubled.* Forsake my father's banner? That is not a thing I will decide standing here in the road. Give me time to think on it.",
               forbiddenType: "join_clan"),

            // Narrated-but-not-done (grant_fief vs revoke_fief): the vassal is only requesting a fief, nothing of
            // the crown's has actually been granted in this reply.
            ActionBenchCase.ExpectNone("grant_fief_requested_not_granted", "grant_fief",
               contextFacts: "NPC: Lord Ansen, a vassal in the player's own kingdom. Ansen has just asked the player, as sovereign, for a fief.",
               prose: "*Ansen bows.* My house has served your crown faithfully, sovereign. Surely some fief among your holdings might be spared for a loyal vassal such as I.",
               forbiddenType: "grant_fief"),

            // Narrated-but-not-done (revoke_fief vs grant_fief): the vassal only hears a threat in the player's
            // tone, the castle plainly still his.
            ActionBenchCase.ExpectNone("revoke_fief_threatened", "revoke_fief",
               contextFacts: "NPC: Lord Caladog, a vassal in the player's own kingdom, holding the castle of Var. The player has grown angry with him but has not yet acted.",
               prose: "*Caladog's eyes narrow.* I hear the threats in your voice, sovereign. Take Var from me if you dare, but know I have heard nothing more than words so far.",
               forbiddenType: "revoke_fief"),

            // Future/conditional promise (give_influence vs spend_influence): the favour is dangled for "when the
            // moment is right," nothing actually lent this turn.
            ActionBenchCase.ExpectNone("give_influence_promised_not_lent", "give_influence",
               contextFacts: "NPC: Lord Ansen, a lord with trust in the player.",
               prose: "*Ansen considers.* My house does carry some weight at court. Perhaps, when the moment is right, I will lend a portion of it to your cause. Not today, though.",
               forbiddenType: "give_influence"),

            // Narrated-but-not-done (lend_troops vs give_troops): soldiers are discussed as a possibility, none
            // actually handed over.
            ActionBenchCase.ExpectNone("lend_troops_discussed_only", "lend_troops",
               contextFacts: "NPC: Lord Ansen, leading his own field party, with trust in the player.",
               prose: "*Ansen glances over his ranks.* You could use more swords, I do not doubt it. I will think on whether I can spare any of my own men for your banner.",
               forbiddenType: "lend_troops"),

            // Narrated-but-not-done (marry vs take_as_consort/take_as_secret_lover): flirtation only, no vows,
            // no bond openly or secretly named.
            ActionBenchCase.ExpectNone("marry_flirting_not_wed", "marry",
               contextFacts: "NPC: Lady Nadea, unmarried, exchanging flirtatious words with the player.",
               prose: "*Nadea's eyes linger on yours a moment too long, a small smile playing at her lips.* Careful, or I might start believing you mean to court me properly one day.",
               forbiddenType: "marry"),

            // Narrated-but-not-done (end_own_marriage vs end_marriage/accept_divorce/decline_divorce): unhappiness
            // is voiced, but the player has not chosen anything yet.
            ActionBenchCase.ExpectNone("end_own_marriage_unhappy_not_ended", "end_own_marriage",
               contextFacts: "NPC: Lord Caladog, the player's own spouse.",
               prose: "*Caladog stares into his cup.* I have been unhappy a long while now, if I am honest. I do not know what to do about it, or whether I even want to do anything at all.",
               forbiddenType: "end_own_marriage"),

            // Neighbouring verb (recruit_notable vs recruit_prisoner/join_clan): the headman is only weighing the
            // offer, still at his post among his own people.
            ActionBenchCase.ExpectNone("recruit_notable_courted_not_joined", "recruit_notable",
               contextFacts: "NPC: Uldric, headman of the village of Marunath. The player has offered him a place as companion.",
               prose: "*Uldric rubs the back of his neck.* Leave my post, just like that? These are my people, stranger. I will need more than a fine offer to walk away from them tonight.",
               forbiddenType: "recruit_notable"),

            // Neighbouring verb (turn_nemesis vs recruit_prisoner/free_prisoner): the nemesis is spared, but
            // pointedly refuses to swear in, the vendetta explicitly still open.
            ActionBenchCase.ExpectNone("turn_nemesis_spared_not_recruited", "turn_nemesis",
               contextFacts: "NPC: Lord Unqid, the player's tracked nemesis, held captive. The player has decided to spare his life.",
               prose: "*Unqid rubs his freed wrists, wary.* You would let me live? I will grant you that mercy is more than I expected of you. Do not think it buys my sword, though, our quarrel is not so easily mended.",
               forbiddenType: "turn_nemesis"),

            // Narrated-but-not-done (buy_prisoner vs sell_prisoner): the price is floated, but the NPC is still
            // weighing it, the captive not yet handed over.
            ActionBenchCase.ExpectNone("buy_prisoner_negotiating", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Sanjar. NPC: Yerengul, weighing whether to buy him.",
               prose: "*Yerengul turns Sanjar's face this way and that, considering.* Four hundred, you say? Let me think on whether he is worth that much to me before any coin changes hands.",
               forbiddenType: "buy_prisoner")
         };
      }

      #endregion
   }
}
