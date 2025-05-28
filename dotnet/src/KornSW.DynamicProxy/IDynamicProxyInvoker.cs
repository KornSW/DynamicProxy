using System;
using System.Collections.Generic;

namespace System.Reflection.Dynamic {

	public interface IDynamicProxyInvoker {

		object InvokeMethod(string methodName, object[] arguments, string[] argumentNames, string methodSignatureString);

		//Function GetPropertyValue(propertyName As String, indexArguments As Object()) As Object
		//Sub SetPropertyValue(propertyName As String, value As Object, indexArguments As Object())
		//Event Raise(eventName As String, arguments As Object())

	}

}