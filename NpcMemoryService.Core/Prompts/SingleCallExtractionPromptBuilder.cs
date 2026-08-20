// Code written by Gabriel Mailhot, 19/08/2026.
// SPIKE (increment 1 of the single-call experiment): a ONE-CALL composer+extractor prompt. The model writes the
// NPC's in-character reply AND, in the same response, grounds and emits the machine tags - the CHECK technique that
// lifted the two-call interpreter, applied to a single generation. The open question this probe answers: does the
// CHECK discipline survive when the SAME model, at a prose-friendly temperature, is also being creative, and does it
// ground its OWN prose honestly (rather than rationalising an implied deed as done)? Deliberately self-contained and
// condensed; if the spike passes, increment 2 factors the shared grounding rules out of ActionInterpreterPromptBuilder
// so DIRECT and interpret draw from one source, and increment 3 wires this into the real (rich) DIRECT PromptBuilder.

#region

using System.Text;
using NpcMemoryService.Core.Actions;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>Builds the single-call composer+extractor system prompt used by the spike (cr.single_call_probe).</summary>
   public static class SingleCallExtractionPromptBuilder
   {
      /// <summary>
      ///   The user-turn nudge that closes the call. Names the three-step contract one last time so a model that
      ///   skims the system prompt still writes the reply, grounds each deed, then emits only the grounded tags.
      /// </summary>
      public const string FinalInstruction =
         "Write the NPC's reply now. First the [DIALOGUE] block, then a CHECK line for every deed the reply brings " +
         "about (quote your own words that show it DONE, or CHECK <type>: NONE for a refusal or a mere intention), " +
         "then the [ACTION]/[EVENT] blocks for the grounded CHECK lines only. Nothing else.";

      /// <summary>
      ///   Assembles the system prompt: the composer role, the scenario facts, the three-step protocol, the shared
      ///   grounding rules, and the per-verb catalog render. <paramref name="facts" /> is a short digest naming who
      ///   the NPC and player are and any live state (captive, regard, an outstanding bargain).
      /// </summary>
      public static string Build(string facts)
      {
         var sb = new StringBuilder();

         sb.AppendLine("You are ROLE-PLAYING as an NPC in a medieval game, and in the SAME response you record what");
         sb.AppendLine("happens as machine-readable tags. Two jobs, one reply: be the character, then be the scribe.");
         sb.AppendLine();
         sb.AppendLine("WHO IS WHO:");
         sb.AppendLine(facts);
         sb.AppendLine("THE VOICE: you speak as the NPC. 'I', 'me', 'my' are you; 'you', 'your' are the player. Settle WHO");
         sb.AppendLine("performs each deed before tagging: you (the NPC), the player, or a named third party.");
         sb.AppendLine();
         sb.AppendLine("WORK IN THREE STEPS, IN THIS ORDER:");
         sb.AppendLine("STEP 1 - THE REPLY. Speak in the NPC's own voice, in grounded period prose, reacting to the player.");
         sb.AppendLine("   Put ONLY the spoken/narrated reply inside a [DIALOGUE] ... [/DIALOGUE] block. This is the only");
         sb.AppendLine("   thing the player ever sees. Do not mention tags, checks, or rules inside it.");
         sb.AppendLine("STEP 2 - THE CHECK. For EACH action your reply brings about, write one line: CHECK <type>: followed");
         sb.AppendLine("   by a SHORT quote from YOUR OWN reply that shows the deed actually HAPPENING now (an acceptance, a");
         sb.AppendLine("   transfer, a done act). If the deed was only offered, demanded, weighed, refused, or deferred,");
         sb.AppendLine("   write CHECK <type>: NONE and quote the refusal or deferral words instead. Grounding your own");
         sb.AppendLine("   prose keeps you honest: do not tag a deed you only implied or intended.");
         sb.AppendLine("STEP 3 - THE TAGS. Emit one [ACTION] ... [/ACTION] block per deed, in the EXACT format below, for");
         sb.AppendLine("   the grounded CHECK lines ONLY (a CHECK ...: NONE emits NOTHING for that verb), plus one [EVENT]");
         sb.AppendLine("   ... [/EVENT] if the moment is memorable.");
         sb.AppendLine();
         sb.AppendLine("DISCIPLINE (this is what keeps the player's reply clean): your ENTIRE output is only these blocks -");
         sb.AppendLine("[DIALOGUE], then the CHECK lines, then [ACTION]/[EVENT]. NEVER think out loud, weigh options, second-");
         sb.AppendLine("guess yourself, or discuss these rules or the action list anywhere in your output; that reasoning is");
         sb.AppendLine("SILENT, it never reaches the page. Nothing may appear outside the blocks. The CHECK lines come AFTER");
         sb.AppendLine("[/DIALOGUE], never inside it, and each is ONE short line. If a deed is uncertain, make ONE decisive");
         sb.AppendLine("call or withhold it - do NOT deliberate in prose. The action list below is COMPLETE; if a deed is not");
         sb.AppendLine("in it, it is not an action, so withhold rather than hunt for one. When the tags are written, STOP.");
         sb.AppendLine();
         AppendOutputFormat(sb);
         sb.AppendLine();
         AppendGroundingRules(sb);
         sb.AppendLine();
         AppendActionCatalog(sb);

         return sb.ToString();
      }

      // The EXACT tag shape the game's parser reads. The spike's first run proved the model gets the REASONING right
      // but improvises the format (bare verbs, verbs in their own brackets, free-text events) when it is not pinned,
      // so the parser recovers nothing. This template is the same shape ActionInterpreterPromptBuilder emits: one
      // [ACTION] block per deed, the verb ALWAYS as "type: <verb>", each parameter on its own "name: value" line.
      private static void AppendOutputFormat(StringBuilder sb)
      {
         sb.AppendLine("EXACT TAG FORMAT (the game parses this LITERALLY - one [ACTION] block per deed; the verb ALWAYS on");
         sb.AppendLine("its own line as \"type: <verb>\", NEVER a bare verb and NEVER a verb in its own brackets; each");
         sb.AppendLine("parameter on its own \"name: value\" line):");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: take_gold");
         sb.AppendLine("amount: 220");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: change_relation");
         sb.AppendLine("delta: 3");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("[EVENT]");
         sb.AppendLine("type: first_meeting|farewell|conflict|collaboration|agreement|flirt|intimacy|betrayal|confrontation|other");
         sb.AppendLine("summary: One sentence, FIRST PERSON and PAST TENSE, naming the player by the name in the facts.");
         sb.AppendLine("[/EVENT]");
         sb.AppendLine("Parameters by verb: change_relation uses delta (an integer -15..+10, 0 means emit nothing);");
         sb.AppendLine("give_gold/take_gold use amount (whole denars); buy_prisoner/sell_prisoner use target (the captive's");
         sb.AppendLine("name) and price; give_troops/lend_troops use amount; harm_prisoner uses severity (light|moderate|");
         sb.AppendLine("severe); most other verbs that concern one hero use target. Omit any parameter a verb does not need.");
         sb.AppendLine("The [EVENT] type words label the EVENT only; NEVER emit one as an [ACTION] type.");
      }

      // The concept-level rules that govern the tags, condensed from the interpreter's hard-won set. These are the
      // ones the bench proved matter most: completed-deed-only, direction, the vow carve-out, the terminal-deed rule,
      // and vague-goodwill. Increment 2 will make DIRECT and interpret share ONE copy of these.
      private static void AppendGroundingRules(StringBuilder sb)
      {
         sb.AppendLine("RULES FOR THE TAGS:");
         sb.AppendLine("A. ONLY A COMPLETED DEED COUNTS. A future, offered, conditional, or refused deed is not done: emit");
         sb.AppendLine("   nothing for it. A concrete deed is NEVER recorded as only change_relation or end_conversation.");
         sb.AppendLine("B. DIRECTION IS EVERYTHING. Follow the gold and the chain: give_gold is coin from you TO the player,");
         sb.AppendLine("   take_gold is coin from the player TO you; buy_prisoner is your coin to the player and the chain to");
         sb.AppendLine("   you, sell_prisoner is the reverse. Tag which way EACH actually moved in this reply.");
         sb.AppendLine("C. A VOW IS A PRESENT DEED. Swearing, vowing, or naming a bond happens the moment the words are");
         sb.AppendLine("   spoken ('I swear I will pay by winter' is swear_oath now). But a vow WITHHELD ('I will not swear')");
         sb.AppendLine("   is a refusal, never a deed.");
         sb.AppendLine("D. A BEAT THAT ENDS ON A DEED RECORDS THE DEED, not end_conversation alone. A captive killed =");
         sb.AppendLine("   execute_prisoner; a companion cast out = expel_from_clan; an escort sent home = dismiss_escort; a");
         sb.AppendLine("   companion taking their leave = part_ways; a marriage dissolved = accept_divorce/end_own_marriage.");
         sb.AppendLine("   The parting words are only the wrapper; the deed inside is the action. A threatened or merely");
         sb.AppendLine("   lamented ending, though, stays a bare end_conversation.");
         sb.AppendLine("E. VAGUE GOODWILL IS NOT A TRANSFER. 'you will be rewarded', 'my house owes you' emit nothing;");
         sb.AppendLine("   give_influence/give_troops/give_gold fire only on a concrete, countable thing moving NOW.");
      }

      // The per-verb "emit when / not when" render, drawn straight from the shared GameActionCatalog so the single
      // call teaches the same discriminants the interpreter does. change_relation and end_conversation are chat-flow
      // fallbacks the model already knows; every dispatchable verb is listed here with its tells and anti-patterns.
      private static void AppendActionCatalog(StringBuilder sb)
      {
         sb.AppendLine("THE ACTIONS YOU MAY EMIT (emit the specific one when its tell fits; withhold on its anti-pattern):");
         foreach (GameActionSpec spec in GameActionCatalog.All)
         {
            sb.Append("- ").Append(spec.Type).Append(": ").AppendLine(spec.Description);

            if (spec.Tells.Count > 0)
               sb.Append("    emit when: ").AppendLine(string.Join("; ", spec.Tells));

            if (spec.AntiPatterns.Count > 0)
               sb.Append("    not when: ").AppendLine(string.Join("; ", spec.AntiPatterns));
         }
      }
   }
}
