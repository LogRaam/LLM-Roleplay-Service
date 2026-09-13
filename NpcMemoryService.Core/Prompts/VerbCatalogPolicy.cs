// Code written by Gabriel Mailhot, 12/09/2026.
//
// MEASURED, 2026-09-12. A Compact prompt is 15,957 characters of a 16,000-character ceiling. Of that, the
// GAME ACTIONS catalogue is 7,019 - sixty-nine verbs, 105 characters each at the median, a flat distribution
// with nothing left to shorten. The character himself gets 560. Forty-four percent of what a small model
// reads is a list of deeds; three and a half percent is who it is.
//
// The catalogue was measured on a character for whom NOT ONE availability flag was set, so not one of the
// gated teaching sections rendered - and it still paid for all sixty-nine verbs, every one of which the game
// would have refused.
//
// WHY THAT IS SAFE TO CUT, which is the part that took the measuring. Fifty-three of the sixty-nine verbs are
// taught TWICE: once here, unconditionally, and once in a section of their own that renders only when the
// deed is actually possible - and that section carries the whole [ACTION] block, its parameters and its
// conditions. The other sixteen come through ExtraActionTeachings, which is gated the same way by the mod's
// verb registry. Nothing needs this list in order to be emittable. It is a second, unconditional copy of
// teaching that already exists conditionally, and the conditions are already computed and already travelling
// in the context (NpcCanBeAppointedGovernor, ConsortEligible, PlayerCanMediatePeace, and a dozen more).
//
// SO THE CATALOGUE BECOMES AN INDEX OF WHAT THIS PROMPT ACTUALLY TAUGHT, and it is DERIVED rather than
// declared - it reads the finished prompt back and lists what is in it. A hand-kept map of verb to gate would
// have rotted the way cr.help's hand-kept inventory rotted (70 commands of 259), and the cure there was the
// same: make the thing enumerable and check completeness against it, never keep the list better.
//
// WHAT THIS MUST NOT DO, and the whole shape of the rule below exists to prevent it: silently remove a verb
// the model could otherwise have emitted. Absence does not read as "forbidden" to a language model, it reads
// as "not mentioned", and the model then supplies the permission itself - Gabriel's ruling of 09/09/2026 and
// the captive cut-off of 12/09/2026 are the same lesson twice. So a verb is dropped ONLY on positive
// evidence: it must be named in Gated below, which says "this verb has a teaching of its own that renders
// when it is possible", AND that teaching must be absent from this turn's prompt. Everything else stays.
// Unknown is not closed. A verb whose closed door is worth NAMING is named by DeedUnavailabilityPolicy, which
// is a separate channel and untouched by this.
//
// AND THE ASYMMETRY THAT KEEPS IT HONEST: forgetting to add a verb to Gated costs nothing (it stays listed,
// exactly as today). Wrongly adding one is caught by VerbCatalogPolicyTests, which renders a maximally
// permissive prompt and asserts every gated verb is taught in it. A teaching that changes shape turns the
// test red instead of turning a capability off.
//
// NOT the Action Interpreter. ActionInterpreterPromptBuilder reads GameActionCatalog directly for a different
// job - reading a model's prose back into verbs - and must keep every verb whether or not it is offerable
// this turn. Nothing here touches that path.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NpcMemoryService.Core.Actions;

#endregion

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    ///   Decides which verbs the GAME ACTIONS catalogue lists, by reading back what the finished prompt
    ///   already taught. Pure: it takes text and a vocabulary and returns text.
    /// </summary>
    public static class VerbCatalogPolicy
    {
        /// <summary>
        ///   Stands in the prompt where the verb list goes, until the prompt is finished and it is known what
        ///   was taught. The catalogue is rendered near the TOP and the teaching sections all come after it,
        ///   so the list cannot be composed in place - it has to be filled in on the way out.
        /// </summary>
        public const string Placeholder = "<<<CR_VERB_CATALOG>>>";

        /// <summary>
        ///   Verbs that have a teaching of their own, rendered only when the deed is actually possible. These
        ///   and only these may be dropped from the catalogue, and only on a turn where that teaching is
        ///   absent. Conservative on purpose: this is a floor that can grow, and every name on it is pinned
        ///   by a test that renders a permissive prompt and finds it taught there.
        /// </summary>
        public static IReadOnlyCollection<string> Gated { get; } = new HashSet<string>(StringComparer.Ordinal) {
            // The council offers, each one gated on its own host-computed NpcCanX flag.
            "appoint_governor", "assign_party_role", "rejoin_party", "dispatch_mission", "mediate_peace",
            "grant_fief", "revoke_fief", "expel_from_clan", "grant_stipend", "swear_oath",

            // Riding and escort, gated on whether this person is or could be doing it.
            "follow_me", "dismiss_escort", "ride_with_me", "part_ways",

            // Bonds, each gated on an eligibility the host resolves.
            "take_as_consort", "take_as_secret_lover", "open_relationship", "close_relationship", "end_affair",

            // Prisoners and recruitment.
            "recruit_prisoner", "recruit_notable", "rescue_prisoner", "teach_skill", "join_clan",
            "take_into_service",
            "end_mercenary", "recall_companion", "scheme_heed",

            // Added 2026-09-13 from a live report: a companion in a PRIVATE BRIEFING reached for
            // scheme_assist, the bridge refused it, and the player read "(There is no plot of theirs you
            // could help with just now.)" in the middle of his own scene. The verb is gated - on
            // SchemeAgentTargetName - and the first pass missed it because the gate reads through a local
            // (`string? target = context?.X; if (IsNullOrWhiteSpace(target)) return;`) rather than testing
            // the context inline. A reminder that the Gated list is a floor found by evidence, never a
            // reading of the code: each of these earns its place by the permissive test finding it taught.
            // pay_blackmail was offered to the same test and REFUSED: nothing in a permissive prompt
            // teaches it, so gating it would have deleted it rather than trimmed a duplicate. Left out,
            // which is the whole point of making the list earn itself.
            "scheme_assist", "reassure_companion", "retire"
        };

        /// <summary>
        ///   Fills the catalogue in, now that the prompt is finished and it is known what it taught. Returns
        ///   the prompt unchanged when there is no placeholder in it, so a builder that never rendered a
        ///   catalogue - or a second call on an already-resolved prompt - is a no-op rather than a fault.
        /// </summary>
        public static string Resolve(string prompt, IEnumerable<GameActionDefinition>? vocabulary,
                                     LeanPromptLevel lean)
        {
            if (string.IsNullOrEmpty(prompt) || prompt.IndexOf(Placeholder, StringComparison.Ordinal) < 0)
                return prompt;

            List<GameActionDefinition> all = (vocabulary ?? Enumerable.Empty<GameActionDefinition>())
                                             .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Type))
                                             .ToList();

            HashSet<string> taught = TaughtIn(prompt);

            var sb = new StringBuilder();

            foreach (GameActionDefinition def in all.Where(d => Offered(d.Type, taught)))
            {
                string parameterList = def.Parameters == null || def.Parameters.Count == 0
                    ? string.Empty
                    : $" (parameters: {string.Join(", ", def.Parameters)})";

                sb.AppendLine($"- {def.Type}: {ActionVocabularyPolicy.Describe(def.Description, lean)}{parameterList}");
            }

            // TrimEnd so the replacement leaves exactly the blank line the surrounding section already wrote,
            // rather than a doubled one that would drift every golden-prompt comparison.
            return prompt.Replace(Placeholder + Environment.NewLine, sb.ToString())
                         .Replace(Placeholder, sb.ToString().TrimEnd());
        }

        /// <summary>
        ///   Whether this verb is listed. A verb with no teaching of its own is always listed - unknown is not
        ///   closed. A gated one is listed exactly when this prompt taught it.
        /// </summary>
        public static bool Offered(string type, HashSet<string> taught)
            => !Gated.Contains(type) || taught.Contains(type);

        /// <summary>
        ///   Every verb this prompt teaches an [ACTION] block for, read back out of the finished text. Reading
        ///   the OUTPUT rather than a map of the code is what makes this incapable of drifting from it: a
        ///   teaching that moves, or arrives from the mod's own verb registry through ExtraActionTeachings, is
        ///   seen the same way.
        /// </summary>
        public static HashSet<string> TaughtIn(string? prompt)
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(prompt)) return found;

            // The trailing \r? is load-bearing: AppendLine writes CRLF on Windows, so without it the
            // $ anchor never reaches a line end, this matches NOTHING at all - silently - and every
            // gated verb disappears from the catalogue at once.
            // gated verb disappears from the catalogue at once.
            foreach (Match m in Regex.Matches(prompt!, @"(?m)^type:[ \t]*([a-z_]+)[ \t]*\r?$"))
                found.Add(m.Groups[1].Value);

            return found;
        }
    }
}
