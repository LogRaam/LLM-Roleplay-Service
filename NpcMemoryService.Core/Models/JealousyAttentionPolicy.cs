// Code written by Gabriel Mailhot, 17/07/2026.
// Jealousy, neglect measure (romance audit M-J4). A co-partner grows jealous when NEGLECTED too long, measured as
// days since they were last attended to. The old measure took the most recent of ANY recorded event, which
// silently reset the neglect clock on things that are not attention at all: a jealousy REACTION the system itself
// planted (self-defeating, the loop could never escalate), a received letter (logged as Other), a quarrel, a
// captivity. This pure rule names what actually counts as the player attending to a partner, so the competition
// and estrangement loop can finally advance when a partner truly is being ignored.

namespace NpcMemoryService.Core.Models
{
    /// <summary>Which recorded events count as the player having ATTENDED TO a partner, for the neglect clock.</summary>
    public static class JealousyAttentionPolicy
    {
        /// <summary>
        ///   True only for a positive, player-directed interaction: a first meeting, a collaboration, an
        ///   agreement, a flirt, or an intimacy. Deliberately EXCLUDES the jealousy reaction itself, a captivity,
        ///   a quarrel/betrayal/confrontation, a farewell, and the catch-all Other (which holds received letters
        ///   as well as ambiguous notes), none of which is the player warmly attending to this partner.
        /// </summary>
        public static bool CountsAsAttention(NotableEventType type)
            => type == NotableEventType.FirstMeeting
               || type == NotableEventType.Collaboration
               || type == NotableEventType.Agreement
               || type == NotableEventType.Flirt
               || type == NotableEventType.Intimacy;
    }
}
