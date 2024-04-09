using Discord;
using Discord.Interactions;
using Lavalink4NET;

namespace CastorDJ.AutoCompleteHandlers
{
    public class MusicAutoCompleteHandler : AutocompleteHandler
    {
        private readonly IAudioService _audioService;

        public MusicAutoCompleteHandler(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var text = autocompleteInteraction.Data.Options.FirstOrDefault().Value.ToString();

            var suggestions = await _audioService.Tracks.LoadTracksAsync(text, Lavalink4NET.Rest.Entities.Tracks.TrackSearchMode.YouTube);

            if(suggestions.IsSuccess && suggestions.Count > 0)
            {
                var response = suggestions.Tracks.Take(25).Select(t => new AutocompleteResult(t.Title, t.Title));

                return AutocompletionResult.FromSuccess(response);
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
