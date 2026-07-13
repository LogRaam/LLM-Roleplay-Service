// Code written by Gabriel Mailhot, 01/07/2026.
// Each parsed WITNESS_REACTION block becomes its own chat message under the witness's own portrait
// (ChatViewModel.CollectWitnessReactions), never words folded into the main speaker's line (the
// scene-discipline rule: "a witness's words appear only in their own reaction"). The parser is
// tolerant by contract (SectionResponseParser never throws): a block missing the name or the text
// cannot be attributed or displayed, so it is dropped rather than surfacing a mystery speaker or an
// empty bubble, and zero blocks must still yield an empty list, not null, since every call site
// foreaches the result unconditionally.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="SectionResponseParser" /> behaviour for the
   ///   <c>[WITNESS_REACTION]</c> section, which was previously untested.
   /// </summary>
   [TestFixture]
   public class WitnessReactionParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // The baseline contract: name and text both present is the whole reason a witness can appear
      // as its own chat bubble with its own portrait.
      [Test]
      public void Single_witness_reaction_with_name_and_text_is_parsed()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[WITNESS_REACTION]\nname: Gunther\ntext: *He frowns.*\n[/WITNESS_REACTION]";

         var result = _parser.Parse(raw);

         result.WitnessReactions.Should().HaveCount(1);
         result.WitnessReactions[0].Name.Should().Be("Gunther");
         result.WitnessReactions[0].Text.Should().Be("*He frowns.*");
      }

      // A multi-NPC scene can have several present witnesses react in the same reply; ChatViewModel
      // displays each in turn, so stopping at the first block would silence every witness after it.
      [Test]
      public void Multiple_witness_reaction_blocks_are_all_parsed()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[WITNESS_REACTION]\nname: Gunther\ntext: *He frowns.*\n[/WITNESS_REACTION]\n" +
            "[WITNESS_REACTION]\nname: Aeva\ntext: *She looks away.*\n[/WITNESS_REACTION]";

         var result = _parser.Parse(raw);

         result.WitnessReactions.Should().HaveCount(2);
         result.WitnessReactions[0].Name.Should().Be("Gunther");
         result.WitnessReactions[1].Name.Should().Be("Aeva");
      }

      // No name means the consumer cannot resolve which hero's portrait to show the reaction under,
      // so it must be dropped rather than displayed as an unattributed message.
      [Test]
      public void Witness_reaction_missing_name_is_skipped()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[WITNESS_REACTION]\ntext: *He frowns.*\n[/WITNESS_REACTION]\n" +
            "[WITNESS_REACTION]\nname: Aeva\ntext: *She nods.*\n[/WITNESS_REACTION]";

         var result = _parser.Parse(raw);

         result.WitnessReactions.Should().HaveCount(1);
         result.WitnessReactions[0].Name.Should().Be("Aeva");
      }

      // No text is nothing to show; keeping the block would render an empty chat bubble under that
      // witness's portrait for no reason.
      [Test]
      public void Witness_reaction_missing_text_is_skipped()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[WITNESS_REACTION]\nname: Gunther\n[/WITNESS_REACTION]";

         var result = _parser.Parse(raw);

         result.WitnessReactions.Should().BeEmpty();
      }

      // Every call site foreaches WitnessReactions unconditionally (ChatViewModel); a null here would
      // NPE the very first time a reply has no witnesses to react, which is the common case.
      [Test]
      public void No_witness_reaction_blocks_yields_empty_list_not_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");

         result.WitnessReactions.Should().NotBeNull();
         result.WitnessReactions.Should().BeEmpty();
      }
   }
}
