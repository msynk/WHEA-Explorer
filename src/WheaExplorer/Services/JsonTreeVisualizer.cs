using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace WheaExplorer.Services;

public static class JsonTreeVisualizer
{
    private static readonly Brush ValueBrush = new SolidColorBrush(Color.FromRgb(0x0B, 0x5E, 0x8A));
    private static readonly Brush KeyBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1C));
    private static readonly Brush TypeBrush = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x78));

    static JsonTreeVisualizer()
    {
        ValueBrush.Freeze();
        KeyBrush.Freeze();
        TypeBrush.Freeze();
    }

    public static void Populate(TreeView tree, string jsonOrError)
    {
        tree.Items.Clear();
        if (string.IsNullOrWhiteSpace(jsonOrError))
            return;

        try
        {
            var token = JToken.Parse(jsonOrError);
            tree.Items.Add(BuildNode(token, "(root)"));
        }
        catch (Exception ex)
        {
            tree.Items.Add(new TreeViewItem
            {
                Header = Row("Could not build tree", ex.Message, Brushes.DarkRed, Brushes.DarkRed),
                IsExpanded = true
            });
        }
    }

    private static TreeViewItem BuildNode(JToken token, string label)
    {
        var item = new TreeViewItem { IsExpanded = token is JObject or JArray };

        switch (token)
        {
            case JObject obj:
                item.Header = Row(label, $"object · {obj.Count} propert{(obj.Count == 1 ? "y" : "ies")}", KeyBrush, TypeBrush);
                foreach (var prop in obj.Properties())
                    item.Items.Add(BuildNode(prop.Value, prop.Name));
                break;
            case JArray arr:
                item.Header = Row(label, $"array · {arr.Count} item{(arr.Count == 1 ? "" : "s")}", KeyBrush, TypeBrush);
                for (var i = 0; i < arr.Count; i++)
                    item.Items.Add(BuildNode(arr[i]!, $"[{i}]"));
                break;
            case JValue val:
                item.Header = ValueRow(label, val);
                break;
            default:
                item.Header = Row(label, token.ToString(), KeyBrush, ValueBrush);
                break;
        }

        return item;
    }

    private static UIElement Row(string name, string detail, Brush nameBrush, Brush detailBrush)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
            Foreground = nameBrush,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        });
        panel.Children.Add(new TextBlock
        {
            Text = detail,
            Foreground = detailBrush,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        });
        return panel;
    }

    private static UIElement ValueRow(string name, JValue val)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        panel.Children.Add(new TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
            Foreground = KeyBrush,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        var typeHint = val.Type switch
        {
            JTokenType.String => "string",
            JTokenType.Integer => "number",
            JTokenType.Float => "number",
            JTokenType.Boolean => "boolean",
            JTokenType.Null => "null",
            _ => val.Type.ToString().ToLowerInvariant()
        };

        panel.Children.Add(new TextBlock
        {
            Text = typeHint,
            Foreground = TypeBrush,
            FontSize = 11,
            Margin = new Thickness(0, 2, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        var valueText = val.Type == JTokenType.String
            ? "\"" + (val.Value<string>() ?? "").Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : val.ToString();

        panel.Children.Add(new TextBlock
        {
            Text = valueText,
            Foreground = ValueBrush,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        });

        return panel;
    }
}
