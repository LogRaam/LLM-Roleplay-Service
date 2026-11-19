// Code written by Gabriel Mailhot, 14/11/2026.
// PromptBuilder.RenderStockSections: what each section of the character prompt writes in the STOCK prompt, for one
// character in one encounter. Bhelogan (Muh Prompts, a profile editor for CR's Custom files), 14/11/2026: "read-only stock
// text of a prompt section (for the profile editor)". If this is wrong: the editor shows a text the model never reads (a
// second renderer drifting from the real one), shows the player's own rewording as if it were CR's, or shows the
// unfilled verb catalogue placeholder instead of the verbs.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class StockSectionsTests
   {
      // STAKE: the editor's "stock text" is what the model really reads; joined, the sections are the stock prompt.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_character_in_an_encounter_WHEN_the_stock_sections_are_read_THEN_joined_they_are_the_stock_prompt(LeanPromptLevel lean)
      {
         PromptBuilder builder = GoldenPromptTests.Builder(AdultContentLevel.Mature);
         EncounterContext context = GoldenPromptTests.RichEncounter(lean);

         string prompt = builder.BuildSystemPrompt(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), context);
         IReadOnlyList<PromptSectionRendering> sections = builder.RenderStockSections(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), context);

         string.Concat(sections.Select(s => s.Text)).Should().Be(prompt);
      }

      // STAKE: every section is listed, in the stock order, with its own key, zone and lock.
      [Test]
      public void GIVEN_the_stock_sections_WHEN_read_THEN_every_keyed_section_is_listed_in_the_stock_order()
      {
         IReadOnlyList<PromptSectionRendering> sections = GoldenPromptTests.Builder(AdultContentLevel.Mature)
                                                                           .RenderStockSections(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), GoldenPromptTests.RichEncounter(LeanPromptLevel.Full));

         sections.Select(s => s.Key).Should().Equal(PromptBuilder.CharacterSections.Select(s => s.Key));
         sections.Select(s => s.Lock).Should().Equal(PromptBuilder.CharacterSections.Select(s => s.Lock));
      }

      // STAKE: the editor shows CR's words, not the profile the player is editing: its rewording and its order are left out.
      [Test]
      public void GIVEN_an_encounter_carrying_a_profile_WHEN_the_stock_sections_are_read_THEN_its_rewording_and_order_are_not_in_them()
      {
         PromptBuilder builder = GoldenPromptTests.Builder(AdultContentLevel.Mature);
         EncounterContext plain = GoldenPromptTests.RichEncounter(LeanPromptLevel.Full);
         EncounterContext profiled = GoldenPromptTests.RichEncounter(LeanPromptLevel.Full);
         // init-only on the context (set once by the host); the rich fixture is built elsewhere, so set them as the host would at creation.
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.PromptTexts))!
                                 .SetValue(profiled, new Dictionary<string, PromptSectionText> {["identity"] = new() {Text = "PROFILE IDENTITY TEXT"}});
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.PromptLayout))!
                                 .SetValue(profiled, new PromptLayout {Off = new[] {"dialogue_style"}});

         string stock = string.Concat(builder.RenderStockSections(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), profiled).Select(s => s.Text));

         stock.Should().NotContain("PROFILE IDENTITY TEXT");
         stock.Should().Be(builder.BuildSystemPrompt(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), plain));
      }

      // STAKE: the verb catalogue is filled in where it sits, by what the whole prompt taught, never left as a placeholder.
      [Test]
      public void GIVEN_the_stock_sections_WHEN_read_THEN_no_section_holds_the_verb_catalogue_placeholder()
      {
         IReadOnlyList<PromptSectionRendering> sections = GoldenPromptTests.Builder(AdultContentLevel.Mature)
                                                                           .RenderStockSections(GoldenPromptTests.RichNpc(), GoldenPromptTests.World(), GoldenPromptTests.RichEncounter(LeanPromptLevel.Full));

         sections.Should().NotContain(s => s.Text.Contains(VerbCatalogPolicy.Placeholder));
      }

      // STAKE (cr.test persona_surface in Gabriel's game, 15/11/2026): the persona CR hands another mod (CrPrompt.Persona)
      // carried "The first time your own words in [DIALOGUE] plainly tell the player this, add this block" inside
      // matters_most. The trait stays in its section; how the player learns it, CR's reply format, is a section of its own
      // that a persona leaves out. The prompt the model reads is unchanged.
      [Test]
      public void GIVEN_traits_the_player_has_yet_to_learn_WHEN_the_stock_sections_are_read_THEN_the_discovery_teaching_is_in_its_own_sections()
      {
         NpcProfile npc = GoldenPromptTests.RichNpc();
         npc.MattersMost = MattersMost.Children;
         npc.Fear = Fear.TheSea;
         npc.Superstition = Superstition.Omens;

         IReadOnlyList<PromptSectionRendering> sections = GoldenPromptTests.Builder(AdultContentLevel.Off)
                                                                           .RenderStockSections(npc, GoldenPromptTests.World(), GoldenPromptTests.RichEncounter(LeanPromptLevel.Full));
         string Of(string key) => sections.Single(s => s.Key == key).Text;

         Of("matters_most").Should().Contain("WHAT MATTERS MOST TO YOU").And.NotContain("[DISCOVERY]").And.NotContain("[DIALOGUE]");
         Of("fear_and_superstition").Should().Contain("A FEAR OF YOURS").And.NotContain("[DISCOVERY]").And.NotContain("[DIALOGUE]");
         Of("matters_most_discovery").Should().Contain("[DISCOVERY]").And.Contain("key: " + MattersMostDiscovery.Key);
         Of("fear_discovery").Should().Contain("[DISCOVERY]").And.Contain(NatureDiscovery.FearKey);
      }

      // STAKE (tashmetu, Nexus 19/11/2026): with Traits of Calradia a lord its traits make Steadfast read as "The Hollow Lord"
      // in the same prompt, and the archetype "rides inside another one, so turning it off loses more than it should". The
      // archetype and the personality line are sections of their own: a profile turns them off and the identity stays.
      [Test]
      public void GIVEN_a_profile_turning_off_archetype_and_personality_WHEN_the_prompt_is_built_THEN_they_are_gone_and_the_identity_stays()
      {
         PromptBuilder builder = GoldenPromptTests.Builder(AdultContentLevel.Off);
         NpcProfile npc = GoldenPromptTests.RichNpc();
         npc.Trait = "The Hollow Lord. A vacancy wearing armor.";
         npc.Personality = "Cold and watchful.";
         EncounterContext profiled = GoldenPromptTests.RichEncounter(LeanPromptLevel.Full);
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.PromptLayout))!
                                 .SetValue(profiled, new PromptLayout {Off = new[] {"archetype", "personality"}});

         string stock = builder.BuildSystemPrompt(npc, GoldenPromptTests.World(), GoldenPromptTests.RichEncounter(LeanPromptLevel.Full));
         string prompt = builder.BuildSystemPrompt(npc, GoldenPromptTests.World(), profiled);

         stock.Should().Contain("The Hollow Lord").And.Contain("Cold and watchful.");
         prompt.Should().NotContain("The Hollow Lord").And.NotContain("CHARACTER:").And.NotContain("Cold and watchful.").And.NotContain("STAY TRUE TO THIS NATURE");
         prompt.Should().Contain("YOU ARE " + npc.Name.ToUpperInvariant());
      }
   }
}
