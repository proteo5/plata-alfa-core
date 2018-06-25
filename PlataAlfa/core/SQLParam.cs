using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PlataAlfa.core
{
    public class  SQLParam
    {
        public string Param { get; set; }
        public SqlDbType SqlType { get; set; }
        public dynamic Value { get; set; }
    }
}
