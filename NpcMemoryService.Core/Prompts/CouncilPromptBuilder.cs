// Code written by Gabriel Mailhot, 22/08/2026.
// The council's OWN ONE-CALL GROUP SCENE prompt. The existing council path (PromptBuilder.AppendCouncilNarratorFraming
// and the plain whole-table framing beside it) piggybacks the ordinary 1:1 CHAT prompt: one "anchor" NPC speaks
// the real [DIALOGUE] and every other seated member only gets an optional [WITNESS_REACTION], which a model may
// simply leave silent. The result Gabriel flagged: one councillor dominates a sitting, the rest fade. This
// builder replaces that shape for a NEW, dedicated council call: every seated member speaks in their own
// [SPEAKER: Name] block, Discord/chat style, equally required, none privileged as the "real" speaker. It KEEPS
// the two rules the old framing already got right, reworded for the new format: (1) nothing is CARRIED OUT at
// the table (no gold, troops, marriage, scheme, no [QUEST]/[QUEST_COMPLETE]/[EVENT]); a real commitment is
// recorded as a [RESOLUTION], settled only when the council rises; (2) a genuine regard shift IS immediate, an
// actor-attributed change_relation [ACTION], never a promise. The [RESOLUTION] block's own field names
// (type/actor/target_settlement/detail, plus every catalogue-specific grounding field) are BYTE-FOR-BYTE the
// existing format, so the mod's existing FinalizeCouncilResolutions/SealCouncilEngagements/CouncilLift keep
// parsing it unchanged regardless of which of the two council prompt paths produced it.
//
// Refinement (22/08/2026, same day): (1) a real player-reported bug, a male councillor voiced as a woman
// because the roster never stated anyone's sex and the model guessed from the name; CouncilMemberInput.IsFemale
// is now rendered explicitly and unmissably on every roster line. (2) the author wants brief narrator "camera"
// hand-offs between speakers, not just a single leading beat; [SCENE] may now appear anywhere, interspersed
// between [SPEAKER] blocks, optional and brief rather than mandatory or singular.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Builds the system prompt for one council one-call group-scene turn. Pure and stateless: every fact comes
   ///   from the <see cref="CouncilPromptInput" /> the caller passes to <see cref="Build" />.
   ///   <para>
   ///     Assembled STABLE-HEAD first (<see cref="StableHead" />, identical on every call regardless of roster or
   ///     turn, so a provider can prompt-cache it) followed by the VARIABLE per-turn tail: the seated roster, the
   ///     world state, the transcript so far, the player's line, and which extra [RESOLUTION] kinds are on offer.
   ///   </para>
   /// </summary>
   public static class CouncilPromptBuilder
   {
      private static readonly string _stableHead = BuildStableHead();

      /// <summary>
      ///   The invariant head of every council group-scene prompt: the framing, the output format, the
      ///   every-member-speaks rule, the [RESOLUTION] channel, and the actor-attributed change_relation channel.
      ///   Exposed so a test can assert a built prompt STARTS with it.
      /// </summary>
      internal static string StableHead => _stableHead;

      /// <summary>Builds the full council group-scene prompt for <paramref name="input" />.</summary>
      public static string Build(CouncilPromptInput input)
      {
         var sb = new StringBuilder();

         sb.Append(_stableHead);
         AppendRoster(sb, input.Roster);
         AppendPlayerIdentity(sb, input);
         AppendWorldState(sb, input);
         AppendTranscript(sb, input.TranscriptSoFar);
         AppendOfferedKinds(sb, input.OfferedResolutionKinds);
         AppendPlayerLine(sb, input);

         return sb.ToString();
      }

      #region private

      private static string BuildStableHead()
      {
         var sb = new StringBuilder();

         sb.AppendLine("YOU ARE NARRATING A COUNCIL: A ROUND-TABLE GROUP SCENE, ONE CALL, EVERY SEAT VOICED:");
         sb.AppendLine("This is a single reply covering the WHOLE table. Every seated councillor present speaks a");
         sb.AppendLine("full, substantive contribution this turn, in their own voice, Discord/chat style: their name,");
         sb.AppendLine("then their words. No one member presides over the rest or answers on the table's behalf. The");
         sb.AppendLine("floor is open to everyone: you may respond to, build on, agree or disagree with, or address by");
         sb.AppendLine("name another person present, not only the player. Do not simply defer back to the player, and");
         sb.AppendLine("never write another person's reply, only your own.");
         sb.AppendLine();

         sb.AppendLine("OUTPUT FORMAT (produce ONLY these blocks, freely interleaved in this shape, nothing else):");
         sb.AppendLine("[SCENE] a brief connective narrator beat, one or two lines, belonging to no single speaker: a");
         sb.AppendLine("hand-off of the floor (\"Ajin turns to Hophtalamos, who takes up the thread\"), a reaction");
         sb.AppendLine("(\"Sophia startles at his words\"), the room, the mood. [SCENE] beats are OPTIONAL and may");
         sb.AppendLine("appear anywhere: before the first speaker, between any two [SPEAKER] blocks, or not at all.");
         sb.AppendLine("Keep each one brief, and use one only where it genuinely helps the scene flow; do NOT insert");
         sb.AppendLine("one between every pair of speakers, or the scene bloats.");
         sb.AppendLine("[SPEAKER: Name]");
         sb.AppendLine("That member's full contribution, exactly as themselves. A block may also open with the");
         sb.AppendLine("member's own brief reaction before their words proper.");
         sb.AppendLine("[SPEAKER: Name]");
         sb.AppendLine("A further block, from the same or a different member: cross-talk, a retort, an aside,");
         sb.AppendLine("optionally preceded by its own [SCENE] hand-off. Take as many [SPEAKER] blocks as the scene");
         sb.AppendLine("genuinely calls for.");
         sb.AppendLine("Use the member's name EXACTLY as it is listed below in every [SPEAKER: Name] tag.");
         sb.AppendLine();

         sb.AppendLine("EVERY SEATED MEMBER LISTED BELOW MUST SPEAK, AT LEAST ONCE, THIS TURN:");
         sb.AppendLine("Equal participation. No one is left silent, and no one monopolises the floor at the expense of");
         sb.AppendLine("another member's turn to speak. A member may take a second [SPEAKER: Name] block for");
         sb.AppendLine("cross-talk, but only after every other seated member has had their own first word.");
         sb.AppendLine();

         sb.AppendLine("NO DEED IS SEALED AT THIS TABLE, BUT YOUR REGARD IS REAL:");
         sb.AppendLine("This is counsel, not a contract. Nothing is CARRIED OUT here and now: no gold changes hands,");
         sb.AppendLine("no troops are lent, no marriage or scheme is sealed, and no one emits a [QUEST],");
         sb.AppendLine("[QUEST_COMPLETE] or [EVENT] block. A real commitment is RECORDED below as a [RESOLUTION] and");
         sb.AppendLine("settled only when the council rises.");
         sb.AppendLine("What DOES change here and now is how each of you FEELS. If what is said at this table genuinely");
         sb.AppendLine("moves a member's regard for the player, warmer or colder, let it show and let it count: that is");
         sb.AppendLine("not a deed but an honest reaction. Register it as an [ACTION] change_relation, attributed to");
         sb.AppendLine("whichever member it belongs to, never as a promise for later.");
         sb.AppendLine();

         sb.AppendLine("RECORDING WHAT THE TABLE DECIDES:");
         sb.AppendLine("When a SEATED MEMBER commits to something the game should carry out (a task taken up, a");
         sb.AppendLine("mission agreed to), name them and record it instead of just speaking it:");
         sb.AppendLine("[RESOLUTION]");
         sb.AppendLine("type: quest");
         sb.AppendLine("actor: <the member's name exactly as listed below>");
         sb.AppendLine("target_settlement: <the town or village the deed concerns, spelled exactly as a real place>");
         sb.AppendLine("detail: <what they pledge, in plain words>");
         sb.AppendLine("[/RESOLUTION]");
         sb.AppendLine("target_settlement is REQUIRED for a quest pledge: a task the table sets always concerns a");
         sb.AppendLine("PLACE, and without a real settlement named the game cannot act on the decision. Worked example:");
         sb.AppendLine("[RESOLUTION]");
         sb.AppendLine("type: quest");
         sb.AppendLine("actor: Ajin the Hawk");
         sb.AppendLine("target_settlement: Pravend");
         sb.AppendLine("detail: Ajin will ride to Pravend and clear the bandits raiding its roads.");
         sb.AppendLine("[/RESOLUTION]");
         sb.AppendLine();

         sb.AppendLine("REGISTERING A LIVE REGARD SHIFT:");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: change_relation");
         sb.AppendLine("actor: <the member whose regard moved, exactly as listed below>");
         sb.AppendLine("delta: <a small signed number, e.g. 2 or -2>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Worked example:");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: change_relation");
         sb.AppendLine("actor: Hophtalamos the Shipwright");
         sb.AppendLine("delta: 2");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine();

         sb.AppendLine("Emit NO section other than [SCENE], [SPEAKER: Name], [RESOLUTION], and [ACTION] type:");
         sb.AppendLine("change_relation. No other [ACTION] type, no [QUEST], [QUEST_COMPLETE], [EVENT], [DIALOGUE], or");
         sb.AppendLine("[WITNESS_REACTION] block belongs in this reply.");

         return sb.ToString();
      }

      private static void AppendRoster(StringBuilder sb, IReadOnlyList<CouncilMemberInput> roster)
      {
         sb.AppendLine();
         sb.AppendLine("THE COUNCIL PRESENT (every one of these must speak this turn):");
         sb.AppendLine("Voice EACH member true to their AGE and life stage: a child speaks and reasons as a child");
         sb.AppendLine("(short attention, play, blunt or shy, not political counsel), a green youth as a youth, an");
         sb.AppendLine("elder with the weight of years. Never write a young child as a small, articulate adult.");

         if (roster == null || roster.Count == 0)
         {
            sb.AppendLine("(the roster is empty)");
            return;
         }

         foreach (CouncilMemberInput member in roster)
         {
            if (member == null || string.IsNullOrWhiteSpace(member.Name)) continue;

            var clauses = new List<string>();
            if (member.Age.HasValue) clauses.Add("aged " + member.Age.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(member.PersonaLine)) clauses.Add(member.PersonaLine!.Trim());
            if (!string.IsNullOrWhiteSpace(member.Culture)) clauses.Add(member.Culture!.Trim());
            clauses.Add("current regard toward the player: " + FormatRegard(member.RegardTowardPlayer));

            // Bug fix: with no sex stated the model guessed one from the name alone and got it wrong, voicing a
            // male councillor as a woman. Stated right beside the name, before anything else, so it is the very
            // first fact read about this seat and can never be missed or overridden by a later clause.
            sb.AppendLine("- " + member.Name.Trim() + ", " + DescribeSex(member.IsFemale) + ": "
                           + string.Join("; ", clauses));
         }
      }

      /// <summary>
      ///   Names the player and forbids the "the player" system label in the output. The stable head necessarily
      ///   uses "the player" as instruction vocabulary; without this the model echoed it into narration ("he turns
      ///   his gaze toward the player") instead of using the character's name (player report 2026-08-23).
      /// </summary>
      private static void AppendPlayerIdentity(StringBuilder sb, CouncilPromptInput input)
      {
         string? name = string.IsNullOrWhiteSpace(input.PlayerName) ? null : input.PlayerName!.Trim();
         if (name == null) return;

         sb.AppendLine();
         sb.AppendLine("THE PLAYER AT THIS TABLE IS NAMED " + name + ".");
         sb.AppendLine("Call them " + name + " (or address them directly as \"you\") in every [SCENE] beat and every");
         sb.AppendLine("[SPEAKER] line. NEVER write \"the player\" in your output: it is a system label, not a name.");
      }

      private static string DescribeSex(bool isFemale) => isFemale ? "a woman" : "a man";

      private static string FormatRegard(int regard)
         => (regard >= 0 ? "+" : string.Empty) + regard.ToString(System.Globalization.CultureInfo.InvariantCulture);

      private static void AppendWorldState(StringBuilder sb, CouncilPromptInput input)
      {
         var clauses = new List<string>();
         if (input.Day.HasValue) clauses.Add("day " + input.Day.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
         if (!string.IsNullOrWhiteSpace(input.Season)) clauses.Add(input.Season!.Trim());
         if (!string.IsNullOrWhiteSpace(input.Place)) clauses.Add("at " + input.Place!.Trim());

         if (clauses.Count == 0) return;

         sb.AppendLine();
         sb.AppendLine("WORLD STATE: " + string.Join(", ", clauses) + ".");
      }

      private static void AppendTranscript(StringBuilder sb, IReadOnlyList<string> transcriptSoFar)
      {
         List<string>? lines = transcriptSoFar?.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
         if (lines == null || lines.Count == 0) return;

         sb.AppendLine();
         sb.AppendLine("THE SITTING SO FAR (earlier turns this council, for continuity only, already spoken and not");
         sb.AppendLine("to be re-decided):");
         foreach (string line in lines)
            sb.AppendLine(line.Trim());
      }

      private static void AppendOfferedKinds(StringBuilder sb, IReadOnlyList<string> offeredResolutionKinds)
      {
         List<string>? kinds = offeredResolutionKinds?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
         if (kinds == null || kinds.Count == 0) return;

         sb.AppendLine();
         sb.AppendLine("BEYOND THE ALWAYS-AVAILABLE \"quest\" KIND, THIS COUNCIL MAY ALSO RESOLVE: "
                        + string.Join(", ", kinds) + ".");
         sb.AppendLine("Each still uses the exact [RESOLUTION] block shape above (type/actor/detail, plus whatever");
         sb.AppendLine("grounding field that kind needs); never propose a kind not named here.");
      }

      private static void AppendPlayerLine(StringBuilder sb, CouncilPromptInput input)
      {
         // Opening turn only: set the scene (who gathered, why, the mood) as a stage direction, never as a
         // spoken player line. The table then opens on its own below (PlayerLine is empty this turn).
         if (!string.IsNullOrWhiteSpace(input.OpeningCue))
         {
            sb.AppendLine();
            sb.AppendLine("THE SITTING IS JUST OPENING. THE SITUATION:");
            sb.AppendLine(input.OpeningCue!.Trim());
         }

         sb.AppendLine();
         sb.AppendLine(string.IsNullOrWhiteSpace(input.PlayerLine)
            ? "THE PLAYER HAS NOT YET SPOKEN THIS TURN: the table opens on its own."
            : "THE PLAYER SAYS THIS TURN, TO THE WHOLE TABLE:");

         if (!string.IsNullOrWhiteSpace(input.PlayerLine))
         {
            string player = string.IsNullOrWhiteSpace(input.PlayerName) ? "The player" : input.PlayerName!.Trim();
            sb.AppendLine(player + ": " + input.PlayerLine.Trim());
         }
      }

      #endregion
   }
}
