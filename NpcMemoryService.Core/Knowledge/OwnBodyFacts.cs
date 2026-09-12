// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 6: the state of the body the character is standing in.
//
// Found by auditing the context rather than by a player report, which is what the pillar was for. A grep of
// EncounterContext returned ZERO for wound, injury and health, while the engine keeps Hero.IsWounded,
// HitPoints, MaxHitPoints and IsDisabled. So a lord carried off a field with a spear through his shoulder
// talks as though nothing had happened, and a man the game has permanently maimed never mentions it.
//
// WHAT THIS PACK DELIBERATELY DOES NOT CARRY: age. The first draft of this increment proposed it, and checking
// before writing - the discipline here_and_now earned - found it was already done: PromptBuilder states the
// character's years and their life stage (LifeStageDescriber) and already forbids affecting a youth they have
// outgrown. A second source for one fact is how two sources come to disagree. Named here so its absence reads
// as a decision.
//
// The property that makes it affordable: an unhurt character renders NOTHING. This pack is carried by
// everyone alive, and everyone alive is usually fine, so in the common case it costs zero characters of a
// prompt whose Compact form has no room to spare.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   How badly hurt they are. Degree comes from the engine's own hit points against its own maximum; the
    ///   engine's <c>IsWounded</c> flag decides whether they count as wounded at all, the same way
    ///   <c>here_and_now</c> trusts the engine's night reading over arithmetic on the hour.
    /// </summary>
    public enum HurtBand
    {
        /// <summary>Nothing read. Says nothing rather than claiming health.</summary>
        Unknown,

        /// <summary>Whole. Renders nothing: a sentence about not being injured is a sentence wasted.</summary>
        Unhurt,

        /// <summary>Knocked about, but nothing that would stop them.</summary>
        Scratched,

        /// <summary>Really hurt, and it shows in how they stand and what they can promise.</summary>
        Wounded,

        /// <summary>Barely on their feet. Anyone in the room can see it.</summary>
        Grievous
    }

    /// <summary>
    ///   The body this conversation is happening in. Everyone knows their own, nobody had been told.
    /// </summary>
    public sealed class OwnBodyFacts
    {
        /// <summary>How badly hurt, right now.</summary>
        public HurtBand Hurt { get; init; } = HurtBand.Unknown;

        /// <summary>
        ///   A lasting injury the game itself records (<c>Hero.IsDisabled</c>) - a maiming they carry for the
        ///   rest of the campaign, not a wound that heals. Separate from <see cref="Hurt" /> because a man can
        ///   be maimed and perfectly well, and the two say completely different things about him.
        /// </summary>
        public bool IsMaimed { get; init; }

        /// <summary>True when there is anything at all worth saying about the body.</summary>
        public bool HasAnythingToSay
            => IsMaimed || (Hurt != HurtBand.Unknown && Hurt != HurtBand.Unhurt);
    }
}
