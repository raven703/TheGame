using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Persist the framework result independently of MCP job initialization/polling.
[InitializeOnLoad]
public sealed class CombatTestEvidence : ICallbacks
{
    private static readonly TestRunnerApi Api;
    private static readonly string DirectoryPath = Path.GetFullPath("Tools/CombatEvidence/TestRuns");
    static CombatTestEvidence()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new CombatTestEvidence());
    }
    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        Directory.CreateDirectory(DirectoryPath);
        string prefix = Path.Combine(DirectoryPath, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        TestRunnerApi.SaveResultToFile(result, prefix + ".xml");
        string log = Path.GetFullPath("Logs/Editor.log");
        if (File.Exists(log))
        {
            using (var input = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var output = File.Create(prefix + ".log")) input.CopyTo(output);
        }
    }
}