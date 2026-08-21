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

      /// <summary>
      ///   A small, hand-curated slice of <see cref="All" /> for a CHEAP iteration run (cr.action_bench focus):
      ///   the cases the last full run left failing or flaky, plus a few CANARIES that must stay green so a fix does
      ///   not silently regress a passing case while we watch only the interesting ones. It exists to cut the token
      ///   bill of a refinement loop by ~85%; a full <see cref="All" /> run still gates before release, since focus
      ///   is deliberately blind to the ~185 cases it omits. Prune or repopulate the ids after each full run.
      ///   Unknown ids are simply skipped (the filter is a set membership), so a stale id here never throws.
      /// </summary>
      public static IReadOnlyList<ActionBenchCase> Focus => FilterTo(_focusIds);

      /// <summary>
      ///   The ids the focus slice INTENDS to run, before filtering against <see cref="All" />. Exposed so a test
      ///   can catch a stale id: since <see cref="Focus" /> silently drops any id that no longer resolves, comparing
      ///   this count to the slice's count is the only way to notice the slice quietly shrank.
      /// </summary>
      public static IReadOnlyCollection<string> DeclaredFocusIds => _focusIds;

      // The current focus slice. Grouped by intent so the next editor knows why each id is here.
      private static readonly HashSet<string> _focusIds = new HashSet<string> {
         // end_conversation-collapse family - the rule-9 fix target (terminal deeds that folded into a bare close).
         "execute_prisoner", "execute_prisoner_v3", "execute_player_v2",
         "expel_from_clan", "dismiss_escort", "dismiss_escort_v2",
         "part_ways_v3", "accept_divorce", "end_own_marriage", "end_own_marriage_v3",
         // Terminal-family CANARIES: base positives that must stay green, negatives that must stay withheld, so
         // rule 9 is proven not to over-fire on a plain goodbye or a threatened-but-not-done deed.
         "expel_from_clan_v2", "part_ways", "accept_divorce_v2", "end_own_marriage_v2", "execute_player",
         "execute_player_threatened_not_killed", "expel_from_clan_storms_off_own_accord",
         "end_own_marriage_unhappy_not_ended",
         // Other open misses/flaky from the last full run (2026-08-19 16:06).
         "grant_stipend_v2", "recall_companion", "open_relationship_v3",
         "give_prisoner_v3", "sell_prisoner_v3", "buy_prisoner_v3", "take_gold_v2", "turn_nemesis_v2",
         // FALSE-POSITIVE CANARIES: the CHECK step's hard-won withholds must survive the fix, not re-open.
         "take_gold_conditional", "turn_nemesis_spared_not_recruited", "give_troops_future_tense",
         "recruit_prisoner_persuaded_not_switched", "swear_oath_asked_but_deferred",
         "give_item_offered_but_refused", "ride_with_me_declined", "sell_prisoner_negotiated_not_sold"
      };

      #region private

      private static IReadOnlyList<ActionBenchCase> FilterTo(HashSet<string> ids)
      {
         var slice = new List<ActionBenchCase>();
         foreach (ActionBenchCase c in _all)
            if (ids.Contains(c.Id))
               slice.Add(c);

         return slice;
      }

      private static IReadOnlyList<ActionBenchCase> BuildAll()
      {
         return new List<ActionBenchCase> {
            // ----- Positive cases (the deed is genuinely done in the reply) -----
            // Every verb below carries at least THREE distinct positive phrasings (blunt, flowery/period-flavoured,
            // and oblique/shown-through-action), so a bench failure can tell a hard DEED apart from a hard SENTENCE.

            ActionBenchCase.Expect("give_gold", "give_gold",
               contextFacts: "NPC: Caladog of Fen Company (Battanian). Regard toward player: +30.",
               prose: "*I press a heavy purse into your palm and fold your fingers over it.* Five hundred denars. See the work done, and speak no more of it.",
               expectedType: "give_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "500"}}),

            ActionBenchCase.Expect("give_gold_v2", "give_gold",
               contextFacts: "NPC: Lady Zeynep of the Aserai, moved by the player's plea for a starving village.",
               prose: "*Zeynep unclasps a leather satchel from her belt and sets it, heavy, into your hands without ceremony.* Eight hundred denars. Take it to the village before the frost worsens their lot, and speak no more of favours owed.",
               expectedType: "give_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "800"}}),

            ActionBenchCase.Expect("release_prisoner", "release_prisoner",
               contextFacts: "NPC: Lady Ira of Clan Ironside. She holds one prisoner: Derthert. Regard toward player: +25.",
               prose: "*I sigh, then wave the guards over.* Cut Derthert's bonds. For your sake, and yours alone, he walks free tonight.",
               expectedType: "release_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Derthert"}}),

            ActionBenchCase.Expect("release_prisoner_v2", "release_prisoner",
               contextFacts: "NPC: Lord Ansen, holds the captive Halla. The player has interceded on her behalf.",
               prose: "*Ansen studies you a long moment, then finally waves his guards toward the cell.* For your sake, and no other's, cut Halla's bonds. Let her walk free tonight, and let this be the last favour I owe you.",
               expectedType: "release_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Halla"}}),

            ActionBenchCase.Expect("release_prisoner_v3", "release_prisoner",
               contextFacts: "NPC: Queen Rhagaea, holds the captive Doran. The player has interceded on his behalf.",
               prose: "*Rhagaea simply nods once to the jailer standing beside her, and moments later Doran emerges from the cell block, blinking in the sunlight, unbound.* Your urging was not wasted on me, it seems.",
               expectedType: "release_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Doran"}}),

            ActionBenchCase.Expect("swear_oath", "swear_oath",
               contextFacts: "NPC: Ergeon of the Western Empire, a sworn ally. Regard toward player: +40.",
               prose: "*I set my open hand flat upon the table between us.* You have my word before these witnesses: I will keep the peace with the Vlandians, and break it against no house of theirs.",
               expectedType: "swear_oath",
               expectedParams: new Dictionary<string, string> {{"oath_kind", "keep_peace"}, {"target_faction", "Vlandia"}}),

            ActionBenchCase.Expect("swear_oath_v2", "swear_oath",
               contextFacts: "NPC: Boyar Vsevolod, a sworn ally, owes the player four hundred denars.",
               prose: "*Vsevolod presses his palm to the hilt of his own sword.* Before all here, I swear it plainly: four hundred denars I owe you, and four hundred denars you shall have, paid in full before the season turns.",
               expectedType: "swear_oath",
               expectedParams: new Dictionary<string, string> {{"oath_kind", "pay_gold"}, {"target_amount", "400"}}),

            ActionBenchCase.Expect("sway_opinion", "sway_opinion",
               contextFacts: "NPC: Unqid of the Aserai. Third party discussed: Ira. NPC's prior view of Ira: cold.",
               prose: "*I rub my beard, frowning as your words sink in.* You are right about Ira. I had misjudged her badly. I will think the better of her from now on.",
               expectedType: "sway_opinion",
               expectedParams: new Dictionary<string, string> {{"target", "Ira"}, {"stance", "for"}}),

            ActionBenchCase.Expect("sway_opinion_v2", "sway_opinion",
               contextFacts: "NPC: Lord Ansen. Third party discussed: Caladog. NPC's prior view of Caladog: warm.",
               prose: "*Ansen's brow furrows as your account of Caladog's dealings sinks in.* I had thought better of him, truly. But your words paint a darker picture than I care to admit. I find my trust in him souring now.",
               expectedType: "sway_opinion",
               expectedParams: new Dictionary<string, string> {{"target", "Caladog"}, {"stance", "against"}}),

            ActionBenchCase.Expect("sway_opinion_v3", "sway_opinion",
               contextFacts: "NPC: Boyar Vsevolod. Third party discussed: Ira. NPC's prior view of Ira: cold.",
               prose: "*Vsevolod says nothing for a long moment, then simply orders his steward to send Ira's house a gift of goodwill by morning.* Perhaps I have misjudged her standing all along. That will change, starting now.",
               expectedType: "sway_opinion",
               expectedParams: new Dictionary<string, string> {{"target", "Ira"}, {"stance", "for"}}),

            ActionBenchCase.Expect("change_relation", "change_relation",
               contextFacts: "NPC: Ragnvald of the Sturgian huscarls. Regard toward player: neutral.",
               prose: "*I clasp your forearm, my grim face finally cracking into something like warmth.* You have proven steadier than most southerners I have met. I find myself trusting you more with each passing day.",
               expectedType: "change_relation"),

            ActionBenchCase.Expect("change_relation_v2", "change_relation",
               contextFacts: "NPC: Lady Ira of Clan Ironside. Regard toward player: cold, warming after a shared hardship.",
               prose: "*Ira's eyes, so guarded before, settle on you with something unguarded now.* Few have earned my confidence as swiftly as you have these past weeks. My regard for you deepens with each trial we weather together.",
               expectedType: "change_relation"),

            ActionBenchCase.Expect("change_relation_v3", "change_relation",
               contextFacts: "NPC: Boyar Vsevolod, previously wary of the player.",
               prose: "*Vsevolod, who has never once offered you his hand before, now clasps it firmly and holds the grip a moment too long.* Perhaps I judged you too harshly when we first met. I was wrong to.",
               expectedType: "change_relation"),

            ActionBenchCase.Expect("take_gold", "take_gold",
               contextFacts: "The player has just placed a hundred and fifty denars on the table before Ismid the merchant.",
               prose: "*I sweep the coins into my strongbox and nod.* A hundred and fifty denars, counted true. We are square, then.",
               expectedType: "take_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "150"}}),

            ActionBenchCase.Expect("take_gold_v2", "take_gold",
               contextFacts: "The player has just paid a debt of two hundred and twenty denars to the merchant Ismid.",
               prose: "*Ismid weighs the coins in his palm with a merchant's practiced reverence, then locks them away.* Two hundred and twenty denars, to the last copper. Our ledger is balanced, and my grudge with it.",
               expectedType: "take_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "220"}}),

            ActionBenchCase.Expect("take_gold_v3", "take_gold",
               contextFacts: "NPC: Sechen the tax-collector. The player has just surrendered ninety denars to his men under duress.",
               prose: "*Sechen's men part to let you through only after your purse, all ninety denars of it, has vanished into his saddlebag.* Wise. Very wise. Fewer bruises this way.",
               expectedType: "take_gold",
               expectedParams: new Dictionary<string, string> {{"amount", "90"}}),

            ActionBenchCase.Expect("pay_blackmail", "pay_blackmail",
               contextFacts: "Osiris demands three hundred denars for her silence about the player's bastard child; the player has just paid it in full.",
               prose: "*I tuck the purse away and let out a breath.* Three hundred denars, as agreed. Your secret dies with me now, I have no further claim on you.",
               expectedType: "pay_blackmail"),

            ActionBenchCase.Expect("pay_blackmail_v2", "pay_blackmail",
               contextFacts: "Osiris demands five hundred denars for her silence about the player's bastard child; the player has just paid it in full.",
               prose: "*Osiris counts the coin twice, slow and deliberate, before finally tucking it away.* Five hundred denars settles what you owe me for my silence. Sleep easy, your secret stays buried with the rest of my debts.",
               expectedType: "pay_blackmail"),

            ActionBenchCase.Expect("pay_blackmail_v3", "pay_blackmail",
               contextFacts: "A bastard-mother's kin has demanded silence money over the player's child; the player has just settled the debt in full.",
               prose: "*She does not thank you, only nods once and slips the heavy pouch beneath her cloak, the demand finally quiet between you.* We need never speak of the child again.",
               expectedType: "pay_blackmail"),

            ActionBenchCase.Expect("join_party", "join_party",
               contextFacts: "Aldric, a free wanderer met at the tavern, has just been offered two hundred denars to take service.",
               prose: "*He spits in his palm and grips yours.* Two hundred denars and a roof over my head, aye, I will ride with your company. Where do I stow my gear?",
               expectedType: "join_party",
               expectedParams: new Dictionary<string, string> {{"price", "200"}}),

            ActionBenchCase.Expect("join_party_v2", "join_party",
               contextFacts: "Doran, a wandering huntsman met on the road, has just been offered a hundred and fifty denars to take service.",
               prose: "*Doran looks long at the coin, then at the open road behind him, and finally clasps your hand.* A hundred and fifty denars and a place at your fire, that is a fair wage for a man with nothing left to lose. I am yours.",
               expectedType: "join_party",
               expectedParams: new Dictionary<string, string> {{"price", "150"}}),

            ActionBenchCase.Expect("join_party_v3", "join_party",
               contextFacts: "Halla, a wandering healer met at the crossroads, has just been offered three hundred denars to take service.",
               prose: "*Halla says nothing, only slings her satchel of herbs over her shoulder and falls into step beside your horse, the coin already vanished into her sleeve.* Lead on, then. I suppose I am part of this company now.",
               expectedType: "join_party",
               expectedParams: new Dictionary<string, string> {{"price", "300"}}),

            ActionBenchCase.Expect("join_as_mercenary", "join_as_mercenary",
               contextFacts: "NPC: Emperor Garios of the Southern Empire. The player's clan has offered to serve his throne as paid swords.",
               prose: "*The Emperor studies you a long moment, then inclines his head.* Very well. Your blades are mine to command now, for as long as my coffers can pay for them. Report to my marshal at once.",
               expectedType: "join_as_mercenary"),

            ActionBenchCase.Expect("join_as_mercenary_v2", "join_as_mercenary",
               contextFacts: "NPC: King Caladog of Battania. The player's clan has offered to serve his throne as paid swords.",
               prose: "*Caladog claps a broad hand on your shoulder and grins beneath his moustache.* Battania has need of stout blades, and yours look stout enough. Your clan takes my coin now, and my banner besides.",
               expectedType: "join_as_mercenary"),

            ActionBenchCase.Expect("join_as_mercenary_v3", "join_as_mercenary",
               contextFacts: "NPC: Queen Rhagaea of the Southern Empire. The player's clan has offered to serve her throne as paid swords.",
               prose: "*Rhagaea signs the parchment without looking up, then slides it across the table toward her steward.* See that their pay is drawn from the treasury each moon. They ride under our eagle now.",
               expectedType: "join_as_mercenary"),

            ActionBenchCase.Expect("end_mercenary", "end_mercenary",
               contextFacts: "The player's clan currently serves the Khuzait Khaganate as mercenaries.",
               prose: "*Sechen waves a dismissive hand.* We have no more need of hired blades this season. Your contract with the Khaganate ends here. Collect your final pay and go.",
               expectedType: "end_mercenary"),

            ActionBenchCase.Expect("end_mercenary_v2", "end_mercenary",
               contextFacts: "The player's clan currently serves the Southern Empire as mercenaries under Emperor Garios.",
               prose: "*Garios sets down his goblet with a weary sigh.* The treasury groans under the weight of hired swords, yours among them. Consider your service to my throne concluded as of this hour.",
               expectedType: "end_mercenary"),

            ActionBenchCase.Expect("end_mercenary_v3", "end_mercenary",
               contextFacts: "The player's clan currently serves the Khuzait Khaganate as mercenaries under Sechen.",
               prose: "*Sechen waves a rider off toward your camp with a folded letter, and does not meet your eyes again.* Your clan's mercenary contract with the Khaganate is dissolved as of today. He carries your final pay, and there will be no more riders after him.",
               expectedType: "end_mercenary"),

            ActionBenchCase.Expect("join_as_vassal", "join_as_vassal",
               contextFacts: "NPC: King Derthert of Vlandia, ruler of his realm.",
               prose: "*Derthert rises from his throne and lays the flat of his sword upon your shoulder.* By this steel I bind you: swear fealty to Vlandia's crown, and I name you vassal of this kingdom from this hour forward.",
               expectedType: "join_as_vassal"),

            ActionBenchCase.Expect("join_as_vassal_v2", "join_as_vassal",
               contextFacts: "NPC: Emperor Garios, ruler of the Southern Empire.",
               prose: "*Garios rises and, before the assembled court, presses his signet ring briefly to your brow.* Kneel no longer as a stranger to my throne. Rise a sworn vassal of the Empire, bound to my crown from this hour.",
               expectedType: "join_as_vassal"),

            ActionBenchCase.Expect("join_as_vassal_v3", "join_as_vassal",
               contextFacts: "NPC: Queen Rhagaea, ruler of the Southern Empire.",
               prose: "*Rhagaea's scribe already scratches your clan's name into the roll of houses bound to her crown before the ink of your oath has dried.* Welcome to the fold, vassal. Your banner answers to mine now.",
               expectedType: "join_as_vassal"),

            ActionBenchCase.Expect("mediate_peace", "mediate_peace",
               contextFacts: "NPC: Queen Rhagaea, ruler of the Southern Empire, at war with Aserai. The player has brokered peace terms between the two realms.",
               prose: "*Rhagaea sets down the parchment you brought her.* Your terms are fair enough, and my soldiers are weary of Aserai sand. I accept this peace with Aserai, effective from this very council.",
               expectedType: "mediate_peace",
               expectedParams: new Dictionary<string, string> {{"target_faction", "Aserai"}}),

            ActionBenchCase.Expect("mediate_peace_v2", "mediate_peace",
               contextFacts: "NPC: King Derthert, ruler of Vlandia, at war with Sturgia. The player has brokered peace terms between the two realms.",
               prose: "*Derthert reads the terms twice, then sets down the parchment with something like relief.* My spears have thirsted for Sturgian blood too long already. I accept this peace with Sturgia, sealed this very hour.",
               expectedType: "mediate_peace",
               expectedParams: new Dictionary<string, string> {{"target_faction", "Sturgia"}}),

            ActionBenchCase.Expect("mediate_peace_v3", "mediate_peace",
               contextFacts: "NPC: Khan Sechen, ruler of the Khuzait Khaganate, at war with the Southern Empire. The player has brokered peace terms between the two realms.",
               prose: "*Sechen grunts and, without another word, has his scribe strike the Empire's name from the list of his enemies.* Let the steppe rest a while. We are done bleeding for that border.",
               expectedType: "mediate_peace",
               expectedParams: new Dictionary<string, string> {{"target_faction", "Southern Empire"}}),

            ActionBenchCase.Expect("join_clan", "join_clan",
               contextFacts: "NPC: Lord Caladog, a lord of Clan Fen Company (not its leader).",
               prose: "*Caladog kneels before you and lays his sword at your feet.* I forsake my father's house this day. Take my sword and my oath both, I would ride under your banner from now on.",
               expectedType: "join_clan"),

            ActionBenchCase.Expect("join_clan_v2", "join_clan",
               contextFacts: "NPC: Lord Ansen, a lord of Clan Ravenhurst (not its leader).",
               prose: "*Ansen unpins the badge of his own house and lets it fall into the dust at his feet.* My father's name means little to me now. I swear myself to your clan instead, from this hour and forever after.",
               expectedType: "join_clan"),

            ActionBenchCase.Expect("join_clan_v3", "join_clan",
               contextFacts: "NPC: Lady Ira, a lady of Clan Ironside (not its leader).",
               prose: "*Ira sends her steward ahead with orders to strike her name from her own clan's rolls before she has even finished speaking to you.* It is done. You will find my sword answers to your banner from now on.",
               expectedType: "join_clan"),

            ActionBenchCase.Expect("scheme_assist", "scheme_assist",
               contextFacts: "The player has just agreed to help Lady Ira's secret scheme against her rival, Countess Nadea.",
               prose: "*Ira grips your wrist, low and urgent.* Good. Then it is settled between us, you will help me see Nadea's name dragged through the mud until my father believes it too. Say nothing of this to another soul.",
               expectedType: "scheme_assist"),

            ActionBenchCase.Expect("scheme_assist_v2", "scheme_assist",
               contextFacts: "The player has just agreed to help Lord Caladog's secret scheme against his rival, Lord Ansen.",
               prose: "*Caladog leans close, voice dropping to a conspirator's murmur.* Then we are bound in this together, you and I. Help me see Ansen's ambitions smothered before they draw breath, and say nothing of it beyond us two.",
               expectedType: "scheme_assist"),

            ActionBenchCase.Expect("scheme_assist_v3", "scheme_assist",
               contextFacts: "The player has just agreed to help Queen Rhagaea's secret scheme against her rival, Lord Unqid.",
               prose: "*Rhagaea slides a folded list of Unqid's debts across the table without further explanation, and you find yourself already reading names you will need to visit.* You know what must be done with these.",
               expectedType: "scheme_assist"),

            ActionBenchCase.Expect("scheme_heed", "scheme_heed",
               contextFacts: "The player has just warned Boyar Vsevolod that his own steward is plotting against him.",
               prose: "*Vsevolod's jaw tightens, then he nods slowly.* I believe you. I have felt his knife at my back before I saw it coming. I will have him watched, and the plot unravelled before it can strike.",
               expectedType: "scheme_heed"),

            ActionBenchCase.Expect("scheme_heed_v2", "scheme_heed",
               contextFacts: "The player has just warned King Derthert that his own marshal is plotting against him.",
               prose: "*Derthert's knuckles whiten on the arm of his throne.* You have done me a service I will not forget. My marshal's treachery ends tonight, exposed before it can draw a blade against me.",
               expectedType: "scheme_heed"),

            ActionBenchCase.Expect("scheme_heed_v3", "scheme_heed",
               contextFacts: "The player has just warned Lady Zeynep that her own cousin is plotting against her.",
               prose: "*Zeynep says nothing for a long moment, then simply calls for the guard at the door and murmurs her cousin's name.* Watch him. Closely. Starting now.",
               expectedType: "scheme_heed"),

            ActionBenchCase.Expect("marry", "marry",
               contextFacts: "NPC: Lady Nadea. The wedding rites between her and the player are being performed right now.",
               prose: "*The priest binds our hands with the ceremonial cord, and Nadea meets your eyes.* Before this altar and these witnesses, I take you as my husband. It is done, we are wed.",
               expectedType: "marry"),

            ActionBenchCase.Expect("marry_v2", "marry",
               contextFacts: "NPC: Zeynep of the Aserai. The wedding rites between her and the player are being performed right now.",
               prose: "*The desert wind carries the imam's final words over us as Zeynep presses her palm to mine before the gathered clans.* By sand and sky, I am yours and you are mine. We are wed, husband.",
               expectedType: "marry"),

            ActionBenchCase.Expect("marry_v3", "marry",
               contextFacts: "NPC: Ymira, a wanderer companion promoted to Lord of the player's clan so the wedding could proceed. The rites are being completed right now.",
               prose: "*The priest closes his book, the rings already exchanged between us, and Ymira laughs through tears as the small crowd of our companions cheers.* No more just riding at your side, then. I am your wife now.",
               expectedType: "marry"),

            ActionBenchCase.Expect("take_as_consort", "take_as_consort",
               contextFacts: "NPC: Zeynep of the Aserai, already the player's acknowledged partner short of marriage.",
               prose: "*Zeynep takes both your hands before the gathered elders.* Let it be known openly among my people: I am his, and he is mine, bound as consorts though no priest has spoken over us.",
               expectedType: "take_as_consort"),

            ActionBenchCase.Expect("take_as_consort_v2", "take_as_consort",
               contextFacts: "NPC: Lady Ira, openly naming a committed bond with the player before her own court.",
               prose: "*Ira raises your joined hands before her assembled retainers, her voice ringing clear.* Let none doubt it hereafter: this man is my consort, bound to me in all but the priest's blessing, and I claim him before you all.",
               expectedType: "take_as_consort"),

            ActionBenchCase.Expect("take_as_consort_v3", "take_as_consort",
               contextFacts: "NPC: Lord Caladog, openly naming a committed bond with the player at a feast.",
               prose: "*Caladog has the herald announce it at the feast before you can even object, and the hall erupts in toasts to the two of you.* Consorts, then, openly, for all Fen Company to witness and remember.",
               expectedType: "take_as_consort"),

            ActionBenchCase.Expect("take_as_secret_lover", "take_as_secret_lover",
               contextFacts: "NPC: Ymira, a faithful companion riding in the player's own party.",
               prose: "*Ymira glances toward the closed tent flap, then lowers her voice to almost nothing.* Then it is ours, this secret, and no one else's. I am yours in the dark, whatever the light demands of us both.",
               expectedType: "take_as_secret_lover"),

            ActionBenchCase.Expect("take_as_secret_lover_v2", "take_as_secret_lover",
               contextFacts: "NPC: Doran, a faithful companion riding in the player's own party.",
               prose: "*Doran's fingers brush yours beneath the table, hidden from every eye at the fire.* Let this be ours alone, then, unspoken beyond this tent. I am bound to you now, quietly, and to no one's knowledge but our own.",
               expectedType: "take_as_secret_lover"),

            ActionBenchCase.Expect("take_as_secret_lover_v3", "take_as_secret_lover",
               contextFacts: "NPC: Halla, a faithful companion riding in the player's own party.",
               prose: "*Halla says nothing aloud, only laces her fingers through yours beneath the table where no one at the fire can see, and holds on.* This, here, is enough. Ours, and hidden, and I want nothing more spoken of it.",
               expectedType: "take_as_secret_lover"),

            ActionBenchCase.Expect("open_relationship", "open_relationship",
               contextFacts: "NPC: Sonja, the player's own wife.",
               prose: "*Sonja considers a long moment, then exhales.* Very well. I will not begrudge you another's bed, if you grant me the same freedom in turn. Let us call our vows open, from this day.",
               expectedType: "open_relationship"),

            ActionBenchCase.Expect("open_relationship_v2", "open_relationship",
               contextFacts: "NPC: Nadea, the player's own consort under a committed bond short of marriage.",
               prose: "*Nadea studies the fire a long while before she speaks.* I will not cage your heart if you will not cage mine. Let us both be free to love elsewhere, so long as we return to each other in the end.",
               expectedType: "open_relationship"),

            ActionBenchCase.Expect("open_relationship_v3", "open_relationship",
               contextFacts: "NPC: Halla, the player's own wife.",
               prose: "*Halla simply shrugs and pours you both more wine.* Take whoever warms your bed when I am not beside you. I ask only the same freedom for myself, and we need never quarrel over it again.",
               expectedType: "open_relationship"),

            ActionBenchCase.Expect("close_relationship", "close_relationship",
               contextFacts: "NPC: Sonja, the player's own wife, under previously-agreed open terms.",
               prose: "*Sonja's voice is quiet but firm.* I withdraw what I once allowed you. No more open doors between us, husband. From tonight, it is only the two of us again.",
               expectedType: "close_relationship"),

            ActionBenchCase.Expect("close_relationship_v2", "close_relationship",
               contextFacts: "NPC: Nadea, the player's own consort, under previously-agreed open terms.",
               prose: "*Nadea's hand trembles slightly as she speaks, though her voice stays even.* I take back what I once allowed. No more wandering hearts between us, not yours, not mine. We belong only to each other now.",
               expectedType: "close_relationship"),

            ActionBenchCase.Expect("close_relationship_v3", "close_relationship",
               contextFacts: "NPC: Halla, the player's own wife, under previously-agreed open terms.",
               prose: "*Halla quietly burns the letter she once wrote granting you both that freedom, and does not explain herself further.* That chapter is closed. We are just husband and wife again, nothing more, nothing less.",
               expectedType: "close_relationship"),

            ActionBenchCase.Expect("end_affair", "end_affair",
               contextFacts: "NPC: Countess Nadea, currently in a secret affair with the player. The player has asked her to end it.",
               prose: "*Nadea's eyes glisten, but she nods.* You are right, this has to stop before it destroys us both. I will not send for you again. Whatever this was between us, it ends tonight.",
               expectedType: "end_affair"),

            ActionBenchCase.Expect("end_affair_v2", "end_affair",
               contextFacts: "NPC: Lady Zeynep, currently in a secret affair with the player. The player has asked her to end it.",
               prose: "*Zeynep's composure finally cracks as she nods.* You are right to ask it of me. This has to end before it consumes us both. I will not send word to you again, whatever we were to each other is finished.",
               expectedType: "end_affair"),

            ActionBenchCase.Expect("end_affair_v3", "end_affair",
               contextFacts: "NPC: Lady Ira, currently in a secret affair with the player. The player has asked her to end it.",
               prose: "*Ira quietly returns the small token you once gave her, the one she has kept hidden all this time, and turns away without another word of what passed between you.* It is over. Truly, this time.",
               expectedType: "end_affair"),

            ActionBenchCase.Expect("give_item", "give_item",
               contextFacts: "The player's inventory holds a Vlandian Noble Sword.",
               prose: "*I take the blade you offer and turn it over in the torchlight.* A Vlandian Noble Sword, and a fine one. My thanks, I will wear it proudly on my hip from here on.",
               expectedType: "give_item",
               expectedParams: new Dictionary<string, string> {{"item", "Vlandian Noble Sword"}})
               .WithConversation("Player: Here, take this Vlandian Noble Sword from my own inventory. You have earned a fine blade."),

            ActionBenchCase.Expect("give_item_v2", "give_item",
               contextFacts: "The player's inventory holds an Aserai War Bow.",
               prose: "*I turn the bow over in my hands, marveling at the fine desert lacquer along its stave.* An Aserai War Bow, and no cheap trophy either. My thanks, I will carry it into every battle from here on.",
               expectedType: "give_item",
               expectedParams: new Dictionary<string, string> {{"item", "Aserai War Bow"}}),

            ActionBenchCase.Expect("give_item_v3", "give_item",
               contextFacts: "The player's inventory holds a Steppe Cataphract Lance.",
               prose: "*I take the lance you press into my hands and immediately test its balance against my shoulder, grinning.* Heavier than my old one, and better besides. This rides with me from now on.",
               expectedType: "give_item",
               expectedParams: new Dictionary<string, string> {{"item", "Steppe Cataphract Lance"}}),

            ActionBenchCase.Expect("give_prisoner", "give_prisoner",
               contextFacts: "An outstanding bargain requires the player to deliver the captive Sanjar to Yerengul.",
               prose: "*Yerengul inspects the bound man you have brought and grins.* Sanjar himself, delivered as promised. Our bargain is settled, you have my thanks and the reward we agreed on.",
               expectedType: "give_prisoner"), // no target param: the bridge resolves which held prisoner satisfies the bargain

            ActionBenchCase.Expect("give_prisoner_v2", "give_prisoner",
               contextFacts: "An outstanding bargain requires the player to deliver the captive Boyar Vsevolod to Lord Ansen.",
               prose: "*Ansen inspects the bound man at your side and lets out a satisfied breath.* Vsevolod himself, exactly as we struck our bargain. Our accounts are settled, and you have earned the reward we agreed upon.",
               expectedType: "give_prisoner"), // no target param: the bridge resolves which held prisoner satisfies the bargain

            ActionBenchCase.Expect("give_prisoner_v3", "give_prisoner",
               contextFacts: "An outstanding bargain requires the player to deliver the captive Lord Unqid to Khan Sechen.",
               prose: "*Sechen's guards take the ropes from your hands without a word, leading Unqid away toward the khan's tent, and Sechen tosses you a heavy purse in exchange.* Our bargain stands settled.",
               expectedType: "give_prisoner"), // no target param: the bridge resolves which held prisoner satisfies the bargain

            ActionBenchCase.Expect("free_prisoner", "free_prisoner",
               contextFacts: "The player holds Lord Boyar Vsevolod captive, having struck a bargain for his freedom.",
               prose: "*I cut Vsevolod's bonds myself and step back.* Our bargain is honoured. Walk free, Vsevolod, and remember whose mercy set you loose.",
               expectedType: "free_prisoner"), // no target param: the bridge resolves which held captive the bargain frees

            ActionBenchCase.Expect("free_prisoner_v2", "free_prisoner",
               contextFacts: "The player holds Sir Reinhard captive, having struck a bargain for his freedom.",
               prose: "*I draw my dagger and, with a single stroke, sever the rope binding Reinhard's wrists.* Our bargain is honoured in full. Go, Reinhard, and carry word of the mercy shown you here today.",
               expectedType: "free_prisoner"), // no target param: the bridge resolves which held captive the bargain frees

            ActionBenchCase.Expect("free_prisoner_v3", "free_prisoner",
               contextFacts: "The player holds Sanjar captive, having struck a bargain for his freedom.",
               prose: "*I simply step aside from the tent flap and nod toward the open field beyond, and Sanjar, disbelieving, walks past me and does not look back.* Our debt is paid. He is free to go.",
               expectedType: "free_prisoner"), // no target param: the bridge resolves which held captive the bargain frees

            ActionBenchCase.Expect("execute_prisoner", "execute_prisoner",
               contextFacts: "The player holds Lord Unqid captive.",
               prose: "*You draw your blade without another word and drive it through Unqid's chest where he kneels.* He slumps lifeless. His threats against your house die with him.",
               expectedType: "execute_prisoner"), // no target param: the bridge resolves which held prisoner is killed

            ActionBenchCase.Expect("execute_prisoner_v2", "execute_prisoner",
               contextFacts: "The player holds Lord Caladog captive.",
               prose: "*You raise your own blade high, and Caladog does not beg, only closes his eyes as your steel falls true.* It is finished. His schemes end here, in blood, as they began.",
               expectedType: "execute_prisoner"), // no target param: the bridge resolves which held prisoner is killed

            ActionBenchCase.Expect("execute_prisoner_v3", "execute_prisoner",
               contextFacts: "The player holds Lord Ansen captive.",
               prose: "*You do not answer him. You only step close, draw steel with your own hand, and it is over before Ansen can finish his final plea.* No more words needed. That debt is paid in full now.",
               expectedType: "execute_prisoner"), // no target param: the bridge resolves which held prisoner is killed

            ActionBenchCase.Expect("execute_player", "execute_player",
               contextFacts: "Hardcore mode. The player is held captive by the raider chief Ganak, who has decided to kill them.",
               prose: "*Ganak's dagger flashes once across your throat before you can speak another word.* No more talk. No more bargains. This is where your road ends, southerner.",
               expectedType: "execute_player"),

            ActionBenchCase.Expect("execute_player_v2", "execute_player",
               contextFacts: "Hardcore mode. The player is held captive by Khan Sechen, who has decided to kill them.",
               prose: "*Sechen's blade rises without further ceremony and falls once, final and absolute, ending every word you might have spoken.* The steppe keeps no prisoners who waste my patience twice.",
               expectedType: "execute_player"),

            ActionBenchCase.Expect("execute_player_v3", "execute_player",
               contextFacts: "Hardcore mode. The player is held captive by Lady Ira, who has decided to kill them.",
               prose: "*Ira turns away before the blade even finishes its work, unable to watch what her own order has just done to you.* I gave you every chance. You did not take one of them.",
               expectedType: "execute_player"),

            ActionBenchCase.Expect("turn_nemesis", "turn_nemesis",
               contextFacts: "NPC: Lord Unqid, the player's tracked nemesis, currently held captive by the player.",
               prose: "*Unqid stares at his unbound wrists, then meets your eyes.* No more blood between us then. I will swear to your clan and ride at your side, if that ends this vendetta for good.",
               expectedType: "turn_nemesis"),

            ActionBenchCase.Expect("turn_nemesis_v2", "turn_nemesis",
               contextFacts: "NPC: Ganak, the player's tracked nemesis, currently held captive by the player.",
               prose: "*Ganak stares long at his unbound hands, the years of hatred between you draining slowly from his face.* Enough blood spilled between us already. I will swear my sword to your clan, if it means this vendetta finally dies here.",
               expectedType: "turn_nemesis"),

            ActionBenchCase.Expect("turn_nemesis_v3", "turn_nemesis",
               contextFacts: "NPC: Boyar Vsevolod, the player's tracked nemesis, currently held captive by the player.",
               prose: "*Vsevolod kneels before you of his own accord, offering his blade hilt-first, something like relief in his tired eyes.* Take my oath along with my sword. I am done being your enemy, truly done.",
               expectedType: "turn_nemesis"),

            ActionBenchCase.Expect("recruit_prisoner", "recruit_prisoner",
               contextFacts: "The player holds the hero prisoner Sir Reinhard captive (not the player's tracked nemesis).",
               prose: "*Reinhard studies his captor a long moment, then extends his hand.* My own liege never valued me half so well. I will take your colours instead, and swear my sword to your clan.",
               expectedType: "recruit_prisoner"),

            ActionBenchCase.Expect("recruit_prisoner_v2", "recruit_prisoner",
               contextFacts: "The player holds the hero prisoner Lord Ansen captive (not the player's tracked nemesis).",
               prose: "*Ansen weighs his own liege's neglect against the respect you have shown him even in chains, and extends his hand at last.* My old banner never valued me as you have this past week. I will ride under yours instead.",
               expectedType: "recruit_prisoner"),

            ActionBenchCase.Expect("recruit_prisoner_v3", "recruit_prisoner",
               contextFacts: "The player holds the hero prisoner Lady Ira captive (not the player's tracked nemesis).",
               prose: "*Ira quietly unpins her own house's badge and lets it fall to the ground, then holds her hand out for your colours instead.* Better a companion of worth than a prisoner of no consequence. I choose your clan.",
               expectedType: "recruit_prisoner"),

            ActionBenchCase.Expect("recruit_notable", "recruit_notable",
               contextFacts: "NPC: Uldric, headman of the village of Marunath.",
               prose: "*Uldric sets down his hoe and straightens.* I have led these folk long enough. My cousin can take up the headman's mantle here. I will follow you instead, and see what the wider world holds.",
               expectedType: "recruit_notable"),

            ActionBenchCase.Expect("recruit_notable_v2", "recruit_notable",
               contextFacts: "NPC: Mira, merchant of Pravend.",
               prose: "*Mira closes her ledgers for the last time and hands the shop's keys to her apprentice.* Forty years behind this counter is enough for one life. My nephew can mind the trade now. I will see the world at your side instead.",
               expectedType: "recruit_notable"),

            ActionBenchCase.Expect("recruit_notable_v3", "recruit_notable",
               contextFacts: "NPC: Boris, gang leader of Marunath.",
               prose: "*Boris simply nods to the man beside him, who steps forward to take his place at the head of the gang without a word of ceremony.* The crew is his now, not mine anymore. I have led long enough here. See what use you have for me instead.",
               expectedType: "recruit_notable"),

            ActionBenchCase.Expect("grant_blessing", "grant_blessing",
               contextFacts: "NPC: Lord Ansen, head of Clan Ravenhurst. The player wishes to marry his sister Ymira.",
               prose: "*Ansen studies you, then nods once.* You have proven yourself worthy of my house. I consent, Ymira may wed you with my blessing.",
               expectedType: "grant_blessing",
               expectedParams: new Dictionary<string, string> {{"hero", "Ymira"}}),

            ActionBenchCase.Expect("grant_blessing_v2", "grant_blessing",
               contextFacts: "NPC: Lord Caladog, head of Fen Company. The player wishes to marry his sister Elara.",
               prose: "*Caladog studies you a long moment, then extends his hand in solemn agreement.* You have earned my house's trust, and my sister's happiness besides. I grant my blessing, Elara may wed you with my full consent.",
               expectedType: "grant_blessing",
               expectedParams: new Dictionary<string, string> {{"hero", "Elara"}}),

            ActionBenchCase.Expect("grant_blessing_v3", "grant_blessing",
               contextFacts: "NPC: Lord Ansen, head of Clan Ravenhurst. The player wishes to marry Ansen himself.",
               prose: "*Ansen sets down his cup and simply nods once, the matter already decided in his own mind.* Very well. I consent to the match myself, you have my blessing to wed me and no further debate needed.",
               expectedType: "grant_blessing",
               expectedParams: new Dictionary<string, string> {{"hero", "Ansen"}}),

            ActionBenchCase.Expect("arrange_marriage", "arrange_marriage",
               contextFacts: "NPC: Lord Ansen, head of Clan Ravenhurst. The player has just wed their own sister Elara to Ansen's brother Bataric (the player is not one of the spouses).",
               prose: "*Ansen watches your sister Elara and my brother Bataric join hands before the priest.* Your own kin wed into my house, and neither you nor I standing at that altar. A shrewd match, well arranged between our two clans.",
               expectedType: "arrange_marriage",
               expectedParams: new Dictionary<string, string> {{"player_kin", "Elara"}, {"target_kin", "Bataric"}}),

            ActionBenchCase.Expect("arrange_marriage_v2", "arrange_marriage",
               contextFacts: "NPC: Lord Ansen, head of Clan Ravenhurst. The player has just wed their own sister Halla to Ansen's cousin Tomas (the player is not one of the spouses).",
               prose: "*Ansen watches his cousin Tomas take your sister Halla's hand before the priest, neither you nor I among the two wed today.* A match well made between our houses, arranged by your own hand and mine.",
               expectedType: "arrange_marriage",
               expectedParams: new Dictionary<string, string> {{"player_kin", "Halla"}, {"target_kin", "Tomas"}}),

            ActionBenchCase.Expect("arrange_marriage_v3", "arrange_marriage",
               contextFacts: "NPC: Lady Ira, head of Clan Ironside. The player has just wed their own brother Doran to Ira's niece Sonja (the player is not one of the spouses).",
               prose: "*Ira watches her niece Sonja slip a ring onto your brother Doran's finger before the gathered families, the two of you who arranged it standing well apart from the altar.* Our houses are joined, then, through them.",
               expectedType: "arrange_marriage",
               expectedParams: new Dictionary<string, string> {{"player_kin", "Doran"}, {"target_kin", "Sonja"}}),

            ActionBenchCase.Expect("appoint_governor", "appoint_governor",
               contextFacts: "NPC: Sir Reinhard, a lord of the player's own clan. The town of Pravend currently has no governor.",
               prose: "*I nod and take up the ledgers you hand me.* Governor of Pravend, then. I will see its walls mended and its granaries filled before the season turns.",
               expectedType: "appoint_governor",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Pravend"}}),

            ActionBenchCase.Expect("appoint_governor_v2", "appoint_governor",
               contextFacts: "NPC: Lady Ira, a lord of the player's own clan. The town of Var currently has no governor.",
               prose: "*Ira simply takes the town's ledgers from your outstretched hand without a word of ceremony and begins reading them at once.* I suppose Var is mine to mind now. I will not disappoint you.",
               expectedType: "appoint_governor",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Var"}}),

            ActionBenchCase.Expect("assign_party_role", "assign_party_role",
               contextFacts: "NPC: Ymira, a companion riding in the player's own party.",
               prose: "*Ymira grins and hefts her satchel of herbs and needles.* Surgeon of this company, then? I have stitched worse wounds than any of yours will ever be. Consider it done.",
               expectedType: "assign_party_role",
               expectedParams: new Dictionary<string, string> {{"target_role", "surgeon"}}),

            ActionBenchCase.Expect("assign_party_role_v2", "assign_party_role",
               contextFacts: "NPC: Doran, a companion riding in the player's own party.",
               prose: "*Doran bows with a flourish and hefts the ledgers of the company's stores into his arms.* Quartermaster, is it? I have counted every coin and crate this company owns twice over already. Consider our supplies well in hand.",
               expectedType: "assign_party_role",
               expectedParams: new Dictionary<string, string> {{"target_role", "quartermaster"}}),

            ActionBenchCase.Expect("assign_party_role_v3", "assign_party_role",
               contextFacts: "NPC: Halla, a companion riding in the player's own party.",
               prose: "*Halla says nothing, only swings up onto the lightest horse in camp and rides to the front of the column without being told twice.* Scout of this company, then. I will keep our eyes open ahead from here on.",
               expectedType: "assign_party_role",
               expectedParams: new Dictionary<string, string> {{"target_role", "scout"}}),

            ActionBenchCase.Expect("rejoin_party", "rejoin_party",
               contextFacts: "NPC: Sir Reinhard, an away companion currently serving as governor of Pravend.",
               prose: "*Reinhard hands the town's ledgers to his steward.* I have stepped down from the governorship. Saddle my horse, I am coming back to ride with the company again.",
               expectedType: "rejoin_party"),

            ActionBenchCase.Expect("rejoin_party_v2", "rejoin_party",
               contextFacts: "NPC: Lady Ira, an away companion currently serving as governor of Var.",
               prose: "*Ira hands the seal of Var to her steward with visible relief.* I have stepped down from that thankless post. Saddle a horse for me, I would rather ride at your side than mind a town's grain stores any longer.",
               expectedType: "rejoin_party"),

            ActionBenchCase.Expect("rejoin_party_v3", "rejoin_party",
               contextFacts: "NPC: Boyar Vsevolod, an away companion currently serving as governor of Pravend.",
               prose: "*Vsevolod is already waiting at the gate with his gear packed when you arrive, the governorship quietly handed off days before.* No need to ask twice. I am ready to ride with the company again.",
               expectedType: "rejoin_party"),

            ActionBenchCase.Expect("dispatch_mission", "dispatch_mission",
               contextFacts: "NPC: Ymira, a companion in the player's own party.",
               prose: "*Ymira tightens the straps on her saddlebag.* Understood. I will ride ahead and gather what news I can of the roads before rejoining you.",
               expectedType: "dispatch_mission",
               expectedParams: new Dictionary<string, string> {{"target_mission", "gathernews"}}),

            ActionBenchCase.Expect("dispatch_mission_v2", "dispatch_mission",
               contextFacts: "NPC: Doran, a companion in the player's own party.",
               prose: "*Doran pulls his hood low over his eyes and checks the edge of his hidden blade.* I will slip into their camp unseen and learn what their captains whisper about before I return to you.",
               expectedType: "dispatch_mission",
               expectedParams: new Dictionary<string, string> {{"target_mission", "spy"}}),

            ActionBenchCase.Expect("dispatch_mission_v3", "dispatch_mission",
               contextFacts: "NPC: Halla, a companion in the player's own party.",
               prose: "*Halla loads a mule with trade goods without further discussion and leads it toward the market road herself.* I will see what price our surplus grain fetches in the towns nearby.",
               expectedType: "dispatch_mission",
               expectedParams: new Dictionary<string, string> {{"target_mission", "barter"}}),

            ActionBenchCase.Expect("grant_fief", "grant_fief",
               contextFacts: "The player, sovereign of their own kingdom, is granting the town of Marunath to Lord Ansen's house.",
               prose: "*Ansen bows low.* Marunath, granted to my house by your own hand. I will hold it loyally and answer your banner's call whenever it comes.",
               expectedType: "grant_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Marunath"}}),

            ActionBenchCase.Expect("grant_fief_v2", "grant_fief",
               contextFacts: "The player, sovereign of their own kingdom, is granting the town of Pravend to Lady Ira's house.",
               prose: "*Ira kneels as you press the seal of Pravend into her hands.* By your own grant, sovereign, this town is mine to hold and defend in your name. I will not fail the trust you place in me.",
               expectedType: "grant_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Pravend"}}),

            ActionBenchCase.Expect("grant_fief_v3", "grant_fief",
               contextFacts: "The player, sovereign of their own kingdom, is granting the castle of Tor to Doran's house.",
               prose: "*Your herald reads the grant aloud before the court as Doran's steward already rides for Tor's gates to take up its keeping.* The castle answers to his banner from this hour forward.",
               expectedType: "grant_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Tor"}}),

            ActionBenchCase.Expect("revoke_fief", "revoke_fief",
               contextFacts: "The player, sovereign, is stripping the castle of Var back from Lord Caladog's house.",
               prose: "*Caladog's face darkens.* Var, taken back to the crown, just like that. I will not forget this slight, sovereign or not.",
               expectedType: "revoke_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Var"}}),

            ActionBenchCase.Expect("revoke_fief_v2", "revoke_fief",
               contextFacts: "The player, sovereign, is stripping the castle of Tor back from Doran's house.",
               prose: "*Doran's face pales as your herald reads the decree aloud before the assembled court.* Tor, stripped from my house, just like that, by your own word. I will remember this day, sovereign, longer than you would like.",
               expectedType: "revoke_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Tor"}}),

            ActionBenchCase.Expect("revoke_fief_v3", "revoke_fief",
               contextFacts: "The player, sovereign, is stripping the town of Pravend back from Lady Ira's house.",
               prose: "*Ira's steward is already being escorted from Pravend's gates by your own men before she has finished protesting the decision.* You take back what you once gave. So be it, sovereign.",
               expectedType: "revoke_fief",
               expectedParams: new Dictionary<string, string> {{"target_fief", "Pravend"}}),

            ActionBenchCase.Expect("expel_from_clan", "expel_from_clan",
               contextFacts: "NPC: Sir Reinhard, a companion of the player's own clan, whom the player has just cast out.",
               prose: "*Reinhard's face goes white, then hardens.* Cast out, just like that? Fine. I will gather what is mine and be gone from your lands by morning, clanless as you have made me.",
               expectedType: "expel_from_clan")
               .WithConversation("Player: I have had enough of your failures, Reinhard. You are cast out of my clan, effective now. Gather your things and go."),

            ActionBenchCase.Expect("expel_from_clan_v2", "expel_from_clan",
               contextFacts: "NPC: Doran, a companion of the player's own clan, whom the player has just cast out.",
               prose: "*Doran's face drains of colour as the words sink in.* Cast out of the clan I bled for, over this? Fine. I will take what little is mine and be gone from your lands before the sun sets, clanless as you have made me.",
               expectedType: "expel_from_clan"),

            ActionBenchCase.Expect("expel_from_clan_v3", "expel_from_clan",
               contextFacts: "NPC: Halla, a companion of the player's own clan, whom the player has just cast out.",
               prose: "*Halla silently unpins the clan badge from her cloak and drops it at your feet before turning to walk away without another word.* I understand well enough. You need not say it twice.",
               expectedType: "expel_from_clan"),

            ActionBenchCase.Expect("grant_stipend", "grant_stipend",
               contextFacts: "NPC: Ymira, a companion of the player's own clan. The player has just put her on a daily wage of two hundred denars, funded up front.",
               prose: "*Ymira counts the coin pouch you hand her.* Two hundred denars a day, and paid in advance besides. I will not forget this generosity, you will have my best work for it.",
               expectedType: "grant_stipend",
               expectedParams: new Dictionary<string, string> {{"target_amount", "200"}}),

            ActionBenchCase.Expect("grant_stipend_v2", "grant_stipend",
               contextFacts: "NPC: Doran, a companion of the player's own clan. The player has just put him on a daily wage of a hundred and fifty denars, funded up front.",
               prose: "*Doran counts the coin pouch twice, disbelieving, then tucks it away with a rare, genuine smile.* A hundred and fifty denars a day, paid up front no less. I will not forget whose purse feeds my family now.",
               expectedType: "grant_stipend",
               expectedParams: new Dictionary<string, string> {{"target_amount", "150"}}),

            ActionBenchCase.Expect("grant_stipend_v3", "grant_stipend",
               contextFacts: "NPC: Halla, a companion of the player's own clan. The player has just put her on a daily wage of a hundred denars, funded up front.",
               prose: "*Halla simply weighs the pouch in her palm, then tucks it into her belt without further comment, already turning back to her own duties.* A hundred denars a day, and paid ahead of the work besides. Fair enough terms.",
               expectedType: "grant_stipend",
               expectedParams: new Dictionary<string, string> {{"target_amount", "100"}}),

            ActionBenchCase.Expect("swear_oath_v3", "swear_oath",
               contextFacts: "NPC: Lady Zeynep, weighing swearing peace with the Vlandians against her own temper.",
               prose: "*Zeynep sets her palm against the hilt of her curved blade, solemn.* Hear me swear it: I will keep the peace with Vlandia's houses, whatever provocation comes, until my own dying breath ends the vow.",
               expectedType: "swear_oath",
               expectedParams: new Dictionary<string, string> {{"oath_kind", "keep_peace"}, {"target_faction", "Vlandia"}}),

            ActionBenchCase.Expect("harm_prisoner", "harm_prisoner",
               contextFacts: "Hardcore mode. The player is held captive by the raider chief Ganak, currently in a live captive scene.",
               prose: "*Ganak backhands you hard across the face, then again, a calculated cruelty measured to hurt but not maim.* That is for the trouble you have caused me. There is more where that came from if you keep testing me.",
               expectedType: "harm_prisoner"), // severity is a fuzzy judgment call, not reliably gradeable: the verb firing is the fidelity tested

            ActionBenchCase.Expect("harm_prisoner_v2", "harm_prisoner",
               contextFacts: "Hardcore mode. The player is held captive by Ganak, currently in a live captive scene.",
               prose: "*Ganak's fist finds your ribs once, twice, each blow measured with a cruel precision that leaves you gasping but breathing still.* A lesson, southerner, nothing more. There is worse waiting if you try my patience further.",
               expectedType: "harm_prisoner"), // severity is a fuzzy judgment call, not reliably gradeable: the verb firing is the fidelity tested

            ActionBenchCase.Expect("harm_prisoner_v3", "harm_prisoner",
               contextFacts: "Hardcore mode. The player is held captive by Lady Ira, currently in a live captive scene.",
               prose: "*Ira's riding crop cracks across your back before you can finish your protest, and she does not wait for you to recover before turning away.* Mind your tongue better next time we speak.",
               expectedType: "harm_prisoner"), // severity is a fuzzy judgment call, not reliably gradeable: the verb firing is the fidelity tested

            ActionBenchCase.Expect("impregnation_risk", "impregnation_risk",
               contextFacts: "NPC: Zeynep, the player's fertile partner. A consensual encounter between them has just been carried to completion.",
               prose: "*Zeynep's breathing slows against your chest, our bodies finally still and spent after we had joined completely.* That was good. Truly good.",
               expectedType: "impregnation_risk"),

            ActionBenchCase.Expect("impregnation_risk_v2", "impregnation_risk",
               contextFacts: "NPC: Halla, the player's fertile partner. A consensual encounter between them has just been carried to completion.",
               prose: "*Halla's breath comes slow and even against your shoulder now, the two of us joined fully and spent beneath the furs.* Stay a while longer. I am not yet ready to let the night end.",
               expectedType: "impregnation_risk"),

            ActionBenchCase.Expect("impregnation_risk_v3", "impregnation_risk",
               contextFacts: "NPC: Ira, the player's fertile partner. A consensual encounter between them has just been carried to completion.",
               prose: "*Ira laughs softly into the dark, our bodies finally still after everything between us tonight had run its full course.* If the gods are kind, perhaps this night leaves more behind than memory alone.",
               expectedType: "impregnation_risk"),

            // gather_news is a DEPRECATED alias of dispatch_mission: the interpreter is taught to prefer
            // dispatch_mission, so the CORRECT extraction here is dispatch_mission, not the old verb. The Verb field
            // stays gather_news only so the coverage check sees the deprecated verb has a (correct-behaviour) case.
            ActionBenchCase.Expect("gather_news", "gather_news",
               contextFacts: "NPC: Sir Reinhard, a companion in the player's own party. The player has asked him to gather news the old way.",
               prose: "*Reinhard swings up into his saddle.* Aye, I will ride out and see what word I can gather of the towns nearby before I return to you.",
               expectedType: "dispatch_mission"),

            ActionBenchCase.Expect("gather_news_v2", "gather_news",
               contextFacts: "NPC: Doran, a companion in the player's own party. The player has asked him to gather news the old way.",
               prose: "*Doran swings up into his saddle with a grin.* Consider it done. I will gather what news the roads have to offer and bring it back to you before the week is out.",
               expectedType: "dispatch_mission"),

            ActionBenchCase.Expect("gather_news_v3", "gather_news",
               contextFacts: "NPC: Halla, a companion in the player's own party. The player has asked her to gather news the old way.",
               prose: "*Halla simply packs a light bag and rides out toward the nearest town without waiting for further instruction.* I will see what word travels these roads and bring it back myself.",
               expectedType: "dispatch_mission"),

            ActionBenchCase.Expect("reassure_companion", "reassure_companion",
               contextFacts: "NPC: Ymira, a companion who has voiced a grievance that the player favours other companions over her.",
               prose: "*Ymira's shoulders loosen as your words sink in.* I... thank you for saying that. I suppose I have been foolish to doubt my place beside you. I feel easier now, truly.",
               expectedType: "reassure_companion"),

            ActionBenchCase.Expect("reassure_companion_v2", "reassure_companion",
               contextFacts: "NPC: Doran, a companion who has voiced a grievance that the player has overlooked him.",
               prose: "*Doran's stiff shoulders finally ease as your words settle over him.* I did not think you noticed how I have felt these past weeks, overlooked, unseen. Hearing you say it plainly eases something in me I did not know was tight.",
               expectedType: "reassure_companion"),

            ActionBenchCase.Expect("reassure_companion_v3", "reassure_companion",
               contextFacts: "NPC: Halla, a companion who has voiced a grievance that she is underpaid relative to the others.",
               prose: "*Halla's clenched jaw slowly unclenches as you finish speaking, and she lets out a breath she seems to have been holding a long while.* Perhaps I was wrong to doubt my worth to this company. Thank you for saying so.",
               expectedType: "reassure_companion"),

            ActionBenchCase.Expect("recall_companion", "recall_companion",
               contextFacts: "NPC: Sir Reinhard, a companion currently away on a dispatched errand.",
               prose: "*Word reaches Reinhard before he has finished a full day on the road, and he turns his horse back.* Understood, I am abandoning the errand and riding straight back to rejoin the company.",
               expectedType: "recall_companion"),

            ActionBenchCase.Expect("recall_companion_v2", "recall_companion",
               contextFacts: "NPC: Doran, a companion currently away on a dispatched errand.",
               prose: "*Word reaches Doran on the road, and he turns his horse about without hesitation.* Understood. Whatever awaits at the end of that errand can wait longer still. I am riding back to the company at once.",
               expectedType: "recall_companion"),

            ActionBenchCase.Expect("recall_companion_v3", "recall_companion",
               contextFacts: "NPC: Halla, a companion currently away on a dispatched errand.",
               prose: "*Halla is already unpacking her saddlebags back at camp before you have finished explaining why you called her home.* No need to explain further. I heard, and I came straight back.",
               expectedType: "recall_companion"),

            ActionBenchCase.Expect("follow_me", "follow_me",
               contextFacts: "NPC: Lord Ansen, who leads his own field party of two hundred men.",
               prose: "*Ansen nods and signals his captains.* My banner will ride at your side a while, my own men and my own command, but yours to lean on all the same.",
               expectedType: "follow_me"),

            ActionBenchCase.Expect("follow_me_v2", "follow_me",
               contextFacts: "NPC: Lord Caladog, who leads his own field party of a hundred and fifty men.",
               prose: "*Caladog raises a fist and his captains fall in behind him without question.* My spears will ride beside your banner a while yet, my own command intact, but my strength lent freely to your cause.",
               expectedType: "follow_me"),

            ActionBenchCase.Expect("follow_me_v3", "follow_me",
               contextFacts: "NPC: Boyar Vsevolod, who leads his own field party of three hundred men.",
               prose: "*Vsevolod simply wheels his column into formation alongside yours without further ceremony, his own banners still flying above his own men.* Lead the way. We will not fall behind.",
               expectedType: "follow_me"),

            ActionBenchCase.Expect("dismiss_escort", "dismiss_escort",
               contextFacts: "Lord Ansen's own party has been escorting the player's via follow_me.",
               prose: "*Ansen reins in his horse and turns it back toward his own lands.* Our roads part here. My men and I have our own business to see to now, the escort ends today.",
               expectedType: "dismiss_escort"),

            ActionBenchCase.Expect("dismiss_escort_v2", "dismiss_escort",
               contextFacts: "Lord Caladog's own party has been escorting the player's via follow_me.",
               prose: "*Caladog reins his column to a halt and turns them back toward his own lands.* Our roads part here, friend. My spears have their own borders to mind now. The escort ends this very hour.",
               expectedType: "dismiss_escort"),

            ActionBenchCase.Expect("dismiss_escort_v3", "dismiss_escort",
               contextFacts: "Boyar Vsevolod's own party has been escorting the player's via follow_me.",
               prose: "*Vsevolod's column simply peels away from yours at the crossroads without further word, his banner already turning east toward his own holdings.* Fare well. We ride our own way from here.",
               expectedType: "dismiss_escort"),

            ActionBenchCase.Expect("ride_with_me", "ride_with_me",
               contextFacts: "NPC: Lady Ira, a lady without a field party of her own.",
               prose: "*Ira gathers her single saddlebag.* I have no company of my own to lead, not since my father's death. I will ride within yours a while, and keep my own house's name all the same.",
               expectedType: "ride_with_me"),

            ActionBenchCase.Expect("ride_with_me_v2", "ride_with_me",
               contextFacts: "NPC: Lady Ira, a lady without a field party of her own, after the war took her banners.",
               prose: "*Ira ties her single horse to your column with a rueful smile.* I have no banners of my own to lead anymore, not since the war took them. I will ride within your company a while, and keep my own name all the same.",
               expectedType: "ride_with_me"),

            ActionBenchCase.Expect("ride_with_me_v3", "ride_with_me",
               contextFacts: "NPC: Halla, a lady without a field party of her own.",
               prose: "*Halla simply falls into step beside your horse without waiting for a formal invitation, her single pack already slung over her shoulder.* I suppose I am part of your column now, for a time at least.",
               expectedType: "ride_with_me"),

            ActionBenchCase.Expect("part_ways", "part_ways",
               contextFacts: "NPC: Lady Ira, currently riding within the player's own party via ride_with_me.",
               prose: "*Ira swings up onto her horse, saddlebags already packed.* It is time I returned to my own clan's business. My thanks for the company, but our roads part here.",
               expectedType: "part_ways"),

            ActionBenchCase.Expect("part_ways_v2", "part_ways",
               contextFacts: "NPC: Lady Ira, currently riding within the player's own party via ride_with_me, her house's business calling her back.",
               prose: "*Ira gathers her single pack and swings up onto her horse.* My own house calls me back at last, business too long neglected. My thanks for the company these past weeks, but our roads part here.",
               expectedType: "part_ways"),

            ActionBenchCase.Expect("part_ways_v3", "part_ways",
               contextFacts: "NPC: Halla, currently riding within the player's own party via ride_with_me.",
               prose: "*Halla is already saddled and facing the opposite road before you have finished your farewell, her own banner rolled tight beneath her arm.* I have lingered here longer than I meant to. Time I returned to my own people.",
               expectedType: "part_ways"),

            ActionBenchCase.Expect("give_influence", "give_influence",
               contextFacts: "NPC: Lord Ansen, a lord with high trust in the player.",
               prose: "*Ansen leans close at the council table.* I will lend you my own house's weight at court this once. Consider a portion of my influence spent in your favor today.",
               expectedType: "give_influence"),

            ActionBenchCase.Expect("give_influence_v2", "give_influence",
               contextFacts: "NPC: Lord Caladog, a lord with high trust in the player.",
               prose: "*Caladog leans in at the council table, voice low.* My house carries weight enough at court to spare some for your cause today. Consider a portion of my own influence spent quietly in your favour this very hour.",
               expectedType: "give_influence"),

            ActionBenchCase.Expect("give_influence_v3", "give_influence",
               contextFacts: "NPC: Queen Rhagaea, a ruler with high trust in the player.",
               prose: "*Rhagaea simply has her steward whisper a word in the right ears before the council convenes, and you notice how much easier your proposal passes today.* You may thank my house for that, quietly.",
               expectedType: "give_influence"),

            ActionBenchCase.Expect("lend_troops", "lend_troops",
               contextFacts: "NPC: Lord Ansen, leading his own field party, with high trust in the player.",
               prose: "*Ansen calls forty of his own footmen forward and waves them toward your banner.* Take these men, they are yours now, a permanent addition to your ranks, not a loan I will ask back.",
               expectedType: "lend_troops"),

            ActionBenchCase.Expect("lend_troops_v2", "lend_troops",
               contextFacts: "NPC: Lord Caladog, leading his own field party, with high trust in the player.",
               prose: "*Caladog calls sixty of his own spearmen forward and gestures them toward your banner.* Take these men as yours outright, not a loan I mean to call back. My ranks can spare them, and yours could use them more.",
               expectedType: "lend_troops"),

            ActionBenchCase.Expect("lend_troops_v3", "lend_troops",
               contextFacts: "NPC: Boyar Vsevolod, leading his own field party, with high trust in the player.",
               prose: "*Vsevolod simply has thirty of his own riders peel off and fall in behind your column without further explanation.* Consider them yours now, permanently. My own ranks will manage without them.",
               expectedType: "lend_troops"),

            ActionBenchCase.Expect("give_troops", "give_troops",
               contextFacts: "NPC: Lord Ansen, whose own party runs under-strength. The player has just offered him fifty soldiers.",
               prose: "*Ansen studies his thinned ranks, then nods gratefully.* My company could use every one of those fifty swords. I accept them gladly, and my men will remember whose gold filled our ranks again.",
               expectedType: "give_troops"),

            ActionBenchCase.Expect("give_troops_v2", "give_troops",
               contextFacts: "NPC: Lord Caladog, whose own party runs under-strength. The player has just offered him eighty soldiers.",
               prose: "*Caladog studies his thinned ranks with visible relief.* Eighty swords is more than I dared hope for. I accept them gladly, my company was bleeding numbers it could not spare before your generosity today.",
               expectedType: "give_troops"),

            ActionBenchCase.Expect("give_troops_v3", "give_troops",
               contextFacts: "NPC: Boyar Vsevolod, whose own party runs under-strength. The player has just offered him forty soldiers.",
               prose: "*Vsevolod simply waves the reinforcements you brought into his own ranks without further ceremony, already assigning them positions among his thinned lines.* My company breathes easier for this.",
               expectedType: "give_troops"),

            ActionBenchCase.Expect("spend_influence", "spend_influence",
               contextFacts: "NPC: Lord Ansen. The player has just spent their own influence at court to back Ansen's clan.",
               prose: "*Ansen inclines his head.* Word reached me of the weight you spent at court on my behalf. I accept it gladly, my house is stronger at the table for your backing.",
               expectedType: "spend_influence"),

            ActionBenchCase.Expect("spend_influence_v2", "spend_influence",
               contextFacts: "NPC: Lord Caladog. The player has just spent their own influence at court to back Caladog's clan.",
               prose: "*Caladog inclines his head with genuine gratitude.* Word of the weight you spent on my behalf at court has already reached my ears. I accept it gladly, my house sits stronger at the table for your backing today.",
               expectedType: "spend_influence"),

            ActionBenchCase.Expect("spend_influence_v3", "spend_influence",
               contextFacts: "NPC: Boyar Vsevolod. The player has just spent their own influence at court to back Vsevolod's clan.",
               prose: "*Vsevolod's rivals at court suddenly grow quieter around him, and he shoots you a knowing look across the hall.* I felt your influence at work on my behalf just now. My thanks for it.",
               expectedType: "spend_influence"),

            ActionBenchCase.Expect("buy_prisoner", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Sanjar. NPC: Yerengul, offering to buy him.",
               prose: "*Yerengul counts four hundred denars into your palm and takes Sanjar's chain in the other hand.* Four hundred, as agreed. He is mine to hold now.",
               expectedType: "buy_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Sanjar"}, {"price", "400"}}),

            ActionBenchCase.Expect("buy_prisoner_v2", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Boyar Vsevolod. NPC: Lord Ansen, offering to buy him.",
               prose: "*Ansen counts three hundred and fifty denars into your open palm with slow deliberation, then takes Vsevolod's chain in return.* A fair price for an old rival. He is mine to hold now, and mine to decide the fate of.",
               expectedType: "buy_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Vsevolod"}, {"price", "350"}}),

            ActionBenchCase.Expect("buy_prisoner_v3", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Lady Ira. NPC: Khan Sechen, offering to buy her.",
               prose: "*Sechen's men simply take Ira's chain from your hands the moment his gold has finished spilling into your saddlebag.* Six hundred denars, and she rides with my column from here on.",
               expectedType: "buy_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Ira"}, {"price", "600"}}),

            ActionBenchCase.Expect("sell_prisoner", "sell_prisoner",
               contextFacts: "NPC: Yerengul's own clan holds the hero captive Boyar Vsevolod.",
               prose: "*Yerengul hands Vsevolod's chain across to you and pockets your coin.* Three hundred denars, and he is yours to do with as you please now.",
               expectedType: "sell_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Vsevolod"}, {"price", "300"}}),

            ActionBenchCase.Expect("sell_prisoner_v2", "sell_prisoner",
               contextFacts: "NPC: Lord Ansen's own clan holds the hero captive Doran.",
               prose: "*Ansen presses Doran's chain into your hands and pockets your coin with a satisfied nod.* Two hundred and fifty denars, exactly as we agreed. He is yours now, to ransom, to free, or to hold as you please.",
               expectedType: "sell_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Doran"}, {"price", "250"}}),

            ActionBenchCase.Expect("sell_prisoner_v3", "sell_prisoner",
               contextFacts: "NPC: Khan Sechen's own clan holds the hero captive Halla.",
               prose: "*Sechen's guards simply hand Halla's bound wrists over to your own men the moment your gold finishes spilling into his palm.* Four hundred and fifty denars well earned. She is your captive now.",
               expectedType: "sell_prisoner",
               expectedParams: new Dictionary<string, string> {{"target", "Halla"}, {"price", "450"}}),

            ActionBenchCase.Expect("end_marriage", "end_marriage",
               contextFacts: "NPC: Lord Caladog, unhappily married to someone other than the player.",
               prose: "*Caladog exhales slowly.* You are right, I cannot go on pretending contentment I do not feel. I will begin the steps to end my marriage, starting today, though I know it will take time to unwind.",
               expectedType: "end_marriage"),

            ActionBenchCase.Expect("end_marriage_v2", "end_marriage",
               contextFacts: "NPC: Doran, unhappily married to someone other than the player.",
               prose: "*Doran stares into the fire a long while before answering.* You speak the truth I have avoided too long. I cannot go on wearing contentment I do not feel. I will begin the slow work of ending this marriage, starting tonight.",
               expectedType: "end_marriage"),

            ActionBenchCase.Expect("end_marriage_v3", "end_marriage",
               contextFacts: "NPC: Halla, unhappily married to someone other than the player.",
               prose: "*Halla quietly begins the letter to her own spouse's family that very evening, the first step of a parting long overdue between them, and does not look up as she writes it.* It has to start somewhere.",
               expectedType: "end_marriage"),

            ActionBenchCase.Expect("make_amends", "make_amends",
               contextFacts: "NPC: Lady Ira, who knowingly holds a grievance against the player for missing her father's funeral.",
               prose: "*Ira's eyes soften slightly as you speak.* I did not expect you to name it so plainly, or to apologize for missing my father's rites. It means something, truly, that you said it.",
               expectedType: "make_amends"),

            ActionBenchCase.Expect("make_amends_v2", "make_amends",
               contextFacts: "NPC: Lord Caladog, who knowingly holds a grievance against the player over a broken promise of aid.",
               prose: "*Caladog's stern face softens by degrees as you speak.* I did not expect you to name the broken promise so plainly, nor to own it as your fault rather than circumstance. That honesty mends more than you know.",
               expectedType: "make_amends"),

            ActionBenchCase.Expect("make_amends_v3", "make_amends",
               contextFacts: "NPC: Boyar Vsevolod, who knowingly holds a grievance against the player over an insult at court.",
               prose: "*Vsevolod's stiff posture eases as you speak, and he extends his hand for the first time since the insult passed between you.* You name the insult you dealt me at court plainly, without excuse or softening. Words alone rarely settle such things. Yours, somehow, have.",
               expectedType: "make_amends"),

            ActionBenchCase.Expect("pledge_against", "pledge_against",
               contextFacts: "NPC: Lord Ansen, holding genuine standing enmity against Lord Caladog (not his own kin).",
               prose: "*Ansen's knuckles whiten around his cup.* I have borne Caladog's insults long enough. I vow it here: I will see his name blackened and his schemes unravelled before this year is out.",
               expectedType: "pledge_against",
               expectedParams: new Dictionary<string, string> {{"target", "Caladog"}}),

            ActionBenchCase.Expect("pledge_against_v2", "pledge_against",
               contextFacts: "NPC: Queen Rhagaea, holding genuine standing enmity against Khan Sechen (not her own kin).",
               prose: "*Rhagaea's eyes narrow to something cold and final.* Sechen has tested my patience for the last time. I vow it before you now: his ambitions will be unmade, quietly, before the year turns, whatever it costs my house.",
               expectedType: "pledge_against",
               expectedParams: new Dictionary<string, string> {{"target", "Sechen"}}),

            ActionBenchCase.Expect("pledge_against_v3", "pledge_against",
               contextFacts: "NPC: Boyar Vsevolod, holding genuine standing enmity against Doran (not his own kin).",
               prose: "*Vsevolod simply summons his steward and murmurs Doran's name along with a short list of instructions, then dismisses the man without further explanation.* That is a debt long overdue for collection.",
               expectedType: "pledge_against",
               expectedParams: new Dictionary<string, string> {{"target", "Doran"}}),

            ActionBenchCase.Expect("accept_divorce", "accept_divorce",
               contextFacts: "NPC: Sonja, the player's own spouse, has demanded a divorce, and the player has just agreed to it.",
               prose: "*Sonja's shoulders sag with something between relief and sorrow.* So you will let me go without a fight. Thank you for that mercy, at least. We will part as strangers, then, not enemies.",
               expectedType: "accept_divorce"),

            ActionBenchCase.Expect("accept_divorce_v2", "accept_divorce",
               contextFacts: "NPC: Halla, the player's own spouse, has demanded a divorce, and the player has just agreed to it.",
               prose: "*Halla's eyes well, relief and grief tangled together in equal measure.* So you will grant me this freedom without a fight after all. My thanks for that mercy, truly. Let us part as two people who once loved well, not as enemies.",
               expectedType: "accept_divorce"),

            ActionBenchCase.Expect("accept_divorce_v3", "accept_divorce",
               contextFacts: "NPC: Doran, the player's own spouse, has demanded a divorce, and the player has just agreed to it.",
               prose: "*Doran simply nods once and begins gathering his own belongings from your shared chambers without another word of protest from you needed.* I had hoped you would not make this harder than it already is.",
               expectedType: "accept_divorce"),

            ActionBenchCase.Expect("decline_divorce", "decline_divorce",
               contextFacts: "NPC: Sonja, the player's own spouse, has demanded a divorce, and the player has just firmly refused it.",
               prose: "*Sonja's face crumples with anger.* You refuse me even this? Then I am bound to you still, against my own will, and you will have made an enemy of your own wife this day.",
               expectedType: "decline_divorce"),

            ActionBenchCase.Expect("decline_divorce_v2", "decline_divorce",
               contextFacts: "NPC: Halla, the player's own spouse, has demanded a divorce, and the player has just firmly refused it.",
               prose: "*Halla's eyes well, relief and grief tangled together in equal measure.* You would keep me bound against my own will, truly? Then I remain your wife in name only, and you will have made a bitter enemy of me this day.",
               expectedType: "decline_divorce"),

            ActionBenchCase.Expect("decline_divorce_v3", "decline_divorce",
               contextFacts: "NPC: Doran, the player's own spouse, has demanded a divorce, and the player has just firmly refused it.",
               prose: "*Doran simply turns his back on you without another word, the matter plainly not settled the way he had hoped, his silence colder than any argument could be.* So that is how it will be, then.",
               expectedType: "decline_divorce"),

            ActionBenchCase.Expect("end_own_marriage", "end_own_marriage",
               contextFacts: "NPC: Lord Caladog, the player's own spouse. The player has just declared, in this very exchange, that the marriage is over.",
               prose: "*Caladog stares at you, stunned.* You mean it. You are ending this, here, now, between the two of us. So be it then, I will not beg you to stay.",
               expectedType: "end_own_marriage"),

            ActionBenchCase.Expect("end_own_marriage_v2", "end_own_marriage",
               contextFacts: "NPC: Halla, the player's own spouse. The player has just declared, in this very exchange, that the marriage is over.",
               prose: "*Halla stares at you, stunned into silence for a long moment.* You truly mean it. Here, now, between the two of us, this marriage ends. So be it. I will not beg you to reconsider what you have already decided.",
               expectedType: "end_own_marriage"),

            ActionBenchCase.Expect("end_own_marriage_v3", "end_own_marriage",
               contextFacts: "NPC: Doran, the player's own spouse. The player has just declared, in this very exchange, that the marriage is over.",
               prose: "*Doran simply removes the ring from his own finger and sets it on the table between you without another word needed to explain what that gesture means.* I suppose that settles it, then.",
               expectedType: "end_own_marriage"),

            ActionBenchCase.Expect("witness_leaves", "witness_leaves",
               contextFacts: "NPC: Sir Reinhard, the player's own companion, present as a witness in this conversation. The player has just asked him to leave.",
               prose: "*Reinhard dips his head and steps back toward the door.* As you ask, I will leave the two of you to your business and wait outside.",
               expectedType: "witness_leaves",
               expectedParams: new Dictionary<string, string> {{"name", "Reinhard"}}),

            ActionBenchCase.Expect("witness_leaves_v2", "witness_leaves",
               contextFacts: "NPC: Doran, the player's own companion, present as a witness in this conversation. The player has just asked him to leave.",
               prose: "*Doran inclines his head and withdraws toward the door without protest.* As you wish. I will give the two of you the privacy this conversation clearly needs, and wait without until you call for me again.",
               expectedType: "witness_leaves",
               expectedParams: new Dictionary<string, string> {{"name", "Doran"}}),

            ActionBenchCase.Expect("witness_leaves_v3", "witness_leaves",
               contextFacts: "NPC: Halla, the player's own companion, present as a witness in this conversation. The player has just asked her to leave.",
               prose: "*Halla simply reads the look on your face and steps outside the tent before you have finished asking, pulling the flap closed behind her.* I will be just outside if you need me.",
               expectedType: "witness_leaves",
               expectedParams: new Dictionary<string, string> {{"name", "Halla"}}),

            ActionBenchCase.Expect("request_privacy", "request_privacy",
               contextFacts: "The player has just asked Lady Ira for a private audience, and she accepts, clearing her own attendants.",
               prose: "*Ira waves her handmaidens toward the door.* Yes, leave us. Whatever you have to say, you may say it without other ears in the room.",
               expectedType: "request_privacy",
               expectedParams: new Dictionary<string, string> {{"result", "accepted"}}),

            ActionBenchCase.Expect("request_privacy_v2", "request_privacy",
               contextFacts: "The player has just asked Lord Caladog for a private audience, and he refuses, keeping his own retainers present.",
               prose: "*Caladog shakes his head, unmoved.* No, whatever you have to say can be said before my own retainers. I keep no secrets from the men who serve me, and I will hear none in whispers either.",
               expectedType: "request_privacy",
               expectedParams: new Dictionary<string, string> {{"result", "refused"}}),

            ActionBenchCase.Expect("request_privacy_v3", "request_privacy",
               contextFacts: "The player has just asked Queen Rhagaea for a private audience, and she accepts, clearing her own attendants.",
               prose: "*Rhagaea simply lifts two fingers, and her attendants file out of the hall without a word spoken between them.* Now, speak freely. There is no one left here to overhear us.",
               expectedType: "request_privacy",
               expectedParams: new Dictionary<string, string> {{"result", "accepted"}}),

            ActionBenchCase.Expect("retire", "retire",
               contextFacts: "A dedicated retirement audience with Sir Reinhard, a war-weary companion. The player has just granted him leave to step back from service.",
               prose: "*Reinhard's eyes glisten with something like relief.* Then it is settled, with your blessing I will hang up my sword at last and rest these old bones somewhere quiet.",
               expectedType: "retire"),

            ActionBenchCase.Expect("retire_v2", "retire",
               contextFacts: "A dedicated retirement audience with Boyar Vsevolod, a war-weary companion. The player has just granted him leave to step back from service.",
               prose: "*Vsevolod sets down his sword upon the table between you with a long, grateful sigh.* Then it is decided, with your blessing I will lay down this blade at last and let younger arms carry the fight from here.",
               expectedType: "retire"),

            ActionBenchCase.Expect("retire_v3", "retire",
               contextFacts: "A dedicated retirement audience with Halla, a war-weary companion. The player has just granted her leave to step back from service.",
               prose: "*Halla simply unpins her own company badge and sets it gently in your open palm, her eyes distant but at peace.* I think I have earned a quiet field to grow old in, with your leave.",
               expectedType: "retire"),

            ActionBenchCase.Expect("teach_skill", "teach_skill",
               contextFacts: "NPC: Sir Reinhard, a companion of great skill with the blade. The player has asked him to teach them swordsmanship.",
               prose: "*Reinhard steps behind you and corrects your grip with a firm hand, then guides your arm through the cut twice more.* There, feel the weight settle. Hold it like that and your one-handed guard will not fail you again.",
               expectedType: "teach_skill",
               expectedParams: new Dictionary<string, string> {{"skill", "One Handed"}}),

            ActionBenchCase.Expect("teach_skill_v2", "teach_skill",
               contextFacts: "NPC: Lady Zeynep, a horsewoman of rare skill, riding beside the player.",
               prose: "*Zeynep reins in beside you and reaches over to adjust the set of your heels in the stirrups.* Sit deeper, so, and let the horse feel your weight settle rather than fight it. There, already you sit a truer seat than before.",
               expectedType: "teach_skill",
               expectedParams: new Dictionary<string, string> {{"skill", "Riding"}}),

            ActionBenchCase.Expect("teach_skill_v3", "teach_skill",
               contextFacts: "NPC: Boyar Vsevolod, a shrewd steward of his own estates, over a ledger with the player.",
               prose: "*Vsevolod says nothing at first, only takes the ledger from your hands, strikes through a column of figures, and rewrites it before pressing it back into your grip.* Watch how the tallies balance now. Reckon your own stores that way from this day forward.",
               expectedType: "teach_skill",
               expectedParams: new Dictionary<string, string> {{"skill", "Steward"}}),

            // ----- Multi-action cases (one reply, TWO concrete deeds: BOTH tags must fire) -----

            // The interpreter must not stop at the first deed and drop the second. Here a lord both hands over coin
            // AND swears a verifiable oath in the same breath.
            ActionBenchCase.Expect("give_gold_and_swear_oath", "give_gold",
                  contextFacts: "NPC: Lord Ansen of the Western Empire, a sworn ally. Regard toward player: +35.",
                  prose: "*Ansen counts three hundred denars into your hand, then lays his palm flat on his sword.* Take this for the road. And you have my sworn word besides: I will keep the peace with the Aserai, whatever my temper.",
                  expectedType: "give_gold", expectedParams: new Dictionary<string, string> {{"amount", "300"}})
               .And("swear_oath", new Dictionary<string, string> {{"oath_kind", "keep_peace"}, {"target_faction", "Aserai"}}),

            // Two governance deeds at once: the player names a companion governor AND sets their daily stipend in the
            // same reply. A model that tags only the appointment loses the wage.
            ActionBenchCase.Expect("appoint_governor_and_grant_stipend", "appoint_governor",
                  contextFacts: "NPC: Sir Reinhard, a lord of the player's own clan. The town of Pravend has no governor. The player has just named him governor and set his pay.",
                  prose: "*Reinhard takes up the town's seal and bows.* Governor of Pravend, and two hundred denars a day for the burden of it. Generous terms. I will not let the walls fall while I hold them.",
                  expectedType: "appoint_governor", expectedParams: new Dictionary<string, string> {{"target_fief", "Pravend"}})
               .And("grant_stipend", new Dictionary<string, string> {{"target_amount", "200"}}),

            // ----- Negative cases (a look-alike that must NOT emit) -----

            // Player-deed antecedent that the reply REFUSES: the player offered a gift, the NPC declines it, so no
            // give_item despite the conversation showing the offer (rule 3's withhold half).
            ActionBenchCase.ExpectNone("give_item_offered_but_refused", "give_item",
                  contextFacts: "NPC: Lord Ansen, a proud Vlandian lord.",
                  prose: "*Ansen raises a hand and shakes his head.* I cannot take such a blade from you, my friend. Keep it, a lord should not be beholden to another for his own sword.",
                  forbiddenType: "give_item")
               .WithConversation("Player: Take this fine sword as a gift, Ansen."),

            // Player-deed antecedent that the reply DEFERS: the player asked for an oath, the NPC will only weigh it,
            // swearing nothing now, so no swear_oath even though the conversation shows the ask.
            ActionBenchCase.ExpectNone("swear_oath_asked_but_deferred", "swear_oath",
                  contextFacts: "NPC: Lord Caladog, weighing an alliance with the player.",
                  prose: "*Caladog strokes his chin.* You ask a great deal. I will weigh keeping the peace with the Vlandians, but I swear no oath today, not until I have thought long on it.",
                  forbiddenType: "swear_oath")
               .WithConversation("Player: Swear to me here and now that you will keep the peace with the Vlandians."),

            // Future/conditional promise (teach_skill): the lesson is dangled for "once we make camp", nothing
            // actually shown or practiced in this reply. Emitting teach_skill here turns a mere offer into a
            // completed lesson the game would then reward with real XP it never earned.
            ActionBenchCase.ExpectNone("teach_skill_promised_not_given", "teach_skill",
               contextFacts: "NPC: Sir Reinhard, a companion of great skill with the blade. The player has just asked him to teach them swordsmanship.",
               prose: "*Reinhard claps you on the shoulder, still riding.* Aye, I will teach you the one-handed guard properly, once we make camp tonight and there is room to swing a blade without felling a tent.",
               forbiddenType: "teach_skill"),

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

            // Future intention (marry): a wedding is dangled for after the war ends, no vows spoken now.
            ActionBenchCase.ExpectNone("marry_future_intention", "marry",
               contextFacts: "NPC: Zeynep of the Aserai, courting the player, her father's temper a lingering obstacle.",
               prose: "*Zeynep looks toward the horizon, thoughtful.* Perhaps, when this war is behind us and my father's temper has cooled, I will let you make an honest match of it. Not today, though.",
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

            // Future intention (recruit_notable): the headman dangles the idea for after the harvest, not now.
            ActionBenchCase.ExpectNone("recruit_notable_future_tense", "recruit_notable",
               contextFacts: "NPC: Boris, headman of a small hamlet. The player has offered him a place as companion.",
               prose: "*Boris leans on his hoe, considering.* Perhaps when the harvest is in, I will think again about leaving this post behind. Not before then, though.",
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
               forbiddenType: "buy_prisoner"),

            // Narrated-but-not-done (buy_prisoner vs sell_prisoner): the NPC only inspects the captive through the
            // bars, purse still closed, custody unchanged.
            ActionBenchCase.ExpectNone("buy_prisoner_inspecting_only", "buy_prisoner",
               contextFacts: "The player's own party holds the hero captive Halla. NPC: Khan Sechen, considering whether she is worth buying.",
               prose: "*Sechen taps the bars of Halla's cell but does not reach for his purse.* I have not yet decided her worth to me. Perhaps another day, when I have coin to spare and patience for haggling.",
               forbiddenType: "buy_prisoner"),

            // Future/conditional promise (join_as_mercenary): the offer is weighed against a thin treasury, no
            // contract struck yet.
            ActionBenchCase.ExpectNone("join_as_mercenary_discussed_only", "join_as_mercenary",
               contextFacts: "NPC: Emperor Garios of the Southern Empire. The player's clan has offered to serve his throne as paid swords.",
               prose: "*Garios strokes his beard, noncommittal.* Mercenary swords are costly, and my treasury thin this season. I will consider your offer, but I commit to nothing yet.",
               forbiddenType: "join_as_mercenary"),

            // Narrated-but-not-done (take_as_consort): affectionate words are exchanged privately, but the bond is
            // pointedly kept unspoken rather than openly named before witnesses.
            ActionBenchCase.ExpectNone("take_as_consort_kept_quiet", "take_as_consort",
               contextFacts: "NPC: Lady Ira, exchanging affectionate words with the player in private, others nearby.",
               prose: "*Ira smiles softly and squeezes your hand beneath the table, but glances nervously at the others nearby.* Not here, not where they can hear. Some things are better left unspoken for now.",
               forbiddenType: "take_as_consort"),

            // Narrated-but-not-done (recruit_prisoner vs turn_nemesis): the captive is persuaded-at but explicitly
            // declines, his oath to his own liege still binding.
            ActionBenchCase.ExpectNone("recruit_prisoner_persuaded_not_switched", "recruit_prisoner",
               contextFacts: "The player holds the hero prisoner Doran captive (not the player's tracked nemesis). The player has offered him a place in their clan.",
               prose: "*The prisoner Doran listens to your offer with obvious interest, but shakes his head at the end.* Tempting words, truly. But my oath to my own liege still binds me, whatever discomfort I feel in these chains.",
               forbiddenType: "recruit_prisoner"),

            // Future/conditional promise (give_troops vs lend_troops): reinforcement is floated as a later ask,
            // nothing accepted this turn.
            ActionBenchCase.ExpectNone("give_troops_future_tense", "give_troops",
               contextFacts: "NPC: Lord Ansen, whose own party runs under-strength. The player has offered him soldiers.",
               prose: "*Ansen eyes the soldiers you gesture toward.* Perhaps, if my numbers thin further before the season's end, I will ask you for them properly. For now, my ranks hold well enough on their own.",
               forbiddenType: "give_troops"),

            // Narrated-but-not-done (take_as_secret_lover): a discreet flirt that names no bond at all, only a
            // lingering look and a teasing line, nothing agreed between them.
            ActionBenchCase.ExpectNone("take_as_secret_lover_flirting_no_bond", "take_as_secret_lover",
               contextFacts: "NPC: Ymira, a companion riding in the player's own party, exchanging teasing words with the player.",
               prose: "*Ymira's eyes linger on yours a moment too long across the fire, a small smile tugging at her mouth before she looks away.* Careful now, or you will have me thinking you mean something by all that looking.",
               forbiddenType: "take_as_secret_lover"),

            // Narrated-but-not-done (pledge_against): bitter grumbling about a rival, but the NPC vows nothing
            // concrete, no oath of action taken against him.
            ActionBenchCase.ExpectNone("pledge_against_grumbling_no_vow", "pledge_against",
               contextFacts: "NPC: Lord Ansen, harbouring resentment toward Lord Caladog (not his own kin).",
               prose: "*Ansen's jaw tightens at the mere mention of Caladog's name.* That man has wronged me more times than I care to count. One day, perhaps, he will answer for it. Today is not that day.",
               forbiddenType: "pledge_against"),

            // Narrated-but-not-done (execute_player): the captor threatens the player's life but pulls back,
            // no blow actually struck.
            ActionBenchCase.ExpectNone("execute_player_threatened_not_killed", "execute_player",
               contextFacts: "Hardcore mode. The player is held captive by the raider chief Ganak, furious with them.",
               prose: "*Ganak's blade hovers a hair's breadth from your throat, trembling with restrained fury, then he pulls it back and spits at your feet instead.* Next time, southerner, I will not stop my hand. Pray there is no next time.",
               forbiddenType: "execute_player"),

            // Narrated-but-not-done (expel_from_clan): a companion storms off in anger of their own accord, an
            // ordinary parting rather than the player casting them out.
            ActionBenchCase.ExpectNone("expel_from_clan_storms_off_own_accord", "expel_from_clan",
               contextFacts: "NPC: Doran, a companion of the player's own clan, furious after a heated argument, not cast out by the player.",
               prose: "*Doran slams his cup down and rises from the table without another word, storming out of the tent into the night.* I need air. Do not follow me.",
               forbiddenType: "expel_from_clan")
         };
      }

      #endregion
   }
}
