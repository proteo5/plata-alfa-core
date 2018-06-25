using PlataAlfa.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlataAlfa.data.V1_0.Admin
{
    public class UsuariosDS : DataSteward
    {

        public Envelope<List<dynamic>> GetByUsuario(dynamic data)
        {
            string options = $" WHERE [Usuario] = '{data.Usuario}' ";
            var dataSet = this.GetDataSet("*", options);
            return dataSet;
        }
       
    }
}
