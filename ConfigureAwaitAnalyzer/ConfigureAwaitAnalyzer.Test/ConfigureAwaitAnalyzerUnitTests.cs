using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ConfigureAwaitAnalyzer;

namespace ConfigureAwaitAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void WhenNoCode_ThenNoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void WhenCompilationErrors_ThenNoDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        private static Task<int> Test()
        {
            return Task.FromResult(3);
        }

        private static async Task Run()
        {
            int a = await Test().ConfigureAwait(false);
            await Test().ConfigureAwait(false);
            await Test();
            await Test()

            await Task.Run(() => Console.ReadLine());

            var task = Test();
            var i = await task;
        }

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch
            {
            }
        }
    }
}
";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void WhenAwaitMethodInvocationWithoutConfigureAwait_ThenDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        private static Task<int> Test()
        {
            return Task.FromResult(3);
        }

        private static async Task Run()
        {
            await Test();
        }

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch
            {
            }
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = String.Format("'{0}' call maybe needs to configure an awaiter", "await Test()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                                new DiagnosticResultLocation("Test0.cs", 16, 13)
                            }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void WhenAwaitTaskRunInvocationWithoutConfigureAwait_ThenDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        private static Task<int> Test()
        {
            return Task.FromResult(3);
        }

        private static async Task Run()
        {
            await Task.Run(() => null);
        }

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch
            {
            }
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = String.Format("'{0}' call maybe needs to configure an awaiter", "await Task.Run(() => null)"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                                new DiagnosticResultLocation("Test0.cs", 16, 13)
                            }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void WhenAwaitVariableWithoutConfigureAwait_ThenDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        private static Task<int> Test()
        {
            return Task.FromResult(3);
        }

        private static async Task Run()
        {
            var task = Test();
            var i = await task;
        }

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch
            {
            }
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = String.Format("'{0}' call maybe needs to configure an awaiter", "await task"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                                new DiagnosticResultLocation("Test0.cs", 17, 21)
                            }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void WhenTwoAwaitsInSameInstructionWithoutConfigureAwait_ThenTwoDiagnostics()
        {
            var test = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        private static Task<int> Test()
        {
            return Task.FromResult(3);
        }

        private static async Task Run()
        {
            await Task.Run(async () => await Test());
        }

        static void Main()
        {
            try
            {
                Run().Wait();
            }
            catch
            {
            }
        }
    }
}
";
            var expected1 = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = String.Format("'{0}' call maybe needs to configure an awaiter", "await Task.Run(async () => await Test())"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                                new DiagnosticResultLocation("Test0.cs", 16, 13)
                            }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = String.Format("'{0}' call maybe needs to configure an awaiter", "await Test()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                                new DiagnosticResultLocation("Test0.cs", 16, 40)
                            }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);
        }

        //Diagnostic and CodeFix both triggered and checked for
        //    [TestMethod]
        //    public void TestMethod2()
        //    {
        //        var test = @"
        //using System;
        //using System.Collections.Generic;
        //using System.Linq;
        //using System.Text;
        //using System.Threading.Tasks;
        //using System.Diagnostics;

        //namespace ConsoleApplication1
        //{
        //    class TypeName
        //    {   
        //    }
        //}";
        //        var expected = new DiagnosticResult
        //        {
        //            Id = "ConfigureAwaitAnalyzer",
        //            Message = String.Format("Type name '{0}' contains lowercase letters", "TypeName"),
        //            Severity = DiagnosticSeverity.Warning,
        //            Locations =
        //                new[] {
        //                        new DiagnosticResultLocation("Test0.cs", 11, 15)
        //                    }
        //        };

        //        VerifyCSharpDiagnostic(test, expected);

        //        var fixtest = @"
        //using System;
        //using System.Collections.Generic;
        //using System.Linq;
        //using System.Text;
        //using System.Threading.Tasks;
        //using System.Diagnostics;

        //namespace ConsoleApplication1
        //{
        //    class TYPENAME
        //    {   
        //    }
        //}";
        //        VerifyCSharpFix(test, fixtest);
        //    }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConfigureAwaitAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConfigureAwaitAnalyzer();
        }
    }
}
