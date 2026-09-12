// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 9: what the streets know.
//
// Gabriel, 2026-09-12: "j'aimerais aussi qu'on s'intéresse aux gangs, aux bandits, etc. car plusieurs joueurs
// jouent des malfrats avec le mod Fourberie."
//
// The audit found something precise here, and it decided the shape of this pack: the VOICE of the underworld
// already exists. PromptBuilder matches behaviour guidelines by station and has done for a long time - a gang
// leader is already told "you are an outlaw and a cutthroat, not a nobleman. Speak coarse". So a player
// running a criminal campaign already meets characters who SOUND right and know nothing. The gap is knowledge,
// not persona, which is exactly the sort of thing that stays invisible until somebody grep's for it.
//
// TWO KINDS OF FACT, and they are the reason this pack is the first real use of Gabriel's depth axis:
//
//   The player's NOTORIETY is public. Guards recognise a wanted face, and so does a headman, a merchant and a
//   lord of that realm. Everyone at Ordinary depth.
//
//   WHO RUNS THE STREETS is not. That is underworld knowledge, and a count does not have it. Deep only.
//
// No threshold is invented for any of it: the engine's own CrimeModel classifies a crime rating as mild,
// moderate or severe, the same deference here_and_now shows the engine's night reading and own_body shows its
// IsWounded flag.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   How badly the player is wanted in THIS character's realm, on the engine's own reckoning rather than
    ///   on a threshold chosen here.
    /// </summary>
    public enum Notoriety
    {
        /// <summary>Nothing read. Says nothing rather than vouching for anybody.</summary>
        Unknown,

        /// <summary>No crimes here worth the name. Renders nothing: a clean record is not news.</summary>
        Clean,

        /// <summary>Petty trouble. Enough that people have heard, not enough that anyone acts.</summary>
        Mild,

        /// <summary>A known offender. Doors close and prices change.</summary>
        Moderate,

        /// <summary>Wanted in earnest. The garrison would take them if they could.</summary>
        Severe
    }

    /// <summary>
    ///   What the streets know: how the player stands with the law of this realm, and who holds the criminal
    ///   trade of this place. Read live and thrown away with the encounter, like every pack.
    /// </summary>
    public sealed class UnderworldFacts
    {
        /// <summary>How badly the player is wanted in this character's realm.</summary>
        public Notoriety PlayerNotoriety { get; init; } = Notoriety.Unknown;

        /// <summary>The place whose streets are in question.</summary>
        public string? PlaceName { get; init; }

        /// <summary>
        ///   Who holds the criminal trade here, when somebody does. Underworld knowledge: a lord of the same
        ///   town does not have it, which is the whole reason depth exists.
        /// </summary>
        public string? StreetsRunBy { get; init; }

        /// <summary>True when this character IS the one who holds them.</summary>
        public bool YouRunTheseStreets { get; init; }

        /// <summary>True when anything at all was read.</summary>
        public bool HasAnyReading
            => PlayerNotoriety != Notoriety.Unknown
               || YouRunTheseStreets
               || !string.IsNullOrWhiteSpace(StreetsRunBy);
    }
}
