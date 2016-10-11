namespace Fixie.Runner
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class SourceLocationProvider
    {
        readonly string assemblyPath;
        IDictionary<string, IDictionary<string, SourceLocation>> classMethodLocations;

        public SourceLocationProvider(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
        }

        public bool TryGetSourceLocation(MethodGroup methodGroup, out SourceLocation sourceLocation)
        {
            if (classMethodLocations == null)
                classMethodLocations = CacheLocations(assemblyPath);

            var className = methodGroup.Class;
            var methodName = methodGroup.Method;

            var standardizeTypeName = StandardizeTypeName(className);
            if (classMethodLocations.ContainsKey(standardizeTypeName))
            {
                if (classMethodLocations[standardizeTypeName].ContainsKey(methodName))
                {
                    sourceLocation = classMethodLocations[standardizeTypeName][methodName];
                    return true;
                }
            }

            sourceLocation = null;
            return false;
        }

        static Dictionary<string, IDictionary<string, SourceLocation>> CacheLocations(string assemblyPath)
        {
            var classMethodLocations = new Dictionary<string, IDictionary<string, SourceLocation>>();

            var readerParameters = new ReaderParameters { ReadSymbols = true };
            using (var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters))
            {
                foreach (var type in module.GetTypes())
                {
                    if (!classMethodLocations.ContainsKey(type.FullName))
                        classMethodLocations[type.FullName] = new Dictionary<string, SourceLocation>();

                    foreach (var method in type.GetMethods())
                    {
                        var sequencePoint = FirstOrDefaultSequencePoint(method);
                        if (sequencePoint != null)
                        {
                            if (!classMethodLocations[type.FullName].ContainsKey(method.Name))
                            {
                                classMethodLocations[type.FullName][method.Name] = new SourceLocation(sequencePoint.Document.Url, sequencePoint.StartLine);
                            }
                            else if (sequencePoint.StartLine < classMethodLocations[type.FullName][method.Name].LineNumber)
                            {
                                classMethodLocations[type.FullName][method.Name] = new SourceLocation(sequencePoint.Document.Url, sequencePoint.StartLine);
                            }
                        }
                    }
                }
            }

            return classMethodLocations;
        }

        static SequencePoint FirstOrDefaultSequencePoint(MethodDefinition testMethod)
        {
            CustomAttribute asyncStateMachineAttribute;

            if (TryGetAsyncStateMachineAttribute(testMethod, out asyncStateMachineAttribute))
                testMethod = GetStateMachineMoveNextMethod(asyncStateMachineAttribute);

            return FirstOrDefaultUnhiddenSequencePoint(testMethod);
        }

        static bool TryGetAsyncStateMachineAttribute(MethodDefinition method, out CustomAttribute attribute)
        {
            attribute = method.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == typeof(AsyncStateMachineAttribute).Name);
            return attribute != null;
        }

        static MethodDefinition GetStateMachineMoveNextMethod(CustomAttribute asyncStateMachineAttribute)
        {
            var stateMachineType = (TypeDefinition)asyncStateMachineAttribute.ConstructorArguments[0].Value;
            var stateMachineMoveNextMethod = stateMachineType.GetMethods().First(m => m.Name == "MoveNext");
            return stateMachineMoveNextMethod;
        }

        static SequencePoint FirstOrDefaultUnhiddenSequencePoint(MethodDefinition testMethod)
        {
            var body = testMethod.Body;

            const int lineNumberIndicatingHiddenLine = 16707566; //0xfeefee

            foreach (var instruction in body.Instructions)
            {
                var sequencePoint = testMethod.DebugInformation.GetSequencePoint(instruction);
                if (sequencePoint != null && sequencePoint.StartLine != lineNumberIndicatingHiddenLine)
                    return sequencePoint;
            }

            return null;
        }

        static string StandardizeTypeName(string className)
        {
            //Mono.Cecil respects ECMA-335 for the FullName of a type, which can differ from Type.FullName.
            //In order to make reliable comparisons between the class part of a MethodGroup, the class part
            //must be standardized to the ECMA-335 format.
            //
            //ECMA-335 specifies "/" instead of "+" to indicate a nested type.

            return className.Replace("+", "/");
        }
    }

    static class TypeDefinitionShim
    {
        public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition self)
        {
            if (!self.HasMethods)
                return Enumerable.Empty<MethodDefinition>();

            return self.Methods.Where(method => !method.IsConstructor);
        }
    }
}