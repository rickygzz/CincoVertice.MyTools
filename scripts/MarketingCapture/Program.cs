using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Application.Scripting;
using CincoVertice.MyTools.Ui.Calculator.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Presenters;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Win.Calculator;
using CincoVertice.MyTools.Ui.Win.ColorPicker;
using CincoVertice.MyTools.Ui.Win.ConfigEditor;
using CincoVertice.MyTools.Ui.Win.Diagramming;
using CincoVertice.MyTools.Ui.Win.Diff;
using CincoVertice.MyTools.Ui.Win.Snipping;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones;

// This is a screenshot fixture, not another tray application. It shows real product forms with sample data.
// It does not register hotkeys, run scripts, read the clipboard, capture the desktop, or change user settings.
internal static class Program
{
    private static string _root = string.Empty;
    private static string _images = string.Empty;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint window, nint destination, uint flags);

    [STAThread]
    private static void Main(string[] args)
    {
        _root = Path.GetFullPath(args.Single());
        _images = Path.Combine(_root, "docs", "images");
        Directory.CreateDirectory(_images);
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Snipping();
        Diagram();
        Diff();
        Calculator();
        Colors();
        Zones();
        Scripts();
    }

    private static void Capture(Form form, string name, Size? size = null)
    {
        using (form)
        {
            using var icons = new AppIconService();
            form.Icon = icons.Current;
            if (size is { } chosen) form.Size = chosen;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(80, 80);
            form.Show();
            for (int i = 0; i < 12; i++)
            {
                Application.DoEvents();
                Thread.Sleep(25);
            }
            form.Refresh();
            using var bitmap = new Bitmap(form.Width, form.Height, PixelFormat.Format32bppArgb);
            bool printed;
            using (var graphics = Graphics.FromImage(bitmap))
            {
                nint hdc = graphics.GetHdc();
                try { printed = PrintWindow(form.Handle, hdc, 2); }
                finally { graphics.ReleaseHdc(hdc); }
            }
            if (!printed) form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            string path = Path.Combine(_images, name + ".png");
            bitmap.Save(path, ImageFormat.Png);
            Console.WriteLine($"{name}.png: {bitmap.Width} x {bitmap.Height}");
            form.Hide();
        }
    }

    private static void Snipping()
    {
        var sample = new Bitmap(900, 490);
        using (var graphics = Graphics.FromImage(sample))
        {
            graphics.Clear(Color.FromArgb(245, 247, 250));
            using var title = new Font("Segoe UI", 23, FontStyle.Bold);
            using var body = new Font("Segoe UI", 14);
            graphics.FillRectangle(Brushes.White, 34, 28, 832, 420);
            graphics.DrawString("Weekend trip checklist", title, Brushes.MidnightBlue, 65, 54);
            graphics.DrawString("A sample note for the screenshot tutorial", body, Brushes.SlateGray, 67, 103);
            graphics.DrawString("1. Book the train tickets", body, Brushes.Black, 70, 174);
            graphics.DrawString("2. Share the meeting point", body, Brushes.Black, 70, 224);
            graphics.DrawString("3. Pack a rain jacket", body, Brushes.Black, 70, 274);
            graphics.DrawString("Booking code:", body, Brushes.Black, 70, 365);
            graphics.DrawString("DEMO-1234", body, Brushes.Black, 265, 365);
        }
        var form = new SnipEditorForm(sample);
        form.ShowTool(SnipTool.Arrow);
        form.ShowColor(Color.FromArgb(0, 110, 200));
        form.ShowSizes([2, 4, 6, 8], 4);
        form.ShowTransparencies([0, 25, 50, 75], 0);
        form.ShowArrowHeads(SnipArrowHeads.End);
        form.ShowAnnotations([
            new RectangleAnnotationModel(new Rectangle(58, 210, 374, 48), Color.FromArgb(0, 110, 200), 3),
            new ArrowAnnotationModel(new Point(653, 211), new Point(443, 233), Color.FromArgb(0, 110, 200), 4),
            new TextAnnotationModel(new Point(527, 158), "Meet here first", Color.FromArgb(0, 110, 200), 23),
            new BlurAnnotationModel(new Rectangle(255, 359, 190, 43)),
            new StepAnnotationModel(new Point(501, 384), 1, Color.FromArgb(0, 110, 200), 35),
        ]);
        form.ShowUndoRedo(true, false);
        Capture(form, "snip-editor");
    }

    private static void Diagram()
    {
        DiagramStyleModel blue = new(Color.FromArgb(224, 239, 255), Color.FromArgb(39, 102, 171), 2,
            false, Color.FromArgb(25, 53, 85), 20);
        DiagramStyleModel green = blue with { Fill = Color.FromArgb(226, 245, 232), Line = Color.SeaGreen };
        DiagramModel diagram = new([
            new(1, DiagramShapeKind.RoundedRectangle, new Rectangle(60, 125, 190, 90), "Pick a destination", blue),
            new(2, DiagramShapeKind.RoundedRectangle, new Rectangle(330, 125, 190, 90), "Plan the journey", blue),
            new(3, DiagramShapeKind.RoundedRectangle, new Rectangle(600, 125, 190, 90), "Book the tickets", green),
            new(4, DiagramShapeKind.RoundedRectangle, new Rectangle(330, 325, 190, 90), "Share the plan", green),
            new(5, DiagramShapeKind.Text, new Rectangle(60, 30, 650, 60), "A weekend away, planned together",
                blue with { Fill = Color.Empty, FontSize = 27, TextAlignment = DiagramTextAlignment.Left }),
        ], [
            new(6, 1, 2, DiagramRouting.Straight, DiagramArrowHeads.End, "", blue),
            new(7, 2, 3, DiagramRouting.Straight, DiagramArrowHeads.End, "", blue),
            new(8, 3, 4, DiagramRouting.Elbow, DiagramArrowHeads.End, "Ready to go", blue with { FontSize = 16 }),
        ]);
        File.WriteAllText(Path.Combine(_root, "docs", "tutorials", "samples", "weekend-plan.svg"), DiagramSvg.Write(diagram));
        var form = new DiagramEditorForm();
        form.ShowSizes([1, 2, 3, 4, 6], [12, 14, 16, 18, 20, 24, 28], [0, 1, 2, 3]);
        form.ShowStyle(blue, DiagramRouting.Straight, DiagramArrowHeads.End);
        form.ShowTool(DiagramTool.Select);
        form.ShowDocument("weekend-plan.svg", false);
        form.ShowPropertiesPanel(false);
        form.ShowImagesPanel(false);
        form.ShowLayersPanel(false);
        form.ShowZoom(100);
        form.ShowScene(new(diagram, new HashSet<int>(), new HashSet<int>(), new(1, Point.Empty), null, [], null, null, null));
        Capture(form, "diagram-editor", new Size(1060, 650));
    }

    private static void Diff()
    {
        string folder = Path.Combine(_root, "docs", "tutorials", "samples");
        string[] left = TextDiffer.SplitLines(File.ReadAllText(Path.Combine(folder, "weekend-before.txt")), true);
        string[] right = TextDiffer.SplitLines(File.ReadAllText(Path.Combine(folder, "weekend-after.txt")), true);
        var form = new DiffForm();
        form.ShowText(DiffSide.Left, left);
        form.ShowText(DiffSide.Right, right);
        form.ShowSides("weekend-before.txt", "weekend-after.txt");
        form.ShowTitle("weekend-before.txt vs weekend-after.txt - Compare files");
        DiffResultModel result = TextDiffer.Compare(left, right, new());
        form.ShowDiff(result, false);
        form.ShowCanNavigate(result.HasChanges);
        form.ShowChange(result.Hunks.FirstOrDefault());
        form.ShowStatus(DiffPresenter.Summary(result,
            new(left, "weekend-before.txt", null, DiffTextFormatModel.Default),
            new(right, "weekend-after.txt", null, DiffTextFormatModel.Default), new()),
            $"Change 1 of {result.Hunks.Count}");
        Capture(form, "compare-files", new Size(1100, 560));
    }

    private static void Calculator()
    {
        var calculator = new ExpressionCalculator();
        string[] expressions = ["(45 + 18.50) * 1.15", "10 km to mi", "100 F to C", "2 GB to MiB"];
        var history = expressions.Select(expression => new CalculatorHistoryEntryModel(expression,
            calculator.EvaluateToText(expression, groupThousands: true))).ToArray();
        var form = new CalculatorForm();
        form.SetExpression(expressions[^1], expressions[^1].Length);
        form.ShowHistory(history, "");
        Capture(form, "calculator");
    }

    private static void Colors()
    {
        var form = new ColorForm();
        Color color = Color.FromArgb(39, 102, 171);
        form.ShowPalette(CincoVertice.MyTools.Ui.ColorPicker.Services.ColorPalette.Of(color));
        form.ShowChosen(ColorPresenter.ValuesOf(color));
        Capture(form, "color-picker");
    }

    private static void Zones()
    {
        var form = new ZoneEditorForm();
        form.ShowTargets(["Default layout"], 0, new Size(1920, 1080));
        form.ShowOwnLayout(false, false);
        form.ShowGaps([0, 4, 8, 12, 16], 8);
        form.ShowZones([new(0, 0, 60, 100), new(60, 0, 40, 50), new(60, 50, 40, 50)], null, 8);
        Capture(form, "window-zones", new Size(980, 640));
    }

    private static void Scripts()
    {
        string script = File.ReadAllText(Path.Combine(_root, "docs", "tutorials", "samples", "SearchTheWeb.mts"));
        if (ScriptEngine.FindSyntaxError(script) is { } error)
        {
            throw new InvalidOperationException($"The sample script has an error: {error.Description}");
        }
        var form = new ConfigEditorForm();
        var file = new EditableFileModel("SearchTheWeb.mts", "SearchTheWeb.mts", EditableFileKind.Script);
        form.ShowFiles([new("hotkeys.cfg", "hotkeys.cfg", EditableFileKind.Hotkeys), file], file);
        form.ShowCode(script.ReplaceLineEndings("\n"));
        form.ShowHighlights(new ScriptHighlighter().Find(script.ReplaceLineEndings("\n")));
        form.ShowTitle("SearchTheWeb.mts - Config editor");
        form.ShowCanSave(true);
        form.ShowProblems([]);
        Capture(form, "scripts", new Size(1020, 570));
    }
}
