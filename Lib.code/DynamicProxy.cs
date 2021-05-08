using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Reflection.Dynamic {

  //special thanks to:
  //https://www.codeproject.com/Tips/138388/Dynamic-Generation-of-Client-Proxy-at-Runtime-in
  //https://www.codeproject.com/Articles/121568/Dynamic-Type-Using-Reflection-Emit

  public abstract class DynamicProxy {

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]

    private IDynamicProxyInvoker _Invoker;

    public static TApplicable CreateInstance<TApplicable>(params object[] constructorArgs) {
      return CreateInstance<TApplicable>(new DynamicProxyInvoker(), constructorArgs);
    }

    public static TApplicable CreateInstance<TApplicable>(Action<DynamicProxyInvoker> implementationBuildingMethod, params object[] constructorArgs) {
      DynamicProxyInvoker invoker = new DynamicProxyInvoker();
      implementationBuildingMethod.Invoke(invoker);
      return CreateInstance<TApplicable>(invoker, constructorArgs);
    }

    public static object CreateInstance(Type applicableType, Action<DynamicProxyInvoker> implementationBuildingMethod, params object[] constructorArgs) {
      DynamicProxyInvoker invoker = new DynamicProxyInvoker();
      implementationBuildingMethod.Invoke(invoker);
      return CreateInstance(applicableType, invoker, constructorArgs);
    }

    public static TApplicable CreateInstance<TApplicable>(IDynamicProxyInvoker invoker, params object[] constructorArgs) {
      return (TApplicable)CreateInstance(typeof(TApplicable), invoker, constructorArgs);
    }

    public static object CreateInstance(Type applicableType, IDynamicProxyInvoker invoker, params object[] constructorArgs) {
      Type dynamicType = BuildDynamicType(applicableType);
      var extendedConstructorArgs = constructorArgs.ToList();
      extendedConstructorArgs.Add(invoker);
      var instance = Activator.CreateInstance(dynamicType, extendedConstructorArgs.ToArray());
      return instance;
    }

    public static Type BuildDynamicType<TApplicable>() {
      return BuildDynamicType(typeof(TApplicable));
    }

    public static Type BuildDynamicType(Type applicableType) {

      //TODO: TYP CACHEN!!!!!!!

      Type iDynamicProxyInvokerType = typeof(IDynamicProxyInvoker);
      MethodInfo iDynamicProxyInvokerTypeInvokeMethod = iDynamicProxyInvokerType.GetMethod("InvokeMethod");

      AssemblyBuilder assemblyBuilder = null;
      Type baseType = null;
      if ((applicableType.IsClass)) {
        baseType = applicableType;
      }

      //##### ASSEMBLY & MODULE DEFINITION #####

      var assemblyName = new AssemblyName(applicableType.Name + "_DyamicProxyClass_Assembly");

#if NET46
      assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#endif
#if NET5
      assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif

      var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

      // ##### CLASS DEFINITION #####

      TypeBuilder typeBuilder;
      if (baseType is object) {
        typeBuilder = moduleBuilder.DefineType(applicableType.Name + "_DyamicProxyClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);
      }
      // CODE: Public Class <MyApplicableType>_DyamicProxyClass
      // Inherits <MyApplicableType>
      else {
        typeBuilder = moduleBuilder.DefineType(applicableType.Name + "_DyamicProxyClass", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout);
        typeBuilder.AddInterfaceImplementation(applicableType);
        // CODE: Public Class <MyApplicableType>_DyamicProxyClass
        // Implements <MyApplicableType>
      }

      // ##### FIELD DEFINITIONs #####

      var fieldBuilderDynamicProxyInvoker = typeBuilder.DefineField("_DynamicProxyInvoker", iDynamicProxyInvokerType, FieldAttributes.Private);

      // ##### CONSTRUCTOR DEFINITIONs #####

      if (baseType is object) {

        // create a proxy for each constructor in the base class
        foreach (var constructorOnBase in baseType.GetConstructors()) {
          var constructorArgs = new List<Type>();
          foreach (var p in constructorOnBase.GetParameters())
            constructorArgs.Add(p.ParameterType);
          constructorArgs.Add(typeof(IDynamicProxyInvoker));
          var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, constructorArgs.ToArray());
          // CODE: Public Sub New([...],dynamicProxyInvoker As IDynamicProxyInvoker)

          // Dim dynamicProxyInvokerCParam = constructorBuilder.DefineParameter(constructorArgs.Count, ParameterAttributes.In, "dynmaicProxyInvoker")

          {
            var withBlock = constructorBuilder.GetILGenerator();
            withBlock.Emit(OpCodes.Nop); // ------------------
            withBlock.Emit(OpCodes.Ldarg, 0); // load Argument(0) (which is a pointer to the instance of our class)
            for (int i = 1, loopTo = constructorArgs.Count - 1; i <= loopTo; i++)
              withBlock.Emit(OpCodes.Ldarg, (byte)i); // load the other Arguments (Constructor-Params) excluding the last one
            withBlock.Emit(OpCodes.Call, constructorOnBase); // CODE: MyBase.New([...])
            withBlock.Emit(OpCodes.Nop); // ------------------
            withBlock.Emit(OpCodes.Ldarg, 0); // load Argument(0) (which is a pointer to the instance of our class)
            byte argIndex = (byte)constructorArgs.Count;
            // TODO: prüfen ob valutype!!!!! <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            // .Emit(OpCodes.Ldarg, argIndex) 'load the last Argument (Constructor-Param: IDynamicProxyInvoker)
            withBlock.Emit(OpCodes.Ldarg_S, argIndex); // load the last Argument (Constructor-Param: IDynamicProxyInvoker)
            withBlock.Emit(OpCodes.Stfld, fieldBuilderDynamicProxyInvoker); // CODE: _DynamicProxyInvoker = dynamicProxyInvoker
            withBlock.Emit(OpCodes.Nop);
            withBlock.Emit(OpCodes.Ret); // ------------------
          }
        }
      }
      else // THIS IS WHEN WERE IMPLEMENTING AN INTERFACE INSTEAD OF INHERITING A CLASS
      {
        var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.HasThis, new[] { typeof(IDynamicProxyInvoker) });

        // CODE: Public Sub New(dynamicProxyInvoker As IDynamicProxyInvoker)

        {
          var withBlock1 = constructorBuilder.GetILGenerator();
          withBlock1.Emit(OpCodes.Nop); // ------------------
          withBlock1.Emit(OpCodes.Ldarg, 0); // load Argument(0) (which is a pointer to the instance of our class)
          withBlock1.Emit(OpCodes.Ldarg, 1); // load the Argument (Constructor-Param: IDynamicProxyInvoker)
          withBlock1.Emit(OpCodes.Stfld, fieldBuilderDynamicProxyInvoker); // CODE: _DynamicProxyInvoker = dynamicProxyInvoker
          withBlock1.Emit(OpCodes.Ret); // ------------------
        }
      }

      // ##### METHOD DEFINITIONs #####

      foreach (var mi in applicableType.GetMethods()) {
        var methodNameBlacklist = new[] { "ToString", "GetHashCode", "GetType", "Equals" };
        if (!mi.IsSpecialName && !methodNameBlacklist.Contains(mi.Name)) {
          bool isOverridable = !mi.Attributes.HasFlag(MethodAttributes.Final);
          if (mi.IsPublic && (baseType is null || isOverridable)) {
            var paramTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
            var paramNames = mi.GetParameters().Select(p => p.Name).ToArray();
            var paramEvalIsValueType = mi.GetParameters().Select(p => p.ParameterType.IsValueType).ToArray();
            var paramEvalIsByRef = mi.GetParameters().Select(p => p.IsOut).ToArray();
            var methodBuilder = typeBuilder.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig | MethodAttributes.Virtual, mi.ReturnType, paramTypes);
            var paramBuilders = new ParameterBuilder[paramNames.Length];
            for (int paramIndex = 0, loopTo1 = paramNames.Length - 1; paramIndex <= loopTo1; paramIndex++) {
              if (paramEvalIsByRef[paramIndex]) {
                paramBuilders[paramIndex] = methodBuilder.DefineParameter(paramIndex + 1, ParameterAttributes.Out, paramNames[paramIndex]);
              }
              else {
                paramBuilders[paramIndex] = methodBuilder.DefineParameter(paramIndex + 1, ParameterAttributes.In, paramNames[paramIndex]);
              }

              // TODO: optionale parameter

            }

            {
              var withBlock2 = methodBuilder.GetILGenerator();

              // ##### LOCAL VARIABLE DEFINITIONs #####

              LocalBuilder localReturnValue = null;
              if (mi.ReturnType is object && !(mi.ReturnType.Name == "Void")) {
                localReturnValue = withBlock2.DeclareLocal(mi.ReturnType);
              }

              var argumentRedirectionArray = withBlock2.DeclareLocal(typeof(object[]));
              var argumentNameArray = withBlock2.DeclareLocal(typeof(string[]));
              withBlock2.Emit(OpCodes.Nop); // ------------------------------------------------------------------------

              // ARRAY-INSTANZIIEREN
              withBlock2.Emit(OpCodes.Ldc_I4_S, (byte)paramNames.Length); // CODE: Zahl x als (int32) wobei x die anzhalt der parameter unseerer methode ist
              withBlock2.Emit(OpCodes.Newarr, typeof(object)); // CODE: Dim args(x) As Object
              withBlock2.Emit(OpCodes.Stloc, argumentRedirectionArray);
              withBlock2.Emit(OpCodes.Nop); // ------------------------------------------------------------------------

              // ARRAY-INSTANZIIEREN
              withBlock2.Emit(OpCodes.Ldc_I4_S, (byte)paramNames.Length); // CODE: Zahl x als (int32) wobei x die anzhalt der parameter unseerer methode ist
              withBlock2.Emit(OpCodes.Newarr, typeof(string)); // CODE: Dim args(x) As Object
              withBlock2.Emit(OpCodes.Stloc, argumentNameArray);

              // ------------------------------------------------------------------------

              // parameter in transport-array übertragen
              for (int paramIndex = 0, loopTo2 = paramNames.Length - 1; paramIndex <= loopTo2; paramIndex++) {
                bool paramIsValueType = paramEvalIsValueType[paramIndex];
                bool paramIsByRef = paramEvalIsByRef[paramIndex];
                var paramType = paramTypes[paramIndex];
                withBlock2.Emit(OpCodes.Ldloc, argumentRedirectionArray); // transport-array laden
                withBlock2.Emit(OpCodes.Ldc_I4_S, (byte)paramIndex); // arrayindex als integer (zwecks feld-addressierung) erzeugen
                if (paramIsByRef && paramIsValueType) {
                  withBlock2.Emit(OpCodes.Ldarga_S, paramIndex + 1); // zuzuweisendes methoden-argument auf den stack holen
                }
                else {
                  withBlock2.Emit(OpCodes.Ldarg, paramIndex + 1);
                } // zuzuweisendes methoden-argument auf den stack holen

                if (paramIsByRef) {
                  // resolve incomming byref handle into a new object address
                  if (paramIsValueType) {
                    withBlock2.Emit(OpCodes.Ldobj, paramType);
                  }
                  else {
                    withBlock2.Emit(OpCodes.Ldind_Ref);
                  }
                }

                if (paramIsValueType) {
                  withBlock2.Emit(OpCodes.Box, paramType); // value-types müssen geboxed werden, weil die array-felder vom typ "object" sind
                }

                withBlock2.Emit(OpCodes.Stelem_Ref); // ins transport-array hineinschreiben

                // ------------------------------------------------------------------------

                withBlock2.Emit(OpCodes.Ldloc, argumentNameArray); // transport-array laden
                withBlock2.Emit(OpCodes.Ldc_I4_S, (byte)paramIndex); // arrayindex als integer (zwecks feld-addressierung) erzeugen
                withBlock2.Emit(OpCodes.Ldstr, paramNames[paramIndex]); // name als string bereitlegen (als array inhalt)
                withBlock2.Emit(OpCodes.Stelem_Ref); // ins transport-array hineinschreiben
              }

              withBlock2.Emit(OpCodes.Ldarg_0); // < unsere klasseninstanz auf den stack
              withBlock2.Emit(OpCodes.Ldfld, fieldBuilderDynamicProxyInvoker); // feld '_DynamicProxyInvoker' laden auf den dtack)
              withBlock2.Emit(OpCodes.Ldstr, mi.Name); // < methodenname als string auf den stack holen
              withBlock2.Emit(OpCodes.Ldloc, argumentRedirectionArray); // pufferarray auf den stack holen
              withBlock2.Emit(OpCodes.Ldloc, argumentNameArray); // pufferarray auf den stack holen

              // aufruf auf umgeleitete funktion absetzen
              withBlock2.Emit(OpCodes.Callvirt, iDynamicProxyInvokerTypeInvokeMethod); // _DynamicProxyInvoker.InvokeMethod("Foo", args)
                                                                                       // jetzt liegt ein result auf dem stack...

              if (localReturnValue is null) {
                withBlock2.Emit(OpCodes.Pop); // result (void) vom stack löschen (weil wir nix zurückgeben)
              }
              else if (mi.ReturnType.IsValueType) {
                withBlock2.Emit(OpCodes.Unbox_Any, mi.ReturnType); // value-types müssen unboxed werden, weil der retval in "object" ist
                withBlock2.Emit(OpCodes.Stloc, localReturnValue); // < speichere es in 'returnValueBuffer'
              }
              else {
                withBlock2.Emit(OpCodes.Castclass, mi.ReturnType); // reference-types müssen gecastet werden, weil der retval in "object" ist
                withBlock2.Emit(OpCodes.Stloc, localReturnValue);
              } // < speichere es in 'returnValueBuffer'

              // '#############################

              // TODO: ByRef-Parameter aus transport-array "auspacken" und zurückschreiben!!!

              // %%%%%%%%%%%%  basearg1 = DirectCast(args(0), String)
              // IL_003c: ldarg.1             < lade das erste methoden-agrument auf den stack

              // IL_003d: ldloc.1             < lade array auf den stack
              // IL_003e: ldc.i4.0            < lade integer 0 auf den stack
              // IL_003f: ldelem.ref          < hole die element-referenz aus dem array heraus

              // IL_0040: castclass [mscorlib]System.String            < directcast    Bei refernece-types

              // IL_0045: stind.ref           < auf die oben bereitgelegte adresse des byref parameters rückschreiben  OBJECT-VERSION


              // %%%%%%%%%%%%   basearg4 = DirectCast(args(3), Integer)
              // IL_0046: ldarg.s basearg4

              // IL_0048: ldloc.1             < lade array auf den stack
              // IL_0049: ldc.i4.3            < lade integer 3 auf den stack
              // IL_004a: ldelem.ref          < hole die element-referenz aus dem array heraus

              // IL_004b: unbox.any [mscorlib]System.Int32             < unbox bei value types

              // IL_0050: stind.i4             < auf die bereitgelegte adresse des byref parameters rückschreiben  INTEGER-VERSION

              // '#############################

              if (localReturnValue is object) {
                withBlock2.Emit(OpCodes.Ldloc, localReturnValue);
              }

              withBlock2.Emit(OpCodes.Ret);
            }

            // note: 'DefineMethodOverride' is also used for implementing interface-methods
            typeBuilder.DefineMethodOverride(methodBuilder, mi);
          }
        }
      }

      var dynamicType = typeBuilder.CreateType();
      // assemblyBuilder.Save("Dynassembly.dll")
      return dynamicType;
    }

    // OverridePropertiesForStandardValues(converterTypeBuilder, baseType)

    // ' Methode GetStandardValues mit "Return New StandardValuesCollection(allowedValues)" überschreiben
    // Dim methodBuilder = converterTypeBuilder.DefineMethod("GetStandardValues", MethodAttributes.Public Or MethodAttributes.ReuseSlot Or MethodAttributes.Virtual Or MethodAttributes.HideBySig)
    // methodBuilder.SetReturnType(GetType(TypeConverter.StandardValuesCollection))
    // methodBuilder.SetParameters({GetType(ITypeDescriptorContext)})
    // Dim ilGeneratorOverriding = methodBuilder.GetILGenerator
    // Dim listType = GetType(List(Of String))
    // 'lokale Variable anlegen und eine neue Instanz von List(of String) zuweisen
    // ilGeneratorOverriding.DeclareLocal(listType)
    // ilGeneratorOverriding.Emit(OpCodes.Newobj, listType.GetConstructors()(0))
    // ilGeneratorOverriding.Emit(OpCodes.Stloc_0)

    // For index = 0 To allowedValues.Length - 1
    // 'Liste befüllen
    // ilGeneratorOverriding.Emit(OpCodes.Ldloc_0)
    // ilGeneratorOverriding.Emit(OpCodes.Ldstr, allowedValues(index))
    // ilGeneratorOverriding.Emit(OpCodes.Call, listType.GetMethod("Add", {GetType(String)}))
    // Next

    // 'Neue Instanz TypeConverter.StandardValuesCollection erzeugen (List(of String) als Konstruktorparameter)
    // ilGeneratorOverriding.Emit(OpCodes.Ldloc_0)
    // ilGeneratorOverriding.Emit(OpCodes.Newobj, GetType(TypeConverter.StandardValuesCollection).GetConstructors()(0))
    // 'Und zurück (gibt das oberste Element des Stack zurück.
    // ilGeneratorOverriding.Emit(OpCodes.Ret)

    // Return converterTypeBuilder.CreateType

    // CODDE FÜR REIN:

    // Private _EventTriggers As New Dictionary(Of String, Action(Of Object()))

    // Protected Sub New(invoker As IDynamicProxyInvoker)
    // _Invoker = invoker
    // For Each e In Me.GetType().GetEvents()
    // _EventTriggers.Add(e.Name, Sub(parameters As Object()) e.RaiseMethod.Invoke(Me, parameters))
    // Next
    // End Sub

    // Public ReadOnly Property Invoker As IDynamicProxyInvoker
    // Get
    // Return _Invoker
    // End Get
    // End Property

    // Protected Function InvokeMethod(methodName As String, arguments As Object()) As Object
    // Return _Invoker.InvokeMethod(methodName, arguments)
    // End Function

    // Protected Function GetPropertyValue(propertyName As String, indexArguments As Object()) As Object
    // Return _Invoker.GetPropertyValue(propertyName, indexArguments)
    // End Function

    // Protected Sub SetPropertyValue(propertyName As String, value As Object, indexArguments As Object())
    // _Invoker.SetPropertyValue(propertyName, value, indexArguments)
    // End Sub

    // Private Sub Invoker_Raising(eventName As String, arguments() As Object) Handles _Invoker.Raise
    // Dim e As Action(Of Object())
    // SyncLock _EventTriggers
    // e = _EventTriggers(eventName)
    // End SyncLock
    // e.Invoke(arguments)
    // End Sub

  }
}