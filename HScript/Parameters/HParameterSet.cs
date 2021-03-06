﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;
using Equus.Fjord;
using Equus.Shire;

namespace Equus.HScript.Parameters
{

    public sealed class HParameterSet
    {

        private Dictionary<string, HParameter> _parameters;

        public HParameterSet()
        {
            this._parameters = new Dictionary<string, HParameter>(StringComparer.OrdinalIgnoreCase);
        }

        public int Count
        {
            get { return this._parameters.Count; }
        }

        public HParameter this[string Name]
        {
            get { return this._parameters[Name]; }
        }

        public void Add(string Name, HParameter Value)
        {
            if (this._parameters.ContainsKey(Name))
                throw new HScriptCompileException("Parameter '{0}' already exists", Name);
            this._parameters.Add(Name, Value);
        }

        public void Add(string Name, DataSet Value)
        {
            this.Add(Name, new HParameter(Value));
        }

        public void Add(string Name, FNode Value)
        {
            this.Add(Name, new HParameter(Value));
        }

        public void Add(string Name, FNodeSet Value)
        {
            this.Add(Name, new HParameter(Value));
        }

        public void Add(string Name, Lambda Value)
        {
            this.Add(Name, new HParameter(Value));
        }

        public void Add(string Name, MNode Value)
        {
            this.Add(Name, new HParameter(Value));
        }

        public void Add(string Name, CellHeap Heap, string ScalarName)
        {
            this.Add(Name, Heap, ScalarName);
        }

        public bool Exists(string Name)
        {
            return this._parameters.ContainsKey(Name);
        }


    }

}
