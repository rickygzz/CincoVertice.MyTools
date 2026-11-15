using System.Text;

namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A file as read, in what encoding, and when it was last written, to tell when it changes.</summary>
public sealed record DiffFileModel(string Text, DateTime WrittenUtc, Encoding Encoding);
