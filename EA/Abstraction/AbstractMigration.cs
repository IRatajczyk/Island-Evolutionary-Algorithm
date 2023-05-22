using IEA.ProblemInstance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEA.EA.Abstraction
{
    internal interface IMigrationParameters
    {
        public double MigrationPropability { get;  }
        public int MigrantsCount { get;  }

        public double ChoicePopabilityFactor { get;  }

        public bool SendCopy { get;  }
    }

    internal interface IMigration
    {
        public List<Solution> Migrate(List<Solution> population);
    }
}
