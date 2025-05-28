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
        Assert.AreEqual("There is no Implementation for Method System.String Foo(Int32, System.String)!", ex.Message );
      }
    }

    [TestMethod]
    public void DefaultWithCustomFallback() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();

      target.FallbackInvokeMethod = (method, args, names, signature) => {
        Assert.AreEqual("Foo", method);
        Assert.AreEqual("booooo", args[1]);
        return "hihi";
      };

      IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
      string result = proxy1.Foo(0, "booooo");

      Assert.AreEqual("hihi", result);
    }

    [TestMethod]
    public void RefParamTest() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();
     // target.DefineMethod(nameof(RefParamTest), this.RefParamTest);
      MethodInfo mi = this.GetType().GetMethod(nameof(RefParamTestCore));
      target.DefineMethod(mi, "RefParamTest", this);

      IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
      int refVt = 523;
      string refStr = "ddd";
      object refObj = null;
       
      proxy1.RefParamTest(ref refVt,ref refStr,ref refObj);

      Assert.AreEqual(444, refVt);
      Assert.AreEqual("cool", refStr);
      Assert.AreEqual("1.0", (refObj as Version).ToString(2));

    }

    public void RefParamTestCore(ref int refVt, ref string refStr, ref object refObj) {
      Assert.AreEqual(523, refVt);
      Assert.AreEqual("ddd", refStr);
      refVt = 444;
      refStr = "cool";
      refObj = new Version(1, 0);
    }


    [TestMethod]
    public void OutParamTest() {

      DynamicProxyInvoker target = new DynamicProxyInvoker();
      MethodInfo mi = this.GetType().GetMethod(nameof(OutParamTestCore));
      target.DefineMethod(mi, "OutParamTest", this);

      IDemoService proxy1 = DynamicProxy.CreateInstance<IDemoService>(target);
      int refVt = 523;
      string refStr = null;
      object refObj = null;

      proxy1.OutParamTest(out refVt, out refStr, out refObj);

      Assert.AreEqual(444, refVt);
      Assert.AreEqual("cool", refStr);
      Assert.AreEqual("1.0", (refObj as Version).ToString(2));

    }

    public void OutParamTestCore(out int refVt, out string refStr, out object refObj) {
      refVt = 444;
      refStr = "cool";
      refObj = new Version(1, 0);
    }

    public void SampleRefParam(ref int refVt, ref string refStr, ref object refObj) {
      object[] args = new object[] { null, null, null };

      args[0] = refVt;
      args[1] = refStr;
      args[2] = refObj;

      refVt =  (int)args[0];
      refStr = args[1] as string;
      refObj = args[2];
    }
    public void SampleOutParam(out int refVt, out string refStr, out object refObj) {
      object[] args = new object[] { null, null, null };

      refVt = (int)args[0];
      refStr = args[1] as string;
      refObj = args[2];

    }

  }

  public interface IDemoService {

    string Foo(int bar, string suffix);

    void RefParamTest(ref int refVt, ref string refStr, ref object refObj);
    void OutParamTest(out int refVt, out string refStr, out object refObj);
  }

  public class AnotherServiceToHijack {
    public virtual string GetMyName() {
      return "Einstein";
    }

  }

}
