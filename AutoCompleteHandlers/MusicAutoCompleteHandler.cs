using Discord;
using Discord.Interactions;
using Lavalink4NET;
using System.Text.RegularExpressions;

namespace CastorDJ.AutoCompleteHandlers
{
    public class MusicAutoCompleteHandler : AutocompleteHandler
    {
        private readonly IAudioService _audioService;

        public MusicAutoCompleteHandler(IAudioService audioService)
        {
            _audioService = audioService;
        }

        private static string NormalizeTitle(string title)
        {
            var t = title.ToLowerInvariant();
            t = Regex.Replace(t, "\\s+", " ");
            var remove = new[] { "(official video)", "[official video]", "(lyrics)", "[lyrics]", "(audio)", "[audio]", "(hd)", "[hd]", "|", "/", "•" };
            foreach (var r in remove) t = t.Replace(r, string.Empty);
            return t.Trim();
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var text = autocompleteInteraction.Data.Options.FirstOrDefault()?.Value?.ToString() ?? string.Empty;

            var suggestions = await _audioService.Tracks.LoadTracksAsync(text, Lavalink4NET.Rest.Entities.Tracks.TrackSearchMode.YouTube);

            if (suggestions.IsSuccess && suggestions.Tracks.Count() > 0)
            {
                // Filter out shorts and near-duplicates
                var seen = new HashSet<string>();
                var filtered = suggestions.Tracks
                    .Where(t => t.Duration >= TimeSpan.FromSeconds(60))
                    .Where(t => !t.Uri.AbsolutePath.Contains("/shorts", StringComparison.OrdinalIgnoreCase))
                    .Where(t => seen.Add(NormalizeTitle(t.Title)))
                    .Take(25)
                    .Select(t => new AutocompleteResult(t.Title, t.Identifier));

                return AutocompletionResult.FromSuccess(filtered);
            }
            else
            {
                IEnumerable<AutocompleteResult> results = new[]
                {
                    new AutocompleteResult("Sem resultados", "")
                };
                return AutocompletionResult.FromSuccess(results);
            }
        }
    }
}
