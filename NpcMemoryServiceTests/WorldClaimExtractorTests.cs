// Code written by Gabriel Mailhot, 12/09/2026.
// Invented facts, increment 2. What these pin: that reading a conversation back for claims asks for a narrow,
// safe set of things, and that a line nobody can parse never becomes a fact.
//
// Why it exists. Gabriel's design, 2026-09-12: when an NPC declares a new fact and it is judged credible, it
// may be injected into World Events and travel as a rumour. This is the reading step. It runs at conversation
// close beside ConversationSummarizer - the pass that already exists and already holds the transcript - so
// capturing claims costs no extra LLM call per conversation.
//
// The asymmetry that shapes every choice here: a FALSE POSITIVE is written into the save and read back to
// strangers months later with nobody to explain it. A FALSE NEGATIVE loses a rumour nobody had yet. So the
// prompt is narrow, the parse is strict, and everything doubtful is dropped.
//
// If these fail, either the extractor has started asking for facts the engine owns, or a malformed line has
// started becoming a permanent part of Calradia.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NUnit.Framework;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WorldClaimExtractorTests
   {
      // ── what it asks for ─────────────────────────────────────────────────

      // The safety rule, stated to the model in the words it will act on. If the instruction stops naming
      // these, the gate is still there but it is being fed candidates it will only have to throw away - and
      // one day somebody widens the gate to match the extractor rather than the other way round.
      [Test]
      public void GIVEN_the_instruction_WHEN_read_THEN_it_forbids_everything_the_engine_records()
      {
         string prompt = WorldClaimExtractor.BuildSystemPrompt("Arion");

         foreach (string owned in new[] {
                     "numbers of soldiers", "money", "taxes", "prices", "holds or governs", "wars", "sieges",
                     "battles", "prosperity", "garrisons"
                  })
            prompt.Should().Contain(owned);
      }

      // Gabriel's ruling reaches the model too, not only the gate: the player does not author the world, so
      // what the player said is not even a candidate.
      [Test]
      public void GIVEN_the_instruction_WHEN_read_THEN_it_ignores_whatever_the_player_said()
      {
         WorldClaimExtractor.BuildSystemPrompt("Arion").Should().Contain("ignore anything the PLAYER said");
      }

      // A closed list of five, named to the model. "Any fact you notice" would be an open door into the save.
      [Test]
      public void GIVEN_the_instruction_WHEN_read_THEN_it_asks_only_for_the_five_promotable_kinds()
      {
         string prompt = WorldClaimExtractor.BuildSystemPrompt("Arion");

         foreach (string label in new[] {"BLIGHT", "BANDITRY", "PASSING", "OMEN", "QUARREL"})
            prompt.Should().Contain(label);

         WorldClaimPolicy.PromotableSubjects.Should().HaveCount(5);
      }

      // It must say what to do when there is nothing, or a model with nothing to report will find something.
      [Test]
      public void GIVEN_the_instruction_WHEN_read_THEN_saying_nothing_is_an_offered_answer()
      {
         WorldClaimExtractor.BuildSystemPrompt("Arion").Should().Contain("NONE");
      }

      // ── what it reads back ───────────────────────────────────────────────

      // The mildew, end to end: the line the model would return for the invention that started this.
      [Test]
      public void GIVEN_the_line_for_the_blight_WHEN_parsed_THEN_it_becomes_a_claim_the_gate_accepts()
      {
         WorldClaim claim = WorldClaimExtractor
                            .Parse("CLAIM | BLIGHT | Hargendorf | FACT | A blight has taken the grain in the "
                                   + "fields east of Hargendorf.", "hero_arion", "Arion")
                            .Single();

         claim.Subject.Should().Be(ClaimSubject.Blight);
         claim.PlaceName.Should().Be("Hargendorf");
         claim.StatedAsFact.Should().BeTrue();
         claim.SpeakerId.Should().Be("hero_arion");

         WorldClaimPolicy.Judge(claim, false).Should().Be(ClaimVerdict.Accepted);
      }

      // A hedged claim still travels, just not as far - the prompt teaches characters that a rumour told as a
      // rumour is worth more than an invention told as fact, and the world should keep that distinction.
      [Test]
      public void GIVEN_a_hedged_line_WHEN_parsed_THEN_it_is_marked_as_a_guess_rather_than_dropped()
      {
         WorldClaim claim = WorldClaimExtractor
                            .Parse("CLAIM | OMEN | - | GUESS | They say a comet was seen three nights running.",
                                   "hero_arion", "Arion")
                            .Single();

         claim.StatedAsFact.Should().BeFalse();
         claim.PlaceName.Should().BeNull();
         WorldClaimPolicy.ReachOf(claim).Should().BeLessThan(WorldClaimPolicy.Magnitude);
      }

      // A label the model invented, or one naming a subject the engine owns, is not quietly mapped to
      // something close. It is dropped. The extractor never widens the list.
      [Test]
      public void GIVEN_a_label_that_is_not_one_of_the_five_WHEN_parsed_THEN_nothing_is_recorded()
      {
         WorldClaimExtractor.Parse("CLAIM | GARRISON | Hargendorf | FACT | The garrison numbers four hundred.",
                                   "hero_arion", "Arion").Should().BeEmpty();

         WorldClaimExtractor.Parse("CLAIM | TREASURY | Pravend | FACT | The coffers hold nine thousand denars.",
                                   "hero_arion", "Arion").Should().BeEmpty();
      }

      // A malformed line becomes a permanent fact if it is guessed at, so it is dropped instead. This is the
      // asymmetry in the header made mechanical.
      [Test]
      public void GIVEN_a_line_that_does_not_parse_WHEN_read_THEN_it_is_dropped_rather_than_guessed_at()
      {
         WorldClaimExtractor.Parse("CLAIM | BLIGHT | Hargendorf", "hero_arion", "Arion").Should().BeEmpty();
         WorldClaimExtractor.Parse("CLAIM | BLIGHT | Hargendorf | FACT | ", "hero_arion", "Arion").Should().BeEmpty();
      }

      // "NONE", an empty reply, or the model chatting around the answer must all yield nothing.
      [Test]
      public void GIVEN_nothing_to_report_WHEN_read_THEN_nothing_is_recorded()
      {
         WorldClaimExtractor.Parse("NONE", "hero_arion", "Arion").Should().BeEmpty();
         WorldClaimExtractor.Parse("", "hero_arion", "Arion").Should().BeEmpty();
         WorldClaimExtractor.Parse(null, "hero_arion", "Arion").Should().BeEmpty();
         WorldClaimExtractor.Parse("I did not find any claims of those kinds in the conversation.",
                                   "hero_arion", "Arion").Should().BeEmpty();
      }

      // Several claims in one conversation, and the surrounding prose a model tends to add, must not stop the
      // ones that are well formed from being read.
      [Test]
      public void GIVEN_several_claims_among_chatter_WHEN_read_THEN_the_well_formed_ones_all_survive()
      {
         const string reply = "Here is what I found:\n"
                              + "CLAIM | BLIGHT | Hargendorf | FACT | Blight has taken the eastern fields.\n"
                              + "not a claim, just a note\n"
                              + "CLAIM | BANDITRY | Pravend | GUESS | Travellers have been waylaid on the south road.\n";

         WorldClaimExtractor.Parse(reply, "hero_arion", "Arion").Should().HaveCount(2);
      }

      // A sentence that happens to contain a pipe must not lose its tail: the text is everything after the
      // fourth field, not the fifth field alone.
      [Test]
      public void GIVEN_a_sentence_containing_a_pipe_WHEN_read_THEN_none_of_it_is_lost()
      {
         WorldClaim claim = WorldClaimExtractor
                            .Parse("CLAIM | QUARREL | Pravend | FACT | The steward and the miller fell out | badly.",
                                   "hero_arion", "Arion")
                            .Single();

         claim.Text.Should().EndWith("badly.");
      }
   }
}
