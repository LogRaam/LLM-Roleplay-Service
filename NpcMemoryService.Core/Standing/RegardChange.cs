// Code written by Gabriel Mailhot, 29/09/2026.
// One change of a character's personal regard for the player, and why it changed. Raised by the one place regard is
// changed (ProfileMutator.ApplyReputationDelta), so a listener sees every change, from chat, a letter, a quest or a
// grudge alike. tashmetu (Nexus, 29/09/2026) was asked by a player whether regard could drive the game's own relation;
// the answer is that CR keeps the two apart on purpose, and gives other mods this notice instead of a side door.

namespace NpcMemoryService.Core.Standing
{
   /// <summary>Why a character's regard for the player changed. Plain strings, so a listener can log or switch on them.</summary>
   public static class RegardCause
   {
      /// <summary>Something said in conversation (the change_relation action, or the legacy [REPUTATION] block).</summary>
      public const string Conversation = "conversation";

      /// <summary>A letter the character read, or an offer of service the player answered by letter.</summary>
      public const string Letter = "letter";

      /// <summary>A quest the character gave: its reward, or its failure or abandonment.</summary>
      public const string Quest = "quest";

      /// <summary>A promise sworn in conversation, kept or broken.</summary>
      public const string Commitment = "commitment";

      /// <summary>A debt the player could not honour, and the compensation paid for it.</summary>
      public const string Compensation = "compensation";

      /// <summary>A grievance the player answered and put to rest.</summary>
      public const string Grudge = "grudge";

      /// <summary>Jealousy: a romantic rival, or a partner hurt by the player's other bonds.</summary>
      public const string Jealousy = "jealousy";

      /// <summary>A divorce, or the relief of one agreed between both.</summary>
      public const string Divorce = "divorce";

      /// <summary>A rescue from captivity.</summary>
      public const string Rescue = "rescue";

      /// <summary>A change of stance toward the player (affection).</summary>
      public const string Stance = "stance";

      /// <summary>Violence the player did the character.</summary>
      public const string Violence = "violence";

      /// <summary>A mercenary or vassal leaving the character's realm, judged by how it left.</summary>
      public const string Departure = "departure";

      /// <summary>
      ///   The one-time warm start a character's clan standing with the player gives at a first meeting (2.6.8). Its source
      ///   is the game's relation: a mod mirroring regard into that relation should skip this cause, or count it twice.
      /// </summary>
      public const string ClanStanding = "clan_standing";

      /// <summary>No cause was given (an older caller).</summary>
      public const string Unspecified = "unspecified";

      /// <summary>
      ///   A self-test (cr.testall) moved regard on a real character and will put it back without a notice. Every change
      ///   made while a self-test runs is reported with this cause, whatever its own (tashmetu, 30/09/2026): a mod
      ///   mirroring regard should ignore it, or keep what the test left behind.
      /// </summary>
      public const string SelfTest = "self_test";

      /// <summary>The cause a listener is told: <see cref="SelfTest" /> while a self-test runs, the change's own otherwise.</summary>
      public static string AsReported(string cause, bool selfTestRunning) => selfTestRunning ? SelfTest : cause;
   }

   /// <summary>One change of a character's regard for the player.</summary>
   public sealed class RegardChange
   {
      /// <summary>The character's id (a Hero StringId on the Bannerlord side).</summary>
      public string NpcId { get; set; } = string.Empty;

      /// <summary>The character's name, for a log line.</summary>
      public string NpcName { get; set; } = string.Empty;

      /// <summary>Regard before the change (-100 to 100).</summary>
      public int Before { get; set; }

      /// <summary>Regard after the change (-100 to 100). Never equal to <see cref="Before" />: an unchanged value raises nothing.</summary>
      public int After { get; set; }

      /// <summary>Why it changed: one of <see cref="RegardCause" />.</summary>
      public string Cause { get; set; } = RegardCause.Unspecified;
   }
}
