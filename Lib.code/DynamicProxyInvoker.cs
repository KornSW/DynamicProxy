using System;
using System.Collections.Generic;

namespace System.Reflection.Dynamic {

	public class DynamicProxyInvoker : IDynamicProxyInvoker {

		Dictionary<string, Dictionary<Type[], Func<object[], object>>> methodsPerNameAndSignature = new Dictionary<string, Dictionary<Type[], Func<object[], object>>>();
		public void DefineMethod(string methodName, Action method) {
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] {},
				(object[] args) => {
				  method.Invoke();
				  return null;
		 	  }
		  );
		}

		public void DefineMethod<TArg1>(string methodName, Action<TArg1> method) {
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] { typeof(TArg1) },
				(object[] args) => {
				  method.Invoke((TArg1)args[0]);
				  return null;
			  }
		  );
		}

		public void DefineMethod<TArg1, TArg2>(string methodName, Action<TArg1, TArg2> method) {
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] {typeof(TArg1), typeof(TArg2) },
				(object[] args) => {
					method.Invoke((TArg1)args[0], (TArg2)args[1]);
					return null;
				}
			);
		}

		public void DefineMethod<TResult>(string methodName, Func<TResult> method) {
			var signature = methodName + "()";
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] {},
				(object[] args) => {
					return method.Invoke();
				}
			);
		}

		public void DefineMethod<TArg1, TResult>(string methodName, Func<TArg1, TResult> method) {
			string[] argTypeNames = { typeof(TArg1).Name };
			var signature = methodName + "(" + string.Join(",", argTypeNames) + ")";
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] { typeof(TArg1) },
				(object[] args) => {
					return method.Invoke((TArg1)args[0]);
				}
			);
		}

		public void DefineMethod<TArg1, TArg2, TResult>(string methodName, Func<TArg1, TArg2, TResult> method) {
			if ((!methodsPerNameAndSignature.ContainsKey(methodName))) {
				methodsPerNameAndSignature.Add(methodName, new Dictionary<Type[], Func<object[], object>>());
			}
			methodsPerNameAndSignature[methodName].Add(
				new Type[] { typeof(TArg1), typeof(TArg2)	},
				(object[] args) => {
					return method.Invoke((TArg1)args[0], (TArg2)args[1]);
				}
			);
		}

		public object InvokeMethod(string methodName, object[] arguments, string[] argumentNames) {

			if ((methodsPerNameAndSignature.ContainsKey(methodName))) {
				var signatures = methodsPerNameAndSignature[methodName];
				foreach (Type[] signature in signatures.Keys) {
					if ((signature.Length == arguments.Length)) {
						bool match = true;
						for (int i = 0; i <= arguments.Length - 1; i++) {
							if ((arguments[i] != null && !signature[i].IsAssignableFrom(arguments[i].GetType()))) {
								match = false;
							}
						}
						if ((match)) {
							return signatures[signature].Invoke(arguments);
						}
					}
				}
			}

			return this.FallbackInvokeMethod.Invoke(methodName, arguments, argumentNames);
		}

		public delegate object FallbackInvokeMethodDelegate(string methodName, object[] arguments, string[] argumentNames);

		public FallbackInvokeMethodDelegate FallbackInvokeMethod { get; set; } =
			(m, a, n) => throw new NotImplementedException($"There is no Implementation for Method {m}({String.Join(", ", n)})");

	}

}