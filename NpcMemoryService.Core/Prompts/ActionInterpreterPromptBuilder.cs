// Code written by Gabriel Mailhot, 13/08/2026.

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Actions;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Builds the lean system prompt for the PROSE + INTERPRETER spike's SECOND model: an ACTION INTERPRETER that
   ///   reads a roleplay reply already written by a first model and emits only the structured [ACTION]/[EVENT] tags
   ///   the prose implies. The emitted format mirrors what <see cref="Parsing.SectionResponseParser" /> already
   ///   parses, so the existing pipeline consumes the interpreter's output unchanged.
   ///   <para>
   ///     The prompt is assembled STABLE-PREFIX FIRST (<see cref="StablePrefix" />, identical on every call, so a
   ///     provider can prompt-cache it) followed by the VARIABLE per-turn tail (the caller's context facts and the
   ///     prose to analyze). Pure and stateless: no engine or per-turn data leaks into the cacheable prefix.
   ///   </para>
   /// </summary>
   public static class ActionInterpreterPromptBuilder
   {
      private static readonly string _stablePrefix = BuildStablePrefix();

      /// <summary>
      ///   The invariant head of every interpreter prompt: the action interpreter's role, the tag vocabulary, the
      ///   exact output format, and the hard "do NOT rewrite" rule. Exposed so a test can assert a built prompt
      ///   STARTS with it (the prefix-first ordering that lets a provider cache this part across calls).
      /// </summary>
      internal static string StablePrefix => _stablePrefix;

      /// <summary>
      ///   The single user-turn instruction closing every interpreter call (bench AND runtime must use the same
      ///   one, so the bench measures what the game does). It forces a VISIBLE grounding step: each candidate
      ///   [ACTION] must first be justified on a CHECK line quoting the reply's words that show the deed
      ///   actually HAPPENING - the check that withholds refused, deferred, or merely discussed deeds. Making
      ///   the check written rather than silent is what makes it happen: refusal cues quoted verbatim in the
      ///   rules still get pattern-matched past (bench run 2026-08-19 13:40: 'I swear no oath today' emitted
      ///   swear_oath 3/3 despite the anti-pattern quoting those very words), whereas having to point at the
      ///   grounding words forces the refusal to be read. The parser ignores the CHECK lines (they carry no
      ///   brackets), so the runtime pipeline is unaffected.
      /// </summary>
      public const string FinalInstruction =
         "Work in TWO steps. STEP 1: scan the WHOLE reply for every completed deed first, then for EACH action " +
         "the reply brings to mind, write one grounding line in exactly this form - CHECK <type>: followed by a " +
         "SHORT quote of the reply's words that show the deed actually HAPPENING now (an acceptance, a transfer, " +
         "a done act). If those words are not there - the deed was only offered, demanded, weighed, refused, or " +
         "deferred - write CHECK <type>: NONE, quoting the refusal or deferral words instead. STEP 2: output the " +
         "[ACTION] and [EVENT] blocks for the grounded CHECK lines ONLY. A CHECK ...: NONE action is emitted as " +
         "NOTHING, however concretely the reply named the thing itself. A parting reply usually completes BOTH " +
         "its specific deed (a killing, a dismissal, a divorce accepted, a wage taken up) AND end_conversation: " +
         "CHECK and emit EACH, never the close alone. Output the CHECK lines first, then the tags - and nothing else.";

      /// <summary>
      ///   Assembles a full interpreter prompt: the stable prefix, then the caller's <paramref name="contextFacts" />
      ///   (a short line such as the NPC name and current regard), then a delimiter, then the
      ///   <paramref name="prose" /> to analyze. The result always begins with <see cref="StablePrefix" />.
      /// </summary>
      public static string Build(string prose, string contextFacts)
      {
         var sb = new StringBuilder();

         sb.Append(_stablePrefix);
         sb.AppendLine();
         sb.AppendLine(contextFacts?.Trim() ?? string.Empty);
         sb.AppendLine();
         sb.AppendLine("REPLY TO ANALYZE:");
         sb.Append(prose?.Trim() ?? string.Empty);

         return sb.ToString();
      }

      #region private

      /// <summary>
      ///   Renders every <see cref="GameActionCatalog" /> entry NOT already hand-taught above as a compact
      ///   reference block: one line per action with its description and parameters, then its concept-level
      ///   emission guidance from the catalog: an "emit when" line (the <see cref="GameActionSpec.Tells" />, how the
      ///   deed reads as genuinely happening this turn) and a "not when" line (the
      ///   <see cref="GameActionSpec.AntiPatterns" />, the look-alikes to withhold on). This is the same single
      ///   source the 1:1 prose prompt draws from, so the two never drift. Built once from the static catalog (no
      ///   per-turn data), so it all stays part of the cacheable stable prefix. A LOCAL set, not a static field:
      ///   this method runs from <see cref="_stablePrefix" />'s own field initializer, which runs before any static
      ///   field declared later in the file would be assigned, so a static exclusion set here would still read null
      ///   at that point.
      /// </summary>
      private static void AppendOtherActions(StringBuilder sb)
      {
         sb.AppendLine("OTHER ACTIONS YOU MAY EMIT: ONLY when the prose UNAMBIGUOUSLY shows this exact concrete deed");
         sb.AppendLine("happening in the reply. When in doubt, do not emit. Format each as an [ACTION] block with a");
         sb.AppendLine("'type:' line and the listed parameters, exactly like the actions above.");
         sb.AppendLine();
         sb.AppendLine("RULES THAT GOVERN THESE:");
         sb.AppendLine("1. PREFER THE SPECIFIC ACTION. When a concrete deed below fits what happened, emit THAT action.");
         sb.AppendLine("   change_relation and end_conversation are general fallbacks: a specific action MAY be accompanied");
         sb.AppendLine("   by one, but a concrete deed must NEVER be recorded as ONLY a change_relation or an");
         sb.AppendLine("   end_conversation. A regard shift toward a NAMED third party is sway_opinion, not change_relation;");
         sb.AppendLine("   a companion leaving your service or party is its own verb, not merely end_conversation.");
         sb.AppendLine("2. ONLY A COMPLETED DEED COUNTS, and a FUTURE-TENSE intention is NOT a completed deed. 'I will lend',");
         sb.AppendLine("   'I would give', 'I mean to', 'one day', 'when the time is right', 'not today', 'perhaps', 'if you");
         sb.AppendLine("   first...' all describe something that has NOT happened yet: emit nothing for it. Likewise a deed");
         sb.AppendLine("   merely offered, weighed, or wished for. Emit ONLY when the reply shows the act happening NOW.");
         sb.AppendLine("3. A PLAYER'S DEED THE REPLY REACTS TO STILL COUNTS. If a CONVERSATION SO FAR (below) shows the PLAYER");
         sb.AppendLine("   doing or declaring a deed, and this reply ACCEPTS it or carries it out, emit THAT deed, not merely");
         sb.AppendLine("   change_relation or end_conversation. If the player casts the NPC out and the reply is 'then I am");
         sb.AppendLine("   gone', that is expel_from_clan; if the player hands over a sword and the reply thanks them and takes");
         sb.AppendLine("   it, that is give_item. But if the reply REFUSES or defers the player's deed, withhold (rule 2).");
         sb.AppendLine("3b. A DECLINED OR CONDITIONAL OFFER COMPLETES NOTHING. The facts may state what the player PROPOSED -");
         sb.AppendLine("   never what HAPPENED. Only the reply's ACCEPTANCE completes a deed. Refusals, deferrals and");
         sb.AppendLine("   pre-conditions all mean NOTHING moved: 'No', 'I think not', 'shakes his head', 'not yet', 'for");
         sb.AppendLine("   now', 'let me sleep on it', 'I will consider', 'we will talk further', 'only then', 'stays bound',");
         sb.AppendLine("   'my oath still binds me', 'do not think it buys', 'not a coin has crossed my palm'. Emit NOTHING");
         sb.AppendLine("   for the proposed verb, however concretely the thing itself (chain, coin, sword, soldiers, oath)");
         sb.AppendLine("   is named.");
         sb.AppendLine("4. MIRROR PAIRS: FOLLOW THE GOLD AND THE CHAIN. execute_prisoner = the PLAYER kills a captive the");
         sb.AppendLine("   PLAYER holds; execute_player = the NPC captor kills the PLAYER. buy_prisoner = the NPC's coin lands");
         sb.AppendLine("   in the PLAYER's palm and the captive's chain passes to the NPC; sell_prisoner = the chain passes to");
         sb.AppendLine("   the PLAYER and the player's coin to the NPC. Check which way EACH moved before choosing; if the");
         sb.AppendLine("   reply shows only one leg of the transfer, emit nothing. A captive DELIVERED to settle an");
         sb.AppendLine("   outstanding deliver-prisoner bargain is give_prisoner, even if a reward purse changes hands - the");
         sb.AppendLine("   purse settles the bargain, it is not a price. buy_prisoner needs a PRICE stated FOR the captive in");
         sb.AppendLine("   the reply or the facts: when the facts name an outstanding deliver-prisoner bargain and no price is");
         sb.AppendLine("   stated, the purse is the bargain's reward, so emit give_prisoner, never buy_prisoner. Naming a price");
         sb.AppendLine("   is never enough: whatever the verb, coin AND chain must actually change hands in this reply, or");
         sb.AppendLine("   nothing happened.");
         sb.AppendLine("5. BEFORE emitting change_relation or end_conversation ALONE, scan once more for a concrete named deed:");
         sb.AppendLine("   a warm reaction to the player's apology for a KNOWN grievance is make_amends (with change_relation,");
         sb.AppendLine("   never it alone); a companion accepting a named duty (scout/engineer/quartermaster/surgeon) is");
         sb.AppendLine("   assign_party_role; a settlement notable (headman, gang leader, merchant, artisan) leaving a post to");
         sb.AppendLine("   follow the player is recruit_notable, join_party is ONLY a free wanderer with no post to leave; a");
         sb.AppendLine("   lord vowing to move against a named rival himself is pledge_against, scheme_assist is ONLY the player");
         sb.AppendLine("   joining the NPC's own scheme; a companion cast out is expel_from_clan FIRST, end_conversation may");
         sb.AppendLine("   accompany it but never replace it.");
         sb.AppendLine("6. A VOW IS A PRESENT DEED (exception to rule 2). Swearing, vowing, or naming a bond happens the moment");
         sb.AppendLine("   the words are spoken, even though what is sworn lies in the future: 'I swear I will pay by winter' IS");
         sb.AppendLine("   swear_oath now; 'I vow to see his name ruined' IS pledge_against now; 'let this stay between us' IS");
         sb.AppendLine("   take_as_secret_lover now. Rule 2 still withholds a CONTEMPLATED or conditional vow ('I might swear',");
         sb.AppendLine("   'if you prove yourself I will'), never the vow actually spoken this turn. And a vow explicitly");
         sb.AppendLine("   WITHHELD is a refusal, never a deed: 'I swear no oath today', 'I will not swear', 'I make no");
         sb.AppendLine("   vow', 'I bind myself to nothing' emit NOTHING, even when the withheld oath's content (keep the");
         sb.AppendLine("   peace, the faction, the sum) is spelled out word for word.");
         sb.AppendLine("7. VAGUE GOODWILL IS NOT A TRANSFER. 'I will not forget this', 'my house owes you', 'you will be");
         sb.AppendLine("   rewarded', 'perhaps one day' emit NOTHING. give_influence, give_troops, give_gold fire only on a");
         sb.AppendLine("   concrete, countable thing moving NOW: influence spent this hour, named soldiers changing ranks, coin");
         sb.AppendLine("   in a palm.");
         sb.AppendLine("8. ONE REPLY CAN CARRY MORE THAN ONE DEED. Emit EVERY action the reply completes, not only the first:");
         sb.AppendLine("   a lord who names a governor AND sets their wage is appoint_governor AND grant_stipend; scan the whole");
         sb.AppendLine("   reply before you stop.");
         sb.AppendLine("9. A BEAT THAT ENDS ON A DEED RECORDS THE DEED, NOT end_conversation ALONE. end_conversation alone is");
         sb.AppendLine("   right only for a plain goodbye that carries no deed. When the reply reads as a farewell, a leave-taking");
         sb.AppendLine("   or an ending, ask WHAT ended it and CHECK that verb: a captive killed = execute_prisoner; the captor");
         sb.AppendLine("   killing the player = execute_player; a companion cast from the clan = expel_from_clan; an escort sent");
         sb.AppendLine("   home = dismiss_escort; a companion taking their own leave of the party = part_ways; a companion");
         sb.AppendLine("   summoned back from a posting = recall_companion; a marriage dissolved on the player's consent =");
         sb.AppendLine("   accept_divorce, or dissolved by the NPC themselves = end_own_marriage. The parting words ('go well',");
         sb.AppendLine("   'this is where we part', 'no more talk') are only the WRAPPER; the deed inside is the action, and");
         sb.AppendLine("   end_conversation never replaces it. The CHECK still governs: quote the words that show the deed DONE,");
         sb.AppendLine("   or write NONE and withhold - a threatened execution, a storming-off in anger, an unhappy marriage");
         sb.AppendLine("   merely lamented all end the talk WITHOUT completing the deed, so those stay a bare end_conversation.");
         sb.AppendLine();

         // The signals already taught above by hand, with carefully-tuned wording that must never be diluted:
         // excluded here so they are never taught twice. The four reactive signals (change_relation, give_gold,
         // take_gold, plus end_conversation) and harm_prisoner, whose captive-injury cue needs the same explicit
         // wording rather than the generic one-liner this block would give it. end_conversation is a ChatViewModel
         // chat-flow control (like witness_leaves/request_privacy), not a GameActionCatalog entry, so it never
         // appears in GameActionCatalog.Types in the first place; it is listed here only so the exclusion reads by
         // name alongside the others.
         var coreTaughtTypes = new HashSet<string> {
            "change_relation", "end_conversation", "give_gold", "take_gold", "harm_prisoner"
         };

         foreach (GameActionSpec spec in GameActionCatalog.All)
         {
            if (coreTaughtTypes.Contains(spec.Type)) continue;

            var line = new StringBuilder();
            line.Append("- ").Append(spec.Type).Append(": ").Append(spec.Description);

            if (spec.Parameters.Count > 0)
            {
               line.Append(" (params: ");
               for (var i = 0; i < spec.Parameters.Count; i++)
               {
                  if (i > 0) line.Append("; ");
                  line.Append(spec.Parameters[i].Name).Append('=').Append(spec.Parameters[i].Meaning);
               }
               line.Append(')');
            }

            sb.AppendLine(line.ToString());

            if (spec.Tells.Count > 0)
               sb.Append("    emit when: ").AppendLine(string.Join("; ", spec.Tells));

            if (spec.AntiPatterns.Count > 0)
               sb.Append("    not when: ").AppendLine(string.Join("; ", spec.AntiPatterns));
         }

         sb.AppendLine();
         AppendExamples(sb);
      }

      /// <summary>
      ///   A few worked examples (worked cases beat prose rules for a model): a direction pair, a specific-over-
      ///   generic case, and verbatim-anti-pattern negatives that must emit nothing. Every example models the
      ///   CHECK grounding line the <see cref="FinalInstruction" /> demands, so the model imitates the format:
      ///   a grounded CHECK quotes the deed's words, a NONE CHECK quotes the refusal or deferral and emits no
      ///   tag. Part of the cacheable prefix.
      /// </summary>
      private static void AppendExamples(StringBuilder sb)
      {
         sb.AppendLine("EXAMPLES (study the CHECK lines, the direction, and the withholds):");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart. The player's own party holds the hero captive Sanjar. YOU: Yerengul.");
         sb.AppendLine("Reply: *I count four hundred denars into your palm and take Sanjar's chain in the other hand.* Four hundred, as agreed. He is mine to hold now.");
         sb.AppendLine("CHECK buy_prisoner: \"count four hundred denars into your palm and take Sanjar's chain\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: buy_prisoner");
         sb.AppendLine("target: Sanjar");
         sb.AppendLine("price: 400");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart. The player's own party holds the hero captive Lady Ira. YOU: Khan Sechen, buying her.");
         sb.AppendLine("Reply: *Sechen's men simply take Ira's chain from your hands the moment his gold has finished spilling into your saddlebag.* Six hundred denars, and she rides with my column from here on.");
         sb.AppendLine("CHECK buy_prisoner: \"take Ira's chain from your hands\" and \"his gold has finished spilling into your saddlebag\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: buy_prisoner");
         sb.AppendLine("target: Ira");
         sb.AppendLine("price: 600");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("(passive voice changes nothing: the NPC's coin reached the PLAYER and the chain left the PLAYER's");
         sb.AppendLine("hands for the NPC's men, so this is buy_prisoner, never sell_prisoner)");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart, your prisoner. YOU: Ganak, a raider chief. Hardcore.");
         sb.AppendLine("Reply: *My dagger flashes once across your throat before you can speak another word.* No more talk. This is where your road ends.");
         sb.AppendLine("CHECK execute_player: \"My dagger flashes once across your throat\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: execute_player");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Uldric, headman of the village of Marunath.");
         sb.AppendLine("Reply: *I set down my hoe and take my place beside your banner.* My cousin can take up the headman's mantle here. Lead on.");
         sb.AppendLine("CHECK recruit_notable: \"set down my hoe and take my place beside your banner\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: recruit_notable");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Lord Ansen, a lord with trust in the player.");
         sb.AppendLine("Reply: *Ansen considers.* Perhaps, when the moment is right, I will lend a portion of my house's weight to your cause. Not today, though.");
         sb.AppendLine("CHECK give_influence: NONE (\"Perhaps, when the moment is right\" - a future, conditional intention, nothing lent now)");
         sb.AppendLine("(no output: a future, conditional intention is not a completed deed)");
         sb.AppendLine();
         sb.AppendLine("Facts: The player's party holds no captive. YOU: Yerengul, whose clan holds the hero Vsevolod captive.");
         sb.AppendLine("Reply: *Yerengul strokes his beard.* Three hundred denars for Vsevolod's chain, that is my price. Bring the coin and we will talk further, but he stays bound to my saddle for now.");
         sb.AppendLine("CHECK sell_prisoner: NONE (\"he stays bound to my saddle for now\" - a price named, the chain unmoved)");
         sb.AppendLine("(no output: a price haggled over is not a sale made, and the chain has not changed hands - do NOT");
         sb.AppendLine("emit sell_prisoner while 'he stays bound to my saddle for now')");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart, holding the nemesis Unqid captive. YOU: Unqid, a beaten nemesis.");
         sb.AppendLine("Reply: *Unqid rubs his freed wrists, wary.* You would let me live? That is more mercy than I expected. Do not think it buys my sword, though; our quarrel is not so easily mended.");
         sb.AppendLine("CHECK turn_nemesis: NONE (\"Do not think it buys my sword\" - a refusal to swear in)");
         sb.AppendLine("(no output: spared and freed is not turn_nemesis unless he actually swears into your clan, which he");
         sb.AppendLine("refuses - 'do not think it buys my sword' is a REFUSAL, and a declined offer completes nothing)");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Lady Sora, a lady without a field party of her own. The player has just invited her to ride within their party.");
         sb.AppendLine("Reply: *Sora shakes her head slowly.* No, I think not. My place is with my father's house, not tucked inside another lord's column.");
         sb.AppendLine("CHECK ride_with_me: NONE (\"No, I think not\" - a flat refusal)");
         sb.AppendLine("(no output: a flat refusal completes nothing - do NOT emit ride_with_me on 'No, I think not')");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Ismid the merchant, owed a debt by the player.");
         sb.AppendLine("Reply: Pay me the two hundred denars you owe, and only then will I consider our business settled. Not a coin has crossed my palm yet.");
         sb.AppendLine("CHECK take_gold: NONE (\"Not a coin has crossed my palm yet\" - a demand, not a payment)");
         sb.AppendLine("(no output: a demand is not a payment - do NOT emit take_gold when 'not a coin has crossed my palm')");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart. The player holds Lord Ansen captive. YOU: Osric, Rhobart's sworn companion, witnessing the execution.");
         sb.AppendLine("Reply: *You do not answer him. You only step close, draw steel with your own hand, and it is over before Ansen can finish his final plea.* No more words needed. That debt is paid in full now.");
         sb.AppendLine("CHECK execute_prisoner: \"You only step close, draw steel with your own hand\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: execute_prisoner");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("(the PLAYER - 'you' - performs the killing, so execute_prisoner, never execute_player, even when");
         sb.AppendLine("the killing blow is implied rather than spelled out)");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Lady Nadea, unmarried, courted openly by the player.");
         sb.AppendLine("Reply: *Nadea's smile is warm, but she gently pulls her hand free.* You court me boldly, and I will not pretend I am unmoved. But I promise nothing until my father gives his blessing.");
         sb.AppendLine("CHECK marry: NONE (\"I promise nothing until my father gives his blessing\" - warmth and admitted");
         sb.AppendLine("interest, but the bond itself is explicitly withheld)");
         sb.AppendLine("(no output: admitted interest with the promise withheld completes nothing - do NOT emit marry on");
         sb.AppendLine("'I promise nothing until', however warm the refusal)");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Lord Ansen. The player has just asked him to swear to fight at their side by winter.");
         sb.AppendLine("Reply: *Ansen shakes his head slowly.* I will not bind my house to another's war, not while the granaries stand empty. Ask me again when the stores are full.");
         sb.AppendLine("CHECK swear_oath: NONE (\"I will not bind my house\" - the vow is refused outright, however clearly");
         sb.AppendLine("the would-be oath is named)");
         sb.AppendLine("(no output: a refused oath is no oath - do NOT emit swear_oath on 'I will not bind', even with the");
         sb.AppendLine("oath's content spelled out)");
         sb.AppendLine();
         sb.AppendLine("Facts: YOU: Ymira, a retainer riding within the player's party via ride_with_me, who has decided to return home.");
         sb.AppendLine("Reply: *Ymira shoulders her pack and swings up into the saddle.* My road bends home from here, captain. Fare well - you will not find a better scout this side of the mountains.");
         sb.AppendLine("CHECK part_ways: \"shoulders her pack and swings up into the saddle\" and \"My road bends home from here\"");
         sb.AppendLine("CHECK end_conversation: \"Fare well\"");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: part_ways");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: end_conversation");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("(a parting reply completes BOTH its specific deed AND the close: CHECK and emit each, never the");
         sb.AppendLine("close alone)");
         sb.AppendLine();
         sb.AppendLine("Facts: PLAYER: Rhobart. YOU: Vortigern, whose clan holds the hero captive Maeve; the player is buying her from you.");
         sb.AppendLine("Reply: *I drop Maeve's chain into your palm and sweep your three hundred denars off the table in the same motion.* A fair price for a fighter. She is your burden now.");
         sb.AppendLine("CHECK sell_prisoner: \"drop Maeve's chain into your palm\" (chain TO the player) and \"sweep your three hundred denars\" (coin TO the NPC)");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: sell_prisoner");
         sb.AppendLine("target: Maeve");
         sb.AppendLine("price: 300");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("(direction is what the CHECK must pin: the chain went TO the player and the coin TO the NPC, so");
         sb.AppendLine("sell_prisoner, never buy_prisoner)");
         sb.AppendLine();
      }

      private static string BuildStablePrefix()
      {
         var sb = new StringBuilder();

         sb.AppendLine("You are a structured-signal EXTRACTOR for a roleplay game. A first model has ALREADY written");
         sb.AppendLine("the NPC's reply. Your only job is to read that reply and emit the machine-readable tags it");
         sb.AppendLine("implies, so the game can record what happened. You never speak as the NPC.");
         sb.AppendLine();
         sb.AppendLine("THE VOICE OF THE REPLY: the reply is written in the NPC's own voice. 'I', 'me', 'my' are the NPC");
         sb.AppendLine("named on the YOU line of the facts; 'you', 'your' are the PLAYER named on the PLAYER line. Before");
         sb.AppendLine("emitting any action, settle WHO performs the deed: the NPC (I), the player (you), or a named third");
         sb.AppendLine("party. Never infer the direction from a bare noun: a 'captive' may be held by either side and gold");
         sb.AppendLine("may move either way, so read the facts' PLAYER/YOU lines to know who holds whom and who pays whom.");
         sb.AppendLine();
         sb.AppendLine("Emit ONLY the blocks below, each label on its own line, opened as [LABEL] and closed as [/LABEL]:");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: change_relation");
         sb.AppendLine("delta: <integer>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("change_relation records how the NPC's regard toward the player shifts in THIS reply, judged by");
         sb.AppendLine("what the PLAYER's words and deeds EARNED: positive for a kindness, help, a warning, useful news,");
         sb.AppendLine("warmth, or gratitude the player gave; negative for an insult, threat, lie, broken word, cruelty,");
         sb.AppendLine("or selfish demand the player made. Use roughly -15 (a grave affront) to +10 (deep warmth). A");
         sb.AppendLine("value of 0 means no change, so emit nothing at all.");
         sb.AppendLine("DO NOT PENALISE THE NPC'S OWN MOOD OR CAUTION: a wary, guarded, cold, or suspicious manner the NPC");
         sb.AppendLine("brings to a stranger is their character, NOT a drop the player caused. A helpful act - a warning");
         sb.AppendLine("of danger, aid, information - met with suspicion is STILL helpful: emit 0 or a small positive,");
         sb.AppendLine("never a penalty. Being received coldly is not the same as deserving cold. Emit a NEGATIVE value");
         sb.AppendLine("ONLY when the PLAYER did something that genuinely worsened how the NPC feels toward them.");
         sb.AppendLine("CALIBRATE, DO NOT INFLATE: most replies move regard only a LITTLE or not at all. A small kindness,");
         sb.AppendLine("a warm exchange, one more step in an encounter already under way is +1 to +3. The high end (+8 to");
         sb.AppendLine("+10) is a RARE, genuine turning point (a first declaration of love, a life saved, a betrayal");
         sb.AppendLine("forgiven), never the routine tenor of a scene. When the moment is already captured as an [EVENT]");
         sb.AppendLine("(an intimate beat, a flirt), that EVENT carries its weight: do NOT also stack a large change_relation");
         sb.AppendLine("on top of it. Physical escalation between two who are already close is the EXPRESSION of that bond,");
         sb.AppendLine("not a fresh large gain each beat, so emit 0 or a small value unless the reply marks a real shift.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: end_conversation");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit end_conversation whenever the prose brings THIS exchange to a close, warm or cold: storming");
         sb.AppendLine("off, dismissing the player, turning away to your own business and leaving them, OR an amicable");
         sb.AppendLine("parting (a farewell, a goodnight, sending them off to rest, saying you will speak again later). It");
         sb.AppendLine("ends the current conversation, not the relationship, so a warm \"we will talk in the morning\" as the");
         sb.AppendLine("NPC leaves the player STILL closes it. If the reply plainly reads as a parting, emit it.");
         sb.AppendLine("A CAPTIVE scene often closes with NO spoken goodbye: the captor is DONE and has the prisoner");
         sb.AppendLine("REMOVED FROM THE SCENE - hauled back to the cell, cage, wagon, or the line of captives, chained or");
         sb.AppendLine("staked for the night, dragged off out of sight. That removal, the encounter OVER, ENDS the meeting:");
         sb.AppendLine("emit end_conversation and record a farewell/parting, not a confrontation. But do NOT emit it while");
         sb.AppendLine("the scene is still HAPPENING: handing the prisoner to your men, stepping back to WATCH, settling in");
         sb.AppendLine("to observe, or a guard merely holding or moving the prisoner is NOT a close - the act is under way");
         sb.AppendLine("and continues. The close is ONLY when the prisoner is TAKEN AWAY and the encounter is finished.");
         sb.AppendLine("A reply that CLOSES the exchange almost always also completes the specific deed that ends it (a");
         sb.AppendLine("dismissal, a parting, a casting-out, a divorce accepted, a killing): emit that deed's action WITH");
         sb.AppendLine("end_conversation, never the close alone (rules 1 and 8 below).");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: give_gold");
         sb.AppendLine("amount: <whole number of denars>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit give_gold when the prose shows the NPC HANDING money, coin, or a purse TO the player (a gift,");
         sb.AppendLine("reward, payment, or bribe). If the prose names an amount, use it; if it is vague (a purse, a few");
         sb.AppendLine("coins), estimate a modest, plausible sum that fits the description. This is ONLY the NPC giving to");
         sb.AppendLine("the player, and ONLY an actual handover: a promised, owed, or ordered payment ('collect your pay',");
         sb.AppendLine("'you will be rewarded') is not coin in the player's palm - emit nothing for it.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: take_gold");
         sb.AppendLine("amount: <whole number of denars>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit take_gold when the prose shows the PLAYER paying, handing over, or surrendering money TO the");
         sb.AppendLine("NPC (a fee, tribute, ransom, or debt). Direction is what matters: money moving from the NPC to the");
         sb.AppendLine("player is give_gold; money moving from the player to the NPC is take_gold. But a demand, a");
         sb.AppendLine("reminder, or a promise of payment is NOT a payment: if no coin actually leaves the player's purse");
         sb.AppendLine("in this reply ('pay me and only then', 'not a coin has crossed my palm'), emit nothing.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: harm_prisoner");
         sb.AppendLine("severity: light|moderate|severe");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit harm_prisoner when the facts below show the PLAYER is the captive being held (\"your prisoner\")");
         sb.AppendLine("AND the prose shows them suffering REAL bodily harm THIS reply. That harm is EITHER an injury - a");
         sb.AppendLine("lash that draws blood, a wounding blow, a cut, a burn, a broken bone - OR a FORCED sexual assault:");
         sb.AppendLine("rape, forced penetration, being used roughly against their will is a violent bodily violation, and");
         sb.AppendLine("the captive takes real harm from it. Match severity to the brutality: light for a slap, a welt, or a");
         sb.AppendLine("rough grope; moderate for blood drawn, a heavy blow, or a forced penetration; severe for a grievous");
         sb.AppendLine("wound or a brutal, sustained assault. Do NOT emit it for a threat or humiliation with no physical");
         sb.AppendLine("act, for stripping or binding alone, or for a CONSENSUAL intimate act; and NEVER when the player is");
         sb.AppendLine("not the one held captive.");
         sb.AppendLine();
         sb.AppendLine("[EVENT]");
         sb.AppendLine("type: first_meeting|farewell|conflict|collaboration|agreement|flirt|intimacy|betrayal|confrontation|other");
         sb.AppendLine("summary: One sentence, FIRST PERSON and PAST TENSE. Use \"I\" for yourself (never your own name),");
         sb.AppendLine("and ONLY for what YOU personally did: if the reply shows someone ELSE performing the act (a soldier,");
         sb.AppendLine("a bandmate, anyone at your command) while you order, watch, or hold the prisoner, the deed is");
         sb.AppendLine("THEIRS, so write \"I had my man do X\" or \"my men did X while I watched\", never \"I did X\" for an act");
         sb.AppendLine("you did not perform yourself.");
         sb.AppendLine("Refer to the PLAYER by the name given in the facts below, never by an invented epithet");
         sb.AppendLine("(not \"the coward\", \"the stranger\") and never by a bare \"he\"/\"she\". Record what happened this");
         sb.AppendLine("reply and why it mattered; do NOT state how long ago it was.");
         sb.AppendLine("[/EVENT]");
         sb.AppendLine("These [EVENT] type words (first_meeting, farewell, conflict, collaboration, agreement, flirt,");
         sb.AppendLine("intimacy, betrayal, confrontation, other) label the [EVENT] block ONLY. NEVER emit one as an");
         sb.AppendLine("[ACTION]: an [ACTION] type is ALWAYS one of the action verbs (change_relation, end_conversation,");
         sb.AppendLine("give_gold, take_gold, or one listed under OTHER ACTIONS below). A moment that is intimate or a");
         sb.AppendLine("flirt is recorded as an [EVENT], never emitted as an action. BUT naming or AGREEING to a romantic");
         sb.AppendLine("BOND is a status change, not merely a moment: the two of you agreeing to wed (marry), to a committed");
         sb.AppendLine("bond (take_as_consort), or to a discreet hidden arrangement (take_as_secret_lover) IS one of those");
         sb.AppendLine("ACTIONS, emitted as an [ACTION] and, if you like, an [EVENT] too. The 'never an action' rule covers a");
         sb.AppendLine("flirt or intimacy MOMENT, not the pledging of a bond.");
         sb.AppendLine("first_meeting is ONLY for a genuine first-ever encounter: the two of you have never spoken");
         sb.AppendLine("before. If the facts below show any regard other than +0 toward the player, or name an");
         sb.AppendLine("existing bond (a spouse, consort, lover, or kin), you already know each other, so do NOT use");
         sb.AppendLine("first_meeting. Likewise, if this reply is a later turn of an exchange already under way rather");
         sb.AppendLine("than its opening line, do NOT use first_meeting either: when the facts below state the");
         sb.AppendLine("conversation is already under way, that IS this case, take it as decisive over anything else in");
         sb.AppendLine("the prose. Pick whichever type actually fits what happened this reply instead (collaboration,");
         sb.AppendLine("agreement, flirt, intimacy, confrontation, farewell, or other).");
         sb.AppendLine();
         AppendOtherActions(sb);
         sb.AppendLine("GROUND IN THE FACTS, DO NOT INVENT: the facts below give the setting (place, who is present, the");
         sb.AppendLine("player's standing). Use them to ANCHOR a memory (where it happened, who witnessed it) when it fits");
         sb.AppendLine("what occurred, but record ONLY what actually happened in the reply; never invent events, people, or");
         sb.AppendLine("details the reply does not contain (do not add the player's army or companions unless the reply does).");
         sb.AppendLine();
         sb.AppendLine("HARD RULE: You are given a roleplay reply that is ALREADY WRITTEN. Do NOT rewrite, continue, or");
         sb.AppendLine("comment on it. Read it and output ONLY the [ACTION] and [EVENT] blocks it implies, in the exact");
         sb.AppendLine("format above, and nothing else. If nothing is warranted, output nothing.");

         return sb.ToString();
      }

      #endregion
   }
}
