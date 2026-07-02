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
        {
            // Append event when present and non-trivial.
            // An empty or whitespace summary would pollute the history with lines
            // like "Day N (Other):" that carry no information for future prompts.
            if (response.NewEventData != null && !string.IsNullOrWhiteSpace(response.NewEventData.Summary))
            {
                ApplyNotableEvent(profile, response.NewEventData.Type, response.NewEventData.Summary, gameDay);
            }

            // Adjust reputation when present
            if (response.Reputation != null) ApplyReputationDelta(profile, response.Reputation.ClanDelta ?? 0);

            // Advance the romantic arc based on the event type and current trust level.
            if (response.NewEventData != null
                && !string.IsNullOrWhiteSpace(response.NewEventData.Summary))
            {
                AdvanceRomanticStatus(profile, response.NewEventData.Type, profile.ReputationWithPlayer);
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
        private static void AdvanceRomanticStatus(NpcProfile profile, NotableEventType eventType, int relation)
        {
            if (profile.Romantic == null) return;

            var status    = profile.Romantic.Status;
            bool isMarried = !string.IsNullOrWhiteSpace(profile.SpouseName);
            bool isCasual  = profile.Romantic.Preferences != null
                && profile.Romantic.Preferences.Contains(RomanticPreference.Casual);
            bool isIntense = profile.Romantic.Preferences != null
                && profile.Romantic.Preferences.Contains(RomanticPreference.Intense);

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

            // ── Married: intimacy → SecretLover ─────────────────────────────
            if (isMarried)
            {
                if (eventType == NotableEventType.Intimacy
                    && status != RomanticStatus.SecretLover
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
