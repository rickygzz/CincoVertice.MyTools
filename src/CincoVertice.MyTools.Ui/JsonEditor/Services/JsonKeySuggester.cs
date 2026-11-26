using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Schema;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     The names to offer for a new member of the object at the caret (Ctrl+Space): the schema's, then those the
///     objects in the same place elsewhere have, as the other items of its array: people[*].address. Names it has
///     already aren't offered.
/// </summary>
public static class JsonKeySuggester
{
    /// <returns>The object the caret is in, as a member of it would be written there; null outside every one.</returns>
    public static JsonNodeModel? ObjectAt(JsonNodeModel? root, DiffPosition caret)
    {
        JsonNodeModel? node = JsonPathWriter.NodeAt(root, caret);

        // In a value that isn't an object, as between the quotes of a string just typed, it's the one around it
        while (node is not null && node.Kind != JsonNodeKind.Object)
        {
            node = node.Parent;
        }

        return node;
    }

    public static IReadOnlyList<string> NamesFor(JsonNodeModel target, JsonNodeModel root, JsonSchemaValidator? schema)
    {
        IEnumerable<string> fromSchema = schema?.NamesFor(target) ?? [];
        IEnumerable<string> fromOthers = Alike(root, target)
            .Where(other => !ReferenceEquals(other, target))
            .SelectMany(other => other.Members.Select(member => member.Key));

        return [.. fromSchema.Concat(fromOthers).Distinct().Where(name => target.Member(name) is null)];
    }

    // The objects at the same path, any index for each array on the way
    private static IEnumerable<JsonNodeModel> Alike(JsonNodeModel root, JsonNodeModel target)
    {
        IEnumerable<JsonNodeModel> found = [root];

        foreach (JsonNodeModel step in JsonTree.LineageOf(target).Skip(1))
        {
            found = step.Index is not null
                ? found.Where(node => node.Kind == JsonNodeKind.Array).SelectMany(node => node.Items)
                : found.Select(node => node.Member(step.Key!)).OfType<JsonNodeModel>();
        }

        return found.Where(node => node.Kind == JsonNodeKind.Object);
    }
}
