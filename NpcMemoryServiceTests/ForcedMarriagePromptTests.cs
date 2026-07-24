// Code written by Gabriel Mailhot, 23/07/2026.
// Ratified 2026-07-23, born of a player report (a player earned the family's blessing through a quest and
// then hit the bride's-own-regard gate no one had ever named). Gabriel's call: history sides with the family,
// so a blessed match now MARRIES even below the regard bar, and the unwon heart becomes a resolvable
// ForcedMarriage grudge instead of an invisible refusal. That decision has a trap this fixture exists to pin:
// the consent rules waive every trust threshold between spouses ("none of the stranger-resistance trust
// thresholds apply here: your vows already bind you"). Written for a love match at 50+ regard, that exemption
// is harmless; applied to a bride wed by her family's will at low regard, it would turn the political-marriage
// mechanic into a CONSENT BYPASS, the exact opposite of what was ratified. So the marital exemption must skip
// an unwilling spouse (she keeps her own nature's thresholds, as if unwed), the proposal must be spoken as
// duty rather than love, and the whole footing must return to the ordinary marital one by ITSELF once the
// grievance fades: the host reads the flag live from the grudge ledger, which is the entire redemption arc.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ForcedMarriagePromptTests
   {
      private const string MaritalExemption = "none of the stranger-resistance trust thresholds apply";
      private const string UnwillingHeading = "NOT by your own choosing";
      private const string VerdictHeading = "WHERE THIS PLAYER ACTUALLY STANDS WITH YOU";
      private const string DutyHeading = "BUT UNDERSTAND WHAT THIS MATCH IS";

      // ── The proposal (before the wedding) ─────────────────────────────────

      // The political union must be spoken AS one. Without this the model plays a bride in love that the
      // ledger says is nearly a stranger, and the resentment the wedding plants arrives out of nowhere.
      [Test]
      public void GIVEN_marriage_is_on_offer_only_through_the_family_WHEN_built_THEN_the_proposal_is_spoken_as_duty()
      {
         string prompt = BuildProposal(reluctant: true);

         prompt.Should().Contain(DutyHeading);
         prompt.Should().Contain("marrying your name, not your heart");
      }

      // The love match must stay exactly as it was: the duty framing on a willing bride would poison every
      // ordinary wedding the mod has shipped for months.
      [Test]
      public void GIVEN_an_ordinary_love_match_WHEN_built_THEN_no_duty_framing_appears()
      {
         string prompt = BuildProposal(reluctant: false);

         prompt.Should().NotContain(DutyHeading);
         prompt.Should().Contain("What exists between you has grown");
      }

      // ── The marriage (after the wedding) ──────────────────────────────────

      // The trap itself. The exemption text exists for a love match; on an unwilling spouse it would read
      // "your vows already bind you" to a woman whose consent was never asked, and every threshold would
      // vanish at the stroke of a political wedding.
      [Test]
      public void GIVEN_an_unwilling_spouse_WHEN_built_THEN_the_marital_consent_exemption_does_not_apply()
      {
         string prompt = BuildMarried(unwilling: true, regard: 10);

         prompt.Should().NotContain(MaritalExemption);
         prompt.Should().Contain(UnwillingHeading);
         prompt.Should().Contain("a wedding is not consent");
      }

      // The teeth behind the words: her own nature's bar must actually PRINT, with the verdict, exactly as
      // for an unmarried person. Words alone would leave the model free to fall back on "but they are wed".
      [Test]
      public void GIVEN_an_unwilling_spouse_WHEN_built_THEN_her_own_thresholds_still_print_with_a_verdict()
      {
         string prompt = BuildMarried(unwilling: true, regard: 10);

         prompt.Should().Contain(VerdictHeading);
         prompt.Should().Contain("That is BELOW it");
      }

      // She is married to the PLAYER: the married-to-another branch frames intimacy as clandestine infidelity
      // against a third party, which would be a lie in her mouth and would teach secrecy where the whole
      // grievance is public.
      [Test]
      public void GIVEN_an_unwilling_spouse_WHEN_built_THEN_the_infidelity_framing_is_absent()
      {
         BuildMarried(unwilling: true, regard: 10).Should().NotContain("an act of infidelity against");
      }

      // The redemption arc's other end, and the regression guard for every existing marriage: a WILLING
      // spouse keeps the full exemption and is never handed a bar. The host reads the unwilling flag live
      // from the grudge ledger, so the day the grievance dies this exact footing takes over by itself.
      [Test]
      public void GIVEN_a_willing_spouse_WHEN_built_THEN_the_marital_exemption_stands_and_no_bar_is_printed()
      {
         string prompt = BuildMarried(unwilling: false, regard: 60);

         prompt.Should().Contain(MaritalExemption);
         prompt.Should().NotContain(UnwillingHeading);
         prompt.Should().NotContain(VerdictHeading);
      }

      #region private

      private static string BuildProposal(bool reluctant)
      {
         NpcProfile npc = Npc(spouseName: null, regard: reluctant ? 10 : 60);

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
               new EncounterContext {
                  LoveMatchEligible = true,
                  LoveMatchBlessed = true,
                  LoveMatchReluctant = reluctant
               });
      }

      private static string BuildMarried(bool unwilling, int regard)
      {
         NpcProfile npc = Npc(spouseName: "The Player", regard: regard);

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
               new EncounterContext {
                  NpcSpouseIsPlayer = true,
                  SpouseMarriedUnwillingly = unwilling
               });
      }

      private static NpcProfile Npc(string spouseName, int regard) => new() {
         Id = "npc_test",
         Name = "Test Lady",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         SpouseName = spouseName,
         ReputationWithPlayer = regard,
         Romantic = new RomanticProfile {
            Status = RomanticStatus.Curious,
            Orientation = SexualOrientation.BiCurious,
            Preferences = new List<RomanticPreference>()
         }
      };

      #endregion
   }
}
