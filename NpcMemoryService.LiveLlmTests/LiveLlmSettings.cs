// Code written by Gabriel Mailhot, 18/07/2026.
// The live-LLM lane's local control panel. It replaces an environment-variable-only opt-in that was correct in
// principle (a firewall no runner setting can open) and miserable in practice: environment variables are
// captured when a process STARTS, so a value set while Visual Studio or nCrunch was already running is simply
// not seen, and the symptom (a breakpoint in the test body never hit, because [SetUp] ignored first) looks
// nothing like the cause. A file next to the tests takes effect immediately and can be read at a glance.
//
// DELIBERATELY ABSENT: the API key. This project's own history is the reason (a key was once leaked through a
// committed launchSettings.json and auto-disabled by OpenRouter). A settings file is exactly the shape of
// thing that gets committed by accident, so the key stays in OPENROUTER_API_KEY, where it cannot be.

#region

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>
   ///   Local, git-ignored settings for the live-LLM lane. Absent file means the firewall is CLOSED, which is
   ///   what keeps a fresh clone and any CI safe by default: there is nothing to forget to turn off.
   /// </summary>
   public sealed class LiveLlmSettings
   {
      /// <summary>The file the tests look for, beside the project (and copied next to the test assembly).</summary>
      public const string FileName = "livellm.settings.json";

      /// <summary>The committed template that documents every option. Copy it to <see cref="FileName" /> to begin.</summary>
      public const string ExampleFileName = "livellm.settings.example.json";

      /// <summary>
      ///   THE FIREWALL. False (or a missing file) means every live test self-skips before a token is spent.
      ///   Setting it true takes effect on the next test run, with no restart of the IDE or the test runner.
      /// </summary>
      [JsonPropertyName("enabled")]
      public bool Enabled { get; set; }

      /// <summary>
      ///   OpenRouter API key. Optional: left empty, the harness falls back to the OPENROUTER_API_KEY
      ///   environment variable.
      ///   <para>
      ///     It lives here only because <see cref="FileName" /> is git-ignored (verified) and because the
      ///     environment variable has a trap that cost real time: a process captures its environment at
      ///     start-up, so a key set while Visual Studio or nCrunch is already running is not seen, and the
      ///     in-game key (which the mod reads from MCM, taking precedence over the variable) can be valid
      ///     while the variable still holds a deleted one. That is exactly what happened here: the variable
      ///     carried a key that no longer existed on the account, and OpenRouter answered "User not found".
      ///   </para>
      ///   NEVER put a real key in <see cref="ExampleFileName" />: that one IS committed.
      /// </summary>
      [JsonPropertyName("apiKey")]
      public string? ApiKey { get; set; }

      /// <summary>The model under test. This is the point of the lane: judge the model you actually ship with.</summary>
      [JsonPropertyName("model")]
      public string Model { get; set; } = "x-ai/grok-4.20";

      /// <summary>
      ///   The model that scores the YES/NO behaviour verdicts. Left empty it reuses <see cref="Model" />, but a
      ///   small, cheap, literal model usually judges better than a creative one, and costs less.
      /// </summary>
      [JsonPropertyName("judgeModel")]
      public string? JudgeModel { get; set; }

      /// <summary>
      ///   OpenRouter reasoning effort ("low", "medium", "high", or empty for the provider default). Lowering or
      ///   disabling reasoning is also what cuts moralizing refusals on adult fiction, so it is worth being able
      ///   to vary it here rather than only in the game's options.
      /// </summary>
      [JsonPropertyName("reasoningEffort")]
      public string? ReasoningEffort { get; set; }

      /// <summary>
      ///   Comma-separated OpenRouter provider slugs to pin, in order (empty = let OpenRouter route). Some
      ///   providers moderate their own output and cut a reply mid-sentence, which is exactly the kind of
      ///   behaviour this lane exists to catch, so pinning must be reproducible.
      /// </summary>
      [JsonPropertyName("providerPin")]
      public string? ProviderPin { get; set; }

      /// <summary>Whether OpenRouter may fall back to unpinned providers when the pinned ones are unavailable.</summary>
      [JsonPropertyName("allowProviderFallbacks")]
      public bool AllowProviderFallbacks { get; set; }

      /// <summary>Output cap per chat turn. The game ships 1500; raise it to study truncation behaviour.</summary>
      [JsonPropertyName("maxTokens")]
      public int MaxTokens { get; set; } = 1500;

      /// <summary>Sampling temperature for the turns under test. The game ships 0.7.</summary>
      [JsonPropertyName("temperature")]
      public float Temperature { get; set; } = 0.7f;

      /// <summary>
      ///   How strongly the model is pushed off words it has already used. The game ships 0.3; this is the one
      ///   lever that reduces repetition on weaker models, so it is worth being able to sweep it here.
      /// </summary>
      [JsonPropertyName("presencePenalty")]
      public float PresencePenalty { get; set; } = 0.3f;

      /// <summary>
      ///   Loads the settings beside the test assembly, then beside the project. Returns a CLOSED default when
      ///   no file exists or it cannot be read: a malformed file must never be read as permission to spend.
      /// </summary>
      public static LiveLlmSettings Load(out string source)
      {
         foreach (string path in CandidatePaths())
            try
            {
               if (!File.Exists(path)) continue;

               var loaded = JsonSerializer.Deserialize<LiveLlmSettings>(File.ReadAllText(path));

               if (loaded == null) continue;

               source = path;

               return loaded;
            }
            catch (Exception ex)
            {
               source = $"{path} (UNREADABLE: {ex.GetType().Name}: {ex.Message})";

               return new LiveLlmSettings();
            }

         source = "(no settings file found)";

         return new LiveLlmSettings();
      }

      /// <summary>
      ///   Where the file is looked for: beside the built test assembly first (so a copied-to-output file wins),
      ///   then walking up to the project directory, so editing the file in the project takes effect whether or
      ///   not a build has run since.
      /// </summary>
      private static string[] CandidatePaths()
      {
         string baseDir = AppContext.BaseDirectory;

         return new[] {
            Path.Combine(baseDir, FileName),
            Path.Combine(baseDir, "..", "..", "..", FileName)
         };
      }
   }
}
