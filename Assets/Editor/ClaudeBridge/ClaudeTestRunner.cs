using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System;

namespace ClaudeBridge
{
    public static class ClaudeTestRunner
    {
        private static string currentCommand = "";
        private static DateTime startTime;

        [UnityEditor.MenuItem("Tools/Claude/Run EditMode Tests")]
        public static void RunEditModeTests() => RunTests(TestMode.EditMode, "RunEditModeTests");

        [UnityEditor.MenuItem("Tools/Claude/Run PlayMode Tests")]
        public static void RunPlayModeTests() => RunTests(TestMode.PlayMode, "RunPlayModeTests");

        private static void RunTests(TestMode mode, string cmd)
        {
            currentCommand = cmd;
            startTime = DateTime.UtcNow;
            
            var api = new TestRunnerApi();
            var filter = new Filter { testMode = mode };
            
            Debug.Log($"[CLAUDE-TEST] Lancement des tests {mode}...");
            api.Execute(new ExecutionSettings(filter));
            api.RegisterCallbacks(new TestListener(cmd, startTime));
        }

        private class TestListener : ICallbacks
        {
            private readonly string cmd;
            private readonly DateTime startTime;
            private int totalTests = 0;
            private int passedTests = 0;
            private int failedTests = 0;
            private int skippedTests = 0;
            private System.Text.StringBuilder failures = new System.Text.StringBuilder();

            public TestListener(string cmd, DateTime startTime)
            {
                this.cmd = cmd;
                this.startTime = startTime;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                totalTests = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                passedTests = result.PassCount;
                failedTests = result.FailCount;
                skippedTests = result.SkipCount;

                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                var passed = result.TestStatus == TestStatus.Passed;
                var msg = $"{passedTests} passed, {failedTests} failed, {skippedTests} skipped ({duration:F2}s)";
                
                Debug.Log($"[CLAUDE-TEST] {msg}");
                
                var bridge = typeof(ClaudeCodeBridge);
                var method = bridge.GetMethod("WriteJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, new object[] { 
                    cmd, 
                    passed ? "success" : "error", 
                    new { 
                        message = msg,
                        total = totalTests,
                        passed = passedTests,
                        failed = failedTests,
                        skipped = skippedTests,
                        durationSeconds = duration,
                        successRate = totalTests > 0 ? (float)passedTests / totalTests * 100 : 0,
                        failures = failures.ToString()
                    }
                });
            }

            public void RunStarted(ITestAdaptor tests)
            {
                Debug.Log($"[CLAUDE-TEST] Tests démarrés: {tests.Name}");
            }

            public void TestStarted(ITestAdaptor test)
            {
                // Debug.Log($"[CLAUDE-TEST] Test: {test.Name}");
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    failedTests++;
                    failures.AppendLine($"❌ {result.Test.Name}: {result.Message}");
                    if (!string.IsNullOrEmpty(result.StackTrace))
                        failures.AppendLine($"   {result.StackTrace}");
                }
                else if (result.TestStatus == TestStatus.Passed)
                {
                    passedTests++;
                }
                else if (result.TestStatus == TestStatus.Skipped)
                {
                    skippedTests++;
                }
            }
        }
    }
}