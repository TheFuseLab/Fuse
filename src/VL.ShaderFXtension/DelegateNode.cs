﻿using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Extensions;

namespace VL.ShaderFXtension
{
    class DelegateUtil
    {

        public delegate void BuildInputData(StringBuilder theBuilder, KeyValuePair<string,AbstractGpuValue> theInput);
        
        internal static string BuildInput(IDictionary<string,AbstractGpuValue> inputs, BuildInputData theInputBuilder)
        {
            var stringBuilder = new StringBuilder();
            inputs.ForEach(input => { theInputBuilder(stringBuilder, input); });
            if(stringBuilder.Length > 2)stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
        internal static string BuildArguments(IDictionary<string,AbstractGpuValue> inputs)
        {
            return BuildInput(inputs, (stringBuilder, input) =>
            {
                stringBuilder.Append(TypeHelpers.GetType(input.Value).ToLower() + " ");
                stringBuilder.Append(input.Key);
                stringBuilder.Append(", ");
            });
        }
        internal static string BuildArgumentsWithId(IDictionary<string,AbstractGpuValue> inputs)
        {
            return BuildInput(inputs, (stringBuilder, input) =>
            {
                stringBuilder.Append(TypeHelpers.GetType(input.Value).ToLower() + " ");
                stringBuilder.Append(input.Value.ID);
                stringBuilder.Append(", ");
            });
        }

        internal static string BuildCallWithId(IDictionary<string,AbstractGpuValue> inputs)
        {
            return BuildInput(inputs, (stringBuilder, input) =>
            {
                stringBuilder.Append(input.Value.ID);
                stringBuilder.Append(", ");
            });
        }
        
        internal static string BuildCall(IDictionary<string,AbstractGpuValue> inputs)
        {
            return BuildInput(inputs, (stringBuilder, input) =>
            {
                stringBuilder.Append(input.Key);
                stringBuilder.Append(", ");
            });
        }
    }
    
    public class DelegateValue<T> : GpuValue<T>
    {

        public DelegateValue(string theName) : base(theName)
        {
        }

        public override string ID => name;

    }
    
    public class DelegateParameter<T> : ShaderNode<T>
    {

        public DelegateParameter(string theName): base(theName, null,"delegate")
        {
            Output = new DelegateValue<T>(theName);
        }


        public virtual string TypeName()
        {
            return TypeHelpers.GetNameForType<T>().ToLower();
        }

       
    }

    public class DelegateNode<T> : ShaderNode<T> 
    {
        public DelegateNode(AbstractGpuValue theDelegate, OrderedDictionary<string,AbstractGpuValue> theParameters, bool theUseInTemplate, ConstantValue<T> theDefault = null) : base("Delegate", theDefault)
        {
            FunctionName = "delegateFunction" + GetHashCode();
            
            Functions = new Dictionary<string, string>();

            var functionValueMap = new Dictionary<string, string>
            {
                {"resultType", TypeHelpers.GetNameForType<T>().ToLower()},
                {"functionName", FunctionName},
                {"arguments", DelegateUtil.BuildArguments(theParameters)},
                {"functionImplementation", theDelegate.ParentNode.SourceCode()},
                {"result", theDelegate.ID}
            };

            const string functionCode = @"${resultType} ${functionName}(${arguments}){
${functionImplementation}
    return ${result};
}";
            Functions.Add(FunctionName, ShaderTemplateEvaluator.Evaluate(functionCode, functionValueMap) + Environment.NewLine);
            
            var shaderCode = (!theUseInTemplate ? "${resultType} ${resultName}" : "") + " = ${functionName}(${arguments});";
            var valueMap = new Dictionary<string, string>
            {
                {"functionName", FunctionName},
                {"arguments", DelegateUtil.BuildCallWithId(theParameters)},
            };
            Setup(shaderCode, theParameters, valueMap);
        }
        
        public string FunctionName { get;  }
        
        public sealed override IDictionary<string, string> Functions { get; }
    }
    
    public class Delegate1Node<TIn, TOut> : ShaderNode<TOut> 
    {
        public delegate GpuValue<TOut> Delegate1(GpuValue<TIn> theArg1);
        
        public Delegate1Node(Delegate1 theDelegate, GpuValue<TIn> theParameter, bool theUseInTemplate, ConstantValue<TOut> theDefault = null) : base("Delegate", theDefault)
        {
            var inputs = new OrderedDictionary<string, AbstractGpuValue>()
            {
                {"in0", theParameter}
            };
            
            var functionName = "delegateFunction" + GetHashCode();
            
            Functions = new Dictionary<string, string>();
            var delegateNode = theDelegate(theParameter);
            var functionValueMap = new Dictionary<string, string>
            {
                {"resultType", TypeHelpers.GetNameForType<TOut>().ToLower()},
                {"functionName", functionName},
                {"arguments", DelegateUtil.BuildArgumentsWithId(inputs)},
                {"functionImplementation", delegateNode.ParentNode.SourceCode()},
                {"result", delegateNode.ID}
            };

            const string functionCode = @"${resultType} ${functionName}(${arguments}){
${functionImplementation}
    return ${result};
}";
            Functions.Add(functionName, ShaderTemplateEvaluator.Evaluate(functionCode, functionValueMap) + Environment.NewLine);

            var shaderCode = !theUseInTemplate ? "${resultType} ${resultName}" : "" + " = ${functionName}(${arguments});";
            var valueMap = new Dictionary<string, string>
            {
                {"functionName", functionName},
                {"arguments", DelegateUtil.BuildCallWithId(inputs)},
            };
            Setup(shaderCode, inputs, valueMap);
        }
        
        public sealed override IDictionary<string, string> Functions { get; }
    }
}