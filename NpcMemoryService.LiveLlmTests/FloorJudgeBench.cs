// Code written by Gabriel Mailhot, 26/11/2026.
// The group-conversation floor judge bench. Gabriel (26/11/2026): a cheap, fast model judges, after every line, how much
// each listener wants to speak and how they react; it never writes dialogue; "il ne faut pas sacrifier la qualite". Before
// any of it reaches the chat, this measures whether a judge model judges WELL, on scenes written by hand from the fourteen
// scenarios Gabriel validated (26/11/2026), with what each listener should feel. A listener the game already obliges to
// answer (addressed, asked by name) is not scored on intent: the referee (TurnArbiter, Calradia Remembers) settles that.
// Scored: intent within the accepted set, a reaction where the line plainly calls for one, the reply readable; plus latency,
// cost and, on a scene that grows line by line, how much of each call the provider's cache served.

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>One listener to judge in a scene, with the verdicts a good judge may give.</summary>
   public sealed class FloorJudgeCase
   {
      /// <summary>Which validated scenario this comes from, for the printout.</summary>
      public string Id { get; init; } = "";

      /// <summary>The scene so far; the last line is the one judged.</summary>
      public IReadOnlyList<FloorJudgeLine> Scene { get; init; } = new List<FloorJudgeLine>();

      /// <summary>The listener judged.</summary>
      public FloorJudgeListener Listener { get; init; } = new();

      /// <summary>Intents a good judge may give for this listener.</summary>
      public IReadOnlyList<JudgedIntent> Accepted { get; init; } = new List<JudgedIntent>();

      /// <summary>The line plainly calls for a visible reaction from this listener (a tell, a strong feeling).</summary>
      public bool ReactionExpected { get; init; }

      /// <summary>
      ///   The line gives this listener nothing worth showing: a reaction here is noise (Gabriel, 26/11/2026: "une reaction
      ///   possible devrait etre de ne rien faire").
      /// </summary>
      public bool NoReactionExpected { get; init; }
   }

   /// <summary>One judged case.</summary>
   public sealed class FloorJudgeResult
   {
      public FloorJudgeCase Case { get; init; } = new();
      public FloorJudgeVerdict Verdict { get; init; } = new();
      public string Raw { get; init; } = "";
      public long Millis { get; init; }
      public LlmUsage? Usage { get; init; }
      public string? Error { get; init; }

      public bool IntentRight => Error == null && Case.Accepted.Contains(Verdict.Intent);
      public bool ReactionRight => (!Case.ReactionExpected || Verdict.Reaction != null) && (!Case.NoReactionExpected || Verdict.Reaction == null);
   }

   /// <summary>The scenes and the runner.</summary>
   public static class FloorJudgeBench
   {
      private const string Player = "Arwa";

      private static readonly FloorJudgeListener Garios = new() {
         Name = "Garios",
         Persona = "A proud Vlandian lord of an old house, quick to take offence, jealous of his name and of those who serve him.",
         Ties = new List<string> {
            "Arwa: a commander you ride with, useful but beneath your blood", "Ira: a mercenary captain you despise as a sellsword",
            "Bryn: your own young squire, whose conduct reflects on you", "Lucon: a merchant of Ortysia you tolerate for his coin"
         }
      };

      private static readonly FloorJudgeListener Ira = new() {
         Name = "Ira",
         Persona = "A blunt mercenary captain who fights for pay, scorns noble airs and never lets a slight pass.",
         Ties = new List<string> {
            "Arwa: your employer, who pays well", "Garios: a lord who looks down on you; you return the favour",
            "Bryn: a lord's boy squire, beneath notice", "Lucon: a merchant who once cheated your company"
         }
      };

      private static readonly FloorJudgeListener Bryn = new() {
         Name = "Bryn",
         Persona = "A timid squire of sixteen in Garios's service, who keeps quiet before his betters and fears his lord's temper.",
         Ties = new List<string> {
            "Arwa: a commander you admire from afar", "Garios: your lord, whom you must never embarrass",
            "Ira: a frightening mercenary", "Lucon: a merchant you barely know"
         }
      };

      private static readonly FloorJudgeListener Lucon = new() {
         Name = "Lucon",
         Persona = "A cautious old merchant of Ortysia, whose fortune is his warehouses there; he speaks little among warriors.",
         Ties = new List<string> {
            "Arwa: a commander who buys from you", "Garios: a lord you must flatter", "Ira: a mercenary you once short-changed",
            "Bryn: a squire of no account"
         }
      };

      // Gabriel's example (26/11/2026): the one who stole the food grows nervous when the theft is told; the innocent does not.
      private static readonly FloorJudgeListener BrynThief = new() {
         Name = "Bryn", Persona = Bryn.Persona + " Last night, hungry, you stole two loaves and a ham from the camp stores; nobody knows.",
         Ties = Bryn.Ties
      };

      private static readonly FloorJudgePresence ArwaHere = new() {Name = Player, Station = "a commander of the realm", Power = "a rising house, 120 men", IsFemale = true};
      private static readonly FloorJudgePresence GariosHere = new() {Name = "Garios", Station = "a lord of an old Vlandian house", Power = "a great house, 300 men under arms"};
      private static readonly FloorJudgePresence IraHere = new() {Name = "Ira", Station = "a mercenary captain", Power = "sixty hired blades", IsFemale = true};
      private static readonly FloorJudgePresence KingHere = new() {Name = "Derthert", Station = "King of Vlandia", Power = "rules the realm"};
      private static readonly FloorJudgePresence PeasantHere = new() {Name = "Odo", Station = "a village peasant", Power = "none"};
      private static readonly FloorJudgePresence CaptiveHere = new() {Name = "Caladog", Station = "a Battanian lord, the player's captive in chains", Power = "none while captive"};

      // Gabriel (26/11/2026): "on ne reagit pas pareil devant [...] un Roi ou un prisonnier, un paysan". The same listener,
      // the same kind of slight, in different rooms.
      private static FloorJudgeListener In(FloorJudgeListener who, params FloorJudgePresence[] room)
         => new() {Name = who.Name, Persona = who.Persona, Ties = who.Ties, Room = room};

      private static FloorJudgeLine Line(string speaker, string to, string text) => new() {Speaker = speaker, To = to, Text = text};

      private static IReadOnlyList<JudgedIntent> Any(params JudgedIntent[] intents) => intents;

      /// <summary>The scenes, by validated scenario.</summary>
      public static List<FloorJudgeCase> Cases() => new() {
         // (1) a plain question to one; the other has nothing to say
         new() {Id = "1_direct_question", Listener = Ira, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line(Player, "Garios", "Which road is shortest to Pravend from here?")}},
         // (2) a startling question to one; the other reacts, need not speak
         new() {Id = "2_startling", Listener = Bryn, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), ReactionExpected = true,
                Scene = new[] {Line(Player, "Garios", "Tell me plainly, Garios: did you have your own brother poisoned?")}},
         // (3) Gabriel's case: mocked while the player speaks to another
         new() {Id = "3_mocked", Listener = Ira, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent), ReactionExpected = true,
                Scene = new[] {Line(Player, "Garios", "Your hired captain there fights like a farmhand with a pitchfork, doesn't she, Garios?")}},
         // (3b) the timid squire mocked before his lord: a timid boy keeps quiet, his lord bristles
         // The judge reads what he FEELS; "timide reste silencieux" (Gabriel, 26/11/2026) is the referee's Timid rule, not the judge's.
         new() {Id = "3b_squire_mocked_squire", Listener = Bryn, Accepted = Any(JudgedIntent.None, JudgedIntent.Low, JudgedIntent.High, JudgedIntent.Urgent), ReactionExpected = true,
                Scene = new[] {Line(Player, "Ira", "Have you seen Garios's squire hold a sword? Like a girl holding a spoon.")}},
         new() {Id = "3b_squire_mocked_lord", Listener = Garios, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "Ira", "Have you seen Garios's squire hold a sword? Like a girl holding a spoon.")}},
         // (4) a line to everyone: the one with stakes wants in, the one without does not
         new() {Id = "4_everyone_mercenary", Listener = Ira, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "everyone", "I mean to hire more blades and march on Sargot within the week. What do you all say?")}},
         new() {Id = "4_everyone_squire", Listener = Bryn, Accepted = Any(JudgedIntent.None, JudgedIntent.Low),
                Scene = new[] {Line(Player, "everyone", "I mean to hire more blades and march on Sargot within the week. What do you all say?")}},
         // (5) re-judged after another's line: the treasury is empty, the sellsword's pay is at stake
         new() {Id = "5_rejudged", Listener = Ira, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {
                   Line(Player, "everyone", "We should declare war on Battania before the snows."),
                   Line("Garios", Player, "Impossible. The treasury is empty; we could not pay a single hired sword past the month.")
                }},
         // (8) an idle remark to nobody
         new() {Id = "8_idle", Listener = Garios, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line(Player, "", "Looks like rain again.")}},
         new() {Id = "8_idle_squire", Listener = Bryn, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line(Player, "", "Looks like rain again.")}},
         // (9) the silent merchant: the talk touches his town
         new() {Id = "9_silent_touched", Listener = Lucon, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent), ReactionExpected = true,
                Scene = new[] {
                   Line(Player, "Garios", "How many lances can your house raise?"),
                   Line("Garios", Player, "Forty, if the harvest allows."),
                   Line(Player, "Ira", "And your company?"),
                   Line("Ira", Player, "Sixty, paid in advance."),
                   Line(Player, "Garios", "They say the warehouses of Ortysia burned to the ground last night.")
                }},
         // (13) two listeners eager to contradict an offer made to a third
         new() {Id = "13_contradict", Listener = Ira, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "Bryn", "Bryn, I'll give you command of the vanguard tomorrow, over every veteran here.")}},
         new() {Id = "13_contradict_lord", Listener = Garios, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "Bryn", "Bryn, I'll give you command of the vanguard tomorrow, over every veteran here.")}},
         // (14) a loud reaction without speech: bad news of a rival
         new() {Id = "14_reaction", Listener = Ira, Accepted = Any(JudgedIntent.None, JudgedIntent.Low, JudgedIntent.High), ReactionExpected = true,
                Scene = new[] {Line(Player, "Garios", "Garios, your father's keep fell to the Battanians at dawn.")}},
         // a room of five: who will lead the vanguard
         new() {Id = "room5_lord", Listener = Garios, Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "everyone", "Who among you will lead the vanguard at dawn?")}},
         new() {Id = "room5_mercenary", Listener = Ira, Accepted = Any(JudgedIntent.Low, JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line(Player, "everyone", "Who among you will lead the vanguard at dawn?")}},
         new() {Id = "room5_squire", Listener = Bryn, Accepted = Any(JudgedIntent.None, JudgedIntent.Low),
                Scene = new[] {Line(Player, "everyone", "Who among you will lead the vanguard at dawn?")}},
         new() {Id = "room5_merchant", Listener = Lucon, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line(Player, "everyone", "Who among you will lead the vanguard at dawn?")}},
         // Gabriel's tell: the theft is told; the thief's nerves show, the innocent shows nothing
         new() {Id = "tell_thief", Listener = BrynThief, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), ReactionExpected = true,
                Scene = new[] {Line("Garios", Player, "Someone raided the camp stores last night. Two loaves and a whole ham, gone.")}},
         new() {Id = "tell_innocent", Listener = Lucon, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line("Garios", Player, "Someone raided the camp stores last night. Two loaves and a whole ham, gone.")}},
         // Gabriel's stance (26/11/2026): "B sourit a ce que A dit.. donc B approuve": war means pay for the sellsword
         new() {Id = "stance_approves", Listener = Ira, Accepted = Any(JudgedIntent.None, JudgedIntent.Low, JudgedIntent.High), ReactionExpected = true,
                Scene = new[] {Line("Garios", Player, "Strike Battania now, while their host is scattered. Hire every blade you can find and pay them well.")}},
         // rank in the room: the same slight, borne from a king, answered from a peasant or a captive
         // Deference before the king is the referee's rule (TurnArbiter.EffectiveIntent): the judge measured 6/6 Urgent here, so it is
         // scored on the feeling (strong, and shown), not on restraint.
         new() {Id = "room_ira_slighted_by_king", Listener = In(Ira, ArwaHere, KingHere, IraHere), Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent), ReactionExpected = true,
                Scene = new[] {Line("Derthert", Player, "Your hired captain smells of the stable, commander.")}},
         new() {Id = "room_ira_slighted_by_peasant", Listener = In(Ira, ArwaHere, PeasantHere, IraHere), Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line("Odo", Player, "Your hired captain smells of the stable, commander.")}},
         new() {Id = "room_garios_rebuked_by_king", Listener = In(Garios, ArwaHere, KingHere, GariosHere), Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                ReactionExpected = true,
                Scene = new[] {Line("Derthert", Player, "Garios's house has grown soft, commander. I would not trust it with a wall.")}},
         new() {Id = "room_garios_insulted_by_captive", Listener = In(Garios, ArwaHere, CaptiveHere, GariosHere), Accepted = Any(JudgedIntent.High, JudgedIntent.Urgent),
                Scene = new[] {Line("Caladog", Player, "Garios's house has grown soft, commander. I would not trust it with a wall.")}},
         // an ordinary report to the player: the others just listen
         new() {Id = "plain_report", Listener = Ira, Accepted = Any(JudgedIntent.None, JudgedIntent.Low), NoReactionExpected = true,
                Scene = new[] {Line("Garios", Player, "The scouts are back. The road east is clear as far as the ford.")}}
      };

      /// <summary>A scene of six lines judged after each one for the same listener: the calls the cache should serve.</summary>
      public static List<FloorJudgeLine> GrowingScene() => new() {
         Line(Player, "Garios", "How many lances can your house raise?"),
         Line("Garios", Player, "Forty, if the harvest allows."),
         Line(Player, "Ira", "And your company?"),
         Line("Ira", Player, "Sixty, paid in advance, as always."),
         Line("Garios", "Ira", "Coin buys a sword, never a spine."),
         Line(Player, "Lucon", "And you, Lucon, can you feed a hundred men through the winter?")
      };

      /// <summary>One judge call for one listener on one scene.</summary>
      public static async Task<FloorJudgeResult> JudgeAsync(ILlmClient client, FloorJudgeCase c)
      {
         var clock = Stopwatch.StartNew();
         LlmResponse response = await client.CompleteAsync(new LlmRequest {
            SystemPrompt = FloorJudge.StablePrompt(c.Listener),
            Messages = new[] {new LlmMessage(MessageRole.User, FloorJudge.TurnPrompt(c.Listener.Name, c.Scene))},
            Parameters = new LlmParameters {MaxTokens = 80, Creativity = 0.3f, AllowTruncationRetry = false},
            Subject = "floorjudge:" + c.Listener.Name
         });
         clock.Stop();

         return new FloorJudgeResult {
            Case = c, Raw = response.Content ?? "", Millis = clock.ElapsedMilliseconds, Usage = response.Usage,
            Verdict = FloorJudge.Parse(response.Content), Error = response.IsSuccess ? null : response.ErrorMessage ?? "failed"
         };
      }

      /// <summary>The scoreboard, printable.</summary>
      public static string Scoreboard(string model, IReadOnlyList<FloorJudgeResult> results)
      {
         int n = results.Count;
         long[] ms = results.Select(r => r.Millis).OrderBy(x => x).ToArray();
         double cost = results.Sum(r => r.Usage?.Cost ?? 0);
         var lines = new List<string> {
            $"FLOOR JUDGE BENCH: {model}",
            $"intent right {results.Count(r => r.IntentRight)}/{n}, reaction where expected {results.Count(r => r.Case.ReactionExpected && r.Verdict.Reaction != null)}/{results.Count(r => r.Case.ReactionExpected)}, "
            + $"none where none is due {results.Count(r => r.Case.NoReactionExpected && r.Verdict.Reaction == null)}/{results.Count(r => r.Case.NoReactionExpected)}, "
            + $"reactions given {results.Count(r => r.Verdict.Reaction != null)}/{n}, "
            + $"readable {results.Count(r => r.Verdict.Parsed)}/{n}, failures {results.Count(r => r.Error != null)}",
            $"latency median {(n == 0 ? 0 : ms[n / 2])} ms, p95 {(n == 0 ? 0 : ms[Math.Min(n - 1, (int) Math.Ceiling(n * 0.95) - 1)])} ms, cost ${cost:0.00000} ({(n == 0 ? 0 : cost / n):0.000000} per call)"
         };
         foreach (FloorJudgeResult r in results)
            lines.Add($"{(r.IntentRight && r.ReactionRight ? "  ok " : "  MISS")} {r.Case.Id,-26} {r.Case.Listener.Name,-7} intent={r.Verdict.Intent,-6} accepted=[{string.Join("|", r.Case.Accepted)}] reaction={r.Verdict.Reaction ?? "-"}{(r.Error != null ? " ERROR " + r.Error : "")}");

         return string.Join(Environment.NewLine, lines);
      }
   }
}
