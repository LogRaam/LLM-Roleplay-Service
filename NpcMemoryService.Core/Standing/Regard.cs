// Code written by Gabriel Mailhot, 21/09/2026.
// The mod's OWN regard, the one built in conversation, as a type rather than a bare int. See VanillaRelation.cs for
// why both exist and why neither should stay an int.
//
// The tiers below are the marks the code ALREADY decided on, gathered from docs/maps/constants.md on 21/09/2026.
// Nothing was re-tuned in the move. Two of them fall exactly on a RegardBands word boundary, which is the happy case:
// the character reading "deep affection" is at the same point where the game starts trusting them. Two do not, and
// that is recorded here rather than smoothed away, because moving a gate to make a table prettier would change what
// players experience for no reason anybody asked for:
//
//   Cordial      5  = RegardBands "cordial regard"      aligned
//   Warm        20    RegardBands says "genuine regard" from 15   NOT aligned
//   Confiding   30  = RegardBands "deep affection"       aligned
//   Devoted     50  = RegardBands "profound admiration"  aligned
//   DeepBond    60    RegardBands says "most cherished ally" from 70   NOT aligned
//
// Whether the two stragglers should move to the band edges is a tuning question for Gabriel, and a real one: it would
// shift when a lord agrees to leave his house. It is NOT a refactor, so it does not happen inside one.

namespace NpcMemoryService.Core.Standing
{
   /// <summary>
   ///   What one character personally thinks of the player on the MOD's ledger
   ///   (<c>NpcProfile.ReputationWithPlayer</c>, -100 to 100), built by what passes between them in conversation. This
   ///   is the ledger the mod judges a relationship on; <see cref="VanillaRelation" /> is the engine's, and the two
   ///   diverge freely.
   /// </summary>
   public readonly struct Regard
   {
      /// <summary>Past cold politeness into actual goodwill.</summary>
      public const int CordialTier = 5;

      /// <summary>Warm enough to write to the player unbidden, or to speak for them at court.</summary>
      public const int WarmTier = 20;

      /// <summary>Warm enough to be told something held close, to mediate a peace, or to bless a match.</summary>
      public const int ConfidingTier = 30;

      /// <summary>Warm enough for a companion's own heart to be in it.</summary>
      public const int DevotedTier = 50;

      /// <summary>Deep enough to uproot a life: the mark lord, notable and prisoner recruitment all share.</summary>
      public const int DeepBondTier = 60;

      /// <summary>Cold enough to be wary, the first negative word RegardBands gives.</summary>
      public const int WaryTier = -20;

      /// <summary>Cold enough that only the barest dealings remain.</summary>
      public const int HostileTier = -55;

      public Regard(int points) => Points = points;

      /// <summary>The raw stored number. Needed where the profile is read or written, and nowhere else.</summary>
      public int Points { get; }

      /// <summary>At or above <see cref="CordialTier" />.</summary>
      public bool IsCordial => Points >= CordialTier;

      /// <summary>At or above <see cref="WarmTier" />.</summary>
      public bool IsWarm => Points >= WarmTier;

      /// <summary>At or above <see cref="ConfidingTier" />: close enough to be trusted with something.</summary>
      public bool IsConfiding => Points >= ConfidingTier;

      /// <summary>At or above <see cref="DevotedTier" />.</summary>
      public bool IsDevoted => Points >= DevotedTier;

      /// <summary>At or above <see cref="DeepBondTier" />: deep enough to leave a house for.</summary>
      public bool IsDeepBond => Points >= DeepBondTier;

      /// <summary>At or below <see cref="WaryTier" />.</summary>
      public bool IsWary => Points <= WaryTier;

      /// <summary>At or below <see cref="HostileTier" />.</summary>
      public bool IsHostile => Points <= HostileTier;

      /// <summary>The words a character reads for this standing, so the scale has ONE vocabulary (see <see cref="Prompts.RegardBands" />).</summary>
      public string Described => Prompts.RegardBands.Describe(Points);

      public override string ToString() => Points.ToString();
   }
}
