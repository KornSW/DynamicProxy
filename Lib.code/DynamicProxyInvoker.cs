using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Reflection.Dynamic {

  public class DynamicProxyInvoker : IDynamicProxyInvoker {

    private Dictionary<string, Func<object[], object>> _MethodsPerSignature = new Dictionary<string, Func<object[], object>>();

    public void DefineMethod(string methodName, Action method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke();
          return null;
        }
      );

    }

    public void DefineMethod<TArg1>(string methodName, Action<TArg1> method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke((TArg1)args[0]);
          return null;
        }
      );
    }

    public void DefineMethod<TArg1, TArg2>(string methodName, Action<TArg1, TArg2> method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke((TArg1)args[0], (TArg2)args[1]);
          return null;
        }
      );

    }

    public void DefineMethod<TResult>(string methodName, Func<TResult> method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          return method.Invoke();
        }
      );
    }

    public void DefineMethod<TArg1, TResult>(string methodName, Func<TArg1, TResult> method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          return method.Invoke((TArg1)args[0]);
        }
      );
    }

    public void DefineMethod<TArg1, TArg2, TResult>(string methodName, Func<TArg1, TArg2, TResult> method) {
      var signature = method.Method.ToString().Replace(method.Method.Name + "(", methodName + "(");
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          return method.Invoke((TArg1)args[0], (TArg2)args[1]);
        }
      );
    }

    public void DefineMethod(string methodName, Type[] paramTypes, Action<object[]> method) {
      var signature = "Void " + methodName + "(" + String.Join(", ", paramTypes.Select((p) => p.ToString()).ToArray()) + ")";
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke(args);
          return null;
        }
      );
    }

    public void DefineMethod(string methodName, ParameterInfo[] paramTypes, Action<object[]> method) {
      var signature = "Void " + methodName + "(" + String.Join(", ", paramTypes.Select((p) => p.ParameterType.ToString()).ToArray()) + ")";
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke(args);
          return null;
        }
      );
    }

    public void DefineMethod(MethodInfo methodInfo, Action<object[]> method) {
      var signature = methodInfo.ToString();
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          method.Invoke(args);
          return null;
        }
      );
    }


    public void DefineMethod(string methodName, Type[] paramTypes, Type returnType, Func<object[], object> method) {
      var signature = returnType.ToString() + " " + methodName + "(" + String.Join(", ", paramTypes.Select((p) => p.ToString()).ToArray()) + ")";
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          var result = method.Invoke(args);
          return result;
        }
      );
    }

    public void DefineMethod(string methodName, ParameterInfo[] paramTypes, Type returnType, Func<object[], object> method) {
      var signature = returnType.ToString() + " " + methodName + "(" + String.Join(", ", paramTypes.Select((p) => p.ParameterType.ToString()).ToArray()) + ")";
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          var result = method.Invoke(args);
          return result;
        }
      );
    }

    public void DefineMethod(MethodInfo methodInfo, Func<object[], object> method) {
      var signature = methodInfo.ToString();
      _MethodsPerSignature.Add(
        signature,
        (object[] args) => {
          var result = method.Invoke(args);
          return result;
        }
      );
    }

    public object InvokeMethod(string methodName, object[] arguments, string[] argumentNames, string methodSignatureString) {

      Func<object[], object> handle = null;
      lock (_MethodsPerSignature){
        _MethodsPerSignature.TryGetValue(methodSignatureString, out handle);
      }

      if(handle != null) {
        return handle.Invoke(arguments);
      }
      else {
        return this.FallbackInvokeMethod.Invoke(methodName, arguments, argumentNames, methodSignatureString);
      }

    }

    public delegate object FallbackInvokeMethodDelegate(string methodName, object[] arguments, string[] argumentNames, string methodSignatureString);

    public FallbackInvokeMethodDelegate FallbackInvokeMethod { get; set; } =
      (m, a, n, sig) => throw new NotImplementedException($"There is no Implementation for Method {sig}!");

  }

}