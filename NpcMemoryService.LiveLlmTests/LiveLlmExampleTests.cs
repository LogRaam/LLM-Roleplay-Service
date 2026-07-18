// Code written by Gabriel Mailhot, 18/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>
   ///   <see cref="ExplicitAttribute" /> is the belt the csproj comment always promised: nCrunch, and every
   ///   other runner, will not pick these up in an automatic sweep. It matters now that this project sits in
   ///   the CalradiaRemembers solution, because the harness's <c>Assert.Ignore</c> guard only fires once a
   ///   test has already been built, instrumented and STARTED. That costs no tokens, but it costs nCrunch a
   ///   cycle on every file save. Explicit stops them being selected at all; the runtime guard stays as the
   ///   second belt, for the case where someone does select them without the opt-in set.
   ///   <para>
   ///     To run them on purpose: set CR_RUN_LIVE_LLM=1 and OPENROUTER_API_KEY, then
   ///     <c>dotnet test NpcMemoryService.LiveLlmTests --filter TestCategory=LiveLlm</c>, or select the
   ///     fixture by hand in the runner.
   ///   </para>
   /// </summary>
   [TestFixture]
   [Explicit("Calls a real LLM and spends tokens. Run deliberately: CR_RUN_LIVE_LLM=1 plus --filter TestCategory=LiveLlm.")]
   public sealed class LiveLlmExampleTests : LiveLlmHarness
   {
      // C-residue of `chat_bastard_mother`: the mercenary secret + pay_blackmail teaching are proven to be in the
      // prompt headless; this checks the model actually PLAYS the mercenary mother, alluding to the child and/or
      // pressing for coin, rather than greeting the player as an ordinary acquaintance.
      [Test]
      public async Task GIVEN_a_mercenary_bastard_mother_WHEN_the_player_speaks_THEN_she_leverages_the_secret()
      {
         NpcProfile npc = Npc();
         var context = new EncounterContext {BastardMotherTone = "mercenary", BastardBlackmailDemand = 2000};

         NpcChatResult result = await ChatOnce(npc, "You wanted to see me?", context, AdultContentLevel.Explicit);

         result.IsSuccess.Should().BeTrue(result.ErrorMessage);
         bool leverages = await JudgeYes(
            "The NPC alludes to a child she shares with the player AND/OR presses, however coldly, for coin or payment to keep a secret.",
            result.Response!.Dialogue);
         leverages.Should().BeTrue("a mercenary bastard-mother should use her leverage, not chat idly");
      }

      // C-residue of `chat_memory`: the seeded shared history is proven to be IN the prompt by the headless
      // conversion test; this one checks the model actually WEAVES it into the reply rather than greeting the
      // player as a stranger.
      [Test]
      public async Task GIVEN_a_seeded_shared_history_WHEN_asked_about_it_THEN_the_reply_references_it()
      {
         NpcProfile npc = Npc();
         npc.Events.Add(new NotableEvent(6, NotableEventType.Collaboration,
            "We held the ford together against the raiders at Pendraic."));

         NpcChatResult result = await ChatOnce(npc, "Do you remember that day at the ford?");

         result.IsSuccess.Should().BeTrue(result.ErrorMessage);
         bool referencesIt = await JudgeYes(
            "The NPC's reply acknowledges having fought or held the ford together with the player (it does not treat them as a stranger).",
            result.Response!.Dialogue);
         referencesIt.Should().BeTrue("the NPC should draw on the shared memory that is in its prompt");
      }
   }
}