namespace Fixie.Execution.Listeners
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using Execution;

    public class TestExplorerListener :
        Handler<MethodDiscovered>,
        Handler<CaseSkipped>,
        Handler<CasePassed>,
        Handler<CaseFailed>
    {
        readonly BinaryWriter writer;

        public TestExplorerListener(Stream outputStream)
        {
            writer = new BinaryWriter(outputStream);
        }

        public void Handle(MethodDiscovered message)
        {
            var methodGroup = new MethodGroup(message.Class, message.Method);

            Write(new Test
            {
                FullyQualifiedName = methodGroup.FullName,
                DisplayName = methodGroup.FullName
            });
        }

        public void Handle(CaseSkipped message)
        {
            Write(message, x =>
            {
                x.Outcome = "Skipped";
                x.ErrorMessage = message.Reason;
            });
        }

        public void Handle(CasePassed message)
        {
            Write(message, x =>
            {
                x.Outcome = "Passed";
            });
        }

        public void Handle(CaseFailed message)
        {
            var exception = message.Exception;

            Write(message, x =>
            {
                x.Outcome = "Failed";
                x.ErrorMessage = exception.Message;
                x.ErrorStackTrace = exception.TypedStackTrace();
            });
        }

        void Write(CaseCompleted message, Action<TestResult> customize)
        {
            var testResult = new TestResult
            {
                FullyQualifiedName = FullyQualifiedName(message.Class, message.Method),
                DisplayName = message.Name,
                Duration = message.Duration,
                Output = message.Output
            };

            customize(testResult);

            Write(testResult);
        }

        static string FullyQualifiedName(Type @class, MethodInfo method)
            => new MethodGroup(@class, method).FullName;

        void Write<T>(T message)
        {
            writer.Write(Serialize(message));
            writer.Flush();
        }

        static string Serialize<T>(T message)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, message);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public class Test
        {
            public string FullyQualifiedName { get; set; }
            public string DisplayName { get; set; }
        }

        public class TestResult
        {
            public string FullyQualifiedName { get; set; }
            public string DisplayName { get; set; }
            public string Outcome { get; set; }
            public TimeSpan Duration { get; set; }
            public string Output { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorStackTrace { get; set; }
        }
    }
}