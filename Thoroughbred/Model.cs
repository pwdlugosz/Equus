using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.QuarterHorse;
using Equus.Calabrese;
using Equus.Shire;
using Equus.Horse;

namespace Equus.Thoroughbred
{

    public abstract class Model
    {

        public abstract string Name
        {
            get;
            protected set;
        }

        public abstract string Statistics();

        public abstract void Render();

        public abstract void Extend(RecordWriter Output, DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where);

        public abstract RecordSet Extend(DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where);

        public abstract Table Extend(string Dir, string Name, DataSet Data, FNodeSet Inputs, FNodeSet OtherKeepValues, Predicate Where);


    }

}
