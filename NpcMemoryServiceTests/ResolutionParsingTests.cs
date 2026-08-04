// Code written by Gabriel Mailhot, 21/07/2026.
//
// [RESOLUTION] is the council's mechanical afterlife: the positive channel that replaces the old pure
// "nothing may be sealed at this table" prohibition. Nothing executes from parsing alone (the consumer records
// each as provisional and only re-validates/executes it when the council is lifted), but a resolution the
// parser drops here is a pledge the game can never honour, exactly the empty-promise class this whole feature
// exists to close. Mirrors ActionParsingTests' conventions closely: same truncation tolerance, same
// in-dialogue guard, same "missing type = skipped" contract as [ACTION].

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="SectionResponseParser" /> behaviour for the <c>[RESOLUTION]</c> section.
   /// </summary>
   [TestFixture]
   public class ResolutionParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // Baseline: the worked example from the ratified design (type/actor/detail) must parse cleanly, or the
      // whole feature has no floor to build on.
      [Test]
      public void A_well_formed_quest_resolution_is_parsed()
      {
         var raw =
            "[DIALOGUE]Very well.[/DIALOGUE]\n" +
            "[RESOLUTION]\n" +
            "type: quest\n" +
            "actor: Sley\n" +
            "detail: ride to Varcheg and clear the bandits\n" +
            "[/RESOLUTION]";

         var result = _parser.Parse(raw);

         result.Resolutions.Should().HaveCount(1);
         result.Resolutions[0].Type.Should().Be("quest");
         result.Resolutions[0].Actor.Should().Be("Sley");
         result.Resolutions[0].Detail.Should().Be("ride to Varcheg and clear the bandits");
      }

      // A council turn can seat several members each committing to something in the same reply: every block
      // must survive, in order, or a second member's pledge silently vanishes behind the first.
      [Test]
      public void Several_resolutions_in_one_response_are_all_parsed_in_order()
      {
         var raw =
            "[DIALOGUE]Agreed, on both counts.[/DIALOGUE]\n" +
            "[RESOLUTION]\ntype: quest\nactor: Sley\ndetail: ride to Varcheg\n[/RESOLUTION]\n" +
            "[RESOLUTION]\ntype: quest\nactor: Aldric\ndetail: garrison the north gate\n[/RESOLUTION]";

         var result = _parser.Parse(raw);

         result.Resolutions.Should().HaveCount(2);
         result.Resolutions[0].Actor.Should().Be("Sley");
         result.Resolutions[1].Actor.Should().Be("Aldric");
      }

      // Mirrors ActionParsingTests' "Action_missing_type_is_silently_skipped": a block with no "type:" line is
      // not a decision at all, and recording one anyway would hand the mod a resolution its own catalogue
      // cannot ever settle at the lift.
      [Test]
      public void A_resolution_missing_type_is_silently_skipped()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[RESOLUTION]\nactor: Sley\ndetail: no type here\n[/RESOLUTION]\n" +
            "[RESOLUTION]\ntype: quest\nactor: Aldric\ndetail: garrison the gate\n[/RESOLUTION]";

         var result = _parser.Parse(raw);

         result.Resolutions.Should().HaveCount(1);
         result.Resolutions[0].Actor.Should().Be("Aldric");
      }

      // The withdrawal path the owner asked for INSTEAD of a manual delete button: a change of mind belongs in
      // the conversation, so "type: withdraw" must parse exactly like any other resolution: the mod decides
      // what it targets, but the parser must not treat this kind specially or drop it.
      [Test]
      public void A_withdraw_resolution_is_parsed()
      {
         var raw =
            "[DIALOGUE]On second thought, never mind.[/DIALOGUE]\n" +
            "[RESOLUTION]\ntype: withdraw\nactor: Sley\ndetail: the ride to Varcheg\n[/RESOLUTION]";

         var result = _parser.Parse(raw);

         result.Resolutions.Should().HaveCount(1);
         result.Resolutions[0].Type.Should().Be("withdraw");
         result.Resolutions[0].Actor.Should().Be("Sley");
      }

      // Truncation net, same discipline as [ACTION] and the QUEST family: a reply cut off by the token limit
      // opens [RESOLUTION] and never reaches [/RESOLUTION]. Losing it silently would mean a pledge the player
      // watched the NPC speak never got recorded, and the council window's own report at the lift would then
      // have nothing to say about it at all.
      [Test]
      public void A_trailing_unclosed_resolution_block_is_still_parsed()
      {
         var raw = "[DIALOGUE]Consider it done.[/DIALOGUE]\n[RESOLUTION]\ntype: quest\nactor: Sley\ndetail: ride to Varcheg";

         var result = _parser.Parse(raw);

         result.Resolutions.Should().HaveCount(1);
         result.Resolutions[0].Actor.Should().Be("Sley");
         result.Resolutions[0].Detail.Should().Be("ride to Varcheg");
      }

      // Mirrors ActionParsingTests' A7 (quest audit): a model that EXPLAINS the [RESOLUTION] format inside its
      // own spoken [DIALOGUE] must not have the example recorded as a real decision: an NPC describing "I
      // would write [RESOLUTION] type: quest..." is not actually pledging anything.
      [Test]
      public void A_resolution_written_inside_the_dialogue_body_is_not_parsed()
      {
         var raw = "[DIALOGUE]To pledge it I would write [RESOLUTION]\ntype: quest\nactor: Sley\ndetail: ride out\n[/RESOLUTION] like so.[/DIALOGUE]";

         _parser.Parse(raw).Resolutions.Should().BeEmpty();
      }

      // No block at all (an ordinary conversation, or a council turn with nothing pledged) must default to an
      // empty, non-null list: the consumer's council-only processing loop iterates it unconditionally.
      [Test]
      public void No_resolution_block_yields_an_empty_list()
      {
         var raw = "[DIALOGUE]Just a normal reply.[/DIALOGUE]";

         _parser.Parse(raw).Resolutions.Should().BeEmpty();
      }

      // The optional catalogue-specific field this first slice needs: grounding a "quest" kind the same way an
      // ordinary [QUEST] block would (a named settlement). Must survive parsing even though the worked example
      // in the ratified design does not show it, or a bandit-clear pledge can never be grounded at the lift.
      [Test]
      public void A_target_settlement_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: quest\nactor: Sley\ndetail: ride to Varcheg\ntarget_settlement: Varcheg\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetSettlement.Should().Be("Varcheg");
      }

      // assign_party_role's own grounding field, mirroring target_settlement: must survive parsing or the mod
      // has no role name to map into TargetHint and the lift can never assign anything.
      [Test]
      public void A_target_role_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: assign_party_role\nactor: Sley\ndetail: Sley will scout ahead\ntarget_role: Scout\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetRole.Should().Be("Scout");
      }

      // Neither grounding field is mandatory (a quest may name only target_settlement, a role pledge only
      // target_role): a resolution missing one must simply leave it null, never throw or default the other.
      [Test]
      public void A_resolution_missing_both_grounding_fields_leaves_them_null()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: quest\nactor: Sley\ndetail: something vague\n[/RESOLUTION]";

         var resolution = _parser.Parse(raw).Resolutions[0];

         resolution.TargetSettlement.Should().BeNull();
         resolution.TargetRole.Should().BeNull();
         resolution.TargetMission.Should().BeNull();
         resolution.TargetAmount.Should().BeNull();
         resolution.TargetFaction.Should().BeNull();
         resolution.TargetRival.Should().BeNull();
      }

      // dispatch_mission's own grounding field, mirroring target_role: must survive parsing or the mod has no
      // errand type to map into TargetHint and the lift can never dispatch anything.
      [Test]
      public void A_target_mission_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: dispatch_mission\nactor: Sley\ndetail: Sley will ride out for news\ntarget_mission: GatherNews\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetMission.Should().Be("GatherNews");
      }

      // grant_stipend's own grounding field: must survive parsing or the mod has no proposed amount to map into
      // TargetHint, and the lift can never fund the escrow.
      [Test]
      public void A_target_amount_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: grant_stipend\nactor: Sley\ndetail: Sley will draw 100 denars a day\ntarget_amount: 100\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetAmount.Should().Be(100);
      }

      // Tolerant like every other numeric field this parser reads (TryParseSignedInt, e.g. deadline_days): a
      // model that dresses the figure up ("100 denars/day") must not lose the amount just for wrapping it.
      [Test]
      public void A_target_amount_field_wrapped_in_extra_words_is_still_parsed()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: grant_stipend\nactor: Sley\ndetail: a stipend\ntarget_amount: 100 denars a day\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetAmount.Should().Be(100);
      }

      // A target_amount the model left unparseable (garbage, no digits) must leave the field null rather than
      // throw: the mod's own lift guard (CouncilLift.SettleGrantStipend) is what turns a missing amount into a
      // clean MarkFailed, so the parser itself must never crash the whole response over one bad field.
      [Test]
      public void An_unparseable_target_amount_field_is_left_null()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: grant_stipend\nactor: Sley\ndetail: a stipend\ntarget_amount: plenty\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetAmount.Should().BeNull();
      }

      // declare_war's own grounding field (the first FACTION-CENTRIC kind, WarCouncil only): must survive
      // parsing or the mod has no faction name to map into TargetHint and CouncilLift.SettleDeclareWar can
      // never resolve a target to declare war on.
      [Test]
      public void A_target_faction_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: declare_war\ndetail: the council resolves for war\ntarget_faction: Vlandia\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetFaction.Should().Be("Vlandia");
      }

      // declare_war's actor is OPTIONAL (the mod's ResolveDeclareWarActor carve-out defaults an omitted one to
      // the presiding member): the parser itself must not require it, mirroring how every field here stays
      // simply absent rather than enforced.
      [Test]
      public void A_declare_war_resolution_with_no_actor_still_parses()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: declare_war\ndetail: the council resolves for war\ntarget_faction: Vlandia\n[/RESOLUTION]";

         var resolution = _parser.Parse(raw).Resolutions[0];

         resolution.Actor.Should().BeNull();
         resolution.Type.Should().Be("declare_war");
      }

      // pledge_against's own grounding field (Partie 1, COUNCIL_ACTIONS.md's schemes block, reusing the existing
      // 1:1 pledge_against system): must survive parsing or the mod has no rival name to map into TargetHint,
      // and CouncilLift.SettlePledgeAgainst can never resolve who the pledge concerns.
      [Test]
      public void A_target_rival_field_is_parsed_when_present()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[RESOLUTION]\ntype: pledge_against\nactor: Ira\ndetail: Ira will move against Boyar Sevin\ntarget_rival: Boyar Sevin\n[/RESOLUTION]";

         _parser.Parse(raw).Resolutions[0].TargetRival.Should().Be("Boyar Sevin");
      }
   }
}
