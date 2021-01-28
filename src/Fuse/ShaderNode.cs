﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Extensions;
using VL.Lib.Collections;

namespace Fuse
{
    public interface IShaderNode: Trees.IReadOnlyTreeNode
    {

        string ID { get;  }
    }
    
    public abstract class AbstractShaderNode : IShaderNode
    {
        protected string _sourceCode;
        protected IEnumerable<AbstractGpuValue> Ins;


        protected AbstractShaderNode(string theId)
        {
            ID = theId;
        }

        public virtual string Declaration => "";
        public virtual IDictionary<string,string> Functions => new Dictionary<string, string>();

        public virtual List<string> MixIns => new List<string>();
        
        public string ID { get; }

        public string SourceCode => _sourceCode;

        private void BuildSource(StringBuilder theSourceBuilder, HashSet<int> theHashes)
        {
            Children.ForEach(child =>
            {
                if (!(child is AbstractShaderNode input)) return;
               
                ((AbstractShaderNode)child).BuildSource(theSourceBuilder, theHashes);
            });
            if (!SourceCode.IsNullOrEmpty() && theHashes.Add(GetHashCode()))
            {
                theSourceBuilder.Append("        " + SourceCode + Environment.NewLine);
            }
        }
       
        public string BuildSourceCode()
        {
            var myStringBuilder = new StringBuilder();
            var myHashes = new HashSet<int>();

            BuildSource(myStringBuilder, myHashes);
            
            return myStringBuilder.ToString();
        }

        public string Declarations()
        {
            var result = new HashSet<AbstractShaderNode>();
            var myDeclarations = new StringBuilder();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if (!(n is AbstractShaderNode input)) return;
                if (!input.Declaration.IsNullOrEmpty() && result.Add(input))
                    myDeclarations.AppendLine(input.Declaration);
            });
            return myDeclarations.ToString();
        }

        public List<IGpuInput> Inputs()
        {
            var result = new HashSet<IGpuInput>();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if(n is IGpuInput input)result.Add(input);
            });
            return result.ToList();
        }
        
        public List<IDelegateParameter> Delegates()
        {
            var result = new HashSet<IDelegateParameter>();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if(n is IDelegateParameter input)result.Add(input);
            });
            return result.ToList();
        }

        public string BuildMixIns()
        {
            var result = new HashSet<string>();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if(n is AbstractShaderNode input)result.AddRange(input.MixIns);
            });
            
            var myBuilder = new StringBuilder();
            result.ForEach(mixin => myBuilder.Append(","+mixin));
            return myBuilder.ToString();
        }
        
        public List<string> MixinList()
        {
            var result = new List<string>();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if(n is AbstractShaderNode input)result.AddRange(input.MixIns);
            });
            return result;
        }

        public string BuildFunctions(){
       
            var result = new HashSet<string>();
            var myBuilder = new StringBuilder();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if (!(n is AbstractShaderNode input)) return;
                
                input.Functions?.ForEach(kv =>
                {
                    if (!result.Add(kv.Key)) return;
                    
                    myBuilder.Append(kv.Value);
                    myBuilder.AppendLine();
                });
                
            });
            return myBuilder.ToString();
        }
        
        public Dictionary<string,string> FunctionMap(){
       
            var result = new Dictionary<string,string>();
            Trees.ReadOnlyTreeNode.Flatten(this).ForEach(n =>
            {
                if (!(n is AbstractShaderNode input)) return;
                
                input.Functions?.ForEach(kv =>
                {
                    if (result.ContainsKey(kv.Key)) return;
                    
                    result.Add(kv.Key, kv.Value);
                });
                
            });
            return result;
        }

        public IReadOnlyList<Trees.IReadOnlyTreeNode> Children
        {
            get
            {
                var result = new List<Trees.IReadOnlyTreeNode>();
                
                Ins.ForEach(input =>
                {
                    if(input?.ParentNode != null)result.Add(input.ParentNode);
                });
                return result;
            }
        }

        
        
    }
    
    public class ShaderNode<T> : AbstractShaderNode
    {
        public  GpuValue<T> Output { get; protected set; }
        public  ConstantValue<T> Default { get; protected set; }
        
        protected const string DefaultShaderCode = "${resultType} ${resultName} = ${default};";

        protected ShaderNode(string theId, ConstantValue<T> theDefault = null,string outputName = "result") : base(theId)
        {
            Default = theDefault ?? new ConstantValue<T>(0);
            Output = new GpuValue<T>(outputName);
        }

        private Dictionary<string, string> CreateTemplateMap()
        {
            return new Dictionary<string, string>
            {
                {"resultName", Output.ID},
                {"resultType", TypeHelpers.GetNameForType<T>().ToLower()},
                {"default", Default.ID}
            };
        }

        private string GenerateDefaultSource()
        {
            return ShaderTemplateEvaluator.Evaluate(DefaultShaderCode, CreateTemplateMap());
        }

        protected virtual string GenerateSource(string theSourceCode, IEnumerable<AbstractGpuValue> theIns, IDictionary<string, string> theCustomValues = null)
        {
            
            if (ShaderNodesUtil.HasNullValue(theIns))
            {
                return GenerateDefaultSource();
            }

            var templateMap = CreateTemplateMap();
            theCustomValues?.ForEach(kv => templateMap.Add(kv.Key, kv.Value));

            return ShaderTemplateEvaluator.Evaluate(theSourceCode, templateMap);
        }
        
        protected void Setup(string theSourceCode, IEnumerable<AbstractGpuValue> theIns, IDictionary<string, string> theCustomValues = null)
        {
            var abstractGpuValues = theIns.ToList();
            _sourceCode = GenerateSource(theSourceCode, abstractGpuValues, theCustomValues);
            Ins = abstractGpuValues;
            Output.ParentNode = this;
        }
    }
}