using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Copilot
{
    /// <summary>
    /// Provides methods to display autocomplete suggestions in the Visual Studio text editor.
    /// </summary>
    public static class Suggestions
    {
        private static readonly MethodInfo tryDisplaySuggestionAsyncType;

        private static readonly MethodInfo cacheProposalType;

        private static readonly FieldInfo suggestionManagerType;

        private static readonly FieldInfo sessionType;

        private static readonly Type generateResultType;

        private static readonly Type inlineCompletionsType;

        private static readonly Type inlineCompletionSuggestion;

        /// <summary>
        /// Initializes static members of the Suggestions class by loading types from the "Microsoft.VisualStudio.IntelliCode" assembly
        /// and setting up reflection-based access to specific methods and fields.
        /// </summary>
        static Suggestions()
        {
            Type[] types = Assembly.Load("Microsoft.VisualStudio.IntelliCode").GetTypes();

            foreach (Type type in types)
            {
                if (type.Name == "GenerateResult")
                {
                    generateResultType = type;
                }
                if (type.Name == "InlineCompletionsInstance")
                {
                    inlineCompletionsType = type;
                }
                if (type.Name == "InlineCompletionSuggestion")
                {
                    inlineCompletionSuggestion = type;
                }
            }

            cacheProposalType = inlineCompletionsType.GetMethod("CacheProposal", BindingFlags.Instance | BindingFlags.NonPublic);
            suggestionManagerType = inlineCompletionsType.GetField("_suggestionManager", BindingFlags.Instance | BindingFlags.NonPublic);
            sessionType = inlineCompletionsType.GetField("Session", BindingFlags.Instance | BindingFlags.NonPublic);

            if (suggestionManagerType != null)
            {
                tryDisplaySuggestionAsyncType = suggestionManagerType.FieldType.GetMethod("TryDisplaySuggestionAsync");
            }
        }

        /// <summary>
        /// Displays an autocomplete suggestion asynchronously in the provided text view at the specified position.
        /// </summary>
        /// <param name="textView">The text view where the autocomplete suggestion will be displayed.</param>
        /// <param name="autocompleteText">The text to be used for the autocomplete suggestion.</param>
        /// <param name="position">The position in the text view where the autocomplete suggestion will be displayed.</param>
        /// </summary>
        public static async Task ShowAutocompleteAsync(ITextView textView, string autocompleteText, int position)
        {
            object inlineCompletionsInstance = textView.Properties.PropertyList.FirstOrDefault((KeyValuePair<object, object> x) => x.Key is Type && (x.Key as Type).Name == "InlineCompletionsInstance").Value;

            object value = sessionType.GetValue(inlineCompletionsInstance);

            SuggestionSessionBase val = (SuggestionSessionBase)((value is SuggestionSessionBase) ? value : null);

            if (val != null)
            {
                await val.DismissAsync((ReasonForDismiss)131079, default);
            }

            ProposalCollection proposalCollection = Proposals.CollectionFromText(autocompleteText, textView, position);

            int num = 0;

            object obj = Activator.CreateInstance(generateResultType, proposalCollection, null);

            object obj2 = inlineCompletionSuggestion.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(
            [
                new VirtualSnapshotPoint(textView.TextSnapshot, position),
                    null,
                    obj,
                    inlineCompletionsInstance,
                    num
            ]);

            object value2 = suggestionManagerType.GetValue(inlineCompletionsInstance);

            SuggestionSessionBase val2 = await (Task<SuggestionSessionBase>)tryDisplaySuggestionAsyncType.Invoke(value2, [obj2, null]);

            if (val2 != null)
            {
                SuggestionSessionBase val3 = val2;

                cacheProposalType.Invoke(inlineCompletionsInstance, [proposalCollection.Proposals.First()]);
                sessionType.SetValue(inlineCompletionsInstance, val2);

                await val3.DisplayProposalAsync(proposalCollection.Proposals.First(), default);
            }
        }
    }

    /// <summary>
    /// Provides methods to create and manage proposal collections for text suggestions.
    /// </summary>
    public static class Proposals
    {
        /// <summary>
        /// Creates a ProposalCollection from the provided text and position within the given ITextView.
        /// </summary>
        /// <param name="gen">The generated text to be included in the proposal.</param>
        /// <param name="textView">The ITextView instance where the proposal will be applied.</param>
        /// <param name="position">The position within the textView where the proposal starts.</param>
        /// <returns>
        /// A ProposalCollection containing the generated proposal.
        /// </returns>
        public static ProposalCollection CollectionFromText(string gen, ITextView textView, int position)
        {
            VirtualSnapshotPoint val = new(textView.TextSnapshot, position);

            SnapshotSpan val2 = new(val.Position, 0);

            ProposedEdit item = new(val2, gen);

            List<ProposedEdit> list = [item];

            List<Proposal> list2 =
            [
                new Proposal($"{Utils.Constants.EXTENSION_NAME} Suggestion", ImmutableArray.ToImmutableArray(list), val, null, (ProposalFlags)17, null, null, null, null, null)
            ];

            return new ProposalCollection(Utils.Constants.EXTENSION_NAME, (IReadOnlyList<ProposalBase>)list2);
        }
    }
}