using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Reflection.Dynamic {

  [TestClass]
  public class DynamicProxyTests {

    [TestMethod]
    public void DynamicImplementationOfInterface() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();
      target.DefineMethod("Foo",
        (int bar, string suffix) => {
          return bar.ToString() + " " + suffix;
        }
      );

      IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
      string result1 = proxy1.Foo(123, "Hello");
      Assert.AreEqual("123 Hello", result1);

    }

    [TestMethod]
    public void DynamicSpecializationOfClass() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();
      target.DefineMethod("GetMyName",
        () => {
          return "Newton";
        }
      );

      AnotherServiceToHijack proxy2 = DynamicProxy.CreateInstance<AnotherServiceToHijack>(target);
      string result2 = proxy2.GetMyName();
      Assert.AreEqual("Newton", result2);

    }

    [TestMethod]
    public void DefaultThrowsException() {
      try {

        DynamicProxyInvoker target = new DynamicProxyInvoker();
        IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
        proxy1.Foo(0, "");

        Assert.Fail("a NotImplementedException should have been thrown, but didnt!");
      }
      catch (NotImplementedException ex) {
        Assert.AreEqual("There is no Implementation for Method Foo(bar, suffix)", ex.Message );
      }
    }

    [TestMethod]
    public void DefaultWithCustomFallback() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();

      target.FallbackInvokeMethod = (method, args, names) => {
        Assert.AreEqual("Foo", method);
        Assert.AreEqual("booooo", args[1]);
        return "hihi";
      };

      IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
      string result = proxy1.Foo(0, "booooo");

      Assert.AreEqual("hihi", result);
    }

  }

  public interface IDemoService {

    string Foo(int bar, string suffix);

  }

  public class AnotherServiceToHijack {
    public virtual string GetMyName() {
      return "Einstein";
    }
  }

}
