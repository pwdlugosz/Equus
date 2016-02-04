using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Calabrese;
using Equus.HScript;
using Equus.Horse;
using Equus.HScript.Parameters;

namespace Equus.Clydesdale
{

    public sealed class ProcedureParameterMetaData
    {

        public ProcedureParameterMetaData(string Name, bool IsRequired, HParameterAffinity Type, string Description)
        {
            this.Name = Name;
            this.Required = IsRequired;
            this.Type = Type;
            this.Description = Description;
        }

        public ProcedureParameterMetaData(string Text)
        {
            string[] vars = Text.Split('!');
            if (vars.Length != 4)
                throw new Exception("Text passed has an invalid format");
            this.Name = vars[0];
            this.Required = (vars[1].ToLower() == "true");
            HParameterAffinity t = HParameterAffinity.Expression;
            bool b = Enum.TryParse<HParameterAffinity>(vars[2], true, out t);
            if (!b)
                throw new Exception(string.Format("Unknow type '{0}'", vars[2]));
            this.Type = t;
            this.Description = vars[3];
        }

        public string Name
        {
            get;
            set;
        }

        public bool Required
        {
            get;
            set;
        }

        public HParameterAffinity Type
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

    }

    public abstract class Procedure
    {

        protected string _Name;
        protected string _ProcedureDescription;
        protected Workspace _Home;
        protected Dictionary<string, ProcedureParameterMetaData> _ParametersMap;
        protected HParameterSet _Parameters;

        public Procedure(string Name, string Description, Workspace UseSpace, HParameterSet UseParameters)
        {

            this._Name = Name;
            this._ProcedureDescription = Description;
            this._Parameters = UseParameters;
            this._Home = UseSpace;
            this._ParametersMap = new Dictionary<string, ProcedureParameterMetaData>(StringComparer.OrdinalIgnoreCase);

        }

        public string Name
        {
            get { return this._Name; }
        }

        public int ParameterCount
        {
            get
            {
                return this._ParametersMap.Count;
            }
        }

        public Workspace Home
        {
            get { return this._Home; }
        }

        public bool CheckInvoke(out string ErrorDesc)
        {

            foreach (KeyValuePair<string, ProcedureParameterMetaData> kv in this._ParametersMap)
            {
                
                // Check the required parameter exists //
                if (kv.Value.Required && !this._Parameters.Exists(kv.Key))
                {
                    ErrorDesc = string.Format("Missing required parameter '{0}'", kv.Key);
                    return false;
                }

            }
            ErrorDesc = null;
            return true;

        }

        public abstract void Invoke();

        public virtual string Info()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this._ProcedureDescription);
            foreach (KeyValuePair<string, ProcedureParameterMetaData> kv in this._ParametersMap)
                sb.AppendLine(string.Format("{0}: Required? {1}, Type {2}, Description {3}", kv.Value.Name, kv.Value.Required, kv.Value.Type, kv.Value.Description));
            return sb.ToString();
        }

    }

}
