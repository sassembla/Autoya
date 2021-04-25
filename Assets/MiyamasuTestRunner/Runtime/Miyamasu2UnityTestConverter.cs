using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Miyamasu
{
    /**
        generate UnityTest unit source code from miyamasu test unit.
     */
    public class Miyamasu2UnityTestConverter
    {
        public static string GenerateRuntimeTests()
        {
            var targetTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(MiyamasuTestRunner).IsAssignableFrom(t)).ToArray();
            var classDescs = new List<TestEntryClass>();

            foreach (var targetType in targetTypes)
            {
                var allMethods = targetType.GetMethods();
                var setup = allMethods.Where(methods => 0 < methods.GetCustomAttributes(typeof(MSetupAttribute), false).Length).ToArray();
                var teardown = allMethods.Where(methods => 0 < methods.GetCustomAttributes(typeof(MTeardownAttribute), false).Length).ToArray();

                var testMethods = allMethods
                    .Where(methods => 0 < methods.GetCustomAttributes(typeof(MTestAttribute), false).Length)
                    .ToArray();

                if (!testMethods.Any())
                {
                    continue;
                }

                var genTargetTestEntryClass = new TestEntryClass(
                    targetType.Name,
                    setup.FirstOrDefault(),
                    teardown.FirstOrDefault(),
                    testMethods.Select(m => new TestMethod(m.Name, m.ReturnType)).ToArray());
                classDescs.Add(genTargetTestEntryClass);
            }

            var totalClassDesc = RegenerateEntryClasses(classDescs);
            return totalClassDesc;
        }

        private static string RegenerateEntryClasses(List<TestEntryClass> classes)
        {
            var totalClassDesc = string.Empty;

            totalClassDesc += @"
using UnityEngine.TestTools;
using System;
using System.Collections;";

            var methodDesc = @"
    [UnityTest] public IEnumerator ";

            foreach (var klass in classes)
            {
                var classDesc = @"
public class " + klass.className + @"_Miyamasu {";

                /*
                    add method description.
                    setup -> method() -> teardown
                */
                foreach (var method in klass.methods)
                {
                    classDesc += methodDesc + method.name + @"() {
        var rec = new Miyamasu.Recorder(" + "\"" + klass.className + "\", \"" + method.name + "\"" + @");
        var instance = new " + klass.className + @"();
        instance.rec = rec;

        " + SetupDesc(klass.setupMethod) + @"
        
        " + MethodDesc(method) + @"
        rec.MarkAsPassed();

        " + TeardownDesc(klass.teardownMethod) + @"
    }";
                }
                classDesc += @"
}";

                totalClassDesc += classDesc;
            }

            return totalClassDesc;
        }

        private static string MethodDesc(TestMethod method)
        {
            var name = method.name;
            var type = method.returnType;

            if (type == typeof(IEnumerator))
            {
                return @"yield return instance." + method.name + @"();";
            }

            return @"instance." + name + @"(); yield return null;";
        }

        private static string SetupDesc(SetupMethod setup)
        {
            if (setup == null)
            {
                return string.Empty;
            }

            var name = setup.name;
            var returnType = setup.returnType;

            if (returnType == typeof(IEnumerator))
            {
                return @"
        yield return instance." + name + @"();";
            }

            // ret type is void.
            return @"
        try {
            instance." + name + @"();
        } catch (Exception e) {
            rec.SetupFailed(e);
            throw;
        }";
        }

        private static string TeardownDesc(TeardownMethod teardown)
        {
            if (teardown == null)
            {
                return string.Empty;
            }

            var name = teardown.name;
            var returnType = teardown.returnType;


            if (returnType == typeof(IEnumerator))
            {
                return @"
        yield return instance." + name + @"();";
            }

            // ret type is void.
            return @"
        try {
            instance." + name + @"();
        } catch (Exception e) {
            rec.TeardownFailed(e);
            throw;
        }";
        }

        public class TestEntryClass
        {
            public readonly string className;
            public readonly SetupMethod setupMethod;
            public readonly TeardownMethod teardownMethod;
            public readonly TestMethod[] methods;

            public TestEntryClass(string className, MethodInfo setupMethod, MethodInfo teardownMethod, TestMethod[] methods)
            {
                this.className = className;

                if (setupMethod != null)
                {
                    this.setupMethod = new SetupMethod(setupMethod.Name, setupMethod.ReturnType);
                }

                if (teardownMethod != null)
                {
                    this.teardownMethod = new TeardownMethod(teardownMethod.Name, teardownMethod.ReturnType);
                }

                this.methods = methods;
            }
        }

        public class TestMethod
        {
            public readonly string name;
            public readonly Type returnType;
            public TestMethod(string name, Type returnType)
            {
                this.name = name;
                this.returnType = returnType;
            }
        }

        public class SetupMethod
        {
            public readonly string name;
            public readonly Type returnType;
            public SetupMethod(string name, Type returnType)
            {
                this.name = name;
                this.returnType = returnType;
            }
        }
        public class TeardownMethod
        {
            public readonly string name;
            public readonly Type returnType;
            public TeardownMethod(string name, Type returnType)
            {
                this.name = name;
                this.returnType = returnType;
            }
        }
    }
}