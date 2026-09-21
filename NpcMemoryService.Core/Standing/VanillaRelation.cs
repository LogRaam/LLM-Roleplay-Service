// Code written by Gabriel Mailhot, 21/09/2026.
// The engine's own relation between two people, as a TYPE rather than a bare int.
//
// Gabriel, 21/09/2026, naming the cause after a week of hunting duplicated thresholds: "il faut que chaque valeur soit
// un concept du systeme. Age n'est pas un INT, mais un type Age qui centralise ses propres regles."
//
// Two things make a bare int dangerous here, and both have already cost this project real bugs.
//
//   1. THERE ARE TWO SCALES. This one is vanilla's `Hero.GetRelation`, from -100 to 100. The mod keeps its own,
//      `NpcProfile.ReputationWithPlayer`, over the SAME range, built in conversation. A method taking an int cannot
//      tell them apart, and the code does not either: facts named "...Regard" are fed vanilla relation while facts
//      named "RelationToPlayer" are fed the mod's. Lord recruitment gated on the wrong one for months (fixed
//      21/09/2026) and nothing could have caught it, because both are int.
//   2. THE DECISION GETS TAKEN, NOT ASKED. "Is this person a friend" was `>= 10` in three types and `>= 20` in four
//      more, each with its own constant. The rule was copied because copying it was the shortest path for anyone,
//      human or model, who did not know it already existed. A value that answers questions has one place to change.
//
// So the tiers live HERE, and nowhere else. They are the values the code already used on 21/09/2026: nothing was
// re-tuned in the move, and `docs/maps/decisions-regard.md` is the witness that no decision shifted.

namespace NpcMemoryService.Core.Standing
{
   /// <summary>
   ///   How two people stand with each other on the ENGINE's ledger (<c>Hero.GetRelation</c>), -100 to 100. For the
   ///   mod's own conversation-built regard, see <see cref="Regard" />: they are different ledgers and diverge freely.
   /// </summary>
   public readonly struct VanillaRelation
   {
      /// <summary>Warm enough to be called a friend, the tier the prompt uses when it names a third party (`ThirdPartyDescriptor`).</summary>
      public const int FriendTier = 10;

      /// <summary>
      ///   Warm enough to be TOLD something, or to owe something: the tier four separate policies had each minted for
      ///   themselves (rescue gratitude, secret propagation, a spouse's three best friends, a rival suitor's
      ///   associates). Above a friend, below a confidant of the heart.
      /// </summary>
      public const int TrustedTier = 20;

      /// <summary>Deep trust, the top tier the prompt names ("a very close friend").</summary>
      public const int CloseTier = 30;

      /// <summary>Cold enough to be called an enemy.</summary>
      public const int EnemyTier = -10;

      /// <summary>Cold enough to be called a bitter enemy.</summary>
      public const int BitterTier = -30;

      public VanillaRelation(int points) => Points = points;

      /// <summary>The raw engine number. Needed at the boundary (the engine speaks ints) and nowhere else.</summary>
      public int Points { get; }

      /// <summary>At or above <see cref="FriendTier" />.</summary>
      public bool IsFriend => Points >= FriendTier;

      /// <summary>At or above <see cref="TrustedTier" />: close enough to be told a secret, or to feel a debt owed.</summary>
      public bool IsTrusted => Points >= TrustedTier;

      /// <summary>At or above <see cref="CloseTier" />.</summary>
      public bool IsClose => Points >= CloseTier;

      /// <summary>At or below <see cref="EnemyTier" />.</summary>
      public bool IsEnemy => Points <= EnemyTier;

      /// <summary>At or below <see cref="BitterTier" />.</summary>
      public bool IsBitterEnemy => Points <= BitterTier;

      /// <summary>Neither friend nor enemy: the wide middle where most of Calradia sits.</summary>
      public bool IsIndifferent => !IsFriend && !IsEnemy;

      /// <summary>Reads as the number, so a log line or a prompt string needs no ceremony.</summary>
      public override string ToString() => Points.ToString();
   }
}
