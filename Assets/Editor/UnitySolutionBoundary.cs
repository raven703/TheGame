using System.Linq;
using System.Xml.Linq;
using UnityEditor;

// The IDE generator preserves external projects from the previous solution.
// Independent builds belong in DotNet/Game.Core.sln, never in Unity's solution.
public sealed class UnitySolutionBoundary : AssetPostprocessor
{
    public static string OnGeneratedSlnSolution(string path, string content)
    {
        if (!path.EndsWith(".slnx", System.StringComparison.OrdinalIgnoreCase)) return content;
        var document = XDocument.Parse(content);
        foreach (var project in document.Descendants("Project").ToArray())
        {
            var projectPath = ((string)project.Attribute("Path") ?? "").Replace('\\', '/');
            if (projectPath.StartsWith("DotNet/", System.StringComparison.OrdinalIgnoreCase))
                project.Remove();
        }
        return document.ToString() + System.Environment.NewLine;
    }
}