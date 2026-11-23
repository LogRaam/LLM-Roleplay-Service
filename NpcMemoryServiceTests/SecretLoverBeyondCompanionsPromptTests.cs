// Code written by Gabriel Mailhot, 22/11/2026.
// The secret lover beyond companions. Icezera (Discord 22/11/2026) asked how to keep a bond secret; the secret lover was
// open to the player's companions only. Gabriel (22/11/2026): "il ne devrait pas y avoir de restrictions". The proposal
// section spoke only to a comrade in arms ("You have followed this leader through war"), and the bond line told every
// secret lover they were "married to another", false for anyone unwed. If these are wrong, a lady is addressed as a
// soldier, or an unwed lover is told she betrays a spouse she does not have.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SecretLoverBeyondCompanionsPromptTests
   {
      private const string Comrade = "You have followed this leader through war.";
      private const string Married = "Intimate with the player while married to another.";
      private const string Unwed = "Your lover in secret: intimate with the player, and by your shared choice clandestine";

      private static NpcProfile Npc(RomanticStatus status = RomanticStatus.Curious, string spouse = null) => new() {
         Id = "npc_test", Name = "Lady Test", Faction = "Vlandia", Clan = "dey Meroc", ReputationWithPlayer = 60, SpouseName = spouse,
         Romantic = new RomanticProfile {IsFemale = true, Orientation = SexualOrientation.Heterosexual, Status = status}
      };

      private static string Build(NpcProfile npc, bool notACompanion)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerIsFemale = false}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
                               new EncounterContext {LeanLevel = LeanPromptLevel.Full, SecretLoverEligible = true, SecretLoverNotACompanion = notACompanion});

      // STAKE: a companion keeps the comrade's confession, word for word as before.
      [Test]
      public void GIVEN_a_companion_WHEN_the_secret_lover_is_offered_THEN_the_comrade_text_stays()
      {
         Build(Npc(), notACompanion: false).Should().Contain(Comrade).And.Contain("type: take_as_secret_lover");
      }

      // STAKE: a lady is never addressed as a soldier, and still learns the verb.
      [Test]
      public void GIVEN_a_lady_who_is_no_companion_WHEN_the_secret_lover_is_offered_THEN_she_is_not_addressed_as_a_comrade()
      {
         string prompt = Build(Npc(), notACompanion: true);
         prompt.Should().NotContain(Comrade);
         prompt.Should().Contain("kept SECRET by both of you from the court, your kin and every watching eye.");
         prompt.Should().Contain("type: take_as_secret_lover");
      }

      // STAKE: a married lady keeps it from her spouse, which is what makes it an affair.
      [Test]
      public void GIVEN_a_married_lady_WHEN_the_secret_lover_is_offered_THEN_it_is_kept_from_her_spouse()
      {
         Build(Npc(spouse: "Lord Husband"), notACompanion: true).Should().Contain("from your spouse, your household and every watching eye.");
      }

      // STAKE: an unwed secret lover is not told she is married to another; a married one still is.
      [Test]
      public void GIVEN_a_secret_lover_WHEN_the_bond_line_is_written_THEN_it_says_married_only_when_she_is()
      {
         string unwed = Build(Npc(RomanticStatus.SecretLover), notACompanion: true);
         unwed.Should().Contain(Unwed).And.NotContain(Married);

         Build(Npc(RomanticStatus.SecretLover, "Lord Husband"), notACompanion: true).Should().Contain(Married);
      }
   }
}
