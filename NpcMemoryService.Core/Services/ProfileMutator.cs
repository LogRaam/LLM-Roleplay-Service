// Code written by Gabriel Mailhot, 18/05/2026.
// Updated 28/05/2026: Guard against empty/blank event summaries to keep history clean.

#region

using System;
using System.Linq;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Services
{
    /// <summary>
    ///   Applies the structured outcome of an LLM dialogue turn (a <see cref="ParsedResponse" />)
    ///   to an <see cref="NpcProfile" />. Centralizes the mutation logic so it can be reused
    ///   by the mod, the ConsoleRunner, and tests without duplication.
    ///   Keeps <see cref="NpcProfile" /> as a clean POCO — it does not know about parsing,
    ///   and parsing does not know about NpcProfile. The mutator bridges the two.
    /// </summary>
    public static class ProfileMutator
    {
        /// <summary>Regard (ReputationWithPlayer) at which a warm act can rekindle an Estranged bond (M-J5). Tuning.</summary>
        private const int EstrangedReconcileRelation = 30;

        /// <summary>
        ///   Applies the response to the profile:
        ///   - <see cref="ParsedResponse.NewEventData" /> → appends a <see cref="NotableEvent" />
        ///     (skipped if Summary is empty or whitespace-only — LLMs occasionally emit
        ///     syntactically valid but semantically empty [EVENT] blocks).
        ///   - <see cref="ParsedResponse.Reputation" />   → adjusts <see cref="NpcProfile.ReputationWithPlayer" />.
        ///   The Memory section is descriptive only (topic/sentiment/decision) and is NOT
        ///   persisted to the profile by default — it is information the LLM emitted for
        ///   its own future reasoning, already implicit in the event summaries.
        /// </summary>
        /// <param name="profile">The NPC whose memory is being updated. Must not be null.</param>
        /// <param name="response">The parsed LLM response.</param>
        /// <param name="gameDay">The current game day, used to timestamp the new event.</param>
        public static void Apply(NpcProfile profile, ParsedResponse response, int gameDay)
            => Apply(profile, response, gameDay, romanticContentEnabled: true, orientationCompatible: true);

        public static void Apply(NpcProfile profile, ParsedResponse response, int gameDay, bool romanticContentEnabled)
            => Apply(profile, response, gameDay, romanticContentEnabled, orientationCompatible: true);

        /// <summary>
        ///   Same authoritative mutation, with explicit gates on whether the romantic arc may advance (romance
        ///   audit M-R2/M-R3). <paramref name="romanticContentEnabled" /> is false when adult content is Off (the
        ///   whole romance system is disabled there), which froze a hallucinated at-Off intimacy from silently
        ///   climbing the arc. <paramref name="orientationCompatible" /> is false for a pair the NPC's orientation
        ///   rules out, so POSITIVE advancement is skipped for an implausible pair, matching the jealousy filter;
        ///   negative degradation and event/reputation recording still apply either way.
        /// </summary>
        public static void Apply(NpcProfile profile, ParsedResponse response, int gameDay, bool romanticContentEnabled, bool orientationCompatible)
            => Apply(profile, response, gameDay, romanticContentEnabled, orientationCompatible, applyReputation: true);

        /// <summary>
        ///   Same authoritative mutation, with an explicit switch for the legacy <c>[REPUTATION]</c> block (regard
        ///   audit C2). The mod disables that block in the prompt (it is never TAUGHT), yet the parser still parses
        ///   it and this method would still APPLY it, ungated and uncapped: a weak or jailbroken model emitting the
        ///   legacy format could farm regard, or double-count an offense alongside <c>change_relation</c>. The mod
        ///   passes <paramref name="applyReputation" />=false so the block can never move regard; the personal
        ///   ledger moves only through the gated change_relation / RelationGate path. Defaults true for the console
        ///   and tests, which drive the block deliberately.
        /// </summary>
        public static void Apply(NpcProfile profile, ParsedResponse response, int gameDay, bool romanticContentEnabled, bool orientationCompatible, bool applyReputation)
        {
            // Append event when present and non-trivial.
            // An empty or whitespace summary would pollute the history with lines
            // like "Day N (Other):" that carry no information for future prompts.
            if (response.NewEventData != null && !string.IsNullOrWhiteSpace(response.NewEventData.Summary))
            {
                ApplyNotableEvent(profile, response.NewEventData.Type, response.NewEventData.Summary, gameDay);
            }

            // Adjust reputation when present. C2: skipped when the caller disabled the legacy [REPUTATION] block
            // (the mod), so a model emitting it unprompted can never move regard outside the gated change_relation path.
            if (applyReputation && response.Reputation != null) ApplyReputationDelta(profile, response.Reputation.ClanDelta ?? 0);

            // Advance the romantic arc based on the event type and current trust level. Skipped entirely when the
            // romance system is off (M-R3), so no romantic state accumulates behind the player's back.
            if (romanticContentEnabled
                && response.NewEventData != null
                && !string.IsNullOrWhiteSpace(response.NewEventData.Summary))
            {
                AdvanceRomanticStatus(profile, response.NewEventData.Type, profile.ReputationWithPlayer, orientationCompatible);
                AdvanceAttraction(profile, response.NewEventData.Type, orientationCompatible);
            }

            // Record newly discovered trait — deduplicated by key so the same fact is never stored twice.
            if (response.Discovery != null
                && !string.IsNullOrWhiteSpace(response.Discovery.Key)
                && !profile.DiscoveredTraits.Any(t =>
                    string.Equals(t.Key, response.Discovery.Key, StringComparison.OrdinalIgnoreCase)))
            {
                profile.DiscoveredTraits.Add(new DiscoveredTrait {
                    Key         = response.Discovery.Key,
                    Description = response.Discovery.Description,
                    GameDay     = gameDay
                });
            }
        }

        /// <summary>
        ///   Appends a <see cref="NotableEvent" /> to the profile, guarded so a second
        ///   [EVENT FirstMeeting] never duplicates an already-recorded first meeting.
        ///   Callers with a full <see cref="ParsedResponse" /> should prefer <see cref="Apply" />;
        ///   this is exposed separately so a single-event caller (e.g. the mod's captivity or
        ///   jealousy systems, which record events the LLM never emitted) shares the same guard.
        /// </summary>
        /// <param name="profile">The NPC whose history is being updated. Must not be null.</param>
        /// <param name="type">The event's category.</param>
        /// <param name="summary">Natural-language summary. Callers should pre-check it is non-blank.</param>
        /// <param name="gameDay">The current game day, used to timestamp the event.</param>
        public static void ApplyNotableEvent(NpcProfile profile, NotableEventType type, string summary, int gameDay)
        {
            if (type == NotableEventType.FirstMeeting
                && profile.Events.Any(e => e.type == NotableEventType.FirstMeeting))
                return;

            profile.Events.Add(new NotableEvent(gameDay, type, summary));
        }

        /// <summary>
        ///   Adjusts <see cref="NpcProfile.ReputationWithPlayer" /> by <paramref name="delta" />,
        ///   clamped to [-100, 100] — the single authoritative path for this mutation, so no
        ///   caller can push the value outside its documented range.
        /// </summary>
        /// <param name="profile">The NPC whose reputation is being updated. Must not be null.</param>
        /// <param name="delta">Signed change to apply, positive or negative.</param>
        public static void ApplyReputationDelta(NpcProfile profile, int delta)
        {
            int updated = profile.ReputationWithPlayer + delta;
            profile.ReputationWithPlayer = Math.Max(-100, Math.Min(100, updated));
        }

        /// <summary>
        ///   Romance audit C3 save-heal: whether the profile of the player's OWN spouse needs its romantic
        ///   status healed to <see cref="RomanticStatus.Committed" />. The player's spouse should always sit at
        ///   Committed, but marriage never set it and a post-wedding intimacy corrupted it to SecretLover, so
        ///   older saves carry a spouse stuck at a pre-marriage status or clandestine-affair status. Those are
        ///   healed; a deliberately damaged bond (<see cref="RomanticStatus.Estranged" />/
        ///   <see cref="RomanticStatus.Broken" />) is left exactly as the marriage earned it. Pure so the host's
        ///   one-shot backfill is a tested rule, not an ad-hoc condition.
        /// </summary>
        public static bool SpouseStatusNeedsHeal(RomanticStatus current)
            => current != RomanticStatus.Committed
               && current != RomanticStatus.Estranged
               && current != RomanticStatus.Broken;

        /// <summary>
        ///   Advances <see cref="RomanticProfile.Status" /> based on the event type and
        ///   the current trust level. Rules differ by NPC personality:
        ///   <list type="bullet">
        ///     <item>Married — intimacy triggers <see cref="RomanticStatus.SecretLover" />; otherwise unchanged.</item>
        ///     <item>Casual   — goes directly to <see cref="RomanticStatus.Intimate" /> at relation ≥ 5.</item>
        ///     <item>Intense  — skips <see cref="RomanticStatus.Curious" />, reaches Intimate at relation ≥ 10.</item>
        ///     <item>Standard — full progression: Curious → Courting → Intimate at thresholds 10 / 20.</item>
        ///   </list>
        ///   Negative events (Conflict / Betrayal) can push any arc toward Estranged or Broken.
        /// </summary>
        /// <summary>
        ///   Moves <see cref="RomanticProfile.AttractionToPlayer" /> by this event's
        ///   <see cref="AttractionEvolutionPolicy" /> delta, clamped to [-100, 100] (romance audit M-R5: the stat
        ///   was frozen because only duels ever wrote it). Runs alongside the arc advancement, under the same
        ///   romance-enabled gate; court duels keep adding on top of whatever conversation builds.
        /// </summary>
        private static void AdvanceAttraction(NpcProfile profile, NotableEventType eventType, bool orientationCompatible)
        {
            if (profile.Romantic == null) return;

            int delta = AttractionEvolutionPolicy.Delta(eventType, orientationCompatible);
            if (delta == 0) return;

            profile.Romantic.AttractionToPlayer =
                Math.Max(-100, Math.Min(100, profile.Romantic.AttractionToPlayer + delta));
        }

        private static void AdvanceRomanticStatus(NpcProfile profile, NotableEventType eventType, int relation, bool orientationCompatible)
        {
            if (profile.Romantic == null) return;

            var status    = profile.Romantic.Status;
            bool isMarried = !string.IsNullOrWhiteSpace(profile.SpouseName);
            bool isIntense = profile.Romantic.Preferences != null
                && profile.Romantic.Preferences.Contains(RomanticPreference.Intense);
            // Minor 10 (romance audit): Casual and Intense are contradictory cadences and should not coexist, but
            // if a profile carries BOTH, the Casual branch below (checked first) used to fire and leave the Intense
            // cadence permanently dead. Make the precedence explicit and deterministic: Intense wins, so its arc is
            // never silently swallowed.
            bool isCasual  = !isIntense
                && profile.Romantic.Preferences != null
                && profile.Romantic.Preferences.Contains(RomanticPreference.Casual);

            // Romance audit M-R7: a SecretLover whose third-party spouse has died (SpouseName has since cleared)
            // is no longer "married to another", so the clandestine affair surfaces as an open Intimate bond
            // instead of freezing forever under a "married to another" framing that no longer applies. Corrected
            // up front so THIS event then resolves from the right state.
            if (status == RomanticStatus.SecretLover && !isMarried)
            {
                profile.Romantic.Status = RomanticStatus.Intimate;
                status = RomanticStatus.Intimate;
            }

            // ── Negative events degrade any arc ─────────────────────────────
            if (eventType == NotableEventType.Betrayal
                && (status == RomanticStatus.Courting
                    || status == RomanticStatus.Intimate
                    || status == RomanticStatus.SecretLover))
            {
                profile.Romantic.Status = RomanticStatus.Broken;
                return;
            }
            if (eventType == NotableEventType.Conflict
                && (status == RomanticStatus.Intimate || status == RomanticStatus.SecretLover))
            {
                profile.Romantic.Status = relation <= -30
                    ? RomanticStatus.Broken
                    : RomanticStatus.Estranged;
                return;
            }
            if (eventType == NotableEventType.Conflict && status == RomanticStatus.Estranged)
            {
                profile.Romantic.Status = RomanticStatus.Broken;
                return;
            }

            // Romance audit M-R2: POSITIVE advancement below is gated on the pair being orientation-plausible, the
            // same filter the jealousy system already applies. Without this, a romantic act the NPC's orientation
            // rules out (an LLM hallucination) still climbed the arc while jealousy ignored it, a visible
            // incoherence (the profile says "Intimate" but no jealous party ever reacts). Negative degradation
            // above stays UNCONDITIONAL: a betrayal or quarrel wounds a bond whatever the orientation.
            if (!orientationCompatible) return;

            // Romance audit M-J5: Estranged means "trust broken but FEELING REMAINS", so it is RECOVERABLE,
            // unlike Broken ("no path back"). A warm act at genuinely restored regard rekindles the bond instead
            // of leaving it frozen forever: intimacy revives it as Intimate, a flirt as a rebuilding Courting.
            if (status == RomanticStatus.Estranged
                && (eventType == NotableEventType.Flirt || eventType == NotableEventType.Intimacy)
                && relation >= EstrangedReconcileRelation)
            {
                profile.Romantic.Status = eventType == NotableEventType.Intimacy
                    ? RomanticStatus.Intimate
                    : RomanticStatus.Courting;
                return;
            }

            // ── Married: intimacy → SecretLover ─────────────────────────────
            // Romance audit C3: a Committed bond ALWAYS means committed to the PLAYER (legal marriage or consort),
            // never to a third party, so intimacy with the player is open and legitimate and must NOT be
            // reclassified as a clandestine affair. Without this guard a real player-spouse flipped to SecretLover
            // ("married to another, kept hidden") on the first post-wedding intimacy and froze there for life.
            if (isMarried)
            {
                if (eventType == NotableEventType.Intimacy
                    && status != RomanticStatus.SecretLover
                    && status != RomanticStatus.Committed
                    && status != RomanticStatus.Broken)
                {
                    profile.Romantic.Status = RomanticStatus.SecretLover;
                }
                return;
            }

            // ── Casual: direct path to Intimate ─────────────────────────────
            if (isCasual)
            {
                if (eventType == NotableEventType.Intimacy
                    && relation >= 5
                    && status != RomanticStatus.Intimate
                    && status != RomanticStatus.Broken)
                {
                    profile.Romantic.Status = RomanticStatus.Intimate;
                }
                return;
            }

            // ── Intense: skip Curious, Courting → Intimate at ≥ 10 ──────────
            if (isIntense)
            {
                if (eventType == NotableEventType.Flirt && status == RomanticStatus.None)
                {
                    profile.Romantic.Status = RomanticStatus.Courting;
                    return;
                }
                if (eventType == NotableEventType.Intimacy
                    && relation >= 10
                    && status == RomanticStatus.Courting)
                {
                    profile.Romantic.Status = RomanticStatus.Intimate;
                }
                return;
            }

            // ── Standard progression ─────────────────────────────────────────
            switch (status)
            {
                case RomanticStatus.None:
                    if (eventType == NotableEventType.Flirt)
                        profile.Romantic.Status = RomanticStatus.Curious;
                    break;
                case RomanticStatus.Curious:
                    if (eventType == NotableEventType.Flirt && relation >= 10)
                        profile.Romantic.Status = RomanticStatus.Courting;
                    break;
                case RomanticStatus.Courting:
                    if (eventType == NotableEventType.Intimacy && relation >= 20)
                        profile.Romantic.Status = RomanticStatus.Intimate;
                    break;
            }
        }
    }
}
