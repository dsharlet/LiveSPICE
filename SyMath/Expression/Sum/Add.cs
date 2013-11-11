using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SyMath
{
    /// <summary>
    /// Trivial implementation of Sum.
    /// </summary>
    class Add : Sum
    {
        protected List<Expression> terms;
        public override IEnumerable<Expression> Terms { get { return terms; } }

        public Add(IEnumerable<Expression> Terms)
        {
            terms = Terms.ToList();
        }
    }
}
