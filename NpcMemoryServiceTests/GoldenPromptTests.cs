// Code written by Gabriel Mailhot, 11/10/2026.
// What this pins: the WHOLE system prompt a character is sent, character for character, in a set of configurations
// chosen to light up as many of its sections as possible. Gabriel (11/10/2026) agreed the Prompt Composer
// (docs/design/prompt-composer/DESIGN.md in the mod repository): CR's prompt becomes a registry of keyed sections in a
// layout, its texts move out to files, and power users and modders can recompose it. Its first principle: "stock is the
// reference", with nothing customised the prompt sent must be identical to the one sent before. Before this file, no
// test held a whole prompt (the budget tests measure its LENGTH, the cache test one boundary), so a restructuring that
// moved a sentence, dropped a blank line or swapped two sections would have passed every test and reached players.
//
// If a test here fails, the prompt a player's characters receive has changed. That may be intended (a section was
// reworded on purpose): then regenerate with CR_UPDATE_GOLDEN_PROMPTS=1, READ the diff of the golden files, and commit
// them with the change, so the diff is the record of what the model now reads. During the Prompt Composer refactor it
// is never intended: the goldens must not move.

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GoldenPromptTests
   {
      private const string UpdateVariable = "CR_UPDATE_GOLDEN_PROMPTS";

      private static string GoldenFolder([CallerFilePath] string here = "")
         => Path.Combine(Path.GetDirectoryName(here)!, "Golden", "Prompts");

      // ── The configurations ───────────────────────────────────────────────────────────────────────────────────

      /// <summary>Every configuration, by golden file name. Built fresh each call: nothing shared between tests.</summary>
      public static IEnumerable<string> GoldenNames() => Configurations().Keys.OrderBy(k => k, StringComparer.Ordinal);

      private static Dictionary<string, Func<string>> Configurations()
      {
         var c = new Dictionary<string, Func<string>>(StringComparer.Ordinal)
         {
            ["lord_full"] = () => Builder(AdultContentLevel.Mature).BuildSystemPrompt(RichNpc(), World(), RichEncounter(LeanPromptLevel.Full)),
            ["lord_lean"] = () => Builder(AdultContentLevel.Mature).BuildSystemPrompt(RichNpc(), World(), RichEncounter(LeanPromptLevel.Lean)),
            ["bare_off"] = () => Builder(AdultContentLevel.Off).BuildSystemPrompt(new NpcProfile {Id = "npc_bare", Name = "Bare Lord", Faction = "", Clan = ""}, new WorldState {CurrentDay = 1}, null),
            ["builder_flags_inverted"] = () => Builder(AdultContentLevel.Mature, invertFlags: true).BuildSystemPrompt(RichNpc(), World(), RichEncounter(LeanPromptLevel.Full)),
            ["all_flags_full"] = () => Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(AllFlags(RichNpc()), World(), AllFlags(RichEncounter(LeanPromptLevel.Full))),
            ["all_flags_lean"] = () => Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(AllFlags(RichNpc()), World(), AllFlags(RichEncounter(LeanPromptLevel.Lean))),
            ["witnesses"] = () => Builder(AdultContentLevel.Explicit).BuildSystemPrompt(RichNpc(), World(), WithWitnesses(RichEncounter(LeanPromptLevel.Full))),
            ["adult_levels"] = () => Variants(Enum.GetValues(typeof(AdultContentLevel)).Cast<AdultContentLevel>(),
               level => level.ToString(),
               level => Builder(level).BuildSystemPrompt(RichNpc(), World(), RichEncounter(LeanPromptLevel.Full))),
            ["commoner"] = () => Builder(AdultContentLevel.Mature).BuildCommonerSystemPrompt(RichNpc(), Commons(), "[narrative style for {{char}}]"),
            ["lord_lean_no_station"] = () => Builder(AdultContentLevel.Mature).BuildSystemPrompt(RichNpc(), World(), NoStation(RichEncounter(LeanPromptLevel.Lean))),
            ["all_offers_full"] = () => Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(DeepNpc(), World(), Offers(RichEncounter(LeanPromptLevel.Full), captor: false)),
            ["all_offers_lean"] = () => Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(DeepNpc(), World(), Offers(RichEncounter(LeanPromptLevel.Lean), captor: false)),
            ["captor_scene"] = () => Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(DeepNpc(), World(), WithWitnesses(Offers(RichEncounter(LeanPromptLevel.Full), captor: true)))
         };

         // Each value of each choice the encounter carries (scene type, the player's status, a captive scene's stage,
         // intent and aggressor, war status...), one at a time on the rich lord: the branches a single context misses.
         foreach (PropertyInfo p in Settable(typeof(EncounterContext)).Where(p => EnumType(p.PropertyType) != null && p.Name != nameof(EncounterContext.LeanLevel)))
         {
            Type enumType = EnumType(p.PropertyType)!;
            c["enum_" + p.Name] = () => Variants(Enum.GetValues(enumType).Cast<object>(),
               v => v.ToString()!,
               v =>
               {
                  EncounterContext ctx = RichEncounter(LeanPromptLevel.Full);
                  p.SetValue(ctx, v);
                  return Builder(AdultContentLevel.Hardcore).BuildSystemPrompt(RichNpc(), World(), ctx);
               });
         }

         return c;
      }

      // ── The test ─────────────────────────────────────────────────────────────────────────────────────────────

      // STAKE: the prompt is what every character says. A change nobody meant (a lost line, two sections swapped, a
      // variable no longer expanded) changes how every character in every campaign speaks, and nothing else would see it.
      [TestCaseSource(nameof(GoldenNames))]
      public void GIVEN_a_configuration_WHEN_its_prompt_is_built_THEN_it_is_exactly_the_golden_one(string name)
      {
         string actual = Normalize(Configurations()[name]());
         string path = Path.Combine(GoldenFolder(), name + ".txt");

         if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
         {
            Directory.CreateDirectory(GoldenFolder());
            File.WriteAllText(path, actual, new UTF8Encoding(false));
            return;
         }

         Assert.That(File.Exists(path), $"{name}.txt is missing; write it with {UpdateVariable}=1 and read it.");
         string expected = Normalize(File.ReadAllText(path));
         if (expected == actual) return;

         string[] e = expected.Split('\n'), a = actual.Split('\n');
         int line = 0;
         while (line < e.Length && line < a.Length && e[line] == a[line]) line++;
         Assert.Fail($"The {name} prompt changed at line {line + 1}.\n  golden: {(line < e.Length ? e[line] : "(end)")}\n  now:    {(line < a.Length ? a[line] : "(end)")}\n"
                   + $"If the change is meant, regenerate with {UpdateVariable}=1 and read the diff of Golden/Prompts.");
      }

      // STAKE: a golden set that renders the same text for every configuration would hold almost nothing; each family
      // must actually differ from the plain lord, or it is not covering the branches it is named after.
      [Test]
      public void GIVEN_the_configurations_WHEN_built_THEN_each_differs_from_the_plain_lord()
      {
         Dictionary<string, Func<string>> c = Configurations();
         string plain = c["lord_full"]();
         List<string> same = c.Where(kv => kv.Key != "lord_full" && kv.Value() == plain).Select(kv => kv.Key).ToList();

         Assert.That(same, Is.Empty, "configurations that render exactly the plain lord's prompt: " + string.Join(", ", same));
      }

      // ── Builders ─────────────────────────────────────────────────────────────────────────────────────────────

      // As LlmServices.BuildPromptBuilder sets it in the game, with the narrative files as recognisable stand-ins that
      // carry variables, so their expansion is pinned too.
      private static PromptBuilder Builder(AdultContentLevel level, bool invertFlags = false)
         => new()
         {
            AdultLevel = level,
            PlayerIsFemale = true,
            PlayerName = "Arwa",
            PlayerClanName = "Arwa's Company",
            PlayerFactionName = "Vlandia",
            WorldDescription = "[world.txt for {{char}}, speaking to {{user}} in {{location}}]",
            PlayerDescription = "[player_description.txt: {{user}} of {{faction}}]",
            BehaviorGuidelinesOverride = "[behavior_guidelines.txt for {{char}}]",
            PostHistoryInstructions = "[post_history_instructions.txt for {{char}}]",
            HeardNowInstructions = "[heard_now.txt: {{vividworld_news}}]",
            ReplyLanguage = "",
            ActionVocabulary = GameActionCatalog.All.Select(s => new GameActionDefinition {Type = s.Type, Description = s.Description}).ToList(),
            IntimacyThresholdRelief = 0,
            EnableReputationBlock = invertFlags,
            EnableMemoryBlock = invertFlags,
            EnableQuests = !invertFlags
         };

      private static WorldState World()
         => new() {CurrentDay = 120, Season = "Winter", ActiveConflicts = "Vlandia against Sturgia", Rumors = "Grain fails around Charas.", TimeOfDay = "night"};

      private static NpcProfile RichNpc()
      {
         var npc = new NpcProfile
         {
            Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc",
            Personality = "A composed, calculating lord of the western marches, courteous to a fault and slow to forgive.",
            Trait = "Calculating",
            Relationships = "Married to Lady Ira. Sworn to King Derthert. Bitter enemy of Lord Temion.",
            BackgroundContext = "He and the player have crossed paths at tournaments and across a negotiating table."
         };
         for (var i = 0; i < 4; i++)
            npc.Events.Add(new NotableEvent(i * 7, NotableEventType.Other, $"Memory {i}: the player kept their word at the ford of Rovalt."));
         npc.DiscoveredTraits.Add(new DiscoveredTrait {Key = "praise", Description = "Stiffens when praise is withheld."});

         return npc;
      }

      private static EncounterContext RichEncounter(LeanPromptLevel lean)
         => new()
         {
            LeanLevel = lean,
            // No third-party variable may leak in from the process-wide registry: the golden is CR's own prompt.
            ExternalPromptVariables = new Dictionary<string, string> {["vividworld_news"] = "[news a mod handed in]"},
            StationGuidelinesOverride = "[station guidelines for {{char}}]",
            Bearer = new KnowledgeBearer {IsLord = true, HasHouse = true, IsPlayerCompanion = true, HoldsASeat = true},
            HouseStanding = new HouseStandingFacts
            {
               HouseName = "dey Meroc", IsPlayerHouse = true, Fiefs = new List<string> {"Ocs Hall", "Sargot"},
               AtWarWith = new List<string> {"Sturgia"}
            },
            HereAndNow = new HereAndNowFacts
            {
               Season = Season.Winter, HourOfDay = 2, IsNight = true, Weather = Weather.Blizzard,
               NearestPlace = "Ocs Hall", BearingToPlace = "a short ride to the north-east", RealmName = "Vlandia"
            },
            Audience = new ListeningAudience {Regard = 70, InPrivate = true},
            RealmNews = new RealmNewsFacts
            {
               Headline = "Vlandia has declared war on Sturgia.",
               Tidings = new List<Tiding> {new() {Text = "A siege was broken at Ocs Hall."}, new() {Text = "A lord was executed in the east.", Confidence = TidingConfidence.Distant}}
            }
         };

      private static EncounterContext WithWitnesses(EncounterContext ctx)
      {
         var witnesses = new List<WitnessEntry>
         {
            new() {Name = "Ira", RelationToNpc = "his wife", IsFemale = true, Persona = "proud and watchful", PresenceStatus = "standing at the door"},
            new() {Name = "Ajin", RelationToNpc = "a companion of the player", IsPlayerCompanion = true, IsFemale = false, Gear = "a scarred round shield"},
            new() {Name = "Liena", RelationToNpc = "a captive", IsCaptiveVictim = true, IsBroughtCaptive = true, IsFemale = true}
         };
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.Witnesses))!.SetValue(ctx, witnesses);

         return ctx;
      }

      private static CommonsKnowledge Commons()
      {
         var k = new CommonsKnowledge();
         foreach (PropertyInfo p in Settable(typeof(CommonsKnowledge)).Where(p => p.PropertyType == typeof(string)))
            p.SetValue(k, $"[{p.Name}]");

         return k;
      }

      /// <summary>
      ///   Every flag of the object raised, every empty text filled with its own name, every number set to 3: the
      ///   configuration that opens the most gates at once (offers, proposals, notes). Contradictory on purpose; the
      ///   golden only needs it to be deterministic.
      /// </summary>
      private static T AllFlags<T>(T target)
      {
         foreach (PropertyInfo p in Settable(typeof(T)))
         {
            Type t = p.PropertyType;
            if (t == typeof(bool) || t == typeof(bool?)) p.SetValue(target, true);
            else if (t == typeof(string) && string.IsNullOrEmpty(p.GetValue(target) as string)) p.SetValue(target, $"[{p.Name}]");
            else if (t == typeof(int) || t == typeof(int?)) p.SetValue(target, 3);
         }

         return target;
      }

      private static EncounterContext NoStation(EncounterContext ctx)
      {
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.StationGuidelinesOverride))!.SetValue(ctx, null);

         return ctx;
      }

      /// <summary>
      ///   Every offer, proposal and note open at once, as a lord free to make them: the encounter filled in depth, then
      ///   the flags that SHUT offers turned back off (a round-table or council turn, quests suppressed), and the scene a
      ///   captor scene only when asked. The configuration the plain "all flags" one cannot reach.
      /// </summary>
      private static EncounterContext Offers(EncounterContext ctx, bool captor)
      {
         DeepFill(ctx, 0);
         foreach (PropertyInfo p in Settable(typeof(EncounterContext)).Where(p => p.PropertyType == typeof(bool)))
            if (p.Name.StartsWith("Suppress", StringComparison.Ordinal) || p.Name.EndsWith("Turn", StringComparison.Ordinal))
               p.SetValue(ctx, false);
         typeof(EncounterContext).GetProperty(nameof(EncounterContext.IsCaptorScene))!.SetValue(ctx, captor);

         return ctx;
      }

      /// <summary>The rich lord filled in depth (romance, quests, letters...), attracted to a woman like the player.</summary>
      private static NpcProfile DeepNpc()
      {
         NpcProfile npc = RichNpc();
         DeepFill(npc, 0);
         if (npc.Romantic != null) npc.Romantic.Orientation = SexualOrientation.Bisexual;
         // A letter delivered and NOT yet read is the one the character is told about.
         foreach (PlayerLetter letter in npc.ReceivedPlayerLetters)
            typeof(PlayerLetter).GetProperty(nameof(PlayerLetter.HasBeenRead))!.SetValue(letter, false);

         return npc;
      }

      /// <summary>
      ///   Fills an object in depth: flags raised, empty texts named, numbers 3, missing objects created and filled, empty
      ///   lists given an element. Enums are left as they are (each has its own family). Two levels deep.
      /// </summary>
      private static void DeepFill(object target, int depth)
      {
         foreach (PropertyInfo p in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0).OrderBy(p => p.Name, StringComparer.Ordinal))
         {
            Type t = p.PropertyType;
            bool canSet = p.CanWrite && p.GetSetMethod() != null;
            object? value = p.GetValue(target);
            if (canSet && (t == typeof(bool) || t == typeof(bool?))) p.SetValue(target, true);
            else if (canSet && t == typeof(string) && string.IsNullOrEmpty(value as string)) p.SetValue(target, $"[{p.Name}]");
            else if (canSet && (t == typeof(int) || t == typeof(int?))) p.SetValue(target, 3);
            else if (ListElement(t) is { } element)
            {
               object? item = element == typeof(string) ? $"[{p.Name} item]" : depth < 2 ? Make(element, depth + 1) : null;
               if (item == null) continue;
               if (value is System.Collections.IList list && !list.IsReadOnly && list.Count == 0) list.Add(item);
               else if (value == null && canSet)
               {
                  var made = (System.Collections.IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
                  made.Add(item);
                  if (t.IsAssignableFrom(made.GetType())) p.SetValue(target, made);
               }
            }
            else if (canSet && value == null && depth < 2 && t.IsClass && t != typeof(string) && Make(t, depth + 1) is { } child) p.SetValue(target, child);
         }
      }

      private static object? Make(Type t, int depth)
      {
         if (!t.IsClass || t.IsAbstract || t.GetConstructor(Type.EmptyTypes) == null || typeof(Delegate).IsAssignableFrom(t)) return null;
         object o = Activator.CreateInstance(t)!;
         DeepFill(o, depth);

         return o;
      }

      private static Type? ListElement(Type t)
      {
         if (!t.IsGenericType) return null;
         Type def = t.GetGenericTypeDefinition();
         return def == typeof(List<>) || def == typeof(IReadOnlyList<>) || def == typeof(IList<>) || def == typeof(IEnumerable<>) || def == typeof(ICollection<>) || def == typeof(IReadOnlyCollection<>)
            ? t.GetGenericArguments()[0]
            : null;
      }

      private static IEnumerable<PropertyInfo> Settable(Type type)
         => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetSetMethod() != null && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

      private static Type? EnumType(Type t)
         => t.IsEnum ? t : Nullable.GetUnderlyingType(t) is {IsEnum: true} u ? u : null;

      private static string Variants<T>(IEnumerable<T> values, Func<T, string> label, Func<T, string> build)
      {
         // A variant identical to an earlier one is named, not repeated: the file stays readable, and a change that makes
         // two variants differ (or alike) still shows.
         var sb = new StringBuilder();
         var seen = new Dictionary<string, string>(StringComparer.Ordinal);
         foreach (T v in values)
         {
            string name = label(v), text = build(v);
            if (seen.TryGetValue(text, out string? first))
            {
               sb.Append("===== ").Append(name).Append(" ===== (same as ").Append(first).Append(")\n");
               continue;
            }

            seen[text] = name;
            sb.Append("===== ").Append(name).Append(" =====\n");
            sb.Append(text).Append('\n');
         }

         return sb.ToString();
      }

      private static string Normalize(string s) => s.Replace("\r\n", "\n");
   }
}
