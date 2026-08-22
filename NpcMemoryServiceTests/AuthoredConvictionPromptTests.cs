// Code written by Gabriel Mailhot, 13/07/2026.
// A player asked to author what a character BELIEVES, not merely how they sound: "if I wanted a certain character
// to believe they were pulling the strings of my clan behind the scenes, or to believe they were secretly a
// necromancer" (Thoragoros1, 2026-07-12). And he named the reason it is worth building: "that sort of just
// happened with one of my companions, completely by accident". The mod already produces this by emergence. He
// wants to PROVOKE what he tasted by luck.
//
// WHY A SECOND FIELD, AND NOT JUST A LONGER BACKSTORY: the backstory block tells the model, in as many words,
// that it shapes "HOW you speak and WHO you are, NOT what you decide", and that conduct still follows the traits.
// So a belief written into a backstory becomes a COSTUME: the NPC says it with great colour and then behaves
// exactly as before. A conviction is a MOTIVE. It is the one authored field that crosses that line, and these
// tests pin both the crossing and the three guardrails that make it safe to cross.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AuthoredConvictionPromptTests
   {
      private const string Conviction = "He is convinced he is secretly a necromancer, and hides it from every living soul.";

      private static NpcProfile Npc(string conviction = null!, string backstory = null!) => new() {
         Id = "test_hero",
         Name = "Derthert",
         Clan = "dey Meroc",
         Faction = "Vlandia",
         AuthoredBackstory = backstory,
         AuthoredConviction = conviction
      };

      private static string Build(NpcProfile npc)
         => new PromptBuilder().BuildSystemPrompt(npc, new WorldState(), new EncounterContext());

      // The whole point of the field, and the line the backstory refuses to cross. If the prompt does not tell the
      // model that a conviction moves what it DECIDES, the belief is a costume again and the field is pointless.
      [Test]
      public void GIVEN_an_authored_conviction_WHEN_building_the_prompt_THEN_it_drives_what_the_npc_DECIDES_not_merely_how_they_speak()
      {
         string prompt = Build(Npc(Conviction));

         prompt.Should().Contain("WHAT YOU HOLD TRUE");
         prompt.Should().Contain(Conviction);
         prompt.Should().Contain("you ACT on it");
         prompt.Should().Contain("what you WANT");
         prompt.Should().Contain("moves what");     // "...this one moves what you decide"
      }

      // The belief may be FALSE, and that is the drama. An NPC acting on a conviction the world does not share is
      // exactly what the player asked for; a prompt that quietly treated it as fact would have missed the request.
      [Test]
      public void GIVEN_a_conviction_WHEN_building_the_prompt_THEN_it_is_framed_as_a_BELIEF_never_as_the_truth_of_the_world()
      {
         string prompt = Build(Npc(Conviction));

         prompt.Should().Contain("not necessarily the truth of");
         prompt.Should().Contain("Others need not share it");
      }

      // A ruinous belief must be HIDDEN, not confessed on the first turn. This is the difference between a scene
      // that unfolds over a campaign and a character who blurts their secret to a stranger on the road.
      [Test]
      public void GIVEN_a_conviction_that_would_ruin_them_WHEN_building_the_prompt_THEN_hiding_it_is_taught_as_a_motive_of_its_own()
      {
         string prompt = Build(Npc(Conviction));

         prompt.Should().Contain("HIDE it");
         prompt.Should().Contain("Concealing it is itself a motive");
      }

      // GUARDRAIL 1: belief, not capability. A man may believe he is a necromancer; he may not raise the dead.
      // Without this, an authored conviction becomes a licence to rewrite the world's rules from a text box.
      [Test]
      public void GIVEN_a_conviction_WHEN_building_the_prompt_THEN_it_grants_NO_powers()
      {
         string prompt = Build(Npc(Conviction));

         prompt.Should().Contain("NO powers");
         prompt.Should().Contain("only do what");
      }

      // GUARDRAIL 2: it cannot reach past the bridge. A conviction may make an NPC WANT to plot; only a deed can
      // make a plot exist, and every deed still goes through the action gates. The prompt proposes, the bridge rules.
      [Test]
      public void GIVEN_a_conviction_WHEN_building_the_prompt_THEN_it_can_make_them_WANT_a_thing_never_bend_the_world_to_give_it()
      {
         string prompt = Build(Npc(Conviction));

         prompt.Should().Contain("does not bend the world");
         prompt.Should().Contain("Whether you get it is for deeds to decide");
      }

      // GUARDRAIL 3: no lore from outside the setting, and it must name the setting the player is ACTUALLY in, so
      // a total conversion is not told to respect a world its mod deleted.
      [Test]
      public void GIVEN_a_conversion_that_replaced_the_world_WHEN_a_conviction_is_written_THEN_the_limits_name_THEIR_world()
      {
         PromptLore.WorldName = "Middle-earth";
         PromptLore.WorldAdjective = "Middle-earth";   // both, or "Calradian" survives elsewhere in the prompt

         try
         {
            string prompt = Build(Npc(Conviction));

            prompt.Should().Contain("the world of Middle-earth remains exactly as it is");
            prompt.Should().Contain("It imports no lore from outside that world");
            prompt.Should().NotContain("Calradia");
         }
         finally
         {
            PromptLore.WorldName = "Calradia";
            PromptLore.WorldAdjective = "Calradian";
         }
      }

      // The two fields are independent: a player may author a voice with no belief, a belief with no voice, or
      // both. Neither may drag the other into the prompt.
      [Test]
      public void GIVEN_no_conviction_WHEN_building_the_prompt_THEN_the_section_is_absent_entirely()
      {
         Build(Npc(backstory: "A king who trusts ledgers more than men.")).Should().NotContain("WHAT YOU HOLD TRUE");
         Build(Npc()).Should().NotContain("WHAT YOU HOLD TRUE");
         Build(Npc("   ")).Should().NotContain("WHAT YOU HOLD TRUE");
      }

      // Voice AND motive together, which is the case the player actually described: a character with a manner of
      // speaking and a secret they are acting on. Both blocks must survive each other.
      [Test]
      public void GIVEN_both_a_backstory_and_a_conviction_WHEN_building_the_prompt_THEN_both_reach_the_model()
      {
         string prompt = Build(Npc(Conviction, "A Vlandian king who trusts ledgers more than men."));

         prompt.Should().Contain("BACKSTORY AND VOICE");
         prompt.Should().Contain("WHAT YOU HOLD TRUE");
         prompt.Should().Contain("trusts ledgers more than men");
         prompt.Should().Contain(Conviction);
      }
   }
}
