using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace WheaExplorer.Services;

/// <summary>Builds an expandable <see cref="TreeView"/> from decoded JSON for easier browsing.</summary>
public static class JsonTreeViewBuilder
{
    public static void Populate(TreeView tree, string? json)
    {
        tree.Items.Clear();
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var token = JToken.Parse(json);
            switch (token)
            {
                case JObject obj:
                    foreach (var prop in obj.Properties())
                        tree.Items.Add(CreateItem(prop.Value, prop.Name));
                    break;
                case JArray arr:
                    for (var i = 0; i < arr.Count; i++)
                        tree.Items.Add(CreateItem(arr[i]!, $"[{i}]"));
                    break;
                default:
                    tree.Items.Add(CreateItem(token, "Value"));
                    break;
            }

            foreach (TreeViewItem item in tree.Items)
                item.IsExpanded = true;
        }
        catch (Exception ex)
        {
            tree.Items.Add(new TreeViewItem
            {
                Header = $"Could not build tree: {ex.Message}",
                IsEnabled = false
            });
        }
    }

    private static TreeViewItem CreateItem(JToken token, string label)
    {
        var item = new TreeViewItem();

        switch (token.Type)
        {
            case JTokenType.Object:
            {
                var obj = (JObject)token;
                item.Header = $"{label}  ({obj.Properties().Count()} fields)";
                foreach (var prop in obj.Properties())
                    item.Items.Add(CreateItem(prop.Value, prop.Name));
                break;
            }
            case JTokenType.Array:
            {
                var arr = (JArray)token;
                item.Header = $"{label}  ({arr.Count} items)";
                for (var i = 0; i < arr.Count; i++)
                    item.Items.Add(CreateItem(arr[i]!, $"[{i}]"));
                break;
            }
            default:
                item.Header = $"{label}: {FormatScalar(token)}";
                break;
        }

        return item;
    }

    private static string FormatScalar(JToken token)
    {
        if (token.Type == JTokenType.String)
            return $"\"{token.Value<string>()}\"";

        return token.ToString(Newtonsoft.Json.Formatting.None);
    }
}
