// Code written by Gabriel Mailhot, 01/07/2026.

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

      [Test]
      public void Witness_reaction_missing_text_is_skipped()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[WITNESS_REACTION]\nname: Gunther\n[/WITNESS_REACTION]";

         var result = _parser.Parse(raw);

         result.WitnessReactions.Should().BeEmpty();
      }

      [Test]
      public void No_witness_reaction_blocks_yields_empty_list_not_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");

         result.WitnessReactions.Should().NotBeNull();
         result.WitnessReactions.Should().BeEmpty();
      }
   }
}
