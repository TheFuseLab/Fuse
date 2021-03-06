﻿using System.Collections.Generic;

namespace Fuse
{
    public class SemanticValue <T>: GpuValue<T>
    {
        public SemanticValue(string theName) : base(theName)
        {
        }
        
        public override string ID => "streams." + Name;
    }
    public class Semantic<T> : ShaderNode<T>
    {
        
        public Semantic (string theSemantic) : base("Semantic")
        {
            Output = new SemanticValue<T>(theSemantic);
            Setup(new List<AbstractGpuValue>() );
        }

        protected override string SourceTemplate()
        {
            return "";
        }
    }
}