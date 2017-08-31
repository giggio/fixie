namespace Fixie.Tests.Execution.Listeners
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Assertions;
    using Fixie.Execution;
    using Fixie.Execution.Listeners;

    public class TestExplorerListenerTests : MessagingTests
    {
        public void ShouldReportDiscoveredMethodsAsStreamOfJsonMessages()
        {
            var results = new List<TestExplorerListener.Test>();

            using (var stream = new MemoryStream())
            using (var reader = new BinaryReader(stream))
            {
                var listener = new TestExplorerListener(stream);

                using (var console = new RedirectedConsole())
                {
                    Discover(listener);

                    console.Lines().ShouldBeEmpty();
                }

                stream.Seek(0, SeekOrigin.Begin);

                while (stream.Position != stream.Length)
                    results.Add(Deserialize<TestExplorerListener.Test>(reader.ReadString()));

                results = results.OrderBy(x => x.FullyQualifiedName).ToList();
            }

            results.Count.ShouldEqual(5);

            var fail = results[0];
            var failByAssertion = results[1];
            var pass = results[2];
            var skipWithoutReason = results[3];
            var skipWithReason = results[4];

            fail.FullyQualifiedName.ShouldEqual(TestClass + ".Fail");
            fail.DisplayName.ShouldEqual(TestClass + ".Fail");

            failByAssertion.FullyQualifiedName.ShouldEqual(TestClass + ".FailByAssertion");
            failByAssertion.DisplayName.ShouldEqual(TestClass + ".FailByAssertion");

            pass.FullyQualifiedName.ShouldEqual(TestClass + ".Pass");
            pass.DisplayName.ShouldEqual(TestClass + ".Pass");

            skipWithoutReason.FullyQualifiedName.ShouldEqual(TestClass + ".SkipWithoutReason");
            skipWithoutReason.DisplayName.ShouldEqual(TestClass + ".SkipWithoutReason");

            skipWithReason.FullyQualifiedName.ShouldEqual(TestClass + ".SkipWithReason");
            skipWithReason.DisplayName.ShouldEqual(TestClass + ".SkipWithReason");
        }

        public void ShouldReportResultsAsStreamOfJsonMessages()
        {
            var results = new List<TestExplorerListener.TestResult>();

            using (var stream = new MemoryStream())
            using (var reader = new BinaryReader(stream))
            {
                var listener = new TestExplorerListener(stream);

                using (var console = new RedirectedConsole())
                {
                    Run(listener);

                    console.Lines()
                        .ShouldEqual(
                            "Console.Out: Fail",
                            "Console.Error: Fail",
                            "Console.Out: FailByAssertion",
                            "Console.Error: FailByAssertion",
                            "Console.Out: Pass",
                            "Console.Error: Pass");
                }

                stream.Seek(0, SeekOrigin.Begin);

                while (stream.Position != stream.Length)
                    results.Add(Deserialize<TestExplorerListener.TestResult>(reader.ReadString()));
            }

            results.Count.ShouldEqual(5);

            var skipWithReason = results[0];
            var skipWithoutReason = results[1];
            var fail = results[2];
            var failByAssertion = results[3];
            var pass = results[4];

            skipWithReason.FullyQualifiedName.ShouldEqual(TestClass + ".SkipWithReason");
            skipWithReason.DisplayName.ShouldEqual(TestClass + ".SkipWithReason");
            skipWithReason.Outcome.ShouldEqual("Skipped");
            skipWithReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithReason.ErrorMessage.ShouldEqual("Skipped with reason.");
            skipWithReason.ErrorStackTrace.ShouldBeNull();
            skipWithReason.Output.ShouldBeNull();

            skipWithoutReason.FullyQualifiedName.ShouldEqual(TestClass + ".SkipWithoutReason");
            skipWithoutReason.DisplayName.ShouldEqual(TestClass + ".SkipWithoutReason");
            skipWithoutReason.Outcome.ShouldEqual("Skipped");
            skipWithoutReason.Duration.ShouldEqual(TimeSpan.Zero);
            skipWithoutReason.ErrorMessage.ShouldBeNull();
            skipWithoutReason.ErrorStackTrace.ShouldBeNull();
            skipWithoutReason.Output.ShouldBeNull();

            fail.FullyQualifiedName.ShouldEqual(TestClass + ".Fail");
            fail.DisplayName.ShouldEqual(TestClass + ".Fail");
            fail.Outcome.ShouldEqual("Failed");
            fail.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            fail.ErrorMessage.ShouldEqual("'Fail' failed!");
            fail.ErrorStackTrace
                .CleanStackTraceLineNumbers()
                .Lines()
                .ShouldEqual("Fixie.Tests.FailureException", At("Fail()"));
            fail.Output.Lines().ShouldEqual("Console.Out: Fail", "Console.Error: Fail");

            failByAssertion.FullyQualifiedName.ShouldEqual(TestClass + ".FailByAssertion");
            failByAssertion.DisplayName.ShouldEqual(TestClass + ".FailByAssertion");
            failByAssertion.Outcome.ShouldEqual("Failed");
            failByAssertion.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            failByAssertion.ErrorMessage.Lines().ShouldEqual(
                "Assertion Failure",
                "Expected: 2",
                "Actual:   1");
            failByAssertion.ErrorStackTrace
                .CleanStackTraceLineNumbers()
                .ShouldEqual(At("FailByAssertion()"));
            failByAssertion.Output.Lines().ShouldEqual("Console.Out: FailByAssertion", "Console.Error: FailByAssertion");

            pass.FullyQualifiedName.ShouldEqual(TestClass + ".Pass");
            pass.DisplayName.ShouldEqual(TestClass + ".Pass");
            pass.Outcome.ShouldEqual("Passed");
            pass.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
            pass.ErrorMessage.ShouldBeNull();
            pass.ErrorStackTrace.ShouldBeNull();
            pass.Output.Lines().ShouldEqual("Console.Out: Pass", "Console.Error: Pass");
        }
    }
}