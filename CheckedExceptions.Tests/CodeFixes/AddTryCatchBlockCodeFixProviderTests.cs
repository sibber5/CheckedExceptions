namespace Sundstrom.CheckedExceptions.Tests.CodeFixes;

using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;
using Xunit.Abstractions;

using Verifier = CSharpCodeFixVerifier<CheckedExceptionsAnalyzer, AddTryCatchBlockCodeFixProvider, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

public class AddTryCatchBlockCodeFixProviderTests
{
    [Fact]
    public async Task AddTryCatch_ToMethod_WhenUnhandledExceptionThrown()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Should trigger THROW001
            throw new ArgumentException();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Should trigger THROW001
                throw new ArgumentException();
            }
            catch (ArgumentException argumentException)
            {
            }
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("ArgumentException")
            .WithSpan(10, 13, 10, 43);

        await Verifier.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
    }

    [Fact]
    public async Task WhenExceptionTypeIsException_VariableName_ShouldBe_Ex()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Should trigger THROW001
            throw new Exception();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Should trigger THROW001
                throw new Exception();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("Exception")
            .WithSpan(10, 13, 10, 35);

        await Verifier.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
    }

    [Fact]
    public async Task AddTryCatch_ToMethod_WhenUnhandledException()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Should trigger THROW001
            DoSomething();
        }

        [Throws(typeof(InvalidOperationException))]
        public void DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Should trigger THROW001
                DoSomething();
            }
            catch (InvalidOperationException invalidOperationException)
            {
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public void DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
            .WithSpan(10, 13, 10, 26);

        await Verifier.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
    }

    [Fact]
    public async Task AddTryCatch_ToMethod_WhenUnhandledException1()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // Should trigger THROW001
            var x = DoSomething();
            x = x + 1;
        }

        [Throws(typeof(InvalidOperationException))]
        public int DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                // Should trigger THROW001
                var x = DoSomething();
                x = x + 1;
            }
            catch (InvalidOperationException invalidOperationException)
            {
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public int DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
            .WithSpan(10, 21, 10, 34);

        await Verifier.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode, 1);
    }

    [Fact]
    public async Task AddTryCatch_ToMethod_WhenUnhandledException3()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                DoSomething();
            }
            catch (InvalidOperationException ex)
            {
                // Should trigger THROW001
                DoSomething();
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public void DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                DoSomething();
            }
            catch (InvalidOperationException ex)
            {
                try
                {
                    // Should trigger THROW001
                    DoSomething();
                }
                catch (InvalidOperationException invalidOperationException)
                {
                }
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public void DoSomething()
        {
            throw new InvalidOperationException();
        }
    }
}
""";

        var expectedDiagnostic1 = Verifier.UnhandledException("InvalidOperationException")
            .WithSpan(16, 17, 16, 30);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic1], fixedCode, 1);
    }

    [Fact]
    public async Task AddTryCatch_Should_IncludeVariablesInScope()
    {
        var testCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            string str = "";
            int x = 0;
            double d = 2;
            var result = Foo(x);
            #pragma warning disable THROW001 // Unhandled exception
            Console.WriteLine(result);
            #pragma warning restore THROW001
            char ch = 'a';
        }

        [Throws(typeof(ArgumentException))]
        int Foo(int arg) 
        {
            throw new ArgumentException();
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            string str = "";
            try
            {
                int x = 0;
                double d = 2;
                var result = Foo(x);
#pragma warning disable THROW001 // Unhandled exception
                Console.WriteLine(result);
            }
            catch (ArgumentException argumentException)
            {
            }
#pragma warning restore THROW001
            char ch = 'a';
        }

        [Throws(typeof(ArgumentException))]
        int Foo(int arg) 
        {
            throw new ArgumentException();
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("ArgumentException")
            .WithSpan(12, 26, 12, 32);

        await Verifier.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
    }

    // ... ///

    [Fact]
    public async Task ExpressionBody_InLambda_PromoteToBlockBody()
    {
        var testCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                For(x => Test(x));
            }
            catch {}
        }

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }

        public void For(Func<int, bool> f)
        {
            
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                For(x => { try { return Test(x); } catch (InvalidOperationException invalidOperationException) { } });
            }
            catch {}
        }

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }

        public void For(Func<int, bool> f)
        {
            
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
             .WithSpan(12, 26, 12, 33);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic], fixedCode, setup: t => t.CompilerDiagnostics = CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ExpressionBody_InMethod_PromoteToBlockBody()
    {
        var testCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() => Test(42);

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            try
            {
                return Test(42);
            }
            catch (InvalidOperationException invalidOperationException)
            {
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
             .WithSpan(8, 37, 8, 45);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic], fixedCode, setup: t => t.CompilerDiagnostics = CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ExpressionBody_InLocalFunction_PromoteToBlockBody()
    {
        var testCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void Foo() 
        {
            void TestMethod() => Test(42);
        }

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void Foo() 
        {
            void TestMethod() { try { return Test(42); } catch (InvalidOperationException invalidOperationException) { } }
        }

        [Throws(typeof(InvalidOperationException))]
        public bool Test(int x) 
        {
            return true;
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
             .WithSpan(10, 34, 10, 42);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic], fixedCode, setup: t => t.CompilerDiagnostics = CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ExpressionBody_InPropertyDecl_PromoteToBlockBody()
    {
        var testCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public int TestProp => Test(42);

        [Throws(typeof(InvalidOperationException))]
        public int Test(int x) 
        {
            return 0;
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public int TestProp
        {
            get
            {
                try
                {
                    return Test(42);
                }
                catch (InvalidOperationException invalidOperationException)
                {
                }
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public int Test(int x) 
        {
            return 0;
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
             .WithSpan(8, 32, 8, 40);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic], fixedCode, setup: t => t.CompilerDiagnostics = CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ExpressionBody_InAccessorDecl_PromoteToBlockBody()
    {
        var testCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public int TestProp
        {
            get => Test(42);
        }

        [Throws(typeof(InvalidOperationException))]
        public int Test(int x) 
        {
            return 0;
        }
    }
}
""";

        var fixedCode = /* lang=c#-test */  """
using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public int TestProp
        {
            get
            {
                try
                {
                    return Test(42);
                }
                catch (InvalidOperationException invalidOperationException)
                {
                }
            }
        }

        [Throws(typeof(InvalidOperationException))]
        public int Test(int x) 
        {
            return 0;
        }
    }
}
""";

        var expectedDiagnostic = Verifier.UnhandledException("InvalidOperationException")
             .WithSpan(10, 20, 10, 28);

        await Verifier.VerifyCodeFixAsync(testCode, [expectedDiagnostic], fixedCode, setup: t => t.CompilerDiagnostics = CompilerDiagnostics.None);
    }
}